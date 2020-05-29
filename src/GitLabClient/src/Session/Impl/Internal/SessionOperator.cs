using System.Threading.Tasks;
using System.Collections.Generic;
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
         : base(host, settings)
      {
         Host = host; // TODO Can be removed if users keep Host separately from Operator
      }

      internal string Host { get; }

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
                     new ProjectKey(Host, ((Project)await client.RunAsync(
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
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<Commit>)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).MergeRequests.Get(iid).Commits.LoadAllTaskAsync())));
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

      public new void Cancel()
      {
         base.Cancel();
      }
   }
}

