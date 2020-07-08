using System;
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
         Action<string> onProgressChange, Action _)
      {
         if (onProgressChange == null)
         {
            return;
         }

         try
         {
            await doUpdate(contextProvider, onProgressChange);
         }
         catch (RepositoryAccessorException ex)
         {
            throw new LocalCommitStorageUpdaterFailedException("Cannot update file storage", ex);
         }
         finally
         {
            reportProgress(onProgressChange, String.Empty);
         }
      }

      public void RequestUpdate(ICommitStorageUpdateContextProvider contextProvider, Action onFinished)
      {
         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
            {
               try
               {
                  await doUpdate(contextProvider, null);
                  onFinished?.Invoke();
               }
               catch (RepositoryAccessorException ex)
               {
                  ExceptionHandlers.Handle("Silent update failed", ex);
               }
            }), null);
      }

      async public Task doUpdate(ICommitStorageUpdateContextProvider contextProvider, Action<string> onProgressChange)
      {
         CommitStorageUpdateContext context = contextProvider?.GetContext();
         if (contextProvider == null || context == null || context.BaseToHeads == null || _isDisposed)
         {
            return;
         }

         reportProgress(onProgressChange, "Downloading meta-information...");
         IEnumerable<ComparisonInternal> comparisons = await fetchComparisonsAsync(context, onProgressChange);
         traceInformation(String.Format("Got {0} comparisons, onProgressChange is {1} null",
            comparisons.Count(), onProgressChange == null ? "" : "not"));

         reportProgress(onProgressChange, "Meta-information downloaded. Starting to download commit contents...");

         traceInformation("List of comparisons to process:");
         foreach (ComparisonInternal comparison in comparisons)
         {
            traceInformation(String.Format("{0} vs {1} ({2} files)",
               comparison.BaseSha, comparison.HeadSha, comparison.Diffs.Count()));
         }

         await processComparisonsAsync(context, onProgressChange, comparisons);
         reportProgress(onProgressChange, "Commit contents downloaded");
      }

      async private Task<IEnumerable<ComparisonInternal>> fetchComparisonsAsync(
         CommitStorageUpdateContext context, Action<string> onProgressChange)
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

            Comparison comparison = await fetchSingleComparisonAsync(baseShaToHeadSha.Item1, baseShaToHeadSha.Item2);
            if (comparison == null || _isDisposed)
            {
               cancelled = true;
               return;
            }
            comparisons.Add(new ComparisonInternal(comparison.Diffs, baseShaToHeadSha.Item1, baseShaToHeadSha.Item2));
         }

         bool needTraceProgress = onProgressChange != null;
         await TaskUtils.RunConcurrentFunctionsAsync(baseToHeads, doFetch,
            needTraceProgress ? Constants.MaxComparisonInBatch : Constants.MaxComparisonInBatchBackground,
            needTraceProgress ? Constants.ComparisionInterBatchDelay : Constants.ComparisionInterBatchDelayBackground,
            () => cancelled);
         return comparisons;
      }

      async private Task<Comparison> fetchSingleComparisonAsync(string baseSha, string headSha)
      {
         Comparison comparison = _fileStorage.ComparisonCache.LoadComparison(baseSha, headSha);
         if (comparison != null)
         {
            return comparison;
         }

         comparison = await _repositoryAccessor.Compare(_fileStorage.ProjectKey, baseSha, headSha);
         if (comparison == null)
         {
            return null;
         }

         _fileStorage.ComparisonCache.SaveComparison(baseSha, headSha, comparison);
         return comparison;
      }

      private async Task processComparisonsAsync(CommitStorageUpdateContext context,
         Action<string> onProgressChange, IEnumerable<ComparisonInternal> comparisons)
      {
         List<FileRevision> allRevisions = new List<FileRevision>();
         allRevisions.AddRange(comparisons.SelectMany(x => extractRevisionsFromComparison(x)));

         IEnumerable<FileRevision> getMissingRevisions() => getMissingFileRevisions(allRevisions);

         bool needTraceProgress = onProgressChange != null;
         int initialTotalCount = needTraceProgress ? getMissingRevisions().Count() : 0;
         int calculateFetchedCount() => needTraceProgress ? initialTotalCount - getMissingRevisions().Count() : 0;

         IEnumerable<FileRevision> missingRevisions = getMissingRevisions().ToArray();
         traceInformation(String.Format("Total: {0}, Missing: {1}, onProgressChange is {2} null",
            allRevisions.Count(), missingRevisions.Count(), onProgressChange == null ? "" : "not"));
         if (!missingRevisions.Any())
         {
            return;
         }

         await fetchRevisionsAsync(missingRevisions, onProgressChange, initialTotalCount, () => calculateFetchedCount());

         IEnumerable<FileRevision> getRemainingMissingRevisions() => missingRevisions.Intersect(_currentDownloads);
         traceInformation(String.Format("Waiting for remaining {0} revisions", getRemainingMissingRevisions().Count()));
         await TaskUtils.WhileAsync(
            () =>
         {
            if (_isDisposed || !getRemainingMissingRevisions().Any())
            {
               return false;
            }

            if (needTraceProgress)
            {
               int actualFetchedCount = calculateFetchedCount();
               reportCompletionProgress(initialTotalCount, actualFetchedCount, onProgressChange);
            }
            return true;
         });
      }

      async private Task fetchRevisionsAsync(IEnumerable<FileRevision> revisions,
         Action<string> onProgressChange, int totalExpectedCount, Func<int> getActualFetchedCount)
      {
         bool needTraceProgress = onProgressChange != null;
         bool cancelled = _isDisposed;

         int fetchedByMeCount = 0; // this counter allows to not call getActualFetchedCount() on each iteration
         int fetchedCount = getActualFetchedCount();
         async Task doFetch(FileRevision revision)
         {
            if (cancelled || !isMissingRevision(revision) || !_currentDownloads.Add(revision))
            {
               return;
            }

            try
            {
               if (!await fetchSingleRevisionAsync(revision) || _isDisposed)
               {
                  cancelled = true;
                  return;
               }

               if (needTraceProgress)
               {
                  fetchedByMeCount++;
                  reportCompletionProgress(totalExpectedCount, fetchedByMeCount + fetchedCount, onProgressChange);
               }
            }
            finally
            {
               _currentDownloads.Remove(revision);
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(revisions, doFetch,
            needTraceProgress ? Constants.MaxFilesInBatch : Constants.MaxFilesInBatchBackground,
            needTraceProgress ? Constants.FilesInterBatchDelay : Constants.FilesInterBatchDelayBackground,
            () =>
            {
               if (needTraceProgress)
               {
                  fetchedCount = getActualFetchedCount();
                  fetchedByMeCount = 0;
               }
               return cancelled;
            });
      }

      async private Task<bool> fetchSingleRevisionAsync(FileRevision revision)
      {
         File file = await _repositoryAccessor.LoadFile(_fileStorage.ProjectKey, revision.GitFilePath.Value, revision.SHA);
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

      private IEnumerable<FileRevision> getMissingFileRevisions(IEnumerable<FileRevision> revisions)
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
            traceInformation(String.Format("Reported to user: \"{0}\"", message));
         }
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
      private readonly IFileStorage _fileStorage;
      private readonly IRepositoryAccessor _repositoryAccessor;
      private readonly Action _onCloned;
      private readonly Action<FileRevision> _onFetched;
      private readonly HashSet<FileRevision> _currentDownloads = new HashSet<FileRevision>();

      private bool _isDisposed;
   }
}

