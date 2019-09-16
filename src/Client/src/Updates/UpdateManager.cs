using System;
using System.ComponentModel;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Manages updates
   /// </summary>
   public class UpdateManager
   {
      public event Action<MergeRequestUpdates, bool> OnUpdate;

      public UpdateManager(Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke,
         UserDefinedSettings settings)
      {
         Settings = settings;
         Workflow = workflow;
         WorkflowDetailsChecker = new WorkflowDetailsChecker();
         ProjectWatcher = new ProjectWatcher();
         Cache = new WorkflowDetailsCache(settings, workflow);
         Cache.OnUpdate += onCacheUpdated;

         Timer.Elapsed += onTimer;
         Timer.SynchronizingObject = synchronizeInvoke;
         Timer.Start();
      }

      async public Task InitializeAsync()
      {
         await Cache.InitializeAsync();
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
         try
         {
            await Cache.UpdateAsync();
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Auto-update failed");
         }
      }

      /// <summary>
      /// Process a notification from Cache
      /// </summary>
      private void onCacheUpdated(IWorkflowDetails oldDetails, IWorkflowDetails newDetails, bool autoupdate)
      {
         string hostname = Workflow.State.HostName;
         List<Project> enabledProjects = Workflow.GetProjectsToUpdate();

         MergeRequestUpdates updates = WorkflowDetailsChecker.CheckForUpdates(hostname,
            enabledProjects, oldDetails, newDetails);
         ProjectWatcher.ProcessUpdates(updates, Workflow.State.HostName, newDetails);

         Trace.TraceInformation(
            String.Format("[UpdateManager] Merge Request Updates: New {0}, Updated {1}, Closed {2}",
               updates.NewMergeRequests.Count, updates.UpdatedMergeRequests.Count, updates.ClosedMergeRequests.Count));

         OnUpdate?.Invoke(updates, autoupdate);
      }

      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = 5 * 60000 // five minutes in ms
         };

      private Workflow.Workflow Workflow { get; }
      private WorkflowDetailsCache Cache { get; }
      private WorkflowDetailsChecker WorkflowDetailsChecker { get; }
      private ProjectWatcher ProjectWatcher { get; }
      private UserDefinedSettings Settings { get; }
   }
}

