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

      private struct MergeRequestWithProject
      {
         public MergeRequest MergeRequest;
         public ProjectKey Project;

         public MergeRequestWithProject(MergeRequest mergeRequest, ProjectKey project)
         {
            MergeRequest = mergeRequest;
            Project = project;
         }
      }

      /// <summary>
      /// Compares WorkflowDetails
      /// </summary>
      internal IEnumerable<UserEvents.MergeRequestEvent> CheckForUpdates(
         IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequestWithProject> diff = getMergeRequestDiff(oldDetails, newDetails);
         return getMergeRequestUpdates(diff, oldDetails, newDetails);
      }

      /// <summary>
      /// Calculate difference between two WorkflowDetails objects
      /// </summary>
      private TwoListDifference<MergeRequestWithProject> getMergeRequestDiff(
         IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequestWithProject> diff = new TwoListDifference<MergeRequestWithProject>
         {
            FirstOnly = new List<MergeRequestWithProject>(),
            SecondOnly = new List<MergeRequestWithProject>(),
            Common = new List<Tuple<MergeRequestWithProject, MergeRequestWithProject>>()
         };

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
         TwoListDifference<MergeRequestWithProject> diff, IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         List<UserEvents.MergeRequestEvent> updates = new List<UserEvents.MergeRequestEvent>();

         foreach (MergeRequestWithProject mergeRequest in diff.SecondOnly)
         {
            FullMergeRequestKey fmk = new FullMergeRequestKey
            {
               ProjectKey = mergeRequest.Project,
               MergeRequest = mergeRequest.MergeRequest
            };

            updates.Add(new UserEvents.MergeRequestEvent
               {
                  FullMergeRequestKey = fmk,
                  EventType = UserEvents.MergeRequestEvent.Type.NewMergeRequest,
                  Scope = null
               });
         }

         foreach (MergeRequestWithProject mergeRequest in diff.FirstOnly)
         {
            FullMergeRequestKey fmk = new FullMergeRequestKey
            {
               ProjectKey = mergeRequest.Project,
               MergeRequest = mergeRequest.MergeRequest
            };

            updates.Add(new UserEvents.MergeRequestEvent
               {
                  FullMergeRequestKey = fmk,
                  EventType = UserEvents.MergeRequestEvent.Type.ClosedMergeRequest,
                  Scope = null
               });
         }

         foreach (Tuple<MergeRequestWithProject, MergeRequestWithProject> mrPair in diff.Common)
         {
            Debug.Assert(mrPair.Item1.MergeRequest.Id == mrPair.Item2.MergeRequest.Id);

            MergeRequestKey mergeRequestKey = new MergeRequestKey
            {
               ProjectKey = mrPair.Item2.Project,
               IId = mrPair.Item2.MergeRequest.IId
            };

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
               FullMergeRequestKey fmk = new FullMergeRequestKey
               {
                  ProjectKey = mergeRequestKey.ProjectKey,
                  MergeRequest = mrPair.Item2.MergeRequest
               };

               updates.Add(new UserEvents.MergeRequestEvent
                  {
                     FullMergeRequestKey = fmk,
                     EventType = UserEvents.MergeRequestEvent.Type.UpdatedMergeRequest,
                     Scope = new UserEvents.MergeRequestEvent.UpdateScope
                     {
                        Commits = commitsUpdated,
                        Labels = labelsUpdated,
                        Details = detailsUpdated
                     }
                  });
            }
         }

         return updates;
      }
   }
}

