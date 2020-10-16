using System.Collections.Generic;
using System.Diagnostics;
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

      public abstract Task<IEnumerable<MergeRequest>> Process(GitLab gl);

      protected Task<IEnumerable<MergeRequest>> load(dynamic accessor,
         GitLabSharp.Accessors.MergeRequestsFilter filter, int? maxResults)
      {
         // See restrictions at https://docs.gitlab.com/ee/api/README.html#offset-based-pagination
         Debug.Assert(!maxResults.HasValue || maxResults.Value <= 100);

         return maxResults.HasValue
            ? accessor.LoadTaskAsync(filter, new PageFilter(maxResults.Value, 1))
            : accessor.LoadAllTaskAsync(filter);
      }
   }

   internal abstract class CrossProjectMergeRequestSearchProcessor : MergeRequestSearchProcessor
   {
      internal CrossProjectMergeRequestSearchProcessor(bool onlyOpen)
         : base(onlyOpen)
      {
      }

      public override abstract Task<IEnumerable<MergeRequest>> Process(GitLab gl);

      protected Task<IEnumerable<MergeRequest>> load(GitLab gl, int? maxResults,
         MergeRequestsFilter filter)
      {
         return load(gl.MergeRequests, filter, maxResults);
      }
   }

   internal abstract class SingleProjectMergeRequestSearchProcessor : MergeRequestSearchProcessor
   {
      internal SingleProjectMergeRequestSearchProcessor(bool onlyOpen)
         : base(onlyOpen)
      {
      }

      public override abstract Task<IEnumerable<MergeRequest>> Process(GitLab gl);

      protected Task<IEnumerable<MergeRequest>> load(GitLab gl, string projectname, int? maxResults,
         MergeRequestsFilter filter)
      {
         return load(gl.Projects.Get(projectname).MergeRequests, filter, maxResults);
      }
   }
}

