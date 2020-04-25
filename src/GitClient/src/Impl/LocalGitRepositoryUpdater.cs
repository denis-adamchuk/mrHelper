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
   internal class LocalGitRepositoryUpdater : ILocalGitRepositoryUpdater
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
      internal event Action Updated;

      async public Task CancelUpdate()
      {
         try
         {
            await _operationManager.Cancel(_updateOperationDescriptor);
         }
         finally
         {
            _updateOperationDescriptor = null;
         }
      }

      async public Task Update(IProjectUpdateContextProvider contextProvider, Action<string> onProgressChange)
      {
         if (contextProvider == null)
         {
            Debug.Assert(false);
            return;
         }

         if (onProgressChange != null)
         {
            _onProgressChange = onProgressChange;
         }

         while (_updating)
         {
            await Task.Delay(50);
         }

         _updating = true;
         try
         {
            IProjectUpdateContext context = await contextProvider.GetContext();
            await processContext(context);
         }
         finally
         {
            _updating = false;
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
            throw new RepositoryUpdateException("Cannot update git repository", ex);
         }
      }

      async private Task processFullProjectUpdate(FullUpdateContext context)
      {
         if (context.Sha == null)
         {
            Debug.Assert(false);
            Trace.TraceError("[LocalGitRepositoryUpdater] Unexpected project update content");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         if (!context.Sha.Any() || context.LatestChange == DateTime.MinValue)
         {
            // It is not always a problem. May happen when a MR is opened from Search tab
            // for a project that is not added to the list.
            Trace.TraceWarning("[LocalGitRepositoryUpdater] Repository will not be updated because of empty context");
         }

         DateTime prevLatestTimeStamp = _lastestFullUpdateTimestamp;
         if (_localGitRepository.ExpectingClone)
         {
            await cloneAsync(_updateMode == EUpdateMode.ShallowClone);
            _lastestFullUpdateTimestamp = context.LatestChange;
            Trace.TraceInformation(String.Format(
               "[LocalGitRepositoryUpdater] Repository cloned. Updating LatestChange timestamp to {0}",
               _lastestFullUpdateTimestamp.ToLocalTime().ToString()));
            Cloned?.Invoke();
         }

         if (context.LatestChange > _lastestFullUpdateTimestamp)
         {
            if (_updateMode != EUpdateMode.ShallowClone)
            {
               await fetchAsync(false);
            }
            _lastestFullUpdateTimestamp = context.LatestChange;
            Trace.TraceInformation(String.Format(
               "[LocalGitRepositoryUpdater] Repository {0} updated. Updating LatestChange timestamp to {1}",
               _updateMode == EUpdateMode.ShallowClone ? "not" : String.Empty,
               _lastestFullUpdateTimestamp.ToLocalTime().ToString()));
         }
         else if (context.LatestChange == _lastestFullUpdateTimestamp)
         {
            Trace.TraceInformation(String.Format("[LocalGitRepositoryUpdater] Repository not updated"));
         }
         else if (context.LatestChange < _lastestFullUpdateTimestamp)
         {
            // This is not a problem and may happen when, for example, a Merge Request with the most newest
            // version has been closed.
            Trace.TraceInformation("[LocalGitRepositoryUpdater] New LatestChange is older than a previous one");
         }

         if (_updateMode != EUpdateMode.FullCloneWithoutSingleCommitFetches)
         {
            await fetchCommitsAsync(context.Sha, _updateMode == EUpdateMode.ShallowClone);
         }

         if (_lastestFullUpdateTimestamp != prevLatestTimeStamp)
         {
            Updated?.Invoke();
         }
      }

      async private Task processPartialProjectUpdate(PartialUpdateContext context)
      {
         if (context.Sha == null || !context.Sha.Any())
         {
            Debug.Assert(false);
            Trace.TraceError("[LocalGitRepositoryUpdater] Unexpected project update content");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         if (_localGitRepository.ExpectingClone)
         {
            Trace.TraceError(
               "[LocalGitRepositoryUpdater] Partial updates cannot be applied to a not cloned repository");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         if (_updateMode == EUpdateMode.FullCloneWithoutSingleCommitFetches)
         {
            Trace.TraceError(
               "[LocalGitRepositoryUpdater] Partial updates are not supported in this repository");
            throw new RepositoryUpdateException("Cannot update git repository", null);
         }

         if (await fetchCommitsAsync(context.Sha, _updateMode == EUpdateMode.ShallowClone))
         {
            Updated?.Invoke();
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
         string arguments = String.Format("fetch {0}",
            getFetchArguments(null, shallowFetch));
         await doUpdateOperationAsync(arguments, _localGitRepository.Path);
      }

      async private Task<bool> fetchCommitsAsync(IEnumerable<string> shas, bool shallowFetch)
      {
         IEnumerable<string> goodSha = shas.Where(x => x != null).Distinct();

         int iCommit = 0;
         foreach (string sha in goodSha)
         {
            if (!await _localGitRepository.ContainsSHAAsync(sha))
            {
               string arguments = String.Format("fetch {0}",
                  getFetchArguments(sha, shallowFetch));
               await doUpdateOperationAsync(arguments, _localGitRepository.Path);
               ++iCommit;
            }
         }

         if (iCommit > 0)
         {
            Trace.TraceInformation(String.Format(
               "[LocalGitRepositoryUpdater] Fetched commits: {0}. Total: {1}", iCommit, goodSha.Count()));
            return true;
         }
         return false;
      }

      async private Task doUpdateOperationAsync(string arguments, string path)
      {
         ExternalProcess.AsyncTaskDescriptor descriptor = startUpdateOperation(arguments, path);
         await waitUpdateOperationAsync(arguments, descriptor);
      }

      private ExternalProcess.AsyncTaskDescriptor startUpdateOperation(string arguments, string path)
      {
         return _operationManager.CreateDescriptor("git", arguments, path,
            (status) => _onProgressChange?.Invoke(status));
      }

      private async Task waitUpdateOperationAsync(
         string arguments, ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         try
         {
            _updateOperationDescriptor = descriptor;
            Trace.TraceInformation(String.Format(
               "[LocalGitRepositoryUpdater] START git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments,
               _localGitRepository.Path,
               _localGitRepository.ProjectKey.ProjectName));
            await _operationManager.Wait(descriptor);
         }
         finally
         {
            Trace.TraceInformation(String.Format(
               "[LocalGitRepositoryUpdater] FINISH git with arguments \"{0}\" in \"{1}\" for {2}",
               arguments,
               _localGitRepository.Path,
               _localGitRepository.ProjectKey.ProjectName));
            _updateOperationDescriptor = null;
         }
      }

      private static string getCloneArguments(bool shallow)
      {
         return String.Format(" --progress {0} --no-tags"
              + " -c credential.helper=manager -c credential.interactive=auto -c credential.modalPrompt=true",
              shallow ? "--depth=1" : String.Empty);
      }

      private static string getFetchArguments(string sha, bool shallow)
      {
         if (sha == null)
         {
            return String.Format(" --progress --no-tags {0}",
               GitTools.SupportsFetchAutoGC() ? "--no-auto-gc" : String.Empty);
         }

         return String.Format(" --progress {0} --no-tags {1} {2}",
            String.Format("origin {0}:refs/keep-around/{0}", sha),
            shallow ? "--depth=1" : String.Empty,
            GitTools.SupportsFetchAutoGC() ? "--no-auto-gc" : String.Empty);
      }

      private readonly ILocalGitRepository _localGitRepository;
      private readonly IExternalProcessManager _operationManager;
      private readonly EUpdateMode _updateMode;

      private ExternalProcess.AsyncTaskDescriptor _updateOperationDescriptor;

      private bool _updating = false;
      private Action<string> _onProgressChange;
      private DateTime _lastestFullUpdateTimestamp = DateTime.MinValue;
   }
}

