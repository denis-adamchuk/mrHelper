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
         ShallowClone, // implies single commit fetching
         FullCloneWithoutSingleCommitFetches,
         FullCloneWithSingleCommitFetches
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

      async public Task Update(IProjectUpdateContextProvider contextProvider, Action<string> onProgressChange)
      {
         if (_isDisposed)
         {
            return;
         }

         if (contextProvider == null)
         {
            Debug.Assert(false);
            return;
         }

         IProjectUpdateContext context = await contextProvider.GetContext();
         if (context == null)
         {
            return;
         }

         bool needUpdate = isUpdateNeeded(context, _updatingContext);

         if (onProgressChange != null)
         {
            _onProgressChange = onProgressChange;
         }

         if (_updateOperationDescriptor != null)
         {
            _updateOperationDescriptor.OnProgressChange = onProgressChange;
         }

         await TaskUtils.WhileAsync(() => _updatingContext != null);

         if (needUpdate)
         {
            _updatingContext = context;
            try
            {
               await processContext(context);
            }
            finally
            {
               _updatingContext = null;
            }
         }

         _onProgressChange = null;
      }

      async public Task SilentUpdate(IProjectUpdateContextProvider contextProvider)
      {
         try
         {
            await Update(contextProvider, null);
         }
         catch (RepositoryUpdateException ex)
         {
            ExceptionHandlers.Handle("Silent update failed", ex);
         }
      }

      async private Task processContext(IProjectUpdateContext context)
      {
         try
         {
            Debug.Assert(_updateOperationDescriptor == null);

            if (context is FullUpdateContext ps)
            {
               await processFullProjectUpdate(ps);
            }
            else if (context is PartialUpdateContext pu)
            {
               await processPartialProjectUpdate(pu);
            }

            Debug.Assert(_updateOperationDescriptor == null);
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

      async private Task processFullProjectUpdate(FullUpdateContext context)
      {
         if (context.Sha == null)
         {
            Debug.Assert(false);
            traceError("Unexpected project update content");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         if (!context.Sha.Any())
         {
            // It is not always a problem. May happen when a MR is opened from Search tab
            // for a project that is not added to the list. Or when MR list is empty
            // for a project.
            traceDebug("Empty context");
         }

         DateTime prevLatestTimeStamp = _latestFullUpdateTimestamp;
         if (_localGitRepository.ExpectingClone)
         {
            await cloneAsync(_updateMode == EUpdateMode.ShallowClone);
            _latestFullUpdateTimestamp = context.LatestChange;
            traceInformation(String.Format("Repository cloned. Updating LatestChange timestamp to {0}",
               _latestFullUpdateTimestamp.ToLocalTime().ToString()));
            Cloned?.Invoke();
         }

         if (context.LatestChange > _latestFullUpdateTimestamp)
         {
            if (_updateMode != EUpdateMode.ShallowClone)
            {
               await fetchAsync(false);
            }
            _latestFullUpdateTimestamp = context.LatestChange;
            traceInformation(String.Format("Repository {0} updated. Updating LatestChange timestamp to {1}",
               _updateMode == EUpdateMode.ShallowClone ? "not" : String.Empty,
               _latestFullUpdateTimestamp.ToLocalTime().ToString()));
         }
         else if (context.LatestChange == _latestFullUpdateTimestamp)
         {
            traceDebug("Repository not updated");
         }
         else if (context.LatestChange < _latestFullUpdateTimestamp)
         {
            // This is not a problem and may happen when, for example, a Merge Request with the most newest
            // version has been closed.
            traceInformation("New LatestChange is older than a previous one");
         }

         if (_updateMode != EUpdateMode.FullCloneWithoutSingleCommitFetches)
         {
            await fetchCommitsAsync(context.Sha, _updateMode == EUpdateMode.ShallowClone);
         }
      }

      async private Task processPartialProjectUpdate(PartialUpdateContext context)
      {
         if (context.Sha == null || !context.Sha.Any())
         {
            Debug.Assert(false);
            traceError("Unexpected project update content");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         if (_localGitRepository.ExpectingClone)
         {
            traceError("Partial updates cannot be applied to a not cloned repository");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         if (_updateMode == EUpdateMode.FullCloneWithoutSingleCommitFetches)
         {
            traceError("Partial updates are not supported in this repository");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         await fetchCommitsAsync(context.Sha, _updateMode == EUpdateMode.ShallowClone);
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
         string arguments = String.Format("fetch {0}",
            getFetchArguments(null, shallowFetch));
         await doUpdateOperationAsync(arguments, _localGitRepository.Path);
      }

      async private Task fetchCommitsAsync(IEnumerable<string> shas, bool shallowFetch)
      {
         IEnumerable<string> goodSha = shas.Where(x => x != null).Distinct();

         Exception exception = null;
         List<string> missingSha = new List<string>();
         await TaskUtils.RunConcurrentFunctionsAsync(goodSha,
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

         int iCommit = 0;
         foreach (string sha in missingSha)
         {
            string arguments = String.Format("fetch {0}",
               getFetchArguments(sha, shallowFetch));
            await doUpdateOperationAsync(arguments, _localGitRepository.Path);
            ++iCommit;
         }

         if (iCommit > 0)
         {
            traceInformation(String.Format("Fetched commits: {0}. Total: {1}", iCommit, goodSha.Count()));
         }
      }

      async private Task doUpdateOperationAsync(string arguments, string path)
      {
         ExternalProcess.AsyncTaskDescriptor descriptor = startUpdateOperation(arguments, path);
         await waitUpdateOperationAsync(arguments, descriptor);
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

      private static bool isUpdateNeeded(IProjectUpdateContext proposed, IProjectUpdateContext updating)
      {
         Debug.Assert(proposed != null);
         if (updating == null)
         {
            return true;
         }

         if (updating is FullUpdateContext fullUpdating)
         {
            if (proposed is PartialUpdateContext)
            {
               return true;
            }

            FullUpdateContext fullProposed = proposed as FullUpdateContext;
            return fullProposed.LatestChange  > fullUpdating.LatestChange
               || (fullProposed.LatestChange == fullUpdating.LatestChange
                  && !areEqualShaCollections(fullProposed.Sha, fullUpdating.Sha));
         }

         PartialUpdateContext partialUpdating = updating as PartialUpdateContext;
         if (proposed is FullUpdateContext)
         {
            return true;
         }

         PartialUpdateContext partialProposed = proposed as PartialUpdateContext;
         return !areEqualShaCollections(partialProposed.Sha, partialUpdating.Sha);
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
      private IProjectUpdateContext _updatingContext;
      private Action<string> _onProgressChange;
      private DateTime _latestFullUpdateTimestamp = DateTime.MinValue;
   }
}

