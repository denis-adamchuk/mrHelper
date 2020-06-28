using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.StorageSupport
{
   internal class LocalGitRepositoryData : ILocalGitCommitStorageData, IDisposable
   {
      internal LocalGitRepositoryData(IExternalProcessManager operationManager, string path)
      {
         _operationManager = operationManager;
         _path = path;
      }

      public IEnumerable<string> Get(GitDiffArguments arguments)
      {
         return doGet(arguments, _cachedDiffs);
      }

      public IEnumerable<string> Get(GitShowRevisionArguments arguments)
      {
         return doGet(arguments, _cachedRevisions);
      }

      async public Task LoadFromDisk(GitDiffArguments arguments)
      {
         await doUpdate(arguments, _cachedDiffs);
      }

      async public Task LoadFromDisk(GitShowRevisionArguments arguments)
      {
         await doUpdate(arguments, _cachedRevisions);
      }

      public void Dispose()
      {
         _isDisposed = true;
      }

      private IEnumerable<string> doGet<T>(T arguments, Dictionary<T, IEnumerable<string>> cache)
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

         try
         {
            IEnumerable<string> stdOut = ExternalProcess.Start("git", arguments.ToString(), true, _path).StdOut;
            cache.Add(arguments, stdOut);
            return stdOut;
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               throw new GitNotAvailableDataException(ex);
            }
            throw;
         }
      }

      async private Task doUpdate<T>(T arguments, Dictionary<T, IEnumerable<string>> cache)
      {
         if (_isDisposed || cache.ContainsKey(arguments) || !((dynamic)arguments).IsValid())
         {
            return;
         }

         try
         {
            ExternalProcess.AsyncTaskDescriptor d = _operationManager.CreateDescriptor(
               "git", arguments.ToString(), _path, null);
            await _operationManager.Wait(d);
            cache[arguments] = d.StdOut;
         }
         catch (Exception ex)
         {
            if (ex is OperationCancelledException)
            {
               return;
            }
            if (ex is SystemException || ex is GitCallFailedException)
            {
               throw new LoadFromDiskFailedException(ex);
            }
            throw;
         }
      }

      private readonly string _path;
      private bool _isDisposed;
      private readonly IExternalProcessManager _operationManager;

      private readonly Dictionary<GitDiffArguments, IEnumerable<string>> _cachedDiffs =
         new Dictionary<GitDiffArguments, IEnumerable<string>>();

      private readonly Dictionary<GitShowRevisionArguments, IEnumerable<string>> _cachedRevisions =
         new Dictionary<GitShowRevisionArguments, IEnumerable<string>>();
   }
}

