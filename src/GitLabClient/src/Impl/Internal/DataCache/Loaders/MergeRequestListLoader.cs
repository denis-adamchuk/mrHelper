using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class MergeRequestListLoader : BaseDataCacheLoader, IMergeRequestListLoader
   {
      internal MergeRequestListLoader(string hostname, DataCacheOperator op,
         IVersionLoader versionLoader, InternalCacheUpdater cacheUpdater, DataCacheConnectionContext dataCacheConnectionContext)
         : base(op)
      {
         _hostname = hostname;
         _cacheUpdater = cacheUpdater;
         _versionLoader = versionLoader;
         _dataCacheConnectionContext = dataCacheConnectionContext;
         Debug.Assert(_dataCacheConnectionContext.CustomData is SearchQueryCollection);
      }

      async public Task Load()
      {
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests = await loadMergeRequestsAsync();
         List<MergeRequestKey> updatedMergeRequests = getUpdatedMergeRequests(mergeRequests);
         _cacheUpdater.UpdateMergeRequests(mergeRequests);
         await _versionLoader.LoadVersionsAndCommits(updatedMergeRequests);
      }

      private List<MergeRequestKey> getUpdatedMergeRequests(
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests)
      {
         List<MergeRequestKey> updatedMergeRequests = new List<MergeRequestKey>();
         foreach (KeyValuePair<ProjectKey, IEnumerable<MergeRequest>> kv in mergeRequests)
         {
            ProjectKey projectKey = kv.Key;
            foreach (MergeRequest mergeRequest in kv.Value)
            {
               MergeRequestKey mrk = new MergeRequestKey(kv.Key, mergeRequest.IId);
               DateTime? oldUpdatedAt = _cacheUpdater.Cache.GetMergeRequest(mrk)?.Updated_At;
               DateTime newUpdatedAt = mergeRequest.Updated_At;
               if (!oldUpdatedAt.HasValue || oldUpdatedAt < newUpdatedAt)
               {
                  updatedMergeRequests.Add(mrk);
               }
            }
         }
         return updatedMergeRequests;
      }

      async private Task<Dictionary<ProjectKey, IEnumerable<MergeRequest>>> loadMergeRequestsAsync()
      {
         SearchQueryCollection queries = (SearchQueryCollection)_dataCacheConnectionContext.CustomData;
         IEnumerable<MergeRequest> mergeRequests = await fetchMergeRequestsAsync(queries);
         return await groupMergeRequests(mergeRequests);
      }

      async private Task<IEnumerable<MergeRequest>> fetchMergeRequestsAsync(SearchQueryCollection queries)
      {
         Exception exception = null;
         List<MergeRequest> mergeRequests = new List<MergeRequest>();
         async Task processSearchQuery(SearchQuery query)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               IEnumerable<MergeRequest> mergeRequestsChunk = await call(
                  () => _operator.SearchMergeRequestsAsync(query),
                  String.Format("Cancelled loading merge requests with search string \"{0}\"", query.ToString()),
                  String.Format("Cannot load merge requests with search string \"{0}\"", query.ToString()));
               mergeRequests.AddRange(mergeRequestsChunk);
            }
            catch (BaseLoaderException ex)
            {
               bool isProjectSpecified = !String.IsNullOrEmpty(query.ProjectName);
               if (isProjectSpecified)
               {
                  ProjectKey projectKey = new ProjectKey(_hostname, query.ProjectName);
                  if (isForbiddenProjectException(ex))
                  {
                     _dataCacheConnectionContext.Callbacks.OnForbiddenProject?.Invoke(projectKey);
                     return;
                  }
                  if (isNotFoundProjectException(ex))
                  {
                     _dataCacheConnectionContext.Callbacks.OnNotFoundProject?.Invoke(projectKey);
                     return;
                  }
               }
               exception = ex;
            }
         }
         await TaskUtils.RunConcurrentFunctionsAsync(queries.Queries, query => processSearchQuery(query),
            () => Constants.MergeRequestLoaderSearchQueryBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }

         // leave unique Ids
         return mergeRequests
            .GroupBy(x => x.Id) // important to use Id (not IId) because loading is cross-project
            .Select(x => x.First())
            .ToList();
      }

      async private Task<Dictionary<ProjectKey, IEnumerable<MergeRequest>>> groupMergeRequests(
         IEnumerable<MergeRequest> mergeRequests)
      {
         Exception exception = null;
         var groupedMergeRequests = new Dictionary<ProjectKey, IEnumerable<MergeRequest>>();
         async Task resolve(KeyValuePair<int, List<MergeRequest>> keyValuePair)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               ProjectKey? project = await resolveProject(keyValuePair.Key);
               groupedMergeRequests.Add(project.Value, keyValuePair.Value);
            }
            catch (BaseLoaderException ex)
            {
               exception = ex;
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(groupMergeRequestsByProject(mergeRequests), x => resolve(x),
            () => Constants.ProjectResolverBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }
         return groupedMergeRequests;
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

         Project project = await call(() => _operator.GetProjectAsync(projectId.ToString()),
            String.Format("Cancelled resolving project with Id \"{0}\"", projectId),
            String.Format("Cannot load project with Id \"{0}\"", projectId));
         ProjectKey projectKey = new ProjectKey(_hostname, project.Path_With_Namespace);
         GlobalCache.AddProjectKey(_hostname, projectId, projectKey);
         return projectKey;
      }

      private static bool isForbiddenProjectException(BaseLoaderException ex)
      {
         System.Net.HttpWebResponse response = getWebResponse(ex);
         return response != null && response.StatusCode == System.Net.HttpStatusCode.Forbidden;
      }

      private static bool isNotFoundProjectException(BaseLoaderException ex)
      {
         System.Net.HttpWebResponse response = getWebResponse(ex);
         return response != null && response.StatusCode == System.Net.HttpStatusCode.NotFound;
      }

      private static System.Net.HttpWebResponse getWebResponse(BaseLoaderException ex)
      {
         if (ex.InnerException?.InnerException is GitLabRequestException rx)
         {
            if (rx.InnerException is System.Net.WebException wx)
            {
               System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
               return response;
            }
         }
         return null;
      }

      private readonly string _hostname;
      private readonly IVersionLoader _versionLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DataCacheConnectionContext _dataCacheConnectionContext;
   }
}

