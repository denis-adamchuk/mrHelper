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
         return _services?.Single((x) => x.Name == "Jira")?.Properties["Url"]?.ToString() ?? String.Empty;
      }

      private class Service
      {
         public string Name = null;
         public Dictionary<string, object> Properties = null;
      }

      private List<Service> _services;

      private const string ServiceListFileName = "services.json";
   }
}

