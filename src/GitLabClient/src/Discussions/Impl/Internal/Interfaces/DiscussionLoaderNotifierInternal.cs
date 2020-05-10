using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Types;

namespace mrHelper.Client.Workflow
{
   internal class DiscussionLoaderNotifierInternal :
      BaseNotifier<IDiscussionLoaderListenerInternal>,
      IDiscussionLoaderListenerInternal
   {
      public void OnPostLoadDiscussionsInternal(MergeRequestKey mrk, IEnumerable<Discussion> discussions,
         EDiscussionUpdateType type) => notifyAll(x => x.OnPostLoadDiscussionsInternal(mrk, discussions, type));
   }
}

