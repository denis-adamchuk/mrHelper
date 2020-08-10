using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;

namespace mrHelper.GitLabClient
{
   public interface IDiscussionLoader
   {
      Task<IEnumerable<Discussion>> LoadDiscussions(MergeRequestKey mrk);

      event Action<MergeRequestKey> DiscussionsLoading;
      event Action<MergeRequestKey, IEnumerable<Discussion>> DiscussionsLoaded;
   }
}

