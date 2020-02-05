using System;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   /// <summary>
   ///
   /// </summary>
   public interface ILocalGitRepository : IGitRepository
   {
      new ILocalGitRepositoryData Data { get; }

      string Path { get; }

      ILocalGitRepositoryUpdater Updater { get; }

      event Action<ILocalGitRepository, DateTime> Updated;
      event Action<ILocalGitRepository> Disposed;

      bool DoesRequireClone();
   }
}

