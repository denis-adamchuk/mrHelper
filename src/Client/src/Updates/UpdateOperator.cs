using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using System.Diagnostics;

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

      async internal Task<Commit> GetLatestCommitAsync(MergeRequestDescriptor mrd)
      {
         try
         {
            List<Commit> commits = (List<Commit>)(await Client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).Commits.LoadTaskAsync(
                  new PageFilter { PageNumber = 1, PerPage = 1 })));
            return commits.Count > 0 ? commits[0] : new Commit();
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot load the latest commit from GitLab");
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

