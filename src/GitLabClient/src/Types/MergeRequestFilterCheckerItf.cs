using GitLabSharp.Entities;

namespace mrHelper.Client.Types
{
   public interface IMergeRequestFilterChecker
   {
      bool DoesMatchFilter(MergeRequest mergeRequest);
   }
}

