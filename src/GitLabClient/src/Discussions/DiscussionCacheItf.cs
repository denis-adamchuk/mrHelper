using System;
using GitLabSharp.Utils;
using mrHelper.Client.Common;
using mrHelper.Client.Types;

namespace mrHelper.Client.Discussions
{
   public struct DiscussionCount
   {
      public DiscussionCount(int? resolvable, int? resolved, EStatus status)
      {
         Resolvable = resolvable;
         Resolved = resolved;
         Status = status;
      }

      public enum EStatus
      {
         NotAvailable,
         Loading,
         Ready
      }

      public int? Resolvable { get; }
      public int? Resolved { get; }
      public EStatus Status { get; }
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

      void RequestUpdate(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished);

      event Action<UserEvents.DiscussionEvent> DiscussionEvent;
   }
}

