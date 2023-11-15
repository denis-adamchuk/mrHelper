using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using System.Diagnostics;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Loaders.Cache
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
      internal void UpdateMergeRequests(Dictionary<ProjectKey, IEnumerable<MergeRequest>> mergeRequests)
      {
         if (mergeRequests == null)
         {
            Debug.Assert(false);
            return;
         }

         foreach (KeyValuePair<ProjectKey, IEnumerable<MergeRequest>> kv in mergeRequests)
         {
            IEnumerable<MergeRequest> previouslyCachedMergeRequests = _cache.GetMergeRequests(kv.Key);
#if DEBUG
            IEnumerable<MergeRequest> newMergeRequests = kv.Value;
            if (previouslyCachedMergeRequests != null && newMergeRequests.Count() != previouslyCachedMergeRequests.Count())
            {
               Trace.TraceInformation(String.Format(
                  "[InternalCacheUpdater] Number of cached merge requests for project {0} at {1} is {2} (was {3} before update)",
                  kv.Key.ProjectName, kv.Key.HostName, newMergeRequests.Count(), previouslyCachedMergeRequests.Count()));
            }
#endif

            cleanupOldRecords(kv.Key, previouslyCachedMergeRequests, kv.Value);
         }

         _cache.SetMergeRequests(mergeRequests);
      }

      /// <summary>
      /// Cache passed commits
      /// </summary>
      internal void UpdateCommits(MergeRequestKey mrk, IEnumerable<Commit> commits)
      {
         if (commits == null)
         {
            Debug.Assert(false);
            return;
         }

         _cache.SetCommits(mrk, commits);
      }

      /// <summary>
      /// Cache passed versions
      /// </summary>
      internal void UpdateVersions(MergeRequestKey mrk, IEnumerable<Version> versions)
      {
         if (versions == null)
         {
            Debug.Assert(false);
            return;
         }

         _cache.SetVersions(mrk, versions);
      }

      /// <summary>
      /// Cache passed approval configurations
      /// </summary>
      internal void UpdateApprovals(MergeRequestKey mrk, MergeRequestApprovalConfiguration approvals)
      {
         if (approvals == null)
         {
            Debug.Assert(false);
            return;
         }

         _cache.SetApprovals(mrk, approvals);
      }

      internal void UpdateEnvironmentStatus(MergeRequestKey mrk, IEnumerable<EnvironmentStatus> status)
      {
         if (status == null)
         {
            Debug.Assert(false);
            return;
         }

         _cache.SetEnvironmentStatus(mrk, status);
      }

      internal void UpdateAvatar(int userId, byte[] avatar)
      {
         _cache.SetAvatar(userId, avatar);
      }

      /// <summary>
      /// Cache passed merge request
      /// </summary>
      internal void UpdateMergeRequest(MergeRequestKey mrk, MergeRequest mergeRequest)
      {
         if (mergeRequest == null)
         {
            Debug.Assert(false);
            return;
         }

         _cache.UpdateMergeRequest(mrk, mergeRequest);
      }

      public IInternalCache Cache => _cache;

      private void cleanupOldRecords(ProjectKey key,
         IEnumerable<MergeRequest> oldRecords, IEnumerable<MergeRequest> newRecords)
      {
         if (oldRecords != null)
         {
            foreach (MergeRequest mergeRequest in oldRecords)
            {
               if (!newRecords.Any((x) => x.Id == mergeRequest.Id))
               {
                  _cache.CleanupVersions(new MergeRequestKey(key, mergeRequest.IId));
               }
            }
         }
      }

      private readonly InternalCache _cache;
   }
}

