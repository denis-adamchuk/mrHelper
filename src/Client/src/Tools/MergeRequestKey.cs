using System;

namespace mrHelper.Client.Tools
{
   public struct MergeRequestKey
   {
      public ProjectKey ProjectKey;
      public int IId;

      public MergeRequestKey(string hostname, string projectname, int iid)
      {
         ProjectKey = new ProjectKey{ HostName = hostname, ProjectName = projectname };
         IId = iid;
      }
   }
}

