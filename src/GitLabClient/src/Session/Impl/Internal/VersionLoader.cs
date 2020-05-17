using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.MergeRequests;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.Session
{
   internal class VersionLoader : BaseSessionLoader, IVersionLoader
   {
      internal VersionLoader(SessionOperator op, InternalCacheUpdater cacheUpdater)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
      }

      async public Task<bool> LoadCommitsAsync(MergeRequestKey mrk)
      {
         IEnumerable<Commit> commits = await call(
            () => _operator.GetCommitsAsync(mrk.ProjectKey.ProjectName, mrk.IId),
            String.Format("Cancelled loading commits for merge request with IId {0}", mrk.IId),
            String.Format("Cannot load commits for merge request with IId {0}", mrk.IId));
         if (commits != null)
         {
            _cacheUpdater.UpdateCommits(mrk, commits);
            return true;
         }
         return false;
      }

      async public Task<bool> LoadVersionsAsync(MergeRequestKey mrk)
      {
         IEnumerable<Version> versions = await call(
            () => _operator.GetVersionsAsync(mrk.ProjectKey.ProjectName, mrk.IId),
            String.Format("Cancelled loading versions for merge request with IId {0}", mrk.IId),
            String.Format("Cannot load versions for merge request with IId {0}", mrk.IId));
         if (versions != null)
         {
            _cacheUpdater.UpdateVersions(mrk, versions);
            return true;
         }
         return false;
      }

      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

