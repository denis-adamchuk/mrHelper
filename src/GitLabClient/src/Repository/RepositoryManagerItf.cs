using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Repository
{
   public class RepositoryManagerException : ExceptionEx
   {
      internal RepositoryManagerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface IRepositoryManager
   {
      Task<Comparison> Compare(ProjectKey projectKey, string from, string to);

      Task<File> LoadFile(ProjectKey projectKey, string filename, string sha);

      Task<Commit> LoadCommit(ProjectKey projectKey, string sha);

      Task<Branch> CreateNewBranch(ProjectKey projectKey, string name, string sha);

      Task DeleteBranch(ProjectKey projectKey, string name);

      Task Cancel();
   }
}

