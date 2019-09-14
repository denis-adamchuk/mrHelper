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
   // TODO: Cleanup Closed merge requests

   internal class WorkflowDetailsCache
   {
      internal WorkflowDetailsCache(UserDefinedSettings settings, Workflow.Workflow workflow)
      {
         Workflow = workflow;
         Operator = new UpdateOperator(settings);

         Workflow.PostSwitchProject += async (_, mergeRequests) =>
         {
            Trace.TraceInformation("[WorkflowDetailsCache] Processing project switch");

            List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
            Debug.Assert(enabledProjects.Any((x) => x.Id == Workflow.State.Project.Id));

            cacheMergeRequests(Workflow.State.HostName, Workflow.State.Project, mergeRequests);
            await cacheVersionsAsync(Workflow.State.HostName, Details.GetMergeRequests(Workflow.State.Project.Id));
         };

         Workflow.PostSwitchMergeRequest += async (_) =>
         {
            Trace.TraceInformation("[WorkflowDetailsCache] Processing merge request switch");

            List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
            Debug.Assert(enabledProjects.Any((x) => x.Id == Workflow.State.MergeRequest.Project_Id));

            await cacheVersionsAsync(Workflow.State.HostName, Workflow.State.MergeRequest);
         };
      }

      internal async Task UpdateAsync()
      {
         Trace.TraceInformation("[WorkflowDetailsCache] Processing external Update request");

         await cacheMergeRequestsAsync(Workflow.State.HostName, Workflow.State.Project);
         await cacheVersionsAsync(Workflow.State.HostName, Details.GetMergeRequests(Workflow.State.Project.Id));

         Trace.TraceInformation("[WorkflowDetailsCache] External Update request processed");
      }

      internal WorkflowDetails Details { get; private set; } = new WorkflowDetails();

      /// <summary>
      /// Load merge requests from GitLab and cache them
      /// </summary>
      async private Task cacheMergeRequestsAsync(string hostname, Project project)
      {
         List<MergeRequest> mergeRequests;
         try
         {
            mergeRequests = await Operator.GetMergeRequestsAsync(hostname, Details.GetProjectName(project.Id));
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
         Details.SetProjectName(project.Id, project.Path_With_Namespace);

         List<MergeRequest> previouslyCachedMergeRequests = Details.GetMergeRequests(project.Id);
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            Details.AddMergeRequest(project.Id, mergeRequest);
         }

         Trace.TraceInformation(String.Format(
            "[WorkflowDetailsCache] Number of cached merge requests for project {0} at {1} is {2} (was {3} before update)",
               project.Path_With_Namespace, hostname, mergeRequests.Count, previouslyCachedMergeRequests.Count));
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
         MergeRequestDescriptor mrd = new MergeRequestDescriptor
            {
               HostName = hostname,
               ProjectName = Details.GetProjectName(mergeRequest.Project_Id),
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

         DateTime previouslyCachedTimestamp = Details.GetLatestChangeTimestamp(mergeRequest.Id);
         Details.SetLatestChangeTimestamp(mergeRequest.Id, latestVersion.Created_At);

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

      private UpdateOperator Operator { get; set; }
      private Workflow.Workflow Workflow { get; }
      private UserDefinedSettings Settings { get; }
   }
}

