using GitLabSharp.Entities;
using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   public interface IMergeRequestLoaderListener
   {
      void OnPreLoadMergeRequest(MergeRequestKey mrk);
      void OnPostLoadMergeRequest(MergeRequestKey mrk, MergeRequest mergeRequest);
      void OnFailedLoadMergeRequest(MergeRequestKey mrk);
   }
}

