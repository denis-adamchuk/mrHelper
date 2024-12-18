﻿using System;

namespace mrHelper.Integration.DiffTool
{
   public class BC3Tool : BCTool
   {
      public static string Name => "BC3";

      protected override string getIntegrationKey() => "mrhelper-bc3-integration";
      protected override string getDefaultShortcut() => "32843"; // Alt-K
      protected override string getLaunchArguments() => " %22%25f1%22 %l1 %22%25f2%22";

      protected override string getToolName() => "Beyond Compare 3";
      protected override string[] getToolRegistryNames() => new string[]
         { getToolName(), "Beyond Compare Version 3", "Beyond Compare version 3" };
      protected override string getToolCompanyName() => "Scooter Software";
      protected override string getConfigFileName() => "BCPreferences.xml";
      protected override string getConfigFileComment() =>
         String.Format(" Produced by {0} from {1} ", getToolName(), getToolCompanyName());
      protected override string getConfigVersion() => "";
   }
}

