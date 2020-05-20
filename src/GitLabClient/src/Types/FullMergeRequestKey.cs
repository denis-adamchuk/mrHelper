using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using System.Collections.Generic;

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

      public override bool Equals(object obj)
      {
         return obj is FullMergeRequestKey key
            && ProjectKey.Equals(key.ProjectKey)
            && MergeRequest.IId == key.MergeRequest.IId;
      }

      public override int GetHashCode()
      {
         int hashCode = 1485227685;
         hashCode = hashCode * -1521134295 + ProjectKey.GetHashCode();
         hashCode = hashCode * -1521134295 + EqualityComparer<MergeRequest>.Default.GetHashCode(MergeRequest);
         return hashCode;
      }
   }
}

