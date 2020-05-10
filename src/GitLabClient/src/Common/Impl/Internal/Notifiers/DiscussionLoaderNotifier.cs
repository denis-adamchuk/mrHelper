using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   internal class DiscussionLoaderNotifier : BaseNotifier<IDiscussionLoaderListener>, IDiscussionLoaderListener
   {
      public void OnPreLoadDiscussions(MergeRequestKey mrk) =>
         notifyAll(x => x.OnPreLoadDiscussions(mrk));

      public void OnPostLoadDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions) =>
         notifyAll(x => x.OnPostLoadDiscussions(mrk, discussions));

      public void OnFailedLoadDiscussions(MergeRequestKey mrk) =>
         notifyAll(x => x.OnFailedLoadDiscussions(mrk));
   }
}

