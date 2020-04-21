using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Discussions
{
   public interface IDiscussionProvider
   {
      Task<IEnumerable<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk);

      event Action<UserEvents.DiscussionEvent> DiscussionEvent;
   }
}

