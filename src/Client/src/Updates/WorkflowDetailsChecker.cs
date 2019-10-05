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
   public enum UpdateKind
   {
      New,
      Closed,
      CommitsUpdated,
      LabelsUpdated,
      CommitsAndLabelsUpdated
   }

   public struct UpdatedMergeRequest
   {
      public UpdateKind UpdateKind;
      public MergeRequest MergeRequest;
      public string HostName;
      public Project Project;

      public UpdatedMergeRequest(UpdateKind kind, MergeRequest mergeRequest, string hostname, Project project)
      {
         UpdateKind = kind;
         MergeRequest = mergeRequest;
         HostName = hostname;
         Project = project;
      }
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
      internal List<UpdatedMergeRequest> CheckForUpdates(string hostname, List<Project> enabledProjects,
         IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         TwoListDifference<MergeRequestWithProject> diff = getMergeRequestDiff(
            hostname, enabledProjects, oldDetails, newDetails);
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
      private List<UpdatedMergeRequest> getMergeRequestUpdates(string hostname,
         TwoListDifference<MergeRequestWithProject> diff, IWorkflowDetails oldDetails, IWorkflowDetails newDetails)
      {
         List<UpdatedMergeRequest> updates = new List<UpdatedMergeRequest>();

         foreach (MergeRequestWithProject mergeRequest in diff.SecondOnly)
         {
            updates.Add(new UpdatedMergeRequest(
               UpdateKind.New, mergeRequest.MergeRequest, hostname, mergeRequest.Project));
         }

         foreach (MergeRequestWithProject mergeRequest in diff.FirstOnly)
         {
            updates.Add(new UpdatedMergeRequest(
               UpdateKind.Closed, mergeRequest.MergeRequest, hostname, mergeRequest.Project));
         }

         foreach (Tuple<MergeRequestWithProject, MergeRequestWithProject> mrPair in diff.Common)
         {
            Debug.Assert(mrPair.Item1.MergeRequest.Id == mrPair.Item2.MergeRequest.Id);
            MergeRequest mergeRequest = mrPair.Item1.MergeRequest;
            Project project = mrPair.Item1.Project;

            MergeRequestKey mergeRequestKey = new MergeRequestKey(
               hostname, project.Path_With_Namespace, mergeRequest.IId);
            DateTime previouslyCachedChangeTimestamp = oldDetails.GetLatestChangeTimestamp(mergeRequestKey);
            DateTime newCachedChangeTimestamp = newDetails.GetLatestChangeTimestamp(mergeRequestKey);

            bool labelsUpdated = !Enumerable.SequenceEqual(mrPair.Item1.MergeRequest.Labels,
                                                           mrPair.Item2.MergeRequest.Labels);
            bool commitsUpdated = newCachedChangeTimestamp > previouslyCachedChangeTimestamp;

            if (labelsUpdated || commitsUpdated)
            {
               UpdateKind kind = (labelsUpdated && commitsUpdated ? UpdateKind.CommitsAndLabelsUpdated :
                                 (labelsUpdated ? UpdateKind.LabelsUpdated : UpdateKind.CommitsUpdated));
               updates.Add(new UpdatedMergeRequest(kind, mergeRequest, hostname, project));
            }
         }

         return updates;
      }
   }
}

