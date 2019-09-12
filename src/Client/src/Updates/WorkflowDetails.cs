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
   // TODO: It is not enough to have unique project id because of multiple hosts

   internal struct WorkflowDetails
   {
      /// <summary>
      /// Return project name (Path_With_Namespace) by unique project Id
      /// </summary>
      public string GetProjectName(int projectId)
      {
         Debug.Assert(ProjectNames.ContainsKey(projectId));
         return ProjectNames.ContainsKey(projectId) ? ProjectNames[projectId] : String.Empty;
      }

      /// <summary>
      /// Add a project name/id pair to the cache
      /// </summary>
      public void SetProjectName(int projectId, string name)
      {
         ProjectNames[projectId] = name;
      }

      /// <summary>
      /// Return a list of merge requests by unique project id
      /// </summary>
      public List<MergeRequest> GetMergeRequests(int project Id)
      {
         return MergeRequests.ContainsKey(projectId) ? MergeRequests[projectId] : new List<MergeRequest>();
      }

      /// <summary>
      /// Add a merge request to a list of merge requests for the given project
      /// </summary>
      public void AddMergeRequest(int projectId, MergeRequest mergeRequest)
      {
         GetMergeRequests(projectId).Add(mergeRequest);
      }

      /// <summary>
      /// Return a timestamp of the most recent commit of a specified merge request
      /// </summary>
      public DateTime GetLatestCommitTimestamp(int mergeRequestId)
      {
         Debug.Assert(Commits.ContainsKey(mergeRequestId));
         return Commits.ContainsKey(mergeRequestId) ? Commits[mergeRequestId] : DateTime.MinValue;
      }

      /// <summary>
      /// Update a timestamp of the most recent commit of a specified merge request
      /// </summary>
      public void SetLatestCommitTimestamp(int mergeRequestId, DateTime timestamp)
      {
         Commits[mergeRequestId] = timestamp;
      }

      // maps unique project id to project's Path with Namespace property
      private Dictionary<int, string> ProjectNames = new Dictionary<int, string>();

      // maps unique project id to list of merge requests
      private Dictionary<int, List<MergeRequest>> MergeRequests = new Dictionary<int, List<MergeRequest>>();

      // maps unique Merge Request Id (not IId) to a timestamp of its latest commit
      private Dictionary<int, DateTime> Commits = new Dictionary<int, DateTime>();
   }
}

