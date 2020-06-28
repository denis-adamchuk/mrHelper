using System;
using System.Collections.Generic;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
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

   public interface IGitCommitStorageData
   {
      IEnumerable<string> Get(GitShowRevisionArguments arguments);
      IEnumerable<string> Get(GitDiffArguments arguments);
   }

   public interface IGitCommitStorage
   {
      IGitCommitStorageData Data { get; }
   }
}

