using System;
using System.Collections.Generic;
using mrHelper.Core.Git;

namespace mrHelper.Client
{
   public delegate bool CheckForUpdates();

   public class CancelledByUserException : Exception {}
   public class RepeatOperationException : Exception {}

   ///<summary>
   /// Creates and manages GitClient objects.
   ///<summary>
   public class GitClientManager
   {
      public bool IsInitialized()
      {
         return GetRepository() != null;
      }

      public event EventHandler<GitUtils.OperationStatusChangeArgs> OnOperationStatusChange;

      /// <summary>
      /// Create an instance of GitClientManager
      /// Throws:
      /// ArgumentException is local git folder is bad path
      /// </summary>
      public GitClientManager(string localFolder, string hostName)
      {
         try
         {
            Directory.CreateDirectory(LocalGitFolder);
         }
         catch (Exception)
         {
            throw new ArgumentException("Bad local folder path");
         }

         LocalFolder = localFolder;
         HostName = hostName;
      }

      /// <summary>
      /// Create a GitClient object. Checks internal cache also.
      /// Return GitClient object if creation succeeded, throws otherwise.
      /// Throw GitOperationException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async public Task<GitClient> GetClientAsync(string projectName, CommitChecker commitChecker)
      {
         if (Clients.ContainsKey(projectName))
         {
            GitClient client = Clients[projectName];
            await checkForRepositoryUpdatesAsync(client, commitChecker);
            return client;
         }

         string path = Path.Combine(LocalFolder, projectName);
         if (!isCloneAllowed(path))
         {
            return null;
         }

         GitClient client = createClientAsync(path, projectName, commitChecker);
         Debug.Assert(client != null);
         Clients[projectName] = client;
         return client;
      }

      /// <summary>
      /// Create a GitClient object.
      /// Return GitClient object if creation succeeded, throws otherwise.
      /// Throw GitOperationException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async private GitClient createClientAsync(string path, string projectName, CommitChecker commitChecker)
      {
         if (isCloneNeeded())
         {
            GitClient client = new GitClient();
            await runAsync(client, (client) => client.CloneAsync(HostName, projectName, path), "clone");
            Debug.Assert(client.IsGitClient(client.Path));
            client.SetUpdater(UpdateManager.GetGitClientUpdater());
            return client;
         }

         GitClient client = new GitClient(path);
         if (await checkForRepositoryUpdatesAsync(client, commitChecker))
         {
            await runAsync(client, (client) => client.FetchAsync(), "fetch");
            client.SetUpdater(UpdateManager.GetGitClientUpdater());
         }
         return client;
      }

      /// <summary>
      /// Check if Path exists and asks user if we can clone a git repository.
      /// Return true if Path exists or user allows to create it, false otherwise
      /// </summary>
      private bool isCloneAllowed()
      {
         if (!Directory.Exists(Path))
         {
            if (MessageBox.Show("There is no git repository at \"" + Path + "\". "
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
      private bool isCloneNeeded()
      {
         return !Directory.Exists(Path) || !GitClient.IsGitClient(Path);
      }

      /// <summary>
      /// Run a git command asynchronously.
      /// Throw GitOperationException on unrecoverable errors.
      /// Throw CancelledByUserException and RepeatOperationException.
      /// </summary>
      async private Task runAsync(GitClient client, Command command, string name)
      {
         try
         {
            await command(client);
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is InvalidOperationException));

            // Exception handling does not mean that we can return valid GitClient
            bool cancelledByUser = isCancelledByUser(ex);

            string result = cancelledByUser ? "cancelled by user" : "failed";
            OnOperationStatusChange?.Invoke(sender, String.Format("git {0} {1}", name, result));

            if (cancelledByUser)
            {
               throw new CancelledByUserException();
            }

            if (isSSLCertificateProblem(ex))
            {
               if (handleSSLCertificateProblem())
               {
                  throw new RepeatOperationException();
               }
               throw new CancelledByUserException();
            }

            throw;
         }

         OnOperationStatusChange?.Invoke(sender, String.Empty);
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

         OnOperationStatusChange?.Invoke(sender,
               "SSL certificate verification disabled. Please repeat git operation.");
         return true;
      }

      /// <summary>
      /// Check exception Details to figure out if it was caused by SSL certificate problem.
      /// </summary>
      private bool isSSLCertificateProblem(Exception ex)
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
         Debug.Assert(IsInitialized());

         if (!client.LastUpdateTime.HasValue)
         {
            return true;
         }

         return await commitChecker.AreNewCommits(gitClient.LastUpdateTime.Value);
      }

      private string Host { get; }
      private string LocalFolder { get; }
      private UpdateManager updateManager { get; }

      private Dictionary<string, GitClient> Clients { get; set; }
   }
}

