using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Constants;

namespace mrHelper.Client.Workflow
{
   public class SearchWorkflowManager : BaseWorkflowManager
   {
      public SearchWorkflowManager(IHostProperties settings)
         : base(settings)
      {
      }

      async public Task<Dictionary<Project, IEnumerable<MergeRequest>>>
         LoadAllMergeRequestsAsync(string hostname, object search, int? maxResults)
      {
         _operator = new WorkflowDataOperator(hostname, _settings.GetAccessToken(hostname));

         Dictionary<Project, IEnumerable<MergeRequest>> mergeRequests =
            await loadMergeRequestsAsync(hostname, search, maxResults);
         if (mergeRequests == null)
         {
            return null; // cancelled
         }

         return mergeRequests;
      }

      async private Task<Dictionary<Project, IEnumerable<MergeRequest>>> loadMergeRequestsAsync(
         string hostname, object search, int? maxResults)
      {
         IEnumerable<MergeRequest> mergeRequests;
         try
         {
            mergeRequests = await _operator.SearchMergeRequestsAsync(search, maxResults, false);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading merge requests with search string \"{0}\"", search);
            string errorMessage = String.Format("Cannot load merge requests with search string \"{0}\"", search);
            handleOperatorException(ex, cancelMessage, errorMessage, null);
            return null;
         }

         Dictionary<Project, IEnumerable<MergeRequest>> result = new Dictionary<Project, IEnumerable<MergeRequest>>();

         bool cancelled = false;
         async Task resolve(KeyValuePair<int, List<MergeRequest>> keyValuePair)
         {
            if (cancelled)
            {
               return;
            }

            Project? project = await resolveProject(hostname, keyValuePair.Key);
            if (project == null)
            {
               cancelled = true;
               return;
            }
            result.Add(project.Value, keyValuePair.Value);
         }

         await TaskUtils.RunConcurrentFunctionsAsync(groupMergeRequestsByProject(mergeRequests), x => resolve(x),
            Constants.ProjectsInBatch, Constants.ProjectsInterBatchDelay, () => cancelled);
         if (cancelled)
         {
            return null;
         }

         return result;
      }

      private Dictionary<int, List<MergeRequest>> groupMergeRequestsByProject(IEnumerable<MergeRequest> mergeRequests)
      {
         Dictionary<int, List<MergeRequest>> grouped = new Dictionary<int, List<MergeRequest>>();
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            int projectId = mergeRequest.Project_Id;
            if (!grouped.ContainsKey(projectId))
            {
               grouped[projectId] = new List<MergeRequest>();
            }
            grouped[projectId].Add(mergeRequest);
         }
         return grouped;
      }

      async private Task<Project?> resolveProject(string hostname, int projectId)
      {
         try
         {
            return await _operator.GetProjectAsync(projectId.ToString());
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled resolving project with Id \"{0}\"", projectId);
            string errorMessage = String.Format("Cannot load project with Id \"{0}\"", projectId);
            handleOperatorException(ex, cancelMessage, errorMessage, null);
         }
         return null;
      }
   }
}

