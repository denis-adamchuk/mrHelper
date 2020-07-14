using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using GitLabSharp.Entities;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Repository;

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
         Func<int> getStorageCount)
      {
         _synchronizeInvoke = synchronizeInvoke;
         _fileStorage = fileStorage;
         _repositoryAccessor = repositoryAccessor;
         _getStorageCount = getStorageCount;
      }

      public void StopUpdate() { }
      public bool CanBeStopped()
      {
         // TODO It is a nice extra feature to allow stop downloading
         return false;
      }

      public void Dispose()
      {
         _repositoryAccessor.Dispose();
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
            ++_activeAwaitedUpdateRequestCount;
            traceInformation(String.Format("StartUpdate() called with context of type {0}",
               contextProvider?.GetContext()?.GetType().ToString() ?? "null"));
            await doUpdate(true, contextProvider?.GetContext(), onProgressChange);
         }
         catch (RepositoryAccessorException ex)
         {
            throw new LocalCommitStorageUpdaterFailedException("Cannot update file storage", ex);
         }
         finally
         {
            --_activeAwaitedUpdateRequestCount;
            reportProgress(onProgressChange, String.Empty);
            traceInformation("StartUpdate() finished");
         }
      }

      public void RequestUpdate(ICommitStorageUpdateContextProvider contextProvider, Action onFinished)
      {
         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
            {
               try
               {
                  traceInformation(String.Format("RequestUpdate() called with context of type {0}, storage count is {1}",
                     contextProvider?.GetContext()?.GetType().ToString() ?? "null", _getStorageCount()));

                  await doUpdate(false, contextProvider?.GetContext(), null);
                  onFinished?.Invoke();
               }
               catch (RepositoryAccessorException ex)
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
         Action<string> onProgressChange)
      {
         if (context == null || context.BaseToHeads == null || _isDisposed)
         {
            return;
         }

         reportProgress(onProgressChange, "Downloading meta-information...");
         IEnumerable<ComparisonInternal> comparisons = await fetchComparisonsAsync(isAwaitedUpdate, context);
         traceInformation(String.Format("Got {0} comparisons, isAwaitedUpdate={1}",
            comparisons.Count(), isAwaitedUpdate.ToString()));
         traceInformation("List of comparisons to process:");
         foreach (ComparisonInternal comparison in comparisons)
         {
            traceInformation(String.Format("{0} vs {1} ({2} files)",
               comparison.BaseSha, comparison.HeadSha, comparison.Diffs.Count()));
         }

         reportProgress(onProgressChange, "Starting to download files from GitLab...");
         await processComparisonsAsync(isAwaitedUpdate, context, onProgressChange, comparisons);
         reportProgress(onProgressChange, "Files downloaded");
      }

      async private Task<IEnumerable<ComparisonInternal>> fetchComparisonsAsync(bool isAwaitedUpdate,
         CommitStorageUpdateContext context)
      {
         List<Tuple<string, string>> baseToHeads = context.BaseToHeads
            .SelectMany(x => x.Value, (kv, headSha) => new Tuple<string, string>(kv.Key, headSha))
            .ToList();

         bool cancelled = _isDisposed;
         List<ComparisonInternal> comparisons = new List<ComparisonInternal>();
         async Task doFetch(Tuple<string, string> baseShaToHeadSha)
         {
            if (cancelled)
            {
               return;
            }

            await suspendProcessingOfNonAwaitedUpdate(isAwaitedUpdate);
            Comparison comparison = await fetchSingleComparisonAsync(baseShaToHeadSha.Item1, baseShaToHeadSha.Item2);
            if (comparison == null || _isDisposed)
            {
               cancelled = true;
               return;
            }
            comparisons.Add(new ComparisonInternal(comparison.Diffs, baseShaToHeadSha.Item1, baseShaToHeadSha.Item2));
         }

         await TaskUtils.RunConcurrentFunctionsAsync(baseToHeads, doFetch,
            () => getComparisonBatchLimits(isAwaitedUpdate), () => cancelled);
         return comparisons;
      }

      private static async Task suspendProcessingOfNonAwaitedUpdate(bool isAwaitedUpdate)
      {
         // suspend all background work while processing `awaited' requests
         await TaskUtils.WhileAsync(() => !isAwaitedUpdate && _activeAwaitedUpdateRequestCount > 0);
      }

      async private Task<Comparison> fetchSingleComparisonAsync(string baseSha, string headSha)
      {
         Comparison comparison = _fileStorage.ComparisonCache.LoadComparison(baseSha, headSha);
         if (comparison != null)
         {
            return comparison;
         }

         traceDebug(String.Format("Fetching comparison {0} vs {1}...", baseSha, headSha));
         comparison = await _repositoryAccessor.Compare(_fileStorage.ProjectKey, baseSha, headSha);
         if (comparison == null)
         {
            return null;
         }

         _fileStorage.ComparisonCache.SaveComparison(baseSha, headSha, comparison);
         traceDebug(String.Format("Saved comparison {0} vs {1}", baseSha, headSha));
         return comparison;
      }

      private async Task processComparisonsAsync(bool isAwaitedUpdate, CommitStorageUpdateContext context,
         Action<string> onProgressChange, IEnumerable<ComparisonInternal> comparisons)
      {
         List<FileRevision> allRevisions = new List<FileRevision>();
         allRevisions.AddRange(comparisons.SelectMany(x => extractRevisionsFromComparison(x)));

         FileRevision[] initialMissingRevisions = selectMissingFileRevisions(allRevisions).ToArray();
         int initialMissingCount = initialMissingRevisions.Length;
         traceInformation(String.Format("Downloading file revisions. Total: {0}, Missing: {1}, isAwaitedUpdate={2}",
            allRevisions.Count(), initialMissingCount, isAwaitedUpdate.ToString()));
         if (initialMissingCount == 0)
         {
            return;
         }

         bool needTraceProgress = onProgressChange != null;
         await fetchRevisionsAsync(isAwaitedUpdate, initialMissingRevisions, onProgressChange,
            () => needTraceProgress ? initialMissingCount - selectMissingFileRevisions(allRevisions).Count() : 0);
      }

      async private Task fetchRevisionsAsync(bool isAwaitedUpdate, IEnumerable<FileRevision> missingRevisions,
         Action<string> onProgressChange, Func<int> getActualFetchedCount)
      {
         bool cancelled = _isDisposed;

         int fetchedByMeCount = 0; // this counter allows to not call getActualFetchedCount() on each iteration
         int fetchedCount = getActualFetchedCount();
         async Task doFetch(FileRevision revision)
         {
            if (cancelled || !isMissingRevision(revision))
            {
               traceDebug(String.Format("Skipped file {0} with SHA {1}", revision.GitFilePath.Value, revision.SHA));
               return;
            }

            await suspendProcessingOfNonAwaitedUpdate(isAwaitedUpdate);
            if (!await fetchSingleRevisionAsync(revision) || _isDisposed)
            {
               cancelled = true;
               return;
            }

            fetchedByMeCount++;
            reportCompletionProgress(missingRevisions.Count(), fetchedByMeCount + fetchedCount, onProgressChange);
         }

         await TaskUtils.RunConcurrentFunctionsAsync(missingRevisions, doFetch,
            () => getFileRevisionBatchLimits(isAwaitedUpdate),
            () =>
            {
               traceDebug("Batch completed");
               fetchedCount = getActualFetchedCount();
               fetchedByMeCount = 0;
               return cancelled;
            });
      }

      async private Task<bool> fetchSingleRevisionAsync(FileRevision revision)
      {
         traceDebug(String.Format("Fetching file {0} with SHA {1}...", revision.GitFilePath.Value, revision.SHA));
         File file = await _repositoryAccessor.LoadFile(_fileStorage.ProjectKey,
            revision.GitFilePath.Value, revision.SHA);
         if (file == null)
         {
            return false;
         }

         string content = StringUtils.Base64Decode(file.Content).Replace("\n", "\r\n");
         _fileStorage.FileCache.WriteFileRevision(revision, content);
         traceDebug(String.Format("Saved file {0} with SHA {1}", revision.GitFilePath.Value, revision.SHA));
         return true;
      }

      private IEnumerable<FileRevision> extractRevisionsFromComparison(ComparisonInternal comparison)
      {
         IEnumerable<FileRevision> baseFiles = FileStorageUtils.CreateFileRevisions(
            comparison.Diffs, comparison.BaseSha, true);
         IEnumerable<FileRevision> headFiles = FileStorageUtils.CreateFileRevisions(
            comparison.Diffs, comparison.HeadSha, false);
         return baseFiles.Concat(headFiles);
      }

      private IEnumerable<FileRevision> selectMissingFileRevisions(IEnumerable<FileRevision> revisions)
      {
         return revisions.Where(x => isMissingRevision(x));
      }

      private bool isMissingRevision(FileRevision fileRevision)
      {
         return !_fileStorage.FileCache.ContainsFileRevision(fileRevision);
      }

      private void reportProgress(Action<string> onProgressChange, string message)
      {
         onProgressChange?.Invoke(message);
         if (onProgressChange != null)
         {
            traceDebug(String.Format("Reported to user: \"{0}\"", message));
         }
      }

      private void traceDebug(string message)
      {
#if DEBUG
         Trace.TraceInformation(String.Format("[DEBUG] [FileStorageUpdater] ({0}) {1}",
            _fileStorage.ProjectKey.ProjectName, message));
#endif
      }

      private void traceInformation(string message)
      {
         Trace.TraceInformation(String.Format("[FileStorageUpdater] ({0}) {1}",
            _fileStorage.ProjectKey.ProjectName, message));
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
            string message = String.Format("File download progress: {0}%", percentage);
            reportProgress(onProgressChange, message);
         }
      }

      private TaskUtils.BatchLimits getComparisonBatchLimits(bool isAwaitedUpdate)
      {
         if (isAwaitedUpdate)
         {
            return Constants.ComparisonLoadingForAwaitedUpdateBatchLimits;
         }

         return new TaskUtils.BatchLimits
         {
            Size = Constants.ComparisonLoadingForNonAwaitedUpdateBatchLimits.Size,
            Delay = Constants.ComparisonLoadingForNonAwaitedUpdateBatchLimits.Delay * _getStorageCount()
         };
      }

      private TaskUtils.BatchLimits getFileRevisionBatchLimits(bool isAwaitedUpdate)
      {
         if (isAwaitedUpdate)
         {
            return Constants.FileRevisionLoadingForAwaitedUpdateBatchLimits;
         }

         return new TaskUtils.BatchLimits
         {
            Size = Constants.FileRevisionLoadingForNonAwaitedUpdateBatchLimits.Size,
            Delay = Constants.FileRevisionLoadingForNonAwaitedUpdateBatchLimits.Delay * _getStorageCount()
         };
      }

      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly IFileStorage _fileStorage;
      private readonly IRepositoryAccessor _repositoryAccessor;
      private readonly Func<int> _getStorageCount;

      private bool _isDisposed;

      /// <summary>
      /// Number of awaited requests in all storages (it is static!)
      /// </summary>
      private static int _activeAwaitedUpdateRequestCount;
   }
}

