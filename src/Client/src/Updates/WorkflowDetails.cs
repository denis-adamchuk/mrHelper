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

namespace mrHelper.Client.Updates
{
   internal class WorkflowDetails : IWorkflowDetails
   {
      internal WorkflowDetails()
      {
         MergeRequests = new Dictionary<ProjectKey, List<MergeRequest>>();
         Changes = new Dictionary<MergeRequestKey, DateTime>();
      }

      private WorkflowDetails(WorkflowDetails details)
      {
         MergeRequests = new Dictionary<ProjectKey, List<MergeRequest>>(details.MergeRequests);
         Changes = new Dictionary<MergeRequestKey, DateTime>(details.Changes);
      }

      /// <summary>
      /// Create a copy of object
      /// </summary>
      public IWorkflowDetails Clone()
      {
         return new WorkflowDetails(this);
      }

      /// <summary>
      /// Return a list of merge requests by unique project id
      /// </summary>
      public List<MergeRequest> GetMergeRequests(ProjectKey key)
      {
         return MergeRequests.ContainsKey(key) ? MergeRequests[key] : new List<MergeRequest>();
      }

      /// <summary>
      /// Sets a list of merge requests for the given project
      /// </summary>
      internal void SetMergeRequests(ProjectKey key, List<MergeRequest> mergeRequests)
      {
         MergeRequests[key] = mergeRequests;
      }

      /// <summary>
      /// Return a timestamp of the most recent version of a specified merge request
      /// </summary>
      public DateTime GetLatestChangeTimestamp(MergeRequestKey mrk)
      {
         return Changes.ContainsKey(mrk) ? Changes[mrk] : DateTime.MinValue;
      }

      /// <summary>
      /// Update a timestamp of the most recent version of a specified merge request
      /// </summary>
      internal void SetLatestChangeTimestamp(MergeRequestKey mrk, DateTime timestamp)
      {
         Changes[mrk] = timestamp;
      }

      /// <summary>
      /// Remove records from Changes collection
      /// </summary>
      internal void CleanupTimestamps(MergeRequestKey mrk)
      {
         Changes.Remove(mrk);
      }

      // maps unique project id to list of merge requests
      private Dictionary<ProjectKey, List<MergeRequest>> MergeRequests;

      // maps Merge Request to a timestamp of its latest version
      private readonly Dictionary<MergeRequestKey, DateTime> Changes;
   }
}

