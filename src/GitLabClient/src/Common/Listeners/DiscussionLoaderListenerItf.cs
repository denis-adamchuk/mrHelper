using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   public interface IDiscussionLoaderListener
   {
      void OnPreLoadDiscussions(MergeRequestKey mrk);
      void OnPostLoadDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions);
      void OnFailedLoadDiscussions(MergeRequestKey mrk);
   }
}

