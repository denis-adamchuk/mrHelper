using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Client.Workflow;
using System.ComponentModel;
using System.Diagnostics;
using Version = GitLabSharp.Entities.Version;
using GitLabSharp;

namespace mrHelper.Client.Updates
{
   internal class WorkflowDetailsCache
   {
      /// <summary>
      /// Cache passed merge requests
      /// </summary>
      internal void UpdateMergeRequests(string hostname, string projectname, List<MergeRequest> mergeRequests)
      {
         ProjectKey key = new ProjectKey{ HostName = hostname, ProjectName = projectname };
         List<MergeRequest> previouslyCachedMergeRequests = InternalDetails.GetMergeRequests(key);
         InternalDetails.SetMergeRequests(key, mergeRequests);

         if (mergeRequests.Count != previouslyCachedMergeRequests.Count)
         {
            Trace.TraceInformation(String.Format(
               "[WorkflowDetailsCache] Number of cached merge requests for project {0} at {1} is {2} (was {3} before update)",
               projectname, hostname, mergeRequests.Count, previouslyCachedMergeRequests.Count));
         }

         cleanupOldRecords(key, previouslyCachedMergeRequests, mergeRequests);
      }

      /// <summary>
      /// Cache passed version
      /// </summary>
      internal void UpdateLatestVersion(MergeRequestKey mrk, Version latestVersion)
      {
         DateTime previouslyCachedTimestamp = InternalDetails.GetLatestChangeTimestamp(mrk);
         InternalDetails.SetLatestChangeTimestamp(mrk, latestVersion.Created_At);

         if (previouslyCachedTimestamp > latestVersion.Created_At)
         {
            Debug.Assert(false);
            Trace.TraceWarning("[WorkflowDetailsCache] Latest version is older than a previous one");
         }

         if (latestVersion.Created_At != previouslyCachedTimestamp)
         {
            Trace.TraceInformation(String.Format(
               "[WorkflowDetailsCache] Latest version of merge request with IId {0} has timestamp {1} (was {2} before update)",
               mrk.IId,
               latestVersion.Created_At.ToLocalTime().ToString(),
               previouslyCachedTimestamp.ToLocalTime().ToString()));
         }
      }

      internal IWorkflowDetails Details { get { return InternalDetails; } }

      private void cleanupOldRecords(ProjectKey key, List<MergeRequest> oldRecords, List<MergeRequest> newRecords)
      {
         foreach (MergeRequest mergeRequest in oldRecords)
         {
            if (!newRecords.Any((x) => x.Id == mergeRequest.Id))
            {
               InternalDetails.CleanupTimestamps(new MergeRequestKey { ProjectKey = key, IId = mergeRequest.IId });
            }
         }
      }

      private WorkflowDetails InternalDetails { get; } = new WorkflowDetails();
   }
}

