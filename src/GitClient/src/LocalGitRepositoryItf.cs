using System;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   public enum ELocalGitRepositoryState
   {
      NotCloned, // Git folder is ready for clone
      Cloned,    // State of a git repository before the first update
      Ready      // State of a git repository after the first update
   }

   public interface ILocalGitRepository : IGitRepository
   {
      new ILocalGitRepositoryData Data { get; }

      string Path { get; }

      ILocalGitRepositoryUpdater Updater { get; }

      event Action<ILocalGitRepository> Updated;
      event Action<ILocalGitRepository> Disposed;

      ELocalGitRepositoryState State { get; }
   }
}

