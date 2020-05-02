using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace mrHelper.Integration
{
   public class CustomProtocol
   {
      private readonly string ProtocolName;

      public CustomProtocol(string protocolName)
      {
         ProtocolName = protocolName;
      }

      public void RegisterInRegistry(string protocolDescription, Dictionary<string, string> commands, string icon)
      {
         RegistryKey hcr = Registry.ClassesRoot;
         RegistryKey appSubKey = hcr.CreateSubKey(ProtocolName);
         appSubKey.SetValue(String.Empty /* Default */, String.Format("URL: {0}", protocolDescription));
         appSubKey.SetValue("URL Protocol", String.Empty);

         RegistryKey defaultIconSubKey = hcr.CreateSubKey(
            String.Format(@"{0}\DefaultIcon", ProtocolName));
         defaultIconSubKey.SetValue(String.Empty /* Default */, icon);

         foreach (KeyValuePair<string, string> command in commands)
         {
            string cmdName = command.Key;
            RegistryKey commandSubKey = hcr.CreateSubKey(
               String.Format(@"{0}\shell\{1}\command", ProtocolName, cmdName));
            commandSubKey.SetValue(String.Empty /* Default */, command.Value);
         }
      }

      public void RemoveFromRegistry()
      {
         RegistryKey hcr = Registry.ClassesRoot;
         hcr.DeleteSubKeyTree(ProtocolName);
      }
   }
}

