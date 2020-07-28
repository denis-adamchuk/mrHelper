using System;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.GitLabClient.Managers
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

