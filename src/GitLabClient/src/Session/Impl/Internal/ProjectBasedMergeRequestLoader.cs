using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Types;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.Client.Session
{
   internal class ProjectBasedMergeRequestLoader : BaseSessionLoader, IMergeRequestListLoader
   {
      public ProjectBasedMergeRequestLoader(SessionOperator op,
         IVersionLoader versionLoader, InternalCacheUpdater cacheUpdater,
         SessionContext sessionContext)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _versionLoader = versionLoader;
         _sessionContext = sessionContext;
         Debug.Assert(_sessionContext.CustomData is ProjectBasedContext);
      }

      async public Task Load()
      {
         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests = await loadMergeRequestsAsync();
         _cacheUpdater.UpdateMergeRequests(mergeRequests);
         await _versionLoader.LoadVersionsAndCommits(mergeRequests);
      }

      async private Task<Dictionary<ProjectKey, IEnumerable<MergeRequest>>> loadMergeRequestsAsync()
      {
         ProjectBasedContext pbc = (ProjectBasedContext)_sessionContext.CustomData;

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
                  _sessionContext.Callbacks.OnForbiddenProject?.Invoke(project);
               }
               else if (isNotFoundProjectException(ex))
               {
                  _sessionContext.Callbacks.OnNotFoundProject?.Invoke(project);
               }
               else
               {
                  exception = ex;
               }
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(pbc.Projects, x => loadProject(x),
            Constants.MaxProjectsInBatch, Constants.ProjectsInterBatchDelay, () => exception != null);
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
               new SearchCriteria(new object[] { new SearchByProject(project.ProjectName) }), null, true),
            String.Format("Cancelled loading merge requests for project \"{0}\"", project.ProjectName),
            String.Format("Cannot load project \"{0}\"", project.ProjectName));
      }

      private static bool isForbiddenProjectException(BaseLoaderException ex)
      {
         System.Net.HttpWebResponse response = getWebResponse(ex);
         return response != null ? response.StatusCode == System.Net.HttpStatusCode.Forbidden : false;
      }

      private static bool isNotFoundProjectException(BaseLoaderException ex)
      {
         System.Net.HttpWebResponse response = getWebResponse(ex);
         return response != null ? response.StatusCode == System.Net.HttpStatusCode.NotFound : false;
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
      private readonly SessionContext _sessionContext;
   }
}

