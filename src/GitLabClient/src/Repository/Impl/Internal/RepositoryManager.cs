using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Common;
using System;
using System.Diagnostics;

namespace mrHelper.Client.Repository
{
   internal class RepositoryManager : IRepositoryManager, IDisposable
   {
      internal RepositoryManager(IHostProperties settings)
      {
         _settings = settings;
      }

      public void Dispose()
      {
         _operator?.Dispose();
      }

      public Task<Comparison> Compare(ProjectKey projectKey, string from, string to)
      {
         return call(projectKey, () => _operator.CompareAsync(projectKey.ProjectName, from, to),
            "Cancelled Compare() call", "Failed Compare() call");
      }

      public Task<File> LoadFile(ProjectKey projectKey, string filename, string sha)
      {
         return call(projectKey, () => _operator.LoadFileAsync(projectKey.ProjectName, filename, sha),
            "File loading cancelled", "Cannot load file");
      }

      public Task<Commit> LoadCommit(ProjectKey projectKey, string sha)
      {
         return call(projectKey, () => _operator.LoadCommitAsync(projectKey.ProjectName, sha),
            "Commit loading cancelled", "Cannot load commit");
      }

      public Task<Branch> CreateNewBranch(ProjectKey projectKey, string name, string sha)
      {
         return call(projectKey, () => _operator.CreateNewBranchAsync(projectKey.ProjectName, name, sha),
            "Branch creation cancelled", "Cannot create a new branch");
      }

      public Task DeleteBranch(ProjectKey projectKey, string name)
      {
         return call(projectKey, () => _operator.DeleteBranchAsync(projectKey.ProjectName, name),
            "Branch deletion cancelled", "Cannot delete a branch");
      }

      async private Task call(ProjectKey projectKey, Func<Task> func, string cancelMessage, string errorMessage)
      {
         _operator?.Dispose();
         _operator = new RepositoryOperator(projectKey.HostName, _settings);
         try
         {
            await func();
         }
         catch (OperatorException ex)
         {
            if (ex.Cancelled)
            {
               Trace.TraceInformation(String.Format("[RepositoryManager] {0}", cancelMessage));
               return;
            }
            throw new RepositoryManagerException(errorMessage, ex);
         }
      }

      async private Task<T> call<T>(ProjectKey projectKey, Func<Task<T>> func, string cancelMessage, string errorMessage)
      {
         _operator?.Dispose();
         _operator = new RepositoryOperator(projectKey.HostName, _settings);
         try
         {
            return await func();
         }
         catch (OperatorException ex)
         {
            if (ex.Cancelled)
            {
               Trace.TraceInformation(String.Format("[RepositoryManager] {0}", cancelMessage));
               return default(T);
            }
            throw new RepositoryManagerException(errorMessage, ex);
         }
      }

      private readonly IHostProperties _settings;
      private RepositoryOperator _operator;
   }
}

