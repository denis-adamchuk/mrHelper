namespace mrDiffTool
{

   public interface IntegratedDiffTool
   {
      string GetToolCommandArguments();

      string GetToolName();

      string GetToolCommand();

      void PatchToolConfig(string launchCommand);
   }

}
