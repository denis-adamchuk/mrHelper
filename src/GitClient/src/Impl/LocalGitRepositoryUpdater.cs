using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   /// <summary>
   /// Updates attached LocalGitRepository object
   /// </summary>
   internal class LocalGitRepositoryUpdater : IDisposable, ILocalGitRepositoryUpdater
   {
      /// <summary>
      /// Bind to the specific LocalGitRepository object
      /// </summary>
      internal LocalGitRepositoryUpdater(IProjectWatcher projectWatcher, Func<Action<string>, Task> onUpdate,
         Func<ProjectKey, bool> isMyProject, ISynchronizeInvoke synchronizeInvoke, Func<Task> onCancelUpdate)
      {
         _onUpdate = onUpdate;
         _onCancelUpdate = onCancelUpdate;
         _isMyProject = isMyProject;
         _projectWatcher = projectWatcher;
         _synchronizeInvoke = synchronizeInvoke;
      }

      public void Dispose()
      {
         if (_projectWatcher != null)
         {
            Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] Dispose and unsubscribe from Project Watcher"));
            _projectWatcher.OnProjectUpdate -= onProjectWatcherUpdate;
         }
      }

      public Task CancelUpdate()
      {
         return _onCancelUpdate();
      }

      async public Task ForceUpdate(IInstantProjectChecker instantChecker, Action<string> onProgressChange)
      {
         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryUpdater] Processing manual update. Stored LatestChange: {0}. ProjectChecker: {1}",
               _latestChange.ToLocalTime().ToString(), (instantChecker?.ToString() ?? "null")));

         if (instantChecker == null)
         {
            Trace.TraceError(String.Format("[LocalGitRepositoryUpdater] Unexpected case, manual update w/o instant checker"));
            Debug.Assert(false);
            return;
         }

         _updating = true;
         try
         {
            DateTime newLatestChange = await instantChecker.GetLatestChangeTimestamp();
            Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] Repository Latest Change: {0}",
               newLatestChange.ToLocalTime().ToString()));

            // this may cancel currently running onTimer update
            await checkTimestampAndUpdate(newLatestChange, onProgressChange);
         }
         finally
         {
            _updating = false;
         }

         // if doUpdate succeeded, it is ok to start periodic updates
         if (!_subscribed)
         {
            Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] Subscribe to Project Watcher"));
            _projectWatcher.OnProjectUpdate += onProjectWatcherUpdate;
            _subscribed = true;
         }
      }

      private void onProjectWatcherUpdate(IEnumerable<ProjectUpdate> updates)
      {
         if (_synchronizeInvoke == null)
         {
            Debug.Assert(false);
            return;
         }

         _synchronizeInvoke.BeginInvoke(new Action<IEnumerable<ProjectUpdate>>(
            async (updatesInternal) =>
               await onProjectWatcherUpdateAsync(updatesInternal) ), new object[] { updates });
      }

      async private Task onProjectWatcherUpdateAsync(IEnumerable<ProjectUpdate> updates)
      {
         Debug.Assert(_subscribed);

         if (_updating)
         {
            Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] Update cancelled due to a pending update"));
            return;
         }

         bool needUpdateLocalGitRepository = false;
         DateTime updateTimestamp = DateTime.MinValue;
         foreach (ProjectUpdate update in updates)
         {
            if (_isMyProject(update.ProjectKey))
            {
               needUpdateLocalGitRepository = true;
               updateTimestamp = update.Timestamp;
               Trace.TraceInformation(String.Format(
                  "[LocalGitRepositoryUpdater] Auto-updating git repository {0}, update timestamp {1}",
                     update.ProjectKey.ProjectName, updateTimestamp.ToLocalTime().ToString()));
               break;
            }
         }

         if (!needUpdateLocalGitRepository)
         {
            return;
         }

         _updating = true;
         try
         {
            await checkTimestampAndUpdate(updateTimestamp, null);
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

      async private Task checkTimestampAndUpdate(DateTime newLatestChange, Action<string> onProgressChange)
      {
         if (newLatestChange > _latestChange)
         {
            // don't send out newLatestChange! it can be misleading because it depends on IInstantProjectChecker implementation.
            await doUpdate(onProgressChange);

            _latestChange = newLatestChange;

            Trace.TraceInformation(String.Format(
               "[LocalGitRepositoryUpdater] Repository updated. Updating LatestChange timestamp to {0}",
                  _latestChange.ToLocalTime().ToString()));
         }
         else if (newLatestChange == _latestChange)
         {
            Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] Repository is not updated"));
         }
         else if (newLatestChange < _latestChange)
         {
            Trace.TraceInformation("[LocalGitRepositoryUpdater] New LatestChange is older than a previous one");
         }
      }

      async private Task doUpdate(Action<string> onProgressChange)
      {
         try
         {
            await _onUpdate(onProgressChange);
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

      private readonly Func<Action<string>, Task> _onUpdate;
      private readonly Func<Task> _onCancelUpdate;
      private readonly Func<ProjectKey, bool> _isMyProject;
      private readonly IProjectWatcher _projectWatcher;
      private readonly ISynchronizeInvoke _synchronizeInvoke;

      private DateTime _latestChange = DateTime.MinValue;

      private bool _updating = false;
      private bool _subscribed = false;
   }
}

