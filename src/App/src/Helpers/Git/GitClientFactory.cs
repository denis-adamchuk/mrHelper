using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using mrHelper.Client.Types;
using mrHelper.Client.MergeRequests;

namespace mrHelper.App.Helpers
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
      public GitClientFactory(string parentFolder, IProjectWatcher projectWatcher, ISynchronizeInvoke synchronizeInvoke)
      {
         if (!Directory.Exists(parentFolder))
         {
            throw new ArgumentException("Bad parent folder \"" + parentFolder + "\"");
         }

         ParentFolder = parentFolder;
         _projectWatcher = projectWatcher;
         _synchronizeInvoke = synchronizeInvoke;

         Trace.TraceInformation(String.Format("[GitClientFactory] Created GitClientFactory for parentFolder {0}",
            parentFolder));
      }

      /// <summary>
      /// Create a GitClient object or return it if already cached.
      /// Throws if
      /// </summary>
      public GitClient GetClient(string hostName, string projectName)
      {
         string[] splitted = projectName.Split('/');
         if (splitted.Length < 2)
         {
            throw new ArgumentException("Bad project name \"" + projectName + "\"");
         }

         string path = Path.Combine(ParentFolder, splitted[1]);

         ProjectKey key = new ProjectKey{ HostName = hostName, ProjectName = projectName };
         if (_clients.ContainsKey(key))
         {
            return _clients[key];
         }

         GitClient client = new GitClient(key, path, _projectWatcher, _synchronizeInvoke);
         _clients[key] = client;
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
         foreach (KeyValuePair<ProjectKey, GitClient> client in _clients)
         {
            client.Value.Dispose();
         }
         _clients.Clear();
      }

      private readonly Dictionary<ProjectKey, GitClient> _clients = new Dictionary<ProjectKey, GitClient>();
      private readonly IProjectWatcher _projectWatcher;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
   }
}

