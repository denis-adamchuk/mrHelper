using System;
using System.Collections.Generic;

namespace mrHelper.Client
{
   public struct MergeRequestUpdates
   {
      public List<MergeRequest> NewMergeRequests;
      public List<MergeRequest> UpdatedMergeRequests;
   }

   public class WorkflowUpdater
   {
      WorkflowUpdater(UserDefinedSettings settings)
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
      }

      public event EventHandler<MergeRequestUpdates> OnUpdate;

      public WorkflowState State { get; set; }

      async void onTimer(object sender, EventArgs e)
      {
         MergeRequestUpdates updates = await getUpdatesAsync(lastCheckTimestamp);
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

         GitLab gl = new GitLab(State.HostName, Tools.GetAccessToken(State.HostName, Settings));
         foreach (var project in Projects)
         {
            List<MergeRequest> mergeRequests = new List<MergeRequest>();
            try
            {
               mergeRequests = await gl.Projects.Get(project.Path_With_Namespace).MergeRequests.LoadAllTaskAsync(
                  new MergeRequestsFilter());
            }
            catch (GitLabRequestException ex)
            {
               ExceptionHandlers.Handle(ex, "Cannot load merge requests on auto-update", false);
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
                  List<Version> versions = new List<Version>();
                  try
                  {
                     versions = await gl.Projects.Get(project.Path_With_Namespace).MergeRequests.
                        Get(mergeRequest.IId).Versions.LoadAllTaskAsync();
                  }
                  catch (GitLabRequestException ex)
                  {
                     ExceptionHandlers.Handle(ex, "Cannot load merge request versions on auto-update", false);
                     continue;
                  }

                  if (versions.Count == 0)
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

