using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using mrHelper.Common.Tools;

namespace mrHelper.Integration.DiffTool
{
   public abstract class BCTool : IIntegratedDiffTool
   {
      public string GetToolCommand()
      {
         return Command;
      }

      public string GetToolCommandArguments()
      {
         return " //solo //expandall \\\"$LOCAL\\\" \\\"$REMOTE\\\"";
      }

      public string GetToolName()
      {
         return getToolName();
      }

      public string GetInstallLocation()
      {
         return getInstallLocation();
      }

      public static string Command => "BCompare.exe";

      /// <summary>
      /// Adds a command to launch MRHelper to Beyond Compare 3/4 preferences file
      /// Throws DiffToolIntegrationException
      /// </summaryArgument>
      public void PatchToolConfig(string launchCommand)
      {
         string configFilePath = getConfigFilePath();

         bool createDocument = true;
         XmlDocument document = new XmlDocument();
         if (System.IO.File.Exists(configFilePath))
         {
            try
            {
               document.Load(configFilePath);
               createDocument = false;
            }
            catch (XmlException xe)
            {
               try
               {
                  System.IO.File.Delete(configFilePath);
               }
               catch (Exception)
               {
                  throw xe;
               }
            }
         }

         if (createDocument)
         {
            if (!System.IO.Directory.Exists(getConfigPath()))
            {
               System.IO.Directory.CreateDirectory(getConfigPath());
               Trace.TraceInformation(String.Format("Created directory {0}", getConfigPath()));
            }

            XmlNode docNode = document.CreateXmlDeclaration("1.0", "UTF-8", null);
            document.AppendChild(docNode);

            XmlNode comment = document.CreateComment(getConfigFileComment());
            document.AppendChild(comment);
         }

         string fullCommand = launchCommand + getLaunchArguments();
         XmlNode openWiths = getAllApplicationRecords(document);
         XmlNode appRecord = findOurApplicationRecord(openWiths);
         if (appRecord != null)
         {
            XmlNode currentCmdLine = appRecord.SelectSingleNode("CmdLine");
            XmlNode currentShortCut = appRecord.SelectSingleNode("ShortCut");
            Debug.Assert(currentCmdLine != null && currentShortCut != null);

            ((XmlElement)currentCmdLine).SetAttribute("Value", fullCommand);
            ((XmlElement)currentShortCut).SetAttribute("Value", getDefaultShortcut());

            document.Save(configFilePath);

            Trace.TraceInformation(String.Format(
               "Updated \"{0}\" file. CmdLine=\"{1}\". ShortCut=\"{2}\"",
               configFilePath, fullCommand, getDefaultShortcut()));
            return;
         }

         int id = getAllApplicationRecordIds(openWiths).DefaultIfEmpty(-1 /* to obtain Id = 0 */).Max() + 1;
         XmlNode newNode = document.CreateElement("_" + id.ToString());

         XmlElement cmdLine = document.CreateElement("CmdLine");
         cmdLine.SetAttribute("Value", fullCommand);

         XmlElement description = document.CreateElement("Description");
         description.SetAttribute("Value", getIntegrationKey());

         XmlElement shortcut = document.CreateElement("ShortCut");
         shortcut.SetAttribute("Value", getDefaultShortcut());

         newNode.AppendChild(cmdLine);
         newNode.AppendChild(description);
         newNode.AppendChild(shortcut);
         openWiths.AppendChild(newNode);

         document.Save(configFilePath);

         Trace.TraceInformation(String.Format(
            "Patched \"{0}\" file. CmdLine=\"{1}\". Description=\"{2}\". ShortCut=\"{3}\"",
            configFilePath, fullCommand, getIntegrationKey(), getDefaultShortcut()));
      }

      /// <summary>
      /// Throws DiffToolIntegrationException
      /// </summaryArgument>
      private static XmlNode getAllApplicationRecords(XmlDocument document)
      {
         XmlNode root = document.SelectSingleNode("BCPreferences");
         if (root == null)
         {
            if (document.DocumentElement != null)
            {
               throw new DiffToolIntegrationException(String.Format(
                  "Wrong root element. Must be \"BCPreferences\" node."), null);
            }

            root = document.CreateElement("BCPreferences");
            if (root == null)
            {
               throw new DiffToolIntegrationException(String.Format(
                  "Cannot create \"BCPreferences\" node"), null);
            }
            document.AppendChild(root);
         }

         XmlNode tbcPrefs = root.SelectSingleNode("TBcPrefs");
         if (tbcPrefs == null)
         {
            tbcPrefs = document.CreateElement("TBcPrefs");
            if (tbcPrefs == null)
            {
               throw new DiffToolIntegrationException(String.Format(
                  "Cannot create \"TBcPrefs\" node"), null);
            }
            root.AppendChild(tbcPrefs);
         }

         XmlNode openWiths = tbcPrefs.SelectSingleNode("OpenWiths");
         if (openWiths == null)
         {
            openWiths = document.CreateElement("OpenWiths");
            if (openWiths == null)
            {
               throw new DiffToolIntegrationException(String.Format(
                  "Cannot create \"OpenWiths\" node"), null);
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

      private XmlNode findOurApplicationRecord(XmlNode openWiths)
      {
         // check if we already integrated
         foreach (XmlNode child in openWiths.ChildNodes)
         {
            XmlNode currentDescription = child.SelectSingleNode("Description");
            if (currentDescription != null)
            {
               XmlNode value = currentDescription.Attributes.GetNamedItem("Value");
               if (value.Value == getIntegrationKey())
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

      public bool isInstalled(string toolpath)
      {
         return !String.IsNullOrEmpty(toolpath);
      }

      protected abstract string getIntegrationKey();
      protected abstract string getDefaultShortcut();
      protected abstract string getLaunchArguments();

      protected abstract string getToolName();
      protected abstract string[] getToolRegistryNames();
      protected abstract string getToolCompanyName();
      protected abstract string getConfigFileName();
      protected abstract string getConfigFileComment();
      protected virtual string getInstallLocation()
      {
         AppFinder.AppInfo appInfo = AppFinder.GetApplicationInfo(getToolRegistryNames());
         return appInfo != null && isInstalled(appInfo.InstallPath) ? appInfo.InstallPath : null;
      }
      protected virtual string getConfigPath()
      {
         string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         return System.IO.Path.Combine(appData, getToolCompanyName(), getToolName());
      }
      protected virtual string getConfigFilePath()
      {
         return System.IO.Path.Combine(getConfigPath(), getConfigFileName());
      }
   }
}

