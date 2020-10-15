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
   internal class ProjectBasedMergeRequestLoader : BaseDataCacheLoader, IMergeRequestListLoader
   {
      public ProjectBasedMergeRequestLoader(DataCacheOperator op,
         IVersionLoader versionLoader, InternalCacheUpdater cacheUpdater,
         DataCacheConnectionContext dataCacheConnectionContext)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _versionLoader = versionLoader;
         _dataCacheConnectionContext = dataCacheConnectionContext;
      }

      async public Task Load()
      {
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests = await loadMergeRequestsAsync();
         _cacheUpdater.UpdateMergeRequests(mergeRequests);
         await _versionLoader.LoadVersionsAndCommits(mergeRequests);
      }

      async private Task<Dictionary<ProjectKey, IEnumerable<MergeRequest>>> loadMergeRequestsAsync()
      {
         SearchBasedContext sbc = (SearchBasedContext)_dataCacheConnectionContext.CustomData;
         IEnumerable<ProjectKey> projects = sbc.SearchCriteria
            .Criteria
            .Where(criteria => criteria is SearchByProject)
            .Select(criteria => (criteria as SearchByProject).ProjectKey)
            .ToArray();

         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests =
            new Dictionary<ProjectKey, IEnumerable<MergeRequest>>();

         Exception exception = null;
         async Task loadProject(ProjectKey project)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               IEnumerable<MergeRequest> projectMergeRequests = await loadProjectMergeRequestsAsync(project);
               mergeRequests.Add(project, projectMergeRequests);
            }
            catch (BaseLoaderException ex)
            {
               if (isForbiddenProjectException(ex))
               {
                  _dataCacheConnectionContext.Callbacks.OnForbiddenProject?.Invoke(project);
               }
               else if (isNotFoundProjectException(ex))
               {
                  _dataCacheConnectionContext.Callbacks.OnNotFoundProject?.Invoke(project);
               }
               else
               {
                  exception = ex;
               }
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(projects, x => loadProject(x),
            () => Constants.MergeRequestLoaderProjectBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }
         return mergeRequests;
      }

      private Task<IEnumerable<MergeRequest>> loadProjectMergeRequestsAsync(ProjectKey project)
      {
         return call(
            () => _operator.SearchMergeRequestsAsync(
               new SearchCriteria(new object[] { new SearchByProject(project) }, true), null),
            String.Format("Cancelled loading merge requests for project \"{0}\"", project.ProjectName),
            String.Format("Cannot load project \"{0}\"", project.ProjectName));
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

      private readonly IVersionLoader _versionLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DataCacheConnectionContext _dataCacheConnectionContext;
   }
}

