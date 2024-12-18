﻿using System;

namespace mrHelper.Integration.DiffTool
{
   public class BC4PortableTool : BCTool
   {
      public BC4PortableTool(string location)
      {
         Location = location;
      }

      public static string Name => "BC4Portable";

      protected override string getIntegrationKey() => "mrhelper-bc4-integration";
      protected override string getDefaultShortcut() => "32843"; // Alt-K
      protected override string getLaunchArguments() => " %22%25f1%22 %l1 %22%25f2%22";

      protected override string getToolName() => "Beyond Compare 4";
      protected override string[] getToolRegistryNames() => new string[]
         { getToolName(), "Beyond Compare Version 4", "Beyond Compare version 4" };
      protected override string getToolCompanyName() => "Scooter Software";
      protected override string getConfigFileName() => "BCPreferences.xml";
      protected override string getConfigFileComment() =>
         String.Format(" Produced by {0} from {1} ", getToolName(), getToolCompanyName());
      protected override string getInstallLocation() => Location;
      protected override string getConfigFilePath() => System.IO.Path.Combine(Location, getConfigFileName());
      protected override string getConfigVersion() => "1";

      private string Location { get; }
   }
}

