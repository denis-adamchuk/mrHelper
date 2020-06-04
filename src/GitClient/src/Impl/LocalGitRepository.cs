using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   /// <summary>
   /// Provides access to git repository.
   /// </summary>
   internal class LocalGitRepository : ILocalGitRepository, IDisposable
   {
      // @{ IGitRepository
      IGitRepositoryData IGitRepository.Data => ExpectingClone ? null : _data;

      public ProjectKey ProjectKey { get; }

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
      // @{ IGitRepository

      // @{ ILocalGitRepository
      public ILocalGitRepositoryData Data => ExpectingClone ? null : _data;

      public string Path { get; }

      public ILocalGitRepositoryUpdater Updater => _updater;

      public bool ExpectingClone { get; private set; } = true;
      // @} ILocalGitRepository

      /// <summary>
      /// Construct LocalGitRepository with a path that either does not exist or it is empty
      /// or points to a valid git repository
      /// Throws ArgumentException if requirements on `path` argument are not met
      /// </summary>
      internal LocalGitRepository(string parentFolder, ProjectKey projectKey,
         ISynchronizeInvoke synchronizeInvoke, bool useShallowClone, Action<ILocalGitRepository> onClonedRepo)
      {
         Path = LocalGitRepositoryPathFinder.FindPath(parentFolder, projectKey);

         if (!GitTools.IsSingleCommitFetchSupported(Path)) //-V3022
         {
            throw new ArgumentException("Cannot work with such repositories");
         }

         LocalGitRepositoryUpdater.EUpdateMode mode = useShallowClone
            ? LocalGitRepositoryUpdater.EUpdateMode.ShallowClone
            : LocalGitRepositoryUpdater.EUpdateMode.FullCloneWithSingleCommitFetches;

         // PathFinder must guarantee the following
         Debug.Assert(isEmptyFolder(Path)
            || (GitTools.GetRepositoryProjectKey(Path).HasValue
               && GitTools.GetRepositoryProjectKey(Path).Value.Equals(projectKey)));

         _operationManager = new GitOperationManager(synchronizeInvoke, Path);
         _updater = new LocalGitRepositoryUpdater(synchronizeInvoke, this, _operationManager, mode, onCloned, onFetched);
         _onClonedRepo = onClonedRepo;
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

      private void onFetched(string sha)
      {
         Debug.Assert(!_cached_existingSha.Contains(sha));
         _cached_existingSha.Add(sha);
      }

      private void onCloned()
      {
         ExpectingClone = false;
         _onClonedRepo?.Invoke(this);
      }

      private static bool isEmptyFolder(string path)
      {
         return !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any();
      }

      private readonly HashSet<string> _cached_existingSha = new HashSet<string>();
      private LocalGitRepositoryData _data;
      private LocalGitRepositoryUpdater _updater;
      private bool _isDisposed;
      private readonly GitOperationManager _operationManager;
      private readonly Action<ILocalGitRepository> _onClonedRepo;
   }
}

