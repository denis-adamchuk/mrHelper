using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   public static class LocalGitRepositoryPathFinder
   {
      public static string FindPath(string parentFolder, ProjectKey projectKey)
      {
         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryPathFinder] Searching for a path for project {0} in \"{1}\"...",
            projectKey.ProjectName, parentFolder));

         string[] splitted = projectKey.ProjectName.Split('/');
         if (splitted.Length < 2)
         {
            throw new ArgumentException("Bad project name \"" + projectKey.ProjectName + "\"");
         }

         return findRepositoryAtDisk(parentFolder, projectKey, out string repositoryPath)
            ? repositoryPath : cookRepositoryName(parentFolder, projectKey);
      }

      private static bool findRepositoryAtDisk(string parentFolder, ProjectKey projectKey,
         out string repositoryPath)
      {
         string[] childFolders = Directory.GetDirectories(parentFolder);
         foreach (string childFolder in childFolders)
         {
            repositoryPath = Path.Combine(parentFolder, childFolder);
            string repositoryName = GitTools.GetRepositoryName(repositoryPath);
            if (String.IsNullOrWhiteSpace(repositoryName))
            {
               Trace.TraceWarning(String.Format(
                  "[LocalGitRepositoryPathFinder] Path \"{0}\" is not a valid git repository",
                  repositoryPath));
               continue;
            }

            Match m = gitRepo_re.Match(repositoryName);
            if (m.Success && m.Groups.Count == 5 && m.Groups[3].Success && m.Groups[4].Success)
            {
               string hostname = StringUtils.GetHostWithPrefix(m.Groups[3].Value);
               if (hostname != projectKey.HostName)
               {
                  continue;
               }

               int startIndex = m.Groups[4].Value.StartsWith(":") ? 1 : 0;

               string gitSuffix = ".git";
               int endIndex = m.Groups[4].Value.EndsWith(gitSuffix)
                  ? m.Groups[4].Value.Length - gitSuffix.Length : m.Groups[4].Value.Length;

               string project = m.Groups[4].Value.Substring(startIndex, endIndex - startIndex);
               if (project != projectKey.ProjectName)
               {
                  continue;
               }

               Trace.TraceInformation(String.Format(
                  "[LocalGitRepositoryPathFinder] Found repository at \"{0}\"", repositoryPath));
               return true;
            }
         }

         repositoryPath = String.Empty;
         return false;
      }

      private static string cookRepositoryName(string parentFolder, ProjectKey projectKey)
      {
         Debug.Assert(projectKey.ProjectName.Count(x => x == '/') == 1);
         string defaultName = projectKey.ProjectName.Replace('/', '_');
         string defaultPath = Path.Combine(parentFolder, defaultName);

         int index = 2;
         string proposedPath = defaultPath;
         while (Directory.Exists(proposedPath))
         {
            proposedPath = String.Format("{0}_{1}", defaultPath, index++);
         }

         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryPathFinder] Proposed repository path is \"{0}\"", proposedPath));
         return proposedPath;
      }

      // from https://stackoverflow.com/a/2514986/9195131
      private static string GitRepositoryRegularExpression = @"(\w+://)?(.+@)*([\w\d\.]+)/*(.*)";
      private static readonly Regex gitRepo_re = new Regex(GitRepositoryRegularExpression,
         RegexOptions.Compiled | RegexOptions.IgnoreCase);
   }
}

