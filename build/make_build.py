import os
import re
import sys
import uuid
import shutil
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

      deployHelp = 'Deploy MSI to a shared location'
      parser.add_argument('-d', '--deploy', action='store_true', help=deployHelp)

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

   def push(self):
      return self.args.push

   def deploy(self):
      return self.args.deploy

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

   def msix_bin(self):
      return self.config.get('Path', 'msix_Bin')

   def build_script(self):
      return self.config.get('Path', 'BuildScript')

   def msix_build_script(self):
      return self.config.get('Path', 'msix_BuildScript')

   def version_file(self):
      return self.config.get('Version', 'AssemblyInfo')

   def installer_project(self):
      return self.config.get('Installer', 'project')

   def msi_original_name(self):
      return self.config.get('Installer', 'msi_original_name')

   def msi_target_name_template(self):
      return self.config.get('Installer', 'msi_target_name_template')

   def msix_manifest(self):
      return self.config.get('Installer', 'msix_manifest')

   def msix_original_name(self):
      return self.config.get('Installer', 'msix_original_name')

   def msix_target_name_template(self):
      return self.config.get('Installer', 'msix_target_name_template')

   def latest_version_filename(self):
      return self.config.get('Deploy', 'latest_version_filename')

   def deploy_path(self):
      return self.config.get('Deploy', 'path')

   def _initialize(self, filename):
      self._addOption('Path', 'repository', '.')
      self._addOption('Path', 'Extras', 'extras')
      self._addOption('Path', 'Bin', 'bin')
      self._addOption('Path', 'msix_Bin', 'bin')
      self._addOption('Path', 'BuildScript', 'build/build-install.bat')
      self._addOption('Path', 'msix_BuildScript', 'build/build-publish.bat')
      self._addOption('Version', 'AssemblyInfo', 'Properties/SharedAssemblyInfo.cs')
      self._addOption('Installer', 'project', '')
      self._addOption('Installer', 'msi_original_name', '')
      self._addOption('Installer', 'msi_target_name_template', '')
      self._addOption('Installer', 'msix_manifest', '')
      self._addOption('Installer', 'msix_original_name', '')
      self._addOption('Installer', 'msix_target_name_template', '')
      self._addOption('Deploy', 'latest_version_filename', 'latest')
      self._addOption('Deploy', 'path', '')

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
      self._validatePathInConfig(self.config, 'Path', 'msix_Bin')
      self._validateFileInConfig(self.config, 'Path', 'BuildScript')
      self._validateFileInConfig(self.config, 'Path', 'msix_BuildScript')
      self._validateFileInConfig(self.config, 'Version', 'AssemblyInfo')
      self._validateFileInConfig(self.config, 'Installer', 'project')
      self._validateFileInConfig(self.config, 'Installer', 'msix_manifest')
      self._validatePathInConfig(self.config, 'Deploy', 'path')

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

   def __init__(self, version, installer_project, msix_manifest):
      self.version = version

      if not os.path.exists(installer_project) or not os.path.isfile(installer_project):
         raise self.Exception(f'Bad installer project file "{installer_project}"')
      self.installer_project = installer_project

      if not os.path.exists(msix_manifest) or not os.path.isfile(msix_manifest):
         raise self.Exception(f'Bad MSIX manifest file "{msix_manifest}"')
      self.msix_manifest = msix_manifest

   def prebuild(self):
      self._write_version()
      self._write_version_msix()

   def _write_version(self):
      version_rex = re.compile(r'(\s*\"ProductVersion\"\s*=\s*\"8)(?:\:[0-9\.]+\")')
      product_code_rex = re.compile(r'(\s*\"ProductCode\"\s*=\s*\"8:)(?:{[0-9A-F\-]+})(\")')
      assembly_version_rex = re.compile(r'(\s*\"AssemblyAsmDisplayName\".*\"8:mrHelper.*Version)(?:=[0-9\.]+,)(.*)')

      lines = []
      with open (self.installer_project, 'r') as f:
         for line in f:
            if re.match(version_rex, line):
               three_digit_version = self.version[:-2]
               lines.append(version_rex.sub(r'\1:{0}"'.format(three_digit_version), line))
            elif re.match(product_code_rex, line):
               guid = str(uuid.uuid4())
               guid = guid.upper()
               lines.append(product_code_rex.sub(r'\1{{{0}}}\2'.format(guid), line))
            elif re.match(assembly_version_rex, line):
               lines.append(assembly_version_rex.sub(r'\1={0},\2'.format(self.version), line))
            else:
               lines.append(line)

      with open(self.installer_project, 'w') as f:
         for line in lines:
            f.write(line)

   def _write_version_msix(self):
      identity_version_rex = re.compile(r'(\s*<Identity.*Version=)(?:\"[0-9\.]+\")(.*)')

      lines = []
      with open (self.msix_manifest, 'r') as f:
         for line in f:
            if re.match(identity_version_rex, line):
               three_digit_version = self.version[:-2]
               lines.append(identity_version_rex.sub(r'\1"{0}"\2'.format(three_digit_version), line))
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
      return self._rename_msi()

   def _rename_msi(self):
      msi_new_name = self.msi_target_name_template
      msi_new_name = msi_new_name.replace('{Version}', self.version)
      msi_new_path = os.path.join(self.bin_path, msi_new_name)
      os.rename(self.msi_path, msi_new_path)
      return msi_new_path


