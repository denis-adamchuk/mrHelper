using System;
using System.ComponentModel;
using System.Collections.Generic;
using GitLabSharp.Entities;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Types;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Manages updates
   /// </summary>
   internal class UpdateManager : IDisposable, IUpdateManager
   {
      internal event Action<List<UserEvents.MergeRequestEvent>> OnUpdate;

      internal UpdateManager(ISynchronizeInvoke synchronizeInvoke, IHostProperties settings,
         string hostname, List<Project> projects, WorkflowDetailsCache cache, int autoUpdatePeriodMs)
      {
         _operator = new UpdateOperator(settings);
         _hostname = hostname;
         _projects = projects;
         _cache = cache;

         _timer = new System.Timers.Timer { Interval = autoUpdatePeriodMs };
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = synchronizeInvoke;
         _timer.Start();
      }

      public void Dispose()
      {
         _timer.Stop();
         _timer.Dispose();
         _oneShotTimer?.Stop();
         _oneShotTimer?.Dispose();
      }

      public IInstantProjectChecker GetLocalProjectChecker(MergeRequestKey mrk)
      {
         return new LocalProjectChecker(mrk, _cache.Details.Clone());
      }

      public IInstantProjectChecker GetLocalProjectChecker(ProjectKey pk)
      {
         MergeRequestKey mrk = _cache.Details.GetMergeRequests(pk).
            Select(x => new MergeRequestKey
            {
               ProjectKey = pk,
               IId = x.IId
            }).OrderByDescending(x => _cache.Details.GetLatestChangeTimestamp(x)).FirstOrDefault();
         return GetLocalProjectChecker(mrk);
      }

      public IInstantProjectChecker GetRemoteProjectChecker(MergeRequestKey mrk)
      {
         return new RemoteProjectChecker(mrk, _operator);
      }

      public void RequestOneShotUpdate(MergeRequestKey mrk, int firstChanceDelay, int secondChanceDelay)
      {
         cancelOneShotTimer();

         _oneShotTimer = new System.Timers.Timer
         {
            Interval = firstChanceDelay,
            AutoReset = false,
            SynchronizingObject = _timer.SynchronizingObject
         };

         _oneShotTimer.Elapsed +=
            async (s, e) =>
         {
            if (String.IsNullOrEmpty(_hostname) || _projects == null)
            {
               Debug.Assert(false);
               Trace.TraceWarning("[UpdateManager] One-Shot Timer update is cancelled");
               return;
            }

            IWorkflowDetails oldDetails = _cache.Details.Clone();

            await loadDataAndUpdateCacheAsync(mrk);

            List<UserEvents.MergeRequestEvent> updates = _checker.CheckForUpdates(_hostname, _projects,
               oldDetails, _cache.Details);

            int legalUpdates = updates.Count(x => x.Labels);
            Debug.Assert(legalUpdates == 0 || legalUpdates == 1);

            Trace.TraceInformation(
               String.Format(
                  "[UpdateManager] Updated Labels: {0}. MRK: HostName={1}, ProjectName={2}, IId={3}",
                  legalUpdates, mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId));

            OnUpdate?.Invoke(updates);

            if (Convert.ToInt32(_oneShotTimer.Interval) == firstChanceDelay)
            {
              _oneShotTimer.Interval = secondChanceDelay;
              _oneShotTimer.Start();
            }
         };

         _oneShotTimer.Start();
      }

      private void cancelOneShotTimer()
      {
         if (_oneShotTimer?.Enabled ?? false)
         {
            Trace.TraceInformation("[UpdateManager] One-Shot Timer cancelled");
            _oneShotTimer.Stop();
            _oneShotTimer.Dispose();
         }
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      async private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         if (String.IsNullOrEmpty(_hostname) || _projects == null)
         {
            Debug.Assert(false);
            Trace.TraceWarning("[UpdateManager] Auto-update is cancelled");
            return;
         }

         cancelOneShotTimer();

         IWorkflowDetails oldDetails = _cache.Details.Clone();

         await loadDataAndUpdateCacheAsync(_hostname, _projects);

         List<UserEvents.MergeRequestEvent> updates = _checker.CheckForUpdates(_hostname, _projects,
            oldDetails, _cache.Details);

         Trace.TraceInformation(
            String.Format(
               "[UpdateManager] Merge Request Updates: New {0}, Updated commits {1}, Updated labels {2}, Closed {3}",
               updates.Count(x => x.New),
               updates.Count(x => x.Commits),
               updates.Count(x => x.Labels),
               updates.Count(x => x.Closed)));

         OnUpdate?.Invoke(updates);
      }

      async private Task loadDataAndUpdateCacheAsync(string hostname, List<Project> projects)
      {
         foreach (Project project in projects)
         {
            List<MergeRequest> mergeRequests = await loadMergeRequestsAsync(hostname, project.Path_With_Namespace);
            if (mergeRequests == null)
            {
               continue;
            }

            Dictionary<MergeRequestKey, Version> latestVersions = new Dictionary<MergeRequestKey, Version>();
            foreach (MergeRequest mergeRequest in mergeRequests)
            {
               ProjectKey pk = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace };
               MergeRequestKey mrk = new MergeRequestKey
               {
                  ProjectKey = pk,
                  IId = mergeRequest.IId
               };

               Version? latestVersion = await loadLatestVersionAsync(mrk);
               if (latestVersion != null)
               {
                  latestVersions[mrk] = latestVersion.Value;
               }
            }

            _cache.UpdateMergeRequests(hostname, project.Path_With_Namespace, mergeRequests);
            foreach (KeyValuePair<MergeRequestKey, Version> latestVersion in latestVersions)
            {
               _cache.UpdateLatestVersion(latestVersion.Key, latestVersion.Value);
            }
         }
      }

      async private Task loadDataAndUpdateCacheAsync(MergeRequestKey mrk)
      {
         MergeRequest? mergeRequest = await loadMergeRequestAsync(mrk);
         if (mergeRequest.HasValue)
         {
            _cache.UpdateMergeRequest(mrk, mergeRequest.Value);
         }
      }

      async private Task<List<MergeRequest>> loadMergeRequestsAsync(string hostname, string projectname)
      {
         try
         {
            return await _operator.GetMergeRequestsAsync(hostname, projectname);
         }
         catch (OperatorException)
         {
            string message = String.Format(
               "[UpdateManager] Cannot load merge requests. HostName={0}, ProjectName={1}", hostname, projectname);
            Trace.TraceError(message);
         }
         return null;
      }

      async private Task<Version?> loadLatestVersionAsync(MergeRequestKey mrk)
      {
         try
         {
            return await _operator.GetLatestVersionAsync(mrk);
         }
         catch (OperatorException)
         {
            string message = String.Format(
               "[UpdateManager] Cannot load latest version. MRK: HostName={0}, ProjectName={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId);
            Trace.TraceError(message);
         }
         return null;
      }

      async private Task<MergeRequest?> loadMergeRequestAsync(MergeRequestKey mrk)
      {
         try
         {
            return await _operator.GetMergeRequestAsync(mrk);
         }
         catch (OperatorException)
         {
            string message = String.Format(
               "[UpdateManager] Cannot load merge request. MRK: HostName={0}, ProjectName={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId);
            Trace.TraceError(message);
         }
         return null;
      }

      private readonly System.Timers.Timer _timer;
      private System.Timers.Timer _oneShotTimer;

      private readonly WorkflowDetailsCache _cache;
      private readonly WorkflowDetailsChecker _checker = new WorkflowDetailsChecker();
      private readonly UpdateOperator _operator;

      private readonly string _hostname;
      private readonly List<Project> _projects;
   }
}

