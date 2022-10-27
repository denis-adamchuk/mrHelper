namespace mrHelper.GitLabClient
{
   public interface IMergeRequestFilterChecker
   {
      bool DoesMatchFilter(FullMergeRequestKey mergeRequest);
   }
}

