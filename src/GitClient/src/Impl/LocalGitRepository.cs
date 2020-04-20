using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitClient
{
   /// <summary>
   /// Provides access to git repository.
   /// </summary>
   internal class LocalGitRepository : ILocalGitRepository
   {
      // @{ IGitRepository
      IGitRepositoryData IGitRepository.Data => DoesRequireClone() ? null : _data;

      public ProjectKey ProjectKey { get; }

      public bool ContainsSHA(string sha)
      {
         if (_cached_existingSha.Contains(sha))
         {
            return true;
         }
         if (containsEntity(sha))
         {
            _cached_existingSha.Add(sha);
            return true;
         }
         return false;
      }
      // @{ IGitRepository

      // @{ ILocalGitRepository
      public ILocalGitRepositoryData Data => DoesRequireClone() ? null : _data;

      public string Path { get; }

      public ILocalGitRepositoryUpdater Updater => _updater;

      public event Action<ILocalGitRepository> Updated;
      public event Action<ILocalGitRepository> Disposed;

      public bool DoesRequireClone()
      {
         if (!_cached_canClone.HasValue) { canClone(); }
         if (!_cached_isValidRepository.HasValue) { isValidRepository(); }

         Debug.Assert(_cached_canClone.Value || _cached_isValidRepository.Value);
         return !_cached_isValidRepository.Value;
      }
      // @} ILocalGitRepository

      // Host Name and Project Name

      /// <summary>
      /// Construct LocalGitRepository with a path that either does not exist or it is empty
      /// or points to a valid git repository
      /// Throws ArgumentException if requirements on `path` argument are not met
      /// </summary>
      internal LocalGitRepository(ProjectKey projectKey, string path,
         ISynchronizeInvoke synchronizeInvoke, bool useShallowClone)
      {
         Path = path;
         if (!canClone() && !isValidRepository())
         {
            throw new ArgumentException("Path \"" + path + "\" already exists but it is not a valid git repository");
         }

         if (useShallowClone && !GitTools.IsSingleCommitFetchSupported(Path))
         {
            throw new ArgumentException("Cannot use shallow clone if single commit fetch is not supported");
         }

         LocalGitRepositoryUpdater.EUpdateMode mode = useShallowClone
            ? LocalGitRepositoryUpdater.EUpdateMode.ShallowClone
            : (GitTools.IsSingleCommitFetchSupported(Path)
               ? LocalGitRepositoryUpdater.EUpdateMode.FullCloneWithSingleCommitFetches
               : LocalGitRepositoryUpdater.EUpdateMode.FullCloneWithoutSingleCommitFetches);

         ProjectKey = projectKey;
         _operationManager = new GitOperationManager(synchronizeInvoke, Path);
         _updater = new LocalGitRepositoryUpdater(this, _operationManager, mode);
         _data = new LocalGitRepositoryData(_operationManager, Path);
         _updater.Cloned += resetCachedState;
         _updater.Updated += () => Updated?.Invoke(this);

         Trace.TraceInformation(String.Format(
            "[LocalGitRepository] Created LocalGitRepository at path {0} for host {1} and project {2} "
          + "can clone at this path = {3}, isValidRepository = {4}",
            Path, ProjectKey.HostName, ProjectKey.ProjectName, canClone(), isValidRepository()));
      }

      async internal Task DisposeAsync()
      {
         Trace.TraceInformation(String.Format("[LocalGitRepository] Disposing LocalGitRepository at path {0}", Path));
         _data.DisableUpdates();
         await _operationManager.CancelAll();
         Disposed?.Invoke(this);
      }

      /// <summary>
      /// Check if Clone can be called for this LocalGitRepository
      /// </summary>
      private bool canClone()
      {
         _cached_canClone = !Directory.Exists(Path) || !Directory.EnumerateFileSystemEntries(Path).Any();
         return _cached_canClone.Value;
      }

      private bool isValidRepository()
      {
         _cached_isValidRepository = GitTools.IsValidGitRepository(Path);
         return _cached_isValidRepository.Value;
      }

      private bool containsEntity(string entity)
      {
         try
         {
            return ExternalProcess.Start("git", String.Format("cat-file -t {0}", entity), true, Path).StdErr.Count() == 0;
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               return false;
            }
            throw;
         }
      }

      private void resetCachedState()
      {
         _cached_canClone = null;
         _cached_isValidRepository = null;
      }

      private bool? _cached_isValidRepository;
      private bool? _cached_canClone;
      private HashSet<string> _cached_existingSha = new HashSet<string>();
      private readonly LocalGitRepositoryData _data;
      private readonly LocalGitRepositoryUpdater _updater;
      private readonly IExternalProcessManager _operationManager;
   }
}

