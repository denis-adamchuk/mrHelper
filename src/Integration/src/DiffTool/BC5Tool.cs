using System;

namespace mrHelper.Integration.DiffTool
{
   public class BC5Tool : BCTool
   {
      public static string Name => "BC5";

      protected override string getIntegrationKey() => "mrhelper-bc5-integration";
      protected override string getDefaultShortcut() => "32843"; // Alt-K
      protected override string getLaunchArguments() => " \"%f1\" %l1 \"%f2\"";

      protected override string getToolName() => "Beyond Compare 5";
      protected override string[] getToolRegistryNames() => new string[]
         { getToolName(), "Beyond Compare Version 5", "Beyond Compare version 5" };
      protected override string getToolCompanyName() => "Scooter Software";
      protected override string getConfigFileName() => "BCPreferences.xml";
      protected override string getConfigFileComment() =>
         String.Format(" Produced by {0} from {1} ", getToolName(), getToolCompanyName());
      protected override string getConfigVersion() => "2";
   }
}

