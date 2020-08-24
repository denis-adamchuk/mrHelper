using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient;

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
         ProjectAccessor projectAccessor, string parentFolder, int revisionsToKeep, int comparisonsToKeep)
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

         if (projectAccessor == null)
         {
            throw new ArgumentException("projectAccessor argument cannot be null");
         }

         if (synchronizeInvoke == null)
         {
            throw new ArgumentException("synchronizeInvoke argument cannot be null");
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

         if (_fileStorages.TryGetValue(key, out FileStorage cachedFileStorage))
         {
            return cachedFileStorage;
         }
         else if (_gitRepositories.TryGetValue(key, out GitRepository cachedGitRepository))
         {
            return cachedGitRepository;
         }

         ILocalCommitStorage result;
         try
         {
            if (type == LocalCommitStorageType.FileStorage)
            {
               FileStorage storage = new FileStorage(ParentFolder, key, _synchronizeInvoke,
                  _projectAccessor.GetSingleProjectAccessor(key.ProjectName).GetRepositoryAccessor(),
                  _revisionsToKeep, _comparisonsToKeep, () => _fileStorages.Count);
               _fileStorages[key] = storage;
               result = storage;
            }
            else
            {
               Debug.Assert(type == LocalCommitStorageType.FullGitRepository
                         || type == LocalCommitStorageType.ShallowGitRepository);

               GitRepository storage = new GitRepository(ParentFolder, key, _synchronizeInvoke, type,
                  (r) => GitRepositoryCloned?.Invoke(r));
               _gitRepositories[key] = storage;
               result = storage;
            }
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot create commit storage", ex);
            return null;
         }
         return result;
      }

      public event Action<ILocalCommitStorage> GitRepositoryCloned;

      public void Dispose()
      {
         Trace.TraceInformation(String.Format(
            "[LocalCommitStorageFactory ] Disposing a factory for parentFolder {0}", ParentFolder));

         foreach (FileStorage storage in _fileStorages.Values)
         {
            storage.Dispose();
         }
         _fileStorages.Clear();

         foreach (GitRepository storage in _gitRepositories.Values)
         {
            storage.Dispose();
         }
         _fileStorages.Clear();

         _isDisposed = true;
      }

      private readonly Dictionary<ProjectKey, GitRepository> _gitRepositories =
         new Dictionary<ProjectKey, GitRepository>();
      private readonly Dictionary<ProjectKey, FileStorage> _fileStorages =
         new Dictionary<ProjectKey, FileStorage>();
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly ProjectAccessor _projectAccessor;
      private readonly int _revisionsToKeep;
      private readonly int _comparisonsToKeep;

      private bool _isDisposed;
   }
}

