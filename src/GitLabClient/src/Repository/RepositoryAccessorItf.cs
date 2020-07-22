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
      Task<Comparison> Compare(string projectName, string from, string to);

      Task<File> LoadFile(string projectName, string filename, string sha);

      Task<Commit> LoadCommit(string projectName, string sha);

      Task<IEnumerable<Branch>> GetBranches(string projectName);

      Task<Branch> CreateNewBranch(string projectName, string name, string sha);

      Task<Branch> FindPreferredTargetBranch(string projectName, string sourceBranchName);

      Task DeleteBranch(string projectName, string name);

      void Cancel();
   }
}

