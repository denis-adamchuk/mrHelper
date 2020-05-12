using System;
using GitLabSharp.Utils;
using mrHelper.Client.Common;
using mrHelper.Client.Types;

namespace mrHelper.Client.Discussions
{
   public struct DiscussionCount
   {
      public enum EStatus
      {
         NotAvailable,
         Loading,
         Ready
      }

      public int? Resolvable;
      public int? Resolved;
      public EStatus Status;
   }

   public class DiscussionCacheException : ExceptionEx
   {
      internal DiscussionCacheException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface IDiscussionCache : IDiscussionLoader
   {
      DiscussionCount GetDiscussionCount(MergeRequestKey mrk);

      IUpdateToken RequestUpdate(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished);

      event Action<UserEvents.DiscussionEvent> DiscussionEvent;
   }
}

