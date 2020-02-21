using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Types
{
   public struct FullMergeRequestKey
   {
      public ProjectKey ProjectKey;
      public MergeRequest MergeRequest;
   }
}

