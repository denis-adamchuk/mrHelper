using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.MergeRequests
{
   internal class WorkflowDetails : IWorkflowDetails
   {
      internal WorkflowDetails()
      {
         _mergeRequests = new Dictionary<ProjectKey, MergeRequest[]>();
         _changes = new Dictionary<MergeRequestKey, DateTime>();
      }

      private WorkflowDetails(WorkflowDetails details)
      {
         _mergeRequests = details._mergeRequests.ToDictionary(
            item => item.Key,
            item => item.Value.ToArray());
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
         return _mergeRequests.ContainsKey(key) ?
            _mergeRequests[key].ToArray() : Array.Empty<MergeRequest>();
      }

      /// <summary>
      /// Sets a list of merge requests for the given project
      /// </summary>
      internal void SetMergeRequests(ProjectKey key, IEnumerable<MergeRequest> mergeRequests)
      {
         _mergeRequests[key] = mergeRequests.ToArray();
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
      /// Updates a merge request
      /// </summary>
      internal void UpdateMergeRequest(MergeRequestKey mrk, MergeRequest mergeRequest)
      {
         if (_mergeRequests.ContainsKey(mrk.ProjectKey))
         {
            int index = Array.FindIndex(_mergeRequests[mrk.ProjectKey], x => x.IId == mrk.IId);
            if (index != -1)
            {
               _mergeRequests[mrk.ProjectKey][index] = mergeRequest;
            }
         }
      }

      /// <summary>
      /// Remove records from Changes collection
      /// </summary>
      internal void CleanupTimestamps(MergeRequestKey mrk)
      {
         _changes.Remove(mrk);
      }

      // maps unique project id to list of merge requests
      private readonly Dictionary<ProjectKey, MergeRequest[]> _mergeRequests;

      // maps Merge Request to a timestamp of its latest version
      private readonly Dictionary<MergeRequestKey, DateTime> _changes;
   }
}
