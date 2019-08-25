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
      internal UpdateOperator(UserDefinedSettings settings)
      {
         Settings = settings;
      }

      async internal Task<List<MergeRequest>> GetMergeRequestsAsync(string host, string project)
      {
         GitLabClient client = new GitLabClient(host, Tools.Tools.GetAccessToken(host, Settings));
         try
         {
           return (List<MergeRequest>)(await client.RunAsync(async (gitlab) =>
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

      async internal Task<int> GetCommitsCountAsync(MergeRequestDescriptor mrd)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            return (int)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).Commits.CountTaskAsync()));
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is GitLabClientCancelled));
            if (ex is GitLabSharpException || ex is GitLabRequestException)
            {
               ExceptionHandlers.Handle(ex, "Cannot load commit count from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<Commit> GetLatestCommitAsync(MergeRequestDescriptor mrd)
      {
         GitLabClient client = new GitLabClient(mrd.HostName, Tools.Tools.GetAccessToken(mrd.HostName, Settings));
         try
         {
            return (Commit)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(mrd.ProjectName).MergeRequests.Get(mrd.IId).Commits.LoadTaskAsync(
                  new PageFilter { PageNumber = 1, PerPage = 1 })));
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

      private UserDefinedSettings Settings { get; }
   }
}

