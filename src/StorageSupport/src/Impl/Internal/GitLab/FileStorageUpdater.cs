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
using mrHelper.GitLabClient;
using static mrHelper.StorageSupport.HeadInfo;

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

   internal class FileInternal
   {
      // public constructor allows to use Activator.CreateInstance()
      public FileInternal(string path, string sha)
      {
         Path = path;
         SHA = sha;
      }

      internal string Path { get; }
      internal string SHA { get; }
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
         RepositoryAccessor repositoryAccessor,
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
               catch (Exception ex)
               {
                  Debug.Assert(ex is RepositoryAccessorException || ex is LocalCommitStorageUpdaterLimitException);
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
         if (context == null || context.BaseToHeads.Data == null || !context.BaseToHeads.Data.Any() || _isDisposed)
         {
            return;
         }

         reportProgress(onProgressChange, "Downloading meta-information...");
         IEnumerable<ComparisonInternal> comparisons = await fetchComparisonsAsync(isAwaitedUpdate, context);
         if (comparisons == null)
         {
            return;
         }

         Action<string> traceFunction = traceDebug;
         if (isAwaitedUpdate)
         {
            traceFunction = traceInformation;
         }

         traceFunction(String.Format("Got {0} comparisons, isAwaitedUpdate={1}",
            comparisons.Count(), isAwaitedUpdate.ToString()));
         foreach (ComparisonInternal comparison in comparisons)
         {
            traceFunction(String.Format("{0} vs {1} ({2} files)",
               comparison.BaseSha, comparison.HeadSha, comparison.Diffs.Count()));
         }

         reportProgress(onProgressChange, "Starting to download files from GitLab...");
         await processComparisonsAsync(isAwaitedUpdate, onProgressChange, comparisons);
         reportProgress(onProgressChange, "Files downloaded");
      }

      async private Task<IEnumerable<ComparisonInternal>> fetchComparisonsAsync(bool isAwaitedUpdate,
         CommitStorageUpdateContext context)
      {
         List<Tuple<string, string, IEnumerable<FileInfo>>> baseToHeads = context.BaseToHeads.Data
            .SelectMany(
               (x) => x.Value,
               (kv, head) => new Tuple<string, string, IEnumerable<FileInfo>>(kv.Key.Sha, head.Sha, head.Files))
            .ToList();

         bool cancelled = _isDisposed;
         Exception exception = null;
         List<ComparisonInternal> comparisons = new List<ComparisonInternal>();
         async Task doFetch(Tuple<string, string, IEnumerable<FileInfo>> baseShaToHeadSha)
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

            try
            {
               throwOnBadComparison(comparison);
            }
            catch (Exception ex)
            {
               exception = ex;
               cancelled = true;
               return;
            }

            IEnumerable<DiffStruct> filteredDiffs = filterDiffs(comparison.Diffs, baseShaToHeadSha.Item3);
            if (filteredDiffs.Any())
            {
               comparisons.Add(new ComparisonInternal(filteredDiffs, baseShaToHeadSha.Item1, baseShaToHeadSha.Item2));
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(baseToHeads, doFetch,
            () => getComparisonBatchLimits(isAwaitedUpdate), () => cancelled);
         if (exception != null)
         {
            throw exception;
         }
         return cancelled ? null : comparisons;
      }

      private static void throwOnBadComparison(Comparison comparison)
      {
         if (comparison.Diffs.Count() > Constants.MaxAllowedDiffsInComparison)
         {
            throw new LocalCommitStorageUpdaterLimitException("Too many files in diff");
         }

         if (comparison.Compare_Timeout)
         {
            throw new LocalCommitStorageUpdaterLimitException("GitLab failed to compare selected revisions");
         }
      }

      private static IEnumerable<DiffStruct> filterDiffs(IEnumerable<DiffStruct> diffs, IEnumerable<FileInfo> filter)
      {
         bool doesDiffMatchFileInfo(DiffStruct diff, FileInfo fileInfo)
         {
            bool doesNewPathMatch = diff.New_Path == fileInfo.NewPath;
            bool doesOldPathMatch = diff.Old_Path == fileInfo.OldPath;

            // When file is renamed, Comparison has different old_path and new_path values but
            // Discussion Position has the same old_path and new_path values.
            return diff.Renamed_File ? doesNewPathMatch : doesNewPathMatch && doesOldPathMatch;
         }
         return diffs.Where(diff => filter?.Any(fileInfo => doesDiffMatchFileInfo(diff, fileInfo)) ?? true);
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
         comparison = await _repositoryAccessor.Compare(baseSha, headSha);
         if (comparison == null)
         {
            return null;
         }

         _fileStorage.ComparisonCache.SaveComparison(baseSha, headSha, comparison);
         traceDebug(String.Format("Saved comparison {0} vs {1}", baseSha, headSha));
         return comparison;
      }

      private async Task processComparisonsAsync(bool isAwaitedUpdate,
         Action<string> onProgressChange, IEnumerable<ComparisonInternal> comparisons)
      {
         List<FileInternal> allFiles = new List<FileInternal>();
         allFiles.AddRange(comparisons.SelectMany(x => extractFilesFromComparison(x)));

         FileInternal[] initialMissingFiles = selectMissingFiles(allFiles).ToArray();
         int initialMissingCount = initialMissingFiles.Length;
         if (initialMissingCount == 0)
         {
            return;
         }

         traceInformation(String.Format("Downloading files. Total: {0}, Missing: {1}, isAwaitedUpdate={2}",
            allFiles.Count(), initialMissingCount, isAwaitedUpdate.ToString()));
         bool needTraceProgress = onProgressChange != null;
         await fetchFilesAsync(isAwaitedUpdate, initialMissingFiles, onProgressChange,
            () => needTraceProgress ? initialMissingCount - selectMissingFiles(allFiles).Count() : 0);
      }

      async private Task fetchFilesAsync(bool isAwaitedUpdate, IEnumerable<FileInternal> missingFiles,
         Action<string> onProgressChange, Func<int> getActualFetchedCount)
      {
         bool cancelled = _isDisposed;

         int fetchedByMeCount = 0; // this counter allows to not call getActualFetchedCount() on each iteration
         int fetchedCount = getActualFetchedCount();
         async Task doFetch(FileInternal file)
         {
            if (cancelled || !isMissingFile(file))
            {
               traceDebug(String.Format("Skipped file {0} with SHA {1}", file.Path, file.SHA));
               return;
            }

            await suspendProcessingOfNonAwaitedUpdate(isAwaitedUpdate);
            if (!await fetchSingleFileAsync(file) || _isDisposed)
            {
               cancelled = true;
               return;
            }

            fetchedByMeCount++;
            reportCompletionProgress(missingFiles.Count(), fetchedByMeCount + fetchedCount, onProgressChange);
         }

         await TaskUtils.RunConcurrentFunctionsAsync(missingFiles, doFetch,
            () => getFileFetchBatchLimits(isAwaitedUpdate),
            () =>
            {
               traceDebug("Batch completed");
               fetchedCount = getActualFetchedCount();
               fetchedByMeCount = 0;
               return cancelled;
            });
      }

      async private Task<bool> fetchSingleFileAsync(FileInternal file)
      {
         traceDebug(String.Format("Fetching file {0} with SHA {1}...", file.Path, file.SHA));
         File gitlabFile = await _repositoryAccessor.LoadFile(file.Path, file.SHA);
         if (gitlabFile == null)
         {
            return false;
         }

         byte[] content = System.Convert.FromBase64String(gitlabFile.Content);
         try
         {
            _fileStorage.FileCache.WriteFileRevision(file.Path, file.SHA, content);
            traceDebug(String.Format("Saved file {0} with SHA {1}", file.Path, file.SHA));
         }
         catch (FileStorageRevisionCacheException ex)
         {
            ExceptionHandlers.Handle(String.Format("Cannot save a file {0} with SHA {1}", file.Path, file.SHA), ex);
         }
         return true;
      }

      private IEnumerable<FileInternal> extractFilesFromComparison(ComparisonInternal comparison)
      {
         IEnumerable<FileInternal> baseFiles = convertDiffToFiles(comparison.Diffs, comparison.BaseSha, true);
         IEnumerable<FileInternal> headFiles = convertDiffToFiles(comparison.Diffs, comparison.HeadSha, false);
         return baseFiles.Concat(headFiles);
      }

      private IEnumerable<FileInternal> convertDiffToFiles(IEnumerable<DiffStruct> diffs, string sha, bool old)
      {
         return FileStorageUtils.TransformDiffs<FileInternal>(diffs, sha, old);
      }

      private IEnumerable<FileInternal> selectMissingFiles(IEnumerable<FileInternal> files)
      {
         return files.Where(x => isMissingFile(x));
      }

      private bool isMissingFile(FileInternal file)
      {
         return !_fileStorage.FileCache.ContainsFileRevision(new FileRevision(file.Path, file.SHA));
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

      private TaskUtils.BatchLimits getFileFetchBatchLimits(bool isAwaitedUpdate)
      {
         if (isAwaitedUpdate)
         {
            return Constants.FileLoadingForAwaitedUpdateBatchLimits;
         }

         return new TaskUtils.BatchLimits
         {
            Size = Constants.FileLoadingForNonAwaitedUpdateBatchLimits.Size,
            Delay = Constants.FileLoadingForNonAwaitedUpdateBatchLimits.Delay * _getStorageCount()
         };
      }

      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly IFileStorage _fileStorage;
      private readonly RepositoryAccessor _repositoryAccessor;
      private readonly Func<int> _getStorageCount;

      private bool _isDisposed;

      /// <summary>
      /// Number of awaited requests in all storages (it is static!)
      /// </summary>
      private static int _activeAwaitedUpdateRequestCount;
   }
}

