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

      async public Task<bool> Load()
      {
         ProjectBasedContext pbc = (ProjectBasedContext)_sessionContext.CustomData;

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

         Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests =
            new Dictionary<ProjectKey, IEnumerable<MergeRequest>>();
         async Task loadProject(ProjectKey project)
         {
            if (cancelled)
            {
               return;
            }

            try
            {
               IEnumerable<MergeRequest> projectMergeRequests = await loadProjectMergeRequestsAsync(project);
               if (mergeRequests == null)
               {
                  cancelled = true;
               }
               else
               {
                  mergeRequests.Add(project, projectMergeRequests);
               }
            }
            catch (SessionException ex)
            {
               if (isForbiddenProjectException(ex))
               {
                  _sessionContext.Callbacks.OnForbiddenProject?.Invoke(project);
                  return;
               }
               else if (isNotFoundProjectException(ex))
               {
                  _sessionContext.Callbacks.OnNotFoundProject?.Invoke(project);
                  return;
               }
               exception = ex;
               cancelled = true;
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(pbc.Projects, x => loadProject(x),
            Constants.ProjectsInBatch, Constants.ProjectsInterBatchDelay, () => cancelled);

         _cacheUpdater.UpdateMergeRequests(mergeRequests);
         List<MergeRequestKey> allKeys = new List<MergeRequestKey>();
         foreach (KeyValuePair<ProjectKey, IEnumerable<MergeRequest>> kv in mergeRequests)
         {
            foreach (MergeRequest mergeRequest in kv.Value)
            {
               allKeys.Add(new MergeRequestKey(kv.Key, mergeRequest.IId));
            }
         }
         await TaskUtils.RunConcurrentFunctionsAsync(allKeys, x => loadVersionsLocal(x),
            Constants.MergeRequestsInBatch, Constants.MergeRequestsInterBatchDelay, () => cancelled);

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

      private Task<IEnumerable<MergeRequest>> loadProjectMergeRequestsAsync(ProjectKey project)
      {
         return call(
            () => _operator.SearchMergeRequestsAsync(
               new SearchCriteria(new object[] { new SearchByProject(project.ProjectName) }), null, true),
            String.Format("Cancelled loading merge requests for project \"{0}\"", project.ProjectName),
            String.Format("Cannot load project \"{0}\"", project.ProjectName));
      }

      private static bool isForbiddenProjectException(SessionException ex)
      {
         System.Net.HttpWebResponse response = getWebResponse(ex);
         return response != null ? response.StatusCode == System.Net.HttpStatusCode.Forbidden : false;
      }

      private static bool isNotFoundProjectException(SessionException ex)
      {
         System.Net.HttpWebResponse response = getWebResponse(ex);
         return response != null ? response.StatusCode == System.Net.HttpStatusCode.NotFound : false;
      }

      private static System.Net.HttpWebResponse getWebResponse(SessionException ex)
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

