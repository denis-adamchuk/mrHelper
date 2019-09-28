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
   public class UpdateManager
   {
      public event Action<MergeRequestUpdates> OnUpdate;

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

         Workflow.PostSwitchHost += async (state, projects) =>
         {
            Trace.TraceInformation("[UpdateManager] Processing host switch");

            await initializeAsync(state.HostName);
         };

         Workflow.PostSwitchProject += (state, mergeRequests) =>
         {
            Trace.TraceInformation("[UpdateManager] Processing project switch");

            Debug.Assert(Workflow.GetProjectsToUpdate().Any((x) => x.Id == state.Project.Id));

            Cache.UpdateMergeRequests(state.HostName, state.Project, mergeRequests);
         };

         Workflow.PostLoadLatestVersion += (state, version) =>
         {
            Trace.TraceInformation("[UpdateManager] Processing latest version load");

            Debug.Assert(Workflow.GetProjectsToUpdate().Any((x) => x.Id == state.MergeRequest.Project_Id));

            Cache.UpdateLatestVersion(state.MergeRequest.Id, version);
         };
      }

      async private Task initializeAsync(string hostname)
      {
         Trace.TraceInformation("[UpdateManager] Initializing");

         List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
         await loadDataAndUpdateCacheAsync(hostname, enabledProjects);

         Trace.TraceInformation("[UpdateManager] Initialized");
      }

      public IProjectWatcher GetProjectWatcher()
      {
         return ProjectWatcher;
      }

      /// <summary>
      /// Checks local cache to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      public IInstantProjectChecker GetLocalProjectChecker(int mergeRequestId)
      {
         return new LocalProjectChecker(mergeRequestId, Cache.Details.Clone());
      }

      /// <summary>
      /// Makes a request to GitLab to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      public IInstantProjectChecker GetRemoteProjectChecker(MergeRequestDescriptor mrd)
      {
         return new RemoteProjectChecker(mrd, Operator);
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      async private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         IWorkflowDetails oldDetails = Cache.Details.Clone();

         string hostname = Workflow.State.HostName;
         List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
         await loadDataAndUpdateCacheAsync(hostname, enabledProjects);

         MergeRequestUpdates updates = WorkflowDetailsChecker.CheckForUpdates(hostname,
            enabledProjects, oldDetails, Cache.Details);
         ProjectWatcher.ProcessUpdates(updates, hostname, Cache.Details);

         Trace.TraceInformation(
            String.Format("[UpdateManager] Merge Request Updates: New {0}, Updated {1}, Closed {2}",
               updates.NewMergeRequests.Count, updates.UpdatedMergeRequests.Count, updates.ClosedMergeRequests.Count));

         if (updates.UpdatedMergeRequests.Count > 0)
         {
            Trace.TraceInformation(
               String.Format("[UpdateManager] Updated commits: {0}. Updated labels: {1}",
                  updates.UpdatedMergeRequests.Count((x) => x.UpdateKind.HasFlag(UpdateKind.CommitsUpdated)),
                  updates.UpdatedMergeRequests.Count((x) => x.UpdateKind.HasFlag(UpdateKind.LabelsUpdated))));
         }

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

            Dictionary<int, Version> latestVersions = new Dictionary<int, Version>();
            foreach (MergeRequest mergeRequest in mergeRequests)
            {
               MergeRequestDescriptor mrd = new MergeRequestDescriptor
               {
                  HostName = hostname,
                  ProjectName = project.Path_With_Namespace,
                  IId = mergeRequest.IId
               };

               Version? latestVersion = await loadLatestVersionAsync(mrd);
               if (latestVersion != null)
               {
                  latestVersions[mergeRequest.Id] = latestVersion.Value;
               }
            }

            Cache.UpdateMergeRequests(hostname, project, mergeRequests);
            foreach (KeyValuePair<int, Version> latestVersion in latestVersions)
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

      async private Task<Version?> loadLatestVersionAsync(MergeRequestDescriptor mrd)
      {
         try
         {
            return await Operator.GetLatestVersionAsync(mrd);
         }
         catch (OperatorException)
         {
            string message = String.Format(
               "[UpdateManager] Cannot load latest version. MRD: HostName={0}, ProjectName={1}, IId={2}",
               mrd.HostName, mrd.ProjectName, mrd.IId);
            Trace.TraceError(message);
         }
         return null;
      }

      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = 5 * 60000 // five minutes in ms
         };

      private Workflow.Workflow Workflow { get; }
      private WorkflowDetailsCache Cache { get; }
      private WorkflowDetailsChecker WorkflowDetailsChecker { get; }
      private ProjectWatcher ProjectWatcher { get; }
      private UserDefinedSettings Settings { get; }
      private UpdateOperator Operator { get; }
   }
}

