using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace mrDiffTool
{
   public class BC3Tool : IntegratedDiffTool
   {
      public string GetToolCommand()
      {
         return "BCompare.exe";
      }

      public string GetToolCommandArguments()
      {
         return " //solo //expandall \\\"$LOCAL\\\" \\\"$REMOTE\\\"";
      }

      public string GetToolName()
      {
         return "Beyond Compare 3"; 
      }

      public void PatchToolConfig(string launchCommand)
      {
         var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         var bcFolder = System.IO.Path.Combine(appData, "Scooter Software", "Beyond Compare 3");
         var prefs = System.IO.Path.Combine(bcFolder, "BCPreferences.xml");
         if (!System.IO.File.Exists(prefs))
         {
            return; } string integrationKey = "mrhelper-bc3-integration"; string defaultShortcut = "32843"; // Alt-K
         var arguments = " %25F1 %l1 %25F2 %l2";

         XmlDocument document = new XmlDocument();
         document.Load(prefs);
         var root = document.SelectSingleNode("BCPreferences");
         var tbPrefs = root.SelectSingleNode("TBcPrefs");
         var opensWith = tbPrefs.SelectSingleNode("OpenWiths");
         List<int> ids = new List<int>();
         foreach (XmlNode child in opensWith.ChildNodes)
         {
            ids.Add(int.Parse(child.Name.TrimStart('_')));
            var currentDescription = child.SelectSingleNode("Description");
            if (currentDescription != null)
            {
               var value = currentDescription.Attributes.GetNamedItem("Value");
               if (value.Value == integrationKey)
               {
                  // seems to be already patched but let's update it 
                  var currentCmdLine = child.SelectSingleNode("CmdLine");
                  var currentShortCut = child.SelectSingleNode("ShortCut");
                  if (currentCmdLine == null || currentShortCut == null)
                  {
                     // looks broken
                     opensWith.RemoveChild(child);
                     continue;
                  }

                  ((XmlElement)currentCmdLine).SetAttribute("Value", launchCommand + arguments);
                  ((XmlElement)currentShortCut).SetAttribute("Value", defaultShortcut);
                  document.Save(prefs);
                  return;
               }
            }
         }

         ids.Sort();
         int id = ids.Count > 0 ? ids.Last() + 1 : 1;
         var newNode = document.CreateElement("_" + id.ToString());

         var cmdLine = document.CreateElement("CmdLine");
         cmdLine.SetAttribute("Value", launchCommand + arguments);

         var description = document.CreateElement("Description");
         description.SetAttribute("Value", integrationKey);

         var shortcut = document.CreateElement("ShortCut");
         shortcut.SetAttribute("Value", defaultShortcut);

         newNode.AppendChild(cmdLine);
         newNode.AppendChild(description);
         newNode.AppendChild(shortcut);
         opensWith.AppendChild(newNode);

         document.Save(prefs);
      }
   }
}
