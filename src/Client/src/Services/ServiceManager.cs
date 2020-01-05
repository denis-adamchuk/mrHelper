using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Services
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
               _services = CommonTools.JsonFileReader.LoadFromFile<List<Service>>(ServiceListFileName);
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle(ex, "Cannot load services from file");
            }
         }
      }

      public string GetHelpUrl()
      {
         int index = _services?.FindIndex(x => x.Name == "Help") ?? -1;
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
         int index = _services?.FindIndex(x => x.Name == "BugReport") ?? -1;
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
         int index = _services?.FindIndex(x => x.Name == "Jira") ?? -1;
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
         int index = _services?.FindIndex(x => x.Name == "ServiceMessages") ?? -1;
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
         int index = _services?.FindIndex(x => x.Name == "CheckForUpdates") ?? -1;
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
            ExceptionHandlers.Handle(ex, "Cannot deserialize JSON ");
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

      private readonly List<Service> _services = new List<Service>();

      private const string ServiceListFileName = "services.json";
   }
}

