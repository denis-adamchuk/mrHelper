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
         Debug.WriteLine(String.Format("GitClientUpdater.SetCommitChecker -- {0}",
            (commitChecker?.ToString() ?? "null")));
      }

      public void Dispose()
      {
         Debug.WriteLine(String.Format("GitClientUpdater disposes and unsubscribes from projectwatcher"));
         _projectWatcher.OnProjectUpdate -= onProjectWatcherUpdate;
      }

      async public Task ManualUpdateAsync()
      {
         Debug.WriteLine("GitClientUpdater.ManualUpdateAsync -- begin");

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
            Debug.WriteLine(String.Format("GitClientUpdater subscribes to projectwatcher"));
            _projectWatcher.OnProjectUpdate += onProjectWatcherUpdate;
            _subscribed = true;
         }
      }

      async private void onProjectWatcherUpdate(object sender, List<ProjectUpdate> updates)
      {
         Debug.WriteLine("GitClientUpdater.onProjectWatcherUpdate -- begin");
         Debug.Assert(_subscribed);

         if (!_lastUpdateTime.HasValue || _updating)
         {
            Debug.WriteLine(
               String.Format("GitClientUpdater.onProjectWatcherUpdate -- early return. timestamp={0}, updating={1}",
                  _lastUpdateTime.ToString(), _updating.ToString()));
            return;
         }

         bool needUpdateGitClient = false;
         foreach (ProjectUpdate update in updates)
         {
            if (_isMyProject(update.HostName, update.ProjectName))
            {
               needUpdateGitClient = true;
               Debug.WriteLine(String.Format("GitClientUpdater.onProjectWatcherUpdate -- will update my project {0}",
                  update.ProjectName));
               break;
            }
         }

         if (!needUpdateGitClient)
         {
            Debug.WriteLine("GitClientUpdater.onProjectWatcherUpdate -- early return. needUpdateClient = false");
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
            Debug.WriteLine(String.Format("GitClientUpdater.doUpdate -- early return"));
            Debug.Assert(false);
            return;
         }

         if (autoupdate || await _commitChecker.AreNewCommitsAsync(_lastUpdateTime.Value))
         {
            Debug.WriteLine(String.Format("GitClientUpdater.doUpdate -- obligatoryUpdate={0}, timestamp={1}",
               autoupdate, _lastUpdateTime.ToString()));

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
         Debug.WriteLine(String.Format("GitClientUpdater.doUpdate -- timestamp updated to {0}", _lastUpdateTime));
      }

      private OnUpdate _onUpdate { get; }
      private IsMyProject _isMyProject { get; }
      private CommitChecker _commitChecker { get; set; }
      private IProjectWatcher _projectWatcher { get; }
      private DateTime? _lastUpdateTime { get; set; } = DateTime.MinValue;

      private bool _updating = false;
      private bool _subscribed = false;
   }
}

