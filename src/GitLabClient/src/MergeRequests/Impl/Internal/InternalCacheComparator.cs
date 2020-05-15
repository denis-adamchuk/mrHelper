using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.MergeRequests
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

      private struct MergeRequestWithProject
      {
         public MergeRequestWithProject(MergeRequest mergeRequest, ProjectKey project)
         {
            MergeRequest = mergeRequest;
            Project = project;
         }

         public MergeRequest MergeRequest { get; }
         public ProjectKey Project { get; }
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
               fmk, UserEvents.MergeRequestEvent.Type.NewMergeRequest, null));
         }

         foreach (MergeRequestWithProject mergeRequest in diff.FirstOnly)
         {
            FullMergeRequestKey fmk = new FullMergeRequestKey(mergeRequest.Project, mergeRequest.MergeRequest);

            updates.Add(new UserEvents.MergeRequestEvent(
               fmk, UserEvents.MergeRequestEvent.Type.ClosedMergeRequest, null));
         }

         foreach (Tuple<MergeRequestWithProject, MergeRequestWithProject> mrPair in diff.Common)
         {
            Debug.Assert(mrPair.Item1.MergeRequest.Id == mrPair.Item2.MergeRequest.Id);

            MergeRequestKey mergeRequestKey = new MergeRequestKey(mrPair.Item2.Project, mrPair.Item2.MergeRequest.IId);

            IEnumerable<Version> oldVersions = oldDetails.GetVersions(mergeRequestKey);
            IEnumerable<Version> newVersions = newDetails.GetVersions(mergeRequestKey);

            bool labelsUpdated = !Enumerable.SequenceEqual(mrPair.Item1.MergeRequest.Labels,
                                                           mrPair.Item2.MergeRequest.Labels);
            bool commitsUpdated = newVersions.Count() > oldVersions.Count();

            bool detailsUpdated =
                  mrPair.Item1.MergeRequest.Author.Id     != mrPair.Item2.MergeRequest.Author.Id
               || mrPair.Item1.MergeRequest.Source_Branch != mrPair.Item2.MergeRequest.Source_Branch
               || mrPair.Item1.MergeRequest.Target_Branch != mrPair.Item2.MergeRequest.Target_Branch
               || mrPair.Item1.MergeRequest.Title         != mrPair.Item2.MergeRequest.Title
               || mrPair.Item1.MergeRequest.Description   != mrPair.Item2.MergeRequest.Description;

            if (labelsUpdated || commitsUpdated || detailsUpdated)
            {
               FullMergeRequestKey fmk = new FullMergeRequestKey(
                  mergeRequestKey.ProjectKey, mrPair.Item2.MergeRequest);

               updates.Add(new UserEvents.MergeRequestEvent(
                  fmk, UserEvents.MergeRequestEvent.Type.UpdatedMergeRequest,
                  new UserEvents.MergeRequestEvent.UpdateScope(commitsUpdated, labelsUpdated, detailsUpdated)));
            }
         }

         return updates;
      }
   }
}

