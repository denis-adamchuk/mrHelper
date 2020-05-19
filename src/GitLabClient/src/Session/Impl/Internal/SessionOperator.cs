using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Types;

namespace mrHelper.Client.Session
{
   /// <summary>
   /// Implements Session-related interaction with GitLab
   /// </summary>
   internal class SessionOperator : BaseOperator
   {
      internal SessionOperator(string host, IHostProperties settings)
         : base(settings)
      {
         Host = host;
      }

      internal string Host { get; }

      internal Task<User> GetCurrentUserAsync()
      {
         return callWithNewClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  () =>
                     CommonOperator.SearchCurrentUserAsync(client)));
      }

      internal Task<ProjectKey> GetProjectAsync(string projectName)
      {
         return callWithNewClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     new ProjectKey(Host, ((Project)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).LoadTaskAsync())).Path_With_Namespace)));
      }

      internal Task<IEnumerable<MergeRequest>> SearchMergeRequestsAsync(
         SearchCriteria searchCriteria, int? maxResults, bool onlyOpen)
      {
         return callWithNewClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  () =>
                     CommonOperator.SearchMergeRequestsAsync(client, searchCriteria, maxResults, onlyOpen)));
      }

      internal Task<IEnumerable<Commit>> GetCommitsAsync(string projectName, int iid)
      {
         return callWithNewClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<Commit>)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).MergeRequests.Get(iid).Commits.LoadAllTaskAsync())));
      }

      internal Task<IEnumerable<Version>> GetVersionsAsync(string projectName, int iid)
      {
         return callWithNewClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                  (IEnumerable<Version>)await client.RunAsync(
                     async (gl) =>
                        await gl.Projects.Get(projectName).MergeRequests.Get(iid).Versions.LoadAllTaskAsync())));
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

      async private Task<T> callWithNewClient<T>(Func<GitLabClient, Task<T>> func)
      {
         return await callWithNewClient<T>(Host,
            async (client) =>
               await keepClient(client, func));
      }

      async private Task<T> keepClient<T>(GitLabClient client, Func<GitLabClient, Task<T>> func)
      {
         _clients.Add(client);
         try
         {
            return await func(client);
         }
         finally
         {
            _clients.Remove(client);
         }
      }

      private readonly List<GitLabClient> _clients = new List<GitLabClient>();
   }
}

