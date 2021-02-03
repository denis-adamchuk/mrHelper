using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.GitLabClient.Loaders.Cache
{
   /// <summary>
   /// Checks IInternalCache for updates
   /// </summary>
   internal class InternalMergeRequestCacheComparator
   {
      private struct TwoListDifference<T>
      {
         public TwoListDifference(List<T> firstOnly, List<T> secondOnly, List<Tuple<T, T>> common)
         {
            FirstOnly = firstOnly;
            SecondOnly = secondOnly;
            Common = common;
         }

         public List<T> FirstOnly { get; }
         public List<T> SecondOnly { get; }
         public List<Tuple<T, T>> Common { get; }
      }

      private struct MergeRequestWithProject : IEquatable<MergeRequestWithProject>
      {
         public MergeRequestWithProject(MergeRequest mergeRequest, ProjectKey project)
         {
            MergeRequest = mergeRequest;
            Project = project;
         }

         public MergeRequest MergeRequest { get; }
         public ProjectKey Project { get; }

         public override bool Equals(object obj)
         {
            return obj is MergeRequestWithProject project && Equals(project);
         }

         public bool Equals(MergeRequestWithProject other)
         {
            return Project.Equals(other.Project) && MergeRequest.IId == other.MergeRequest.IId;
         }

         public override int GetHashCode()
         {
            int hashCode = -1994398954;
            hashCode = hashCode * -1521134295 + Project.GetHashCode();
            hashCode = hashCode * -1521134295 + MergeRequest.IId.GetHashCode();
            return hashCode;
         }
      }

      /// <summary>
      /// Compares IInternalCache objects
      /// </summary>
      internal IEnumerable<UserEvents.MergeRequestEvent> CheckForUpdates(
         IInternalCache oldDetails, IInternalCache newDetails)
      {
         TwoListDifference<MergeRequestWithProject> diff = getMergeRequestDiff(oldDetails, newDetails);
         return getMergeRequestUpdates(diff, oldDetails, newDetails);
      }

      /// <summary>
      /// Calculate difference between two IInternalCache objects
      /// </summary>
      private TwoListDifference<MergeRequestWithProject> getMergeRequestDiff(
         IInternalCache oldDetails, IInternalCache newDetails)
      {
         TwoListDifference<MergeRequestWithProject> diff = new TwoListDifference<MergeRequestWithProject>
         (
            new List<MergeRequestWithProject>(),
            new List<MergeRequestWithProject>(),
            new List<Tuple<MergeRequestWithProject, MergeRequestWithProject>>()
         );

         HashSet<ProjectKey> projectKeys = oldDetails.GetProjects().Concat(newDetails.GetProjects()).ToHashSet();

         foreach (ProjectKey projectKey in projectKeys)
         {
            MergeRequest[] previouslyCachedMergeRequests = oldDetails.GetMergeRequests(projectKey).ToArray();
            MergeRequest[] newCachedMergeRequests = newDetails.GetMergeRequests(projectKey).ToArray();

            Array.Sort(previouslyCachedMergeRequests, (x, y) => x.Id.CompareTo(y.Id));
            Array.Sort(newCachedMergeRequests, (x, y) => x.Id.CompareTo(y.Id));

            int iPrevMR = 0, iNewMR = 0;
            while (iPrevMR < previouslyCachedMergeRequests.Count() && iNewMR < newCachedMergeRequests.Count())
            {
               if (previouslyCachedMergeRequests[iPrevMR].Id < newCachedMergeRequests[iNewMR].Id)
               {
                  diff.FirstOnly.Add(new MergeRequestWithProject(previouslyCachedMergeRequests[iPrevMR], projectKey));
                  ++iPrevMR;
               }
               else if (previouslyCachedMergeRequests[iPrevMR].Id == newCachedMergeRequests[iNewMR].Id)
               {
                  diff.Common.Add(new Tuple<MergeRequestWithProject, MergeRequestWithProject>(
                     new MergeRequestWithProject(previouslyCachedMergeRequests[iPrevMR], projectKey),
                     new MergeRequestWithProject(newCachedMergeRequests[iNewMR], projectKey)));
                  ++iPrevMR;
                  ++iNewMR;
               }
               else
               {
                  diff.SecondOnly.Add(new MergeRequestWithProject(newCachedMergeRequests[iNewMR], projectKey));
                  ++iNewMR;
               }
            }

            while (iPrevMR < previouslyCachedMergeRequests.Count())
            {
               diff.FirstOnly.Add(new MergeRequestWithProject(previouslyCachedMergeRequests[iPrevMR], projectKey));
               ++iPrevMR;
            }

            while (iNewMR < newCachedMergeRequests.Count())
            {
               diff.SecondOnly.Add(new MergeRequestWithProject(newCachedMergeRequests[iNewMR], projectKey));
               ++iNewMR;
            }
         }

         return diff;
      }

      /// <summary>
      /// Convert a difference between two states into a list of merge request updates splitted in new/updated/closed
      /// </summary>
      private IEnumerable<UserEvents.MergeRequestEvent> getMergeRequestUpdates(
         TwoListDifference<MergeRequestWithProject> diff, IInternalCache oldDetails, IInternalCache newDetails)
      {
         List<UserEvents.MergeRequestEvent> updates = new List<UserEvents.MergeRequestEvent>();

         foreach (MergeRequestWithProject mergeRequest in diff.SecondOnly)
         {
            FullMergeRequestKey fmk = new FullMergeRequestKey(mergeRequest.Project, mergeRequest.MergeRequest);

            updates.Add(new UserEvents.MergeRequestEvent(
               fmk, UserEvents.MergeRequestEvent.Type.AddedMergeRequest, null));
         }

         foreach (MergeRequestWithProject mergeRequest in diff.FirstOnly)
         {
            FullMergeRequestKey fmk = new FullMergeRequestKey(mergeRequest.Project, mergeRequest.MergeRequest);

            updates.Add(new UserEvents.MergeRequestEvent(
               fmk, UserEvents.MergeRequestEvent.Type.RemovedMergeRequest, null));
         }

         foreach (Tuple<MergeRequestWithProject, MergeRequestWithProject> mrPair in diff.Common)
         {
            MergeRequest mergeRequest1 = mrPair.Item1.MergeRequest;
            MergeRequest mergeRequest2 = mrPair.Item2.MergeRequest;
            Debug.Assert(mergeRequest1.Id == mergeRequest2.Id);

            MergeRequestKey mergeRequestKey = new MergeRequestKey(mrPair.Item2.Project, mergeRequest2.IId);

            IEnumerable<Version> oldVersions = oldDetails.GetVersions(mergeRequestKey);
            IEnumerable<Version> newVersions = newDetails.GetVersions(mergeRequestKey);

            bool labelsUpdated = !Enumerable.SequenceEqual(mergeRequest1.Labels, mergeRequest2.Labels);
            bool commitsUpdated = newVersions.Count() > oldVersions.Count();

            MergeRequestApprovalConfiguration oldApprovals = oldDetails.GetApprovals(mergeRequestKey);
            MergeRequestApprovalConfiguration newApprovals = newDetails.GetApprovals(mergeRequestKey);
            bool detailsUpdated = areMergeRequestsDifferentInDetails(mergeRequest1, mergeRequest2)
                               || areApprovalsDifferent(oldApprovals, newApprovals);

            if (labelsUpdated || commitsUpdated || detailsUpdated)
            {
               FullMergeRequestKey fmk = new FullMergeRequestKey(
                  mergeRequestKey.ProjectKey, mergeRequest2);

               updates.Add(new UserEvents.MergeRequestEvent(
                  fmk, UserEvents.MergeRequestEvent.Type.UpdatedMergeRequest,
                  new UserEvents.MergeRequestEvent.UpdateScope(commitsUpdated, labelsUpdated, detailsUpdated)));
            }
         }

         return updates;
      }

      private bool areMergeRequestsDifferentInDetails(MergeRequest mergeRequest1, MergeRequest mergeRequest2)
      {
         return
             mergeRequest1.Id != mergeRequest2.Id
          || mergeRequest1.IId != mergeRequest2.IId
          || mergeRequest1.Title != mergeRequest2.Title
          || mergeRequest1.Description != mergeRequest2.Description
          || mergeRequest1.Source_Branch != mergeRequest2.Source_Branch
          || mergeRequest1.Target_Branch != mergeRequest2.Target_Branch
          || mergeRequest1.State != mergeRequest2.State
          || mergeRequest1.Web_Url != mergeRequest2.Web_Url
          || mergeRequest1.Work_In_Progress != mergeRequest2.Work_In_Progress
          || mergeRequest1.Project_Id != mergeRequest2.Project_Id
          || mergeRequest1.Squash != mergeRequest2.Squash
          || mergeRequest1.Should_Remove_Source_Branch != mergeRequest2.Should_Remove_Source_Branch
          || mergeRequest1.Force_Remove_Source_Branch != mergeRequest2.Force_Remove_Source_Branch
          || (mergeRequest1.Author?.Id ?? 0) != (mergeRequest2.Author?.Id ?? 0)
          || (mergeRequest1.Assignee?.Id ?? 0) != (mergeRequest2.Assignee?.Id ?? 0)
          || mergeRequest1.Merge_Status != mergeRequest2.Merge_Status
          || mergeRequest1.Rebase_In_Progress != mergeRequest2.Rebase_In_Progress
          || mergeRequest1.Has_Conflicts != mergeRequest2.Has_Conflicts
          || mergeRequest1.Blocking_Discussions_Resolved != mergeRequest2.Blocking_Discussions_Resolved;
      }

      private bool areApprovalsDifferent(MergeRequestApprovalConfiguration approvals1,
                                         MergeRequestApprovalConfiguration approvals2)
      {
         if (approvals1 == null && approvals2 != null)
         {
            return true;
         }
         else if (approvals1 != null && approvals2 == null)
         {
            return true;
         }
         else if (approvals1 == approvals2)
         {
            return false;
         }

         if (approvals1.Approved_By == null && approvals2.Approved_By != null)
         {
            return true;
         }
         else if (approvals1.Approved_By != null && approvals2.Approved_By == null)
         {
            return true;
         }
         else if (approvals1.Approved_By != null && approvals2.Approved_By != null)
         {
            var userIds1 = approvals1.Approved_By.Select(user => user.User.Id);
            var userIds2 = approvals2.Approved_By.Select(user => user.User.Id);
            if (!Enumerable.SequenceEqual(userIds1, userIds2))
            {
               return true;
            }
         }

         return approvals1.Id != approvals2.Id
             || approvals1.Project_Id != approvals2.Project_Id
             || approvals1.Title != approvals2.Title
             || approvals1.Description != approvals2.Description
             || approvals1.State != approvals2.State
             || approvals1.Created_At != approvals2.Created_At
             || approvals1.Updated_At != approvals2.Updated_At
             || approvals1.Merge_Status != approvals2.Merge_Status
             || approvals1.Approvals_Required != approvals2.Approvals_Required
             || approvals1.Approvals_Left != approvals2.Approvals_Left;
      }
   }
}

