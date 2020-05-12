using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Types;

namespace mrHelper.Client.Session
{
   internal class VersionLoader : BaseSessionLoader, IVersionLoader
   {
      internal VersionLoader(SessionOperator op, InternalCacheUpdater cache)
         : base(op)
      {
         _cache = cache;
      }

      async public Task<bool> LoadCommitsAsync(MergeRequestKey mrk)
      {
         IEnumerable<Commit> commits;
         try
         {
            commits = await _operator.GetCommitsAsync(mrk.ProjectKey.ProjectName, mrk.IId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading commits for merge request with IId {0}",
               mrk.IId);
            string errorMessage = String.Format("Cannot load commits for merge request with IId {0}",
               mrk.IId);
            handleOperatorException(ex, cancelMessage, errorMessage, null);
            return false;
         }
         _cache.UpdateCommits(mrk, commits);
         return true;
      }

      async public Task<bool> LoadVersionsAsync(MergeRequestKey mrk)
      {
         IEnumerable<GitLabSharp.Entities.Version> versions;
         try
         {
            versions = await _operator.GetVersionsAsync(mrk.ProjectKey.ProjectName, mrk.IId);
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading versions for merge request with IId {0}",
               mrk.IId);
            string errorMessage = String.Format("Cannot load versions for merge request with IId {0}",
               mrk.IId);
            handleOperatorException(ex, cancelMessage, errorMessage, null);
            return false;
         }
         _cache.UpdateVersions(mrk, versions);
         return true;
      }

      private readonly InternalCacheUpdater _cache;
   }
}

