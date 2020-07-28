using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Operators.Search;

namespace mrHelper.GitLabClient.Operators
{
   /// <summary>
   /// Implements common interaction with GitLab
   /// </summary>
   internal static class CommonOperator
   {
      async internal static Task<IEnumerable<MergeRequest>> SearchMergeRequestsAsync(
         GitLabTaskRunner client, SearchCriteria searchCriteria, int? maxResults, bool onlyOpen)
      {
         List<MergeRequest> mergeRequests = new List<MergeRequest>();
         foreach (object search in searchCriteria.Criteria)
         {
            mergeRequests.AddRange(
               await OperatorCallWrapper.Call(
                  async () =>
                     (IEnumerable<MergeRequest>)(await client.RunAsync(
                        async (gl) =>
                           await MergeRequestSearchProcessorFactory.Create(search, onlyOpen).Process(gl, maxResults)))));
         }
         return mergeRequests;
      }

      internal static Task<User> SearchCurrentUserAsync(GitLabTaskRunner client)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (User)await client.RunAsync(
                  async (gl) =>
                     await gl.CurrentUser.LoadTaskAsync()));
      }

      internal static Task<Project> SearchProjectAsync(GitLabTaskRunner client, string projectname)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (Project)await client.RunAsync(
                  async (gl) =>
                     await gl.Projects.Get(projectname).LoadTaskAsync()));
      }
   }
}

