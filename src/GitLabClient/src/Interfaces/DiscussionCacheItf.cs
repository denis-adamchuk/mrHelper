using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using GitLabSharp.Utils;

namespace mrHelper.GitLabClient
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

      IEnumerable<Discussion> GetDiscussions(MergeRequestKey mrk);

      void RequestUpdate(MergeRequestKey? mrk, int interval, Action onUpdateFinished);

      void RequestUpdate(MergeRequestKey? mrk, int[] intervals);

      event Action<UserEvents.DiscussionEvent> DiscussionEvent;
   }
}

