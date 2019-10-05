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
      public string HostName;
      public Project Project;
   }

   public struct NewOrClosedMergeRequest
   {
      public MergeRequest MergeRequest;
      public string HostName;
      public Project Project;
   }

   public struct MergeRequestUpdates
   {
      public string HostName;
      public List<NewOrClosedMergeRequest> NewMergeRequests;
      public List<UpdatedMergeRequest> UpdatedMergeRequests;
      public List<NewOrClosedMergeRequest> ClosedMergeRequests;
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
      internal MergeRequestUpdates CheckForUpdates(string hostname, List<Project> enabledProjects,
         IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequestWithProject> diff = getMergeRequestDiff(hostname, enabledProjects, oldDetails, newDetails);
         return getMergeRequestUpdates(hostname, diff, oldDetails, newDetails);
      }

      /// <summary>
      /// Calculate difference between two WorkflowDetails objects
      /// </summary>
      private TwoListDifference<MergeRequestWithProject> getMergeRequestDiff(string hostname,
         List<Project> enabledProjects, IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequestWithProject> diff = new TwoListDifference<MergeRequestWithProject>
         {
            FirstOnly = new List<MergeRequestWithProject>(),
            SecondOnly = new List<MergeRequestWithProject>(),
            Common = new List<Tuple<MergeRequestWithProject, MergeRequestWithProject>>()
         };

         foreach (var project in enabledProjects)
         {
            ProjectKey key = new ProjectKey{ HostName = hostname, ProjectName = project.Path_With_Namespace };
            List<MergeRequest> previouslyCachedMergeRequests = oldDetails.GetMergeRequests(key);
            List<MergeRequest> newCachedMergeRequests = newDetails.GetMergeRequests(key);

            previouslyCachedMergeRequests.Sort((x, y) => x.Id.CompareTo(y.Id));
            newCachedMergeRequests.Sort((x, y) => x.Id.CompareTo(y.Id));

            int iPrevMR = 0, iNewMR = 0;
            while (iPrevMR < previouslyCachedMergeRequests.Count && iNewMR < newCachedMergeRequests.Count)
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

            while (iPrevMR < previouslyCachedMergeRequests.Count)
            {
               diff.FirstOnly.Add(new MergeRequestWithProject(previouslyCachedMergeRequests[iPrevMR], project));
               ++iPrevMR;
            }

            while (iNewMR < newCachedMergeRequests.Count)
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
      private MergeRequestUpdates getMergeRequestUpdates(string hostname,
         TwoListDifference<MergeRequestWithProject> diff, IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         MergeRequestUpdates updates = new MergeRequestUpdates
         {
            HostName = hostname,
            NewMergeRequests = new List<NewOrClosedMergeRequest>(),
            UpdatedMergeRequests = new List<UpdatedMergeRequest>(),
            ClosedMergeRequests = new List<NewOrClosedMergeRequest>()
         };

         foreach (MergeRequestWithProject mergeRequest in diff.SecondOnly)
         {
            updates.NewMergeRequests.Add(new NewOrClosedMergeRequest
               { MergeRequest = mergeRequest.MergeRequest, HostName = hostname, Project = mergeRequest.Project });
         }

         foreach (MergeRequestWithProject mergeRequest in diff.FirstOnly)
         {
            updates.ClosedMergeRequests.Add(new NewOrClosedMergeRequest
               { MergeRequest = mergeRequest.MergeRequest, HostName = hostname, Project = mergeRequest.Project });
         }

         Func<UpdateKind?, UpdateKind, UpdateKind> updateFlag =
            (old, flag) =>
         {
            return old != null ? (UpdateKind)(old | flag) : flag;
         };

         foreach (Tuple<MergeRequestWithProject, MergeRequestWithProject> mrPair in diff.Common)
         {
            Debug.Assert(mrPair.Item1.MergeRequest.Id == mrPair.Item2.MergeRequest.Id);
            MergeRequest mergeRequest = mrPair.Item1.MergeRequest;
            string projectName = mrPair.Item1.Project.Path_With_Namespace;

            UpdateKind? kind = new Nullable<UpdateKind>();
            if (!Enumerable.SequenceEqual(mrPair.Item1.MergeRequest.Labels, mrPair.Item2.MergeRequest.Labels))
            {
               kind = updateFlag(kind, UpdateKind.LabelsUpdated);
            }

            MergeRequestKey mergeRequestKey = new MergeRequestKey(hostname, projectName, mergeRequest.IId);

            DateTime previouslyCachedChangeTimestamp = oldDetails.GetLatestChangeTimestamp(mergeRequestKey);
            DateTime newCachedChangeTimestamp = newDetails.GetLatestChangeTimestamp(mergeRequestKey);

            if (newCachedChangeTimestamp > previouslyCachedChangeTimestamp)
            {
               kind = updateFlag(kind, UpdateKind.CommitsUpdated);
            }

            if (kind.HasValue)
            {
               updates.UpdatedMergeRequests.Add(new UpdatedMergeRequest
               {
                  UpdateKind = kind.Value,
                  MergeRequest = mergeRequest,
                  HostName = hostname,
                  Project = mrPair.Item1.Project
               });
            }
         }

         return updates;
      }
   }
}

