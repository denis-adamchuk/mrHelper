using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Projects;

namespace mrHelper.StorageSupport
{
   ///<summary>
   /// Creates ILocalCommitStorage objects.
   ///<summary>
   public class LocalCommitStorageFactory : ILocalCommitStorageFactory, IDisposable
   {
      public string ParentFolder { get; }

      /// <summary>
      /// Create a factory
      /// Throws ArgumentException if passed ParentFolder does not exist and cannot be created
      /// </summary>
      public LocalCommitStorageFactory(ISynchronizeInvoke synchronizeInvoke,
         IProjectAccessor projectAccessor, string parentFolder, int revisionsToKeep, int comparisonsToKeep)
      {
         if (!Directory.Exists(parentFolder))
         {
            try
            {
               Directory.CreateDirectory(parentFolder);
            }
            catch (Exception ex)
            {
               throw new ArgumentException(String.Format("Cannot create folder \"{0}\"", parentFolder), ex);
            }
         }

         ParentFolder = parentFolder;
         _synchronizeInvoke = synchronizeInvoke;
         _projectAccessor = projectAccessor;
         _revisionsToKeep = revisionsToKeep;
         _comparisonsToKeep = comparisonsToKeep;

         Trace.TraceInformation(String.Format(
            "[LocalCommitStorageFactory] Created a factory for parentFolder {0}", parentFolder));
      }

      /// <summary>
      /// Create a commit storage object or return it if already cached.
      /// Throws if
      /// </summary>
      public ILocalCommitStorage GetStorage(ProjectKey key, LocalCommitStorageType type)
      {
         if (_isDisposed)
         {
            return null;
         }

         if (_storages.TryGetValue(key, out ILocalCommitStorage cachedStorage))
         {
            return cachedStorage;
         }

         ILocalCommitStorage storage;
         try
         {
            if (type == LocalCommitStorageType.FileStorage)
            {
               storage = new FileStorage(ParentFolder, key, _synchronizeInvoke,
                  _projectAccessor.GetSingleProjectAccessor(key.ProjectName).RepositoryAccessor,
                  _revisionsToKeep, _comparisonsToKeep, () => _storages.Count);
            }
            else
            {
               Debug.Assert(type == LocalCommitStorageType.FullGitRepository
                         || type == LocalCommitStorageType.ShallowGitRepository);

               storage = new GitRepository(ParentFolder, key, _synchronizeInvoke, type,
                  (r) => GitRepositoryCloned?.Invoke(r));
            }
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot create commit storage", ex);
            return null;
         }
         _storages[key] = storage;
         return storage;
      }

      public event Action<ILocalCommitStorage> GitRepositoryCloned;

      public void Dispose()
      {
         Trace.TraceInformation(String.Format(
            "[LocalCommitStorageFactory ] Disposing a factory for parentFolder {0}", ParentFolder));
         foreach (ILocalCommitStorage storage in _storages.Values)
         {
            storage.Dispose();
         }
         _storages.Clear();
         _isDisposed = true;
      }

      private readonly Dictionary<ProjectKey, ILocalCommitStorage> _storages =
         new Dictionary<ProjectKey, ILocalCommitStorage>();
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly IProjectAccessor _projectAccessor;
      private readonly int _revisionsToKeep;
      private readonly int _comparisonsToKeep;

      private bool _isDisposed;
   }
}

