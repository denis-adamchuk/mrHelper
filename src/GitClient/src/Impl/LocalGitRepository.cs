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
   internal class LocalGitRepository : ILocalGitRepository
   {
      // @{ IGitRepository
      IGitRepositoryData IGitRepository.Data => ExpectingClone ? null : _data;

      public ProjectKey ProjectKey { get; }

      public bool ContainsSHA(string sha)
      {
         if (_cached_existingSha.Contains(sha))
         {
            return true;
         }
         if (GitTools.DoesEntityExistAtPath(Path, sha))
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

      public event Action<ILocalGitRepository> Updated;
      public event Action<ILocalGitRepository> Disposed;

      public bool ExpectingClone { get; private set; } = true;
      // @} ILocalGitRepository

      /// <summary>
      /// Construct LocalGitRepository with a path that either does not exist or it is empty
      /// or points to a valid git repository
      /// Throws ArgumentException if requirements on `path` argument are not met
      /// </summary>
      internal LocalGitRepository(string parentFolder, ProjectKey projectKey,
         ISynchronizeInvoke synchronizeInvoke, bool useShallowClone)
      {
         Path = LocalGitRepositoryPathFinder.FindPath(parentFolder, projectKey);

         if (useShallowClone && !GitTools.IsSingleCommitFetchSupported(Path))
         {
            throw new ArgumentException("Cannot use shallow clone if single commit fetch is not supported");
         }

         // PathFinder must guarantee the following
         Debug.Assert(isEmptyFolder(Path) || GitTools.GetRepositoryProjectKey(Path).Equals(projectKey));

         _operationManager = new GitOperationManager(synchronizeInvoke, Path);

         LocalGitRepositoryUpdater.EUpdateMode mode = useShallowClone
            ? LocalGitRepositoryUpdater.EUpdateMode.ShallowClone
            : (GitTools.IsSingleCommitFetchSupported(Path)
               ? LocalGitRepositoryUpdater.EUpdateMode.FullCloneWithSingleCommitFetches
               : LocalGitRepositoryUpdater.EUpdateMode.FullCloneWithoutSingleCommitFetches);
         ExpectingClone = isEmptyFolder(Path);
         _updater = new LocalGitRepositoryUpdater(this, _operationManager, mode);
         _updater.Cloned += onCloned;
         _updater.Updated += onUpdated;

         _data = new LocalGitRepositoryData(_operationManager, Path);

         ProjectKey = projectKey;
         Trace.TraceInformation(String.Format(
            "[LocalGitRepository] Created LocalGitRepository at Path {0} for host {1} and project {2}, "
          + "expecting clone = {3}",
            Path, ProjectKey.HostName, ProjectKey.ProjectName, ExpectingClone.ToString()));
      }

      async internal Task DisposeAsync()
      {
         Trace.TraceInformation(String.Format("[LocalGitRepository] Disposing LocalGitRepository at path {0}", Path));
         _data.DisableUpdates();
         await _operationManager.CancelAll();
         Disposed?.Invoke(this);
      }

      private void onCloned()
      {
         ExpectingClone = false;
      }

      private void onUpdated()
      {
         Updated?.Invoke(this);
      }

      private static bool isEmptyFolder(string path)
      {
         return !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any();
      }

      private readonly HashSet<string> _cached_existingSha = new HashSet<string>();
      private readonly LocalGitRepositoryData _data;
      private readonly LocalGitRepositoryUpdater _updater;
      private readonly IExternalProcessManager _operationManager;
   }
}

