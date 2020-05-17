using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using System.Diagnostics;
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
                           await loadMergeRequests(gl, search, maxResults, onlyOpen)))));
         }
         return mergeRequests;
      }

      async private static Task<IEnumerable<MergeRequest>> loadMergeRequests(GitLab gl,
         object search, int? maxResults, bool onlyOpen)
      {
         if (search is Types.SearchByIId sid)
         {
            return new MergeRequest[]
               { await gl.Projects.Get(sid.ProjectName).MergeRequests.Get(sid.IId).LoadTaskAsync() };
         }

         BaseMergeRequestAccessor accessor = search is Types.SearchByProject sbp
            ? (BaseMergeRequestAccessor)gl.Projects.Get(sbp.ProjectName).MergeRequests
            : (BaseMergeRequestAccessor)gl.MergeRequests;
         if (maxResults.HasValue)
         {
            PageFilter pageFilter = new PageFilter(maxResults.Value, 1);
            return await accessor.LoadTaskAsync(convertSearchToFilter(search, onlyOpen), pageFilter);
         }
         return await accessor.LoadAllTaskAsync(convertSearchToFilter(search, onlyOpen));
      }

      internal static Task<IEnumerable<User>> SearchUserAsync(GitLabClient client, string name)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (IEnumerable<User>)await client.RunAsync(
                  async (gl) =>
                     await gl.Users.SearchTaskAsync(name)));
      }

      internal static Task<Project> SearchProjectAsync(GitLabClient client, string projectname)
      {
         return OperatorCallWrapper.Call(
            async () =>
               (Project)await client.RunAsync(
                  async (gl) =>
                     await gl.Projects.Get(projectname).LoadTaskAsync()));
      }

      private static MergeRequestsFilter convertSearchToFilter(object search, bool onlyOpen)
      {
         MergeRequestsFilter.WorkInProgressFilter wipFilter = onlyOpen
            ? MergeRequestsFilter.WorkInProgressFilter.Yes
            : MergeRequestsFilter.WorkInProgressFilter.All;
         MergeRequestsFilter.StateFilter stateFilter = onlyOpen
            ? MergeRequestsFilter.StateFilter.Open
            : MergeRequestsFilter.StateFilter.All;

         if (search is Types.SearchByIId sbi)
         {
            return new MergeRequestsFilter(null, wipFilter, stateFilter, false, null, null, new int[] { sbi.IId });
         }
         else if (search is Types.SearchByProject sbp)
         {
            return new MergeRequestsFilter(null, wipFilter, stateFilter, false, null, null, null);
         }
         else if (search is Types.SearchByTargetBranch sbt)
         {
            return new MergeRequestsFilter(null, wipFilter, stateFilter, false, null, sbt.TargetBranchName, null);
         }
         else if (search is Types.SearchByText sbtxt)
         {
            return new MergeRequestsFilter(null, wipFilter, stateFilter, false, sbtxt.Text, null, null);
         }

         Debug.Assert(false);
         return default(MergeRequestsFilter);
      }
   }
}

