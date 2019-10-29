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

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Manages updates
   /// </summary>
   public class UpdateManager : IDisposable
   {
      public event Action<List<UpdatedMergeRequest>> OnUpdate;

      public UpdateManager(Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         UserDefinedSettings settings)
      {
         Settings = settings;
         Workflow = workflow;
         WorkflowDetailsChecker = new WorkflowDetailsChecker();
         ProjectWatcher = new ProjectWatcher();
         Cache = new WorkflowDetailsCache();
         Operator = new UpdateOperator(Settings);

         Timer.Elapsed += onTimer;
         Timer.SynchronizingObject = synchronizeInvoke;
         Timer.Start();

         Workflow.PostLoadHostProjects += (hostname, projects) =>
         {
            Trace.TraceInformation(String.Format(
               "[UpdateManager] Set hostname for updates to {0}, will update {1} projects", hostname, projects.Count));
            _hostname = hostname;
            _projects = projects;
         };

         Workflow.PostLoadProjectMergeRequests += (hostname, project, mergeRequests) =>
            Cache.UpdateMergeRequests(hostname, project.Path_With_Namespace, mergeRequests);

         Workflow.PostLoadLatestVersion += (hostname, projectname, mergeRequest, version) =>
            Cache.UpdateLatestVersion(new MergeRequestKey(hostname, projectname, mergeRequest.IId), version);
      }

      public void Dispose()
      {
         Timer.Dispose();
      }

      public IProjectWatcher GetProjectWatcher()
      {
         return ProjectWatcher;
      }

      public List<MergeRequest> GetMergeRequests(ProjectKey projectKey)
      {
         return Cache.Details.GetMergeRequests(projectKey);
      }

      /// <summary>
      /// Checks local cache to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      public IInstantProjectChecker GetLocalProjectChecker(MergeRequestKey mrk)
      {
         return new LocalProjectChecker(mrk, Cache.Details.Clone());
      }

      /// <summary>
      /// Checks local cache to detect if there are project changes caused by new versions of any merge request
      /// </summary>
      public IInstantProjectChecker GetLocalProjectChecker(ProjectKey pk)
      {
         if (Cache.Details.GetMergeRequests(pk).Count == 0)
         {
            return GetLocalProjectChecker(default(MergeRequestKey));
         }

         MergeRequestKey mrk = Cache.Details.GetMergeRequests(pk).
            Select(x => new MergeRequestKey(pk.HostName, pk.ProjectName, x.IId)).
            OrderByDescending(x => Cache.Details.GetLatestChangeTimestamp(x)).First();
         return GetLocalProjectChecker(mrk);
      }

      /// <summary>
      /// Makes a request to GitLab to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      public IInstantProjectChecker GetRemoteProjectChecker(MergeRequestKey mrk)
      {
         return new RemoteProjectChecker(mrk, Operator);
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      async private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         if (String.IsNullOrEmpty(_hostname) || _projects == null)
         {
            Trace.TraceWarning("[UpdateManager] Auto-update is cancelled");
            return;
         }

         IWorkflowDetails oldDetails = Cache.Details.Clone();

         await loadDataAndUpdateCacheAsync(_hostname, _projects);

         List<UpdatedMergeRequest> updates = WorkflowDetailsChecker.CheckForUpdates(_hostname, _projects,
            oldDetails, Cache.Details);
         ProjectWatcher.ProcessUpdates(updates, _hostname, Cache.Details);

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

            Cache.UpdateMergeRequests(hostname, project.Path_With_Namespace, mergeRequests);
            foreach (KeyValuePair<MergeRequestKey, Version> latestVersion in latestVersions)
            {
               Cache.UpdateLatestVersion(latestVersion.Key, latestVersion.Value);
            }
         }
      }

      async private Task<List<MergeRequest>> loadMergeRequestsAsync(string hostname, string projectname)
      {
         try
         {
            return await Operator.GetMergeRequestsAsync(hostname, projectname);
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
            return await Operator.GetLatestVersionAsync(mrk);
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

      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = 5 * 60000 // five minutes in ms
         };

      // TODO Change private properties to private fields across the solution
      private Workflow.Workflow Workflow { get; }
      private WorkflowDetailsCache Cache { get; }
      private WorkflowDetailsChecker WorkflowDetailsChecker { get; }
      private ProjectWatcher ProjectWatcher { get; }
      private UserDefinedSettings Settings { get; }
      private UpdateOperator Operator { get; }

      private string _hostname;
      private List<Project> _projects;
   }
}

