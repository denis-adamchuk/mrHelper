using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Integration
{
   public class CustomProtocol
   {
      private readonly string ProtocolName;
      private readonly string ProtocolDescription;
      private readonly Dictionary<string, string> Commands;
      private readonly string Icon;

      public CustomProtocol(string protocolName, string protocolDescription,
         Dictionary<string, string> commands, string icon)
      {
         ProtocolName = protocolName;
         ProtocolDescription = protocolDescription;
         Commands = commands;
         Icon = icon;
      }

      public void RegisterInRegistry()
      {
         RegistryKey hcr = Registry.ClassesRoot;
         RegistryKey appSubKey = hcr.CreateSubKey(ProtocolName);
         appSubKey.SetValue(String.Empty /* Default */, ProtocolDescription);
         appSubKey.SetValue("URL Protocol", String.Empty);

         RegistryKey defaultIconSubKey = hcr.CreateSubKey(
            String.Format(@"{0}\DefaultIcon", ProtocolName));
         defaultIconSubKey.SetValue(String.Empty /* Default */, Icon);

         foreach (KeyValuePair<string, string> command in Commands)
         {
            string cmdName = command.Key;
            RegistryKey commandSubKey = hcr.CreateSubKey(
               String.Format(@"{0}\shell\{1}\command", ProtocolName, cmdName));
            commandSubKey.SetValue(String.Empty /* Default */, command.Value);
         }
      }
   }
}

