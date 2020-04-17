using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using System.Linq;
using mrHelper.Common.Tools;

namespace mrHelper.GitClient
{
   /// <summary>
   /// Updates attached ILocalGitRepository object
   /// </summary>
   internal class LocalGitRepositoryUpdater : IDisposable, ILocalGitRepositoryUpdater
   {
      /// <summary>
      /// Bind to the specific LocalGitRepository object
      /// </summary>
      internal LocalGitRepositoryUpdater(
         IProjectWatcher projectWatcher,
         ILocalGitRepository localGitRepository,
         IExternalProcessManager operationManager,
         ISynchronizeInvoke synchronizeInvoke,
         bool shallowCloneAllowed,
         bool singleCommitFetchSupported)
      {
         _projectWatcher = projectWatcher;
         _localGitRepository = localGitRepository;
         _operationManager = operationManager;
         _synchronizeInvoke = synchronizeInvoke;
         _shallowCloneAllowed = shallowCloneAllowed;
         _singleCommitFetchSupported = singleCommitFetchSupported;
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

      async public Task Update(IInstantProjectChecker instantChecker, Action<string> onProgressChange)
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

         ProjectSnapshot projectSnapshot = await instantChecker.GetProjectSnapshot();
         await enqueueAndProcess(projectSnapshot);

         _onProgressChange = null;

         // if doUpdate succeeded, it is ok to start periodic updates
         if (!_subscribed)
         {
            Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] Subscribe to Project Watcher"));
            _projectWatcher.OnProjectUpdate += onProjectWatcherUpdate;
            _subscribed = true;
         }
      }

      private void onProjectWatcherUpdate(ProjectUpdate updates)
      {
         if (_synchronizeInvoke == null)
         {
            Debug.Assert(false);
            return;
         }

         _synchronizeInvoke.BeginInvoke(new Action<ProjectUpdate>(
            async (updatesInternal) =>
               await onProjectWatcherUpdateAsync(updatesInternal) ), new object[] { updates });
      }

      async private Task onProjectWatcherUpdateAsync(ProjectUpdate updates)
      {
         Debug.Assert(_subscribed);

         if (!updates.TryGetValue(_localGitRepository.ProjectKey, out ProjectSnapshot snapshot))
         {
            return;
         }

         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryUpdater] Auto-updating git repository {0}",
           _localGitRepository.ProjectKey.ProjectName));

         try
         {
            await enqueueAndProcess(snapshot);
         }
         catch (RepositoryUpdateException ex)
         {
            ExceptionHandlers.Handle("Repository update failed (triggered by PW)", ex);
         }
      }

      async private Task enqueueAndProcess(ProjectSnapshot projectSnapshot)
      {
         _projectSnapshots.Enqueue(projectSnapshot);

         try
         {
            await processQueuedSnapshots();
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

      async private Task processQueuedSnapshots()
      {
         if (_updating)
         {
            return;
         }

         _updating = true;
         try
         {
            while (_projectSnapshots.Any())
            {
               await processSnapshot(_projectSnapshots.Dequeue());
            }
         }
         finally
         {
            _updating = false;
         }
      }

      async private Task processSnapshot(ProjectSnapshot projectSnapshot)
      {
         Debug.Assert(_updateOperationDescriptor == null);

         if (_shallowCloneAllowed)
         {
            if (_localGitRepository.DoesRequireClone())
            {
               await cloneAsync(true);
               Cloned?.Invoke();
            }
         }
         else
         {
            if (_localGitRepository.DoesRequireClone())
            {
               await cloneAsync(false);
               Cloned?.Invoke();
               _latestFullFetchTimeStamp = projectSnapshot.LatestChange;
            }

            if (projectSnapshot.LatestChange > _latestFullFetchTimeStamp)
            {
               await fetchAsync();
               _latestFullFetchTimeStamp = projectSnapshot.LatestChange;
            }
         }

         if (_singleCommitFetchSupported)
         {
            await fetchMissingCommitsAsync(projectSnapshot);
         }
         Updated?.Invoke();

         Debug.Assert(_updateOperationDescriptor == null);
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

      async private Task fetchMissingCommitsAsync(ProjectSnapshot projectSnapshot)
      {
         foreach (string sha in projectSnapshot.Sha.Distinct())
         {
            if (!_localGitRepository.ContainsSHA(sha))
            {
               string arguments = String.Format("fetch {0}", getFetchArguments(sha));
               await doUpdateOperationAsync(arguments, _localGitRepository.Path);
            }
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
               "[LocalGitRepository] START git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments,
               _localGitRepository.Path,
               _localGitRepository.ProjectKey.ProjectName));
            await _operationManager.Wait(descriptor);
         }
         finally
         {
            Trace.TraceInformation(String.Format(
               "[LocalGitRepository] FINISH git with arguments \"{0}\" in \"{1}\" for {2}",
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

      private readonly bool _shallowCloneAllowed;
      private readonly bool _singleCommitFetchSupported;

      private ExternalProcess.AsyncTaskDescriptor _updateOperationDescriptor;
      private readonly Queue<ProjectSnapshot> _projectSnapshots = new Queue<ProjectSnapshot>();

      private bool _updating = false;
      private bool _subscribed = false;
      private Action<string> _onProgressChange;
      private DateTime _latestFullFetchTimeStamp = DateTime.MinValue;
   }
}

