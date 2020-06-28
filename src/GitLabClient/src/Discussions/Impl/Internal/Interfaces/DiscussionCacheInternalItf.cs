using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Discussions
{
   public enum DiscussionUpdateType
   {
      InitialSnapshot,
      PeriodicUpdate,
      NewMergeRequest
   }

   internal interface IDiscussionCacheInternal : IDiscussionCache
   {
      event Action<MergeRequestKey, IEnumerable<Discussion>, DiscussionUpdateType> DiscussionsLoadedInternal;
   }
}

