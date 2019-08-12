namespace mrHelper.Common.Interfaces
{
   public interface IIntegratedDiffTool
   {
      string GetToolCommandArguments();

      string GetToolName();

      string[] GetToolRegistryNames();

      string GetToolCommand();

      void PatchToolConfig(string launchCommand);
   }

}

