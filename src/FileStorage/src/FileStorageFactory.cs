using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Repository;
using mrHelper.Client.Types;

namespace mrHelper.FileStorage
{
   ///<summary>
   /// Creates FileStorage objects.
   ///<summary>
   public class FileStorageFactory : IDisposable
   {
      public string ParentFolder { get; }

      /// <summary>
      /// Create a factory
      /// Throws ArgumentException if passed ParentFolder does not exist
      /// </summary>
      public FileStorageFactory(string parentFolder,
         ISynchronizeInvoke synchronizeInvoke, IRepositoryManager repositoryManager)
      {
         if (!Directory.Exists(parentFolder))
         {
            throw new ArgumentException("Bad parent folder \"" + parentFolder + "\"");
         }

         ParentFolder = parentFolder;
         _synchronizeInvoke = synchronizeInvoke;
         _repositoryManager = repositoryManager;

         Trace.TraceInformation(String.Format(
            "[FileStorageFactory] Created FileStorageFactory for parentFolder {0}", parentFolder));
      }

      /// <summary>
      /// </summary>
      public ILocalGitCommitStorage GetRepository(MergeRequestKey mrk)
      {
         if (_isDisposed)
         {
            return null;
         }

         throw new NotImplementedException();
      }

      public void Dispose()
      {
         Trace.TraceInformation(String.Format(
            "[FileStorageFactory] Disposing FileStorageFactory for parentFolder {0}", ParentFolder));
         foreach (FileStorage repo in _repos.Values)
         {
            repo.Dispose();
         }
         _repos.Clear();
         _isDisposed = true;
      }

      private readonly Dictionary<MergeRequestKey, FileStorage> _repos =
         new Dictionary<MergeRequestKey, FileStorage>();
      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly IRepositoryManager _repositoryManager;

      private bool _isDisposed;
   }
}

