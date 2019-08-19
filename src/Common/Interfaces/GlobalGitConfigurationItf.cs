using System;

namespace mrHelper.Common.Interfaces
{
   public interface IGlobalGitConfiguration
   {
      void SetGlobalDiffTool(string name, string command);
      void RemoveGlobalDiffTool(string name);
   }
}

