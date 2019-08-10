using System;
using System.Collections.Generic;

namespace mrHelper.Client
{
   /// <summary>
   /// Checks for new versions of a specific merge requests at GitLab
   /// </summary>
   public class GitClientUpdater
   {
      public GitClientUpdater(string project, int mergeRequestId)
      {
         LastCheckTimeStamp = DateTime.Min;

         Timer.Tick += new System.EventHandler(onTimer);
         Timer.Start();
      }

      public event EventHandler OnUpdate;

      public Task<bool> CheckNewVersionsAsync()
      {
         await checkNewVersionsAsync(LastCheckTimeStamp);
      }

      async void onTimer(object sender, EventArgs e)
      {
         if (await CheckNewVersionsAsync(LastCheckTimestamp))
         {
            OnUpdate?.Invoke(this);
         }
      }

      /// <summary>
      /// Checks if there is a version in GitLab which is newer than the passed timestamp.
      /// </summary>
      async private Task<bool> checkNewVersionsAsync(DateTime timestamp)
      {
         List<Version> versions = null;
         GitLab gl = new GitLab(hostName, accessToken));
         try
         {
            versions = await gl.Projects.Get(projectName).MergeRequests.Get(mergeRequestIId).
               Versions.LoadAllTaskAsync();
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot check GitLab for updates");
         }

         return versions != null && versions.Count > 0
            && versions[0].Created_At.ToLocalTime() > timestamp;
      }

      private DateTime LastCheckTimestamp { get; }
   }
}

