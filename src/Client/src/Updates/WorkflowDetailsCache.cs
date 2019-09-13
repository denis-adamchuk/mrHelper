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

namespace mrHelper.Client.Updates
{
   // TODO: Cleanup Closed merge requests

   internal class WorkflowDetailsCache
   {
      internal WorkflowDetailsCache(UserDefinedSettings settings, Workflow.Workflow workflow)
      {
         Workflow = workflow;

         Workflow.PostSwitchHost += (_, __) =>
         {
            createOperator(settings);
         };

         Workflow.PostSwitchProject += async (_, __) =>
         {
            List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
            Debug.Assert(enabledProjects.Any((x) => x.Id == Workflow.State.Project.Id));

            Debug.WriteLine(String.Format(
               "[WorkflowDetailsCache] Start handling PostSwitchProject. Host: {0}, Project: {1}",
                  Workflow.State.HostName, Workflow.State.Project.Path_With_Namespace));

            await Operator.CancelAsync();

            _updating = true;
            await cacheMergeRequestsAsync(Workflow.State.HostName, Workflow.State.Project);
            await cacheVersionsAsync(Workflow.State.HostName, Details.GetMergeRequests(Workflow.State.Project.Id));
            _updating = false;

            Debug.WriteLine(String.Format("[WorkflowDetailsCache] End handling PostSwitchProject."));
         };

         Workflow.PostSwitchMergeRequest += async (_) =>
         {
            List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
            Debug.Assert(enabledProjects.Any((x) => x.Id == Workflow.State.MergeRequest.Project_Id));

            Debug.WriteLine(String.Format(
               "[WorkflowDetailsCache] Start handling PostSwitchMergeRequest. Host: {0}, Project: {1}, IId: {2}",
                  Workflow.State.HostName, Workflow.State.Project.Path_With_Namespace, Workflow.State.MergeRequest.IId));

            await Operator.CancelAsync();

            _updating = true;
            await cacheVersionsAsync(Workflow.State.HostName, Workflow.State.MergeRequest);
            _updating = false;

            Debug.WriteLine(String.Format("[WorkflowDetailsCache] End handling PostSwitchMergeRequest."));
         };

         createOperator(settings);
      }

      internal async Task UpdateAsync()
      {
         if (_updating)
         {
            return;
         }

         await cacheMergeRequestsAsync(Workflow.State.HostName, Workflow.State.Project);
         await cacheVersionsAsync(Workflow.State.HostName, Details.GetMergeRequests(Workflow.State.Project.Id));
      }

      internal WorkflowDetails Details { get; private set; }

      /// <summary>
      /// Load merge requests from GitLab and cache them
      /// </summary>
      async private Task cacheMergeRequestsAsync(string hostname, Project project)
      {
         Details.SetProjectName(project.Id, project.Path_With_Namespace);

         Debug.WriteLine(String.Format("[WorkflowDetailsCache] Checking merge requests for project {0} (id {1})",
            Details.GetProjectName(project.Id), project.Id));

         List<MergeRequest> previouslyCachedMergeRequests = Details.GetMergeRequests(project.Id);
         Debug.WriteLine(String.Format(
            "[WorkflowDetailsCache] {0} merge requests for this project were cached before",
            previouslyCachedMergeRequests.Count));

         List<MergeRequest> mergeRequests;
         try
         {
            mergeRequests = await Operator.GetMergeRequestsAsync(Details.GetProjectName(project.Id));
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, String.Format("Cannot load merge requests for project Id {0}",
               project.Id));
            return;
         }

         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            Details.AddMergeRequest(project.Id, mergeRequest);
         }

         Trace.TraceInformation(String.Format(
            "[WorkflowDetailsCache] Cached {0} merge requests (labels not applied) for project {1} at {2}",
               mergeRequests.Count, project.Path_With_Namespace, hostname));
      }

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
         Debug.WriteLine(String.Format(
            "[WorkflowDetailsCache] Checking Versions for merge request {0} from project {1}",
               mergeRequest.IId, Details.GetProjectName(mergeRequest.Project_Id)));

         Debug.WriteLine(String.Format(
            "[WorkflowDetailsCache] Previously cached Version timestamp for this merge request is {0}",
               Details.GetLatestChangeTimestamp(mergeRequest.Id)));

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
            ExceptionHandlers.Handle(ex, String.Format("Cannot load a version for mr Id {0}", mergeRequest.Id));
            return;
         }

         Details.SetLatestChangeTimestamp(mergeRequest.Id, latestVersion.Created_At);

         Trace.TraceInformation(String.Format(
            "[WorkflowDetailsCache] Latest version for merge request with Id {0} has timestamp {1}. Cached.",
               mergeRequest.Id, latestVersion.Created_At));
      }

      private void createOperator(UserDefinedSettings settings)
      {
         Operator?.CancelAsync();

         string host = Workflow.State.HostName;
         Operator = new UpdateOperator(host, Tools.Tools.GetAccessToken(host, settings));
      }

      private UpdateOperator Operator { get; set; }
      private Workflow.Workflow Workflow { get; }
      private bool _updating = false;
   }
}

