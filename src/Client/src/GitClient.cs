using System;
using mrHelper.Core.Git;

namespace mrHelper.Client
{
   public AsyncOperationResult
   {
      Success,
      CancelledByUser
   }

   ///<summary>
   /// Wrapper on GitRepository that handles interaction with user
   /// It is a lazy wrapper, it does not clone/fetch until InitializeAsync() is called.
   ///<summary>
   public class GitClient
   {
      public bool IsInitialized()
      {
         return GetRepository() != null;
      }

      public event EventHandler OnAsyncOperationStarted;
      public event EventHandler<GitUtils.OperationStatusChangeArgs> OnOperationStatusChange;
      public event EventHandler OnAsyncOperationFinished;

      /// <summary>
      /// Creates an instance of GitClient
      /// Throws:
      /// ArgumentException is local git folder is bad path
      /// </summary>
      public GitClient(string localFolder, string hostName, string projectName, IUpdateChecker updateChecker)
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
         ProjectName = projectName;
         UpdateCheker = updateChecker;
      }

      /// <summary>
      /// Initializes GitRepository member asynchronously.
      /// Sets asynchronous exception GitOperationException in case of problems with git.
      /// </summary>
      async public Task<GitClientResult> InitializeAsync()
      {
         if (!isCloneAllowed())
         {
            return GitClientResult.CancelledByUser;
         }

         GitRepository = new GitRepository(Path, false);
         GitRepository.OnOperationStatusChange += (sender, e) => { OnOperationStatusChange?.Invoke(sender, e); }; 

         if (isCloneNeeded())
         {
            return await runAsync(() => GitRepository.CloneAsync(HostName, ProjectName, Path), "clone");
         }
         else if (await checkForRepositoryUpdatesAsync())
         {
            return await runAsync(() => GitRepository.FetchAsync(), "fetch");
         }

         return GitClientUpdateStatus.Success;
      }

      public GitRepository GitRepository { get; private set; } = null;

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
         return !Directory.Exists(Path) || !GitRepository.IsGitRepository(Path);
      }

      async private Task<AsyncOperationResult> runAsync(Command command, string name)
      {
         OnAsyncOperationStarted?.Invoke(this);

         try
         {
            await command();
         }
         catch (Exception ex)
         {
            Debug.Assert(!(ex is InvalidOperationException));

            GitRepository = null;
            return handleExceptionInRunAsync(ex, name);
         }
         finally
         {
            OnAsyncOperationFinished?.Invoke(this);
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
      async private Task<bool> checkForRepositoryUpdatesAsync()
      {
         Debug.Assert(IsInitialized());

         if (!GitRepository.LastUpdateTime.HasValue)
         {
            return true;
         }

         return UpdateChecker.AreAnyUpdatesAsync(GitRepository.LastUpdateTime.Value);
      }

      private string Path
      {
         get
         {
            return Path.Combine(LocalFolder, ProjectName);
         }
      }

      private string Host { get; }
      private string LocalFolder { get; }
      private string ProjectName { get; }
   }
}

