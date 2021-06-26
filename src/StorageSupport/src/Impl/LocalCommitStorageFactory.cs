using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient;
using mrHelper.Common.Tools;

namespace mrHelper.StorageSupport
{
   ///<summary>
   /// Creates ILocalCommitStorage objects.
   ///<summary>
   public class LocalCommitStorageFactory : ILocalCommitStorageFactory, IDisposable, IFileStorageProperties
   {
      public string ParentFolder { get; }

      /// <summary>
      /// Create a factory
      /// Throws ArgumentException if passed ParentFolder does not exist and cannot be created
      /// </summary>
      public LocalCommitStorageFactory(
         string parentFolder,
         ISynchronizeInvoke synchronizeInvoke,
         ProjectAccessor projectAccessor,
         IFileStorageProperties properties)
      {
         if (!Directory.Exists(parentFolder))
         {
            try
            {
               Directory.CreateDirectory(parentFolder);
            }
            catch (Exception ex) // Any exception from Directory.CreateDirectory()
            {
               throw new ArgumentException(String.Format("Cannot create folder \"{0}\"", parentFolder), ex);
            }
         }

         ParentFolder = parentFolder;
         _synchronizeInvoke = synchronizeInvoke ?? throw new ArgumentException("synchronizeInvoke argument cannot be null");
         _projectAccessor = projectAccessor ?? throw new ArgumentException("projectAccessor argument cannot be null");
         _properties = properties;

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
            RepositoryAccessor repositoryAccessor =
               _projectAccessor.GetSingleProjectAccessor(key.ProjectName).GetRepositoryAccessor();
            if (type == LocalCommitStorageType.FileStorage)
            {
               FileStorage storage = new FileStorage(ParentFolder, key, _synchronizeInvoke, repositoryAccessor, this);
               _fileStorages[key] = storage;
               result = storage;
            }
            else
            {
               Debug.Assert(type == LocalCommitStorageType.FullGitRepository
                         || type == LocalCommitStorageType.ShallowGitRepository);

               GitRepository storage = new GitRepository(ParentFolder, key, _synchronizeInvoke, type,
                  (r) => GitRepositoryCloned?.Invoke(r), repositoryAccessor);
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

      public int GetRevisionCountToKeep() => _properties.GetRevisionCountToKeep();

      public int GetComparisonCountToKeep() => _properties.GetComparisonCountToKeep();

      public TaskUtils.BatchLimits GetComparisonBatchLimitsForAwaitedUpdate() =>
         _properties.GetComparisonBatchLimitsForAwaitedUpdate();

      public TaskUtils.BatchLimits GetFileBatchLimitsForAwaitedUpdate() =>
         _properties.GetFileBatchLimitsForAwaitedUpdate();

      public TaskUtils.BatchLimits GetComparisonBatchLimitsForNonAwaitedUpdate()
      {
         TaskUtils.BatchLimits defaultLimits = _properties.GetComparisonBatchLimitsForNonAwaitedUpdate();
         return new TaskUtils.BatchLimits
         {
            Size = defaultLimits.Size,
            Delay = defaultLimits.Delay * _fileStorages.Count
         };
      }

      public TaskUtils.BatchLimits GetFileBatchLimitsForNonAwaitedUpdate()
      {
         TaskUtils.BatchLimits defaultLimits = _properties.GetFileBatchLimitsForNonAwaitedUpdate();
         return new TaskUtils.BatchLimits
         {
            Size = defaultLimits.Size,
            Delay = defaultLimits.Delay * _fileStorages.Count
         };
      }

      private readonly Dictionary<ProjectKey, GitRepository> _gitRepositories =
         new Dictionary<ProjectKey, GitRepository>();
      private readonly Dictionary<ProjectKey, FileStorage> _fileStorages =
         new Dictionary<ProjectKey, FileStorage>();
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly ProjectAccessor _projectAccessor;
      private readonly IFileStorageProperties _properties;

      private bool _isDisposed;
   }
}

