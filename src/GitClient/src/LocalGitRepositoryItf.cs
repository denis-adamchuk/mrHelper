using System;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   public interface ILocalGitRepository : IGitRepository
   {
      new ILocalGitRepositoryData Data { get; }

      string Path { get; }

      ILocalGitRepositoryUpdater Updater { get; }

      bool ExpectingClone { get; }
   }
}

