using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.GitClient
{
   internal class CreateBranchFromPatchOperation : ILocalGitRepositoryOperation
   {
      internal CreateBranchFromPatchOperation(string path, IExternalProcessManager operationManager)
      {
         _operationManager = operationManager;
         _path = path;
      }

      async public Task Run(params object[] args)
      {
         if (args.Length != 4)
         {
            throw new ArgumentException("Wrong number of parameters");
         }

         string branchPointSha = args[0].ToString();
         string branchName = args[1].ToString();
         string patch = args[2].ToString();
         string currentBranch = args[3].ToString();

         string patchFilename = String.Format("{0}.patch", branchName);
         string patchFilepath = System.IO.Path.Combine(_path, patchFilename);
         try
         {
            FileUtils.OverwriteFile(patchFilepath, patch);
         }
         catch (Exception ex)
         {
            throw new LocalGitRepositoryOperationException(null, ex);
         }

         try
         {
            await doCreateBranch(branchPointSha, branchName, patchFilepath);
         }
         catch (Exception ex)
         {
            if (ex is SystemException || ex is GitCallFailedException || ex is OperationCancelledException)
            {
               throw new LocalGitRepositoryOperationException(
                  () =>
                  {
                     if (branchName != currentBranch)
                     {
                        string delBranchArgs = String.Format("branch -D {0}", branchName);
                        ExternalProcess.Start("git", delBranchArgs, true, _path);
                     }
                  }, ex);
            }
            throw;
         }
         finally
         {
            ExternalProcess.Start("git", "reset --hard", true, _path);
         }
      }

      async public Task Cancel()
      {
         if (_currentSubOperation != null)
         {
            await _operationManager.Cancel(_currentSubOperation);
            _currentSubOperation = null;
         }
      }

      async private Task doCreateBranch(string branchPointSha, string branchName, string patchFilepath)
      {
         // Checkout a branch (create if it does not exist) and reset it to a SHA
         string checkoutArgs = String.Format("checkout -B {0} {1}", branchName, branchPointSha);
         _currentSubOperation = _operationManager.CreateDescriptor("git", checkoutArgs, _path, null);
         await _operationManager.Wait(_currentSubOperation);
         if (_currentSubOperation == null)
         {
            throw new OperationCancelledException();
         }

         // Apply a patch directly to the index
         string applyArgs = String.Format("apply --reject --ignore-space-change --ignore-whitespace {0}",
            StringUtils.EscapeSpaces(patchFilepath));
         _currentSubOperation = _operationManager.CreateDescriptor("git", applyArgs, _path, null);
         try
         {
            await _operationManager.Wait(_currentSubOperation);
         }
         catch (GitCallFailedException ex)
         {
            // exception is swallowed because git apply which generates .rej files throws
            ExceptionHandlers.Handle("git apply failed", ex);
         }
         finally
         {
            System.IO.File.Delete(patchFilepath);
         }
         if (_currentSubOperation == null)
         {
            throw new OperationCancelledException();
         }

         // Add all files to index
         _currentSubOperation = _operationManager.CreateDescriptor("git", "add .", _path, null);
         await _operationManager.Wait(_currentSubOperation);
         if (_currentSubOperation == null)
         {
            throw new OperationCancelledException();
         }

         // Create a commit with patch
         string commitArgs = String.Format("commit -m {0}", branchName);
         _currentSubOperation = _operationManager.CreateDescriptor("git", commitArgs, _path, null);
         await _operationManager.Wait(_currentSubOperation);
         if (_currentSubOperation == null)
         {
            throw new OperationCancelledException();
         }

         _currentSubOperation = null;
      }

      private string _path;
      private readonly IExternalProcessManager _operationManager;
      private ExternalProcess.AsyncTaskDescriptor _currentSubOperation;
   }
}

