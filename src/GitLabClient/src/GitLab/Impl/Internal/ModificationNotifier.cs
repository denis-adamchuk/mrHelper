using System;

namespace mrHelper.Client.Common
{
   internal class ModificationNotifier : IModificationNotifier
   {
      internal void OnMergeRequestModified()
      {
         MergeRequestModified?.Invoke();
      }

      internal void OnDiscussionModified()
      {
         DiscussionModified?.Invoke();
      }

      public event Action MergeRequestModified;
      public event Action DiscussionModified;
   }
}

