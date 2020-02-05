using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitClient;

namespace mrHelper.App.Helpers
{
   internal class CancelledByUserException : Exception {}
   internal class RepeatOperationException : Exception {}

   /// <summary>
   /// Prepares LocalGitRepository to use.
   /// </summary>
   internal class GitInteractiveUpdater
   {
      internal event Action<string> InitializationStatusChange;

      internal GitInteractiveUpdater()
      {
      }

      /// <summary>
      /// Update passed LocalGitRepository object.
      /// Throw GitOperationException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async internal Task UpdateAsync(ILocalGitRepository repo, IInstantProjectChecker instantChecker,
         Action<string> onProgressChange)
      {
         if (repo.DoesRequireClone() && !isCloneAllowed(repo.Path))
         {
            InitializationStatusChange?.Invoke("Clone rejected");
            throw new CancelledByUserException();
         }

         InitializationStatusChange?.Invoke("Updating git repository...");

         await runAsync(async () => await repo.Updater.ForceUpdate(instantChecker, onProgressChange));
         InitializationStatusChange?.Invoke("Git repository updated");
      }

      /// <summary>
      /// Check if Path exists and asks user if we can clone a git repository.
      /// Return true if Path exists or user allows to create it, false otherwise
      /// </summary>
      private bool isCloneAllowed(string path)
      {
         if (!System.IO.Directory.Exists(path))
         {
            if (MessageBox.Show(String.Format("There is no git repository at \"{0}\"."
               + "Do you want to run 'git clone'?", path), "Information", MessageBoxButtons.YesNo,
               MessageBoxIcon.Information) == DialogResult.No)
            {
               return false;
            }
         }
         return true;
      }

      private delegate Task Command();

      /// <summary>
      /// Run a git command asynchronously.
      /// Throw GitOperationException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async private Task runAsync(Command command)
      {
         try
         {
            await command();
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is InvalidOperationException));

            // Exception handling does not mean that we can return valid LocalGitRepository
            bool cancelledByUser = isCancelledByUser(ex);

            string result = cancelledByUser ? "cancelled by user" : "failed";
            InitializationStatusChange?.Invoke(String.Format("Git repository update {0}", result));

            if (cancelledByUser)
            {
               throw new CancelledByUserException();
            }

            if (isSSLCertificateProblem(ex as GitOperationException))
            {
               InitializationStatusChange?.Invoke("Cannot clone due to SSL verification error");
               if (handleSSLCertificateProblem())
               {
                  throw new RepeatOperationException();
               }
               throw new CancelledByUserException();
            }

            throw;
         }
      }

      /// <summary>
      /// Check exit code.
      /// git returns exit code 128 if user cancels operation.
      /// </summary>
      private bool isCancelledByUser(Exception ex)
      {
         return ex is GitOperationException && (ex as GitOperationException).Cancelled;
      }

      ///<summary>
      /// Handle exceptions caused by SSL certificate problem
      ///</summary>
      private bool handleSSLCertificateProblem()
      {
         if (!isGlobalSSLFixAllowed())
         {
            return false;
         }

         try
         {
            ExternalProcess.Start("git", "config --global http.sslVerify false", true, String.Empty);
         }
         catch (ExternalProcessException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot change global http.verifySSL setting");
            throw;
         }

         InitializationStatusChange?.Invoke("SSL certificate verification disabled. Please repeat git operation.");
         return true;
      }

      /// <summary>
      /// Check exception Details to figure out if it was caused by SSL certificate problem.
      /// </summary>
      private bool isSSLCertificateProblem(GitOperationException ex)
      {
         return ex != null && ex.Details.Contains("SSL certificate problem");
      }

      /// <summary>
      /// Ask user if we can fix SSL verification issue
      /// </summary>
      private bool isGlobalSSLFixAllowed()
      {
         return MessageBox.Show("SSL certificate problem occurred with git server. "
            + "Do you want to disable certificate verification in global git config?",
            "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
      }
   }
}

