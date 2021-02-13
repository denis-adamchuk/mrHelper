using System;

namespace mrHelper.GitLabClient
{
   internal interface IModificationListener
   {
      void OnDiscussionResolved(MergeRequestKey mergeRequestKey);

      void OnDiscussionModified(MergeRequestKey mergeRequestKey);

      void OnTrackedTimeModified(MergeRequestKey mergeRequestKey, TimeSpan span, bool add);
   }
}