class Builder:
   """ Builder - Increments a version of application and runs build script

   """
   class Exception(RuntimeError):
      pass

   def __init__(self, version, version_file, script_file, msix_script_file):
      self.version = version

      if not os.path.exists(version_file) or not os.path.isfile(version_file):
         raise self.Exception(f'Bad file with versions "{version_file}"')
      self.version_file = version_file

      if not os.path.exists(script_file) or not os.path.isfile(script_file):
         raise self.Exception(f'Bad build script "{script_file}"')
      self.script_file = script_file

      if not os.path.exists(msix_script_file) or not os.path.isfile(msix_script_file):
         raise self.Exception(f'Bad MSIX build script "{msix_script_file}"')
      self.msix_script_file = msix_script_file

   def build(self):
      self._write_version()
      r1 = subprocess.call(f'call {self.script_file}', shell=True) == 0
      return False if r1 == False else subprocess.call(f'call {self.msix_script_file}', shell=True) == 0

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


class DeployHelper:
   """ DeployHelper - deploys files to a location

   """
   class Exception(RuntimeError):
      pass

   def __init__(self, deploy_path):
      self.deploy_path = deploy_path

      if not os.path.exists(deploy_path) or not os.path.isdir(deploy_path):
         raise self.Exception(f'Bad path for deployment "{deploy_path}"')

   def deploy(self, version, installer_filepath, msix_installer_filepath):
      dest_msi_filepath = _copy_to_remote(self, version, installer_filepath)
      dest_msix_filepath = _copy_to_remote(self, version, msix_installer_filepath)
      _update_version_information_at_remote(version, dest_msi_filepath, dest_msix_filepath);

   def _copy_to_remote(self, version, installer_filepath):
      if not os.path.exists(installer_filepath) or not os.path.isfile(installer_filepath):
         raise self.Exception(f'Installer cannot be found at "{installer_filepath}"')

      splitted = os.path.split(os.path.abspath(installer_filepath))
      if len(splitted) != 2:
         raise self.Exception(f'Bad path "{installer_filepath}"')

      installer_filename = splitted[1]

      dest_installer_path = os.path.join(self.deploy_path, version)
      if not os.path.exists(dest_installer_path):
         os.mkdir(dest_installer_path)
      elif os.path.isfile(dest_installer_path):
         raise self.Exception(f'Cannot create a directory "{dest_installer_path}"')

      dest_installer_filepath = os.path.join(dest_installer_path, installer_filename)
      if os.path.exists(dest_installer_filepath):
         if os.path.isfile(dest_installer_filepath):
            os.remove(dest_installer_filepath)
         else:
            raise self.Exception(f'Cannot copy installer to "{dest_installer_filepath}" \
                                   because a directory with the same name already exists')
      shutil.copyfile(installer_filepath, dest_installer_filepath)
      return dest_installer_filepath

   def _update_version_information_at_remote(self, version, dest_msi_filepath, dest_msi_filepath):
      with open(config.latest_version_filename(), 'w') as latestVersion:
         latestVersion.write(self._get_json(args.version(), dest_msi_filepath, dest_msix_filepath))
      dest_latest_version_filename = os.path.join(self.deploy_path, config.latest_version_filename())
      shutil.copyfile(config.latest_version_filename(), dest_latest_version_filename)
      os.remove(config.latest_version_filename())

   def _get_json(self, version, msi_filepath, msix_filepath):
      msi_path = msi_filepath.replace("\\", "/")
      msix_path = msix_filepath.replace("\\", "/")
      return f'{{ "VersionNumber": "{version}", \
                  "InstallerFilePath": "{msi_path}", \
                  "XInstallerFilePath": "{msix_path}" }}'


