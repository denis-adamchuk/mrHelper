using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
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
         public Project Project;

         public MergeRequestWithProject(MergeRequest mergeRequest, Project project)
         {
            MergeRequest = mergeRequest;
            Project = project;
         }
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      internal IEnumerable<UserEvents.MergeRequestEvent> CheckForUpdates(string hostname,
         IEnumerable<Project> enabledProjects, IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequestWithProject> diff = getMergeRequestDiff(
            hostname, enabledProjects, oldDetails, newDetails);
         return getMergeRequestUpdates(hostname, diff, oldDetails, newDetails);
      }

      /// <summary>
      /// Calculate difference between two WorkflowDetails objects
      /// </summary>
      private TwoListDifference<MergeRequestWithProject> getMergeRequestDiff(string hostname,
         IEnumerable<Project> enabledProjects, IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequestWithProject> diff = new TwoListDifference<MergeRequestWithProject>
         {
            FirstOnly = new List<MergeRequestWithProject>(),
            SecondOnly = new List<MergeRequestWithProject>(),
            Common = new List<Tuple<MergeRequestWithProject, MergeRequestWithProject>>()
         };

         foreach (Project project in enabledProjects)
         {
            ProjectKey key = new ProjectKey{ HostName = hostname, ProjectName = project.Path_With_Namespace };
            MergeRequest[] previouslyCachedMergeRequests = oldDetails.GetMergeRequests(key).ToArray();
            MergeRequest[] newCachedMergeRequests = newDetails.GetMergeRequests(key).ToArray();

            Array.Sort(previouslyCachedMergeRequests, (x, y) => x.Id.CompareTo(y.Id));
            Array.Sort(newCachedMergeRequests, (x, y) => x.Id.CompareTo(y.Id));

            int iPrevMR = 0, iNewMR = 0;
            while (iPrevMR < previouslyCachedMergeRequests.Count() && iNewMR < newCachedMergeRequests.Count())
            {
               if (previouslyCachedMergeRequests[iPrevMR].Id < newCachedMergeRequests[iNewMR].Id)
               {
                  diff.FirstOnly.Add(new MergeRequestWithProject(previouslyCachedMergeRequests[iPrevMR], project));
                  ++iPrevMR;
               }
               else if (previouslyCachedMergeRequests[iPrevMR].Id == newCachedMergeRequests[iNewMR].Id)
               {
                  diff.Common.Add(new Tuple<MergeRequestWithProject, MergeRequestWithProject>(
                     new MergeRequestWithProject(previouslyCachedMergeRequests[iPrevMR], project),
                     new MergeRequestWithProject(newCachedMergeRequests[iNewMR], project)));
                  ++iPrevMR;
                  ++iNewMR;
               }
               else
               {
                  diff.SecondOnly.Add(new MergeRequestWithProject(newCachedMergeRequests[iNewMR], project));
                  ++iNewMR;
               }
            }

            while (iPrevMR < previouslyCachedMergeRequests.Count())
            {
               diff.FirstOnly.Add(new MergeRequestWithProject(previouslyCachedMergeRequests[iPrevMR], project));
               ++iPrevMR;
            }

            while (iNewMR < newCachedMergeRequests.Count())
            {
               diff.SecondOnly.Add(new MergeRequestWithProject(newCachedMergeRequests[iNewMR], project));
               ++iNewMR;
            }
         }

         return diff;
      }

      /// <summary>
      /// Convert a difference between two states into a list of merge request updates splitted in new/updated/closed
      /// </summary>
      private IEnumerable<UserEvents.MergeRequestEvent> getMergeRequestUpdates(string hostname,
         TwoListDifference<MergeRequestWithProject> diff, IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         List<UserEvents.MergeRequestEvent> updates = new List<UserEvents.MergeRequestEvent>();

         foreach (MergeRequestWithProject mergeRequest in diff.SecondOnly)
         {
            FullMergeRequestKey fmk = new FullMergeRequestKey
            {
               ProjectKey = new ProjectKey
               {
                  HostName = hostname,
                  ProjectName = mergeRequest.Project.Path_With_Namespace
               },
               MergeRequest = mergeRequest.MergeRequest
            };

            MergeRequestKey mergeRequestKey = new MergeRequestKey
            {
               ProjectKey = fmk.ProjectKey,
               IId = fmk.MergeRequest.IId
            };

            updates.Add(new UserEvents.MergeRequestEvent
               {
                  FullMergeRequestKey = fmk,
                  EventType = UserEvents.MergeRequestEvent.Type.NewMergeRequest,
                  NewVersions = newDetails.GetVersions(mergeRequestKey),
                  Scope = null
               });
         }

         foreach (MergeRequestWithProject mergeRequest in diff.FirstOnly)
         {
            FullMergeRequestKey fmk = new FullMergeRequestKey
            {
               ProjectKey = new ProjectKey
               {
                  HostName = hostname,
                  ProjectName = mergeRequest.Project.Path_With_Namespace
               },
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
            MergeRequest mergeRequest = mrPair.Item2.MergeRequest;
            Project project = mrPair.Item2.Project;

            MergeRequestKey mergeRequestKey = new MergeRequestKey
            {
               ProjectKey = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace },
               IId = mergeRequest.IId
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
                  MergeRequest = mergeRequest
               };

               updates.Add(new UserEvents.MergeRequestEvent
                  {
                     FullMergeRequestKey = fmk,
                     EventType = UserEvents.MergeRequestEvent.Type.UpdatedMergeRequest,
                     NewVersions = newVersions,
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

