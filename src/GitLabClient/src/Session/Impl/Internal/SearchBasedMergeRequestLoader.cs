using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Types;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.Client.Session
{
   internal class SearchBasedMergeRequestLoader : BaseSessionLoader, IMergeRequestListLoader
   {
      internal SearchBasedMergeRequestLoader(SessionOperator op,
         IVersionLoader versionLoader, InternalCacheUpdater cacheUpdater, SessionContext sessionContext)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _versionLoader = versionLoader;
         _sessionContext = sessionContext;
         Debug.Assert(_sessionContext.CustomData is ProjectBasedContext);
      }

      async public Task<bool> Load()
      {
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests = await loadMergeRequestsAsync();
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
               if (!await _versionLoader.LoadVersionsAsync(mrk) || !await _versionLoader.LoadCommitsAsync(mrk))
               {
                  cancelled = true;
               }
            }
            catch (SessionException ex)
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

            _cacheUpdater.UpdateMergeRequests(kv.Key, kv.Value);

            await TaskUtils.RunConcurrentFunctionsAsync(kv.Value,
               x => loadVersionsLocal(new MergeRequestKey(kv.Key, x.IId)),
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

      async private Task<Dictionary<ProjectKey, IEnumerable<MergeRequest>>> loadMergeRequestsAsync()
      {
         SearchBasedContext sbc = (SearchBasedContext)_sessionContext.CustomData;

         IEnumerable<MergeRequest> mergeRequests = await call(
            () => _operator.SearchMergeRequestsAsync(sbc.SearchCriteria, sbc.MaxSearchResults, sbc.OnlyOpen),
            String.Format("Cancelled loading merge requests with search string \"{0}\"", sbc.SearchCriteria.ToString()),
            String.Format("Cannot load merge requests with search string \"{0}\"", sbc.SearchCriteria.ToString()));
         if (mergeRequests == null)
         {
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
         ProjectKey projectKey = await call(() => _operator.GetProjectAsync(projectId.ToString()),
            String.Format("Cancelled resolving project with Id \"{0}\"", projectId),
            String.Format("Cannot load project with Id \"{0}\"", projectId));
         return projectKey.Equals(default(ProjectKey)) ? new Nullable<ProjectKey>() : projectKey;
      }

      private readonly IVersionLoader _versionLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly SessionContext _sessionContext;
   }
}

