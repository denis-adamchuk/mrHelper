using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using System.Diagnostics;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.MergeRequests
{
   internal class InternalCacheUpdater
   {
      internal InternalCacheUpdater(InternalCache internalCache)
      {
         _cache = internalCache;
      }

      /// <summary>
      /// Cache passed merge requests
      /// </summary>
      internal void UpdateMergeRequests(ProjectKey key, IEnumerable<MergeRequest> mergeRequests)
      {
         IEnumerable<MergeRequest> previouslyCachedMergeRequests = _cache.GetMergeRequests(key);
         _cache.SetMergeRequests(key, mergeRequests);

         if (mergeRequests.Count() != previouslyCachedMergeRequests.Count())
         {
            Debug.WriteLine(String.Format(
               "[InternalCacheUpdater] Number of cached merge requests for project {0} at {1} is {2} (was {3} before update)",
               key.ProjectName, key.HostName, mergeRequests.Count(), previouslyCachedMergeRequests.Count()));
         }

         cleanupOldRecords(key, previouslyCachedMergeRequests, mergeRequests);
      }

      /// <summary>
      /// Cache passed commits
      /// </summary>
      internal void UpdateCommits(MergeRequestKey mrk, IEnumerable<Commit> commits)
      {
         Commit oldLatestCommit =
            _cache.GetCommits(mrk).OrderBy(x => x.Created_At).LastOrDefault();
         Commit newLatestCommit =
            commits.OrderBy(x => x.Created_At).LastOrDefault();

         _cache.SetCommits(mrk, commits);

         if (oldLatestCommit != null && newLatestCommit != null
          && oldLatestCommit.Created_At > newLatestCommit.Created_At)
         {
            Debug.Assert(false);
            Trace.TraceWarning("[InternalCacheUpdater] Latest commit is older than a previous one");
         }
      }

      /// <summary>
      /// Cache passed versions
      /// </summary>
      internal void UpdateVersions(MergeRequestKey mrk, IEnumerable<Version> versions)
      {
         Version oldLatestVersion =
            _cache.GetVersions(mrk).OrderBy(x => x.Created_At).LastOrDefault();
         Version newLatestVersion =
            versions.OrderBy(x => x.Created_At).LastOrDefault();

         _cache.SetVersions(mrk, versions);

         if (oldLatestVersion != null && newLatestVersion != null
          && oldLatestVersion.Created_At > newLatestVersion.Created_At)
         {
            Debug.Assert(false);
            Trace.TraceWarning("[InternalCacheUpdater] Latest version is older than a previous one");
         }
      }

      /// <summary>
      /// Cache passed merge request
      /// </summary>
      internal void UpdateMergeRequest(MergeRequestKey mrk, MergeRequest mergeRequest)
      {
         _cache.UpdateMergeRequest(mrk, mergeRequest);
      }

      public IInternalCache Cache => _cache;

      private void cleanupOldRecords(ProjectKey key,
         IEnumerable<MergeRequest> oldRecords, IEnumerable<MergeRequest> newRecords)
      {
         foreach (MergeRequest mergeRequest in oldRecords)
         {
            if (!newRecords.Any((x) => x.Id == mergeRequest.Id))
            {
               _cache.CleanupVersions(new MergeRequestKey(key, mergeRequest.IId));
            }
         }
      }

      private readonly InternalCache _cache;
   }
}

