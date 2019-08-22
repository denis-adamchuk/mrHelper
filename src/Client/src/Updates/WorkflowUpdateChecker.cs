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
   public class WorkflowUpdateChecker : IProjectWatcher
   {
      internal WorkflowUpdateChecker(UserDefinedSettings settings, UpdateOperator updateOperator,
         Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke)
      {
         Settings = settings;
         Settings.PropertyChanged += (sender, property) =>
         {
            if (property.PropertyName == "LastUsedLabels")
            {
               _cachedLabels = Tools.Tools.SplitLabels(Settings.LastUsedLabels);
               Trace.TraceInformation(String.Format("[WorkflowUpdateChecker] Updated cached Labels to {0}",
                  Settings.LastUsedLabels));
               Trace.TraceInformation("[WorkflowUpdateChecker] Label Filter used: " + (Settings.CheckedLabelsFilter ? "Yes" : "No"));
            }
         };

         _cachedLabels = Tools.Tools.SplitLabels(Settings.LastUsedLabels);
         Trace.TraceInformation(String.Format("[WorkflowUpdateChecker] Initially cached Labels {0}",
            Settings.LastUsedLabels));
         Trace.TraceInformation("[WorkflowUpdateChecker] Label Filter used: " + (Settings.CheckedLabelsFilter ? "Yes" : "No"));

         Timer.Elapsed += onTimer;
         Timer.SynchronizingObject = synchronizeInvoke;
         Timer.Start();

         UpdateOperator = updateOperator;
         Workflow = workflow;
      }

      public event EventHandler<MergeRequestUpdates> OnUpdate;
      public event EventHandler<List<ProjectUpdate>> OnProjectUpdate;

      private struct TwoListDifference<T>
      {
         public List<T> FirstOnly;
         public List<T> SecondOnly;
         public List<T> Common;
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      async void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         MergeRequestUpdates updates = new MergeRequestUpdates
         {
            NewMergeRequests = new List<MergeRequest>(),
            UpdatedMergeRequests = new List<MergeRequest>(),
            ClosedMergeRequests = new List<MergeRequest>()
         };

         try
         {
            TwoListDifference<MergeRequest> diff = await getMergeRequestDiffAsync();
            updates = await getMergeRequestUpdatesAsync(diff);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Auto-update failed");
            return;
         }

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Found: New: {0}, Updated: {1}, Closed: {2}",
            updates.NewMergeRequests.Count, updates.UpdatedMergeRequests.Count, updates.ClosedMergeRequests.Count));

         // Need to gather projects before we filter out some MRs
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();
         projectUpdates.AddRange(getProjectUpdates(updates.NewMergeRequests));
         projectUpdates.AddRange(getProjectUpdates(updates.UpdatedMergeRequests));

         Debug.WriteLine("[WorkflowUpdateChecker] Filtering New MR");
         applyLabelFilter(updates.NewMergeRequests);
         traceUpdates(updates.NewMergeRequests, "Filtered New");

         Debug.WriteLine("[WorkflowUpdateChecker] Filtering Updated MR");
         applyLabelFilter(updates.UpdatedMergeRequests);
         traceUpdates(updates.UpdatedMergeRequests, "Filtered Updated");

         Debug.WriteLine("[WorkflowUpdateChecker] Filtering Closed MR");
         applyLabelFilter(updates.ClosedMergeRequests);
         traceUpdates(updates.ClosedMergeRequests, "Filtered Closed");

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Filtered : New: {0}, Updated: {1}, Closed: {2}",
            updates.NewMergeRequests.Count, updates.UpdatedMergeRequests.Count, updates.ClosedMergeRequests.Count));

         if (updates.NewMergeRequests.Count > 0
          || updates.UpdatedMergeRequests.Count > 0
          || updates.ClosedMergeRequests.Count > 0)
         {
            OnUpdate?.Invoke(this, updates);
         }

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Updating {0} projects", projectUpdates.Count));
         if (projectUpdates.Count > 0)
         {
            OnProjectUpdate?.Invoke(this, projectUpdates);
         }
      }

      /// <summary>
      /// Remove merge requests that don't match Label Filter from the passed list
      /// </summary>
      private void applyLabelFilter(List<MergeRequest> mergeRequests)
      {
         if (!Settings.CheckedLabelsFilter)
         {
            Debug.WriteLine("[WorkflowUpdateChecker] Label Filter is off");
            return;
         }

         for (int iMergeRequest = mergeRequests.Count - 1; iMergeRequest >= 0; --iMergeRequest)
         {
            MergeRequest mergeRequest = mergeRequests[iMergeRequest];
            if (_cachedLabels.Intersect(mergeRequest.Labels).Count() == 0)
            {
               Debug.WriteLine(String.Format(
                  "[WorkflowUpdateChecker] Merge request {0} from project {1} does not match labels",
                     mergeRequest.Title, getMergeRequestProjectName(mergeRequest)));

               mergeRequests.RemoveAt(iMergeRequest);
            }
         }
      }

      /// <summary>
      /// Calculate difference between current list of merge requests at GitLab and current list in the Workflow
      /// </summary>
      async private Task<TwoListDifference<MergeRequest>> getMergeRequestDiffAsync()
      {
         TwoListDifference<MergeRequest> diff = new TwoListDifference<MergeRequest>
         {
            FirstOnly = new List<MergeRequest>(),
            SecondOnly = new List<MergeRequest>(),
            Common = new List<MergeRequest>()
         };

         if (Workflow.State.HostName == null)
         {
            Debug.WriteLine("[WorkflowUpdateChecker] Host name is null");
            return diff;
         }

         List<Project> projectsToCheck = Tools.Tools.LoadProjectsFromFile(Workflow.State.HostName);
         if (projectsToCheck == null && Workflow.State.Project.Path_With_Namespace != null)
         {
            projectsToCheck = new List<Project>
            {
               Workflow.State.Project
            };
         }

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Checking {0} projects",
            (projectsToCheck?.Count ?? 0)));

         if (projectsToCheck == null)
         {
            return diff;
         }

         foreach (var project in projectsToCheck)
         {
            Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Checking merge requests for project {0}",
               project.Path_With_Namespace));

            List<MergeRequest> mergeRequests =
               await UpdateOperator.GetMergeRequests(Workflow.State.HostName, project.Path_With_Namespace);

            Debug.WriteLine(String.Format("[WorkflowUpdateChecker] This project has {0} merge requests at GitLab",
               (mergeRequests?.Count ?? 0)));

            if (mergeRequests == null)
            {
               continue;
            }

            if (_cachedMergeRequests.ContainsKey(project.Path_With_Namespace))
            {
               List<MergeRequest> cachedMergeRequests = _cachedMergeRequests[project.Path_With_Namespace];

               Debug.WriteLine(String.Format("[WorkflowUpdateChecker] {0} merge requests for this project were cached before",
                  cachedMergeRequests.Count));

               diff.FirstOnly.AddRange(cachedMergeRequests.Except(mergeRequests).ToList());
               diff.SecondOnly.AddRange(mergeRequests.Except(cachedMergeRequests).ToList());
               diff.Common.AddRange(cachedMergeRequests.Intersect(mergeRequests).ToList());
            }

            Debug.WriteLine("[WorkflowUpdateChecker] Caching merge requests for this project");

            _cachedMergeRequests[project.Path_With_Namespace] = mergeRequests;
         }

         return diff;
      }

      /// <summary>
      /// Convert a difference between two states into a list of merge request updates splitted in new/updated/closed
      /// </summary>
      async private Task<MergeRequestUpdates> getMergeRequestUpdatesAsync(TwoListDifference<MergeRequest> diff)
      {
         MergeRequestUpdates updates = new MergeRequestUpdates
         {
            NewMergeRequests = diff.SecondOnly,
            UpdatedMergeRequests = new List<MergeRequest>(), // will be filled in below
            ClosedMergeRequests = diff.FirstOnly
         };

         foreach (MergeRequest mergeRequest in diff.Common)
         {
            Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Checking commits for merge request {0} from project {1}",
               mergeRequest.IId, getMergeRequestProjectName(mergeRequest)));

            List<Commit> commits = await UpdateOperator.GetCommits(
               new MergeRequestDescriptor
               {
                  HostName = Workflow.State.HostName,
                  ProjectName = getMergeRequestProjectName(mergeRequest),
                  IId = mergeRequest.IId
               });

            Debug.WriteLine(String.Format("[WorkflowUpdateChecker] This merge request has {0} commits at GitLab",
               (commits?.Count ?? 0)));

            if (commits == null)
            {
               continue;
            }

            if (_cachedCommits.ContainsKey(mergeRequest.IId))
            {
               Debug.WriteLine(String.Format("[WorkflowUpdateChecker] {0} Commits for this merge request were cached before",
                  _cachedCommits[mergeRequest.IId].Count));

               if (commits.Count > _cachedCommits[mergeRequest.IId].Count)
               {
                  updates.UpdatedMergeRequests.Add(mergeRequest);
               }
            }

            Debug.WriteLine(String.Format("[WorkflowUpdateChecker] Caching commits for this merge request"));

            _cachedCommits[mergeRequest.IId] = commits;
         }

         foreach (MergeRequest mergeRequest in updates.ClosedMergeRequests)
         {
            Debug.WriteLine(String.Format(
               "[WorkflowUpdateChecker] Removing commits of merge request {0} from project {1} from cache",
                  mergeRequest.IId, getMergeRequestProjectName(mergeRequest)));

            _cachedCommits.Remove(mergeRequest.IId);
         }

         return updates;
      }

      /// <summary>
      /// Convert a list of Project Id to list of Project names
      /// </summary>
      private List<ProjectUpdate> getProjectUpdates(List<MergeRequest> mergeRequests)
      {
         List<ProjectUpdate> projectUpdates = new List<ProjectUpdate>();

         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            projectUpdates.Add(
               new ProjectUpdate
               {
                  HostName = Workflow.State.HostName,
                  ProjectName = getMergeRequestProjectName(mergeRequest)
               });
         }

         return projectUpdates;
      }

      /// <summary>
      /// Find a project name for a passed merge request
      /// </summary>
      private string getMergeRequestProjectName(MergeRequest mergeRequest)
      {
         foreach (Project project in Workflow.State.Projects)
         {
            if (project.Id == mergeRequest.Project_Id)
            {
               return project.Path_With_Namespace;
            }
         }

         return String.Empty;
      }

      /// <summary>
      /// Debug trace
      /// </summary>
      private void traceUpdates(List<MergeRequest> mergeRequests, string name)
      {
         if (mergeRequests.Count == 0)
         {
            return;
         }

         Debug.WriteLine(String.Format("[WorkflowUpdateChecker] {0} Merge Requests:", name));

         foreach (MergeRequest mr in mergeRequests)
         {
            Debug.WriteLine(String.Format("[WorkflowUpdateChecker] IId: {0}, Title: {1}", mr.IId, mr.Title));
         }
      }

      private Workflow.Workflow Workflow { get; }
      private UpdateOperator UpdateOperator { get; }
      private UserDefinedSettings Settings { get; }

      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = 60000 // ms
         };

      private List<string> _cachedLabels;
      private Dictionary<string, List<MergeRequest>> _cachedMergeRequests = new Dictionary<string, List<MergeRequest>>();
      private Dictionary<int, List<Commit>> _cachedCommits = new Dictionary<int, List<Commit>>();
   }
}

