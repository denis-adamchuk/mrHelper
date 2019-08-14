using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Common.Exceptions;

namespace mrHelper.App.Helpers
{
   public class CancelledByUserException : Exception {}
   public class RepeatOperationException : Exception {}

   internal class GitClientInitializer
   {
      internal event EventHandler<string> OnInitializationStatusChange;

      internal GitClientInitializer(GitClientFactory factory)
      {
         GitClientFactory = factory;
      }

      /// <summary>
      /// Initializes passed GitClient object.
      /// Throw GitOperationException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async internal Task InitAsync(GitClient client, string path, string hostName,
         string projectName, CommitChecker commitChecker)
      {
         if (!isCloneAllowed(path))
         {
            throw new CancelledByUserException();
         }

         await initClientAsync(client, path, projectName, hostName, commitChecker);
      }

      internal void CancelAsyncOperation()
      {
         GitClient?.CancelAsyncOperation();
      }

      /// <summary>
      /// Initializes passed GitClient object.
      /// Throw GitOperationException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async private Task initClientAsync(GitClient client, string path, string hostName,
         string projectName, CommitChecker commitChecker)
      {
         if (isCloneNeeded(path))
         {
            await runAsync(async () => await client.CloneAsync(hostName, projectName, path), "clone");
            Debug.Assert(GitClient.IsGitClient(client.Path));
         }
         else if (await checkForRepositoryUpdatesAsync(client, commitChecker))
         {
            await runAsync(async () => await client.FetchAsync(), "fetch");
         }
      }

      /// <summary>
      /// Check if Path exists and asks user if we can clone a git repository.
      /// Return true if Path exists or user allows to create it, false otherwise
      /// </summary>
      private bool isCloneAllowed(string path)
      {
         if (!System.IO.Directory.Exists(path))
         {
            if (MessageBox.Show("There is no git repository at \"" + path + "\". "
               + "Do you want to run 'git clone'?", "Information", MessageBoxButtons.YesNo,
               MessageBoxIcon.Information) == DialogResult.No)
            {
               return false;
            }
         }
         return true;
      }

      /// <summary>
      /// Check if Path exists and it is a valid git repository
      /// </summary>
      private bool isCloneNeeded(string path)
      {
         return !System.IO.Directory.Exists(path) || !GitClient.IsGitClient(path);
      }

      private delegate Task Command();

      /// <summary>
      /// Run a git command asynchronously.
      /// Throw GitOperationException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async private Task runAsync(Command command, string name)
      {
         try
         {
            await command();
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is InvalidOperationException));

            // Exception handling does not mean that we can return valid GitClient
            bool cancelledByUser = isCancelledByUser(ex);

            string result = cancelledByUser ? "cancelled by user" : "failed";
            OnInitializationStatusChange?.Invoke(this, String.Format("git {0} {1}", name, result));

            if (cancelledByUser)
            {
               throw new CancelledByUserException();
            }

            if (isSSLCertificateProblem(ex as GitOperationException))
            {
               if (handleSSLCertificateProblem())
               {
                  throw new RepeatOperationException();
               }
               throw new CancelledByUserException();
            }

            throw;
         }

         //OnInitializationStatusChange?.Invoke(sender, String.Empty);
      }

      /// <summary>
      /// Check exit code.
      /// git returns exit code -1 if user cancels operation.
      /// </summary>
      private bool isCancelledByUser(Exception ex)
      {
         return ex is GitOperationException && (ex as GitOperationException).ExitCode == -1;
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
            GitUtils.SetGlobalSSLVerify(false);
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot change global http.verifySSL setting");
            throw;
         }

         OnInitializationStatusChange?.Invoke(this,
               "SSL certificate verification disabled. Please repeat git operation.");
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

      /// <summary>
      /// Checks if there is a version in GitLab which is newer than latest Git Repository update.
      /// Returns 'true' if there is a newer version.
      /// </summary>
      async static private Task<bool> checkForRepositoryUpdatesAsync(GitClient client, CommitChecker commitChecker)
      {
         if (!client.LastUpdateTime.HasValue)
         {
            return true;
         }

         return await commitChecker.AreNewCommitsAsync(client.LastUpdateTime.Value);
      }

      private GitClientFactory GitClientFactory { get; }
      private GitClient GitClient { get; set; }
   }
}

