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
      public GitClient GetClient(string path, string hostName, string projectName, bool needUpdates = true)
      {
         Key key = new Key{ HostName = hostName, ProjectName = projectName };
         if (Clients.ContainsKey(key))
         {
            return Clients[key];
         }

         GitClient client = isCloneNeeded(path) ? new GitClient() : new GitClient(path, needUpdates);
         Clients[key] = client;
         return client;
      }

      /// <summary>
      /// Check if Path exists and it is a valid git repository
      /// </summary>
      private bool isCloneNeeded(string path)
      {
         return !System.IO.Directory.Exists(path) || !GitClient.IsGitClient(path);
      }

      private struct Key
      {
         public string HostName;
         public string ProjectName;
      }
      private Dictionary<Key, GitClient> Clients { get; set; }
   }
}

