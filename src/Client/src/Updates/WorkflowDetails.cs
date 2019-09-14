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
         ProjectNames = new Dictionary<ProjectKey, string>();
         MergeRequests = new Dictionary<ProjectKey, List<MergeRequest>>();
         Changes = new Dictionary<int, DateTime>();
      }

      private WorkflowDetails(WorkflowDetails details)
      {
         ProjectNames = new Dictionary<ProjectKey, string>(details.ProjectNames);
         MergeRequests = new Dictionary<ProjectKey, List<MergeRequest>>(details.MergeRequests);
         Changes = new Dictionary<int, DateTime>(details.Changes);
      }

      /// <summary>
      /// Create a copy of object
      /// </summary>
      public IWorkflowDetails Clone()
      {
         return new WorkflowDetails(this);
      }

      /// <summary>
      /// Return project name (Path_With_Namespace) by unique project Id
      /// </summary>
      public string GetProjectName(ProjectKey key)
      {
         Debug.Assert(ProjectNames.ContainsKey(key));
         return ProjectNames.ContainsKey(key) ? ProjectNames[key] : String.Empty;
      }

      /// <summary>
      /// Add a project name/id pair to the cache
      /// </summary>
      internal void SetProjectName(ProjectKey key, string name)
      {
         ProjectNames[key] = name;
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
      public DateTime GetLatestChangeTimestamp(int mergeRequestId)
      {
         return Changes.ContainsKey(mergeRequestId) ? Changes[mergeRequestId] : DateTime.MinValue;
      }

      /// <summary>
      /// Update a timestamp of the most recent version of a specified merge request
      /// </summary>
      internal void SetLatestChangeTimestamp(int mergeRequestId, DateTime timestamp)
      {
         Changes[mergeRequestId] = timestamp;
      }

      /// <summary>
      /// Remove records from Changes collection
      /// </summary>
      internal void CleanupTimestamps(int mergeRequestId)
      {
         Changes.Remove(mergeRequestId);
      }

      /// <summary>
      /// Return project Id by merge request Id
      /// </summary>
      public ProjectKey GetProjectKey(int mergeRequestId)
      {
         foreach (KeyValuePair<ProjectKey, List<MergeRequest>> mergeRequests in MergeRequests)
         {
            if (mergeRequests.Value.Any((x) => x.Id == mergeRequestId))
            {
               return mergeRequests.Key;
            }
         }
         return new ProjectKey { HostName = String.Empty, ProjectId = 0 };
      }

      // maps unique project id to project's Path with Namespace property
      private Dictionary<ProjectKey, string> ProjectNames;

      // maps unique project id to list of merge requests
      private Dictionary<ProjectKey, List<MergeRequest>> MergeRequests;

      // maps unique Merge Request Id (not IId) to a timestamp of its latest version
      private Dictionary<int, DateTime> Changes;
   }
}

