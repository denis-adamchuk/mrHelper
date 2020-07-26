using System;

namespace mrHelper.Client.Common
{
   public interface IModificationNotifier
   {
      event Action MergeRequestModified;
      event Action DiscussionModified;
   }
}

