using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class RenamedProjectException : BaseLoaderException
   {
      public RenamedProjectException(Exception innerException)
         : base(String.Empty, innerException) { }
   }

   internal class MergeRequestLoader : BaseDataCacheLoader, IMergeRequestLoader
   {
      internal MergeRequestLoader(DataCacheOperator op, InternalCacheUpdater cacheUpdater, bool updateOnlyOpened,
         bool isApprovalStatusSupported)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _versionLoader = new VersionLoader(op, cacheUpdater);
         _approvalLoader = isApprovalStatusSupported ? new ApprovalLoader(op, cacheUpdater) : null;
         _updateOnlyOpened = updateOnlyOpened;
      }

      async public Task LoadMergeRequest(MergeRequestKey mrk)
      {
         bool fetchOnlyOpenMergeRequests = _updateOnlyOpened;

         MergeRequest mergeRequest = await fetchMergeRequest(mrk);
         if (mergeRequest != null && (!fetchOnlyOpenMergeRequests || mergeRequest.State == "opened"))
         {
            MergeRequest cachedMergeRequest = _cacheUpdater.Cache.GetMergeRequest(mrk);
            _cacheUpdater.UpdateMergeRequest(mrk, mergeRequest);

            MergeRequestKey[] dummyArray = new MergeRequestKey[] { mrk };
            if (Helpers.GetVersionLoaderKey(cachedMergeRequest) != Helpers.GetVersionLoaderKey(mergeRequest))
            {
               await _versionLoader.LoadVersionsAndCommits(dummyArray);
            }

            // Note: GitLab (13.6) does not changed Updated_At when approval is revoked
            if (_approvalLoader != null)
            {
               await _approvalLoader.LoadApprovals(dummyArray);
            }
         }
      }

      private async Task<MergeRequest> fetchMergeRequest(MergeRequestKey mrk)
      {
         try
         {
            return await call(
               () => _operator.GetMergeRequestAsync(mrk.ProjectKey.ProjectName, mrk.IId, true),
               String.Format("Cancelled loading MR with IId {0}", mrk.IId),
               String.Format("Cannot load merge request with IId {0}", mrk.IId));
         }
         catch (BaseLoaderException ex)
         {
            if (ex.GetWebResponse()?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
               throw new RenamedProjectException(ex);
            }
            throw;
         }
      }

      private readonly bool _updateOnlyOpened;
      private readonly IVersionLoader _versionLoader;
      private readonly IApprovalLoader _approvalLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

