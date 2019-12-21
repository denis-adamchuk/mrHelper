using System;
using System.ComponentModel;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Updates;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Manages updates
   /// </summary>
   internal class UpdateManager : IDisposable, IUpdateManager
   {
      internal event Action<List<UpdatedMergeRequest>> OnUpdate;

      internal UpdateManager(ISynchronizeInvoke synchronizeInvoke, UserDefinedSettings settings,
         string hostname, List<Project> projects, WorkflowDetailsCache cache)
      {
         _operator = new UpdateOperator(settings);
         _hostname = hostname;
         _projects = projects;
         _cache = cache;

         _timer = new System.Timers.Timer { Interval = settings.AutoUpdatePeriodMs };
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = synchronizeInvoke;
         _timer.Start();
      }

      public void Dispose()
      {
         _timer.Stop();
         _timer.Dispose();
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

      public void RequestOneShotUpdate(MergeRequestKey mrk, int delay)
      {
         System.Timers.Timer oneShotTimer = new System.Timers.Timer { Interval = delay };
         oneShotTimer.AutoReset = false;
         oneShotTimer.SynchronizingObject = _timer.SynchronizingObject;
         oneShotTimer.Elapsed +=
            async (s, e) =>
         {
            if (String.IsNullOrEmpty(_hostname) || _projects == null)
            {
               Debug.Assert(false);
               Trace.TraceWarning("[UpdateManager] OneShot Update is cancelled");
               return;
            }

            IWorkflowDetails oldDetails = _cache.Details.Clone();

            await loadDataAndUpdateCacheAsync(mrk);

            List<UpdatedMergeRequest> updates = _checker.CheckForUpdates(_hostname, _projects,
               oldDetails, _cache.Details);

            int legitUpdates =
               updates.Count(x => x.UpdateKind == UpdateKind.LabelsUpdated) +
               updates.Count(x => x.UpdateKind == UpdateKind.CommitsAndLabelsUpdated);

            Debug.Assert(legitUpdates == 0 || legitUpdates == 1);
            Debug.Assert(updates.Count(x => x.UpdateKind == UpdateKind.New) == 0);
            Debug.Assert(updates.Count(x => x.UpdateKind == UpdateKind.CommitsUpdated) == 0);
            Debug.Assert(updates.Count(x => x.UpdateKind == UpdateKind.Closed) == 0);

            Trace.TraceInformation(
               String.Format(
                  "[UpdateManager] Updated Labels: {0}. MRK: HostName={1}, ProjectName={2}, IId={3}",
                  legitUpdates, mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId));

            OnUpdate?.Invoke(updates);
         };

         oneShotTimer.Start();
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

         IWorkflowDetails oldDetails = _cache.Details.Clone();

         await loadDataAndUpdateCacheAsync(_hostname, _projects);

         List<UpdatedMergeRequest> updates = _checker.CheckForUpdates(_hostname, _projects,
            oldDetails, _cache.Details);

         Trace.TraceInformation(
            String.Format(
               "[UpdateManager] Merge Request Updates: New {0}, Updated commits {1}, Updated labels {2}, Closed {3}",
               updates.Count(x => x.UpdateKind == UpdateKind.New),
               updates.Count(x => x.UpdateKind == UpdateKind.CommitsUpdated || x.UpdateKind == UpdateKind.CommitsAndLabelsUpdated),
               updates.Count(x => x.UpdateKind == UpdateKind.LabelsUpdated || x.UpdateKind == UpdateKind.CommitsAndLabelsUpdated),
               updates.Count(x => x.UpdateKind == UpdateKind.Closed)));

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

      private readonly WorkflowDetailsCache _cache;
      private readonly WorkflowDetailsChecker _checker = new WorkflowDetailsChecker();
      private readonly UpdateOperator _operator;

      private readonly string _hostname;
      private readonly List<Project> _projects;
   }
}

