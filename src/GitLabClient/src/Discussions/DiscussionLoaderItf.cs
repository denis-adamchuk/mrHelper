using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Discussions
{
   public interface IDiscussionLoader
   {
      Task<IEnumerable<Discussion>> LoadDiscussions(MergeRequestKey mrk);

      event Action<MergeRequestKey> DiscussionsLoading;
      event Action<MergeRequestKey, IEnumerable<Discussion>> DiscussionsLoaded;
   }
}

