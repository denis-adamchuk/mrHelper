using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;

namespace mrHelper.Integration.GitUI
{
   public class SourceTreeIntegrationHelperException : ExceptionEx
   {
      public SourceTreeIntegrationHelperException(string message)
         : base(message, null)
      {
      }
   }

   public static class SourceTreeIntegrationHelper
   {
      private static readonly string SettingsPath = @"Atlassian\SourceTree\customactions.xml";

      private static readonly string RegistryDisplayName = "SourceTree";

      private static readonly string BinaryFileName = "SourceTree.exe";

      public static bool IsInstalled()
      {
         return getBinaryFilePath() != null;
      }

      public static void Browse(string path)
      {
         if (IsInstalled())
         {
            ExternalProcess.Start(getBinaryFilePath(),
               String.Format("-f {0}", StringUtils.EscapeSpaces(path)), false, ".");
         }
      }

      private static string getBinaryFilePath()
      {
         AppFinder.AppInfo appInfo = AppFinder.GetApplicationInfo(new string[] { RegistryDisplayName });
         return appInfo != null && !String.IsNullOrWhiteSpace(appInfo.InstallPath)
            ? Path.Combine(appInfo.InstallPath, BinaryFileName) : null;
      }

      public static void AddCustomActions(string scriptPath)
      {
         string gitbash = Path.Combine(GitTools.GetBinaryFolder(), Constants.BashFileName);
         if (!File.Exists(gitbash))
         {
            throw new SourceTreeIntegrationHelperException("Cannot find git bash");
         }

         string roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
         string configFilePath = Path.Combine(roamingPath, SettingsPath);
         string configFolder = Path.GetDirectoryName(configFilePath);
         if (!Directory.Exists(configFolder))
         {
            throw new SourceTreeIntegrationHelperException("Cannot find a folder for Source Tree settings");
         }

         // load XML from disk
         XDocument scripts = File.Exists(configFilePath)
            ? XDocument.Load(configFilePath) : new XDocument(new XElement("ArrayOfCustomAction"));

         Debug.Assert(scripts != null);
         string name = Constants.CreateMergeRequestCustomActionName;

         // delete previously added element
         IntegrationHelper.DeleteElements(scripts, "CustomAction", "Caption", new string[] { name });

         // add element
         XElement[] elements = new XElement[] { getCreateMergeRequestElement(name, scriptPath, gitbash) };
         if (!IntegrationHelper.AddElements(scripts, "ArrayOfCustomAction", elements))
         {
            throw new SourceTreeIntegrationHelperException("Unexpected format of Source Tree configuration file");
         }

         // save to disk
         scripts.Save(configFilePath);
      }

      private static XElement getCreateMergeRequestElement(string name, string scriptPath, string gitBashFilePath)
      {
         string scriptFilePath = Path.Combine(scriptPath, Constants.CreateMergeRequestBashScriptName);
         if (!File.Exists(scriptFilePath))
         {
            string error = String.Format("Cannot find \"{0}\" script at \"{1}\"",
               Constants.CreateMergeRequestBashScriptName, scriptPath);
            throw new SourceTreeIntegrationHelperException(error);
         }

         string arguments = string.Format("\"{0}\" $SHA", scriptFilePath);
         return new XElement("CustomAction",
                new XElement("Target", gitBashFilePath),
                new XElement("OpenInSeparateWindow", "false"),
                new XElement("ShowFullOutput", "true"),
                new XElement("Caption", "Create Merge Request"),
                new XElement("Parameters", arguments));
      }
   }
}

