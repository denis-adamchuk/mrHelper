using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.StorageSupport
{
   internal static class LocalCommitStoragePathFinder
   {
      /// <summary>
      /// Returns either a path to empty folder or a path to a folder with already cloned repository
      /// </summary>
      internal static string FindPath(string parentFolder, ProjectKey projectKey, LocalCommitStorageType type)
      {
         Trace.TraceInformation(String.Format(
            "[LocalGitCommitStoragePathFinder] Searching for a storage of type {0} for project {1} in \"{2}\"...",
            type.ToString(), projectKey.ProjectName, parentFolder));

         string[] splitted = projectKey.ProjectName.Split('/');
         if (splitted.Length < 2)
         {
            throw new ArgumentException("Bad project name \"" + projectKey.ProjectName + "\"");
         }

         return findRepositoryAtDisk(parentFolder, projectKey, type, out string repositoryPath)
            ? repositoryPath : cookRepositoryName(parentFolder, projectKey, type);
      }

      private static bool findRepositoryAtDisk(string parentFolder, ProjectKey projectKey,
         LocalCommitStorageType type, out string repositoryPath)
      {
         string[] childFolders;
         try
         {
            childFolders = Directory.GetDirectories(parentFolder);
         }
         catch (Exception) // Any exception from Directory.GetDirectories
         {
            repositoryPath = String.Empty;
            return false;
         }

         foreach (string childFolder in childFolders)
         {
            repositoryPath = Path.Combine(parentFolder, childFolder);
            ProjectKey? projectAtPath = getRepositoryProjectKey(repositoryPath, type);
            if (projectAtPath == null)
            {
               Trace.TraceWarning(String.Format(
                  "[LocalGitCommitStoragePathFinder] Path \"{0}\" is not a valid git repository",
                  repositoryPath));
               continue;
            }

            if (projectAtPath.Value.Equals(projectKey))
            {
               Trace.TraceInformation(String.Format(
                  "[LocalGitCommitStoragePathFinder] Found repository at \"{0}\"", repositoryPath));
               return true;
            }
         }

         repositoryPath = String.Empty;
         return false;
      }

      private static string cookRepositoryName(string parentFolder, ProjectKey projectKey,
         LocalCommitStorageType type)
      {
         Debug.Assert(projectKey.ProjectName.Any(x => x == '/'));
         string defaultName = getDefaultPath(projectKey, type, 1);
         string defaultPath = Path.Combine(parentFolder, defaultName);

         int index = 2;
         string proposedPath = defaultPath;
         while (Directory.Exists(proposedPath))
         {
            string proposedName = getDefaultPath(projectKey, type, index);
            proposedPath = Path.Combine(parentFolder, proposedName);
            ++index;
         }

         Trace.TraceInformation(String.Format(
            "[LocalGitCommitStoragePathFinder] Proposed repository path is \"{0}\"", proposedPath));
         return proposedPath;
      }

      private static string getDefaultPath(ProjectKey projectKey, LocalCommitStorageType type, int index)
      {
         string projectName = projectKey.ProjectName.Replace('/', '_');
         return type == LocalCommitStorageType.FileStorage
            ? String.Format("fs{0:00}", index)
            : (index < 2 ? projectName : String.Format("{0}_{1}", projectName, index));
      }

      private static ProjectKey? getRepositoryProjectKey(string path, LocalCommitStorageType type)
      {
         if (type == LocalCommitStorageType.FileStorage)
         {
            return FileStorageUtils.GetFileStorageProjectKey(path);
         }

         ProjectKey? key = GitTools.GetRepositoryProjectKey(path);
         if (key == null)
         {
            return null;
         }

         bool isShallowRepository = File.Exists(Path.Combine(path, ".git", "shallow"));
         bool isAskingForShallowRepository = type == LocalCommitStorageType.ShallowGitRepository;
         return isShallowRepository == isAskingForShallowRepository ? key : null;
      }
   }
}

