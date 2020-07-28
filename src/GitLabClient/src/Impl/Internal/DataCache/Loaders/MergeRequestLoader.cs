using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class MergeRequestLoader : BaseSessionLoader, IMergeRequestLoader
   {
      internal MergeRequestLoader(DataCacheOperator op, InternalCacheUpdater cacheUpdater)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _versionLoader = new VersionLoader(op, cacheUpdater);
      }

      async public Task LoadMergeRequest(MergeRequestKey mrk)
      {
         IEnumerable<MergeRequest> mergeRequests = await call(
            () => _operator.SearchMergeRequestsAsync(
               new SearchCriteria(new object[] { new SearchByIId(mrk.ProjectKey.ProjectName, mrk.IId) }), null, false),
            String.Format("Cancelled loading MR with IId {0}", mrk.IId),
            String.Format("Cannot load merge request with IId {0}", mrk.IId));

         if (mergeRequests.Any())
         {
            _cacheUpdater.UpdateMergeRequest(mrk, mergeRequests.First());
            await _versionLoader.LoadVersionsAndCommits(
               new Dictionary<ProjectKey, IEnumerable<MergeRequest>>{ { mrk.ProjectKey,  mergeRequests.Take(1) } });
         }
      }

      private readonly IVersionLoader _versionLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

