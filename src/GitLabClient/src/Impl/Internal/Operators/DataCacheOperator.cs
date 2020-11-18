using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Constants;

namespace mrHelper.GitLabClient.Operators
{
   /// <summary>
   /// Implements DataCache-related interaction with GitLab
   /// </summary>
   internal class DataCacheOperator : BaseOperator
   {
      internal DataCacheOperator(string host, IHostProperties settings)
         : base(host, settings)
      {
      }

      internal Task<User> GetCurrentUserAsync()
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  () =>
                     CommonOperator.SearchCurrentUserAsync(client)));
      }

      internal Task<Project> GetProjectAsync(string projectName)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Project)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).LoadTaskAsync())));
      }

      internal Task<MergeRequest> GetMergeRequestAsync(string projectName, int iid, bool? includeRebaseInProgress = null)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (MergeRequest)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).MergeRequests.Get(iid).LoadTaskAsync(includeRebaseInProgress))));
      }

      internal Task<IEnumerable<MergeRequest>> SearchMergeRequestsAsync(SearchQuery searchQuery)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  () =>
                     CommonOperator.SearchMergeRequestsAsync(client, searchQuery)));
      }

      internal Task<IEnumerable<Commit>> GetCommitsAsync(string projectName, int iid)
      {
         // If MaxCommitsToLoad exceeds 100, need to call LoadAllTaskAsync() w/o PageFilter
         Debug.Assert(Constants.MaxCommitsToLoad <= 100);
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<Commit>)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).MergeRequests.Get(iid).Commits.LoadTaskAsync(
                              new GitLabSharp.Accessors.PageFilter(Constants.MaxCommitsToLoad, 1)))));
      }

      internal Task<IEnumerable<Version>> GetVersionsAsync(string projectName, int iid)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                  (IEnumerable<Version>)await client.RunAsync(
                     async (gl) =>
                        await gl.Projects.Get(projectName).MergeRequests.Get(iid).Versions.LoadAllTaskAsync())));
      }

      internal Task<Commit> GetCommitAsync(string projectName, string id)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Commit)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).Repository.Commits.Get(id).LoadTaskAsync())));
      }

      internal Task<IEnumerable<Project>> GetProjects()
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<Project>)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.LoadAllTaskAsync(
                              new GitLabSharp.Accessors.ProjectsFilter(false, true, true)))));
      }

      internal Task<IEnumerable<User>> GetUsers()
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<User>)await client.RunAsync(
                        async (gl) =>
                           await gl.Users.LoadAllTaskAsync())));
      }
   }
}

