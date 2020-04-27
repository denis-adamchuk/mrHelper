using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.GitClient
{
   public static class GitTools
   {
      public class SSLVerificationDisableException : ExceptionEx
      {
         internal SSLVerificationDisableException(Exception innerException)
            : base(String.Empty, innerException)
         {
         }
      }

      public static void DisableSSLVerification()
      {
         try
         {
            ExternalProcess.Start("git", "config --global http.sslVerify false", true, String.Empty);
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               throw new SSLVerificationDisableException(ex);
            }
            throw;
         }
      }

      public static class GitVersionAccessor
      {
         public class UnknownVersionException : ExceptionEx
         {
            internal UnknownVersionException(Exception innerException)
               : base(String.Empty, innerException)
            {
            }
         }

         public static Version GetVersion()
         {
            if (_cachedVersion == null)
            {
               _cachedVersion = getVersion();
            }
            return _cachedVersion;
         }

         private static Version _cachedVersion;

         private static Version getVersion()
         {
            try
            {
               IEnumerable<string> stdOut =
                  ExternalProcess.Start("git", "--version", true, String.Empty).StdOut;
               if (!stdOut.Any())
               {
                  throw new UnknownVersionException(null);
               }

               Match m = gitVersion_re.Match(stdOut.First());
               if (!m.Success || m.Groups.Count < 3
                || !m.Groups["major"].Success || !m.Groups["minor"].Success || !m.Groups["build"].Success
                || !int.TryParse(m.Groups["major"].Value, out int major)
                || !int.TryParse(m.Groups["minor"].Value, out int minor)
                || !int.TryParse(m.Groups["build"].Value, out int build))
               {
                  throw new UnknownVersionException(null);
               }

               return new Version(major, minor, build);
            }
            catch (Exception ex)
            {
               if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
               {
                  throw new UnknownVersionException(ex);
               }
               throw;
            }
         }

         private static readonly Regex gitVersion_re =
            new Regex(@"git version (?'major'\d+).(?'minor'\d+).(?'build'\d+)");
      }

      public static bool SupportsFetchAutoGC()
      {
         try
         {
            Version version = GitVersionAccessor.GetVersion();
            return version.Major > 2 || (version.Major == 2 && version.Minor >= 23);
         }
         catch (GitVersionAccessor.UnknownVersionException ex)
         {
            ExceptionHandlers.Handle("Cannot detect git version", ex);
         }
         return false;
      }

      public static void TraceGitConfiguration()
      {
         try
         {
            foreach (string arguments in
               new string[]{ "--version", "config --global --list", "config --system --list" })
            {
               IEnumerable<string> stdOut = ExternalProcess.Start("git", arguments, true, String.Empty).StdOut;
               if (stdOut.Any())
               {
                  Trace.TraceInformation(String.Format("git {0} ==>\n{1}", arguments, String.Join("\n", stdOut)));
               }
            }
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               ExceptionHandlers.Handle("Cannot trace git configuration", ex);
            }
         }
      }

      async public static Task<bool> DoesEntityExistAtPathAsync(
         IExternalProcessManager operationManager, string path, string entity)
      {
         string arguments = String.Format("cat-file -t {0}", entity);
         try
         {
            ExternalProcess.AsyncTaskDescriptor descriptor =
               operationManager.CreateDescriptor("git", arguments, path, null);
            await operationManager.Wait(descriptor);
            return descriptor.StdErr.Count() == 0;
         }
         catch (GitException)
         {
            return false;
         }
      }

      public static ProjectKey? GetRepositoryProjectKey(string path)
      {
         if (_repositoryKeys.TryGetValue(path, out ProjectKey? value))
         {
            return value;
         }

         _repositoryKeys.Add(path, null);
         string repositoryName = getRepositoryName(path);
         if (!String.IsNullOrWhiteSpace(repositoryName))
         {
            Match m = gitRepo_re.Match(repositoryName);
            if (m.Success && m.Groups.Count == 5 && m.Groups[3].Success && m.Groups[4].Success)
            {
               string hostname = StringUtils.GetHostWithPrefix(m.Groups[3].Value);

               string gitSuffix = ".git";
               int startIndex = m.Groups[4].Value.StartsWith(":") ? 1 : 0;
               int endIndex = m.Groups[4].Value.EndsWith(gitSuffix)
                  ? m.Groups[4].Value.Length - gitSuffix.Length : m.Groups[4].Value.Length;

               string project = m.Groups[4].Value.Substring(startIndex, endIndex - startIndex);
               ProjectKey projectKey = new ProjectKey
               {
                  HostName = hostname,
                  ProjectName = project
               };

               _repositoryKeys[path] = projectKey;
            }
         }

         return _repositoryKeys[path];
      }

      public static bool IsSingleCommitFetchSupported(string path)
      {
         // TODO
         // Check if it is possible to run commands like `git fetch origin <sha>:refs/keep-around/sha`
         return true;
      }

      private static string getRepositoryName(string path)
      {
         try
         {
            IEnumerable<string> stdOut =
               ExternalProcess.Start("git", "config --get remote.origin.url", true, path).StdOut;
            return stdOut.Any() ? stdOut.First() : null;
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               return null;
            }
            else
            {
               throw;
            }
         }
      }

      // from https://stackoverflow.com/a/2514986/9195131
      private static string GitRepositoryRegularExpression = @"(\w+://)?(.+@)*([\w\d\.]+)/*(.*)";
      private static readonly Regex gitRepo_re = new Regex(GitRepositoryRegularExpression,
         RegexOptions.Compiled | RegexOptions.IgnoreCase);

      // optimization
      private static Dictionary<string, ProjectKey?> _repositoryKeys = new Dictionary<string, ProjectKey?>();
   }
}

