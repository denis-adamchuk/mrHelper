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

      /// <summary>
      /// Checks local cache to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      public IInstantProjectChecker GetLocalProjectChecker(int mergeRequestId)
      {
         return new LocalProjectChecker(mergeRequestId, Cache.Details.Clone());
      }

      /// <summary>
      /// Makes a request to GitLab to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      public IInstantProjectChecker GetRemoteProjectChecker(MergeRequestDescriptor mrd)
      {
         return new RemoteProjectChecker(mrd, new UpdateOperator(Settings));
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      async private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         string hostname = Workflow.State.HostName;
         List<Project> enabledProjects = Workflow.GetProjectsToUpdate();
         IWorkflowDetails oldDetails = Cache.Details.Clone();

         try
         {
            await Cache.UpdateAsync();
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Auto-update failed");
            return;
         }

         MergeRequestUpdates updates = WorkflowDetailsChecker.CheckForUpdates(hostname,
            enabledProjects, oldDetails, Cache.Details);
         ProjectWatcher.ProcessUpdates(updates, Workflow.State.HostName, Cache.Details);

         int unfilteredNewMergeRequestsCount = updates.NewMergeRequests.Count;
         int unfilteredUpdatedMergeRequestsCount = updates.UpdatedMergeRequests.Count;
         int unfilteredClosedMergeRequestCount = updates.ClosedMergeRequests.Count;

         if (Settings.CheckedLabelsFilter)
         {
            applyLabelFilter(updates.NewMergeRequests, Cache.Details);
            applyLabelFilter(updates.UpdatedMergeRequests, Cache.Details);
            applyLabelFilter(updates.ClosedMergeRequests, Cache.Details);
         }

         Trace.TraceInformation(
            String.Format("[UpdateManager] Merge Request Updates: New {0}/{1}, Updated {2}/{3}, Closed {4}/{5}",
               updates.NewMergeRequests.Count, unfilteredNewMergeRequestsCount,
               updates.UpdatedMergeRequests.Count, unfilteredUpdatedMergeRequestsCount,
               updates.ClosedMergeRequests.Count, unfilteredClosedMergeRequestCount));

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
      private void applyLabelFilter(List<MergeRequest> mergeRequests, IWorkflowDetails details)
      {
         for (int iMergeRequest = mergeRequests.Count - 1; iMergeRequest >= 0; --iMergeRequest)
         {
            MergeRequest mergeRequest = mergeRequests[iMergeRequest];
            if (_cachedLabels.Intersect(mergeRequest.Labels).Count() == 0)
            {
               mergeRequests.RemoveAt(iMergeRequest);
            }
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

