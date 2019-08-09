namespace mrHelper.Client
{
   public struct MergeRequestUpdates
   {
      public List<MergeRequest> NewMergeRequests;
      public List<MergeRequest> UpdatedMergeRequests;
   }

   public class SingleHostUpdateChecker
   {
      SingleHostUpdatChecker<T>(string hostName, List<Project> projects, UserDefinedSettings settings)
      {
         HostName = hostName;
         AccessToken = accessToken;
         Projects = projects;
         LabelFilter = labelFilter;
         Labels = labels;

         Timer.Tick += new System.EventHandler(onTimer);
         Timer.Start();
      }

      public event EventHandler<MergeRequestUpdates> OnUpdate;

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

         if (HostName == null || AccessToken == null || Projects == null)
         {
            return updates;
         }

         GitLab gl = new GitLab(HostName, AccessToken);
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
               if (Labels.Intersect(mergeRequest.Labels).Count == 0)
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

      private string HostName { get; }
      private string AccessToken { get; }
      private List<Project> Projects { get; }
      private List<string> Labels { get; }

      private static readonly int mergeRequestCheckTimerInterval = 60000; // ms

      private readonly System.Timers.Timer Timer { get; } = new System.Timers.Timer
         {
            Interval = mergeRequestCheckTimerInterval
         };
      private DateTime _lastCheckTime = DateTime.Now;
   }
}

