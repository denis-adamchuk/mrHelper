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
      public Task<GitClient> GetClient(string path, string hostName, string projectName)
      {
         Key key = new Key{ HostName = hostName, ProjectName = projectName };
         if (Clients.ContainsKey(key))
         {
            return Clients[key];
         }

         GitClient client = isCloneNeeded() : new GitClient() : new GitClient(path);
         Clients[key] = client;
         return client;
      }

      /// <summary>
      /// Check if Path exists and it is a valid git repository
      /// </summary>
      private bool isCloneNeeded(string path)
      {
         return !Directory.Exists(path) || !GitClient.IsGitClient(path);
      }

      private struct Key
      {
         string HostName;
         string ProjectName;
      }
      private Dictionary<Key, GitClient> Clients { get; set; }
   }
}

