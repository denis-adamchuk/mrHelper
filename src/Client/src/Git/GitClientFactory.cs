using System;
using System.Collections.Generic;
using mrHelper.Client.Git;

namespace mrHelper.Client.Git
{
   ///<summary>
   /// Creates GitClient objects.
   /// This factory is helpful because GitClient objects may have internal state that is expensive to fill up.
   ///<summary>
   public class GitClientFactory
   {
      /// <summary>
      /// Create a GitClient object or return it if already cached.
      /// </summary>
      public GitClient GetClient(string path, string hostName, string projectName, bool enableUpdates = true)
      {
         Key key = new Key{ HostName = hostName, ProjectName = projectName };
         if (Clients.ContainsKey(key))
         {
            return Clients[key];
         }

         GitClient client = new GitClient(hostName, projectName, path);
         Clients[key] = client;
         return client;
      }

      private struct Key
      {
         public string HostName;
         public string ProjectName;
      }
      private Dictionary<Key, GitClient> Clients { get; set; } = new Dictionary<Key, GitClient>();
   }
}

