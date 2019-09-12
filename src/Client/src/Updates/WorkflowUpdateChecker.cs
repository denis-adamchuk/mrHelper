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
      private void CheckForUpdates(List<Project> enabledProjects, WorkflowDetails oldDetails, WorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequest> diff = getMergeRequestDiff(enabledProjects, oldDetails, newDetails);
         return getMergeRequestUpdates(diff, oldDetails, newDetails);
      }

      /// <summary>
      /// Calculate difference between two WorkflowDetails objects
      /// </summary>
      private Task<TwoListDifference<MergeRequest>> getMergeRequestDiff(
         List<Project> enabledProjects, WorkflowDetails oldDetails, WorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequest> diff = new TwoListDifference<MergeRequest>
         {
            FirstOnly = new List<MergeRequest>(),
            SecondOnly = new List<MergeRequest>(),
            Common = new List<MergeRequest>()
         };

         foreach (var project in enabledProjects)
         {
            List<MergeRequest> previouslyCachedMergeRequests =
               oldDetails.MergeRequests.ContainsKey(project.Id) ? oldDetails.MergeRequests[project.Id] : null;

            if (previouslyCachedMergeRequests != null)
            {
               Debug.Assert(newDetails.MergeRequests.ContainsKey(project.Id));

               List<MergeRequest> newCachedMergeRequests = newDetails.MergeRequests[project.Id];
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
         TwoListDifference<MergeRequest> diff, WorkflowDetails oldDetails, WorkflowDetails newDetails)
      {
         MergeRequestUpdates updates = new MergeRequestUpdates
         {
            NewMergeRequests = diff.SecondOnly,
            UpdatedMergeRequests = new List<MergeRequest>(), // will be filled in below
            ClosedMergeRequests = diff.FirstOnly
         };

         foreach (MergeRequest mergeRequest in diff.Common)
         {
            DateTime? previouslyCachedCommitTimestamp = oldDetails.Commits.ContainsKey(mergeRequest.Id) ?
               oldDetails.Commits[mergeRequest.Id] : new Nullable<DateTime>();

            if (previouslyCachedCommitTimestamp != null)
            {
               Debug.Assert(newDetails.Commits.ContainsKey(mergeRequest.Id));

               DateTime newCachedCommitTimestamp = newDetails.Commits[mergeRequest.Id];
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

