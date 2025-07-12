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

      async public Task<Comparison> Compare(string from, string to, IComparisonCache cache)
      {
         Comparison comparison = cache?.LoadComparison(from, to);
         if (comparison != null)
         {
            return comparison;
         }

         comparison = await call(() => _operator.CompareAsync(from, to),
               "Cancelled Compare() call", "Failed Compare() call");
         if (comparison == null)
         {
            return null;
         }

         cache?.SaveComparison(from, to, comparison);
         return comparison;
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
         if (headBranchCommit == null)
         {
            return null; // Operation was canceled
         }

         if (headBranchCommit.Parent_Ids == null)
         {
            Debug.Assert(false);
            return null;
         }

         if (!headBranchCommit.Parent_Ids.Any())
         {
            return null; // It may happen for the first commit in the repository
         }

         string previousCommitSha = headBranchCommit.Id;
         for (int iDepth = 0; iDepth < Constants.MaxCommitDepth; ++iDepth)
         {
            string sha = getParentSha(headBranchCommit.Parent_Ids.First(), iDepth);
            IEnumerable<CommitRef> refs = await loadCommitRefs(sha);
            if (refs == null)
            {
               return null; // Operation was canceled
            }

            if (!refs.Any())
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
         string branchName, Commit commit)
      {
         if (commit.Parent_Ids == null || !commit.Parent_Ids.Any())
         {
            Debug.Assert(false);
            return null;
         }

         for (int iDepth = 0; iDepth < Constants.MaxCommitDepth; ++iDepth)
         {
            string sha = getParentSha(commit.Parent_Ids.First(), iDepth);
            IEnumerable<CommitRef> refs = await loadCommitRefs(sha);
            if (refs == null)
            {
               return null; // Operation was canceled
            }

            if (!refs.Any())
            {
               continue;
            }

            IEnumerable<CommitRef> branchRefs = refs.Where(x => x.Type == "branch");
            if (branchRefs.Count() == 1 && branchRefs.First().Name == branchName)
            {
               continue;
            }

            return branchRefs.Where(x => x.Name != branchName).Select(x => x.Name);
         }
         return null;
      }

      async public Task<bool> IsDescendantOf(string descendantBranchName, string ancestorBranchName)
      {
         if (descendantBranchName == ancestorBranchName)
         {
            return false;
         }

         Commit ancestorCommit = await LoadCommit(ancestorBranchName);
         if (ancestorCommit == null || descendantBranchName == ancestorCommit.Id)
         {
            return false;
         }

         return await isDescendant(descendantBranchName, ancestorCommit.Id, 1);
      }

      private async Task<bool> isDescendant(string commitSha, string ancestorCommitSha, int depth)
      {
         void logResult(string result) =>
            Trace.TraceInformation("[RepositoryAccessor] isDescendant({0}, {1}, {2}): {3}",
               commitSha, ancestorCommitSha, depth, result);

         Debug.Assert(commitSha != ancestorCommitSha);

         if (depth == Constants.MaxCommitDepth)
         {
            logResult("false (depth exceeded)");
            return false;
         }

         Commit commit = await LoadCommit(commitSha);
         if (commit == null)
         {
            logResult("false (commit is null)");
            return false;
         }

         foreach (string parentId in commit.Parent_Ids)
         {
            if (parentId == ancestorCommitSha || await isDescendant(parentId, ancestorCommitSha, depth + 1))
            {
               logResult("true");
               return true;
            }
         }

         logResult("false");
         return false;
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

