﻿namespace mrDiffTool
{
   public interface IntegratedDiffTool
   {
      string GetToolCommandArguments();

      string GetToolName();

      string[] GetToolRegistryNames();

      string GetToolCommand();

      void PatchToolConfig(string launchCommand);
   }

}
