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

         Debug.Assert(_currentUpdateType.HasValue);
         _processManager.Cancel(_currentUpdateOperationDescriptor);
      }

      public bool CanBeStopped()
      {
         return !_isDisposed && _currentUpdateOperationDescriptor != null && _gitRepository.ExpectingClone;
      }

      public void Dispose()
      {
         _isDisposed = true;
      }

      async public Task StartUpdate(ICommitStorageUpdateContextProvider contextProvider,
         Action<string> onProgressChange, Action onUpdateStateChange)
      {
         if (onProgressChange == null || onUpdateStateChange == null)
         {
            throw new NotImplementedException(); // not tested cases
         }

         try
         {
            traceInformation(String.Format("StartUpdate() called with context of type {0}",
               contextProvider?.GetContext()?.GetType().ToString() ?? "null"));
            registerCallbacks(onProgressChange, onUpdateStateChange);
            await doUpdate(true, contextProvider?.GetContext(), onProgressChange, onUpdateStateChange);
         }
         catch (GitCommandException ex)
         {
            handleException(ex);
         }
         finally
         {
            unregisterCallbacks(onProgressChange, onUpdateStateChange);
            reportProgress(onProgressChange, String.Empty);
            traceInformation("StartUpdate() finished");
         }
      }

      public void RequestUpdate(ICommitStorageUpdateContextProvider contextProvider, Action onFinished)
      {
         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
            {
               if (_gitRepository.ExpectingClone)
               {
                  traceInformation("RequestUpdate() does nothing because repository is not cloned");
                  return;
               }

               try
               {
                  traceInformation(String.Format("RequestUpdate() called with context of type {0}",
                     contextProvider?.GetContext()?.GetType().ToString() ?? "null"));

                  await doUpdate(false, contextProvider?.GetContext(), null, null);
                  onFinished?.Invoke();
               }
               catch (GitCommandException ex)
               {
                  ExceptionHandlers.Handle("Silent update failed", ex);
               }
               finally
               {
                  traceInformation("RequestUpdate() finished");
               }
            }), null);
      }

      async public Task doUpdate(bool isAwaitedUpdate, CommitStorageUpdateContext context,
         Action<string> onProgressChange, Action onUpdateStateChange)
      {
         if (context == null || context.BaseToHeads == null || _isDisposed)
         {
            return;
         }

         var flatContext = flattenDictionary(context.BaseToHeads);
         int totalShaCount = flatContext.Count();
         traceInformation(String.Format("Context commits: {0}, latest change: {1}",
            totalShaCount, context.LatestChange?.ToLocalTime().ToString() ?? "N/A"));

         await cloneAsync(isAwaitedUpdate);
         await fetchAllAsync(isAwaitedUpdate, context.LatestChange);
         if (totalShaCount == 0)
         {
            return;
         }

         int fetchedShaCount = 0; // accumulator
         IEnumerable<InternalUpdateContext> internalContexts = createInternalContexts(flatContext, !isAwaitedUpdate);
         traceDebug(String.Format("Number of internal contexts is {0}", internalContexts.Count()));
         foreach (InternalUpdateContext internalContext in internalContexts)
         {
            await fetchCommitsAsync(isAwaitedUpdate, totalShaCount, fetchedShaCount, internalContext.Sha,
               onProgressChange, onUpdateStateChange);
            fetchedShaCount += internalContext.Sha.Count();
            await suspendToProcessOtherRequests(isAwaitedUpdate, fetchedShaCount < totalShaCount);
         }
      }

      private async Task suspendToProcessOtherRequests(bool isAwaitedUpdate, bool areRemainingCommits)
      {
         bool needSuspendFetch =
               !_isDisposed
            && !isAwaitedUpdate
            && (_pendingAwaitedUpdateCount > 0 || _pendingNonAwaitedUpdateCount > 0)
            && areRemainingCommits;
         traceDebug(String.Format("Suspending fetch loop: {0}", needSuspendFetch.ToString()));
         await TaskUtils.IfAsync(() => needSuspendFetch, FetchSuspendDelayMs);
      }

      private IEnumerable<InternalUpdateContext> createInternalContexts(IEnumerable<string> flatContext,
         bool canSplitContext)
      {
         return canSplitContext
            ? new InternalUpdateContext(flatContext).Split(MaxShaInChunk)
            : new InternalUpdateContext[] { new InternalUpdateContext(flatContext) };
      }

      static private IEnumerable<string> flattenDictionary(Dictionary<string, IEnumerable<string>> dict)
      {
         List<string> result = new List<string>();
         result.AddRange(dict.Keys);
         foreach (IEnumerable<string> values in dict.Values)
         {
            result.AddRange(values);
         }
         return result;
      }

      async private Task<IEnumerable<string>> selectMissingSha(IEnumerable<string> sha)
      {
         Exception exception = null;
         List<string> missingSha = new List<string>();
         await TaskUtils.RunConcurrentFunctionsAsync(sha.Distinct(),
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
            Constants.GetMissingShaInBatch, Constants.GetMissingShaInterBatchDelay, () => exception != null);

         if (exception != null)
         {
            throw exception;
         }

         return missingSha;
      }

      async private Task cloneAsync(bool isAwaitedUpdate)
      {
         if (!_gitRepository.ExpectingClone)
         {
            return;
         }

         traceInformation(String.Format("Pending clone. Already locked = {0}. Is awaited update = {1}",
            _currentUpdateType.HasValue.ToString(), isAwaitedUpdate.ToString()));

         await doExclusiveUpdateOperationAsync(isAwaitedUpdate,
            async () =>
         {
            if (!_gitRepository.ExpectingClone)
            {
               traceInformation("Clone is no longer needed");
               return;
            }

            string arguments = String.Format("clone {0} {1}/{2} {3}",
               getCloneArguments(isShallowCloneEnabled()), _gitRepository.ProjectKey.HostName,
               _gitRepository.ProjectKey.ProjectName, StringUtils.EscapeSpaces(_gitRepository.Path));
            await doGitUpdateAsync(arguments, String.Empty);
            _onCloned?.Invoke();
         });
      }

      async private Task fetchAllAsync(bool isAwaitedUpdate, DateTime? latestChange)
      {
         DateTime prevFullUpdateTimestamp = updateTimestamp(latestChange);
         if (isShallowCloneEnabled() || !latestChange.HasValue || latestChange.Value <= prevFullUpdateTimestamp)
         {
            return;
         }

         traceInformation(String.Format("Pending full fetch. Already locked = {0}. Is awaited update = {1}",
            _currentUpdateType.HasValue.ToString(), isAwaitedUpdate.ToString()));

         await doExclusiveUpdateOperationAsync(isAwaitedUpdate,
            async () =>
         {
            string arguments = String.Format("fetch {0}", getFetchArguments(null, false));
            await doGitUpdateAsync(arguments, _gitRepository.Path);
         });
      }

      async private Task fetchCommitsAsync(bool isAwaitedUpdate, int totalMissingShaCount, int fetchedShaCount,
         IEnumerable<string> shas, Action<string> onProgressChange, Action onUpdateStateChange)
      {
         traceInformation(String.Format("Pending per-commit fetch. Already locked = {0}. Is awaited update = {1}",
            _currentUpdateType.HasValue.ToString(), isAwaitedUpdate.ToString()));

         // don't report git internal messages to user, report completion progress manually
         unregisterCallbacks(onProgressChange, null);
         reportWaitingReason(onProgressChange);

         await doExclusiveUpdateOperationAsync(isAwaitedUpdate,
            async () =>
         {
            reportProgress(onProgressChange, "Checking for missing commits...");
            IEnumerable<string> missingSha = await selectMissingSha(shas);
            traceDebug(String.Format("Selected {0} missing commits from {1} requested commits",
               missingSha.Count(), shas.Count()));
            if (!missingSha.Any())
            {
               reportCompletionProgress(totalMissingShaCount, fetchedShaCount, onProgressChange);
            }

            foreach (string sha in missingSha)
            {
               string arguments = String.Format("fetch {0}", getFetchArguments(sha, isShallowCloneEnabled()));
               await doGitUpdateAsync(arguments, _gitRepository.Path);
               _onFetched?.Invoke(sha);

               ++fetchedShaCount;
               reportCompletionProgress(totalMissingShaCount, fetchedShaCount, onProgressChange);
            }
         });
      }

      private void reportWaitingReason(Action<string> onProgressChange)
      {
         if (_currentUpdateType.HasValue)
         {
            if (_currentUpdateType.Value == UpdateType.Awaited)
            {
               reportProgress(onProgressChange, "Waiting for completion of a concurrent update...");
            }
            else
            {
               // TODO This status does not change when _currentUpdateType changes,
               // consider wrapping _currentUpdateType into a property which triggers
               // some event on its change and notifies interested waiters.
               reportProgress(onProgressChange, "Suspending background updates...");
            }
         }
      }

      async private Task doExclusiveUpdateOperationAsync(bool isAwaitedUpdate, Func<Task> updateOperation)
      {
         _pendingAwaitedUpdateCount    += (isAwaitedUpdate ? 1 : 0);
         _pendingNonAwaitedUpdateCount += (isAwaitedUpdate ? 0 : 1);

         bool isLocked() => _currentUpdateType.HasValue || (!isAwaitedUpdate && _pendingAwaitedUpdateCount != 0);
         await TaskUtils.WhileAsync(() => isLocked(), RequestCheckIntervalMs);

         _pendingAwaitedUpdateCount    -= (isAwaitedUpdate ? 1 : 0);
         _pendingNonAwaitedUpdateCount -= (isAwaitedUpdate ? 0 : 1);

         try
         {
            traceDebug(String.Format(
               "LOCK. isAwaitedUpdate={0}, _pendingAwaitedUpdateCount={1}, _pendingNonAwaitedUpdateCount={2}",
               isAwaitedUpdate, _pendingAwaitedUpdateCount, _pendingNonAwaitedUpdateCount));
            Debug.Assert(_currentUpdateOperationDescriptor == null);
            _currentUpdateType = isAwaitedUpdate ? UpdateType.Awaited : UpdateType.NonAwaited;
            await updateOperation?.Invoke();
         }
         finally
         {
            traceDebug(String.Format(
               "UNLOCK. isAwaitedUpdate={0}, _pendingAwaitedUpdateCount={1}, _pendingNonAwaitedUpdateCount={2}",
               isAwaitedUpdate, _pendingAwaitedUpdateCount, _pendingNonAwaitedUpdateCount));
            Debug.Assert(_currentUpdateOperationDescriptor == null);
            _currentUpdateType = null;
         }
      }

      private void registerCallbacks(Action<string> onProgressChange, Action onUpdateStateChange)
      {
         _onProgressChangeCallbacks.Add(onProgressChange);
         _onUpdateStateChangeCallbacks.Add(onUpdateStateChange);
      }

      private void unregisterCallbacks(Action<string> onProgressChange, Action onUpdateStateChange)
      {
         _onProgressChangeCallbacks.Remove(onProgressChange);
         _onUpdateStateChangeCallbacks.Remove(onUpdateStateChange);
      }

      async private Task doGitUpdateAsync(string arguments, string path)
      {
         Debug.Assert(_currentUpdateType.HasValue);
         Debug.Assert(_currentUpdateOperationDescriptor == null);
         if (_isDisposed)
         {
            return;
         }

         void onProgressChangeAggregate(string status) => _onProgressChangeCallbacks?.ForEach(x => x?.Invoke(status));
         void onUpdateStateChangeAggregate() => _onUpdateStateChangeCallbacks?.ForEach(x => x?.Invoke());

         ExternalProcess.AsyncTaskDescriptor descriptor = _processManager.CreateDescriptor(
            "git", arguments, path, onProgressChangeAggregate, null);

         try
         {
            _currentUpdateOperationDescriptor = descriptor;
            onUpdateStateChangeAggregate();
            traceInformation(String.Format("START git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments, _gitRepository.Path, _gitRepository.ProjectKey.ProjectName));
            await _processManager.Wait(descriptor);
         }
         finally
         {
            traceInformation(String.Format("FINISH git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments, _gitRepository.Path, _gitRepository.ProjectKey.ProjectName));
            _currentUpdateOperationDescriptor = null;
            onUpdateStateChangeAggregate();
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

      private DateTime updateTimestamp(DateTime? latestChange)
      {
         DateTime prevFullUpdateTimestamp = _latestFullFetchTimestamp;
         if (latestChange.HasValue)
         {
            if (latestChange.Value > _latestFullFetchTimestamp)
            {
               traceInformation(String.Format("Updating LatestChange timestamp to {0}",
                  latestChange.Value.ToLocalTime().ToString()));
               _latestFullFetchTimestamp = latestChange.Value;
            }
            else if (latestChange < _latestFullFetchTimestamp)
            {
               // This is not a problem and may happen when, for example, a Merge Request with the most newest
               // version has been closed.
               traceInformation("New LatestChange is older than a previous one");
            }
         }
         return prevFullUpdateTimestamp;
      }

      private bool isShallowCloneEnabled()
      {
         return _updateMode == UpdateMode.ShallowClone;
      }

      private void handleException(GitCommandException ex)
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

      private void reportProgress(Action<string> onProgressChange, string message)
      {
         onProgressChange?.Invoke(message);
         if (onProgressChange != null)
         {
            traceInformation(String.Format("Reported to user: \"{0}\"", message));
         }
      }

      private int calculateCompletionPercentage(int totalCount, int completeCount)
      {
         return Convert.ToInt32(Convert.ToDouble(completeCount) / totalCount * 100);
      }

      private void reportCompletionProgress(int totalMissingShaCount, int fetchedShaCount,
         Action<string> onProgressChange)
      {
         if (onProgressChange != null)
         {
            int percentage = calculateCompletionPercentage(totalMissingShaCount, fetchedShaCount);
            string message = String.Format("Commits download progress: {0}%", percentage);
            reportProgress(onProgressChange, message);
         }
      }

      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly IGitRepository _gitRepository;
      private readonly IExternalProcessManager _processManager;
      private readonly UpdateMode _updateMode;
      private readonly Action _onCloned;
      private readonly Action<string> _onFetched;

      private bool _isDisposed;
      private int _pendingAwaitedUpdateCount;
      private int _pendingNonAwaitedUpdateCount;
      private DateTime _latestFullFetchTimestamp = DateTime.MinValue;

      private enum UpdateType
      {
         Awaited,
         NonAwaited
      }
      private UpdateType? _currentUpdateType;
      private ExternalProcess.AsyncTaskDescriptor _currentUpdateOperationDescriptor;

      /// <summary>
      /// These callbacks react on everything related to the _currentUpdateOperationDescriptor
      /// </summary>
      private readonly List<Action<string>> _onProgressChangeCallbacks = new List<Action<string>>();
      private readonly List<Action> _onUpdateStateChangeCallbacks = new List<Action>();

      private static int MaxShaInChunk = 1;
      private static int RequestCheckIntervalMs = 50;

      // suspend delay shall be big enough to allow other to check for a lock and acquire it
      private static int FetchSuspendDelayMs = RequestCheckIntervalMs * 4;
   }
}

