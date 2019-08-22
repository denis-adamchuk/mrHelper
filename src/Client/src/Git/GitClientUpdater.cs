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
      public delegate Task OnUpdate(bool reportProgress);
      public delegate bool IsMyProject(string hostname, string projectname);

      /// <summary>
      /// Bind to the specific GitClient object
      /// </summary>
      internal GitClientUpdater(IProjectWatcher projectWatcher, OnUpdate onUpdate, IsMyProject isMyProject)
      {
         _onUpdate = onUpdate;
         _isMyProject = isMyProject;
         _projectWatcher = projectWatcher;
      }

      /// <summary>
      /// Set an object that allows to check for updates
      /// </summary>
      public void SetCommitChecker(CommitChecker commitChecker)
      {
         _commitChecker = commitChecker;
         Debug.WriteLine(String.Format("[GitClientUpdater] Setting commit checker to {0}",
            (commitChecker?.ToString() ?? "null")));
      }

      public void Dispose()
      {
         Trace.TraceInformation(String.Format("[GitClientUpdater] Dispose and unsubscribe from Project Watcher"));
         _projectWatcher.OnProjectUpdate -= onProjectWatcherUpdate;
      }

      async public Task ManualUpdateAsync()
      {
         Trace.TraceInformation("[GitClientUpdater] Processing manual update");

         _updating = true;
         try
         {
            await doUpdate(false); // this may cancel currently running onTimer update
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

      async private void onProjectWatcherUpdate(object sender, List<ProjectUpdate> updates)
      {
         Debug.WriteLine("[GitClientUpdater ] Processing an update from Project Watcher");
         Debug.Assert(_subscribed);

         if (_updating)
         {
            Debug.WriteLine(String.Format("[GitClientUpdater] Update cancelled. timestamp={0}, updating={1}",
               _lastUpdateTime.ToString(), _updating.ToString()));
            return;
         }

         bool needUpdateGitClient = false;
         foreach (ProjectUpdate update in updates)
         {
            if (_isMyProject(update.HostName, update.ProjectName))
            {
               needUpdateGitClient = true;
               Trace.TraceInformation(String.Format("[GitClientUpdater] Auto-updating git repository {0}",
                  update.ProjectName));
               break;
            }
         }

         if (!needUpdateGitClient)
         {
            Debug.WriteLine("[GitClientUpdater] Received update does not affect me");
            return;
         }

         _updating = true;
         try
         {
            await doUpdate(true);
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

      async private Task doUpdate(bool autoupdate)
      {
         if (_commitChecker == null && !autoupdate)
         {
            Debug.WriteLine(String.Format("[GitClientUpdater] Unexpected case, manual update w/o commit checker"));
            Debug.Assert(false);
            return;
         }

         if (autoupdate || await _commitChecker.AreNewCommitsAsync(_lastUpdateTime))
         {
            Trace.TraceInformation(String.Format("[GitClientUpdater] autoupdate={0}, timestamp={1} (Local Time)",
               autoupdate, _lastUpdateTime.ToLocalTime().ToString()));

            try
            {
               await _onUpdate(!autoupdate);
            }
            catch (GitOperationException ex)
            {
               ExceptionHandlers.Handle(ex, "Cannot update git repository");
               throw;
            }
         }

         _lastUpdateTime = DateTime.Now;
         Debug.WriteLine(String.Format("[GitClientUpdater] Timestamp updated to {0}", _lastUpdateTime));
      }

      private OnUpdate _onUpdate { get; }
      private IsMyProject _isMyProject { get; }
      private CommitChecker _commitChecker { get; set; }
      private IProjectWatcher _projectWatcher { get; }
      private DateTime _lastUpdateTime { get; set; } = DateTime.MinValue;

      private bool _updating = false;
      private bool _subscribed = false;
   }
}

