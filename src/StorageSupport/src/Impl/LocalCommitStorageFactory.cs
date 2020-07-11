using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Session;

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
      /// Throws ArgumentException if passed ParentFolder does not exist
      /// </summary>
      public LocalCommitStorageFactory(ISynchronizeInvoke synchronizeInvoke,
         ISession session, string parentFolder, bool useShallowClone, int revisionsToKeep)
      {
         if (!Directory.Exists(parentFolder))
         {
            Directory.CreateDirectory(parentFolder);
         }

         ParentFolder = parentFolder;
         _synchronizeInvoke = synchronizeInvoke;
         _useShallowClone = useShallowClone;
         _session = session;
         _revisionsToKeep = revisionsToKeep;

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
            if (type == LocalCommitStorageType.GitRepository)
            {
               storage = new GitRepository(ParentFolder, key, _synchronizeInvoke, _useShallowClone,
                  (r) => GitRepositoryCloned?.Invoke(r));
            }
            else if (type == LocalCommitStorageType.FileStorage)
            {
               storage = new FileStorage(ParentFolder, key, _synchronizeInvoke, _session.GetRepositoryAccessor(),
                  _revisionsToKeep, () => _storages.Count);
            }
            else
            {
               Debug.Assert(false);
               return null;
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
      private readonly ISession _session;
      private readonly bool _useShallowClone;
      private readonly int _revisionsToKeep;

      private bool _isDisposed;
   }
}

