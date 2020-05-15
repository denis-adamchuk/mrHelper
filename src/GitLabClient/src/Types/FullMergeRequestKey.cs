using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Types
{
   public struct FullMergeRequestKey
   {
      public FullMergeRequestKey(ProjectKey projectKey, MergeRequest mergeRequest)
      {
         ProjectKey = projectKey;
         MergeRequest = mergeRequest;
      }

      public ProjectKey ProjectKey { get; }
      public MergeRequest MergeRequest { get; }
   }
}

