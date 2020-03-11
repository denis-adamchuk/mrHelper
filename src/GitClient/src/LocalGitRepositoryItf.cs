using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   public interface ILocalGitRepository : IGitRepository
   {
      new ILocalGitRepositoryData Data { get; }

      string Path { get; }

      ILocalGitRepositoryUpdater Updater { get; }

      event Action<ILocalGitRepository> Updated;
      event Action<ILocalGitRepository> Disposed;

      bool DoesRequireClone();

      Task CreateBranchForPatch(string branchPointSha, string branchName, string patch);
   }
}

