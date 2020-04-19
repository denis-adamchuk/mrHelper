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
         ISynchronizeInvoke synchronizeInvoke, bool useShallowClone)
      {
         if (!Directory.Exists(parentFolder))
         {
            throw new ArgumentException("Bad parent folder \"" + parentFolder + "\"");
         }

         ParentFolder = parentFolder;
         _projectWatcher = projectWatcher;
         _synchronizeInvoke = synchronizeInvoke;
         _useShallowClone = useShallowClone;

         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryFactory] Created LocalGitRepositoryFactory for parentFolder {0}", parentFolder));
      }

      /// <summary>
      /// Create a LocalGitRepository object or return it if already cached.
      /// Throws if
      /// </summary>
      public ILocalGitRepository GetRepository(string hostName, string projectName)
      {
         ProjectKey key = new ProjectKey
         {
            HostName = hostName,
            ProjectName = projectName
         };

         if (_repos.TryGetValue(key, out LocalGitRepository cachedRepository))
         {
            return cachedRepository;
         }

         LocalGitRepository repo;
         try
         {
            string path = LocalGitRepositoryPathFinder.FindPath(ParentFolder, key);
            repo = new LocalGitRepository(key, path, _projectWatcher, _synchronizeInvoke, _useShallowClone);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot create LocalGitRepository", ex);
            return null;
         }
         _repos[key] = repo;
         return repo;
      }

      async public Task DisposeAsync()
      {
         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryFactory] Disposing LocalGitRepositoryFactory for parentFolder {0}", ParentFolder));

         // It is safer to clean-up a copy asynchronously
         Dictionary<ProjectKey, LocalGitRepository> repos = _repos.ToDictionary(x => x.Key, x => x.Value);
         _repos.Clear();
         await Task.WhenAll(repos.Values.Select(x => x.DisposeAsync()).ToArray());
      }

      private readonly Dictionary<ProjectKey, LocalGitRepository> _repos =
         new Dictionary<ProjectKey, LocalGitRepository>();
      private readonly IProjectWatcher _projectWatcher;
      private readonly ISynchronizeInvoke _synchronizeInvoke;

      private readonly bool _useShallowClone;
   }
}

