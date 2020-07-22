using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
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

      public Task<Comparison> Compare(string projectName, string from, string to)
      {
         return call(() => _operator.CompareAsync(projectName, from, to),
               "Cancelled Compare() call", "Failed Compare() call");
      }

      public Task<File> LoadFile(string projectName, string filename, string sha)
      {
         return call(() => _operator.LoadFileAsync(projectName, filename, sha),
            "File loading cancelled", "Cannot load file");
      }

      public Task<Commit> LoadCommit(string projectName, string sha)
      {
         return call(() => _operator.LoadCommitAsync(projectName, sha),
            "Commit loading cancelled", "Cannot load commit");
      }

      public Task<IEnumerable<Branch>> GetBranches(string projectName)
      {
         return call(() => _operator.GetBranches(projectName),
            "Branch list loading cancelled", "Cannot load list of branches");
      }

      public Task<Branch> CreateNewBranch(string projectName, string name, string sha)
      {
         return call(() => _operator.CreateNewBranchAsync(projectName, name, sha),
            "Branch creation cancelled", "Cannot create a new branch");
      }

      public Task<Branch> FindPreferredTargetBranch(string projectName, string sourceBranchName)
      {
         throw new NotImplementedException();
      }

      public Task DeleteBranch(string projectName, string name)
      {
         return call(() => _operator.DeleteBranchAsync(projectName, name),
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

      async private Task call(Func<Task> func, string cancelMessage, string errorMessage)
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

      async private Task<T> call<T>(Func<Task<T>> func, string cancelMessage, string errorMessage)
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

