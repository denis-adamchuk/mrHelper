using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using System.Diagnostics;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.Updates
{
   /// <summary>
   /// Implements Updates-related interaction with GitLab
   /// </summary>
   internal class UpdateOperator
   {
      internal UpdateOperator(string host, string token)
      {
         Client = new GitLabClient(host, token);
      }

      async internal Task<List<MergeRequest>> GetMergeRequestsAsync(string project)
      {
         try
         {
           return (List<MergeRequest>)(await Client.RunAsync(async (gitlab) =>
              await gitlab.Projects.Get(project).MergeRequests.LoadAllTaskAsync(new MergeRequestsFilter())));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot load merge requests from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<Version> GetLatestVersionAsync(MergeRequestDescriptor mrd)
      {
         try
         {
            List<Version> versions = (List<Version>)(await Client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).Versions.LoadAllTaskAsync()));
            return versions.Count > 0 ? versions[0] : new Version();
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot load the latest version from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      internal Task CancelAsync()
      {
         return Client.CancelAsync();
      }

      private GitLabClient Client { get; }
   }
}

