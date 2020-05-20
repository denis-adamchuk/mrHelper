using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
         Debug.Assert(_sessionContext.CustomData is SearchBasedContext);
      }

      async public Task<bool> Load()
      {
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests = await loadMergeRequestsAsync();
         if (mergeRequests == null)
         {
            return false; // cancelled
         }

         _cacheUpdater.UpdateMergeRequests(mergeRequests);
         return await _versionLoader.LoadVersionsAndCommits(mergeRequests);
      }

      async private Task<Dictionary<ProjectKey, IEnumerable<MergeRequest>>> loadMergeRequestsAsync()
      {
         SearchBasedContext sbc = (SearchBasedContext)_sessionContext.CustomData;

         IEnumerable<MergeRequest> allMergeRequests = await call(
            () => _operator.SearchMergeRequestsAsync(sbc.SearchCriteria, sbc.MaxSearchResults, sbc.OnlyOpen),
            String.Format("Cancelled loading merge requests with search string \"{0}\"", sbc.SearchCriteria.ToString()),
            String.Format("Cannot load merge requests with search string \"{0}\"", sbc.SearchCriteria.ToString()));
         if (allMergeRequests == null)
         {
            return null;
         }

         // leave unique IIds
         allMergeRequests = allMergeRequests
            .GroupBy(x => x.IId)
            .Select(x => x.First());

         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests =
            new Dictionary<ProjectKey, IEnumerable<MergeRequest>>();

         Exception exception = null;
         bool cancelled = false;
         async Task resolve(KeyValuePair<int, List<MergeRequest>> keyValuePair)
         {
            if (cancelled)
            {
               return;
            }

            try
            {
               ProjectKey? project = await resolveProject(keyValuePair.Key);
               if (project == null)
               {
                  cancelled = true;
                  return;
               }
               mergeRequests.Add(project.Value, keyValuePair.Value);
            }
            catch (SessionException ex)
            {
               exception = ex;
               cancelled = true;
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(groupMergeRequestsByProject(allMergeRequests), x => resolve(x),
            Constants.ProjectsInBatch, Constants.ProjectsInterBatchDelay, () => cancelled);
         if (!cancelled)
         {
            return mergeRequests;
         }

         if (exception != null)
         {
            throw exception;
         }
         return null;
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
         ProjectKey? projectKeyOpt = GlobalCache.GetProjectKey(_operator.Host, projectId);
         if (projectKeyOpt.HasValue)
         {
            return projectKeyOpt.Value;
         }

         ProjectKey projectKey = await call(() => _operator.GetProjectAsync(projectId.ToString()),
            String.Format("Cancelled resolving project with Id \"{0}\"", projectId),
            String.Format("Cannot load project with Id \"{0}\"", projectId));
         if (projectKey.Equals(default(ProjectKey)))
         {
            return new Nullable<ProjectKey>();
         }
         GlobalCache.AddProjectKey(_operator.Host, projectId, projectKey);
         return projectKey;
      }

      private readonly IVersionLoader _versionLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly SessionContext _sessionContext;
   }
}

