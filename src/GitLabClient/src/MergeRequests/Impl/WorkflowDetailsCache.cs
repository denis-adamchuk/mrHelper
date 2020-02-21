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
   internal class WorkflowDetailsCache
   {
      /// <summary>
      /// Cache passed merge requests
      /// </summary>
      internal void UpdateMergeRequests(string hostname, string projectname, IEnumerable<MergeRequest> mergeRequests)
      {
         ProjectKey key = new ProjectKey{ HostName = hostname, ProjectName = projectname };
         IEnumerable<MergeRequest> previouslyCachedMergeRequests = _internalDetails.GetMergeRequests(key);
         _internalDetails.SetMergeRequests(key, mergeRequests);

         if (mergeRequests.Count() != previouslyCachedMergeRequests.Count())
         {
            Trace.TraceInformation(String.Format(
               "[WorkflowDetailsCache] Number of cached merge requests for project {0} at {1} is {2} (was {3} before update)",
               projectname, hostname, mergeRequests.Count(), previouslyCachedMergeRequests.Count()));
         }

         cleanupOldRecords(key, previouslyCachedMergeRequests, mergeRequests);
      }

      /// <summary>
      /// Cache passed version
      /// </summary>
      internal void UpdateLatestVersion(MergeRequestKey mrk, Version latestVersion)
      {
         Version previouslyCachedVersion = _internalDetails.GetLatestVersion(mrk);
         _internalDetails.SetLatestVersion(mrk, latestVersion);

         if (previouslyCachedVersion.Created_At > latestVersion.Created_At)
         {
            Debug.Assert(false);
            Trace.TraceWarning("[WorkflowDetailsCache] Latest version is older than a previous one");
         }

         if (latestVersion.Created_At != previouslyCachedVersion.Created_At)
         {
            Trace.TraceInformation(String.Format(
               "[WorkflowDetailsCache] Latest version of merge request with IId {0} has timestamp {1} (was {2} before update)",
               mrk.IId,
               latestVersion.Created_At.ToLocalTime().ToString(),
               previouslyCachedVersion.Created_At.ToLocalTime().ToString()));
         }
      }

      /// <summary>
      /// Cache passed merge request
      /// </summary>
      internal void UpdateMergeRequest(MergeRequestKey mrk, MergeRequest mergeRequest)
      {
         _internalDetails.UpdateMergeRequest(mrk, mergeRequest);
      }

      internal IWorkflowDetails Details { get { return _internalDetails; } }

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

