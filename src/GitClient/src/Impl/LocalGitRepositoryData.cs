using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mrHelper.GitClient
{
   internal class LocalGitRepositoryData : ILocalGitRepositoryData
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

      public IEnumerable<string> Get(GitRevisionArguments arguments)
      {
         return doGet(arguments, _cachedRevisions);
      }

      public IEnumerable<string> Get(GitNumStatArguments arguments)
      {
         return doGet(arguments, _cachedDiffStat);
      }

      async public Task Update(IEnumerable<GitDiffArguments> arguments)
      {
         await doUpdate(arguments, _cachedDiffs);
      }

      async public Task Update(IEnumerable<GitRevisionArguments> arguments)
      {
         await doUpdate(arguments, _cachedRevisions);
      }

      async public Task Update(IEnumerable<GitNumStatArguments> arguments)
      {
         await doUpdate(arguments, _cachedDiffStat);
      }

      internal void DisableUpdates()
      {
         _disabled = true;
      }

      private IEnumerable<string> doGet<T>(T arguments, Dictionary<T, IEnumerable<string>> cache)
      {
         if (!cache.ContainsKey(arguments))
         {
            cache[arguments] = ExternalProcess.Start("git", arguments.ToString(), true, _path).StdOut;
         }
         return cache[arguments];
      }

      async private Task doUpdate<T>(IEnumerable<T> arguments, Dictionary<T, IEnumerable<string>> cache)
      {
         await doBatchUpdate(arguments,
               async (x) =>
            {
               if (_disabled || cache.ContainsKey(x))
               {
                  return;
               }

               ExternalProcess.AsyncTaskDescriptor d = _operationManager.CreateDescriptor(
                  "git", x.ToString(), _path, null);
               await _operationManager.Wait(d);
               cache[x] = d.StdOut;
            });
      }

      async private Task doBatchUpdate<T>(IEnumerable<T> args, Func<T, Task> func)
      {
         int remaining = args.Count();
         while (remaining > 0 && !_disabled)
         {
            IEnumerable<Task> tasks = args
               .Skip(args.Count() - remaining)
               .Take(MaxGitInParallel)
               .Select(x => func(x));
            remaining -= MaxGitInParallel;
            try
            {
               await Task.WhenAll(tasks);
            }
            catch (GitOperationException)
            {
               // already handled
            }

            await Task.Delay(InterBatchDelay);
         }
      }

      private static int MaxGitInParallel  = 5;
      private static int InterBatchDelay   = 1000; // ms

      private string _path;
      private bool _disabled;
      private IExternalProcessManager _operationManager;

      private readonly Dictionary<GitDiffArguments, IEnumerable<string>> _cachedDiffs =
         new Dictionary<GitDiffArguments, IEnumerable<string>>();

      private readonly Dictionary<GitRevisionArguments, IEnumerable<string>> _cachedRevisions =
         new Dictionary<GitRevisionArguments, IEnumerable<string>>();

      private readonly Dictionary<GitNumStatArguments, IEnumerable<string>> _cachedDiffStat =
         new Dictionary<GitNumStatArguments, IEnumerable<string>>();
   }
}

