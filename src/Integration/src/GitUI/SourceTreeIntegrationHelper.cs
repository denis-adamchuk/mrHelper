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
   public class SourceTreeIntegrationHelperException : ExceptionEx
   {
      public SourceTreeIntegrationHelperException(string message)
         : base(message, null)
      {
      }
   }

   public static class SourceTreeIntegrationHelper
   {
      private static string SettingsPath = @"Atlassian\SourceTree\customactions.xml";

      public static bool IsInstalled()
      {
         // TODO WTF
         // 1. Check in AppFinder
         return true;
      }

      public static void Browse(string path)
      {
      }

      public static void AddCustomActions(string scriptPath)
      {
         string gitbash = Path.Combine(GitTools.GetBinaryFolder(), Constants.BashFileName);
         if (!File.Exists(gitbash))
         {
            throw new GitExtensionsIntegrationHelperException("Cannot find git bash");
         }

         string roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
         string configFilePath = Path.Combine(roamingPath, SettingsPath);
         string configFolder = Path.GetDirectoryName(configFilePath);
         if (!Directory.Exists(configFolder))
         {
            throw new GitExtensionsIntegrationHelperException("Cannot find a folder for Source Tree settings");
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

