using System;
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

   internal class MergeRequestLoader : BaseDataCacheLoader, IMergeRequestLoader, IDisposable
   {
      internal MergeRequestLoader(DataCacheOperator op, InternalCacheUpdater cacheUpdater,
         bool isApprovalStatusSupported, DataCacheCallbacks callbacks)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _versionLoader = new VersionLoader(op, cacheUpdater);
         _approvalLoader = isApprovalStatusSupported ? new ApprovalLoader(op, cacheUpdater) : null;
         _envStatusLoader = new EnvironmentStatusLoader(op, cacheUpdater, callbacks);
         _avatarLoader = new AvatarLoader(cacheUpdater);
      }

      public void Dispose()
      {
         _avatarLoader.Dispose();
      }

      async public Task LoadMergeRequest(MergeRequestKey mrk)
      {
         MergeRequest mergeRequest = await fetchMergeRequest(mrk);
         if (mergeRequest == null)
         {
            return;
         }

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

         await _envStatusLoader.LoadEnvironmentStatus(dummyArray);
         await _avatarLoader.LoadAvatars(dummyArray);
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

      private readonly VersionLoader _versionLoader;
      private readonly EnvironmentStatusLoader _envStatusLoader;
      private readonly ApprovalLoader _approvalLoader;
      private readonly AvatarLoader _avatarLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

