using GitLabSharp.Entities;

namespace mrHelper.GitLabClient
{
   public interface IMergeRequestFilterChecker
   {
      bool DoesMatchFilter(MergeRequest mergeRequest);
   }
}

