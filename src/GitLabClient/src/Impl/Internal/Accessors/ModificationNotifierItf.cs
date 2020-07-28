using System;

namespace mrHelper.GitLabClient.Accessors
{
   internal interface IModificationNotifier
   {
      event Action<MergeRequestKey> MergeRequestModified;

      event Action<MergeRequestKey> DiscussionResolved;

      event Action<MergeRequestKey, TimeSpan, bool> TrackedTimeModified;
   }
}

