using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitClient
{
   ///<summary>
   /// Creates LocalGitRepository objects.
   ///<summary>
   public class LocalGitRepositoryFactory : ILocalGitRepositoryFactory, IDisposable
   {
      public string ParentFolder { get; }

      /// <summary>
      /// Create a factory
      /// Throws ArgumentException if passed ParentFolder does not exist
      /// </summary>
      public LocalGitRepositoryFactory(string parentFolder,
         ISynchronizeInvoke synchronizeInvoke, bool useShallowClone)
      {
         if (!Directory.Exists(parentFolder))
         {
            throw new ArgumentException("Bad parent folder \"" + parentFolder + "\"");
         }

         ParentFolder = parentFolder;
         _synchronizeInvoke = synchronizeInvoke;
         _useShallowClone = useShallowClone;

         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryFactory] Created LocalGitRepositoryFactory for parentFolder {0}", parentFolder));
      }

      /// <summary>
      /// Create a LocalGitRepository object or return it if already cached.
      /// Throws if
      /// </summary>
      public ILocalGitRepository GetRepository(ProjectKey key)
      {
         if (_isDisposed)
         {
            return null;
         }

         if (_repos.TryGetValue(key, out LocalGitRepository cachedRepository))
         {
            return cachedRepository;
         }

         LocalGitRepository repo;
         try
         {
            repo = new LocalGitRepository(ParentFolder, key, _synchronizeInvoke, _useShallowClone,
               (r) => RepositoryCloned?.Invoke(r));
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot create LocalGitRepository", ex);
            return null;
         }
         _repos[key] = repo;
         return repo;
      }

      public event Action<ILocalGitRepository> RepositoryCloned;

      public void Dispose()
      {
         Trace.TraceInformation(String.Format(
            "[LocalGitRepositoryFactory] Disposing LocalGitRepositoryFactory for parentFolder {0}", ParentFolder));
         foreach (LocalGitRepository repo in _repos.Values)
         {
            repo.Dispose();
         }
         _repos.Clear();
         _isDisposed = true;
      }

      private readonly Dictionary<ProjectKey, LocalGitRepository> _repos =
         new Dictionary<ProjectKey, LocalGitRepository>();
      private readonly ISynchronizeInvoke _synchronizeInvoke;

      private readonly bool _useShallowClone;
      private bool _isDisposed;
   }
}

