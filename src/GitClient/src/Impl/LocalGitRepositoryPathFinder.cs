using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.GitClient
{
   public static class LocalGitRepositoryPathFinder
   {
      /// <summary>
      /// Returns either a path to empty folder or a path to a folder with already cloned repository
      /// </summary>
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
            ProjectKey? projectAtPath = GitTools.GetRepositoryProjectKey(repositoryPath);
            if (projectAtPath == null)
            {
               Trace.TraceWarning(String.Format(
                  "[LocalGitRepositoryPathFinder] Path \"{0}\" is not a valid git repository",
                  repositoryPath));
               continue;
            }

            if (projectAtPath.Value.Equals(projectKey))
            {
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
   }
}

