using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
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

   public interface IDiscussionProvider
   {
      Task<IEnumerable<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk);
      DiscussionCount GetDiscussionCount(MergeRequestKey mrk);
   }
}

