using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.GitClient
{
   /// <summary>
   /// Updates attached ILocalGitRepository object
   /// </summary>
   internal class LocalGitRepositoryUpdater : IDisposable, ILocalGitRepositoryUpdater
   {
      internal enum EUpdateMode
      {
         ShallowClone, // implies single commit fetching
         FullCloneWithoutSingleCommitFetches,
         FullCloneWithSingleCommitFetches
      }

      /// <summary>
      /// Bind to the specific LocalGitRepository object
      /// </summary>
      internal LocalGitRepositoryUpdater(
         IProjectWatcher projectWatcher,
         ILocalGitRepository localGitRepository,
         IExternalProcessManager operationManager,
         ISynchronizeInvoke synchronizeInvoke,
         EUpdateMode mode)
      {
         _projectWatcher = projectWatcher;
         _localGitRepository = localGitRepository;
         _operationManager = operationManager;
         _synchronizeInvoke = synchronizeInvoke;
         _updateMode = mode;
      }

      internal event Action Cloned;
      internal event Action Updated;

      public void Dispose()
      {
         if (_projectWatcher != null)
         {
            Trace.TraceInformation(String.Format(
               "[LocalGitRepositoryUpdater] Dispose and unsubscribe from Project Watcher"));
            _projectWatcher.OnProjectUpdate -= onProjectWatcherUpdate;
         }
      }

      async public Task CancelUpdate()
      {
         try
         {
            await _operationManager.Cancel(_updateOperationDescriptor);
         }
         finally
         {
            _updateOperationDescriptor = null;
         }
      }

      async public Task Update(IProjectUpdateFactory instantChecker, Action<string> onProgressChange)
      {
         if (instantChecker == null)
         {
            Debug.Assert(false);
            return;
         }

         if (onProgressChange != null)
         {
            _onProgressChange = onProgressChange;
         }

         while (_updating)
         {
            await Task.Delay(50);
         }

         _updating = true;
         try
         {
            IProjectUpdate projectSnapshot = await instantChecker.GetUpdate();
            await enqueueAndProcess(projectSnapshot);
         }
         finally
         {
            _updating = false;
         }

         _onProgressChange = null;

         // if doUpdate succeeded, it is ok to start periodic updates
         if (!_subscribed)
         {
            Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] Subscribe to Project Watcher"));
            _projectWatcher.OnProjectUpdate += onProjectWatcherUpdate;
            _subscribed = true;
         }
      }

      private void onProjectWatcherUpdate(ProjectWatcherUpdateArgs args)
      {
         if (_synchronizeInvoke == null)
         {
            Debug.Assert(false);
            return;
         }

         _synchronizeInvoke.BeginInvoke(new Action<ProjectWatcherUpdateArgs>(
            async (argsInternal) =>
               await onProjectWatcherUpdateAsync(argsInternal) ), new object[] { args });
      }

      async private Task onProjectWatcherUpdateAsync(ProjectWatcherUpdateArgs args)
      {
         Debug.Assert(_subscribed);

         if (!args.ProjectKey.Equals(_localGitRepository.ProjectKey))
         {
            return;
         }

         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryUpdater] Received an update from project watcher for git repository {0}",
           _localGitRepository.ProjectKey.ProjectName));

         if (_updating)
         {
            enqueue(args.ProjectUpdate);
            return;
         }

         _updating = true;
         try
         {
            await enqueueAndProcess(args.ProjectUpdate);
         }
         catch (RepositoryUpdateException ex)
         {
            ExceptionHandlers.Handle("Repository update failed (triggered by PW)", ex);
         }
         finally
         {
            _updating = false;
         }
      }

      async private Task enqueueAndProcess(IProjectUpdate update)
      {
         enqueue(update);
         await processQueue();
      }

      private void enqueue(IProjectUpdate update)
      {
         _queuedUpdates.Enqueue(update);
      }

      async private Task processQueue()
      {
         try
         {
            while (_queuedUpdates.Any())
            {
               Debug.Assert(_updateOperationDescriptor == null);

               IProjectUpdate request = _queuedUpdates.Dequeue();
               if (request is FullProjectUpdate ps)
               {
                  await processFullProjectUpdate(ps);
               }
               else if (request is PartialProjectUpdate pu)
               {
                  await processPartialProjectUpdate(pu);
               }

               Debug.Assert(_updateOperationDescriptor == null);
            }
         }
         catch (GitException ex)
         {
            if (ex is OperationCancelledException)
            {
               throw new UpdateCancelledException();
            }
            else if (ex is GitCallFailedException gfex
                  && gfex.InnerException is ExternalProcessFailureException pfex
                  && String.Join("\n", pfex.Errors).Contains("SSL certificate problem"))
            {
               throw new SecurityException(ex);
            }
            throw new RepositoryUpdateException("Cannot update git repository", ex);
         }
      }

      async private Task processFullProjectUpdate(FullProjectUpdate projectUpdate)
      {
         if (projectUpdate.Sha == null
         || !projectUpdate.Sha.Any()
         ||  projectUpdate.LatestChange == DateTime.MinValue)
         {
            Debug.Assert(false);
            Trace.TraceError("[LocalGitRepositoryUpdater] Unexpected project update content");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         if (_updateMode == EUpdateMode.ShallowClone)
         {
            if (_localGitRepository.DoesRequireClone())
            {
               await cloneAsync(true);
               Trace.TraceInformation("[LocalGitRepositoryUpdater] Repository cloned (shallow clone)");
               Cloned?.Invoke();
            }
         }
         else
         {
            if (_localGitRepository.DoesRequireClone())
            {
               await cloneAsync(true);
               _latestFullFetchTimeStamp = projectUpdate.LatestChange;
               Trace.TraceInformation(String.Format(
                  "[LocalGitRepositoryUpdater] Repository cloned. Updating LatestChange timestamp to {0}",
                  _latestFullFetchTimeStamp.ToLocalTime().ToString()));
               Cloned?.Invoke();
            }

            if (projectUpdate.LatestChange > _latestFullFetchTimeStamp)
            {
               await fetchAsync();
               _latestFullFetchTimeStamp = projectUpdate.LatestChange;
               Trace.TraceInformation(String.Format(
                  "[LocalGitRepositoryUpdater] Repository updated. Updating LatestChange timestamp to {0}",
                  _latestFullFetchTimeStamp.ToLocalTime().ToString()));
            }
            else if (projectUpdate.LatestChange == _latestFullFetchTimeStamp)
            {
               Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] Repository is not updated"));
            }
            else if (projectUpdate.LatestChange < _latestFullFetchTimeStamp)
            {
               Trace.TraceInformation("[LocalGitRepositoryUpdater] New LatestChange is older than a previous one");
            }
         }

         if (_updateMode != EUpdateMode.FullCloneWithoutSingleCommitFetches)
         {
            await fetchCommitsAsync(projectUpdate.Sha);
         }
         Updated?.Invoke();
      }

      async private Task processPartialProjectUpdate(PartialProjectUpdate projectUpdate)
      {
         if (projectUpdate.Sha == null || !projectUpdate.Sha.Any())
         {
            Debug.Assert(false);
            Trace.TraceError("[LocalGitRepositoryUpdater] Unexpected project update content");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         if (_localGitRepository.DoesRequireClone())
         {
            Trace.TraceError(
               "[LocalGitRepositoryUpdater] Partial updates cannot be applied to a not cloned repository");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         if (_updateMode == EUpdateMode.FullCloneWithoutSingleCommitFetches)
         {
            Trace.TraceError(
               "[LocalGitRepositoryUpdater] Partial updates are not supported in this repository");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         await fetchCommitsAsync(projectUpdate.Sha);
         Updated?.Invoke();
      }

      async private Task cloneAsync(bool shallowClone)
      {
         string arguments = String.Format("clone {0} {1}/{2} {3}",
            getCloneArguments(shallowClone),
            _localGitRepository.ProjectKey.HostName,
            _localGitRepository.ProjectKey.ProjectName,
            StringUtils.EscapeSpaces(_localGitRepository.Path));
         await doUpdateOperationAsync(arguments, String.Empty);
      }

      async private Task fetchAsync()
      {
         string arguments = String.Format("fetch {0}", getFetchArguments(null));
         await doUpdateOperationAsync(arguments, _localGitRepository.Path);
      }

      async private Task fetchCommitsAsync(IEnumerable<string> shas)
      {
         int iCommit = 0;
         foreach (string sha in shas.Distinct())
         {
            if (sha != null && !_localGitRepository.ContainsSHA(sha))
            {
               string arguments = String.Format("fetch {0}", getFetchArguments(sha));
               await doUpdateOperationAsync(arguments, _localGitRepository.Path);
               ++iCommit;
            }
         }
         if (iCommit > 0)
         {
            Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] Fetched commits: {0}", iCommit));
         }
      }

      async private Task doUpdateOperationAsync(string arguments, string path)
      {
         ExternalProcess.AsyncTaskDescriptor descriptor = startUpdateOperation(arguments, path);
         await waitUpdateOperationAsync(arguments, descriptor);
      }

      private ExternalProcess.AsyncTaskDescriptor startUpdateOperation(string arguments, string path)
      {
         return _operationManager.CreateDescriptor("git", arguments, path,
            (status) => _onProgressChange?.Invoke(status));
      }

      private async Task waitUpdateOperationAsync(
         string arguments, ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         try
         {
            _updateOperationDescriptor = descriptor;
            Trace.TraceInformation(String.Format(
               "[LocalGitRepositoryUpdater] START git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments,
               _localGitRepository.Path,
               _localGitRepository.ProjectKey.ProjectName));
            await _operationManager.Wait(descriptor);
         }
         finally
         {
            Trace.TraceInformation(String.Format(
               "[LocalGitRepositoryUpdater] FINISH git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments,
               _localGitRepository.Path,
               _localGitRepository.ProjectKey.ProjectName));
            _updateOperationDescriptor = null;
         }
      }

      private static string getCloneArguments(bool shallow)
      {
         return String.Format(" --progress {0} --no-tags"
              + " -c credential.helper=manager -c credential.interactive=auto -c credential.modalPrompt=true",
              shallow ? "--depth=1" : String.Empty);
      }

      private static string getFetchArguments(string sha)
      {
         if (sha == null)
         {
            return String.Format(" --progress --no-tags {0}",
               GitTools.SupportsFetchAutoGC() ? "--no-auto-gc" : String.Empty);
         }

         return String.Format(" --progress --depth=1 --no-tags {0} {1}",
            String.Format("origin {0}:refs/keep-around/{0}", sha),
            GitTools.SupportsFetchAutoGC() ? "--no-auto-gc" : String.Empty);
      }

      private readonly IProjectWatcher _projectWatcher;
      private readonly ILocalGitRepository _localGitRepository;
      private readonly IExternalProcessManager _operationManager;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly EUpdateMode _updateMode;

      private ExternalProcess.AsyncTaskDescriptor _updateOperationDescriptor;
      private readonly Queue<IProjectUpdate> _queuedUpdates = new Queue<IProjectUpdate>();

      private bool _updating = false;
      private bool _subscribed = false;
      private Action<string> _onProgressChange;
      private DateTime _latestFullFetchTimeStamp = DateTime.MinValue;
   }
}

