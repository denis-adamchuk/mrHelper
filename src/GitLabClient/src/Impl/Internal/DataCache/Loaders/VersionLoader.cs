using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class VersionLoader : BaseDataCacheLoader, IVersionLoader
   {
      internal VersionLoader(DataCacheOperator op, InternalCacheUpdater cacheUpdater)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
      }

      async public Task LoadVersionsAndCommits(IEnumerable<MergeRequestKey> mergeRequestKeys)
      {
         Exception exception = null;
         async Task loadVersionsLocal(Tuple<MergeRequestKey, bool> tuple)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               await (tuple.Item2 ? LoadVersionsAsync(tuple.Item1) : LoadCommitsAsync(tuple.Item1));
            }
            catch (BaseLoaderException ex)
            {
               exception = ex;
            }
         }

         // to load versions and commits in parallel
         IEnumerable<Tuple<MergeRequestKey, bool>> duplicateKeys =
               mergeRequestKeys
               .Select(x => new Tuple<MergeRequestKey, bool>(x, true))
            .Concat(
               mergeRequestKeys
               .Select(x => new Tuple<MergeRequestKey, bool>(x, false)));

         await TaskUtils.RunConcurrentFunctionsAsync(duplicateKeys, x => loadVersionsLocal(x),
            () => Constants.VersionLoaderMergeRequestBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }

         IEnumerable<Tuple<ProjectKey, string>> missingCommitIds = gatherMissingCommitIds(mergeRequestKeys);
         IEnumerable<Commit> missingCommits = await loadMissingCommits(missingCommitIds.Distinct());
         applyCommitsToVersions(mergeRequestKeys, missingCommits);
      }

      private IEnumerable<Tuple<ProjectKey, string>> gatherMissingCommitIds(IEnumerable<MergeRequestKey> allKeys)
      {
         List<Tuple<ProjectKey, string>> missingCommitIds = new List<Tuple<ProjectKey, string>>();
         foreach (MergeRequestKey mrk in allKeys)
         {
            IEnumerable<Commit> commits = _cacheUpdater.Cache.GetCommits(mrk);
            IEnumerable<Version> versions = _cacheUpdater.Cache.GetVersions(mrk);
            foreach (Version version in versions)
            {
               if (!commits.Any(x => x.Id == version.Head_Commit_SHA)
                && !String.IsNullOrEmpty(version.Head_Commit_SHA))
               {
                  missingCommitIds.Add(new Tuple<ProjectKey, string>(mrk.ProjectKey, version.Head_Commit_SHA));
               }
            }
         }
         return missingCommitIds;
      }

      async private Task<IEnumerable<Commit>> loadMissingCommits(
         IEnumerable<Tuple<ProjectKey, string>> missingCommitIds)
      {
         Exception exception = null;
         List<Commit> missingCommits = new List<Commit>();
         async Task loadMissingCommits(Tuple<ProjectKey, string> commitIds)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               missingCommits.Add(await LoadCommit(commitIds.Item1, commitIds.Item2));
            }
            catch (BaseLoaderException ex)
            {
               exception = ex;
            }
         }
         await TaskUtils.RunConcurrentFunctionsAsync(missingCommitIds, x => loadMissingCommits(x),
            () => Constants.VersionLoaderCommitBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }
         return missingCommits;
      }

      private void applyCommitsToVersions(IEnumerable<MergeRequestKey> allKeys, IEnumerable<Commit> missingCommits)
      {
         foreach (MergeRequestKey mrk in allKeys)
         {
            IEnumerable<Commit> commits = _cacheUpdater.Cache.GetCommits(mrk);
            IEnumerable<Version> versions = _cacheUpdater.Cache.GetVersions(mrk);
            List<Version> versionsExtended = new List<Version>();
            foreach (Version version in versions)
            {
               if (String.IsNullOrEmpty(version.Head_Commit_SHA))
               {
                  continue;
               }

               Commit commit = commits.SingleOrDefault(x => x.Id == version.Head_Commit_SHA);
               if (commit == null)
               {
                  commit = missingCommits.SingleOrDefault(x => x.Id == version.Head_Commit_SHA);
               }
               IEnumerable<Commit> versionCommits = commit == null ? null : new Commit[] { commit };
               versionsExtended.Add(new Version(version.Id, version.Base_Commit_SHA, version.Head_Commit_SHA,
                  version.Start_Commit_SHA, version.Created_At, version.Diffs, versionCommits));
            }
            _cacheUpdater.UpdateVersions(mrk, versionsExtended);
         }
      }

      async public Task LoadCommitsAsync(MergeRequestKey mrk)
      {
         IEnumerable<Commit> commits = await call(
            () => _operator.GetCommitsAsync(mrk.ProjectKey.ProjectName, mrk.IId),
            String.Format("Cancelled loading commits for merge request with IId {0}", mrk.IId),
            String.Format("Cannot load commits for merge request with IId {0}", mrk.IId));
         _cacheUpdater.UpdateCommits(mrk, commits);
      }

      async public Task LoadVersionsAsync(MergeRequestKey mrk)
      {
         IEnumerable<Version> versions = await call(
            () => _operator.GetVersionsAsync(mrk.ProjectKey.ProjectName, mrk.IId),
            String.Format("Cancelled loading versions for merge request with IId {0}", mrk.IId),
            String.Format("Cannot load versions for merge request with IId {0}", mrk.IId));
         _cacheUpdater.UpdateVersions(mrk, versions);
      }

      async public Task<Commit> LoadCommit(ProjectKey projectKey, string id)
      {
         return await call(
            () => _operator.GetCommitAsync(projectKey.ProjectName, id),
            String.Format("Cancelled loading commit {0}", id),
            String.Format("Cannot load commit {0}", id));
      }

      private readonly InternalCacheUpdater _cacheUpdater;
   }
}

