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

      deployHelp = 'Deploy MSI/MSIX to a shared location'
      parser.add_argument('-d', '--deploy', action='store_true', help=deployHelp)

      msixHelp = 'Build (and deploy) MSIX'
      parser.add_argument('-x', '--msix', action='store_true', help=msixHelp)

      betaHelp = 'Deploy to Beta folder'
      parser.add_argument('-b', '--beta', action='store_true', help=betaHelp)

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

   def msix(self):
      return self.args.msix

   def beta(self):
      return self.args.beta

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
      return self.config.get('Installer', 'Bin')

   def msix_bin(self):
      return self.config.get('Installer', 'msix_Bin')

   def build_script(self):
      return self.config.get('Path', 'BuildScript')

   def msix_build_script(self):
      return self.config.get('Path', 'msix_BuildScript')

   def version_file(self):
      return self.config.get('Version', 'AssemblyInfo')

   def msi_target_name_template(self):
      return self.config.get('Installer', 'msi_target_name_template')

   def msix_manifest(self):
      return self.config.get('Installer', 'msix_manifest')

   def msix_target_name_template(self):
      return self.config.get('Installer', 'msix_target_name_template')

   def latest_version_filename(self):
      return self.config.get('Deploy', 'latest_version_filename')

   def deploy_path(self):
      return self.config.get('Deploy', 'path')

   def beta_path(self):
      return self.config.get('Deploy', 'beta_path')

   def _initialize(self, filename):
      self._addOption('Path', 'repository', '.')
      self._addOption('Path', 'Extras', 'extras')
      self._addOption('Installer', 'Bin', 'bin')
      self._addOption('Installer', 'msix_Bin', 'bin')
      self._addOption('Path', 'BuildScript', 'build/build-install.bat')
      self._addOption('Path', 'msix_BuildScript', 'build/build-publish.bat')
      self._addOption('Version', 'AssemblyInfo', 'Properties/SharedAssemblyInfo.cs')
      self._addOption('Installer', 'msi_target_name_template', '')
      self._addOption('Installer', 'msix_manifest', '')
      self._addOption('Installer', 'msix_target_name_template', '')
      self._addOption('Deploy', 'latest_version_filename', 'latest')
      self._addOption('Deploy', 'path', '')
      self._addOption('Deploy', 'beta_path', '')

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
      self._validatePathInConfig(self.config, 'Installer', 'Bin')
      self._validatePathInConfig(self.config, 'Installer', 'msix_Bin')
      self._validateFileInConfig(self.config, 'Path', 'BuildScript')
      self._validateFileInConfig(self.config, 'Path', 'msix_BuildScript')
      self._validateFileInConfig(self.config, 'Version', 'AssemblyInfo')
      self._validateFileInConfig(self.config, 'Installer', 'msix_manifest')
      self._validatePathInConfig(self.config, 'Deploy', 'path')
      self._validatePathInConfig(self.config, 'Deploy', 'beta_path')

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

   def __init__(self, version, version_file, msix_manifest):
      self.version = version

      if not os.path.exists(version_file) or not os.path.isfile(version_file):
         raise self.Exception(f'Bad file with versions "{version_file}"')
      self.version_file = version_file

      if not os.path.exists(msix_manifest) or not os.path.isfile(msix_manifest):
         raise self.Exception(f'Bad MSIX manifest file "{msix_manifest}"')
      self.msix_manifest = msix_manifest

   def prebuild(self):
      self._write_version_code()
      self._write_version_msix()

   def _write_version_code(self):
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

   def _write_version_msix(self):
      identity_version_rex = re.compile(r'(\s*<Identity.*Version=)(?:\"[0-9\.]+\")(.*)')

      lines = []
      with open (self.msix_manifest, 'r') as f:
         for line in f:
            if re.match(identity_version_rex, line):
               lines.append(identity_version_rex.sub(r'\1"{0}"\2'.format(self.version), line))
            else:
               lines.append(line)

      with open(self.msix_manifest, 'w') as f:
         for line in lines:
            f.write(line)


class Builder:
   """ Builder - Runs build script

   """
   class Exception(RuntimeError):
      pass

   def build(self, script_file, arguments):
      if not os.path.exists(script_file) or not os.path.isfile(script_file):
         raise self.Exception(f'Bad build script "{script_file}"')
      self.script_file = script_file

      if subprocess.call(f'call {self.script_file} {arguments}', shell=True) != 0:
         raise self.Exception(f'Build failed')


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

   def deploy(self, version, installer_filepath):
      return self._copy_to_remote(version, installer_filepath)

   def update_version_information(self, version, json):
      self._update_version_information_at_remote(version, json)

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

   def _update_version_information_at_remote(self, version, json):
      with open(config.latest_version_filename(), 'w') as latestVersion:
         latestVersion.write(json)
      dest_latest_version_filename = os.path.join(self.deploy_path, config.latest_version_filename())
      shutil.copyfile(config.latest_version_filename(), dest_latest_version_filename)
      os.remove(config.latest_version_filename())


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

   prebuilder = PreBuilder(args.version(), config.version_file(), config.msix_manifest())
   prebuilder.prebuild()

   builder = Builder()

   msi_filename = config.msi_target_name_template().replace("{Version}", args.version())
   msi_filepath = os.path.join(config.bin(), msi_filename)
   msix_filename = config.msix_target_name_template().replace("{Version}", args.version())
   msix_filepath = os.path.join(config.msix_bin(), msix_filename)

   builder.build(config.build_script(), msi_filepath)
   if args.msix():
      builder.build(config.msix_build_script(), msix_filepath + " " + config.msix_manifest())

   if args.deploy():
      deployer = DeployHelper(config.beta_path() if args.beta() else config.deploy_path())
      dest_msi = deployer.deploy(args.version(), msi_filepath).replace("\\", "/")
      if args.msix():
         dest_msix = deployer.deploy(args.version(), msix_filepath).replace("\\", "/")
      if not args.beta():
         if args.msix():
            json = f'{{ "VersionNumber": "{args.version()}", "InstallerFilePath": "{dest_msi}", "XInstallerFilePath": "{dest_msix}" }}'
         else:
            json = f'{{ "VersionNumber": "{args.version()}", "InstallerFilePath": "{dest_msi}" }}'
         deployer.update_version_information(args.version(), json)

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

