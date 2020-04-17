using System;
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
         _host = host;
         _token = token;
      }

      async internal Task<User> GetCurrentUserAsync()
      {
         GitLabClient client = new GitLabClient(_host, _token);
         _clients.Add(client);
         try
         {
            return (User)(await client.RunAsync(async (gl) => await gl.CurrentUser.LoadTaskAsync() ));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               throw new OperatorException(ex);
            }
            throw;
         }
         finally
         {
            _clients.Remove(client);
         }
      }

      async internal Task<Project> GetProjectAsync(string projectName)
      {
         GitLabClient client = new GitLabClient(_host, _token);
         _clients.Add(client);
         try
         {
            return (Project)(await client.RunAsync(async (gl) => await gl.Projects.Get(projectName).LoadTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               throw new OperatorException(ex);
            }
            throw;
         }
         finally
         {
            _clients.Remove(client);
         }
      }

      async internal Task<IEnumerable<MergeRequest>> SearchMergeRequestsAsync(
         object search, int? maxResults, bool onlyOpen)
      {
         GitLabClient client = new GitLabClient(_host, _token);
         _clients.Add(client);
         try
         {
            return await CommonOperator.SearchMergeRequestsAsync(client, search, maxResults, onlyOpen);
         }
         finally
         {
            _clients.Remove(client);
         }
      }

      async internal Task<IEnumerable<Commit>> GetCommitsAsync(string projectName, int iid)
      {
         GitLabClient client = new GitLabClient(_host, _token);
         _clients.Add(client);
         try
         {
            return (IEnumerable<Commit>)(await client.RunAsync(async (gl) =>
               await gl.Projects.Get(projectName).MergeRequests.Get(iid).Commits.LoadAllTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               throw new OperatorException(ex);
            }
            throw;
         }
         finally
         {
            _clients.Remove(client);
         }
      }

      async internal Task<IEnumerable<Version>> GetVersionsAsync(string projectName, int iid)
      {
         GitLabClient client = new GitLabClient(_host, _token);
         _clients.Add(client);
         try
         {
            return await CommonOperator.GetVersionsAsync(client, projectName, iid);
         }
         finally
         {
            _clients.Remove(client);
         }
      }

      async internal Task<Version> GetLatestVersionAsync(string projectName, int iid)
      {
         GitLabClient client = new GitLabClient(_host, _token);
         _clients.Add(client);
         try
         {
            return await CommonOperator.GetLatestVersionAsync(client, projectName, iid);
         }
         finally
         {
            _clients.Remove(client);
         }
      }

      public Task CancelAsync()
      {
         List<Task> tasks = new List<Task>();
         foreach (GitLabClient client in _clients)
         {
            tasks.Add(client.CancelAsync());
         }
         return Task.WhenAll(tasks);
      }

      private readonly List<GitLabClient> _clients = new List<GitLabClient>();
      private readonly string _host;
      private readonly string _token;
   }
}

