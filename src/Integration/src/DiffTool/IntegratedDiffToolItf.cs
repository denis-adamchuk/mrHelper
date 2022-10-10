namespace mrHelper.Integration.DiffTool
{
   public interface IIntegratedDiffTool
   {
      string GetToolCommandArguments();

      string GetToolName();

      string GetInstallLocation();

      string GetToolCommand();

      void PatchToolConfig(string launchCommand);
   }

}

