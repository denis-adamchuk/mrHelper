using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class MergeRequestLoader : BaseDataCacheLoader, IMergeRequestLoader
   {
      internal MergeRequestLoader(DataCacheOperator op, InternalCacheUpdater cacheUpdater, bool updateOnlyOpened)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _versionLoader = new VersionLoader(op, cacheUpdater);
         _updateOnlyOpened = updateOnlyOpened;
      }

      async public Task LoadMergeRequest(MergeRequestKey mrk)
      {
         bool fetchOnlyOpenMergeRequests = _updateOnlyOpened;

         MergeRequest mergeRequest = await call(
            () => _operator.GetMergeRequestAsync(mrk.ProjectKey.ProjectName, mrk.IId, true),
            String.Format("Cancelled loading MR with IId {0}", mrk.IId),
            String.Format("Cannot load merge request with IId {0}", mrk.IId));

         if (mergeRequest != null && (!fetchOnlyOpenMergeRequests || mergeRequest.State == "opened"))
         {
            DateTime? oldUpdatedAt = _cacheUpdater.Cache.GetMergeRequest(mrk)?.Updated_At;
            DateTime newUpdatedAt = mergeRequest.Updated_At;
            _cacheUpdater.UpdateMergeRequest(mrk, mergeRequest);

            if (!oldUpdatedAt.HasValue || oldUpdatedAt < newUpdatedAt)
            {
               await _versionLoader.LoadVersionsAndCommits(new MergeRequestKey[] { mrk });
            }
         }
      }

      private readonly bool _updateOnlyOpened;
      private readonly IVersionLoader _versionLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

