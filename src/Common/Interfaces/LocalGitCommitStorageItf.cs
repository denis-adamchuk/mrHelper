namespace mrHelper.Common.Interfaces
{
   public interface ILocalGitCommitStorage : IGitCommitStorage
   {
      new ILocalGitCommitStorageData Data { get; }

      string Path { get; }

      ILocalGitCommitStorageUpdater Updater { get; }
   }
}

