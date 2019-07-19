namespace mrDiffTool
{

   public interface IntegratedDiffTool
   {
      string GetToolCommandArguments();

      string[] GetToolNames();

      string GetToolCommand();

      void PatchToolConfig(string launchCommand);
   }

}
