import os
import re
import sys
import uuid
import zipfile
import argparse
import traceback
import subprocess
import configparser


class CommandLineArgs:
   """ CommandLineArgs - Reads command-line arguments, validates them and provides access to them

   """
   class Exception(RuntimeError):
      pass

   def __init__(self, argv):
      parser = argparse.ArgumentParser(formatter_class=argparse.RawTextHelpFormatter,\
         description = 'mrHelper build script')

      versionHelp = 'Version number (e.g. 0.4.6.0)'
      parser.add_argument('-v', '--version', dest='version', nargs=1, help=versionHelp)

      pushHelp = 'Push incremented version to remote'
      parser.add_argument('-p', '--push', action='store_true', help=pushHelp)

      configHelp = 'Configuration filename with path'
      parser.add_argument('-c', '--config', dest='config', nargs=1, help=configHelp)

      msiHelp = 'Create MSI (if not specified, creates a zip)'
      parser.add_argument('-m', '--msi', action='store_true', help=msiHelp)

      self.args = parser.parse_known_args(argv)[0]

      if not self.args.version:
         parser.print_usage()
         raise self.Exception('Bar command-line arguments')
      elif not self._validateVersion(self.version()):
         raise self.Exception('Bad version format')

      if not self.args.config:
         self.args.config = ['make_build.cfg']
      else:
         if not os.path.exists(self.config()) or not os.path.isfile(self.config()):
            raise self.Exception(f'Bad configuration file "{self.config()}"')

   def version(self):
      return self.args.version[0]

   def config(self):
      return self.args.config[0]

   def msi(self):
      return self.args.msi

   def push(self):
      return self.args.push

   def _validateVersion(self, version):
      version_rex = re.compile(r'^(?:[0-9]+\.){3}[0-9]+$')
      return re.match(version_rex, version)


class ScriptConfigParser:
   """ ScriptConfigParser - Reads configuration file, validates its content and provides access to it

   """
   class Exception(RuntimeError):
      pass

   def __init__(self, filename):
      self.config = configparser.ConfigParser()
      self.config.read(filename)
      self._initialize(filename)
      self._validate()

   def repository(self):
      return self.config.get('Path', 'repository')

   def extras(self):
      return self.config.get('Path', 'Extras')

   def bin(self):
      return self.config.get('Path', 'Bin')

   def build_script(self):
      return self.config.get('Path', 'BuildScript')

   def version_file(self):
      return self.config.get('Version', 'AssemblyInfo')

   def installer_project(self):
      return self.config.get('Installer', 'project')

   def msi_original_name(self):
      return self.config.get('Installer', 'msi_original_name')

   def msi_target_name_template(self):
      return self.config.get('Installer', 'msi_target_name_template')

   def _initialize(self, filename):
      self._addOption('Path', 'repository', '.')
      self._addOption('Path', 'Extras', 'extras')
      self._addOption('Path', 'Bin', 'bin')
      self._addOption('Path', 'BuildScript', 'build/build-release.bat')
      self._addOption('Version', 'AssemblyInfo', 'Properties/SharedAssemblyInfo.cs')
      self._addOption('Installer', 'project', '')
      self._addOption('Installer', 'msi_original_name', '')
      self._addOption('Installer', 'msi_target_name_template', '')

      with open(filename, 'w') as configFile:
         self.config.write(configFile)

   def _addOption(self, section, option, value):
      if not self.config.has_section(section):
         self.config.add_section(section)
      if not self.config.has_option(section, option):
         self.config.set(section, option, value)

   def _validate(self):
      self._validatePathInConfig(self.config, 'Path', 'repository')
      self._validatePathInConfig(self.config, 'Path', 'Extras')
      self._validatePathInConfig(self.config, 'Path', 'Bin')
      self._validateFileInConfig(self.config, 'Path', 'BuildScript')
      self._validateFileInConfig(self.config, 'Version', 'AssemblyInfo')
      self._validateFileInConfig(self.config, 'Installer', 'project')

   def _validatePathInConfig(self, config, section, option):
      path = self.config.get(section, option)
      if not os.path.exists(path) or not os.path.isdir(path):
         raise self.Exception(f'Bad path "{path}"')

   def _validateFileInConfig(self, config, section, option):
      path = self.config.get(section, option)
      if not os.path.exists(path) or not os.path.isfile(path):
         raise self.Exception(f'Bad file "{path}"')


