using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;

namespace mrHelper.GitLabClient.Operators.Search
{
   internal abstract class MergeRequestSearchProcessor
   {
      internal MergeRequestSearchProcessor(bool onlyOpen)
      {
         _wipFilter = MergeRequestsFilter.WorkInProgressFilter.All;
         _stateFilter = onlyOpen
            ? MergeRequestsFilter.StateFilter.Open : MergeRequestsFilter.StateFilter.All;
      }

      protected MergeRequestsFilter.WorkInProgressFilter _wipFilter;
      protected MergeRequestsFilter.StateFilter _stateFilter;

      public abstract Task<IEnumerable<MergeRequest>> Process(GitLab gl, int? maxResults);
   }

   internal abstract class CrossProjectMergeRequestSearchProcessor : MergeRequestSearchProcessor
   {
      internal CrossProjectMergeRequestSearchProcessor(bool onlyOpen)
         : base(onlyOpen)
      {
      }

      public override abstract Task<IEnumerable<MergeRequest>> Process(GitLab gl, int? maxResults);

      async protected Task<IEnumerable<MergeRequest>> load(GitLab gl, int? maxResults,
         MergeRequestsFilter filter)
      {
         Task<IEnumerable<MergeRequest>> t = maxResults.HasValue
            ? gl.MergeRequests.LoadTaskAsync(filter, new PageFilter(maxResults.Value, 1))
            : gl.MergeRequests.LoadAllTaskAsync(filter);
         return await t;
      }
   }

   internal abstract class SingleProjectMergeRequestSearchProcessor : MergeRequestSearchProcessor
   {
      internal SingleProjectMergeRequestSearchProcessor(bool onlyOpen)
         : base(onlyOpen)
      {
      }

      public override abstract Task<IEnumerable<MergeRequest>> Process(GitLab gl, int? maxResults);

      async protected Task<IEnumerable<MergeRequest>> load(GitLab gl, string projectname, int? maxResults,
         MergeRequestsFilter filter)
      {
         Task<IEnumerable<MergeRequest>> t = maxResults.HasValue
            ? gl.Projects.Get(projectname).MergeRequests.LoadTaskAsync(filter, new PageFilter(maxResults.Value, 1))
            : gl.Projects.Get(projectname).MergeRequests.LoadAllTaskAsync(filter);
         return await t;
      }
   }
}

