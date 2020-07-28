using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class SearchBasedMergeRequestLoader : BaseSessionLoader, IMergeRequestListLoader
   {
      internal SearchBasedMergeRequestLoader(string hostname, DataCacheOperator op,
         IVersionLoader versionLoader, InternalCacheUpdater cacheUpdater, DataCacheConnectionContext sessionContext)
         : base(op)
      {
         _hostname = hostname;
         _cacheUpdater = cacheUpdater;
         _versionLoader = versionLoader;
         _sessionContext = sessionContext;
         Debug.Assert(_sessionContext.CustomData is SearchBasedContext);
      }

      async public Task Load()
      {
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests = await loadMergeRequestsAsync();
         _cacheUpdater.UpdateMergeRequests(mergeRequests);
         await _versionLoader.LoadVersionsAndCommits(mergeRequests);
      }

      async private Task<Dictionary<ProjectKey, IEnumerable<MergeRequest>>> loadMergeRequestsAsync()
      {
         SearchBasedContext sbc = (SearchBasedContext)_sessionContext.CustomData;

         IEnumerable<MergeRequest> allMergeRequests = await call(
            () => _operator.SearchMergeRequestsAsync(sbc.SearchCriteria, sbc.MaxSearchResults, sbc.OnlyOpen),
            String.Format("Cancelled loading merge requests with search string \"{0}\"", sbc.SearchCriteria.ToString()),
            String.Format("Cannot load merge requests with search string \"{0}\"", sbc.SearchCriteria.ToString()));

         // leave unique Ids
         allMergeRequests = allMergeRequests
            .GroupBy(x => x.Id) // important to use Id (not IId) because loading is cross-project
            .Select(x => x.First());

         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests =
            new Dictionary<ProjectKey, IEnumerable<MergeRequest>>();

         Exception exception = null;
         async Task resolve(KeyValuePair<int, List<MergeRequest>> keyValuePair)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               ProjectKey? project = await resolveProject(keyValuePair.Key);
               mergeRequests.Add(project.Value, keyValuePair.Value);
            }
            catch (BaseLoaderException ex)
            {
               exception = ex;
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(groupMergeRequestsByProject(allMergeRequests), x => resolve(x),
            () => Constants.MergeRequestLoaderProjectBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }
         return mergeRequests;
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
         ProjectKey? projectKeyOpt = GlobalCache.GetProjectKey(_hostname, projectId);
         if (projectKeyOpt.HasValue)
         {
            return projectKeyOpt.Value;
         }

         ProjectKey projectKey = await call(() => _operator.GetProjectAsync(projectId.ToString()),
            String.Format("Cancelled resolving project with Id \"{0}\"", projectId),
            String.Format("Cannot load project with Id \"{0}\"", projectId));
         GlobalCache.AddProjectKey(_hostname, projectId, projectKey);
         return projectKey;
      }

      private readonly string _hostname;
      private readonly IVersionLoader _versionLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DataCacheConnectionContext _sessionContext;
   }
}

