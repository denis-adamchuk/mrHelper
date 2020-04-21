using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Discussions
{
   public interface IDiscussionLoader
   {
      event Action<MergeRequestKey> PreLoadDiscussions;
      event Action<MergeRequestKey, IEnumerable<Discussion>> PostLoadDiscussions;
      event Action<MergeRequestKey> FailedLoadDiscussions;
   }
}

