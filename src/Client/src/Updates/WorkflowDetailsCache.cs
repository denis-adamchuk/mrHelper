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

namespace mrHelper.Client.Updates
{
   // TODO: Cleanup Closed merge requests

   internal class WorkflowDetailsCache
   {
      internal WorkflowDetailsCache(UpdateOperator updateOperator, Workflow workflow)
      {
         UpdateOperator = updateOperator;
      }

      internal async void UpdateAsync(string hostname)
      {
         await cacheMergeRequestsAsync(hostname, Workflow.State.Project);

         foreach (MergeRequest mergeRequest in Details.GetMergeRequests(Workflow.State.Project.Id))
         {
            await cacheCommitsAsync(hostname, mergeRequest);
         }
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
            mergeRequests = await UpdateOperator.GetMergeRequestsAsync(hostname, Details.GetProjectName(project.Id));
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

      /// <summary>
      /// Load commits from GitLab and cache them
      /// </summary>
      async private Task cacheCommitsAsync(string hostname, MergeRequest mergeRequest)
      {
         Debug.WriteLine(String.Format(
            "[WorkflowDetailsCache] Checking commits for merge request {0} from project {1}",
               mergeRequest.IId, Details.GetProjectName(mergeRequest.Project.Id)));

         Debug.WriteLine(String.Format(
            "[WorkflowDetailsCache] Previously cached commit timestamp for this merge request is {0}",
               Details.GetLatestCommitAsync(mergeRequest.Id)));

         MergeRequestDescriptor mrd = new MergeRequestDescriptor
            {
               HostName = hostname,
               ProjectName = Details.GetProjectName(mergeRequest.Project.Id),
               IId = mergeRequest.IId
            };

         Commit latestCommit;
         try
         {
            latestCommit = await UpdateOperator.GetLatestCommitAsync(mrd);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, String.Format("Cannot load commits for mr Id {0}", mergeRequest.Id));
            return;
         }

         Details.SetLatestCommitTimestamp(mergeRequest.Id, latestCommit.Created_At);

         Trace.TraceInformation(String.Format(
            "[WorkflowDetailsCache] Latest commit for merge request with Id {0} has timestamp {1}. Cached.",
               mergeRequest.Id, latestCommit.Created_At));
      }

      private readonly UpdateOperator UpdateOperator { get; }
      private readonly Workflow Workflow { get; }
   }
}

