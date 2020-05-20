using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   /// <summary>
   /// Implements common interaction with GitLab
   /// </summary>
   internal static class CommonOperator
   {
      async internal static Task<IEnumerable<MergeRequest>> SearchMergeRequestsAsync(
         GitLabClient client, SearchCriteria searchCriteria, int? maxResults, bool onlyOpen)
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

      internal static Task<User> SearchCurrentUserAsync(GitLabClient client)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (User)await client.RunAsync(
                  async (gl) =>
                     await gl.CurrentUser.LoadTaskAsync()));
      }

      internal static Task<IEnumerable<User>> SearchUserAsync(GitLabClient client, string name, bool isUsername)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (IEnumerable<User>)await client.RunAsync(
                  async (gl) =>
                     await (isUsername ? gl.Users.SearchByUsernameTaskAsync(name) : gl.Users.SearchTaskAsync(name))));
      }

      internal static Task<Project> SearchProjectAsync(GitLabClient client, string projectname)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (Project)await client.RunAsync(
                  async (gl) =>
                     await gl.Projects.Get(projectname).LoadTaskAsync()));
      }
   }
}

