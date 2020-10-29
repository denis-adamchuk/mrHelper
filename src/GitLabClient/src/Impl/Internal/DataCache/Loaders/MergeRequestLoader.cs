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

         IEnumerable<MergeRequest> mergeRequests = await call(
            () => _operator.SearchMergeRequestsAsync(
               new SearchQuery
               {
                  IId = mrk.IId,
                  ProjectName = mrk.ProjectKey.ProjectName,
                  State = fetchOnlyOpenMergeRequests ? "opened" : null,
                  MaxResults = 1
               }),
            String.Format("Cancelled loading MR with IId {0}", mrk.IId),
            String.Format("Cannot load merge request with IId {0}", mrk.IId));

         if (mergeRequests.Any())
         {
            _cacheUpdater.UpdateMergeRequest(mrk, mergeRequests.First());

            // TODO Optimization - Don't load versions and commits if nothing changed
            await _versionLoader.LoadVersionsAndCommits(
               new Dictionary<ProjectKey, IEnumerable<MergeRequest>>{ { mrk.ProjectKey,  mergeRequests.Take(1) } });
         }
      }

      private readonly bool _updateOnlyOpened;
      private readonly IVersionLoader _versionLoader;
      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

