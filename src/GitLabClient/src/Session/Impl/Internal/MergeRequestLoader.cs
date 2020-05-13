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
         return await loadMergeRequestAsync(mrk);
      }

      async private Task<bool> loadMergeRequestAsync(MergeRequestKey mrk)
      {
         MergeRequest mergeRequest = new MergeRequest();
         try
         {
            SearchByIId searchByIId = new SearchByIId { ProjectName = mrk.ProjectKey.ProjectName, IId = mrk.IId };
            IEnumerable<MergeRequest> mergeRequests =
               await _operator.SearchMergeRequestsAsync(searchByIId, null, true /* TODO only open */);
            mergeRequest = mergeRequests.FirstOrDefault();
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading MR with IId {0}", mrk.IId);
            string errorMessage = String.Format("Cannot load merge request with IId {0}", mrk.IId);
            handleOperatorException(ex, cancelMessage, errorMessage);
            return false;
         }

         _cacheUpdater.UpdateMergeRequest(mrk, mergeRequest);
         return await _versionLoader.LoadVersionsAsync(mrk) && await _versionLoader.LoadCommitsAsync(mrk);
      }

      private readonly IVersionLoader _versionLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

