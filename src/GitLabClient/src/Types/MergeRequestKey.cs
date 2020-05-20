using mrHelper.Common.Interfaces;
using System.Collections.Generic;

namespace mrHelper.Client.Types
{
   public struct MergeRequestKey
   {
      public MergeRequestKey(ProjectKey projectKey, int iId)
      {
         ProjectKey = projectKey;
         IId = iId;
      }

      public ProjectKey ProjectKey { get; }
      public int IId { get; }

      public override bool Equals(object obj)
      {
         return obj is MergeRequestKey key
            && ProjectKey.Equals(ProjectKey, key.ProjectKey)
            && IId == key.IId;
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

