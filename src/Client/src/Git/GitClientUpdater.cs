using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GitLabSharp.Entities;
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
      internal GitClientUpdater(IProjectWatcher projectWatcher, Func<Action<string>, Task> onUpdate,
         Func<string, string, bool> isMyProject)
      {
         _onUpdate = onUpdate;
         _isMyProject = isMyProject;
         _projectWatcher = projectWatcher;
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

      async private void onProjectWatcherUpdate(List<ProjectUpdate> updates)
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
            if (_isMyProject(update.HostName, update.ProjectName))
            {
               needUpdateGitClient = true;
               updateTimestamp = update.Timestamp;
               Trace.TraceInformation(String.Format(
                  "[GitClientUpdater] Auto-updating git repository {0}, update timestamp {1}",
                     update.ProjectName, updateTimestamp.ToLocalTime().ToString()));
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
            await doUpdate(onProgressChange);

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

      async private Task doUpdate(Action<string> onProgressChange)
      {
         try
         {
            await _onUpdate(onProgressChange);
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot update git repository");
            throw;
         }
      }

      private Func<Action<string>, Task> _onUpdate { get; }
      private Func<string, string, bool> _isMyProject { get; }
      private IProjectWatcher _projectWatcher { get; }
      private DateTime _latestChange { get; set; } = DateTime.MinValue;

      private bool _updating = false;
      private bool _subscribed = false;
   }
}

