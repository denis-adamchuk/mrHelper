using System;

namespace mrHelper.GitLabClient
{
   public interface IModificationNotifier
   {
      event Action<MergeRequestKey> MergeRequestModified;

      event Action<MergeRequestKey> DiscussionResolved;

      event Action<MergeRequestKey, TimeSpan, bool> TrackedTimeModified;
   }
}

