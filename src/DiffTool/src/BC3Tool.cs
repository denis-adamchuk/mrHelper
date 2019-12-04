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
         return ToolName;
      }

      public string[] GetToolRegistryNames()
      {
         return ToolRegistryNames;
      }

      /// <summary>
      /// Adds a command to launch MRHelper to Beyond Compare 3 preferences file
      /// Throws DiffToolIntegrationException
      /// Throws exceptions related to XML parsing
      /// </summaryArgument>
      public void PatchToolConfig(string launchCommand)
      {
         string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         string configFilePath = System.IO.Path.Combine(appData, ToolCompanyName, ToolName, ConfigFileName);

         XmlDocument document = new XmlDocument();
         if (System.IO.File.Exists(configFilePath))
         {
            document.Load(configFilePath);
         }
         else
         {
            XmlNode docNode = document.CreateXmlDeclaration("1.0", "UTF-8", null);
            document.AppendChild(docNode);

            XmlNode comment = document.CreateComment(ConfigFileComment);
            document.AppendChild(comment);
         }

         string fullCommand = launchCommand + LaunchArguments;
         XmlNode openWiths = getAllApplicationRecords(document);
         XmlNode appRecord = findOurApplicationRecord(openWiths);
         if (appRecord != null)
         {
            XmlNode currentCmdLine = appRecord.SelectSingleNode("CmdLine");
            XmlNode currentShortCut = appRecord.SelectSingleNode("ShortCut");
            Debug.Assert(currentCmdLine != null && currentShortCut != null);

            ((XmlElement)currentCmdLine).SetAttribute("Value", fullCommand);
            ((XmlElement)currentShortCut).SetAttribute("Value", DefaultShortcut);

            Trace.TraceInformation(String.Format(
               "Updated \"{0}\" file. CmdLine=\"{1}\". ShortCut=\"{2}\"",
               configFilePath, fullCommand, DefaultShortcut));

            document.Save(configFilePath);
            return;
         }

         int id = getAllApplicationRecordIds(openWiths).DefaultIfEmpty(-1 /* to obtain Id = 0 */).Max() + 1;
         XmlNode newNode = document.CreateElement("_" + id.ToString());

         XmlElement cmdLine = document.CreateElement("CmdLine");
         cmdLine.SetAttribute("Value", fullCommand);

         XmlElement description = document.CreateElement("Description");
         description.SetAttribute("Value", IntegrationKey);

         XmlElement shortcut = document.CreateElement("ShortCut");
         shortcut.SetAttribute("Value", DefaultShortcut);

         newNode.AppendChild(cmdLine);
         newNode.AppendChild(description);
         newNode.AppendChild(shortcut);
         openWiths.AppendChild(newNode);

         Trace.TraceInformation(String.Format(
            "Patched \"{0}\" file. CmdLine=\"{1}\". Description=\"{2}\". ShortCut=\"{3}\"",
            configFilePath, fullCommand, IntegrationKey, DefaultShortcut));

         document.Save(configFilePath);
      }

      private static XmlNode getAllApplicationRecords(XmlDocument document)
      {
         XmlNode root = document.SelectSingleNode("BCPreferences");
         if (root == null)
         {
            if (document.DocumentElement != null)
            {
               throw new DiffToolIntegrationException(String.Format("Wrong root element. Must be \"BCPreferences\" node."));
            }

            root = document.CreateElement("BCPreferences");
            if (root == null)
            {
               throw new DiffToolIntegrationException(String.Format("Cannot create \"BCPreferences\" node"));
            }
            document.AppendChild(root);
         }

         XmlNode tbcPrefs = root.SelectSingleNode("TBcPrefs");
         if (tbcPrefs == null)
         {
            tbcPrefs = document.CreateElement("TBcPrefs");
            if (tbcPrefs == null)
            {
               throw new DiffToolIntegrationException(String.Format("Cannot create \"TBcPrefs\" node"));
            }
            root.AppendChild(tbcPrefs);
         }

         XmlNode openWiths = tbcPrefs.SelectSingleNode("OpenWiths");
         if (openWiths == null)
         {
            openWiths = document.CreateElement("OpenWiths");
            if (openWiths == null)
            {
               throw new DiffToolIntegrationException(String.Format("Cannot create \"OpenWiths\" node"));
            }
            tbcPrefs.AppendChild(openWiths);
         }

         return openWiths;
      }

      private static IEnumerable<int> getAllApplicationRecordIds(XmlNode openWiths)
      {
         return openWiths.ChildNodes
            .Cast<XmlNode>()
            .Select(x => int.TryParse(x.Name.TrimStart('_'), out int id) ? id : -1);
      }

      private static XmlNode findOurApplicationRecord(XmlNode openWiths)
      {
         // check if we already integrated
         foreach (XmlNode child in openWiths.ChildNodes)
         {
            XmlNode currentDescription = child.SelectSingleNode("Description");
            if (currentDescription != null)
            {
               XmlNode value = currentDescription.Attributes.GetNamedItem("Value");
               if (value.Value == IntegrationKey)
               {
                  // seems to be already patched but let's update it 
                  XmlNode currentCmdLine = child.SelectSingleNode("CmdLine");
                  XmlNode currentShortCut = child.SelectSingleNode("ShortCut");
                  if (currentCmdLine == null || currentShortCut == null)
                  {
                     Trace.TraceWarning(
                        String.Format("Configuration file is already patched, but {0} is missing.",
                                      (currentCmdLine == null ? "CmdLine" : "ShortCut")));

                     openWiths.RemoveChild(child);
                     continue;
                  }

                  return child;
               }
            }
         }

         return null;
      }

      private readonly static string IntegrationKey = "mrhelper-bc3-integration";
      private readonly static string DefaultShortcut = "32843"; // Alt-K
      private readonly static string LaunchArguments = " %22%25f1%22 %l1 %22%25f2%22";

      private readonly static string ToolName = "Beyond Compare 3";
      private readonly static string[] ToolRegistryNames = { ToolName, "Beyond Compare Version 3" };
      private readonly static string ToolCompanyName = "Scooter Software";
      private readonly static string ConfigFileName = "BCPreferences.xml";
      private readonly static string ConfigFileComment =
         String.Format(" Produced by {0} from {1} ", ToolName, ToolCompanyName);
   }
}

