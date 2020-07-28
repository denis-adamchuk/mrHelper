using System;

namespace mrHelper.GitLabClient.Accessors
{
   internal interface IModificationListener
   {
      void OnMergeRequestModified(MergeRequestKey mergeRequestKey);

      void OnDiscussionResolved(MergeRequestKey mergeRequestKey);

      void OnTrackedTimeModified(MergeRequestKey mergeRequestKey, TimeSpan span, bool add);
   }
}

