using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;

namespace mrHelper.StorageSupport
{
   internal enum UpdateMode
   {
      ShallowClone,                    // "git clone --depth=1" and "git fetch --depth=1 sha:/refs/keep-around/sha"
      FullCloneWithSingleCommitFetches // "git clone" and "git fetch" and "git fetch sha:/refs/keep-around/sha"
   }

   /// <summary>
   /// Updates attached IGitRepository object
   /// </summary>
   internal class GitRepositoryUpdaterInternal : ILocalCommitStorageUpdater, IDisposable
   {
      /// <summary>
      /// Bind to the specific GitRepository object
      /// </summary>
      internal GitRepositoryUpdaterInternal(
         ISynchronizeInvoke synchronizeInvoke,
         IGitRepository gitRepository,
         IExternalProcessManager operationManager,
         UpdateMode mode,
         Action onCloned,
         Action<string> onFetched)
      {
         _synchronizeInvoke = synchronizeInvoke;
         _gitRepository = gitRepository;
         _processManager = operationManager;
         _updateMode = mode;
         _onCloned = onCloned;
         _onFetched = onFetched;
      }

      public void StopUpdate()
      {
         if (!CanBeStopped())
         {
            return;
         }

         _processManager.Cancel(_updateOperationDescriptor);
      }

      public bool CanBeStopped()
      {
         return !_isDisposed
            && _updateOperationDescriptor != null                    // update is running
            && _updateOperationDescriptor.OnProgressChange != null   // update is caused by StartUpdate() call
            && _gitRepository.ExpectingClone;                        // update is 'git clone'
      }

      public void Dispose()
      {
         _isDisposed = true;
      }

      async public Task StartUpdate(ICommitStorageUpdateContextProvider contextProvider,
         Action<string> onProgressChange, Action onUpdateStateChange)
      {
         if (onProgressChange == null)
         {
            return;
         }

         await update(contextProvider, onProgressChange, onUpdateStateChange, true, false);
      }

      async public Task update(ICommitStorageUpdateContextProvider contextProvider,
         Action<string> onProgressChange, Action onUpdateStateChange, bool canClone, bool canSplit)
      {
         CommitStorageUpdateContext context = contextProvider?.GetContext();
         if (contextProvider == null || (context != null && context.BaseToHeads == null))
         {
            Debug.Assert(false);
            return;
         }

         if (_isDisposed || context == null || (_gitRepository.ExpectingClone && !canClone))
         {
            return;
         }

         if (onProgressChange != null)
         {
            // save callbacks for operations that may start
            _onProgressChange = onProgressChange;
            _onUpdateStateChange = onUpdateStateChange;
         }

         if (_updateOperationDescriptor != null)
         {
            // already started, joining it
            _updateOperationDescriptor.OnProgressChange = getProgressChangeFunctor();
            _onUpdateStateChange?.Invoke();
         }

         if (UpdateContextUtils.IsWorthNewUpdate(context, _updatingContext))
         {
            await processContext(context, canSplit);
         }
         else
         {
            // TODO This is wrong, we need to check for something more stable than _updatingContext which maybe set and unset
            await TaskUtils.WhileAsync(() => _updatingContext != null);
         }

         _onProgressChange = null;
         _onUpdateStateChange = null;
      }

