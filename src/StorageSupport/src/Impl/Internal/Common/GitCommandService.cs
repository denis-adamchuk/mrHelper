using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;

namespace mrHelper.StorageSupport
{
   internal class GitCommandServiceInternalException : ExceptionEx
   {
      public GitCommandServiceInternalException(Exception ex)
         : base(String.Empty, ex)
      {
      }
   }

   internal abstract class GitCommandService : IAsyncGitCommandService, IDisposable
   {
      internal GitCommandService(IExternalProcessManager processManager)
      {
         _processManager = processManager;
         FullContextDiffProvider = new FullContextDiffProvider(this);
         GitDiffAnalyzer = new GitDiffAnalyzer(this);
      }

      public IEnumerable<string> ShowRevision(GitShowRevisionArguments arguments)
      {
         try
         {
            return runCommandAndCacheResult(arguments, _cachedRevisions);
         }
         catch (GitCommandServiceInternalException ex)
         {
            ExceptionHandlers.Handle(ex.Message, ex);
            throw new GitNotAvailableDataException(ex);
         }
      }

      async public Task FetchAsync(GitShowRevisionArguments arguments)
      {
         try
         {
            await runCommandAndCacheResultAsync(arguments, _cachedRevisions);
         }
         catch (GitCommandServiceInternalException ex)
         {
            ExceptionHandlers.Handle(ex.Message, ex);
            throw new FetchFailedException(ex);
         }
      }

      public IEnumerable<string> ShowDiff(GitDiffArguments arguments)
      {
         try
         {
            return runCommandAndCacheResult(arguments, _cachedDiffs);
         }
         catch (GitCommandServiceInternalException ex)
         {
            ExceptionHandlers.Handle(ex.Message, ex);
            throw new GitNotAvailableDataException(ex);
         }
      }

      async public Task FetchAsync(GitDiffArguments arguments)
      {
         try
         {
            await runCommandAndCacheResultAsync(arguments, _cachedDiffs);
         }
         catch (GitCommandServiceInternalException ex)
         {
            ExceptionHandlers.Handle(ex.Message, ex);
            throw new FetchFailedException(ex);
         }
      }

      async public Task FetchAsync(RevisionComparisonArguments arguments, RepositoryAccessor repositoryAccessor)
      {
         try
         {
            await runCommandAndCacheResultAsync(arguments, _cachedComparisons, repositoryAccessor);
         }
         catch (GitCommandServiceInternalException ex)
         {
            ExceptionHandlers.Handle(ex.Message, ex);
            throw new FetchFailedException(ex);
         }
      }

      public ComparisonEx GetComparison(RevisionComparisonArguments arguments)
      {
         if (_cachedComparisons.TryGetValue(arguments, out ComparisonEx value))
         {
            return value;
         }
         return null;
      }

      public int LaunchDiffTool(DiffToolArguments arguments)
      {
         try
         {
            return (int)runCommand(arguments);
         }
         catch (GitCommandServiceInternalException ex)
         {
            ExceptionHandlers.Handle(ex.Message, ex);
            throw new DiffToolLaunchException(ex);
         }
      }

      abstract public IFileRenameDetector RenameDetector { get; }

      public FullContextDiffProvider FullContextDiffProvider { get; }

      public GitDiffAnalyzer GitDiffAnalyzer { get; }

      public void Dispose()
      {
         _isDisposed = true;
      }

      abstract protected object runCommand(GitDiffArguments arguments);
      abstract protected object runCommand(GitShowRevisionArguments arguments);
      abstract protected object runCommand(DiffToolArguments arguments);
      abstract protected Task<object> runCommandAsync(GitDiffArguments arguments);
      abstract protected Task<object> runCommandAsync(GitShowRevisionArguments arguments);
      abstract protected Task<object> runCommandAsync(RevisionComparisonArguments arguments,
         RepositoryAccessor repositoryAccessor);

      private K runCommandAndCacheResult<T, K>(T arguments, SelfCleanUpDictionary<T, K> cache) where K : class
      {
         if (_isDisposed)
         {
            return default(K);
         }

         if (cache.TryGetValue(arguments, out K value))
         {
            return value;
         }

         if (!((dynamic)arguments).IsValid())
         {
            return default(K);
         }

         K result = (K)runCommand((dynamic)arguments);
         if (result != null)
         {
            cache.Add(arguments, result);
         }
         return result;
      }

      async private Task runCommandAndCacheResultAsync<T, K>(T arguments,
         SelfCleanUpDictionary<T, K> cache) where K : class
      {
         if (_isDisposed || cache.ContainsKey(arguments) || !((dynamic)arguments).IsValid())
         {
            return;
         }

         K result = (K)(await runCommandAsync((dynamic)arguments));
         if (result == null || cache.ContainsKey(arguments))
         {
            return;
         }
         cache.Add(arguments, result);
      }

      async private Task runCommandAndCacheResultAsync<T, K>(T arguments,
         SelfCleanUpDictionary<T, K> cache, RepositoryAccessor repositoryAccessor) where K : class
      {
         if (_isDisposed || cache.ContainsKey(arguments) || !((dynamic)arguments).IsValid())
         {
            return;
         }

         K result = (K)(await runCommandAsync((dynamic)arguments, repositoryAccessor));
         if (result == null || cache.ContainsKey(arguments))
         {
            return;
         }
         cache.Add(arguments, result);
      }

      protected ExternalProcess.Result startExternalProcess(
         string appName, string arguments, string path, bool wait, int[] successcodes)
      {
         try
         {
            return ExternalProcess.Start(appName, arguments, wait, path, successcodes);
         }
         catch (ExternalProcessFailureException ex)
         {
            throw new GitCommandServiceInternalException(ex);
         }
         catch (ExternalProcessSystemException ex)
         {
            throw new GitCommandServiceInternalException(ex);
         }
      }

      async protected Task<ExternalProcess.AsyncTaskDescriptor> startExternalProcessAsync(
         string appName, string arguments, string path, int[] successCodes)
      {
         try
         {
            ExternalProcess.AsyncTaskDescriptor d = _processManager.CreateDescriptor(
               appName, arguments, path, null, successCodes);
            await _processManager.Wait(d);
            return d;
         }
         catch (OperationCancelledException)
         {
            return null;
         }
         catch (SystemException ex)
         {
            throw new GitCommandServiceInternalException(ex);
         }
         catch (GitCallFailedException ex)
         {
            throw new GitCommandServiceInternalException(ex);
         }
      }

      private bool _isDisposed;

      protected readonly IExternalProcessManager _processManager;

      private readonly SelfCleanUpDictionary<RevisionComparisonArguments, ComparisonEx> _cachedComparisons =
         new SelfCleanUpDictionary<RevisionComparisonArguments, ComparisonEx>(CacheCleanupPeriodSeconds);

      private readonly SelfCleanUpDictionary<GitDiffArguments, IEnumerable<string>> _cachedDiffs =
         new SelfCleanUpDictionary<GitDiffArguments, IEnumerable<string>>(CacheCleanupPeriodSeconds);

      private readonly SelfCleanUpDictionary<GitShowRevisionArguments, IEnumerable<string>> _cachedRevisions =
         new SelfCleanUpDictionary<GitShowRevisionArguments, IEnumerable<string>>(CacheCleanupPeriodSeconds);

      private readonly static int CacheCleanupPeriodSeconds = 60 * 60 * 24 * 5; // 5 days
   }
}

