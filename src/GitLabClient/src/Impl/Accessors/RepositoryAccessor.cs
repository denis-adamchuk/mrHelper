using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Constants;

namespace mrHelper.GitLabClient
{
   public class RepositoryAccessorException : ExceptionEx
   {
      internal RepositoryAccessorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public class RepositoryAccessor : IDisposable
   {
      internal RepositoryAccessor(IHostProperties settings, ProjectKey projectKey,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         _settings = settings;
         _projectKey = projectKey;
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      public Task<Comparison> Compare(string from, string to)
      {
         return call(() => _operator.CompareAsync(from, to),
               "Cancelled Compare() call", "Failed Compare() call");
      }

      public Task<File> LoadFile(string filename, string sha)
      {
         return call(() => _operator.LoadFileAsync(filename, sha),
            "File loading cancelled", "Cannot load file");
      }

      public Task<Commit> LoadCommit(string sha)
      {
         return call(() => _operator.LoadCommitAsync(sha),
            "Commit loading cancelled", "Cannot load commit");
      }

      async public Task<Commit> FindFirstBranchCommit(string branchName)
      {
         Commit headBranchCommit = await LoadCommit(branchName);
         if (headBranchCommit.Parent_Ids == null || !headBranchCommit.Parent_Ids.Any())
         {
            Debug.Assert(false);
            return null;
         }

         string previousCommitSha = headBranchCommit.Id;
         for (int iDepth = 0; iDepth < Constants.MaxCommitDepth; ++iDepth)
         {
            string sha = getParentSha(headBranchCommit.Parent_Ids.First(), iDepth);
            IEnumerable<CommitRef> refs = await loadCommitRefs(sha);
            if (refs == null || !refs.Any())
            {
               continue;
            }

            IEnumerable<CommitRef> branchRefs = refs.Where(x => x.Type == "branch");
            if (branchRefs.Count() != 1 || branchRefs.First().Name != branchName)
            {
               break;
            }

            previousCommitSha = sha;
         }

         if (previousCommitSha == headBranchCommit.Id)
         {
            return headBranchCommit;
         }
         return await LoadCommit(previousCommitSha);
      }

      public Task<IEnumerable<Branch>> GetBranches(string search)
      {
         return call(() => _operator.GetBranches(search),
            "Branch list loading cancelled", "Cannot load list of branches");
      }

      public Task<Branch> CreateNewBranch(string name, string sha)
      {
         return call(() => _operator.CreateNewBranchAsync(name, sha),
            "Branch creation cancelled", "Cannot create a new branch");
      }

      async public Task<IEnumerable<string>> FindPreferredTargetBranchNames(
         Branch sourceBranch, Commit sourceBranchCommit)
      {
         if (sourceBranchCommit.Parent_Ids == null || !sourceBranchCommit.Parent_Ids.Any())
         {
            Debug.Assert(false);
            return null;
         }

         for (int iDepth = 0; iDepth < Constants.MaxCommitDepth; ++iDepth)
         {
            string sha = getParentSha(sourceBranchCommit.Parent_Ids.First(), iDepth);
            IEnumerable<CommitRef> refs = await loadCommitRefs(sha);
            if (refs == null || !refs.Any())
            {
               continue;
            }

            IEnumerable<CommitRef> branchRefs = refs.Where(x => x.Type == "branch");
            if (branchRefs.Count() == 1 && branchRefs.First().Name == sourceBranch.Name)
            {
               continue;
            }

            return branchRefs.Where(x => x.Name != sourceBranch.Name).Select(x => x.Name);
         }
         return null;
      }

      public Task DeleteBranch(string name)
      {
         return call(() => _operator.DeleteBranchAsync(name),
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

      private static string getParentSha(string sha, int depth)
      {
         return String.Format("{0}{1}", sha, new string('^', depth));
      }

      private Task<IEnumerable<CommitRef>> loadCommitRefs(string sha)
      {
         return call(() => _operator.LoadCommitRefsAsync(sha),
            "Commit refs loading cancelled", "Cannot load commit refs");
      }

      async private Task call(Func<Task> func, string cancelMessage, string errorMessage)
      {
         if (_operator == null)
         {
            _operator = new RepositoryOperator(_projectKey, _settings, _networkOperationStatusListener);
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
            _operator = new RepositoryOperator(_projectKey, _settings, _networkOperationStatusListener);
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

      private readonly ProjectKey _projectKey;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
      private readonly IHostProperties _settings;

      private RepositoryOperator _operator;
   }
}

