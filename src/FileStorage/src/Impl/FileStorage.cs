using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Types;
using mrHelper.Client.Repository;
using mrHelper.Common.Tools;

namespace mrHelper.FileStorage
{
   /// <summary>
   /// Provides access to git repository.
   /// </summary>
   internal class FileStorage : IFileStorage, IDisposable
   {
      // @{ IGitRepository
      IGitCommitStorageData IGitCommitStorage.Data => ExpectingClone ? null : _data;
      // @{ IGitRepository

      // @{ ILocalGitRepository
      public ILocalGitCommitStorageData Data => ExpectingClone ? null : _data;

      public string Path { get; }

      public ILocalGitCommitStorageUpdater Updater => _updater;
      // @} ILocalGitRepository

      // @{ IFileStorage
      public bool ExpectingClone { get; private set; } = true;

      public MergeRequestKey MergeRequestKey { get; }
      // @} IFileStorage

      /// <summary>
      /// </summary>
      internal FileStorage(string parentFolder, MergeRequestKey mrk,
         ISynchronizeInvoke synchronizeInvoke, IRepositoryManager repositoryManager)
      {
         Path = FileStoragePathFinder.FindPath(parentFolder, mrk);

         _operationManager = new GitOperationManager(synchronizeInvoke, Path);
         _updater = new FileStorageUpdater(synchronizeInvoke, this, repositoryManager, onCloned, onFetched);
         _data = new FileStorageData(_operationManager, Path);

         ExpectingClone = isEmptyFolder(Path);
         MergeRequestKey = mrk;
         Trace.TraceInformation(String.Format(
            "[FileStorage] Created FileStorage at Path {0} for host {1}, project {2}, IId {3}, "
          + "expecting clone = {4}",
            Path, mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId, ExpectingClone.ToString()));
      }

      public void Dispose()
      {
         Trace.TraceInformation(String.Format("[FileStorage] Disposing LocalGitRepository at path {0}", Path));

         _data.Dispose();
         _data = null;

         _updater.Dispose();
         _updater = null;

         _operationManager.Dispose();

         _isDisposed = true;
      }

      public override string ToString()
      {
         return String.Format("[FileStorage] {0}:{1} at {2}",
            MergeRequestKey.ProjectKey.ProjectName, MergeRequestKey.IId, MergeRequestKey.ProjectKey.HostName);
      }

      private void onFetched(string sha)
      {
         Debug.Assert(!_cached_existingSha.Contains(sha));
         _cached_existingSha.Add(sha);
      }

      private void onCloned()
      {
         ExpectingClone = false;
      }

      private static bool isEmptyFolder(string path)
      {
         return !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any();
      }

      private readonly HashSet<string> _cached_existingSha = new HashSet<string>();
      private FileStorageData _data;
      private FileStorageUpdater _updater;
      private bool _isDisposed;
      private readonly GitOperationManager _operationManager;
   }
}

