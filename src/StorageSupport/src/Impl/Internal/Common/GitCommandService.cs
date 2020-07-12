using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.StorageSupport
{
   internal abstract class GitCommandService : IAsyncGitCommandService, IDisposable
   {
      internal GitCommandService(IExternalProcessManager processManager)
      {
         _processManager = processManager;
      }

      public IEnumerable<string> ShowRevision(GitShowRevisionArguments arguments)
      {
         return runCommandAndCacheResult(arguments, _cachedRevisions);
      }

      public Task FetchAsync(GitShowRevisionArguments arguments)
      {
         return runCommandAndCacheResultAsync(arguments, _cachedRevisions);
      }

      public IEnumerable<string> ShowDiff(GitDiffArguments arguments)
      {
         return runCommandAndCacheResult(arguments, _cachedDiffs);
      }

      public Task FetchAsync(GitDiffArguments arguments)
      {
         return runCommandAndCacheResultAsync(arguments, _cachedDiffs);
      }

      abstract public int LaunchDiffTool(DiffToolArguments arguments);

      abstract public IFileRenameDetector RenameDetector { get; }

      public void Dispose()
      {
         _isDisposed = true;
      }

      abstract protected IEnumerable<string> getSync<T>(T arguments);
      abstract protected Task<IEnumerable<string>> getAsync<T>(T arguments);

      private IEnumerable<string> runCommandAndCacheResult<T>(T arguments, Dictionary<T, IEnumerable<string>> cache)
      {
         if (_isDisposed)
         {
            return null;
         }

         if (cache.TryGetValue(arguments, out IEnumerable<string> value))
         {
            return value;
         }

         if (!((dynamic)arguments).IsValid())
         {
            return null;
         }

         IEnumerable<string> result = getSync(arguments);
         if (result != null)
         {
            cache.Add(arguments, result);
         }
         return result;
      }

      async private Task runCommandAndCacheResultAsync<T>(T arguments, Dictionary<T, IEnumerable<string>> cache)
      {
         if (_isDisposed || cache.ContainsKey(arguments) || !((dynamic)arguments).IsValid())
         {
            return;
         }

         IEnumerable<string> result = await getAsync(arguments);
         if (result == null)
         {
            return;
         }
         cache[arguments] = result;
      }

      protected IEnumerable<string> getSyncFromExternalProcess(
         string appName, string arguments, string path, int[] successcodes)
      {
         try
         {
            IEnumerable<string> stdOut = ExternalProcess.Start(appName, arguments, true, path, successcodes).StdOut;
            return stdOut;
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException
             || ex is ExternalProcessSystemException
             || ex is ArgumentConversionException)
            {
               throw new GitNotAvailableDataException(ex);
            }
            throw;
         }
      }

      async protected Task<IEnumerable<string>> fetchAsyncFromExternalProcess(
         string appName, string arguments, string path, int[] successCodes)
      {
         try
         {
            ExternalProcess.AsyncTaskDescriptor d = _processManager.CreateDescriptor(
               appName, arguments, path, null, successCodes);
            await _processManager.Wait(d);
            return d.StdOut;
         }
         catch (Exception ex)
         {
            if (ex is OperationCancelledException)
            {
               return null;
            }
            if (ex is SystemException || ex is GitCallFailedException || ex is ArgumentConversionException)
            {
               throw new FetchFailedException(ex);
            }
            throw;
         }
      }

      private bool _isDisposed;

      protected readonly IExternalProcessManager _processManager;

      private readonly Dictionary<GitDiffArguments, IEnumerable<string>> _cachedDiffs =
         new Dictionary<GitDiffArguments, IEnumerable<string>>();

      private readonly Dictionary<GitShowRevisionArguments, IEnumerable<string>> _cachedRevisions =
         new Dictionary<GitShowRevisionArguments, IEnumerable<string>>();
   }
}

