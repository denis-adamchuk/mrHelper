using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;

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
               _services = Tools.Tools.LoadListFromFile<Service>(ServiceListFileName);
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle(ex, "Cannot load services from file");
            }
         }
      }

      public string GetJiraServiceUrl()
      {
         int index = _services?.FindIndex((x) => x.Name == "Jira") ?? -1;
         if (index == -1)
         {
            return String.Empty;
         }

         Dictionary<string, object> properties = _services[index].Properties;
         return properties != null && properties.ContainsKey("url") ? properties["url"].ToString() : String.Empty;
      }

#pragma warning disable 0649
      private struct Service
      {
         public string Name;
         public Dictionary<string, object> Properties;
      }
#pragma warning restore 0649

      private List<Service> _services = new List<Service>();

      private const string ServiceListFileName = "services.json";
   }
}

