using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Types;

namespace mrHelper.Client.Session
{
   internal class MergeRequestLoader : BaseSessionLoader, IMergeRequestLoader
   {
      internal MergeRequestLoader(SessionOperator op, InternalCacheUpdater cacheUpdater)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _versionLoader = new VersionLoader(op, cacheUpdater);
      }

      async public Task<bool> LoadMergeRequest(MergeRequestKey mrk)
      {
         IEnumerable<MergeRequest> mergeRequests = await call(
            () => _operator.SearchMergeRequestsAsync(
               new SearchCriteria(new object[] { new SearchByIId(mrk.ProjectKey.ProjectName, mrk.IId) }), null, false),
            String.Format("Cancelled loading MR with IId {0}", mrk.IId),
            String.Format("Cannot load merge request with IId {0}", mrk.IId));
         if (mergeRequests == null)
         {
            return false;
         }

         if (mergeRequests.Any())
         {
            _cacheUpdater.UpdateMergeRequest(mrk, mergeRequests.First());
            return await _versionLoader.LoadVersionsAsync(mrk) && await _versionLoader.LoadCommitsAsync(mrk);
         }
         return true;
      }

      private readonly IVersionLoader _versionLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

