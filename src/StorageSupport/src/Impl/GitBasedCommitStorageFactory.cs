using System;
using System.ComponentModel;
using mrHelper.Client.Types;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

namespace mrHelper.StorageSupport
{
   public class GitBasedCommitStorageFactory : ILocalGitCommitStorageFactory
   {
      public string ParentFolder { get; }

      /// <summary>
      /// Create a factory
      /// Throws ArgumentException if passed ParentFolder does not exist
      /// </summary>
      public GitBasedCommitStorageFactory(string parentFolder,
         ISynchronizeInvoke synchronizeInvoke, bool useShallowClone)
      {
         try
         {
            _gitRepositoryFactory = new LocalGitRepositoryFactory(parentFolder, synchronizeInvoke, useShallowClone);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot create LocalGitRepositoryFactory", ex);
            throw;
         }
      }

      public ILocalGitCommitStorage GetStorage(MergeRequestKey mrk)
      {
         return _gitRepositoryFactory?.GetRepository(mrk.ProjectKey);
      }

      public void Dispose()
      {
         _gitRepositoryFactory?.Dispose();
      }

      private LocalGitRepositoryFactory _gitRepositoryFactory;
   }
}

