using mrHelper.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Common.Tools
{
   public class BranchCreationException : ExceptionEx
   {
      internal BranchCreationException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   public static class GitTools
   {
      private enum FailedStep
      {
         None,
         Stash,
         Checkout,
         Apply,
         Commit
      }

      async public static Task CreateBranchForPatchAsync(string branchPointSha, string branchName,
         string patch, string path)
      {
         string patchFilename = String.Format("{0}.patch", branchName);
         string patchFilepath = System.IO.Path.Combine(path, patchFilename);
         try
         {
            FileUtils.OverwriteFile(patchFilepath, patch);
         }
         catch (Exception ex)
         {
            throw new BranchCreationException(ex);
         }

         FailedStep? failedStep = null;
         string currentBranch = String.Empty, currentSha = String.Empty;

         try
         {
            getCurrentBranch(path, out currentBranch, out currentSha);

            // Put working directory, index and untracked files to stash
            failedStep = FailedStep.Stash;
            await ExternalProcess.StartAsync("git", "stash push -u", path, null, null).Task;

            // Checkout a branch (create if it does not exist) and reset it to a SHA
            failedStep = FailedStep.Checkout;
            string checkoutArgs = String.Format("checkout -B {0} {1}", branchName, branchPointSha);
            await ExternalProcess.StartAsync("git", checkoutArgs, path, null, null).Task;

            // Apply a patch directly to the index
            failedStep = FailedStep.Apply;
            string applyArgs = String.Format("apply --cached {0}", StringUtils.EscapeSpaces(patchFilepath));
            await ExternalProcess.StartAsync("git", applyArgs, path, null, null).Task;

            // Create a commit with patch
            failedStep = FailedStep.Commit;
            string commitArgs = String.Format("commit -m {0}", branchName);
            await ExternalProcess.StartAsync("git", commitArgs, path, null, null).Task;

            failedStep = FailedStep.None;
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               throw new BranchCreationException(ex);
            }
            throw;
         }
         finally
         {
            System.IO.File.Delete(patchFilepath);

            if (failedStep.HasValue)
            {
               if (currentSha != String.Empty && currentBranch != String.Empty)
               {
                  // Checkout a branch that we had before
                  if (currentBranch == "HEAD") // detached HEAD
                  {
                     await ExternalProcess.StartAsync("git", "checkout origin", path, null, null).Task;
                     string resetArgs = String.Format("reset --soft {0}", currentSha);
                     await ExternalProcess.StartAsync("git", resetArgs, path, null, null).Task;
                  }
                  else
                  {
                     string checkoutArgs = String.Format("checkout -B {0} {1}", currentBranch, currentSha);
                     await ExternalProcess.StartAsync("git", checkoutArgs, path, null, null).Task;
                  }
               }

               if (failedStep.Value == FailedStep.Commit)
               {
                  await ExternalProcess.StartAsync("git", "reset --mixed", path, null, null).Task; // Clean-up index
               }

               if ((failedStep.Value == FailedStep.Commit || failedStep.Value == FailedStep.Apply)
                  && branchName != currentBranch)
               {
                  string delBranchArgs = String.Format("branch -d {0}", branchName);
                  await ExternalProcess.StartAsync("git", delBranchArgs, path, null, null).Task;
               }

               if (failedStep.Value == FailedStep.Commit   || failedStep.Value == FailedStep.Apply
                || failedStep.Value == FailedStep.Checkout || failedStep.Value == FailedStep.None)
               {
                  await ExternalProcess.StartAsync("git", "stash pop", path, null, null).Task;
               }
            }
         }
      }

      private static void getCurrentBranch(string path, out string currentBranch, out string currentSha)
      {
         IEnumerable<string> revParseOutput = ExternalProcess.Start(
            "git", "rev-parse HEAD", true, path).StdOut;
         currentSha = revParseOutput.Any() ? revParseOutput.First() : String.Empty;

         IEnumerable<string> revParseOutput2 = ExternalProcess.Start(
            "git", "rev-parse --abbrev-ref HEAD", true, path).StdOut;
         currentBranch = revParseOutput2.Any() ? revParseOutput2.First() : String.Empty;
      }
   }
}

