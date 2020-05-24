using System;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Types
{
   public struct MergeRequestKey : IEquatable<MergeRequestKey>
   {
      public MergeRequestKey(ProjectKey projectKey, int iid)
      {
         ProjectKey = projectKey;
         IId = iid;
      }

      public ProjectKey ProjectKey { get; }
      public int IId { get; }

      public override bool Equals(object obj)
      {
         return obj is MergeRequestKey key && Equals(key);
      }

      public bool Equals(MergeRequestKey other)
      {
         return ProjectKey.Equals(other.ProjectKey) &&
                IId == other.IId;
      }

      public override int GetHashCode()
      {
         int hashCode = -195462282;
         hashCode = hashCode * -1521134295 + ProjectKey.GetHashCode();
         hashCode = hashCode * -1521134295 + IId.GetHashCode();
         return hashCode;
      }
   }
}

