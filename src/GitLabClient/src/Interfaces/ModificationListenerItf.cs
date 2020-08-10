using System;

namespace mrHelper.GitLabClient
{
   public interface IModificationListener
   {
      void OnMergeRequestModified(MergeRequestKey mergeRequestKey);

      void OnDiscussionResolved(MergeRequestKey mergeRequestKey);

      void OnTrackedTimeModified(MergeRequestKey mergeRequestKey, TimeSpan span, bool add);
   }
}

