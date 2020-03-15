using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;

namespace mrHelper.Common.Interfaces
{
   public class GitDataException : ExceptionEx
   {
      public GitDataException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   public class GitNotAvailableDataException : GitDataException
   {
      public GitNotAvailableDataException(Exception ex)
         : base(String.Empty, ex)
      {
      }
   }

   public interface IGitRepositoryData
   {
      IEnumerable<string> Get(GitShowRevisionArguments arguments);
      IEnumerable<string> Get(GitDiffArguments arguments);

      Task<IEnumerable<string>> GetAsync(GitShowRevisionArguments arguments);
      Task<IEnumerable<string>> GetAsync(GitDiffArguments arguments);
   }

   public interface IGitRepository
   {
      IGitRepositoryData Data { get; }

      ProjectKey ProjectKey { get; }

      bool ContainsSHA(string sha);
      bool ContainsBranch(string branchName);
   }
}

