using System;

namespace mrHelper.Common
{
   public interface ICommandCallback
   {
      string GetCurrentHostName();

      string GetCurrentAccessToken();

      string GetCurrentProjectName();

      int GetCurrentMergeRequestIId();

      string GetCurrentLocalGitFolder();
   }
}

