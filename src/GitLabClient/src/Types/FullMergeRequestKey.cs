using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace mrHelper.Client.Types
{
   public struct FullMergeRequestKey : IEquatable<FullMergeRequestKey>
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
         return obj is FullMergeRequestKey key && Equals(key);
      }

      public bool Equals(FullMergeRequestKey other)
      {
         return ProjectKey.Equals(other.ProjectKey)
            && ((MergeRequest == null && other.MergeRequest == null)
               || (MergeRequest.IId == other.MergeRequest.IId));
      }

      public override int GetHashCode()
      {
         int hashCode = 1485227685;
         hashCode = hashCode * -1521134295 + ProjectKey.GetHashCode();
         hashCode = hashCode * -1521134295 + MergeRequest?.IId.GetHashCode() ?? 0;
         return hashCode;
      }
   }
}

