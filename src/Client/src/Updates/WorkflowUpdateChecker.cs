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
   public struct MergeRequestUpdates
   {
      public List<MergeRequest> NewMergeRequests;
      public List<MergeRequest> UpdatedMergeRequests;
      public List<MergeRequest> ClosedMergeRequests;
   }

   /// <summary>
   /// Implements periodic checks for updates of Merge Requests and their Commits
   /// </summary>
   public class WorkflowUpdateChecker
   {
      private struct TwoListDifference<T>
      {
         public List<T> FirstOnly;
         public List<T> SecondOnly;
         public List<T> Common;
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      private void CheckForUpdates(List<Project> enabledProjects, WorkflowDetails prevState, WorkflowDetails curState)
      {
         TwoListDifference<MergeRequest> diff = getMergeRequestDiff(enabledProjects, prevState, curState);
         return getMergeRequestUpdates(diff, prevState, curState);
      }

      /// <summary>
      /// Calculate difference between current list of merge requests at GitLab and current list in the Workflow
      /// </summary>
      private Task<TwoListDifference<MergeRequest>> getMergeRequestDiff(
         List<Project> enabledProjects, WorkflowDetails prevState, WorkflowDetails curState)
      {
         TwoListDifference<MergeRequest> diff = new TwoListDifference<MergeRequest>
         {
            FirstOnly = new List<MergeRequest>(),
            SecondOnly = new List<MergeRequest>(),
            Common = new List<MergeRequest>()
         };

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Checking {0} projects", enabledProjects.Count));
         foreach (var project in enabledProjects)
         {
            List<MergeRequest> previouslyCachedMergeRequests =
               prevState.CachedMergeRequests.ContainsKey(project.Id) ? prevState.CachedMergeRequests[project.Id] : null;

            Debug.Assert(curState.CachedMergeRequests.ContainsKey(project.Id));

            if (previouslyCachedMergeRequests != null)
            {
               List<MergeRequest> newCachedMergeRequests = curState.CachedMergeRequests[project.Id];
               diff.FirstOnly.AddRange(previouslyCachedMergeRequests.Except(newCachedMergeRequests).ToList());
               diff.SecondOnly.AddRange(newCachedMergeRequests.Except(previouslyCachedMergeRequests).ToList());
               diff.Common.AddRange(previouslyCachedMergeRequests.Intersect(newCachedMergeRequests).ToList());
            }
         }

         return diff;
      }

      /// <summary>
      /// Convert a difference between two states into a list of merge request updates splitted in new/updated/closed
      /// </summary>
      private Task<MergeRequestUpdates> getMergeRequestUpdates(
         TwoListDifference<MergeRequest> diff, WorkflowDetails prevState, WorkflowDetails curState)
      {
         MergeRequestUpdates updates = new MergeRequestUpdates
         {
            NewMergeRequests = diff.SecondOnly,
            UpdatedMergeRequests = new List<MergeRequest>(), // will be filled in below
            ClosedMergeRequests = diff.FirstOnly
         };

         foreach (MergeRequest mergeRequest in diff.Common)
         {
            DateTime? previouslyCachedCommitTimestamp = prevState.CachedCommits.ContainsKey(mergeRequest.Id) ?
               prevState.CachedCommits[mergeRequest.Id] : new Nullable<DateTime>();

            Debug.Assert(curState.CachedCommits.ContainsKey(mergeRequest.Id));

            if (previouslyCachedCommitTimestamp != null)
            {
               DateTime newCachedCommitTimestamp = curState.CachedCommits[mergeRequest.Id];
               if (newCachedCommitTimestamp > previouslyCachedCommitTimestamp)
               {
                  updates.UpdatedMergeRequests.Add(mergeRequest);
               }
            }
         }

         return updates;
      }
   }
}

