using mrHelper.Common.Interfaces;

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
   }
}

