using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;
using mrHelper.Client.Repository;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;

namespace mrHelper.StorageSupport
{
   internal class ComparisonInternal
   {
      internal ComparisonInternal(IEnumerable<DiffStruct> diffs, string baseSha, string headSha)
      {
         Diffs = diffs;
         this.BaseSha = baseSha;
         this.HeadSha = headSha;
      }

      internal IEnumerable<ComparisonInternal> Split(int chunkSize)
      {
         List<ComparisonInternal> splitted = new List<ComparisonInternal>();
         int remaining = Diffs.Count();
         while (remaining > 0)
         {
            DiffStruct[] diffsChunk = Diffs
               .Skip(Diffs.Count() - remaining)
               .Take(chunkSize)
               .ToArray();
            remaining -= diffsChunk.Length;
            splitted.Add(new ComparisonInternal(diffsChunk, BaseSha, HeadSha));
         }
         return splitted;
      }

      internal IEnumerable<DiffStruct> Diffs { get; }
      internal string BaseSha { get; }
      internal string HeadSha { get; }
   }

   /// <summary>
   /// </summary>
   internal class FileStorageUpdater : ILocalCommitStorageUpdater, IDisposable
   {
      /// <summary>
      /// </summary>
      internal FileStorageUpdater(
         ISynchronizeInvoke synchronizeInvoke,
         IFileStorage fileStorage,
         IRepositoryAccessor repositoryAccessor,
         Action onCloned,
         Action<FileRevision> onFetched)
      {
         _synchronizeInvoke = synchronizeInvoke;
         _fileStorage = fileStorage;
         _repositoryAccessor = repositoryAccessor;
         _onCloned = onCloned;
         _onFetched = onFetched;
      }

      public void StopUpdate()
      {
         if (!CanBeStopped())
         {
            return;
         }

         _repositoryAccessor.Cancel();
      }

      public bool CanBeStopped()
      {
         return false;
      }

      public void Dispose()
      {
         _repositoryAccessor.Dispose();
         _isDisposed = true;
      }

      async public Task StartUpdate(ICommitStorageUpdateContextProvider contextProvider, Action<string> onProgressChange,
         Action onUpdateStateChange)
      {
         if (onProgressChange == null)
         {
            return;
         }

         try
         {
            await update(contextProvider, onProgressChange, onUpdateStateChange, true, false);
         }
         catch (FileStorageUpdaterException ex)
         {
            if (ex is FileStorageUpdateCancelledException)
            {
               throw new LocalCommitStorageUpdaterCancelledException();
            }
            throw new LocalCommitStorageUpdaterFailedException("Cannot update file storage", ex);
         }
      }

