using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Script.Serialization;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   public class ServiceManager
   {
      public ServiceManager()
      {
         // Check if file exists. If it does not, it is not an error.
         if (System.IO.File.Exists(ServiceListFileName))
         {
            try
            {
               _services = JsonFileReader.LoadFromFile<Service[]>(ServiceListFileName);
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle("Cannot load services from file", ex);
            }
         }
      }

      public string GetHelpUrl()
      {
         int index = _services == null ? -1 : Array.FindIndex(_services, x => x.Name == "Help");
         if (index == -1)
         {
            Trace.TraceWarning(String.Format("[ServiceManager] Help entry is missing"));
            return String.Empty;
         }

         Dictionary<string, object> properties = _services[index].Properties;
         return properties != null && properties.ContainsKey("url") ? properties["url"].ToString() : String.Empty;
      }

      public string GetBugReportEmail()
      {
         int index = _services == null ? -1 : Array.FindIndex(_services, x => x.Name == "BugReport");
         if (index == -1)
         {
            Trace.TraceWarning(String.Format("[ServiceManager] BugReport entry is missing"));
            return String.Empty;
         }

         Dictionary<string, object> properties = _services[index].Properties;
         return properties != null && properties.ContainsKey("email") ? properties["email"].ToString() : String.Empty;
      }

      public string GetJiraServiceUrl()
      {
         int index = _services == null ? -1 : Array.FindIndex(_services, x => x.Name == "Jira");
         if (index == -1)
         {
            Trace.TraceWarning(String.Format("[ServiceManager] Jira entry is missing"));
            return String.Empty;
         }

         Dictionary<string, object> properties = _services[index].Properties;
         return properties != null && properties.ContainsKey("url") ? properties["url"].ToString() : String.Empty;
      }

      public string GetServiceMessageUsername()
      {
         int index = _services == null ? -1 : Array.FindIndex(_services, x => x.Name == "ServiceMessages");
         if (index == -1)
         {
            Trace.TraceWarning(String.Format("[ServiceManager] ServiceMessages entry is missing"));
            return String.Empty;
         }

         Dictionary<string, object> properties = _services[index].Properties;
         return properties != null && properties.ContainsKey("username") ? properties["username"].ToString() : String.Empty;
      }

      public struct LatestVersionInformation
      {
         public string VersionNumber;
         public string InstallerFilePath;
      }

      public LatestVersionInformation? GetLatestVersionInfo()
      {
         int index = _services == null ? -1 : Array.FindIndex(_services, x => x.Name == "CheckForUpdates");
         if (index == -1)
         {
            Trace.TraceWarning(String.Format("[ServiceManager] CheckForUpdates entry is missing"));
            return null;
         }

         Dictionary<string, object> properties = _services[index].Properties;
         string path = properties != null && properties.ContainsKey("latest_version_info") ?
            properties["latest_version_info"].ToString() : String.Empty;

         if (path == String.Empty)
         {
            Trace.TraceWarning(String.Format("[ServiceManager] latest_version_info field is empty"));
            return null;
         }

         if (!System.IO.File.Exists(path))
         {
            Trace.TraceWarning(String.Format("[ServiceManager] Cannot find file \"{0}\"", path));
            return null;
         }

         string json = System.IO.File.ReadAllText(path);
         JavaScriptSerializer serializer = new JavaScriptSerializer();

         try
         {
            return serializer.Deserialize<LatestVersionInformation>(json);
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle("Cannot deserialize JSON ", ex);
         }
         return null;
      }

#pragma warning disable 0649
      private struct Service
      {
         public string Name;
         public Dictionary<string, object> Properties;
      }
#pragma warning restore 0649

      private readonly Service[] _services = Array.Empty<Service>();

      private const string ServiceListFileName = "services.json";
   }
}

