using System;
using mrHelper.Core.Git;

namespace mrHelper.Client
{
   private AsyncOperationResult
   {
      Success,
      CancelledByUser
   }

   public delegate bool CheckForUpdates();

   ///<summary>
   /// Wrapper on GitClient that handles interaction with user
   /// It is a lazy wrapper, it does not clone/fetch until InitializeAsync() is called.
   ///<summary>
   public class GitClientManager
   {
      public bool IsInitialized()
      {
         return GetRepository() != null;
      }

      public event EventHandler<GitUtils.OperationStatusChangeArgs> OnOperationStatusChange;

      /// <summary>
      /// Creates an instance of GitClientManager
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
         UpdateCheker = updateChecker;
      }

      /// <summary>
      /// Initializes GitClient member asynchronously.
      /// Sets asynchronous exception GitOperationException in case of problems with git.
      /// </summary>
      async public Task<AsyncOperationResult> GetClientAsync(string projectName, CheckForUpdates check)
      {
         if (Clients.ContainsKey(projectName))
         {
            GitClient client = Clients[projectName];
            await checkForRepositoryUpdatesAsync(client, check);
            return client;
         }

         GitClient client = getClientAsync(projectName);
         if (client != null)
         {
            Clients[projectName] = client;
         }
         return client;
      }

      public GitClient GitClient { get; private set; } = null;

      async private GitClient getClientAsync(string projectName, CheckForUpdates check)
      {
         string path = Path.Combine(LocalFolder, projectName);
         if (!isCloneAllowed(path))
         {
            return null;
         }

         return createClientAsync(path, projectName, check) == AsyncOperationResult.Success ? gitClient : null;
      }

      async private AsyncOperationResult createClientAsync(string path, string projectName, CheckForUpdates check)
      {
         if (isCloneNeeded())
         {
            GitClient client = new GitClient();
            return await runAsync(() => client.CloneAsync(HostName, projectName, path), "clone");
         }

         GitClient client = new GitClient(path, false);
         if (await checkForRepositoryUpdatesAsync(client, check))
         {
            return await runAsync(() => client.FetchAsync(), "fetch");
         }
         return client
      }

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

      private bool isCloneNeeded()
      {
         return !Directory.Exists(Path) || !GitClient.IsGitClient(Path);
      }

      async private Task<AsyncOperationResult> runAsync(Command command, string name)
      {
         try
         {
            await command();
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is InvalidOperationException));

            GitClient = null;
            return handleExceptionInRunAsync(ex, name);
         }

         OnOperationStatusChange?.Invoke(sender, String.Empty);
         return AsyncOperationResult.Success;
      }

      private void handleExceptionInRunAsync(Exception ex, string name)
      {
         bool cancelledByUser = isCancelledByUser(ex);
         string result = cancelledByUser ? "cancelled by user" : "failed";

         OnOperationStatusChange?.Invoke(sender, String.Format("git {0} {1}", name, result));

         if (cancelledByUser)
         {
            return AsyncOperationResult.CancelledByUser;
         }
         else if (isSSLCertificateProblem(ex))
         {
            return handleSSLCertificateProblem();
         }

         throw ex; // TODO - Or throw;
      }

      private bool isCancelledByUser(Exception ex)
      {
         return ex is GitOperationException && (ex as GitOperationException).ExitCode == -1;
      }

      private bool isSSLCertificateProblem(Exception ex)
      {
         throw new NotImplementedException();
      }

      private bool isGlobalSSLFixAllowed()
      {
         return MessageBox.Show("SSL certificate problem occurred with git server. "
            + "Do you want to disable certificate verification in global git config?",
            "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
      }

      private AsyncOperationResult handleSSLCertificateProblem()
      {
         if (!isGlobalSSLFixAllowed())
         {
            return AsyncOperationResult.CancelledByUser;
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
         return AsyncOperationResult.Success;
      }

      /// <summary>
      /// Checks if there is a version in GitLab which is newer than latest Git Repository update.
      /// Returns 'true' if there is a newer version.
      /// </summary>
      async static private Task<bool> checkForRepositoryUpdatesAsync(GitClient client, CheckForUpdates check)
      {
         Debug.Assert(IsInitialized());

         if (!client.LastUpdateTime.HasValue)
         {
            return true;
         }

         return check(gitClient.LastUpdateTime.Value);
      }

      private string Host { get; }
      private string LocalFolder { get; }

      private Dictionary<string, GitClient> Clients { get; set; }
   }
}

