using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Discussions
{
   public enum EDiscussionUpdateType
   {
      InitialSnapshot,
      PeriodicUpdate,
      NewMergeRequest
   }

   internal interface IDiscussionLoaderInternal
   {
      event Action<MergeRequestKey, IEnumerable<Discussion>, EDiscussionUpdateType> PostLoadDiscussionsInternal;
   }
}

