using System.Collections.Generic;

namespace mrHelper.Common.Interfaces
{
   public interface IGitRepositoryData
   {
      IEnumerable<string> Get(GitShowRevisionArguments arguments);
      IEnumerable<string> Get(GitDiffArguments arguments);
   }

   public interface IGitRepository
   {
      IGitRepositoryData Data { get; }

      ProjectKey ProjectKey { get; }
   }
}