class PreBuilder:
   """ PreBuilder - Performs pre-build steps

   """
   class Exception(RuntimeError):
      pass

   def __init__(self, version, installer_project):
      self.version = version

      if not os.path.exists(installer_project) or not os.path.isfile(installer_project):
         raise self.Exception(f'Bad installer project file "{installer_project}"')
      self.installer_project = installer_project

   def prebuild(self):
      self._write_version()

   def _write_version(self):
      version_rex = re.compile(r'(\s*\"ProductVersion\"\s*=\s*\"8)(?:\:[0-9\.]+\")')
      product_code_rex = re.compile(r'(\s*\"ProductCode\"\s*=\s*\"8:)(?:{[0-9A-F\-]+})(\")')

      lines = []
      with open (self.installer_project, 'r') as f:
         for line in f:
            if re.match(version_rex, line):
               lines.append(version_rex.sub(r'\1:{0}"'.format(self.version[:-2]), line))
            elif re.match(product_code_rex, line):
               guid = str(uuid.uuid4())
               guid = guid.upper()
               print(guid)
               lines.append(product_code_rex.sub(r'\1{{{0}}}\2'.format(guid), line))
            else:
               lines.append(line)

      with open(self.installer_project, 'w') as f:
         for line in lines:
            f.write(line)


class PostBuilder:
   """ PostBuilder - Performs post-build steps

   """
   class Exception(RuntimeError):
      pass

   def __init__(self, version, bin_path, msi_name, msi_target_name_template):
      self.version = version

      msi_path = os.path.join(bin_path, msi_name)
      if not os.path.exists(msi_path) or not os.path.isfile(msi_path):
         raise self.Exception(f'Bad MSI file "{msi_path}"')
      self.msi_path = msi_path
      self.bin_path = bin_path
      self.msi_target_name_template = msi_target_name_template

   def postbuild(self):
      self._rename_msi()

   def _rename_msi(self):
      msi_new_name = self.msi_target_name_template
      msi_new_name = msi_new_name.replace('{Version}', self.version)
      msi_new_path = os.path.join(self.bin_path, msi_new_name)
      os.rename(self.msi_path, msi_new_path)


class Builder:
   """ Builder - Increments a version of application and runs build script

   """
   class Exception(RuntimeError):
      pass

   def __init__(self, version, version_file, script_file):
      self.version = version

      if not os.path.exists(version_file) or not os.path.isfile(version_file):
         raise self.Exception(f'Bad file with versions "{version_file}"')
      self.version_file = version_file

      if not os.path.exists(script_file) or not os.path.isfile(script_file):
         raise self.Exception(f'Bad build script "{script_file}"')
      self.script_file = script_file

   def build(self):
      self._write_version()
      return subprocess.call(f'call {self.script_file}', shell=True) == 0

   def _write_version(self):
      assembly_rex = re.compile(r'(\[assembly\:\s*\S*)(?:\"[0-9\.]+\")(\)\])')

      lines = []
      with open(self.version_file, 'r') as f:
         for line in f:
            if re.match(assembly_rex, line):
               lines.append(assembly_rex.sub(r'\1"{0}"\2'.format(self.version), line))
            else:
               lines.append(line)

      if len(lines) < 2:
         raise self.Exception(f'Unexpected format of file "{self.version_file}"')

      with open(self.version_file, 'w') as f:
         for line in lines:
            f.write(line)


class PackageMaker:
   """ PackageMaker - Creates a build package

   """
   class Exception(RuntimeError):
      pass

   def __init__(self, version, bin_path, extras_path):
      self.version = version

      if not os.path.exists(bin_path) or not os.path.isdir(bin_path):
         raise self.Exception(f'Bad path to binaries "{bin_path}"')
      self.bin_path = bin_path

      if not os.path.exists(extras_path) or not os.path.isdir(extras_path):
         raise self.Exception(f'Bad path to extras "{extras_path}"')
      self.extras_path = extras_path

   def make_package(self):
      archive_name = f"mrHelper-{self.version}.bin.zip"

      if os.path.exists(archive_name):
         if os.path.isdir(archive_name):
            raise self.Exception(f'Cannot create archive because directory "{archive_name}" exists')
         elif os.path.isfile(archive_name):
            raise self.Exception(f'Cannot create archive because file "{archive_name}" already exists')
         else:
            assert(False)

      self._pack(archive_name, [self.bin_path, self.extras_path])

   def _pack(self, archive_name, paths) :
      with zipfile.ZipFile(archive_name, 'a', zipfile.ZIP_DEFLATED) as zip:
         for path in paths:
            if os.path.isdir(path):
               for base, dirs, files in os.walk(path):
                  for file in files:
                     fn = os.path.join(base, file)
                     zip.write(fn, file)
            else :
               assert os.path.isfile(path)
               zip.write(path, os.path.split(path)[1])


