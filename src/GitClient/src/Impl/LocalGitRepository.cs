using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.GitClient
{
   /// <summary>
   /// Provides access to git repository.
   /// </summary>
   internal class LocalGitRepository : ILocalGitRepository, IDisposable
   {
      // @{ IGitRepository
      IGitCommitStorageData IGitCommitStorage.Data => ExpectingClone ? null : _data;
      // @{ IGitRepository

      // @{ ILocalGitRepository
      public ILocalGitCommitStorageData Data => ExpectingClone ? null : _data;

      public string Path { get; }

      public ILocalGitCommitStorageUpdater Updater => _updater;
      // @} ILocalGitRepository

      // @{ ILocalGitRepositoryInternal
      async public Task<bool> ContainsSHAAsync(string sha)
      {
         if (_cached_existingSha.Contains(sha))
         {
            return true;
         }

         if (!_isDisposed && await GitTools.DoesEntityExistAtPathAsync(_operationManager, Path, sha))
         {
            _cached_existingSha.Add(sha);
            return true;
         }
         return false;
      }

      public bool ExpectingClone { get; private set; } = true;

      public ProjectKey ProjectKey { get; }
      // @} ILocalGitRepositoryInternal

      /// <summary>
      /// Construct LocalGitRepository with a path that either does not exist or it is empty
      /// or points to a valid git repository
      /// Throws ArgumentException if requirements on `path` argument are not met
      /// </summary>
      internal LocalGitRepository(string parentFolder, ProjectKey projectKey,
         ISynchronizeInvoke synchronizeInvoke, bool useShallowClone)
      {
         Path = LocalGitRepositoryPathFinder.FindPath(parentFolder, projectKey);

         if (!GitTools.IsSingleCommitFetchSupported(Path)) //-V3022
         {
            throw new ArgumentException("Cannot work with such repositories");
         }

         EUpdateMode mode = useShallowClone ? EUpdateMode.ShallowClone : EUpdateMode.FullCloneWithSingleCommitFetches;

         // PathFinder must guarantee the following
         Debug.Assert(isEmptyFolder(Path)
            || (GitTools.GetRepositoryProjectKey(Path).HasValue
               && GitTools.GetRepositoryProjectKey(Path).Value.Equals(projectKey)));

         _operationManager = new GitOperationManager(synchronizeInvoke, Path);
         _updater = new GitInteractiveUpdater(synchronizeInvoke, this, _operationManager, mode, onCloned, onFetched);
         _data = new LocalGitRepositoryData(_operationManager, Path);

         ExpectingClone = isEmptyFolder(Path);
         ProjectKey = projectKey;
         Trace.TraceInformation(String.Format(
            "[LocalGitRepository] Created LocalGitRepository at Path {0} for host {1} and project {2}, "
          + "expecting clone = {3}",
            Path, ProjectKey.HostName, ProjectKey.ProjectName, ExpectingClone.ToString()));
      }

      public void Dispose()
      {
         Trace.TraceInformation(String.Format("[LocalGitRepository] Disposing LocalGitRepository at path {0}", Path));

         _data.Dispose();
         _data = null;

         _updater.Dispose();
         _updater = null;

         _operationManager.Dispose();

         _isDisposed = true;
      }

      public override string ToString()
      {
         return String.Format("[LocalGitRepository] {0} at {1}", ProjectKey.ProjectName, ProjectKey.HostName);
      }

      private void onFetched(string sha)
      {
         Debug.Assert(!_cached_existingSha.Contains(sha));
         _cached_existingSha.Add(sha);
      }

      private void onCloned()
      {
         ExpectingClone = false;
      }

      private static bool isEmptyFolder(string path)
      {
         return !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any();
      }

      private readonly HashSet<string> _cached_existingSha = new HashSet<string>();
      private LocalGitRepositoryData _data;
      private GitInteractiveUpdater _updater;
      private bool _isDisposed;
      private readonly GitOperationManager _operationManager;
   }
}

