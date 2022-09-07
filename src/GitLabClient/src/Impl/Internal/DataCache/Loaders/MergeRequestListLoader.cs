using System;
using System.Collections.Generic;
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
         InternalCacheUpdater cacheUpdater, DataCacheCallbacks callbacks,
         SearchQueryCollection queryCollection, bool isApprovalStatusSupported)
         : base(op)
      {
         _hostname = hostname;
         _cacheUpdater = cacheUpdater;
         _versionLoader = new VersionLoader(_operator, cacheUpdater);
         _approvalLoader = isApprovalStatusSupported ? new ApprovalLoader(_operator, cacheUpdater) : null;
         _avatarLoader = new AvatarLoader(op, cacheUpdater);
         _callbacks = callbacks;
         _queryCollection = queryCollection;
      }

      async public Task Load()
      {
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests = await loadMergeRequestsAsync();
         IEnumerable<MergeRequestKey> updatedMergeRequests = getUpdatedMergeRequestKeys(mergeRequests);
         _cacheUpdater.UpdateMergeRequests(mergeRequests);
         await _versionLoader.LoadVersionsAndCommits(updatedMergeRequests);

         IEnumerable<MergeRequestKey> mergeRequestKeys = getAllMergeRequestKeys(mergeRequests);
         if (_approvalLoader != null)
         {
            // Note: GitLab (13.6) does not changed Updated_At when approval is revoked
            await _approvalLoader.LoadApprovals(mergeRequestKeys);
         }

         await _avatarLoader.LoadAvatars(mergeRequestKeys);
      }

      private IEnumerable<MergeRequestKey> getUpdatedMergeRequestKeys(
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests)
      {
         List<MergeRequestKey> updatedMergeRequestKeys = new List<MergeRequestKey>();
         foreach (KeyValuePair<ProjectKey, IEnumerable<MergeRequest>> kv in mergeRequests)
         {
            ProjectKey projectKey = kv.Key;
            foreach (MergeRequest mergeRequest in kv.Value)
            {
               MergeRequestKey mrk = new MergeRequestKey(projectKey, mergeRequest.IId);

               MergeRequest cachedMergeRequest = _cacheUpdater.Cache.GetMergeRequest(mrk);
               if (Helpers.GetVersionLoaderKey(cachedMergeRequest) != Helpers.GetVersionLoaderKey(mergeRequest))
               {
                  updatedMergeRequestKeys.Add(mrk);
               }
            }
         }
         return updatedMergeRequestKeys;
      }

      private IEnumerable<MergeRequestKey> getAllMergeRequestKeys(
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests)
      {
         List<MergeRequestKey> allMergeRequestKeys = new List<MergeRequestKey>();
         foreach (KeyValuePair<ProjectKey, IEnumerable<MergeRequest>> kv in mergeRequests)
         {
            foreach (MergeRequest mergeRequest in kv.Value)
            {
               allMergeRequestKeys.Add(new MergeRequestKey(kv.Key, mergeRequest.IId));
            }
         }
         return allMergeRequestKeys;
      }

      async private Task<Dictionary<ProjectKey, IEnumerable<MergeRequest>>> loadMergeRequestsAsync()
      {
         SearchQueryCollection queries = _queryCollection;
         IEnumerable<MergeRequest> mergeRequests = await fetchMergeRequestsAsync(queries);
         IEnumerable<int> renamedProjectIds = checkRenamedProjects(mergeRequests);
         return await groupMergeRequests(mergeRequests, renamedProjectIds);
      }

      private IEnumerable<int> checkRenamedProjects(IEnumerable<MergeRequest> mergeRequests)
      {
         List<int> renamedProjectIds = new List<int>();
         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            ProjectKey? projectKeyOpt = GlobalCache.GetProjectKey(_hostname, mergeRequest.Project_Id);
            if (!projectKeyOpt.HasValue)
            {
               continue;
            }

            MergeRequestKey mrk = new MergeRequestKey(projectKeyOpt.Value, mergeRequest.IId);
            MergeRequest cachedMergeRequest = _cacheUpdater.Cache.GetMergeRequest(mrk);
            if (cachedMergeRequest == null)
            {
               continue;
            }

            if (0 != String.Compare(cachedMergeRequest.Web_Url, mergeRequest.Web_Url))
            {
               renamedProjectIds.Add(mergeRequest.Project_Id);
            }
         }
         return renamedProjectIds;
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
                     _callbacks?.OnForbiddenProject?.Invoke(projectKey);
                     return;
                  }
                  if (isNotFoundProjectException(ex))
                  {
                     _callbacks?.OnNotFoundProject?.Invoke(projectKey);
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
         IEnumerable<MergeRequest> mergeRequests, IEnumerable<int> renamedProjectIds)
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
               ProjectKey project;
               int projectId = keyValuePair.Key;
               ProjectKey? projectKeyOpt = GlobalCache.GetProjectKey(_hostname, projectId);
               if (projectKeyOpt.HasValue && !renamedProjectIds.Contains(projectId))
               {
                  project = projectKeyOpt.Value;
               }
               else
               {
                  project = await resolveProject(projectId);
               }
               groupedMergeRequests.Add(project, keyValuePair.Value);
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

      async private Task<ProjectKey> resolveProject(int projectId)
      {
         Project project = await call(() => _operator.GetProjectAsync(projectId.ToString()),
            String.Format("Cancelled resolving project with Id \"{0}\"", projectId),
            String.Format("Cannot load project with Id \"{0}\"", projectId));
         ProjectKey projectKey = new ProjectKey(_hostname, project.Path_With_Namespace);
         GlobalCache.AddProjectKey(_hostname, projectId, projectKey);
         return projectKey;
      }

      private static bool isForbiddenProjectException(BaseLoaderException ex)
      {
         System.Net.HttpWebResponse response = ex.GetWebResponse();
         return response != null && response.StatusCode == System.Net.HttpStatusCode.Forbidden;
      }

      private static bool isNotFoundProjectException(BaseLoaderException ex)
      {
         System.Net.HttpWebResponse response = ex.GetWebResponse();
         return response != null && response.StatusCode == System.Net.HttpStatusCode.NotFound;
      }

      private readonly string _hostname;
      private readonly IVersionLoader _versionLoader;
      private readonly IApprovalLoader _approvalLoader;
      private readonly IAvatarLoader _avatarLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DataCacheCallbacks _callbacks;
      private readonly SearchQueryCollection _queryCollection;
   }
}

