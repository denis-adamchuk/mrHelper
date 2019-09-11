using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using mrHelper.Client.Git;
using mrHelper.Client.Updates;

namespace mrHelper.Client.Git
{
   ///<summary>
   /// Creates GitClient objects.
   /// This factory is helpful because GitClient objects may have internal state that is expensive to fill up.
   ///<summary>
   public class GitClientFactory : IDisposable
   {
      public string ParentFolder { get; }

      /// <summary>
      /// Create a factory
      /// Throws ArgumentException if passed ParentFolder does not exist
      /// </summary>
      public GitClientFactory(string parentFolder, IProjectWatcher projectWatcher)
      {
         if (!Directory.Exists(parentFolder))
         {
            throw new ArgumentException("Bad \"" + parentFolder + "\" argument");
         }

         ParentFolder = parentFolder;
         ProjectWatcher = projectWatcher;

         Trace.TraceInformation(String.Format("[GitClientFactory] Created GitClientFactory for parentFolder {0}",
            parentFolder));
      }

      /// <summary>
      /// Create a GitClient object or return it if already cached.
      /// Throws if 
      /// </summary>
      public GitClient GetClient(string hostName, string projectName)
      {
         string path = Path.Combine(ParentFolder, projectName.Split('/')[1]);

         Key key = new Key{ HostName = hostName, ProjectName = projectName };
         if (Clients.ContainsKey(key))
         {
            return Clients[key];
         }

         GitClient client = new GitClient(hostName, projectName, path, ProjectWatcher);
         Clients[key] = client;
         return client;
      }

      public void Dispose()
      {
         Trace.TraceInformation(String.Format("[GitClientFactory] Disposing GitClientFactory for parentFolder {0}",
            ParentFolder));
         disposeClients();
      }

      private void disposeClients()
      {
         foreach (KeyValuePair<Key, GitClient> client in Clients)
         {
            client.Value.Dispose();
         }
         Clients.Clear();
      }

      private struct Key
      {
         public string HostName;
         public string ProjectName;
      }
      private Dictionary<Key, GitClient> Clients { get; set; } = new Dictionary<Key, GitClient>();

      private IProjectWatcher ProjectWatcher { get; }
   }
}

