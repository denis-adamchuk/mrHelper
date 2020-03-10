using System;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitClient
{
   public class BranchCreationException : ExceptionEx
   {
      internal BranchCreationException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   public interface ILocalGitRepository : IGitRepository
   {
      new ILocalGitRepositoryData Data { get; }

      string Path { get; }

      ILocalGitRepositoryUpdater Updater { get; }

      event Action<ILocalGitRepository> Updated;
      event Action<ILocalGitRepository> Disposed;

      bool DoesRequireClone();

      void CreateBranchForPatch(string branchPointSha, string branchName, string patch);
   }
}

