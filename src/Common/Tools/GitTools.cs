using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

namespace mrHelper.Common.Tools
{
   public static class GitTools
   {
      public enum ConfigScope
      {
         Local,
         Global,
         System
      }

      public static IEnumerable<string> GetConfigKeyValue(ConfigScope scope, string key, string path = "")
      {
         try
         {
            string scopeString = scope.ToString().ToLower();
            string config = String.Format("config --{0} {1}", scopeString, key);
            ExternalProcess.Result result = ExternalProcess.Start("git", config, true, path);
            return result.StdOut;
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               return Array.Empty<string>();
            }
            throw;
         }
      }

      public static void SetConfigKeyValue(ConfigScope scope, string key, string value, string path = "")
      {
         try
         {
            setConfigKeyValueUnsafe(scope, key, value, path);
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               return;
            }
            throw;
         }
      }

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
            setConfigKeyValueUnsafe(ConfigScope.Global, "http.sslVerify", "false", String.Empty);
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

      public static bool IsGit2Installed()
      {
         AppFinder.AppInfo appInfo = AppFinder.GetApplicationInfo(new string[] { "Git version 2" });
         return appInfo != null && !String.IsNullOrEmpty(appInfo.InstallPath);
      }

      public static string GetBinaryFolder()
      {
         AppFinder.AppInfo appInfo = AppFinder.GetApplicationInfo(new string[] { "Git version 2" });
         if (appInfo != null && !String.IsNullOrEmpty(appInfo.InstallPath))
         {
            return System.IO.Path.Combine(appInfo.InstallPath, "bin");
         }
         return String.Empty;
      }

      public static bool SupportsFetchNoTags()
      {
         try
         {
            Version version = GitVersionAccessor.GetVersion();
            return version.Major > 2 || (version.Major == 2 && version.Minor >= 14);
         }
         catch (GitVersionAccessor.UnknownVersionException ex)
         {
            ExceptionHandlers.Handle("Cannot detect git version", ex);
         }
         return false;
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
               operationManager.CreateDescriptor("git", arguments, path, null, null);
            if (descriptor == null)
            {
               return false;
            }

            await operationManager.Wait(descriptor);
            return descriptor.StdErr.Count() == 0;
         }
         catch (Exception)
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

         ProjectKey? projectKey = GetProjectKeyByRemoteUrl(getRemoteUrl(path));
         _repositoryKeys.Add(path, projectKey);
         return projectKey;
      }

      public static ProjectKey? GetProjectKeyByRemoteUrl(string remoteUrl)
      {
         if (String.IsNullOrWhiteSpace(remoteUrl))
         {
            return null;
         }

         Match m = gitRepo_re.Match(remoteUrl);
         if (m.Success && m.Groups.Count == 5 && m.Groups[3].Success && m.Groups[4].Success)
         {
            string hostname = StringUtils.GetHostWithPrefix(m.Groups[3].Value);

            string gitSuffix = ".git";
            int startIndex = m.Groups[4].Value.StartsWith(":") ? 1 : 0;
            int endIndex = m.Groups[4].Value.EndsWith(gitSuffix)
               ? m.Groups[4].Value.Length - gitSuffix.Length : m.Groups[4].Value.Length;

            string project = m.Groups[4].Value.Substring(startIndex, endIndex - startIndex);
            return new ProjectKey(hostname, project);
         }
         return null;
      }

      public static bool IsSingleCommitFetchSupported(string path)
      {
         // TODO Check if it is possible to run commands like `git fetch origin <sha>:refs/keep-around/sha`
         return true;
      }

      public static IEnumerable<string> GetRemotePointsAt(string path, string sha)
      {
         try
         {
            string arguments = String.Format("branch --format=%(refname:short) --remote --points-at \"{0}\"", sha);
            return ExternalProcess.Start("git", arguments, true, path).StdOut;
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               return Array.Empty<string>();
            }
            throw;
         }
      }

      private static void setConfigKeyValueUnsafe(ConfigScope scope, string key, string value, string path)
      {
         string mode = value == null ? "unset" : "";
         string scopeString = scope.ToString().ToLower();
         string config = String.Format("config --{0} --{1} {2} {3}", scopeString, mode, key, value ?? "");
         ExternalProcess.Start("git", config, true, path);
      }

      private static string getRemoteUrl(string path)
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
      private static readonly string GitRepositoryRegularExpression = @"(\w+://)?(.+@)*([\w\d\.]+)/*(.*)";
      private static readonly Regex gitRepo_re = new Regex(GitRepositoryRegularExpression,
         RegexOptions.Compiled | RegexOptions.IgnoreCase);

      // optimization
      private static Dictionary<string, ProjectKey?> _repositoryKeys = new Dictionary<string, ProjectKey?>();
   }
}