      public void RequestUpdate(ICommitStorageUpdateContextProvider contextProvider, Action onFinished)
      {
         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
            {
               try
               {
                  await update(contextProvider, null, null, false, true);
                  onFinished?.Invoke();
               }
               catch (GitRepositoryUpdaterException ex)
               {
                  ExceptionHandlers.Handle("Silent update failed", ex);
               }
            }), null);
      }

      async private Task processContext(CommitStorageUpdateContext context, bool canSplit)
      {
         if (!context.BaseToHeads.Any())
         {
            if (!_gitRepository.ExpectingClone && _updateMode == UpdateMode.ShallowClone)
            {
               return; // optimization. cannot do anything without Sha list
            }
            traceDebug("Empty context");
         }

         try
         {
            await doPreProcessContext(context);

            IEnumerable<InternalUpdateContext> splitted = canSplit && _updateMode == UpdateMode.ShallowClone
               ? new InternalUpdateContext(context.BaseToHeads).Split(MaxShaInChunk)
               : new InternalUpdateContext[] { new InternalUpdateContext(context.BaseToHeads) };
            foreach (InternalUpdateContext internalContext in splitted)
            {
               await doProcessContext(context, internalContext);

               // this allows others to interleave with their (shorter) requests
               await TaskUtils.IfAsync(() => internalContext != splitted.Last() && !_isDisposed, DelayBetweenChunksMs);
            }
         }
         catch (GitCommandException ex)
         {
            if (ex is OperationCancelledException)
            {
               throw new UpdateCancelledException();
            }
            else if (ex is GitCallFailedException gfex
                  && gfex.InnerException is ExternalProcessFailureException pfex
                  && String.Join("\n", pfex.Errors).Contains("SSL certificate problem"))
            {
               throw new SSLVerificationException(ex);
            }
            else if (ex is GitCallFailedException gfex2
                  && gfex2.InnerException is ExternalProcessFailureException pfex2
                  && String.Join("\n", pfex2.Errors).Contains("already exists and is not an empty directory"))
            {
               throw new NotEmptyDirectoryException(_gitRepository.Path, ex);
            }
            else if (ex is GitCallFailedException gfex3
                  && gfex3.InnerException is ExternalProcessFailureException pfex3
                  && String.Join("\n", pfex3.Errors).Contains("Authentication failed"))
            {
               throw new AuthenticationFailedException(ex);
            }
            else if (ex is GitCallFailedException gfex4
                  && gfex4.InnerException is ExternalProcessFailureException pfex4
                  && String.Join("\n", pfex4.Errors).Contains("could not read Username"))
            {
               throw new CouldNotReadUsernameException(ex);
            }
            throw new GitRepositoryUpdaterException("Cannot update git repository", ex);
         }
      }

      async private Task doPreProcessContext(CommitStorageUpdateContext context)
      {
         await TaskUtils.WhileAsync(() => _updatingContext != null);

         try
         {
            Debug.Assert(_updateOperationDescriptor == null);

            _updatingContext = context;
            traceDebug(String.Format("[LOCK][PRE] by {0}", context.GetType()));

            DateTime prevFullUpdateTimestamp = updateTimestamp(context);

            if (_gitRepository.ExpectingClone)
            {
               await cloneAsync(_updateMode == UpdateMode.ShallowClone);
               traceInformation("Repository cloned.");
               _onCloned?.Invoke();
            }
            else if (_updateMode != UpdateMode.ShallowClone
                  && context.LatestChange.HasValue
                  && context.LatestChange.Value > prevFullUpdateTimestamp)
            {
               await fetchAsync(false);
            }

            Debug.Assert(_updateOperationDescriptor == null);
         }
         finally
         {
            traceDebug(String.Format("[UNLOCK][PRE] by {0}", context.GetType()));
            _updatingContext = null;
         }
      }

      async private Task doProcessContext(
         CommitStorageUpdateContext context, InternalUpdateContext internalUpdateContext)
      {
         await TaskUtils.WhileAsync(() => _updatingContext != null);

         try
         {
            Debug.Assert(_updateOperationDescriptor == null);

            _updatingContext = context;
            traceDebug(String.Format("[LOCK] by {0}", context.GetType()));

            IEnumerable<string> missingSha = await getMissingSha(internalUpdateContext.BaseToHeads);
            await fetchCommitsAsync(missingSha, _updateMode == UpdateMode.ShallowClone);

            Debug.Assert(_updateOperationDescriptor == null);
         }
         finally
         {
            traceDebug(String.Format("[UNLOCK] by {0}", context.GetType()));
            _updatingContext = null;
         }
      }

      async private Task cloneAsync(bool shallowClone)
      {
         string arguments = String.Format("clone {0} {1}/{2} {3}",
            getCloneArguments(shallowClone),
            _gitRepository.ProjectKey.HostName,
            _gitRepository.ProjectKey.ProjectName,
            StringUtils.EscapeSpaces(_gitRepository.Path));
         await doUpdateOperationAsync(arguments, String.Empty);
      }

      async private Task fetchAsync(bool shallowFetch)
      {
         Debug.Assert(shallowFetch == false); // we don't support shallow fetch here

         string arguments = String.Format("fetch {0}",
            getFetchArguments(null, shallowFetch));
         await doUpdateOperationAsync(arguments, _gitRepository.Path);
      }

      async private Task fetchCommitsAsync(IEnumerable<string> shas, bool shallowFetch)
      {
         int iCommit = 0;
         foreach (string sha in shas)
         {
            string arguments = String.Format("fetch {0}", getFetchArguments(sha, shallowFetch));
            await doUpdateOperationAsync(arguments, _gitRepository.Path);
            _onFetched?.Invoke(sha);
            ++iCommit;
         }

         if (iCommit > 0)
         {
            traceInformation(String.Format("Fetched commits: {0}", iCommit));
         }
      }

      async private Task<IEnumerable<string>> getMissingSha(Dictionary<string, IEnumerable<string>> baseToHeads)
      {
         List<string> allSha = new List<string>();
         allSha.AddRange(baseToHeads.Keys);
         foreach (IEnumerable<string> heads in baseToHeads.Values) allSha.AddRange(heads);

         Exception exception = null;
         List<string> missingSha = new List<string>();
         await TaskUtils.RunConcurrentFunctionsAsync(allSha.Distinct(),
            async x =>
            {
               if (exception != null)
               {
                  return;
               }

               try
               {
                  if (!await _gitRepository.ContainsSHAAsync(x))
                  {
                     missingSha.Add(x);
                  }
               }
               catch (OperationCancelledException ex)
               {
                  exception = ex;
               }
            },
            20, 50, () => exception != null);

         if (exception != null)
         {
            throw exception;
         }

         return missingSha;
      }

      async private Task doUpdateOperationAsync(string arguments, string path)
      {
         if (!_isDisposed)
         {
            ExternalProcess.AsyncTaskDescriptor descriptor = startUpdateOperation(arguments, path);
            await waitUpdateOperationAsync(arguments, descriptor);
         }
      }

      private ExternalProcess.AsyncTaskDescriptor startUpdateOperation(string arguments, string path)
      {
         return _processManager.CreateDescriptor("git", arguments, path, getProgressChangeFunctor(), null);
      }

      private Action<string> getProgressChangeFunctor()
      {
         return _onProgressChange == null ?
            null : new Action<string>(status => _onProgressChange?.Invoke(status));
      }

      private async Task waitUpdateOperationAsync(
         string arguments, ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         try
         {
            _updateOperationDescriptor = descriptor;
            _onUpdateStateChange?.Invoke();
            traceInformation(String.Format("START git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments, _gitRepository.Path, _gitRepository.ProjectKey.ProjectName));
            await _processManager.Wait(descriptor);
         }
         finally
         {
            traceInformation(String.Format("FINISH git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments, _gitRepository.Path, _gitRepository.ProjectKey.ProjectName));
            _updateOperationDescriptor = null;
            _onUpdateStateChange?.Invoke();
         }
      }

      private static string getCloneArguments(bool shallow)
      {
         return String.Format(" --progress  {0} {1} {2}",
           shallow ? "--depth=1 --no-checkout" : String.Empty,
           GitTools.SupportsFetchNoTags() ? "--no-tags" : String.Empty,
           "-c credential.helper=manager -c credential.interactive=auto -c credential.modalPrompt=true");
      }

      private static string getFetchArguments(string sha, bool shallow)
      {
         if (sha == null)
         {
            return String.Format(" --progress {0} {1}",
               GitTools.SupportsFetchNoTags() ? "--no-tags" : String.Empty,
               GitTools.SupportsFetchAutoGC() ? "--no-auto-gc" : String.Empty);
         }

         return String.Format(" --progress {0} {1} {2} {3}",
            String.Format("origin {0}:refs/keep-around/{0}", sha),
            shallow ? "--depth=1" : String.Empty,
            GitTools.SupportsFetchNoTags() ? "--no-tags" : String.Empty,
            GitTools.SupportsFetchAutoGC() ? "--no-auto-gc" : String.Empty);
      }

      private DateTime updateTimestamp(CommitStorageUpdateContext context)
      {
         DateTime prevFullUpdateTimestamp = _latestFullFetchTimestamp;
         if (context.LatestChange.HasValue)
         {
            Debug.Assert(context is FullUpdateContext);
            if (context.LatestChange.Value > _latestFullFetchTimestamp)
            {
               traceInformation(String.Format("Updating LatestChange timestamp to {0}",
                  context.LatestChange.Value.ToLocalTime().ToString()));
               _latestFullFetchTimestamp = context.LatestChange.Value;
            }
            else if (context.LatestChange == _latestFullFetchTimestamp)
            {
               traceDebug("Timestamp not updated");
            }
            else if (context.LatestChange < _latestFullFetchTimestamp)
            {
               // This is not a problem and may happen when, for example, a Merge Request with the most newest
               // version has been closed.
               traceInformation("New LatestChange is older than a previous one");
            }
         }
         return prevFullUpdateTimestamp;
      }

      private void traceDebug(string message)
      {
         Debug.WriteLine(String.Format("[GitRepositoryUpdaterInternal] ({0}) {1}",
            _gitRepository.ProjectKey.ProjectName, message));
      }

      private void traceInformation(string message)
      {
         Trace.TraceInformation(String.Format("[GitRepositoryUpdaterInternal] ({0}) {1}",
            _gitRepository.ProjectKey.ProjectName, message));
      }

      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly IGitRepository _gitRepository;
      private readonly IExternalProcessManager _processManager;
      private readonly UpdateMode _updateMode;
      private readonly Action _onCloned;
      private readonly Action<string> _onFetched;

      private ExternalProcess.AsyncTaskDescriptor _updateOperationDescriptor;

      private bool _isDisposed;
      private CommitStorageUpdateContext _updatingContext;
      private Action<string> _onProgressChange;
      private Action _onUpdateStateChange;
      private DateTime _latestFullFetchTimestamp = DateTime.MinValue;

      private static int MaxShaInChunk = 2;
      private static int DelayBetweenChunksMs = 50;
   }
}

