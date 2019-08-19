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
   }

   /// <summary>
   /// Implements periodic checks for updates of Merge Requests and their Versions
   /// </summary>
   public class WorkflowUpdateChecker
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
            }
         };
         _cachedLabels = Tools.Tools.SplitLabels(Settings.LastUsedLabels);

         Timer.Elapsed += onTimer;
         Timer.SynchronizingObject = synchronizeInvoke;
         Timer.Start();

         UpdateOperator = updateOperator;
         Workflow = workflow;
      }

      public event EventHandler<MergeRequestUpdates> OnUpdate;

      async void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         Debug.WriteLine("WorkflowUpdateChecker.onTimer -- begin");

         MergeRequestUpdates updates = new MergeRequestUpdates();
         try
         {
            updates = await getUpdatesAsync(_lastCheckTimeStamp);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Auto-update failed");
         }
         finally
         {
            _lastCheckTimeStamp = DateTime.Now;
            Debug.WriteLine(String.Format("WorkflowUpdateChecker.onTimer -- timestamp updated to {0}",
               _lastCheckTimeStamp.ToLocalTime().ToString()));
         }

         Debug.WriteLine(String.Format("WorkflowUpdateChecker.onTimer -- New: {0}, Updated: {1}",
            updates.NewMergeRequests.Count, updates.UpdatedMergeRequests.Count));

         if (updates.NewMergeRequests.Count > 0 || updates.UpdatedMergeRequests.Count > 0)
         {
            OnUpdate?.Invoke(this, updates);
         }

         Debug.WriteLine("WorkflowUpdateChecker.onTimer -- end");
      }

      /// <summary>
      /// Collects requests that have been created or updated later than timestamp.
      /// By 'updated' we mean that 'merge request has a version with a timestamp later than ...'.
      /// Includes only those merge requests that match Labels filters.
      /// </summary>
      async private Task<MergeRequestUpdates> getUpdatesAsync(DateTime timestamp)
      {
         Debug.WriteLine(String.Format("WorkflowUpdateChecker.getUpdatesAsync -- begin -- timestamp {0}",
            timestamp.ToLocalTime().ToString()));

         MergeRequestUpdates updates = new MergeRequestUpdates
         {
            NewMergeRequests = new List<MergeRequest>(),
            UpdatedMergeRequests = new List<MergeRequest>()
         };

         if (Workflow.State.HostName == null)
         {
            return updates;
         }

         List<Project> projectsToCheck = Tools.Tools.LoadProjectsFromFile(Workflow.State.HostName);
         if (projectsToCheck == null && Workflow.State.Project.Path_With_Namespace != null)
         {
            projectsToCheck = new List<Project>();
            projectsToCheck.Add(Workflow.State.Project);
         }

         if (projectsToCheck == null)
         {
            return updates;
         }

         Debug.WriteLine(String.Format("WorkflowUpdateChecker.getUpdatesAsync -- checking {0} projects",
            projectsToCheck.Count));

         foreach (var project in projectsToCheck)
         {
            Debug.WriteLine(String.Format("WorkflowUpdateChecker.getUpdatesAsync -- checking project {0}",
               project.Path_With_Namespace));

            List<MergeRequest> mergeRequests =
               await UpdateOperator.GetMergeRequests(Workflow.State.HostName, project.Path_With_Namespace);
            if (mergeRequests == null)
            {
               continue;
            }

            Debug.WriteLine(String.Format("WorkflowUpdateChecker.getUpdatesAsync -- project {0} has {1} merge requests",
               project.Path_With_Namespace, mergeRequests.Count));

            foreach (var mergeRequest in mergeRequests)
            {
               if (Settings.CheckedLabelsFilter && _cachedLabels.Intersect(mergeRequest.Labels).Count() == 0)
               {
                  continue;
               }

               Debug.WriteLine(String.Format("WorkflowUpdateChecker.getUpdatesAsync -- merge request {0} matches our Labels",
                  mergeRequest.Title));

               if (mergeRequest.Created_At.ToLocalTime() > timestamp)
               {
                  Debug.WriteLine(String.Format("WorkflowUpdateChecker.getUpdatesAsync -- this merge request is new (created_at = {0})",
                     mergeRequest.Created_At.ToLocalTime().ToString()));

                  updates.NewMergeRequests.Add(mergeRequest);
               }
               else if (mergeRequest.Updated_At.ToLocalTime() > timestamp)
               {
                  Debug.WriteLine(String.Format("WorkflowUpdateChecker.getUpdatesAsync -- this merge request is updated (updated_at = {0})",
                     mergeRequest.Updated_At.ToLocalTime().ToString()));

                  List<GitLabSharp.Entities.Version> versions = await UpdateOperator.GetVersions(
                     new MergeRequestDescriptor
                     {
                        HostName = Workflow.State.HostName,
                        ProjectName = project.Path_With_Namespace,
                        IId = mergeRequest.IId
                     });
                  if (versions == null || versions.Count == 0)
                  {
                     continue;
                  }

                  GitLabSharp.Entities.Version latestVersion = versions[0];
                  if (latestVersion.Created_At.ToLocalTime() > timestamp)
                  {
                     Debug.WriteLine(String.Format("WorkflowUpdateChecker.getUpdatesAsync -- this merge request has a new version -- created_at {0}",
                        latestVersion.Created_At.ToLocalTime().ToString()));

                     updates.UpdatedMergeRequests.Add(mergeRequest);
                  }
               }
            }
         }

         return updates;
      }

      private Workflow.Workflow Workflow { get; }
      private UpdateOperator UpdateOperator { get; }
      private UserDefinedSettings Settings { get; }
      private List<string> _cachedLabels { get; set; }

      private static readonly int mergeRequestCheckTimerInterval = 60000; // ms

      private System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = mergeRequestCheckTimerInterval
         };

      // Using 'Now' to gather only those MR which are created during the application lifetime
      private DateTime _lastCheckTimeStamp = DateTime.Now;
   }
}

