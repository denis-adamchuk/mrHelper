using System;

namespace mrHelper.Common
{
   public interface IClientCallback
   {
      string GetCurrentHostName();

      string GetCurrentAccessToken();

      string GetCurrentProjectName();

      int GetCurrentMergeRequestIId();

      string GetCurrentLocalGitFolder();
   }
}

