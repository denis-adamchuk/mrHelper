namespace mrHelper.UI
{
   public GitClientResult
   {
      Success,
      CancelledByUser
   }

   ///<summary>
   /// Wrapper on GitRepository that handles interaction with user
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

      public GitClient(IClientCallback callback)
      {
         if (callback == null)
         {
            throw new ArgumentException("Callback is null");
         }

         try
         {
            Directory.CreateDirectory(localFolder);
         }
         catch (Exception)
         {
            throw new ArgumentException("Bad local folder");
         }
      }

      /// <summary>
      /// Initializes GitRepository member asynchronously.
      /// Returns Task if initialization started and null otherwise.
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
            return await runAsync(() => GitRepository.CloneAsync( HostName, ProjectName, Path), "clone");
         }
         else if (await checkForRepositoryUpdatesAsync())
         {
            return await runAsync(() => GitRepository.FetchAsync(), "fetch");
         }

         return GitClientUpdateStatus.Success;
      }

      public void CancelOperation()
      {
         Debug.Assert(IsInitialized());

         try
         {
            _gitRepository.CancelAsyncOperation();
         }
         catch (InvalidOperationException)
         {
            Debug.Assert(false);
         }
      }

      async public Task DiffToolAsync(string name, string leftCommit, string rightCommit)
      {
         Debug.Assert(IsInitialized());

         try
         {
            await _gitRepository.DiffToolAsync(name, leftCommit, rightCommit);
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot launch diff tool");
         }
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
            bool cancelledByUser = (ex is GitOperationException && (ex as GitOperationException).ExitCode == -1);
            string result = cancelledByUser ? "cancelled by user" : "failed";
            OnOperationStatusChange?.Invoke(sender, String.Format("git {0} {1}", name, result));
            Debug.Assert(!(ex is InvalidOperationException));
            if (cancelledByUser)
            {
               return AsyncOperationResult.CancelledByUser;
            }
            else if (ex is GitOperationException as gex)
            {
               if (tryFixSSLProblem(gex))
               {
                  OnOperationStatusChange?.Invoke(sender,
                     "SSL certificate verification disabled. Please repeat git operation.");
                  return AsyncOperationResult.Success;
               }
            }

            GitRepository = null;
            throw;
         }
         finally
         {
            OnAsyncOperationFinished?.Invoke(this);
         }

         OnOperationStatusChange?.Invoke(sender, String.Empty);
         return AsyncOperationResult.Success;
      }

      /// <summary>
      /// Checks if there is a version in GitLab which is newer than latest Git Repository update.
      /// Returns 'true' if there is a newer version.
      /// </summary>
      async private Task<bool> checkForRepositoryUpdatesAsync()
      {
         Debug.Assert(IsInitialized());

         if (!_gitRepository.LastUpdateTime.HasValue)
         {
            return true;
         }

         List<Version> versions = null;
         GitLab gl = new GitLab(HostName, AccessToken));
         try
         {
            versions = await gl.Projects.Get(ProjectName).MergeRequests.Get(MergeRequestIId).
               Versions.LoadAllTaskAsync();
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot check GitLab for updates");
         }

         return versions != null && versions.Count > 0
            && versions[0].Created_At.ToLocalTime() > _gitRepository.LastUpdateTime;
      }

      private bool tryFixSSLProblem(GitOperationException ex)
      {
         if (ex == null || !ex.Details.Contains("SSL certificate problem"))
         {
            return false;
         }

         if (MessageBox.Show("SSL certificate problem occured with git server. "
            + "Do you want to disable certificate verification in global git config?",
            "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
         {
            try
            {
               GitUtils.SetGlobalSSLVerify(false);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle(ex, "Cannot change global http.verifySSL setting", true);
               return false;
            }
            return true;
         }

         return false;
      }

      private string Path
      {
         get
         {
            return Path.Combine(Callback.GetCurrentLocalGitFolder(), Callback.GetCurrentProjectName());
         }
      }

      private string Host { get { return Callback.GetCurrentHostName(); } }
      private string ProjectName { get { return Callback.GetCurrentProjectName(); } }
      private string AccessToken { get { return Callback.GetCurrentAccessToken(); } }
      private int MergeRequestIId { get { return Callback.GetCurrentMergeRequestId(); } }

      private IClientCallback Callback { get; }
      private GitRepository GitRepository { get; }
   }
}

