using System;

namespace mrHelper.GitLabClient.Accessors
{
   public class ModificationNotifier : IModificationListener, IModificationNotifier
   {
      public void OnMergeRequestModified(MergeRequestKey mergeRequestKey)
      {
         MergeRequestModified?.Invoke(mergeRequestKey);
      }

      public void OnDiscussionResolved(MergeRequestKey mergeRequestKey)
      {
         DiscussionResolved?.Invoke(mergeRequestKey);
      }

      public void OnTrackedTimeModified(MergeRequestKey mergeRequestKey, TimeSpan span, bool add)
      {
         TrackedTimeModified?.Invoke(mergeRequestKey, span, add);
      }

      public event Action<MergeRequestKey> MergeRequestModified;
      public event Action<MergeRequestKey> DiscussionResolved;
      public event Action<MergeRequestKey, TimeSpan, bool> TrackedTimeModified;
   }
}

