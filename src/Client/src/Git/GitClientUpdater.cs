using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Git
{
   /// <summary>
   /// Updates attached GitClient object
   /// </summary>
   public class GitClientUpdater : IDisposable
   {
      /// <summary>
      /// Bind to the specific GitClient object
      /// </summary>
      internal GitClientUpdater(IProjectWatcher projectWatcher, Func<Action<string>, DateTime, Task> onUpdate,
         Func<ProjectKey, bool> isMyProject, ISynchronizeInvoke synchronizeInvoke)
      {
         _onUpdate = onUpdate;
         _isMyProject = isMyProject;
         _projectWatcher = projectWatcher;
         _synchronizeInvoke = synchronizeInvoke;
      }

      public void Dispose()
      {
         if (_projectWatcher != null)
         {
            Trace.TraceInformation(String.Format("[GitClientUpdater] Dispose and unsubscribe from Project Watcher"));
            _projectWatcher.OnProjectUpdate -= onProjectWatcherUpdate;
         }
      }

      async public Task ManualUpdateAsync(IInstantProjectChecker instantChecker, Action<string> onProgressChange)
      {
         Trace.TraceInformation(
            String.Format("[GitClientUpdater] Processing manual update. Stored LatestChange: {0}. ProjectChecker: {1}",
               _latestChange.ToLocalTime().ToString(), (instantChecker?.ToString() ?? "null")));

         if (instantChecker == null)
         {
            Trace.TraceError(String.Format("[GitClientUpdater] Unexpected case, manual update w/o instant checker"));
            Debug.Assert(false);
            return;
         }

         _updating = true;
         try
         {
            DateTime newLatestChange = await instantChecker.GetLatestChangeTimestampAsync();
            Trace.TraceInformation(String.Format("[GitClientUpdater] Repository Latest Change: {0}",
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
            Trace.TraceInformation(String.Format("[GitClientUpdater] Subscribe to Project Watcher"));
            _projectWatcher.OnProjectUpdate += onProjectWatcherUpdate;
            _subscribed = true;
         }
      }

      private void onProjectWatcherUpdate(List<ProjectUpdate> updates)
      {
         if (_synchronizeInvoke == null)
         {
            Debug.Assert(false);
            return;
         }

         _synchronizeInvoke.BeginInvoke(new Action<List<ProjectUpdate>>(
            async (updatesInternal) => await onProjectWatcherUpdateAsync(updatesInternal) ), new object[] { updates });
      }

      async private Task onProjectWatcherUpdateAsync(List<ProjectUpdate> updates)
      {
         Debug.Assert(_subscribed);

         if (_updating)
         {
            Trace.TraceInformation(String.Format("[GitClientUpdater] Update cancelled due to a pending update"));
            return;
         }

         bool needUpdateGitClient = false;
         DateTime updateTimestamp = DateTime.MinValue;
         foreach (ProjectUpdate update in updates)
         {
            if (_isMyProject(update.ProjectKey))
            {
               needUpdateGitClient = true;
               updateTimestamp = update.Timestamp;
               Trace.TraceInformation(String.Format(
                  "[GitClientUpdater] Auto-updating git repository {0}, update timestamp {1}",
                     update.ProjectKey.ProjectName, updateTimestamp.ToLocalTime().ToString()));
               break;
            }
         }

         if (!needUpdateGitClient)
         {
            return;
         }

         _updating = true;
         try
         {
            await checkTimestampAndUpdate(updateTimestamp, null);
         }
         catch (GitOperationException ex)
         {
            // just swallow it
            Debug.WriteLine(ex.Message);
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
            await doUpdate(onProgressChange, newLatestChange);

            _latestChange = newLatestChange;

            Trace.TraceInformation(String.Format(
               "[GitClientUpdater] Repository updated. Updating LatestChange timestamp to {0}",
                  _latestChange.ToLocalTime().ToString()));
         }
         else if (newLatestChange == _latestChange)
         {
            Trace.TraceInformation(String.Format("[GitClientUpdater] Repository is not updated"));
         }
         else if (newLatestChange < _latestChange)
         {
            Trace.TraceInformation("[GitClientUpdater] New LatestChange is older than a previous one");
         }
      }

      async private Task doUpdate(Action<string> onProgressChange, DateTime latestChange)
      {
         try
         {
            await _onUpdate(onProgressChange, latestChange);
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot update git repository");
            throw;
         }
      }

      private readonly Func<Action<string>, DateTime, Task> _onUpdate;
      private readonly Func<ProjectKey, bool> _isMyProject;
      private readonly IProjectWatcher _projectWatcher;
      private readonly ISynchronizeInvoke _synchronizeInvoke;

      private DateTime _latestChange = DateTime.MinValue;

      private bool _updating = false;
      private bool _subscribed = false;
   }
}

