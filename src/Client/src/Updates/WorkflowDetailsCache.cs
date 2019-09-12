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
   internal class WorkflowDetailsCache
   {
      internal WorkflowDetailsCache(UpdateOperator updateOperator, Workflow workflow)
      {
         UpdateOperator = updateOperator;
         Workflow = workflow;
      }

      internal async void UpdateAsync()
      {
         await cacheMergeRequestsAsync(Workflow.State.HostName, Workflow.State.Project);

         foreach (MergeRequest mergeRequest in CachedMergeRequests[Workflow.State.Project.Id])
         {
            await cacheCommitsAsync(Workflow.state.HostName, mergeRequest);
         }
      }

      internal WorkflowDetails Details { get; private set; }

      /// <summary>
      /// Load merge requests from GitLab and cache them
      /// </summary>
      async private Task cacheMergeRequestsAsync(string hostname, Project project)
      {
         _cachedProjectNames[project.Id] = project.Path_With_Namespace;

         Debug.WriteLine(String.Format("[WorkflowDetailsCache] Checking merge requests for project {0} (id {1})",
            _cachedProjectNames[project.Id], project.Id));

         List<MergeRequest> mergeRequests;
         try
         {
            mergeRequests = await UpdateOperator.GetMergeRequestsAsync(hostname, _cachedProjectNames[project.Id]);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, String.Format("Cannot load merge requests for project Id {0}",
               project.Id));
            return;
         }

         if (_cachedMergeRequests.ContainsKey(project.Id))
         {
            List<MergeRequest> previouslyCachedMergeRequests = _cachedMergeRequests[project.Id];

            Debug.WriteLine(String.Format(
               "[WorkflowDetailsCache] {0} merge requests for this project were cached before",
                  previouslyCachedMergeRequests.Count));

            Debug.WriteLine("[WorkflowDetailsCache] Updating cached merge requests for this project");

            _cachedMergeRequests[project.Id] = mergeRequests;
         }
         else
         {
            Debug.WriteLine(String.Format(
               "[WorkflowDetailsCache] Merge requests for this project were not cached before"));
            Debug.WriteLine("[WorkflowDetailsCache] Caching them now");

            _cachedMergeRequests[project.Id] = mergeRequests;
         }

         Trace.TraceInformation(String.Format(
            "[WorkflowDetailsCache] Cached {0} merge requests (unfiltered) for project {1} at {2}",
               mergeRequests.Count, project.Path_With_Namespace, hostname));
      }

      /// <summary>
      /// Load commits from GitLab and cache them
      /// </summary>
      async private Task cacheCommitsAsync(string hostname, MergeRequest mergeRequest)
      {
         Debug.WriteLine(String.Format(
            "[WorkflowDetailsCache] Checking commits for merge request {0} from project {1}",
               mergeRequest.IId, getMergeRequestProjectName(mergeRequest)));

         MergeRequestDescriptor mrd = new MergeRequestDescriptor
            {
               HostName = hostname,
               ProjectName = getMergeRequestProjectName(mergeRequest),
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

         if (_cachedCommits.ContainsKey(mergeRequest.Id))
         {
            Debug.WriteLine(String.Format(
               "[WorkflowDetailsCache] Previously cached commit timestamp for this merge request is {0}",
                  _cachedCommits[mergeRequest.Id]));

            Debug.WriteLine(String.Format("[WorkflowDetailsCache] Updating cached commits for this merge request"));

            _cachedCommits[mergeRequest.Id] = latestCommit.Created_At;
         }
         else
         {
            Debug.WriteLine(String.Format(
               "[WorkflowDetailsCache] Commits for this merge request were not cached before"));
            Debug.WriteLine(String.Format("[WorkflowDetailsCache] Caching them now"));

            _cachedCommits[mergeRequest.Id] = latestCommit.Created_At;
         }

         Trace.TraceInformation(String.Format(
            "[WorkflowDetailsCache] Latest commit for merge request with Id {0} has timestamp {1}. Cached.",
               mergeRequest.Id, latestCommit.Created_At));
      }

      /// <summary>
      /// Find a project name for a passed merge request
      /// </summary>
      private string getMergeRequestProjectName(MergeRequest mergeRequest)
      {
         if (_cachedProjectNames.ContainsKey(mergeRequest.Project_Id))
         {
            return _cachedProjectNames[mergeRequest.Project_Id];
         }

         return String.Empty;
      }

      // maps unique project id to project's Path with Namespace property
      private readonly Dictionary<int, string> _cachedProjectNames = new Dictionary<int, string>();

      private readonly UpdateOperator UpdateOperator { get; }
      private readonly Workflow Workflow { get; }
   }
}

