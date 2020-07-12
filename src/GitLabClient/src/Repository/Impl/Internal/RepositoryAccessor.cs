using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Common;

namespace mrHelper.Client.Repository
{
   internal class RepositoryAccessor : IRepositoryAccessor
   {
      internal RepositoryAccessor(IHostProperties settings, string hostname)
      {
         _hostname = hostname;
         _settings = settings;
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

      public void Dispose()
      {
         Cancel();
      }

      public void Cancel()
      {
         _operator?.Dispose();
         _operator = null;
      }

      async private Task call(ProjectKey projectKey, Func<Task> func, string cancelMessage, string errorMessage)
      {
         if (_operator == null)
         {
            _operator = new RepositoryOperator(_hostname, _settings);
         }

         try
         {
            await func();
         }
         catch (OperatorException ex)
         {
            if (ex.Cancelled)
            {
               Trace.TraceInformation(String.Format("[RepositoryAccessor] {0}", cancelMessage));
               return;
            }
            throw new RepositoryAccessorException(errorMessage, ex);
         }
      }

      async private Task<T> call<T>(ProjectKey projectKey, Func<Task<T>> func, string cancelMessage, string errorMessage)
      {
         if (_operator == null)
         {
            _operator = new RepositoryOperator(_hostname, _settings);
         }

         try
         {
            return await func();
         }
         catch (OperatorException ex)
         {
            if (ex.Cancelled)
            {
               Trace.TraceInformation(String.Format("[RepositoryAccessor] {0}", cancelMessage));
               return default(T);
            }
            throw new RepositoryAccessorException(errorMessage, ex);
         }
      }

      private readonly string _hostname;
      private readonly IHostProperties _settings;

      private RepositoryOperator _operator;
   }
}

