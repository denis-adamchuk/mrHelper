using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
using System.Collections.Generic;

namespace mrHelper.Client.Repository
{
   public class RepositoryAccessorException : ExceptionEx
   {
      internal RepositoryAccessorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface IRepositoryAccessor : IDisposable
   {
      Task<Comparison> Compare(string from, string to);

      Task<File> LoadFile(string filename, string sha);

      Task<Commit> LoadCommit(string sha);

      Task<IEnumerable<Branch>> GetBranches();

      Task<Branch> CreateNewBranch(string name, string sha);

      Task<string> FindPreferredTargetBranchName(string sourceBranchName, string sourceBranchCommitParentSha);

      Task DeleteBranch(string name);

      void Cancel();
   }
}

