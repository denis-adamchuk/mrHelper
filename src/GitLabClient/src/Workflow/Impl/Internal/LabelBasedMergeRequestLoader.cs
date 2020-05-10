using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.Client.Workflow
{
   internal class LabelBasedMergeRequestLoader : BaseWorkflowLoader, IMergeRequestListLoader
   {
      internal LabelBasedMergeRequestLoader(
         GitLabClientContext clientContext, WorkflowDataOperator op, IVersionLoader versionLoader)
         : base(op)
      {
         _versionLoader = versionLoader;
         _maxResults = clientContext.MaxSearchResults;
      }

      public INotifier<IMergeRequestListLoaderListener> GetNotifier() => _notifier;

      async public Task<bool> Load(IWorkflowContext context)
      {
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests =
            await loadMergeRequestsAsync(context, _maxResults);
         if (mergeRequests == null)
         {
            return false; // cancelled
         }

         Exception exception = null;
         bool cancelled = false;
         async Task loadVersionsLocal(MergeRequestKey mrk)
         {
            if (cancelled)
            {
               return;
            }

            try
            {
               if (!await _versionLoader.LoadVersionsAsync(mrk, false))
               {
                  cancelled = true;
               }
            }
            catch (WorkflowException ex)
            {
               exception = ex;
               cancelled = true;
            }
         }

         foreach (KeyValuePair<ProjectKey, IEnumerable<MergeRequest>> kv in mergeRequests)
         {
            if (cancelled)
            {
               break;
            }

            _notifier.OnPreLoadProjectMergeRequests(kv.Key);
            _notifier.OnPostLoadProjectMergeRequests(kv.Key, kv.Value);

            await TaskUtils.RunConcurrentFunctionsAsync(kv.Value,
               x => loadVersionsLocal(new MergeRequestKey { IId = x.IId, ProjectKey = kv.Key }),
               Constants.MergeRequestsInBatch, Constants.MergeRequestsInterBatchDelay, () => cancelled);
         }
         if (!cancelled)
         {
            return true;
         }

         if (exception != null)
         {
            throw exception;
         }
         return false;
      }

      async private Task<Dictionary<ProjectKey, IEnumerable<MergeRequest>>> loadMergeRequestsAsync(
         object search, int? maxResults)
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

         Dictionary<ProjectKey, IEnumerable<MergeRequest>> result = new Dictionary<ProjectKey, IEnumerable<MergeRequest>>();

         bool cancelled = false;
         async Task resolve(KeyValuePair<int, List<MergeRequest>> keyValuePair)
         {
            if (cancelled)
            {
               return;
            }

            ProjectKey? project = await resolveProject(keyValuePair.Key);
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

      async private Task<ProjectKey?> resolveProject(int projectId)
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

      private readonly int _maxResults;
      private readonly IVersionLoader _versionLoader;
      private readonly MergeRequestListLoaderNotifier _notifier = new MergeRequestListLoaderNotifier();
   }
}

