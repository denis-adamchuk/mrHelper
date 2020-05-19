using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.MergeRequests;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Session
{
   internal class VersionLoader : BaseSessionLoader, IVersionLoader
   {
      internal VersionLoader(SessionOperator op, InternalCacheUpdater cacheUpdater)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
      }

      async public Task<bool> LoadVersionsAndCommits(Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests)
      {
         Exception exception = null;
         bool cancelled = false;
         async Task loadVersionsLocal(Tuple<MergeRequestKey, bool> tuple)
         {
            if (cancelled)
            {
               return;
            }

            try
            {
               if (!await (tuple.Item2 ? LoadVersionsAsync(tuple.Item1) : LoadCommitsAsync(tuple.Item1)))
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

         List<MergeRequestKey> allKeys = new List<MergeRequestKey>();
         foreach (KeyValuePair<ProjectKey, IEnumerable<MergeRequest>> kv in mergeRequests)
         {
            foreach (MergeRequest mergeRequest in kv.Value)
            {
               allKeys.Add(new MergeRequestKey(kv.Key, mergeRequest.IId));
            }
         }

         // to load versions and commits in parallel
         IEnumerable<Tuple<MergeRequestKey, bool>> duplicateKeys =
               allKeys
               .Select(x => new Tuple<MergeRequestKey, bool>(x, true))
            .Concat(
               allKeys
               .Select(x => new Tuple<MergeRequestKey, bool>(x, false)));

         await TaskUtils.RunConcurrentFunctionsAsync(duplicateKeys, x => loadVersionsLocal(x),
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