      async public Task update(ICommitStorageUpdateContextProvider contextProvider, Action<string> onProgressChange,
         Action onUpdateStateChange, bool canClone, bool canSplit)
      {
         CommitStorageUpdateContext context = contextProvider?.GetContext();
         if (contextProvider == null || (context != null && context.BaseToHeads == null))
         {
            Debug.Assert(false);
            return;
         }

         if (_isDisposed || context == null)
         {
            return;
         }

         if (onProgressChange != null)
         {
            // save callbacks for operations that may start
            _onProgressChange = onProgressChange;
            _onUpdateStateChange = onUpdateStateChange;
         }

         if (UpdateContextUtils.IsWorthNewUpdate(context, _updatingContext))
         {
            await processContext(context, canSplit);
         }
         else
         {
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
               catch (FileStorageUpdaterException ex)
               {
                  ExceptionHandlers.Handle("Silent update failed", ex);
               }
            }), null);
      }

      async private Task processContext(CommitStorageUpdateContext context, bool canSplit)
      {
         if (!context.BaseToHeads.Any())
         {
            traceDebug("Empty context");
            return; // optimization. cannot do anything without Sha list
         }

         try
         {
            List<ComparisonInternal> completeComparisons = new List<ComparisonInternal>();
            IEnumerable<InternalUpdateContext> splitted = canSplit
               ? new InternalUpdateContext(context.BaseToHeads).Split(Constants.MaxShaInChunk)
               : new InternalUpdateContext[] { new InternalUpdateContext(context.BaseToHeads) };
            foreach (InternalUpdateContext internalContext in splitted)
            {
               IEnumerable<ComparisonInternal> comparisons = await doPreProcessContext(context, internalContext);
               completeComparisons.AddRange(comparisons);

               // this allows others to interleave with their (shorter) requests
               await TaskUtils.IfAsync(() => internalContext != splitted.Last() && !_isDisposed,
                  Constants.DelayBetweenChunksMs);
            }

            foreach (ComparisonInternal completeComparison in completeComparisons)
            {
               IEnumerable<ComparisonInternal> splittedComparisons = canSplit
                  ? completeComparison.Split(Constants.MaxDiffInChunk)
                  : new ComparisonInternal[] { completeComparison };

               foreach (ComparisonInternal comparison in splittedComparisons)
               {
                  await doProcessContext(context, comparison);

                  // this allows others to interleave with their (shorter) requests
                  await TaskUtils.IfAsync(() => comparison != splittedComparisons.Last() && !_isDisposed,
                     Constants.DelayBetweenChunksMs);
               }
            }
         }
         catch (RepositoryAccessorException ex)
         {
            throw new FileStorageUpdaterException("Cannot download a file from GitLab", ex);
         }
      }

      async private Task<IEnumerable<ComparisonInternal>> doPreProcessContext(CommitStorageUpdateContext context,
         InternalUpdateContext internalContext)
      {
         await TaskUtils.WhileAsync(() => _updatingContext != null);

         try
         {
            _updatingContext = context;

            List<Tuple<string, string>> baseToHeads = internalContext.BaseToHeads
               .SelectMany(x => x.Value, (kv, headSha) => new Tuple<string, string>(kv.Key, headSha))
               .ToList();

            traceDebug(String.Format("[LOCK][PRE] by {0} to download {1} comparisons",
               context.GetType(), baseToHeads.Count()));
            return await getComparisons(baseToHeads);
         }
         finally
         {
            traceDebug(String.Format("[UNLOCK][PRE] by {0}", context.GetType()));
            _updatingContext = null;
         }
      }

      async private Task doProcessContext(CommitStorageUpdateContext context, ComparisonInternal comparison)
      {
         await TaskUtils.WhileAsync(() => _updatingContext != null);

         try
         {
            _updatingContext = context;

            IEnumerable<FileRevision> missingRevisions = getMissingFileRevisions(comparison);

            traceDebug(String.Format("[LOCK] by {0} to download {1} files",
               context.GetType(), missingRevisions.Count()));
            await fetchCommitsAsync(missingRevisions);
         }
         finally
         {
            traceDebug(String.Format("[UNLOCK] by {0}", context.GetType()));
            _updatingContext = null;
         }
      }

      async private Task fetchCommitsAsync(IEnumerable<FileRevision> revisions)
      {
         bool cancelled = false;
         async Task loadRevision(FileRevision revision)
         {
            if (cancelled)
            {
               return;
            }

            GitLabSharp.Entities.File file = null;
            try
            {
               traceDebug(String.Format("Starting to download file \"{0}\" with SHA {1}...",
                  revision.GitFilePath.Value, revision.SHA));
               file = await _repositoryAccessor.LoadFile(
                  _fileStorage.ProjectKey, revision.GitFilePath.Value, revision.SHA);
            }
            finally
            {
               string action = file == null ? "Cancelled" : "Finished";
               traceDebug(String.Format("{0} downloading file \"{1}\" with SHA {2}",
                  action, revision.GitFilePath.Value, revision.SHA));
            }
            if (file == null)
            {
               cancelled = true;
               return;
            }

            string content = StringUtils.Base64Decode(file?.Content).Replace("\n", "\r\n");
            _fileStorage.FileCache.WriteFileRevision(revision, content);

            // TODO Report progress
            _onProgressChange?.Invoke("");
            _onUpdateStateChange?.Invoke();
         }

         await TaskUtils.RunConcurrentFunctionsAsync(revisions, loadRevision,
            Constants.MaxFilesInBatch, Constants.FilesInterBatchDelay, () => cancelled);
      }

      private IEnumerable<FileRevision> getMissingFileRevisions(ComparisonInternal comparison)
      {
         IEnumerable<FileRevision> baseFiles = FileStorageUtils.CreateFileRevisions(
            comparison.Diffs, comparison.BaseSha, true);
         IEnumerable<FileRevision> headFiles = FileStorageUtils.CreateFileRevisions(
            comparison.Diffs, comparison.HeadSha, false);
         return baseFiles
            .Concat(headFiles)
            .Where(x => !_fileStorage.FileCache.ContainsFileRevision(x));
      }

      async private Task<IEnumerable<ComparisonInternal>> getComparisons(IEnumerable<Tuple<string, string>> baseToHeads)
      {
         bool cancelled = false;
         List<ComparisonInternal> comparisons = new List<ComparisonInternal>();
         async Task loadComparison(Tuple<string, string> baseShaToHeadSha)
         {
            Comparison comparison = await getComparison(baseShaToHeadSha.Item1, baseShaToHeadSha.Item2);
            if (comparison == null)
            {
               cancelled = true;
               return;
            }
            comparisons.Add(new ComparisonInternal(comparison.Diffs, baseShaToHeadSha.Item1, baseShaToHeadSha.Item2));
         }

         await TaskUtils.RunConcurrentFunctionsAsync(baseToHeads, loadComparison,
            Constants.MaxComparisonInBatch, Constants.ComparisionInterBatchDelay, () => cancelled);
         return comparisons;
      }

      async private Task<Comparison> getComparison(string baseSha, string headSha)
      {
         Comparison comparison = _fileStorage.ComparisonCache.LoadComparison(baseSha, headSha);
         if (comparison != null)
         {
            return comparison;
         }

         try
         {
            traceDebug(String.Format("Starting to download comparison of {0} vs {1}...", baseSha, headSha));
            comparison = await _repositoryAccessor.Compare(_fileStorage.ProjectKey, baseSha, headSha);
         }
         finally
         {
            string action = comparison == null ? "Cancelled" : "Finished";
            traceDebug(String.Format("{0} downloading comparison of {1} vs {2}...", action, baseSha, headSha));
         }
         if (comparison == null)
         {
            return null;
         }

         _fileStorage.ComparisonCache.SaveComparison(baseSha, headSha, comparison);
         return comparison;
      }

      private void traceDebug(string message)
      {
         Debug.WriteLine(String.Format("[FileStorageUpdater] ({0}) {1}",
            _fileStorage.ProjectKey.ProjectName, message));
      }

      private void traceInformation(string message)
      {
         Trace.TraceInformation(String.Format("[FileStorageUpdater] ({0}) {1}",
            _fileStorage.ProjectKey.ProjectName, message));
      }

      private void traceWarning(string message)
      {
         Trace.TraceWarning(String.Format("[FileStorageUpdater] ({0}) {1}",
            _fileStorage.ProjectKey.ProjectName, message));
      }

      private void traceError(string message)
      {
         Trace.TraceError(String.Format("[FileStorageUpdater] ({0}) {1}",
            _fileStorage.ProjectKey.ProjectName, message));
      }

      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly IFileStorage _fileStorage;
      private readonly IRepositoryAccessor _repositoryAccessor;
      private readonly Action _onCloned;
      private readonly Action<FileRevision> _onFetched;

      private bool _isDisposed;
      private CommitStorageUpdateContext _updatingContext;
      private Action<string> _onProgressChange;
      private Action _onUpdateStateChange;
   }
}

