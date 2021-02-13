using System;

namespace mrHelper.GitLabClient.Accessors
{
   internal class ModificationNotifier : IModificationListener, IModificationNotifier
   {
      public void OnDiscussionResolved(MergeRequestKey mergeRequestKey)
      {
         DiscussionResolved?.Invoke(mergeRequestKey);
      }

      public void OnDiscussionModified(MergeRequestKey mergeRequestKey)
      {
         DiscussionModified?.Invoke(mergeRequestKey);
      }

      public void OnTrackedTimeModified(MergeRequestKey mergeRequestKey, TimeSpan span, bool add)
      {
         TrackedTimeModified?.Invoke(mergeRequestKey, span, add);
      }

      public event Action<MergeRequestKey> DiscussionResolved;
      public event Action<MergeRequestKey> DiscussionModified;
      public event Action<MergeRequestKey, TimeSpan, bool> TrackedTimeModified;
   }
}

