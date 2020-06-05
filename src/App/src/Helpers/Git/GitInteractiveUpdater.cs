using System;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
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
      async internal Task UpdateAsync(ILocalGitRepository repo, IProjectUpdateContextProvider contextProvider,
         Action<string> onProgressChange, Action onUpdateStateChange)
      {
         if (repo.ExpectingClone && !isCloneAllowed(repo.Path))
         {
            InitializationStatusChange?.Invoke("Clone rejected");
            throw new InteractiveUpdateCancelledException();
         }

         InitializationStatusChange?.Invoke("Updating git repository...");

         await runAsync(repo, async () => await repo.Updater.StartUpdate(
            contextProvider, onProgressChange, onUpdateStateChange));
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
            if (MessageBox.Show(String.Format("There is no git repository at \"{0}\". "
               + "Do you want to run 'git clone'?", path), "Information", MessageBoxButtons.YesNo,
               MessageBoxIcon.Information) == DialogResult.No)
            {
               return false;
            }
         }
         return true;
      }

      /// <summary>
      /// Run a git command asynchronously.
      /// Throw InteractiveUpdaterException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async private Task runAsync(ILocalGitRepository repo, Func<Task> command)
      {
         try
         {
            await command();
         }
         catch (RepositoryUpdateException ex)
         {
            string errorMessage = "Cannot initialize git repository";
            if (ex is UpdateCancelledException)
            {
               InitializationStatusChange?.Invoke("Git repository update cancelled by user");
               throw new InteractiveUpdateCancelledException();
            }

            if (ex is SSLVerificationException)
            {
               InitializationStatusChange?.Invoke("Cannot clone due to SSL verification error");
               if (handleSSLCertificateProblem())
               {
                  throw new InteractiveUpdateSSLFixedException();
               }
               throw new InteractiveUpdateCancelledException();
            }

            if (ex is AuthenticationFailedException)
            {
               errorMessage = "Wrong username or password. Authentication failed.";
               if (_fixingAuthFailed.Add(repo))
               {
                  try
                  {
                     await handleAuthenticationFailedException(repo, async () => await runAsync(repo, command));
                     return;
                  }
                  finally
                  {
                     _fixingAuthFailed.Remove(repo);
                  }
               }
            }

            if (ex is CouldNotReadUsernameException)
            {
               errorMessage = String.Format("Cannot work with {0} without credentials", repo.ProjectKey.ProjectName);
            }

            if (ex is NotEmptyDirectoryException)
            {
               InitializationStatusChange?.Invoke("Cannot clone due to bad directory");
               MessageBox.Show(String.Format("git reports that \"{0}\" already exists and is not empty. "
                  + "Please delete this directory and try again.", ex.OriginalMessage), "Warning",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
               throw new InteractiveUpdateCancelledException();
            }

            InitializationStatusChange?.Invoke("Git repository update failed");
            throw new InteractiveUpdateFailed(errorMessage, ex);
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

      async private Task handleAuthenticationFailedException(ILocalGitRepository repo, Func<Task> command)
      {
         string configKey = "credential.interactive";
         string configValue = "always";

         GitTools.EConfigScope scope = repo.ExpectingClone ? GitTools.EConfigScope.Global : GitTools.EConfigScope.Local;
         string path = repo.ExpectingClone ? String.Empty : repo.Path;

         IEnumerable<string> prevValue = GitTools.GetConfigKeyValue(scope, configKey, path);
         string prevInteractiveMode = prevValue.Any() ? prevValue.First() : null; // `null` to unset

         try
         {
            GitTools.SetConfigKeyValue(scope, configKey, configValue, path);
            await command();
         }
         finally
         {
            GitTools.SetConfigKeyValue(scope, configKey, prevInteractiveMode, path);
         }
      }

      private HashSet<ILocalGitRepository> _fixingAuthFailed = new HashSet<ILocalGitRepository>();
   }
}

