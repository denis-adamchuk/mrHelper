using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;
using Newtonsoft.Json;

namespace mrHelper.App.Helpers
{
   public class ServiceManager
   {
      public ServiceManager()
      {
         // Check if file exists. If it does not, it is not an error.
         string filepath = Path.Combine(Directory.GetCurrentDirectory(), ServiceListFileName);
         if (System.IO.File.Exists(filepath))
         {
            try
            {
               _services = JsonUtils.LoadFromFile<Service[]>(filepath);
            }
            catch (Exception ex) // Any exception from JsonUtils.LoadFromFile()
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
            return String.Empty;
         }

         Dictionary<string, object> properties = _services[index].Properties;
         return properties != null && properties.ContainsKey("username") ? properties["username"].ToString() : String.Empty;
      }

      public IEnumerable<string> GetUnimportantSuffices()
      {
         int index = _services == null ? -1 : Array.FindIndex(_services, x => x.Name == "CustomLabels");
         if (index == -1)
         {
            Trace.TraceWarning(String.Format("[ServiceManager] CustomLabels entry is missing"));
            return Array.Empty<string>();
         }

         Dictionary<string, object> properties = _services[index].Properties;
         if (properties == null)
         {
            Trace.TraceWarning(String.Format("[ServiceManager] CustomLabels entry has no properties"));
            return Array.Empty<string>();
         }

         if (!properties.ContainsKey("unimportant"))
         {
            Trace.TraceWarning(String.Format("[ServiceManager] List of unimportant suffices is empty"));
            return Array.Empty<string>();
         }

         List<string> result = new List<string>();
         Newtonsoft.Json.Linq.JArray arrayList = (Newtonsoft.Json.Linq.JArray)properties["unimportant"];
         foreach (Newtonsoft.Json.Linq.JToken d in arrayList)
         {
            if (d["suffix"] != null)
            {
               result.Add(d["suffix"].ToString());
            }
         }

         return result;
      }

      public string GetSourceBranchTemplate()
      {
         int index = _services == null ? -1 : Array.FindIndex(_services, x => x.Name == "SourceBranchTemplate");
         if (index == -1)
         {
            Trace.TraceWarning(String.Format("[ServiceManager] SourceBranchPrefix entry is missing"));
            return String.Empty;
         }

         Dictionary<string, object> properties = _services[index].Properties;
         return properties != null && properties.ContainsKey("value") ? properties["value"].ToString() : String.Empty;
      }

      public string GetSpecialNotePrefix()
      {
         int index = _services == null ? -1 : Array.FindIndex(_services, x => x.Name == "SpecialNotePrefix");
         if (index == -1)
         {
            Trace.TraceWarning(String.Format("[ServiceManager] SpecialNotePrefix entry is missing"));
            return String.Empty;
         }

         Dictionary<string, object> properties = _services[index].Properties;
         return properties != null && properties.ContainsKey("value") ? properties["value"].ToString() : String.Empty;
      }

      public class LatestVersionInformation
      {
         [JsonProperty]
         public string VersionNumber { get; protected set; }

         [JsonProperty]
         public string InstallerFilePath { get; protected set; }
      }

      public LatestVersionInformation GetLatestVersionInfo()
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

         try
         {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<LatestVersionInformation>(json);
         }
         catch (Exception ex) // Any exception from DeserializeObject()
         {
            ExceptionHandlers.Handle("Cannot deserialize JSON ", ex);
         }
         return null;
      }

      private struct Service
      {
         public Service(string name, Dictionary<string, object> properties)
         {
            Name = name;
            Properties = properties;
         }

         public string Name { get; }
         public Dictionary<string, object> Properties { get; }
      }

      private readonly Service[] _services = Array.Empty<Service>();

      private const string ServiceListFileName = "services.json";
   }
}

