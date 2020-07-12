using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.StorageSupport
{
   /// <summary>
   /// Provides access to git repository.
   /// </summary>
   internal class GitRepository : IGitRepository, IDisposable
   {
      // @{ IGitCommitStorage
      IGitCommandService ICommitStorage.Git => ExpectingClone ? null : _commandService;

      public ProjectKey ProjectKey { get; }
      // @{ IGitCommitStorage

      // @{ ILocalGitCommitStorage
      public string Path { get; }

      public ILocalCommitStorageUpdater Updater => _updater;
      // @} ILocalGitCommitStorage

      // @{ IGitRepository
      async public Task<bool> ContainsSHAAsync(string sha)
      {
         if (_cached_existingSha.Contains(sha))
         {
            return true;
         }

         if (!_isDisposed && await GitTools.DoesEntityExistAtPathAsync(_processManager, Path, sha))
         {
            _cached_existingSha.Add(sha);
            return true;
         }
         return false;
      }

      public bool ExpectingClone { get; private set; } = true;

      public IAsyncGitCommandService Git => ExpectingClone ? null : _commandService;
      // @} IGitRepository

      /// <summary>
      /// Construct GitRepository with a path that either does not exist or it is empty
      /// or points to a valid git repository
      /// Throws ArgumentException if requirements on `path` argument are not met
      /// </summary>
      internal GitRepository(string parentFolder, ProjectKey projectKey,
         ISynchronizeInvoke synchronizeInvoke, LocalCommitStorageType type, Action<IGitRepository> onClonedRepo)
      {
         Path = LocalCommitStoragePathFinder.FindPath(parentFolder, projectKey, type);

         if (!GitTools.IsSingleCommitFetchSupported(Path)) //-V3022
         {
            throw new ArgumentException("Cannot work with such repositories");
         }

         bool isShallowCloneAllowed = type == LocalCommitStorageType.ShallowGitRepository;
         UpdateMode mode = isShallowCloneAllowed ? UpdateMode.ShallowClone : UpdateMode.FullCloneWithSingleCommitFetches;

         // PathFinder must guarantee the following
         Debug.Assert(isEmptyFolder(Path)
            || (GitTools.GetRepositoryProjectKey(Path).HasValue
               && GitTools.GetRepositoryProjectKey(Path).Value.Equals(projectKey)));

         _processManager = new GitProcessManager(synchronizeInvoke, Path);
         _updater = new GitRepositoryUpdater(synchronizeInvoke, this, _processManager, mode, onCloned, onFetched);
         _onClonedRepo = onClonedRepo;

         _commandService = new NativeGitCommandService(_processManager, Path);

         ExpectingClone = isEmptyFolder(Path);
         ProjectKey = projectKey;
         Trace.TraceInformation(String.Format(
            "[GitRepository] Created GitRepository at Path {0} for host {1} and project {2}, "
          + "expecting clone = {3}",
            Path, ProjectKey.HostName, ProjectKey.ProjectName, ExpectingClone.ToString()));
      }

      public void Dispose()
      {
         Trace.TraceInformation(String.Format("[GitRepository] Disposing GitRepository at path {0}", Path));

         _commandService.Dispose();
         _commandService = null;

         _updater.Dispose();
         _updater = null;

         _processManager.Dispose();

         _isDisposed = true;
      }

      public override string ToString()
      {
         return String.Format("[GitRepository] {0} at {1}", ProjectKey.ProjectName, ProjectKey.HostName);
      }

      private void onFetched(string sha)
      {
         Debug.Assert(!_cached_existingSha.Contains(sha));
         _cached_existingSha.Add(sha);
      }

      private void onCloned()
      {
         Trace.TraceInformation(String.Format("[GitRepository] ({0}) Repository cloned", ProjectKey.ProjectName));
         ExpectingClone = false;
         _onClonedRepo?.Invoke(this);
      }

      private static bool isEmptyFolder(string path)
      {
         return !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any();
      }

      private readonly HashSet<string> _cached_existingSha = new HashSet<string>();
      private GitCommandService _commandService;
      private GitRepositoryUpdater _updater;
      private bool _isDisposed;
      private readonly GitProcessManager _processManager;
      private readonly Action<IGitRepository> _onClonedRepo;
   }
}

