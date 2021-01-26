using System;

namespace mrHelper.GitLabClient
{
   internal interface IModificationNotifier
   {
      event Action<MergeRequestKey> DiscussionResolved;

      event Action<MergeRequestKey, TimeSpan, bool> TrackedTimeModified;
   }
}

