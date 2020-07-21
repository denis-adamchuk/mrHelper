using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Types;
using mrHelper.Common.Constants;

namespace mrHelper.Client.Session
{
   /// <summary>
   /// Implements Session-related interaction with GitLab
   /// </summary>
   internal class SessionOperator : BaseOperator
   {
      internal SessionOperator(string host, IHostProperties settings)
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

      internal Task<ProjectKey> GetProjectAsync(string projectName)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     new ProjectKey(Hostname, ((Project)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).LoadTaskAsync())).Path_With_Namespace)));
      }

      internal Task<IEnumerable<MergeRequest>> SearchMergeRequestsAsync(
         SearchCriteria searchCriteria, int? maxResults, bool onlyOpen)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  () =>
                     CommonOperator.SearchMergeRequestsAsync(client, searchCriteria, maxResults, onlyOpen)));
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
   }
}

