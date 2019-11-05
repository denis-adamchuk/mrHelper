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

namespace mrHelper.Client.MergeRequests
{
   internal class WorkflowDetails : IWorkflowDetails
   {
      internal WorkflowDetails()
      {
         _mergeRequests = new Dictionary<ProjectKey, List<MergeRequest>>();
         _changes = new Dictionary<MergeRequestKey, DateTime>();
      }

      private WorkflowDetails(WorkflowDetails details)
      {
         _mergeRequests = new Dictionary<ProjectKey, List<MergeRequest>>(details._mergeRequests);
         _changes = new Dictionary<MergeRequestKey, DateTime>(details._changes);
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
      public IEnumerable<MergeRequest> GetMergeRequests(ProjectKey key)
      {
         return _mergeRequests.ContainsKey(key) ? new List<MergeRequest>(_mergeRequests[key]) : new List<MergeRequest>();
      }

      /// <summary>
      /// Sets a list of merge requests for the given project
      /// </summary>
      internal void SetMergeRequests(ProjectKey key, List<MergeRequest> mergeRequests)
      {
         _mergeRequests[key] = mergeRequests;
      }

      /// <summary>
      /// Return a timestamp of the most recent version of a specified merge request
      /// </summary>
      public DateTime GetLatestChangeTimestamp(MergeRequestKey mrk)
      {
         return _changes.ContainsKey(mrk) ? _changes[mrk] : DateTime.MinValue;
      }

      /// <summary>
      /// Update a timestamp of the most recent version of a specified merge request
      /// </summary>
      internal void SetLatestChangeTimestamp(MergeRequestKey mrk, DateTime timestamp)
      {
         _changes[mrk] = timestamp;
      }

      /// <summary>
      /// Remove records from Changes collection
      /// </summary>
      internal void CleanupTimestamps(MergeRequestKey mrk)
      {
         _changes.Remove(mrk);
      }

      // maps unique project id to list of merge requests
      private readonly Dictionary<ProjectKey, List<MergeRequest>> _mergeRequests;

      // maps Merge Request to a timestamp of its latest version
      private readonly Dictionary<MergeRequestKey, DateTime> _changes;
   }
}

