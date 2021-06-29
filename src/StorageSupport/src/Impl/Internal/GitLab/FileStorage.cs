using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.StorageSupport
{
   /// <summary>
   /// </summary>
   internal class FileStorage : IFileStorage, IDisposable
   {
      // @{ IGitCommitStorage
      IGitCommandService ICommitStorage.Git => _commandService;
      public ProjectKey ProjectKey { get; }
      // @{ IGitCommitStorage

      // @{ ILocalGitCommitStorage
      public string Path { get; }
      public ILocalCommitStorageUpdater Updater => _updater;
      public IAsyncGitCommandService Git => _commandService;
      // @} ILocalGitCommitStorage

      // @{ IFileStorage
      public FileStorageComparisonCache ComparisonCache { get; }
      public FileStorageDiffCache DiffCache { get; }
      public FileStorageRevisionCache FileCache { get; }
      // @} IFileStorage

      /// <summary>
      /// </summary>
      internal FileStorage(string parentFolder, ProjectKey projectKey,
         ISynchronizeInvoke synchronizeInvoke, RepositoryAccessor repositoryAccessor, IFileStorageProperties properties)
      {
         Path = LocalCommitStoragePathFinder.FindPath(parentFolder, projectKey, LocalCommitStorageType.FileStorage);
         ProjectKey = projectKey;
         FileStorageUtils.InitalizeFileStorage(Path, ProjectKey);

         ComparisonCache = new FileStorageComparisonCache(Path, properties.GetComparisonCountToKeep());
         FileCache = new FileStorageRevisionCache(Path, properties.GetRevisionCountToKeep());
         DiffCache = new FileStorageDiffCache(Path, this);

         _updater = new FileStorageUpdater(synchronizeInvoke, this, repositoryAccessor, properties);

         _processManager = new GitProcessManager(synchronizeInvoke, Path);
         _commandService = new FileStorageGitCommandService(_processManager, Path, this);

         Trace.TraceInformation(String.Format(
            "[FileStorage] Created FileStorage at Path {0} for host {1}, project {2}, ",
            Path, projectKey.HostName, projectKey.ProjectName));
      }

      public void Dispose()
      {
         Trace.TraceInformation(String.Format("[FileStorage] Disposing FileStorage at path {0}", Path));

         _commandService?.Dispose();
         _commandService = null;

         _updater?.Dispose();
         _updater = null;

         _processManager?.Dispose();
         _processManager = null;
      }

      public override string ToString()
      {
         return String.Format("[FileStorage] {0} at {1}", ProjectKey.ProjectName, ProjectKey.HostName);
      }

      private GitCommandService _commandService;
      private FileStorageUpdater _updater;
      private GitProcessManager _processManager;
   }
}