class RepositoryHelper:
   """ RepositoryHelper - updates remote git repository

   """
   class Exception(RuntimeError):
      pass

   def __init__(self, repository):
      self.repository = repository

      if not os.path.exists(repository) or not os.path.isdir(repository):
         raise self.Exception(f'Bad path to git repository "{repository}"')

      self.exec_in_repository(lambda : self._check())

   def push(self, version_file):
      self.exec_in_repository(lambda : self._push(version_file))

   def add_tag(self, tag):
      self.exec_in_repository(lambda : self._add_tag(tag))

   def exec_in_repository(self, f):
      curdir = os.getcwd()
      os.chdir(self.repository)

      try:
         f()
      finally:
         os.chdir(curdir)

   def _check(self):
      if subprocess.call(f"git rev-parse") != 0:
         raise self.Exception(f'Not a valid git repository "{repository}"')

   def _push(self, version_file):
      if subprocess.call(f"git add {version_file}") != 0:
         raise self.Exception(f'Cannot add "{version_file}" to git index')

      if subprocess.call(f"git commit -m \"Increment version\"") != 0:
         raise self.Exception(f'Cannot create a commit')

      if subprocess.call(f"git push") != 0:
         raise self.Exception(f'Failed to push commit to remote')

   def _add_tag(self, tag):
      if subprocess.call(f"git tag {tag}") != 0:
         raise self.Exception(f'Cannot create a tag "{tag}"')

      if subprocess.call(f"git push origin --tags") != 0:
         raise self.Exception(f'Failed to push new tag to remote')


def get_status_message(succeeded, step_name, version_incremented, build_created, pushed):
   general = f'Succeeded' if succeeded else f'Fatal error at step "{step_name}".'
   version = f'Version number updated: {"Yes" if version_incremented else "No"}.'
   build = f'Build package created {"Yes" if build_created else "No"}.'
   push = f'Pushed to git: {"Yes" if pushed else "No"}.'
   return f'{general}\n{version}\n{build}\n{push}\n{"" if succeeded else "Details:"}'

def handle_error(err_code, e, step_name, version_incremented, build_created, pushed):
   print(get_status_message(False, step_name, version_incremented, build_created, pushed))
   print(e)
   sys.exit(err_code)


try:
   args = CommandLineArgs(sys.argv)

   config = ScriptConfigParser(args.config())

   if args.msi():
      prebuilder = PreBuilder(args.version(), config.installer_project())
      prebuilder.prebuild()

   builder = Builder(args.version(), config.version_file(), config.build_script())
   builder.build()

   if args.msi():
      postbuilder = PostBuilder(args.version(), config.bin(), config.msi_original_name(), config.msi_target_name_template())
      postbuilder.postbuild()

   if not args.msi():
      maker = PackageMaker(args.version(), config.bin(), config.extras())
      maker.make_package()

   if args.push():
      repository = RepositoryHelper(config.repository())
      repository.push(config.version_file())
      repository.add_tag(f"build-{args.version()}")

except ScriptConfigParser.Exception as e:
   handle_error(1, e, "validating config", False, False, False)
except CommandLineArgs.Exception as e:
   handle_error(2, e, "parsing command-line", False, False, False)
except PreBuilder.Exception as e:
   handle_error(6, e, "pre-build step", False, False, False)
except PreBuilder.Exception as e:
   handle_error(7, e, "post-build step", False, True, False)
except Builder.Exception as e:
   handle_error(3, e, "preparing to build", False, False, False)
except PackageMaker.Exception as e:
   handle_error(4, e, "creating archive", True, False, False)
except RepositoryHelper.Exception as e:
   handle_error(5, e, "working with git", True, True, False)
except Exception as e:
   print('Unknown error.\nDetails:')
   print(e)
   print(traceback.format_exc())
   sys.exit(6)
else:
   print(get_status_message(True, "", True, True, args.push()))
   sys.exit(0)

