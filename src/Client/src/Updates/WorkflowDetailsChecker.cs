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
   [Flags]
   public enum UpdateKind
   {
      CommitsUpdated = 1,
      LabelsUpdated  = 2
   }

   public struct UpdatedMergeRequest
   {
      public UpdateKind UpdateKind;
      public MergeRequest MergeRequest;
   }

   public struct MergeRequestUpdates
   {
      public string HostName;
      public List<MergeRequest> NewMergeRequests;
      public List<UpdatedMergeRequest> UpdatedMergeRequests;
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
         public List<Tuple<T, T>> Common;
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      internal MergeRequestUpdates CheckForUpdates(string hostname, List<Project> enabledProjects,
         IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequest> diff = getMergeRequestDiff(hostname, enabledProjects, oldDetails, newDetails);
         return getMergeRequestUpdates(hostname, diff, oldDetails, newDetails);
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
            Common = new List<Tuple<MergeRequest, MergeRequest>>()
         };

         foreach (var project in enabledProjects)
         {
            ProjectKey key = new ProjectKey{ HostName = hostname, ProjectId = project.Id };
            List<MergeRequest> previouslyCachedMergeRequests = oldDetails.GetMergeRequests(key);
            List<MergeRequest> newCachedMergeRequests = newDetails.GetMergeRequests(key);

            previouslyCachedMergeRequests.Sort((x, y) => x.Id.CompareTo(y.Id));
            newCachedMergeRequests.Sort((x, y) => x.Id.CompareTo(y.Id));

            int iPrevMR = 0, iNewMR = 0;
            while (iPrevMR < previouslyCachedMergeRequests.Count && iNewMR < newCachedMergeRequests.Count)
            {
               if (previouslyCachedMergeRequests[iPrevMR].Id < newCachedMergeRequests[iNewMR].Id)
               {
                  diff.FirstOnly.Add(previouslyCachedMergeRequests[iPrevMR]);
                  ++iPrevMR;
               }
               else if (previouslyCachedMergeRequests[iPrevMR].Id == newCachedMergeRequests[iNewMR].Id)
               {
                  diff.Common.Add(new Tuple<MergeRequest, MergeRequest>(
                     previouslyCachedMergeRequests[iPrevMR], newCachedMergeRequests[iNewMR]));
                  ++iPrevMR;
                  ++iNewMR;
               }
               else
               {
                  diff.SecondOnly.Add(newCachedMergeRequests[iNewMR]);
                  ++iNewMR;
               }
            }

            while (iPrevMR < previouslyCachedMergeRequests.Count)
            {
               diff.FirstOnly.Add(previouslyCachedMergeRequests[iPrevMR]);
               ++iPrevMR;
            }

            while (iNewMR < newCachedMergeRequests.Count)
            {
               diff.SecondOnly.Add(newCachedMergeRequests[iNewMR]);
               ++iNewMR;
            }
         }

         return diff;
      }

      /// <summary>
      /// Convert a difference between two states into a list of merge request updates splitted in new/updated/closed
      /// </summary>
      private MergeRequestUpdates getMergeRequestUpdates(string hostname,
         TwoListDifference<MergeRequest> diff, IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         MergeRequestUpdates updates = new MergeRequestUpdates
         {
            HostName = hostname,
            NewMergeRequests = diff.SecondOnly,
            UpdatedMergeRequests = new List<UpdatedMergeRequest>(), // will be filled in below
            ClosedMergeRequests = diff.FirstOnly
         };

         Func<UpdateKind?, UpdateKind, UpdateKind> updateFlag =
            (old, flag) =>
         {
            return old != null ? (UpdateKind)(old | flag) : flag;
         };

         foreach (Tuple<MergeRequest, MergeRequest> mrPair in diff.Common)
         {
            Debug.Assert(mrPair.Item1.Id == mrPair.Item2.Id);

            UpdateKind? kind = new Nullable<UpdateKind>();
            if (!Enumerable.SequenceEqual(mrPair.Item1.Labels, mrPair.Item2.Labels))
            {
               kind = updateFlag(kind, UpdateKind.LabelsUpdated);
            }

            DateTime previouslyCachedChangeTimestamp = oldDetails.GetLatestChangeTimestamp(mrPair.Item1.Id);
            DateTime newCachedChangeTimestamp = newDetails.GetLatestChangeTimestamp(mrPair.Item2.Id);

            if (newCachedChangeTimestamp > previouslyCachedChangeTimestamp)
            {
               kind = updateFlag(kind, UpdateKind.CommitsUpdated);
            }

            if (kind.HasValue)
            {
               updates.UpdatedMergeRequests.Add(new UpdatedMergeRequest
               {
                  UpdateKind = kind.Value,
                  MergeRequest = mrPair.Item1
               });
            }
         }

         return updates;
      }
   }
}

