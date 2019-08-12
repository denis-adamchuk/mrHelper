using System;
using System.Collections.Generic;

namespace mrHelper.Client
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
      WorkflowUpdateChecker(UserDefinedSettings settings, UpdateOperator updateOperator, Workflow workflow)
      {
         Settings = settings;
         Settings.PropertyChange += async (sender, property) =>
         {
            if (property.PropertyName == "LastUsedLabels")
            {
               _cachedLabels = Tools.SplitLabels(Settings.LastUsedLabels);
            }
         }
         _cachedLabels = Tools.SplitLabels(Settings.LastUsedLabels);

         Timer.Tick += new System.EventHandler(onTimer);
         Timer.Start();

         UpdateOperator = updateOperator;
         Workflow = workflow;
      }

      public event EventHandler<MergeRequestUpdates> OnUpdate;

      async void onTimer(object sender, EventArgs e)
      {
         MergeRequestUpdates updates = new MergeRequestUpdates();
         try
         {
            updates = await getUpdatesAsync(lastCheckTimestamp);
         }
         catch (OperatorException)
         {
            ExceptionHandlers.Handle(ex, "Auto-update failed");
         }
         if (updates.NewMergeRequests.Count > 0 || updates.UpdatedMergeRequests.Count > 0)
         {
            OnUpdate?.Invoke(this, updates);
         }
      }

      /// <summary>
      /// Collects requests that have been created or updated later than _lastCheckTime.
      /// By 'updated' we mean that 'merge request has a version with a timestamp later than ...'.
      /// Includes only those merge requests that match Labels filters.
      /// </summary>
      async private Task<MergeRequestUpdates> getUpdatesAsync(DateTime timestamp)
      {
         MergeRequestUpdates updates = new MergeRequestUpdates
         {
            NewMergeRequests = new List<MergeRequest>(),
            UpdatedMergeRequests = new List<MergeRequest>()
         };

         if (State.HostName == null || State.Projects == null)
         {
            return updates;
         }

         foreach (var project in Projects)
         {
            List<MergeRequest> mergeRequests =
               updateOperator.GetMergeRequests(State.HostName, project.Path_With_Namespace);
            if (mergeRequests == null)
            {
               continue;
            }

            foreach (var mergeRequest in mergeRequests)
            {
               if (_cachedLabels.Intersect(mergeRequest.Labels).Count == 0)
               {
                  continue;
               }

               if (mergeRequest.Created_At.ToLocalTime() > _lastCheckTime)
               {
                  updates.NewMergeRequests.Add(mergeRequest);
               }
               else if (mergeRequest.Updated_At.ToLocalTime() > _lastCheckTime)
               {
                  List<Version> versions = updateOperator.GetVersions(
                     new MergeRequestDescriptor
                     {
                        HostName = State.HostName,
                        ProjectName = project.Path_With_Namespace,
                        IId = mergeRequest.IId
                     });
                  if (versions == null || versions.Count == 0)
                  {
                     continue;
                  }

                  Version latestVersion = versions[0];
                  if (latestVersion.Created_At.ToLocalTime() > _lastCheckTime)
                  {
                     updates.UpdatedMergeRequests.Add(mergeRequest);
                  }
               }
            }
         }
      }

      private UserDefinedSettings Settings { get; }
      private List<Label> _cachedLabels { get; set; }

      private static readonly int mergeRequestCheckTimerInterval = 60000; // ms

      private readonly System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = mergeRequestCheckTimerInterval
         };
      private DateTime _lastCheckTime = DateTime.Now;
   }
}

