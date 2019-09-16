using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Client.Workflow;
using System.ComponentModel;
using System.Diagnostics;
using Version = GitLabSharp.Entities.Version;
using GitLabSharp;

namespace mrHelper.Client.Updates
{
   internal class WorkflowDetailsCache
   {
      internal Action<IWorkflowDetails, IWorkflowDetails, bool> OnUpdate;

      internal WorkflowDetailsCache(UserDefinedSettings settings, Workflow.Workflow workflow)
      {
         Workflow = workflow;
         Operator = new UpdateOperator(settings);

         Workflow.PostSwitchProject += async (_, mergeRequests) =>
         {
            Trace.TraceInformation("[WorkflowDetailsCache] Processing project switch");

            List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
            Debug.Assert(enabledProjects.Any((x) => x.Id == Workflow.State.Project.Id));

            IWorkflowDetails oldDetails = OnUpdate == null ? null : Details.Clone();

            cacheMergeRequests(Workflow.State.HostName, Workflow.State.Project, mergeRequests);
            await cacheVersionsAsync(Workflow.State.HostName,
               InternalDetails.GetMergeRequests(getProjectKey(Workflow.State)));

            Debug.Assert(oldDetails != null);
            OnUpdate?.Invoke(oldDetails, Details, false);
         };

         Workflow.PostSwitchMergeRequest += async (_) =>
         {
            Trace.TraceInformation("[WorkflowDetailsCache] Processing merge request switch");

            List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
            Debug.Assert(enabledProjects.Any((x) => x.Id == Workflow.State.MergeRequest.Project_Id));

            IWorkflowDetails oldDetails = OnUpdate == null ? null : Details.Clone();

            await cacheVersionsAsync(Workflow.State.HostName, Workflow.State.MergeRequest);

            Debug.Assert(oldDetails != null);
            OnUpdate?.Invoke(oldDetails, Details, false);
         };
      }

      internal async Task InitializeAsync()
      {
         Trace.TraceInformation("[WorkflowDetailsCache] Initializing");

         List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
         Debug.Assert(enabledProjects.Any((x) => x.Id == Workflow.State.Project.Id));

         await cacheAllAsync();

         Trace.TraceInformation("[WorkflowDetailsCache] Initialized");
      }

      internal async Task UpdateAsync()
      {
         Trace.TraceInformation("[WorkflowDetailsCache] Processing external Update request");

         IWorkflowDetails oldDetails = OnUpdate == null ? null : Details.Clone();

         await cacheAllAsync();

         Debug.Assert(oldDetails != null);
         OnUpdate?.Invoke(oldDetails, Details, true);

         Trace.TraceInformation("[WorkflowDetailsCache] External Update request processed");
      }

      internal IWorkflowDetails Details { get { return InternalDetails; } }

      async private Task cacheAllAsync()
      {
         await cacheMergeRequestsAsync(Workflow.State.HostName, Workflow.State.Project);
         await cacheVersionsAsync(Workflow.State.HostName,
            InternalDetails.GetMergeRequests(getProjectKey(Workflow.State)));
      }

      /// <summary>
      /// Load merge requests from GitLab and cache them
      /// </summary>
      async private Task cacheMergeRequestsAsync(string hostname, Project project)
      {
         ProjectKey key = getProjectKey(hostname, project.Id);
         InternalDetails.SetProjectName(key, project.Path_With_Namespace);

         List<MergeRequest> mergeRequests;
         try
         {
            mergeRequests = await Operator.GetMergeRequestsAsync(hostname, InternalDetails.GetProjectName(key));
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, String.Format("Cannot load merge requests for project Id {0}", project.Id));
            return; // silent return
         }

         cacheMergeRequests(hostname, project, mergeRequests);
      }

      /// <summary>
      /// Cache passed merge requests
      /// </summary>
      private void cacheMergeRequests(string hostname, Project project, List<MergeRequest> mergeRequests)
      {
         ProjectKey key = getProjectKey(hostname, project.Id);
         InternalDetails.SetProjectName(key, project.Path_With_Namespace);

         List<MergeRequest> previouslyCachedMergeRequests = InternalDetails.GetMergeRequests(key);
         InternalDetails.SetMergeRequests(key, mergeRequests);

         Trace.TraceInformation(String.Format(
            "[WorkflowDetailsCache] Number of cached merge requests for project {0} at {1} is {2} (was {3} before update)",
               project.Path_With_Namespace, hostname, mergeRequests.Count, previouslyCachedMergeRequests.Count));

         cleanupOldRecords(previouslyCachedMergeRequests, mergeRequests);
      }

      /// <summary>
      /// Load Versions from GitLab and cache them
      /// </summary>
      async private Task cacheVersionsAsync(string hostname, List<MergeRequest> mergeRequests)
      {
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            await cacheVersionsAsync(hostname, mergeRequest);
         }
      }

      /// <summary>
      /// Load Versions from GitLab and cache them
      /// </summary>
      async private Task cacheVersionsAsync(string hostname, MergeRequest mergeRequest)
      {
         Debug.Assert(InternalDetails.GetProjectKey(mergeRequest.Id).ProjectId != 0);

         MergeRequestDescriptor mrd = new MergeRequestDescriptor
            {
               HostName = hostname,
               ProjectName = InternalDetails.GetProjectName(getProjectKey(hostname, mergeRequest.Project_Id)),
               IId = mergeRequest.IId
            };

         Version latestVersion;
         try
         {
            latestVersion = await Operator.GetLatestVersionAsync(mrd);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, String.Format("Cannot load latest version for MR Id {0}", mergeRequest.Id));
            return; // silent return
         }

         DateTime previouslyCachedTimestamp = InternalDetails.GetLatestChangeTimestamp(mergeRequest.Id);
         InternalDetails.SetLatestChangeTimestamp(mergeRequest.Id, latestVersion.Created_At);

         if (previouslyCachedTimestamp > latestVersion.Created_At)
         {
            Debug.Assert(false);
            Trace.TraceWarning("[WorkflowDetailsCache] Latest version is older than a previous one");
         }

         Trace.TraceInformation(String.Format(
            "[WorkflowDetailsCache] Latest version of merge request with Id {0} has timestamp {1} (was {2} before update)",
               mergeRequest.Id,
               latestVersion.Created_At.ToLocalTime().ToString(),
               previouslyCachedTimestamp.ToLocalTime().ToString()));
      }

      private void cleanupOldRecords(List<MergeRequest> oldRecords, List<MergeRequest> newRecords)
      {
         foreach (MergeRequest mergeRequest in oldRecords)
         {
            if (!newRecords.Any((x) => x.Id == mergeRequest.Id))
            {
               InternalDetails.CleanupTimestamps(mergeRequest.Id);
            }
         }
      }

      private ProjectKey getProjectKey(string hostname, int projectId)
      {
         return new ProjectKey
         {
            HostName = hostname,
            ProjectId = projectId
         };
      }

      private ProjectKey getProjectKey(WorkflowState state)
      {
         return getProjectKey(state.HostName, state.Project.Id);
      }

      private UpdateOperator Operator { get; set; }
      private Workflow.Workflow Workflow { get; }
      private UserDefinedSettings Settings { get; }
      private WorkflowDetails InternalDetails { get; } = new WorkflowDetails();
   }
}

