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

      async public Task ManualUpdateAsync(CommitChecker commitChecker, Action<string> onProgressChange)
      {
         Trace.TraceInformation(
            String.Format("[GitClientUpdater] Processing manual update. LatestChange: {0}. CommitChecker: {1}",
               _latestChange.ToLocalTime().ToString(), (commitChecker?.ToString() ?? "null")));

         if (commitChecker == null)
         {
            Debug.WriteLine(String.Format("[GitClientUpdater] Unexpected case, manual update w/o commit checker"));
            Debug.Assert(false);
            return;
         }
         else
         {
            Debug.WriteLine(String.Format("[GitClientUpdater] Using commit checker {0}", commitChecker.ToString()));
         }

         _updating = true;
         DateTime latestChange = _latestChange;
         try
         {
            Commit commit = await commitChecker.GetLatestCommitAsync();
            Trace.TraceInformation(
               String.Format("[GitClientUpdater] Latest Commit details: SHA {0}, Created_At: {1}",
                  commit.Id, commit.Created_At.ToLocalTime().ToString()));
            if (commit.Created_At > latestChange)
            {
               Trace.TraceInformation(String.Format("[GitClientUpdater] Manual update detected commits newer than {0}",
                  latestChange.ToLocalTime().ToString()));
               latestChange = commit.Created_At;

               await doUpdate(onProgressChange); // this may cancel currently running onTimer update

               _latestChange = latestChange;
               Debug.WriteLine(String.Format("[GitClientUpdater] Timestamp updated to {0}", _latestChange));
            }
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
         Debug.WriteLine("[GitClientUpdater ] Processing an update from Project Watcher");
         Debug.Assert(_subscribed);

         if (_updating)
         {
            Debug.WriteLine(String.Format("[GitClientUpdater] Update cancelled due to a pending update"));
            return;
         }

         bool needUpdateGitClient = false;
         DateTime latestChange = DateTime.MinValue;
         foreach (ProjectUpdate update in updates)
         {
            if (_isMyProject(update.HostName, update.ProjectName))
            {
               needUpdateGitClient = true;
               latestChange = update.LatestChange;
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
            await doUpdate(null);

            _latestChange = latestChange;
            Debug.WriteLine(String.Format("[GitClientUpdater] Timestamp updated to {0}", _latestChange));
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

