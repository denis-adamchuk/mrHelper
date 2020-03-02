using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitClient
{
   ///<summary>
   /// Creates LocalGitRepository objects.
   ///<summary>
   public class LocalGitRepositoryFactory : ILocalGitRepositoryFactory
   {
      public string ParentFolder { get; }

      /// <summary>
      /// Create a factory
      /// Throws ArgumentException if passed ParentFolder does not exist
      /// </summary>
      public LocalGitRepositoryFactory(string parentFolder, IProjectWatcher projectWatcher,
         ISynchronizeInvoke synchronizeInvoke)
      {
         if (!Directory.Exists(parentFolder))
         {
            throw new ArgumentException("Bad parent folder \"" + parentFolder + "\"");
         }

         ParentFolder = parentFolder;
         _projectWatcher = projectWatcher;
         _synchronizeInvoke = synchronizeInvoke;

         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryFactory] Created LocalGitRepositoryFactory for parentFolder {0}", parentFolder));
      }

      /// <summary>
      /// Create a LocalGitRepository object or return it if already cached.
      /// Throws if
      /// </summary>
      public ILocalGitRepository GetRepository(string hostName, string projectName)
      {
         string[] splitted = projectName.Split('/');
         if (splitted.Length < 2)
         {
            throw new ArgumentException("Bad project name \"" + projectName + "\"");
         }

         string path = Path.Combine(ParentFolder, splitted[1]);

         ProjectKey key = new ProjectKey{ HostName = hostName, ProjectName = projectName };
         if (!_repos.ContainsKey(key))
         {
            LocalGitRepository repo;
            try
            {
               repo = new LocalGitRepository(key, path, _projectWatcher, _synchronizeInvoke);
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle("Cannot create LocalGitRepository", ex);
               return null;
            }
            _repos[key] = repo;
         }
         return _repos[key];
      }

      async public Task DisposeProjectAsync(string hostName, string projectName)
      {
         ProjectKey key = new ProjectKey{ HostName = hostName, ProjectName = projectName };
         if (!_repos.ContainsKey(key))
         {
            return;
         }
         await _repos[key].DisposeAsync();
         _repos.Remove(key);
      }

      async public Task DisposeAsync()
      {
         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryFactory] Disposing LocalGitRepositoryFactory for parentFolder {0}", ParentFolder));

         await Task.WhenAll(_repos.Values.Select(x => x.DisposeAsync()).ToArray());
         _repos.Clear();
      }

      private readonly Dictionary<ProjectKey, LocalGitRepository> _repos =
         new Dictionary<ProjectKey, LocalGitRepository>();
      private readonly IProjectWatcher _projectWatcher;
      private readonly ISynchronizeInvoke _synchronizeInvoke;
   }
}