def get_status_message(succeeded, step_name, version_incremented, build_created, pushed, deploy):
   general = f'Succeeded' if succeeded else f'Fatal error at step "{step_name}".'
   version = f'Version number updated: {"Yes" if version_incremented else "No"}.'
   build = f'Build package created {"Yes" if build_created else "No"}.'
   push = f'Pushed to git: {"Yes" if pushed else "No"}.'
   deploy = f'Deployed: {"Yes" if deploy else "No"}.'
   return f'{general}\n{version}\n{build}\n{push}\n{deploy}\n{"" if succeeded else "Details:"}'

def handle_error(err_code, e, step_name, version_incremented, build_created, pushed, deployed):
   print(get_status_message(False, step_name, version_incremented, build_created, pushed, deployed))
   print(e)
   sys.exit(err_code)


try:
   args = CommandLineArgs(sys.argv)

   config = ScriptConfigParser(args.config())

   prebuilder = PreBuilder(args.version(), config.installer_project())
   prebuilder.prebuild()

   builder = Builder(args.version(), config.version_file(), config.build_script())
   builder.build()

   postbuilder = PostBuilder(args.version(), config.bin(), \
      config.msi_original_name(), config.msi_target_name_template())
   installer_filename = postbuilder.postbuild()

   postbuilder = PostBuilder(args.version(), config.bin(), \
      config.msix_original_name(), config.msix_target_name_template())
   msix_installer_filename = postbuilder.postbuild()

   if args.deploy():
      deployer = DeployHelper(config.deploy_path())
      deployer.deploy(args.version(), installer_filename, msix_installer_filename)

   if args.push():
      repository = RepositoryHelper(config.repository())
      repository.push(config.version_file())
      repository.add_tag(f"build-{args.version()}")

except ScriptConfigParser.Exception as e:
   handle_error(1, e, "validating config", False, False, False, False)
except CommandLineArgs.Exception as e:
   handle_error(2, e, "parsing command-line", False, False, False, False)
except PreBuilder.Exception as e:
   handle_error(6, e, "pre-build step", False, False, False, False)
except PreBuilder.Exception as e:
   handle_error(7, e, "post-build step", False, True, False, False)
except Builder.Exception as e:
   handle_error(3, e, "preparing to build", False, False, False, False)
except RepositoryHelper.Exception as e:
   handle_error(5, e, "working with git", True, True, False, False)
except DeployHelper.Exception as e:
   handle_error(8, e, "deployment", True, True, True, False)
except Exception as e:
   print('Unknown error.\nDetails:')
   print(e)
   print(traceback.format_exc())
   sys.exit(6)
else:
   print(get_status_message(True, "", True, True, args.push(), args.deploy()))
   sys.exit(0)

