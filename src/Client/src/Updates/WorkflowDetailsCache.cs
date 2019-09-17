using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Types;
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
      internal void UpdateMergeRequests(string hostname, Project project, List<MergeRequest> mergeRequests)
      {
         ProjectKey key = new ProjectKey{ HostName = hostname, ProjectId = project.Id };
         InternalDetails.SetProjectName(key, project.Path_With_Namespace);

         List<MergeRequest> previouslyCachedMergeRequests = InternalDetails.GetMergeRequests(key);
         InternalDetails.SetMergeRequests(key, mergeRequests);

         Trace.TraceInformation(String.Format(
            "[WorkflowDetailsCache] Number of cached merge requests for project {0} at {1} is {2} (was {3} before update)",
               project.Path_With_Namespace, hostname, mergeRequests.Count, previouslyCachedMergeRequests.Count));

         cleanupOldRecords(previouslyCachedMergeRequests, mergeRequests);
      }

      /// <summary>
      /// Cache passed version
      /// </summary>
      internal void UpdateLatestVersion(int mergeRequestId, Version latestVersion)
      {
         Debug.Assert(InternalDetails.GetProjectKey(mergeRequestId).ProjectId != 0);

         DateTime previouslyCachedTimestamp = InternalDetails.GetLatestChangeTimestamp(mergeRequestId);
         InternalDetails.SetLatestChangeTimestamp(mergeRequestId, latestVersion.Created_At);

         if (previouslyCachedTimestamp > latestVersion.Created_At)
         {
            Debug.Assert(false);
            Trace.TraceWarning("[WorkflowDetailsCache] Latest version is older than a previous one");
         }

         Trace.TraceInformation(String.Format(
            "[WorkflowDetailsCache] Latest version of merge request with Id {0} has timestamp {1} (was {2} before update)",
               mergeRequestId,
               latestVersion.Created_At.ToLocalTime().ToString(),
               previouslyCachedTimestamp.ToLocalTime().ToString()));
      }

      internal IWorkflowDetails Details { get { return InternalDetails; } }

      private void cleanupOldRecords(List<MergeRequest> oldRecords, List<MergeRequest> newRecords)
      {
         foreach (MergeRequest mergeRequest in oldRecords)
         {
            if (!newRecords.Any((x) => x.Id == mergeRequest.Id))
            {
               InternalDetails.CleanupTimestamps(mergeRequest.Id);
            }
         }
      }

      private WorkflowDetails InternalDetails { get; } = new WorkflowDetails();
   }
}

