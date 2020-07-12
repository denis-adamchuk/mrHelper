using System;

namespace mrHelper.StorageSupport
{
   public interface ILocalCommitStorage : ICommitStorage, IDisposable
   {
      string Path { get; }

      ILocalCommitStorageUpdater Updater { get; }

      new IAsyncGitCommandService Git { get; }
   }
}

