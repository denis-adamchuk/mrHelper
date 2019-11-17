using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.DiffTool
{
   public class BC3Tool : IIntegratedDiffTool
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

      public string[] GetToolRegistryNames()
      {
         string[] names = { "Beyond Compare 3", "Beyond Compare Version 3"};
         return names;
      }

      /// <summary>
      /// Adds a command to launch MRHelper to Beyond Compare 3 preferences file
      /// Throws DiffToolIntegrationException
      /// Throws exceptions related to XML parsing
      /// </summaryArgument>
      public void PatchToolConfig(string launchCommand)
      {
         var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         var bcFolder = System.IO.Path.Combine(appData, "Scooter Software", "Beyond Compare 3");
         var prefs = System.IO.Path.Combine(bcFolder, "BCPreferences.xml");
         if (!System.IO.File.Exists(prefs))
         {
            throw new DiffToolIntegrationException(String.Format("File is missing: \"{0}\"", prefs));
         }

         string integrationKey = "mrhelper-bc3-integration";
         string defaultShortcut = "32843"; // Alt-K
         var arguments = " \"%25f1\" %l1 \"%25f2\"";

         XmlDocument document = new XmlDocument();
         document.Load(prefs);
         var root = document.SelectSingleNode("BCPreferences");
         if (root == null)
         {
            throw new DiffToolIntegrationException(
               String.Format("Unexpected format of preferences file \"{0}\". Missing \"BCPreferences\" node", prefs));
         }

         var tbPrefs = root.SelectSingleNode("TBcPrefs");
         if (tbPrefs == null)
         {
            throw new DiffToolIntegrationException(
               String.Format("Unexpected format of preferences file \"{0}\". Missing \"TBcPrefs\" node", prefs));
         }

         var opensWith = tbPrefs.SelectSingleNode("OpenWiths");
         if (opensWith == null)
         {
            throw new DiffToolIntegrationException(
               String.Format("Unexpected format of preferences file \"{0}\". Missing \"opensWith\" node", prefs));
         }

         // check if we already integrated
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
                     Trace.TraceWarning(
                        String.Format("\"{0}\" configuration file is already patched, but {1} is missing.",
                                      prefs, (currentCmdLine == null ? "CmdLine" : "ShortCut")));

                     opensWith.RemoveChild(child);
                     continue;
                  }

                  ((XmlElement)currentCmdLine).SetAttribute("Value", launchCommand + arguments);
                  ((XmlElement)currentShortCut).SetAttribute("Value", defaultShortcut);
                  document.Save(prefs);

                  Trace.TraceInformation(String.Format(
                     "Updated \"{0}\" file. CmdLine=\"{1}\". ShortCut=\"{2}\"",
                     prefs, launchCommand + arguments, defaultShortcut));

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

         Trace.TraceInformation(String.Format(
            "Patched \"{0}\" file. CmdLine=\"{1}\". Description=\"{2}\". ShortCut=\"{3}\"",
            prefs, cmdLine.Value, description.Value, shortcut.Value));

         document.Save(prefs);
      }

   }
}

