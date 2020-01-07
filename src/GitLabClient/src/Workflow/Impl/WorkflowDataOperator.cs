using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.Workflow
{
   /// <summary>
   /// Implements Workflow-related interaction with GitLab
   /// </summary>
   internal class WorkflowDataOperator
   {
      internal WorkflowDataOperator(string host, string token)
      {
         _client = new GitLabClient(host, token);
      }

      async internal Task<User> GetCurrentUserAsync()
      {
         try
         {
            return (User)(await _client.RunAsync(async (gl) => await gl.CurrentUser.LoadTaskAsync() ));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               GitLabExceptionHandlers.Handle(ex, "Cannot load current user from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal Task<Project> GetProjectAsync(string projectName)
      {
         try
         {
            return (Project)(await _client.RunAsync(async (gl) => await gl.Projects.Get(projectName).LoadTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               GitLabExceptionHandlers.Handle(ex, "Cannot load project from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      internal Task<IEnumerable<MergeRequest>> GetMergeRequestsAsync(string projectName)
      {
         return CommonOperator.GetMergeRequestsAsync(_client, projectName);
      }

      internal Task<MergeRequest> GetMergeRequestAsync(string projectName, int iid)
      {
         return CommonOperator.GetMergeRequestAsync(_client, projectName, iid);
      }

      async internal Task<IEnumerable<Commit>> GetCommitsAsync(string projectName, int iid)
      {
         try
         {
            return (IEnumerable<Commit>)(await _client.RunAsync(async (gl) =>
               await gl.Projects.Get(projectName).MergeRequests.Get(iid).Commits.LoadAllTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               GitLabExceptionHandlers.Handle(ex, "Cannot load merge request commits from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      internal Task<Version> GetLatestVersionAsync(string projectName, int iid)
      {
         return CommonOperator.GetLatestVersionAsync(_client, projectName, iid);
      }

      public Task CancelAsync()
      {
         return _client.CancelAsync();
      }

      private readonly GitLabClient _client;
   }
}

