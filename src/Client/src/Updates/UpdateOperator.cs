using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.Client.Updates
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

      async internal Task<List<MergeRequest>> GetMergeRequests(string host, string project)
      {
         GitLabClient client = new GitLabClient(host, Tools.Tools.GetAccessToken(host, Settings));
         try
         {
           return (List<MergeRequest>)(await client.RunAsync(async (gitlab) =>
              await gitlab.Projects.Get(project).MergeRequests.LoadAllTaskAsync(new MergeRequestsFilter())));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               ExceptionHandlers.Handle(ex, "Cannot load merge requests on auto-update");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<List<GitLabSharp.Entities.Version>> GetVersions(MergeRequestDescriptor mrd)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            return (List<GitLabSharp.Entities.Version>)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).Versions.LoadAllTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               ExceptionHandlers.Handle(ex, "Cannot check GitLab for updates");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      private UserDefinedSettings Settings { get; }
   }
}

