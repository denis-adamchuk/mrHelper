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

         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = synchronizeInvoke;
         _timer.Start();
      }

      public void Dispose()
      {
         _timer.Dispose();
      }

      public IProjectWatcher GetProjectWatcher()
      {
         return _projectWatcher;
      }

      public IInstantProjectChecker GetLocalProjectChecker(MergeRequestKey mrk)
      {
         return new LocalProjectChecker(mrk, _cache.Details.Clone());
      }

      public IInstantProjectChecker GetLocalProjectChecker(ProjectKey pk)
      {
         if (_cache.Details.GetMergeRequests(pk).Count == 0)
         {
            return GetLocalProjectChecker(default(MergeRequestKey));
         }

         MergeRequestKey mrk = _cache.Details.GetMergeRequests(pk).
            Select(x => new MergeRequestKey(pk.HostName, pk.ProjectName, x.IId)).
            OrderByDescending(x => _cache.Details.GetLatestChangeTimestamp(x)).First();
         return GetLocalProjectChecker(mrk);
      }

      public IInstantProjectChecker GetRemoteProjectChecker(MergeRequestKey mrk)
      {
         return new RemoteProjectChecker(mrk, _operator);
      }

      internal List<MergeRequest> GetMergeRequests(ProjectKey projectKey)
      {
         return new List<MergeRequest>(_cache.Details.GetMergeRequests(projectKey));
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
         _projectWatcher.ProcessUpdates(updates, _hostname, _cache.Details);

         Trace.TraceInformation(
            String.Format("[UpdateManager] Merge Request Updates: New {0}, Updated commits {1}, Updated labels {2}, Closed {3}",
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
               MergeRequestKey mrk = new MergeRequestKey(hostname, project.Path_With_Namespace, mergeRequest.IId);

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

      private System.Timers.Timer _timer = new System.Timers.Timer
         {
            Interval = 5 * 60000 // five minutes in ms
         };

      private WorkflowDetailsCache _cache;
      private readonly WorkflowDetailsChecker _checker = new WorkflowDetailsChecker();
      private readonly ProjectWatcher _projectWatcher = new ProjectWatcher();
      private readonly UpdateOperator _operator;

      private string _hostname;
      private List<Project> _projects;
   }
}

