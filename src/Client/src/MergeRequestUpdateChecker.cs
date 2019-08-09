namespace mrHelper.Client
{
   /// <summary>
   /// Checks for new versions of a specific merge requests at GitLab
   /// </summary>
   public static class MergeRequestUpdateChecker
   {
      /// <summary>
      /// Checks if there is a version in GitLab which is newer than the passed timestamp.
      /// </summary>
      async static public Task<bool> AreAnyUpdatesAsync(string hostName, string accessToken,
         string projectName, int mergeRequestIId, DateTime timestamp)
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
   }
}

