namespace mrHelper.StorageSupport
{
   public interface ILocalCommitStorage : ICommitStorage
   {
      string Path { get; }

      ILocalCommitStorageUpdater Updater { get; }

      new IAsyncGitCommandService Git { get; }
   }
}

