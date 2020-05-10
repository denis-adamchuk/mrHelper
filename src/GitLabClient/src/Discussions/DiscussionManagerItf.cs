using System;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Discussions
{
   public class DiscussionManagerException : ExceptionEx
   {
      internal DiscussionManagerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface IDiscussionManager :
      IDiscussionProvider,
      IDiscussionEditorFactory,
      IDiscussionCreatorFactory,
      ILoader<IDiscussionLoaderListener>,
      ILoader<IDiscussionEventListener>
   {
      void CheckForUpdates(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished);
   }
}

