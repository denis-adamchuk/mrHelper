using System;
using System.Collections.Generic;
using System.Diagnostics;
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
      private enum FailedStep
      {
         None,
         Stash,
         Checkout,
         Apply,
         Commit
      }

      internal CreateBranchFromPatchOperation(string path, IExternalProcessManager operationManager)
      {
         _operationManager = operationManager;
         _path = path;
      }

      async public Task Run(params object[] args)
      {
         if (args.Length != 3)
         {
            throw new ArgumentException("Wrong number of parameters");
         }

         string branchPointSha = args[0].ToString();
         string branchName = args[1].ToString();
         string patch = args[2].ToString();

         string patchFilename = String.Format("{0}.patch", branchName);
         string patchFilepath = System.IO.Path.Combine(_path, patchFilename);
         try
         {
            FileUtils.OverwriteFile(patchFilepath, patch);
         }
         catch (Exception ex)
         {
            throw new LocalGitRepositoryOperationException(null, null, ex);
         }

         _currentFailedStep = FailedStep.Checkout;
         string currentBranch = String.Empty, currentSha = String.Empty;

         Action resetIndex = () => ExternalProcess.Start("git", "reset --hard", true, _path);
         Action deleteBranch = () =>
         {
            if (branchName != currentBranch)
            {
               string delBranchArgs = String.Format("branch -d {0}", branchName);
               ExternalProcess.Start("git", delBranchArgs, true, _path);
            }
         };

         try
         {
            await doCreateBranch(branchPointSha, branchName, patchFilepath);
         }
         catch (Exception ex)
         {
            if (ex is SystemException || ex is GitCallFailedException)
            {
               if (_currentFailedStep == FailedStep.Commit)
               {
                  throw new LocalGitRepositoryOperationException(() => resetIndex(), () => deleteBranch(), ex);
               }
               else if (_currentFailedStep == FailedStep.Apply || _currentFailedStep == FailedStep.Checkout)
               {
                  throw new LocalGitRepositoryOperationException(null, () => deleteBranch() , ex);
               }
               throw new LocalGitRepositoryOperationException(null, null, ex);
            }
            throw;
         }
         finally
         {
            System.IO.File.Delete(patchFilepath);
         }

         Debug.Assert(_currentFailedStep == FailedStep.None);
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
         _currentFailedStep = FailedStep.Checkout;
         string checkoutArgs = String.Format("checkout -B {0} {1}", branchName, branchPointSha);
         _currentSubOperation = _operationManager.CreateDescriptor("git", checkoutArgs, _path, null);
         await _operationManager.Wait(_currentSubOperation);

         // Apply a patch directly to the index
         _currentFailedStep = FailedStep.Apply;
         string applyArgs = String.Format("apply --index {0}", StringUtils.EscapeSpaces(patchFilepath));
         _currentSubOperation = _operationManager.CreateDescriptor("git", applyArgs, _path, null);
         await _operationManager.Wait(_currentSubOperation);

         // Create a commit with patch
         _currentFailedStep = FailedStep.Commit;
         string commitArgs = String.Format("commit -m {0}", branchName);
         _currentSubOperation = _operationManager.CreateDescriptor("git", commitArgs, _path, null);
         await _operationManager.Wait(_currentSubOperation);

         _currentFailedStep = FailedStep.None;
         _currentSubOperation = null;
      }

      private string _path;
      private readonly IExternalProcessManager _operationManager;
      private ExternalProcess.AsyncTaskDescriptor _currentSubOperation;
      private FailedStep _currentFailedStep;
   }
}

