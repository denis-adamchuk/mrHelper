using System;
using System.ComponentModel;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;
using System.Diagnostics;
using System.Linq;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Manages updates
   /// </summary>
   public class UpdateManager
   {
      public event Action<MergeRequestUpdates> OnUpdate;

      public UpdateManager(Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         UserDefinedSettings settings)
      {
         Settings = settings;
         Workflow = workflow;
         WorkflowDetailsChecker = new WorkflowDetailsChecker();
         ProjectWatcher = new ProjectWatcher();
         Cache = new WorkflowDetailsCache(settings, workflow);

         Timer.Elapsed += onTimer;
         Timer.SynchronizingObject = synchronizeInvoke;
         Timer.Start();
         Settings.PropertyChanged += (sender, property) =>
         {
            if (property.PropertyName == "LastUsedLabels")
            {
               _cachedLabels = Tools.Tools.SplitLabels(Settings.LastUsedLabels);
               Trace.TraceInformation(String.Format("[UpdateManager] Updated cached Labels to {0}",
                  Settings.LastUsedLabels));
               Trace.TraceInformation("[UpdateManager] Label Filter used: " + (Settings.CheckedLabelsFilter ? "Yes" : "No"));
            }
         };

         Trace.TraceInformation(String.Format("[UpdateManager] Initially cached Labels {0}",
            Settings.LastUsedLabels));
         Trace.TraceInformation("[UpdateManager] Label Filter used: " + (Settings.CheckedLabelsFilter ? "Yes" : "No"));
      }

      public IProjectWatcher GetProjectWatcher()
      {
         return ProjectWatcher;
      }

      public CommitChecker GetCommitChecker(int mergeRequestId)
      {
         return new CommitChecker(mergeRequestId, new WorkflowDetails(Cache.Details));
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      async private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
         WorkflowDetails oldDetails = new WorkflowDetails(Cache.Details);

         try
         {
            await Cache.UpdateAsync();
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Auto-update failed");
            return;
         }

         MergeRequestUpdates updates = WorkflowDetailsChecker.CheckForUpdates(
            enabledProjects, oldDetails, Cache.Details);
         ProjectWatcher.ProcessUpdates(updates, Workflow.State.HostName, Cache.Details);

         Debug.WriteLine(String.Format("[UpdateManager] Found: New: {0}, Updated: {1}, Closed: {2}",
            updates.NewMergeRequests.Count, updates.UpdatedMergeRequests.Count, updates.ClosedMergeRequests.Count));

         Debug.WriteLine("[UpdateManager] Filtering New MR");
         applyLabelFilter(updates.NewMergeRequests, Cache.Details);
         traceUpdates(updates.NewMergeRequests, "Filtered New");

         Debug.WriteLine("[UpdateManager] Filtering Updated MR");
         applyLabelFilter(updates.UpdatedMergeRequests, Cache.Details);
         traceUpdates(updates.UpdatedMergeRequests, "Filtered Updated");

         Debug.WriteLine("[UpdateManager] Filtering Closed MR");
         applyLabelFilter(updates.ClosedMergeRequests, Cache.Details);
         traceUpdates(updates.ClosedMergeRequests, "Filtered Closed");

         Debug.WriteLine(String.Format("[UpdateManager] Filtered : New: {0}, Updated: {1}, Closed: {2}",
            updates.NewMergeRequests.Count, updates.UpdatedMergeRequests.Count, updates.ClosedMergeRequests.Count));

         if (updates.NewMergeRequests.Count > 0
          || updates.UpdatedMergeRequests.Count > 0
          || updates.ClosedMergeRequests.Count > 0)
         {
            OnUpdate?.Invoke(updates);
         }
      }

      /// <summary>
      /// Remove merge requests that don't match Label Filter from the passed list
      /// </summary>
      private void applyLabelFilter(List<MergeRequest> mergeRequests, WorkflowDetails details)
      {
         if (!Settings.CheckedLabelsFilter)
         {
            Debug.WriteLine("[UpdateManager] Label Filter is off");
            return;
         }

         for (int iMergeRequest = mergeRequests.Count - 1; iMergeRequest >= 0; --iMergeRequest)
         {
            MergeRequest mergeRequest = mergeRequests[iMergeRequest];
            if (_cachedLabels.Intersect(mergeRequest.Labels).Count() == 0)
            {
               Debug.WriteLine(String.Format(
                  "[UpdateManager] Merge request {0} from project {1} does not match labels",
                     mergeRequest.Title, details.GetProjectName(mergeRequest.Project_Id)));

               mergeRequests.RemoveAt(iMergeRequest);
            }
         }
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

         Debug.WriteLine(String.Format("[UpdateManager] {0} Merge Requests:", name));

         foreach (MergeRequest mr in mergeRequests)
         {
            Debug.WriteLine(String.Format("[UpdateManager] IId: {0}, Title: {1}", mr.IId, mr.Title));
         }
      }

      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = 5 * 60000 // five minutes in ms
         };

      private List<string> _cachedLabels;

      private Workflow.Workflow Workflow { get; }
      private WorkflowDetailsCache Cache { get; }
      private WorkflowDetailsChecker WorkflowDetailsChecker { get; }
      private ProjectWatcher ProjectWatcher { get; }
      private UserDefinedSettings Settings { get; }
   }
}

