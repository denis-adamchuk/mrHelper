using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using Version = GitLabSharp.Entities.Version;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Constants;
using mrHelper.GitLabClient.Operators.Search;

namespace mrHelper.GitLabClient.Operators
{
   /// <summary>
   /// Implements DataCache-related interaction with GitLab
   /// </summary>
   internal class DataCacheOperator : BaseOperator, IDisposable
   {
      internal DataCacheOperator(string host, IHostProperties settings,
         INetworkOperationStatusListener networkOperationStatusListener)
         : base(host, settings, networkOperationStatusListener)
      {
         AvatarOperator = new AvatarOperator(host, settings, networkOperationStatusListener);
      }

      public new void Dispose()
      {
         base.Dispose();
         AvatarOperator.Dispose();
      }

      internal Task<IEnumerable<MergeRequest>> SearchMergeRequestsAsync(SearchQuery searchQuery)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<MergeRequest>)(await client.RunAsync(
                        async (gl) =>
                           await (new MergeRequestSearchProcessor(searchQuery).Process(gl))))));
      }

      internal Task<User> SearchCurrentUserAsync()
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (User)await client.RunAsync(
                        async (gl) =>
                           await gl.CurrentUser.LoadTaskAsync())));
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

      async internal Task<IEnumerable<Commit>> GetCommitsAsync(string projectName, int iid,
         string cachedRevisionTimestamp)
      {
         // If MaxCommitsToLoad exceeds 100, need to call LoadAllTaskAsync() w/o PageFilter
         Debug.Assert(Constants.MaxCommitsToLoad <= 100);

         MergeRequestKey mrk = new MergeRequestKey(new ProjectKey(Hostname, projectName), iid);
         IEnumerable<Commit> cachedCommits = cachedRevisionTimestamp != null
            ? GlobalCache.GetCommits(mrk, cachedRevisionTimestamp) : null;
         if (cachedCommits != null)
         {
            return cachedCommits;
         }

         Task<IEnumerable<Commit>> task = callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<Commit>)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).MergeRequests.Get(iid).Commits.LoadTaskAsync(
                              new GitLabSharp.Accessors.PageFilter(Constants.MaxCommitsToLoad, 1)))));

         IEnumerable<Commit> commits = await task;
         if (cachedRevisionTimestamp != null)
         {
            GlobalCache.SetCommits(mrk, commits, cachedRevisionTimestamp);
         }
         return commits;
      }

      async internal Task<IEnumerable<Version>> GetVersionsAsync(string projectName, int iid,
         string cachedRevisionTimestamp)
      {
         MergeRequestKey mrk = new MergeRequestKey(new ProjectKey(Hostname, projectName), iid);
         IEnumerable<Version> cachedVersions = cachedRevisionTimestamp != null
            ? GlobalCache.GetVersions(mrk, cachedRevisionTimestamp) : null;
         if (cachedVersions != null)
         {
            return cachedVersions;
         }

         Task<IEnumerable<Version>> task = callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                  (IEnumerable<Version>)await client.RunAsync(
                     async (gl) =>
                        await gl.Projects.Get(projectName).MergeRequests.Get(iid).Versions.LoadAllTaskAsync())));

         IEnumerable<Version> versions = await task;
         if (cachedRevisionTimestamp != null)
         {
            GlobalCache.SetVersions(mrk, versions, cachedRevisionTimestamp);
         }
         return versions;
      }

      internal Task<Commit> GetCommitAsync(string projectName, string id)
      {
         // TODO Add ability to store free commits in GlobalCache
         Commit cachedCommit = GlobalCache.GetCommit(new ProjectKey(Hostname, projectName), id);
         if (cachedCommit != null)
         {
            return Task.FromResult(cachedCommit);
         }
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (Commit)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).Repository.Commits.Get(id).LoadTaskAsync())));
      }

      internal Task<MergeRequestApprovalConfiguration> GetApprovalsAsync(string projectName, int iid)
      {
         return callWithSharedClient(
            async (client) =>
               await OperatorCallWrapper.Call(
                  async () =>
                     (MergeRequestApprovalConfiguration)await client.RunAsync(
                        async (gl) =>
                           await gl.Projects.Get(projectName).MergeRequests.Get(iid).GetApprovalConfigurationTaskAsync())));
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

      internal AvatarOperator AvatarOperator { get; }
   }
}

