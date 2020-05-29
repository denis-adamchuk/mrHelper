using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.GitClient
{
   /// <summary>
   /// Updates attached ILocalGitRepository object
   /// </summary>
   internal class LocalGitRepositoryUpdater : ILocalGitRepositoryUpdater, IDisposable
   {
      internal enum EUpdateMode
      {
         ShallowClone,                    // "git clone --depth=1" and "git fetch --depth=1 sha:/refs/keep-around/sha"
         FullCloneWithSingleCommitFetches // "git clone" and "git fetch" and "git fetch sha:/refs/keep-around/sha"
      }

      private class InternalUpdateContext
      {
         internal InternalUpdateContext(IEnumerable<string> sha)
         {
            Sha = sha;
         }

         internal IEnumerable<string> Sha { get; }
      }

      /// <summary>
      /// Bind to the specific LocalGitRepository object
      /// </summary>
      internal LocalGitRepositoryUpdater(
         ILocalGitRepository localGitRepository,
         IExternalProcessManager operationManager,
         EUpdateMode mode)
      {
         _localGitRepository = localGitRepository;
         _operationManager = operationManager;
         _updateMode = mode;
      }

      internal event Action Cloned;

      public void CancelUpdate()
      {
         if (_isDisposed)
         {
            return;
         }

         try
         {
            _operationManager.Cancel(_updateOperationDescriptor);
         }
         finally
         {
            _updateOperationDescriptor = null;
         }
      }

      public void Dispose()
      {
         _isDisposed = true;
      }

      public Task Update(IProjectUpdateContextProvider contextProvider, Action<string> onProgressChange)
      {
         return update(contextProvider, onProgressChange, true);
      }

      async public Task update(IProjectUpdateContextProvider contextProvider, Action<string> onProgressChange,
         bool canClone)
      {
         ProjectUpdateContext context = contextProvider?.GetContext();
         if (contextProvider == null || (context != null && context.Sha == null))
         {
            Debug.Assert(false);
            return;
         }

         if (_isDisposed || context == null || (_localGitRepository.ExpectingClone && !canClone))
         {
            return;
         }

         if (onProgressChange != null)
         {
            _onProgressChange = onProgressChange;
         }

         if (_updateOperationDescriptor != null)
         {
            _updateOperationDescriptor.OnProgressChange = onProgressChange;
         }

         if (isUpdateNeeded(context, _updatingContext))
         {
            await processContext(context);
         }
         else
         {
            await TaskUtils.WhileAsync(() => _updatingContext != null);
         }

         _onProgressChange = null;
      }

      async public Task SilentUpdate(IProjectUpdateContextProvider contextProvider)
      {
         try
         {
            await update(contextProvider, null, false);
         }
         catch (RepositoryUpdateException ex)
         {
            ExceptionHandlers.Handle("Silent update failed", ex);
         }
      }

      private IEnumerable<InternalUpdateContext> splitContext(ProjectUpdateContext context)
      {
         if (_updateMode != EUpdateMode.ShallowClone || !(context is FullUpdateContext))
         {
            return new InternalUpdateContext[] { new InternalUpdateContext(context.Sha.Distinct()) };
         }

         List<InternalUpdateContext> splitted = new List<InternalUpdateContext>();
         FullUpdateContext fullContext = context as FullUpdateContext;
         IEnumerable<string> sha = fullContext.Sha.Distinct();
         int remaining = sha.Count();
         while (remaining > 0)
         {
            string[] shaChunk = sha
               .Skip(sha.Count() - remaining)
               .Take(ShaInChunk)
               .ToArray();
            remaining -= shaChunk.Length;
            splitted.Add(new InternalUpdateContext(shaChunk));
         }
         return splitted;
      }

      async private Task processContext(ProjectUpdateContext context)
      {
         if (!context.Sha.Any())
         {
            // It is not always a problem. May happen when a MR is opened from Search tab
            // for a project that is not added to the list. Or when MR list is empty
            // for a project.
            traceDebug("Empty context");
         }

         try
         {
            await doPreProcessContext(context);

            IEnumerable<InternalUpdateContext> splitted = splitContext(context);
            foreach (InternalUpdateContext internalContext in splitted)
            {
               await doProcessContext(context, internalContext);

               // this allows others to interleave with their (shorter) requests
               await TaskUtils.IfAsync(() => internalContext != splitted.Last() && !_isDisposed, DelayBetweenChunksMs);
            }
         }
         catch (GitException ex)
         {
            if (ex is OperationCancelledException)
            {
               throw new UpdateCancelledException();
            }
            else if (ex is GitCallFailedException gfex
                  && gfex.InnerException is ExternalProcessFailureException pfex
                  && String.Join("\n", pfex.Errors).Contains("SSL certificate problem"))
            {
               throw new SecurityException(ex);
            }
            else if (ex is GitCallFailedException gfex2
                  && gfex2.InnerException is ExternalProcessFailureException pfex2
                  && String.Join("\n", pfex2.Errors).Contains("already exists and is not an empty directory"))
            {
               throw new NotEmptyDirectoryException(_localGitRepository.Path, ex);
            }
            throw new RepositoryUpdateException("Cannot update git repository", ex);
         }
      }

      async private Task doPreProcessContext(ProjectUpdateContext context)
      {
         await TaskUtils.WhileAsync(() => _updatingContext != null);

         try
         {
            Debug.Assert(_updateOperationDescriptor == null);

            _updatingContext = context;
            traceDebug(String.Format("[LOCK][PRE] by {0}", context.GetType()));

            DateTime prevFullUpdateTimestamp = updateTimestamp(context);

            if (_localGitRepository.ExpectingClone)
            {
               await cloneAsync(_updateMode == EUpdateMode.ShallowClone);
               traceInformation("Repository cloned.");
               Cloned?.Invoke();
            }
            else if (_updateMode != EUpdateMode.ShallowClone
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

      async private Task doProcessContext(ProjectUpdateContext context, InternalUpdateContext internalUpdateContext)
      {
         await TaskUtils.WhileAsync(() => _updatingContext != null);

         try
         {
            Debug.Assert(_updateOperationDescriptor == null);

            _updatingContext = context;
            traceDebug(String.Format("[LOCK] by {0}", context.GetType()));

            IEnumerable<string> missingSha = await getMissingSha(internalUpdateContext.Sha);
            await fetchCommitsAsync(missingSha, _updateMode == EUpdateMode.ShallowClone);

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
            _localGitRepository.ProjectKey.HostName,
            _localGitRepository.ProjectKey.ProjectName,
            StringUtils.EscapeSpaces(_localGitRepository.Path));
         await doUpdateOperationAsync(arguments, String.Empty);
      }

      async private Task fetchAsync(bool shallowFetch)
      {
         Debug.Assert(shallowFetch == false); // we don't support shallow fetch here

         string arguments = String.Format("fetch {0}",
            getFetchArguments(null, shallowFetch));
         await doUpdateOperationAsync(arguments, _localGitRepository.Path);
      }

      async private Task fetchCommitsAsync(IEnumerable<string> shas, bool shallowFetch)
      {
         int iCommit = 0;
         foreach (string sha in shas)
         {
            string arguments = String.Format("fetch {0}", getFetchArguments(sha, shallowFetch));
            await doUpdateOperationAsync(arguments, _localGitRepository.Path);
            ++iCommit;
         }

         if (iCommit > 0)
         {
            traceInformation(String.Format("Fetched commits: {0}", iCommit));
         }
      }

      async private Task<IEnumerable<string>> getMissingSha(IEnumerable<string> sha)
      {
         Exception exception = null;
         List<string> missingSha = new List<string>();
         await TaskUtils.RunConcurrentFunctionsAsync(sha,
            async x =>
            {
               if (exception != null)
               {
                  return;
               }

               try
               {
                  if (!await _localGitRepository.ContainsSHAAsync(x))
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
         return _operationManager.CreateDescriptor("git", arguments, path,
            _onProgressChange == null ? null : new Action<string>(status => _onProgressChange?.Invoke(status)));
      }

      private async Task waitUpdateOperationAsync(
         string arguments, ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         try
         {
            _updateOperationDescriptor = descriptor;
            traceInformation(String.Format("START git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments, _localGitRepository.Path, _localGitRepository.ProjectKey.ProjectName));
            await _operationManager.Wait(descriptor);
         }
         finally
         {
            traceInformation(String.Format("FINISH git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments, _localGitRepository.Path, _localGitRepository.ProjectKey.ProjectName));
            _updateOperationDescriptor = null;
         }
      }

      private static bool isUpdateNeeded(ProjectUpdateContext proposed, ProjectUpdateContext updating)
      {
         Debug.Assert(proposed != null);
         if (updating == null)
         {
            return true;
         }

         if (updating.LatestChange.HasValue && proposed.LatestChange.HasValue)
         {
            return proposed.LatestChange  > updating.LatestChange
               || (proposed.LatestChange == updating.LatestChange
                  && !areEqualShaCollections(proposed.Sha, updating.Sha));
         }
         else if (updating.LatestChange.HasValue || proposed.LatestChange.HasValue)
         {
            return true;
         }

         return !areEqualShaCollections(proposed.Sha, updating.Sha);
      }

      private static bool areEqualShaCollections(IEnumerable<string> a, IEnumerable<string> b)
      {
         return Enumerable.SequenceEqual(a.Distinct().OrderBy(x => x), b.Distinct().OrderBy(x => x));
      }

      private static string getCloneArguments(bool shallow)
      {
         return String.Format(" --progress {0} {1} {2}",
           shallow ? "--depth=1" : String.Empty,
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

      private DateTime updateTimestamp(ProjectUpdateContext context)
      {
         DateTime prevFullUpdateTimestamp = _latestFullUpdateTimestamp;
         if (context.LatestChange.HasValue)
         {
            Debug.Assert(context is FullUpdateContext);
            if (context.LatestChange.Value > _latestFullUpdateTimestamp)
            {
               traceInformation(String.Format("Updating LatestChange timestamp to {0}",
                  context.LatestChange.Value.ToLocalTime().ToString()));
               _latestFullUpdateTimestamp = context.LatestChange.Value;
            }
            else if (context.LatestChange == _latestFullUpdateTimestamp)
            {
               traceDebug("Timestamp not updated");
            }
            else if (context.LatestChange < _latestFullUpdateTimestamp)
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
         Debug.WriteLine(String.Format("[LocalGitRepositoryUpdater] ({0}) {1}",
            _localGitRepository.ProjectKey.ProjectName, message));
      }

      private void traceInformation(string message)
      {
         Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] ({0}) {1}",
            _localGitRepository.ProjectKey.ProjectName, message));
      }

      private void traceWarning(string message)
      {
         Trace.TraceWarning(String.Format("[LocalGitRepositoryUpdater] ({0}) {1}",
            _localGitRepository.ProjectKey.ProjectName, message));
      }

      private void traceError(string message)
      {
         Trace.TraceError(String.Format("[LocalGitRepositoryUpdater] ({0}) {1}",
            _localGitRepository.ProjectKey.ProjectName, message));
      }

      private readonly ILocalGitRepository _localGitRepository;
      private readonly IExternalProcessManager _operationManager;
      private readonly EUpdateMode _updateMode;

      private ExternalProcess.AsyncTaskDescriptor _updateOperationDescriptor;

      private bool _isDisposed;
      private ProjectUpdateContext _updatingContext;
      private Action<string> _onProgressChange;
      private DateTime _latestFullUpdateTimestamp = DateTime.MinValue;

      private const int ShaInChunk = 2;
      private const int DelayBetweenChunksMs = 50;
   }
}

