using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;

namespace mrHelper.Integration.GitUI
{
   public class GitExtensionsIntegrationHelperException : ExceptionEx
   {
      public GitExtensionsIntegrationHelperException(string message)
         : base(message, null)
      {
      }
   }

   public static class GitExtensionsIntegrationHelper
   {
      private static readonly string SettingsPath = @"GitExtensions/GitExtensions/GitExtensions.settings";

      private static readonly string BinaryFileName = "gitex.cmd";

      public static bool IsInstalled()
      {
         IEnumerable<string> whereOutput = ExternalProcess.Start("where", BinaryFileName, true, ".").StdOut;
         return whereOutput != null
             && whereOutput.Any()
             && !whereOutput.First().Contains("Could not find files for the given pattern(s)");
      }

      public static void Browse(string path)
      {
         if (IsInstalled())
         {
            ExternalProcess.Start(BinaryFileName,
               String.Format("browse {0}", StringUtils.EscapeSpaces(path)), false, ".");
         }
      }

      public static void AddCustomActions(string scriptPath)
      {
         string roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
         string configFilePath = Path.Combine(roamingPath, SettingsPath);
         if (!File.Exists(configFilePath))
         {
            throw new GitExtensionsIntegrationHelperException("Cannot find Git Extensions configuration file");
         }

         string gitbash = GitTools.GetGitBashPath();
         if (!File.Exists(gitbash))
         {
            throw new GitExtensionsIntegrationHelperException("Cannot find git bash");
         }

         // load XML from disk
         XDocument document = XDocument.Load(configFilePath);

         // find a placeholder for scripts
         XElement ownScripts = document?
            .Descendants("item")
            .FirstOrDefault(x => x.Descendants("string").Any(y => y.Value == "ownScripts"));
         XElement ownScriptsValue = ownScripts?.Element("value")?.Element("string");
         if (ownScriptsValue == null)
         {
            throw new GitExtensionsIntegrationHelperException("Unexpected format of Git Extensions configuration file");
         }

         // deserialize XML
         XDocument scripts = XDocument.Parse(ownScriptsValue.Value);
         if (scripts == null)
         {
            throw new GitExtensionsIntegrationHelperException("Unexpected format of Git Extensions configuration file");
         }

         Debug.Assert(document != null);
         string name = Constants.CreateMergeRequestCustomActionName;

         // delete previously added element
         IntegrationHelper.DeleteElements(scripts, "ScriptInfo", "Name", new string[] { name });

         // add element
         int maxHotKeyNumber = getCurrentMaximumHotKeyNumber(scripts);
         XElement[] elements = new XElement[] { getCreateMergeRequestElement(name, scriptPath, maxHotKeyNumber, gitbash) };
         if (!IntegrationHelper.AddElements(scripts, "ArrayOfScriptInfo", elements))
         {
            throw new GitExtensionsIntegrationHelperException("Unexpected format of Git Extensions configuration file");
         }

         // serialize XML and save to disk
         ownScriptsValue.Value = scripts.ToString();
         document.Save(configFilePath);
      }

      private static int getCurrentMaximumHotKeyNumber(XDocument ownScripts)
      {
         Debug.Assert(ownScripts != null);
         int lowestHotKeyId = 9000; // Git Extensions specific
         IEnumerable<XElement> hotKeyElements = ownScripts.Descendants("HotkeyCommandIdentifier");
         return hotKeyElements.Any()
            ? hotKeyElements.Max(i => Int32.TryParse(i.Value, out int number) ? number : lowestHotKeyId - 1)
            : lowestHotKeyId - 1;
      }

      private static XElement getCreateMergeRequestElement(string name, string scriptPath,
         int maxHotKeyNumber, string gitBashFilePath)
      {
         string scriptFilePath = Path.Combine(scriptPath, Constants.CreateMergeRequestBashScriptName);
         if (!File.Exists(scriptFilePath))
         {
            string error = String.Format("Cannot find \"{0}\" script at \"{1}\"",
               Constants.CreateMergeRequestBashScriptName, scriptPath);
            throw new GitExtensionsIntegrationHelperException(error);
         }

         string arguments = string.Format("\"{0}\" {{sHashes}}", scriptFilePath);
         return new XElement("ScriptInfo",
            new XElement("Name", name),
            new XElement("Arguments", arguments),
            new XElement("Command", gitBashFilePath),
            new XElement("Enabled", "true"),
            new XElement("Icon", "BranchLocal"),
            new XElement("AddToRevisionGridContextMenu", "true"),
            new XElement("OnEvent", "ShowInUserMenuBar"),
            new XElement("AskConfirmation", "false"),
            new XElement("RunInBackground", "false"),
            new XElement("HotkeyCommandIdentifier", ++maxHotKeyNumber));
      }
   }
}

