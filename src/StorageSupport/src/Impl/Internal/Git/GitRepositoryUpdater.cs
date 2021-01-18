using System;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.StorageSupport
{
   /// <summary>
   /// Prepares GitRepository to use.
   /// </summary>
   internal class GitRepositoryUpdater : ILocalCommitStorageUpdater, IDisposable
   {
      /// <summary>
      /// Bind to the specific GitRepository object
      /// </summary>
      internal GitRepositoryUpdater(
         ISynchronizeInvoke synchronizeInvoke,
         IGitRepository gitRepository,
         IExternalProcessManager operationManager,
         UpdateMode mode,
         Action onCloned,
         Action<string> onFetched)
      {
         _gitRepository = gitRepository;
         _updaterInternal = new GitRepositoryUpdaterInternal(synchronizeInvoke, gitRepository,
            operationManager, mode, onCloned, onFetched);
      }

      /// <summary>
      /// Update passed GitRepository object.
      /// Throw InteractiveUpdaterException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async public Task StartUpdate(ICommitStorageUpdateContextProvider contextProvider,
         Action<string> onProgressChange, Action onUpdateStateChange)
      {
         if (_updaterInternal != null)
         {
            if (_gitRepository.ExpectingClone && !isCloneAllowed(_gitRepository.Path))
            {
               throw new LocalCommitStorageUpdaterCancelledException();
            }

            await runAsync(_gitRepository, async () => await _updaterInternal.StartUpdate(
               contextProvider, onProgressChange, onUpdateStateChange));
         }
      }

      public void StopUpdate()
      {
         _updaterInternal?.StopUpdate();
      }

      public bool CanBeStopped()
      {
         return _updaterInternal != null && _updaterInternal.CanBeStopped();
      }

      public void RequestUpdate(ICommitStorageUpdateContextProvider contextProvider, Action onFinished)
      {
         _updaterInternal?.RequestUpdate(contextProvider, onFinished);
      }

      public void Dispose()
      {
         _updaterInternal?.Dispose();
         _updaterInternal = null;
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
      async private Task runAsync(IGitRepository repo, Func<Task> command)
      {
         try
         {
            await command();
         }
         catch (GitRepositoryUpdaterException ex)
         {
            string errorMessage = "Cannot initialize git repository";
            if (ex is UpdateCancelledException)
            {
               throw new LocalCommitStorageUpdaterCancelledException();
            }

            if (ex is SSLVerificationException)
            {
               if (handleSSLCertificateProblem())
               {
                  throw new LocalCommitStorageUpdaterException(String.Empty, ex);
               }
               throw new LocalCommitStorageUpdaterCancelledException();
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
               MessageBox.Show(String.Format("git reports that \"{0}\" already exists and is not empty. "
                  + "Please delete this directory and try again.", ex.OriginalMessage), "Warning",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
               throw new LocalCommitStorageUpdaterCancelledException();
            }

            throw new LocalCommitStorageUpdaterFailedException(errorMessage, ex);
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
            throw new LocalCommitStorageUpdaterFailedException("Cannot change global http.verifySSL setting", ex);
         }

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

      async private Task handleAuthenticationFailedException(ILocalCommitStorage repo, Func<Task> command)
      {
         string configKey = "credential.interactive";
         string configValue = "always";

         GitTools.ConfigScope scope = _gitRepository.ExpectingClone ?
            GitTools.ConfigScope.Global : GitTools.ConfigScope.Local;
         string path = _gitRepository.ExpectingClone ? String.Empty : _gitRepository.Path;

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

      private HashSet<ILocalCommitStorage> _fixingAuthFailed = new HashSet<ILocalCommitStorage>();
      private IGitRepository _gitRepository;
      private GitRepositoryUpdaterInternal _updaterInternal;
   }
}

