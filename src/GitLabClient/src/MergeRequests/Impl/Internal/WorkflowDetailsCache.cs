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
   internal class WorkflowDetailsCache : IWorkflowDetailsCacheReader
   {
      /// <summary>
      /// Cache passed merge requests
      /// </summary>
      internal void UpdateMergeRequests(ProjectKey key, IEnumerable<MergeRequest> mergeRequests)
      {
         IEnumerable<MergeRequest> previouslyCachedMergeRequests = _internalDetails.GetMergeRequests(key);
         _internalDetails.SetMergeRequests(key, mergeRequests);

         if (mergeRequests.Count() != previouslyCachedMergeRequests.Count())
         {
            Debug.WriteLine(String.Format(
               "[WorkflowDetailsCache] Number of cached merge requests for project {0} at {1} is {2} (was {3} before update)",
               key.ProjectName, key.HostName, mergeRequests.Count(), previouslyCachedMergeRequests.Count()));
         }

         cleanupOldRecords(key, previouslyCachedMergeRequests, mergeRequests);
      }

      /// <summary>
      /// Cache passed versions
      /// </summary>
      internal void UpdateVersions(MergeRequestKey mrk, IEnumerable<Version> versions)
      {
         Version oldLatestVersion =
            _internalDetails.GetVersions(mrk).OrderBy(x => x.Created_At).LastOrDefault();
         Version newLatestVersion =
            versions.OrderBy(x => x.Created_At).LastOrDefault();

         _internalDetails.SetVersions(mrk, versions);

         if (oldLatestVersion.Created_At > newLatestVersion.Created_At)
         {
            Debug.Assert(false);
            Trace.TraceWarning("[WorkflowDetailsCache] Latest version is older than a previous one");
         }

         if (newLatestVersion.Created_At != oldLatestVersion.Created_At)
         {
            Debug.WriteLine(String.Format(
               "[WorkflowDetailsCache] Latest version of merge request with IId {0} has timestamp {1} (was {2} before update)",
               mrk.IId,
               newLatestVersion.Created_At.ToLocalTime().ToString(),
               oldLatestVersion.Created_At.ToLocalTime().ToString()));
         }
      }

      /// <summary>
      /// Cache passed merge request
      /// </summary>
      internal void UpdateMergeRequest(MergeRequestKey mrk, MergeRequest mergeRequest)
      {
         _internalDetails.UpdateMergeRequest(mrk, mergeRequest);
      }

      public IWorkflowDetails Details => _internalDetails;

      private void cleanupOldRecords(ProjectKey key,
         IEnumerable<MergeRequest> oldRecords, IEnumerable<MergeRequest> newRecords)
      {
         foreach (MergeRequest mergeRequest in oldRecords)
         {
            if (!newRecords.Any((x) => x.Id == mergeRequest.Id))
            {
               _internalDetails.CleanupVersions(new MergeRequestKey { ProjectKey = key, IId = mergeRequest.IId });
            }
         }
      }

      private readonly WorkflowDetails _internalDetails = new WorkflowDetails();
   }
}

