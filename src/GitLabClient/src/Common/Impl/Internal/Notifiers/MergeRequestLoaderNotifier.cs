using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   internal class MergeRequestLoaderNotifier : BaseNotifier<IMergeRequestLoaderListener>, IMergeRequestLoaderListener
   {
      public void OnPreLoadMergeRequest(MergeRequestKey mrk) =>
         notifyAll(x => x.OnPreLoadMergeRequest(mrk));

      public void OnPostLoadMergeRequest(MergeRequestKey mrk, MergeRequest mergeRequest) =>
         notifyAll(x => x.OnPostLoadMergeRequest(mrk, mergeRequest));

      public void OnFailedLoadMergeRequest(MergeRequestKey mrk) =>
         notifyAll(x => x.OnFailedLoadMergeRequest(mrk));
   }
}

