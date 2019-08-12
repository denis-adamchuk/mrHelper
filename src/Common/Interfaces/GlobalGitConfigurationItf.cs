using System;

namespace mrHelper.Common.Interfaces
{
   public interface IGlobalGitConfiguration
   {
      public void SetDiffTool(string name, string command);
      public void RemoveDiffTool(string name);
   }
}

