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

      public ILocalGitRepositoryState State { get; }

      public ILocalGitRepositoryOperations Operations { get; }
      // @} ILocalGitRepository

      // Host Name and Project Name

      /// <summary>
      /// Construct LocalGitRepository with a path that either does not exist or it is empty
      /// or points to a valid git repository
      /// Throws ArgumentException if requirements on `path` argument are not met
      /// </summary>
      internal LocalGitRepository(ProjectKey projectKey, string path, IProjectWatcher projectWatcher,
         ISynchronizeInvoke synchronizeInvoke)
      {
         Path = path;
         if (!canClone() && !isValidRepository())
         {
            throw new ArgumentException("Path \"" + path + "\" already exists but it is not a valid git repository");
         }

         ProjectKey = projectKey;
         _updater = new LocalGitRepositoryUpdater(projectWatcher,
            async (reportProgress) =>
         {
            bool clone = canClone();
            string arguments = clone
               ? String.Format("clone {0} {1}/{2} {3}",
                  getCloneArguments(), ProjectKey.HostName, ProjectKey.ProjectName, StringUtils.EscapeSpaces(Path))
               : String.Format("fetch {0}", getFetchArguments());

            if (_updateOperationDescriptor == null)
            {
               _updateOperationDescriptor = _operationManager.CreateDescriptor(
                  "git", arguments, clone ? String.Empty : Path, reportProgress);

               try
               {
                  Trace.TraceInformation(String.Format(
                     "[LocalGitRepository] START git with arguments \"{0}\" in \"{1}\" for {2}",
                     arguments, Path, projectKey.ProjectName));
                  await _operationManager.Wait(_updateOperationDescriptor);
               }
               finally
               {
                  Trace.TraceInformation(String.Format(
                     "[LocalGitRepository] FINISH git with arguments \"{0}\" in \"{1}\" for {2}",
                     arguments, Path, projectKey.ProjectName));
                  _updateOperationDescriptor = null;
               }

               if (clone)
               {
                  resetCachedState();
               }

               Updated?.Invoke(this);
            }
            else
            {
               await _operationManager.Join(_updateOperationDescriptor, reportProgress);
            }
         },
         projectKeyToCheck => ProjectKey.Equals(projectKeyToCheck),
         synchronizeInvoke,
            async () =>
         {
            try
            {
               await _operationManager.Cancel(_updateOperationDescriptor);
            }
            finally
            {
               _updateOperationDescriptor = null;
            }
         });

         _operationManager = new GitOperationManager(synchronizeInvoke, path);
         _data = new LocalGitRepositoryData(_operationManager, Path);
         State = new LocalGitRepositoryState(Path, synchronizeInvoke);
         Operations = new LocalGitRepositoryOperations(Path, _operationManager);

         Trace.TraceInformation(String.Format(
            "[LocalGitRepository] Created LocalGitRepository at path {0} for host {1} and project {2} "
          + "can clone at this path = {3}, isValidRepository = {4}",
            path, ProjectKey.HostName, ProjectKey.ProjectName, canClone(), isValidRepository()));
      }

      async internal Task DisposeAsync()
      {
         Trace.TraceInformation(String.Format("[LocalGitRepository] Disposing LocalGitRepository at path {0}", Path));
         _updater.Dispose();
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
         try
         {
            _cached_isValidRepository = Directory.Exists(Path)
                && ExternalProcess.Start("git", "rev-parse --is-inside-work-tree", true, Path).StdErr.Count() == 0;
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               _cached_isValidRepository = false;
            }
            else
            {
               throw;
            }
         }

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

      private string getCloneArguments()
      {
         return " --progress -c credential.helper=manager -c credential.interactive=auto -c credential.modalPrompt=true";
      }

      private string getFetchArguments()
      {
         return String.Format(" --progress {0}", GitTools.SupportsFetchAutoGC() ? "--no-auto-gc" : String.Empty);
      }

      private bool? _cached_isValidRepository;
      private bool? _cached_canClone;
      private HashSet<string> _cached_existingSha = new HashSet<string>();
      private readonly LocalGitRepositoryData _data;
      private readonly LocalGitRepositoryUpdater _updater;
      private readonly IExternalProcessManager _operationManager;
      private ExternalProcess.AsyncTaskDescriptor _updateOperationDescriptor;
   }
}

