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

namespace mrHelper.Client.Updates
{
   public struct MergeRequestUpdates
   {
      public List<MergeRequest> NewMergeRequests;
      public List<MergeRequest> UpdatedMergeRequests;
      public List<MergeRequest> ClosedMergeRequests;
   }

   /// <summary>
   /// Checks WorkflowDetails for updates
   /// </summary>
   internal class WorkflowDetailsChecker
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
      internal MergeRequestUpdates CheckForUpdates(string hostname, List<Project> enabledProjects,
         IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequest> diff = getMergeRequestDiff(hostname, enabledProjects, oldDetails, newDetails);
         return getMergeRequestUpdates(diff, oldDetails, newDetails);
      }

      /// <summary>
      /// Calculate difference between two WorkflowDetails objects
      /// </summary>
      private TwoListDifference<MergeRequest> getMergeRequestDiff(string hostname,
         List<Project> enabledProjects, IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequest> diff = new TwoListDifference<MergeRequest>
         {
            FirstOnly = new List<MergeRequest>(),
            SecondOnly = new List<MergeRequest>(),
            Common = new List<MergeRequest>()
         };

         foreach (var project in enabledProjects)
         {
            ProjectKey key = new ProjectKey{ HostName = hostname, ProjectId = project.Id };
            List<MergeRequest> previouslyCachedMergeRequests = oldDetails.GetMergeRequests(key);
            List<MergeRequest> newCachedMergeRequests = newDetails.GetMergeRequests(key);

            diff.FirstOnly.AddRange(previouslyCachedMergeRequests.Except(newCachedMergeRequests).ToList());
            diff.SecondOnly.AddRange(newCachedMergeRequests.Except(previouslyCachedMergeRequests).ToList());
            diff.Common.AddRange(previouslyCachedMergeRequests.Intersect(newCachedMergeRequests).ToList());
         }

         return diff;
      }

      /// <summary>
      /// Convert a difference between two states into a list of merge request updates splitted in new/updated/closed
      /// </summary>
      private MergeRequestUpdates getMergeRequestUpdates(
         TwoListDifference<MergeRequest> diff, IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         MergeRequestUpdates updates = new MergeRequestUpdates
         {
            NewMergeRequests = diff.SecondOnly,
            UpdatedMergeRequests = new List<MergeRequest>(), // will be filled in below
            ClosedMergeRequests = diff.FirstOnly
         };

         foreach (MergeRequest mergeRequest in diff.Common)
         {
            DateTime previouslyCachedChangeTimestamp = oldDetails.GetLatestChangeTimestamp(mergeRequest.Id);
            DateTime newCachedChangeTimestamp = newDetails.GetLatestChangeTimestamp(mergeRequest.Id);

            if (newCachedChangeTimestamp > previouslyCachedChangeTimestamp)
            {
               updates.UpdatedMergeRequests.Add(mergeRequest);
            }
         }

         return updates;
      }
   }
}

