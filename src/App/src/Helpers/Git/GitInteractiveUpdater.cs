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
   internal class InteractiveUpdateFailed : ExceptionEx
   {
      internal InteractiveUpdateFailed(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   internal class InteractiveUpdateCancelledException : Exception {}
   internal class InteractiveUpdateSSLFixedException : Exception {}

   /// <summary>
   /// Prepares LocalGitRepository to use.
   /// </summary>
   internal class GitInteractiveUpdater
   {
      internal event Action<string> InitializationStatusChange;

      /// <summary>
      /// Update passed LocalGitRepository object.
      /// Throw InteractiveUpdaterException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async internal Task UpdateAsync(ILocalGitRepository repo, IProjectUpdateContext instantChecker,
         Action<string> onProgressChange)
      {
         if (repo.DoesRequireClone() && !isCloneAllowed(repo.Path))
         {
            InitializationStatusChange?.Invoke("Clone rejected");
            throw new InteractiveUpdateCancelledException();
         }

         InitializationStatusChange?.Invoke("Updating git repository...");

         await runAsync(async () => await repo.Updater.Update(instantChecker, onProgressChange));
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
      /// Throw InteractiveUpdaterException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async private Task runAsync(Command command)
      {
         try
         {
            await command();
         }
         catch (RepositoryUpdateException ex)
         {
            if (ex is UpdateCancelledException)
            {
               InitializationStatusChange?.Invoke("Git repository update cancelled by user");
               throw new InteractiveUpdateCancelledException();
            }

            if (ex is SecurityException)
            {
               InitializationStatusChange?.Invoke("Cannot clone due to SSL verification error");
               if (handleSSLCertificateProblem())
               {
                  throw new InteractiveUpdateSSLFixedException();
               }
               throw new InteractiveUpdateCancelledException();
            }

            InitializationStatusChange?.Invoke("Git repository update failed");
            throw new InteractiveUpdateFailed("Cannot update repository", ex);
         }
      }

      ///<summary>
      /// Handle exceptions caused by SSL certificate problem
      /// Throw InteractiveUpdaterException on unrecoverable errors.
      ///</summary>
      private bool handleSSLCertificateProblem()
      {
         if (!isGlobalSSLFixAllowed())
         {
            Trace.TraceInformation("[GitInteractiveUpdater] User rejected to disable SSl certificate verification");
            return false;
         }
         Trace.TraceInformation("[GitInteractiveUpdater] User agreed to disable SSl certificate verification");

         try
         {
            GitTools.DisableSSLVerification();
         }
         catch (GitTools.SSLVerificationDisableException ex)
         {
            throw new InteractiveUpdateFailed("Cannot change global http.verifySSL setting", ex);
         }

         InitializationStatusChange?.Invoke("SSL certificate verification disabled. Please repeat git operation.");
         Trace.TraceInformation("[GitInteractiveUpdater] SSL certificate verification disabled");
         return true;
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

