using System;

namespace mrHelper.Client
{
   /// <summary>
   /// Implements Updates-related interaction with GitLab
   /// </summary>
   internal class UpdateOperator
   {
      internal UpdateOperator(UserDefinedSettings settings)
      {
         Settings = settings;
      }

      async internal Task<List<MergeRequests>> GetMergeRequests(string host, string project)
      {
         GitLabClient client = new GitLabClient(host, Tools.GetAccessToken(host, Settings);
         try
         {
           return await client.RunAsync(async (gitlab) =>
              return await gitlab.Projects.Get(project).MergeRequests.LoadAllTaskAsync(new MergeRequestsFilter());
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge requests on auto-update");
            throw new OperatorException(ex);
         }
      }

      async internal Task<List<Versions>> GetVersions(MergeRequestDescriptor mrd)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.GetAccessToken(mrd.HostName, Settings);
         try
         {
            return await client.RunAsync(async (gitlab) =>
               return await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).Versions.LoadAllTaskAsync());
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot check GitLab for updates");
            throw new OperatorException(ex);
         }
      }

      private Settings Settings { get; }
   }
}

