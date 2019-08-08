namespace mrHelper.Client
{
   /// <summary>
   /// Checks for updates at GitLab
   /// </summary>
   public class SingleMergeRequestUpdateChecker : IUpdateChecker
   {
      public UpdateChecker(string accessToken, string hostName, string projectName, int mergeRequestIId)
      {
         AccessToken = accessToken;
         HostName = hostName;
         ProjectName = projectName;
         MergeRequestIId = mergeRequestIId;
      }

      /// <summary>
      /// Checks if there is a version in GitLab which is newer than the passed timestamp.
      /// </summary>
      async public Task<bool> AreAnyUpdatesAsync(DateTime timestamp)
      {
         List<Version> versions = null;
         GitLab gl = new GitLab(HostName, AccessToken));
         try
         {
            versions = await gl.Projects.Get(ProjectName).MergeRequests.Get(MergeRequestIId).
               Versions.LoadAllTaskAsync();
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot check GitLab for updates");
         }

         return versions != null && versions.Count > 0
            && versions[0].Created_At.ToLocalTime() > timestamp;
      }

      private string AccessToken { get; }
      private string HostName { get; }
      private string ProjectName { get; }
      private int MergeRequestIId { get; }
   }
}

