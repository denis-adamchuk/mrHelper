using System;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using GitLabSharp.Entities;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.GitLabClient;

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
         IFileStorageProperties properties)
      {
         _synchronizeInvoke = synchronizeInvoke;
         _fileStorage = fileStorage;
         _repositoryAccessor = repositoryAccessor;
         _properties = properties;
      }

      public void StopUpdate() { }
      public bool CanBeStopped()
      {
         // TODO It is a nice extra feature to allow stop downloading
         return false;
      }

      public void Dispose()
      {
         _repositoryAccessor?.Dispose();
         _repositoryAccessor = null;

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
                  traceInformation(String.Format("RequestUpdate() called with context of type {0}",
                     contextProvider?.GetContext()?.GetType().ToString() ?? "null"));

                  await doUpdate(false, contextProvider?.GetContext(), null);
                  onFinished?.Invoke();
               }
               catch (RepositoryAccessorException ex)
               {
                  ExceptionHandlers.Handle("Silent update failed", ex);
               }
               catch (LocalCommitStorageUpdaterLimitException ex)
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
         if (context == null || context.BaseToHeads.Data == null || !context.BaseToHeads.Data.Any() || _isDisposed)
         {
            return;
         }

         reportProgress(onProgressChange, "Downloading meta-information...");
         IEnumerable<FileInternal> allFiles = await fetchComparisonsAsync(isAwaitedUpdate, context);
         if (allFiles == null)
         {
            return;
         }

         reportProgress(onProgressChange, "Starting to download files from GitLab...");
         await processComparisonsAsync(isAwaitedUpdate, onProgressChange, allFiles);
         reportProgress(onProgressChange, "Files downloaded");
      }

      async private Task<IEnumerable<FileInternal>> fetchComparisonsAsync(bool isAwaitedUpdate,
         CommitStorageUpdateContext context)
      {
         bool cancelled = _isDisposed;
         Exception exception = null;
         List<FileInternal> allFiles = new List<FileInternal>();
         List<ComparisonInternal> comparisons = new List<ComparisonInternal>();
         async Task doFetch(BaseToHeadsCollection.FlatBaseToHeadInfo baseToHeadInfo)
         {
            if (cancelled)
            {
               return;
            }

            await suspendProcessingOfNonAwaitedUpdate(isAwaitedUpdate);
            Task<Comparison> fetchTask = fetchSingleComparison(baseToHeadInfo.Base.Sha, baseToHeadInfo.Head.Sha);
            if (fetchTask == null || _isDisposed)
            {
               cancelled = true;
               return;
            }

            Comparison comparison = await fetchTask;
            if (comparison == null || _isDisposed)
            {
               cancelled = true;
               return;
            }

            try
            {
               throwOnBadComparison(comparison);
            }
            catch (LocalCommitStorageUpdaterLimitException ex)
            {
               if (baseToHeadInfo.Files?.Any() ?? false)
               {
                  ExceptionHandlers.Handle("Bad Comparison object", ex);
                  traceInformation(String.Format(
                     "[FileStorageUpdater] Applying manual file comparison for {0} files", baseToHeadInfo.Files.Count()));
                  foreach (BaseToHeadsCollection.RelativeFileInfo fileInfo in baseToHeadInfo.Files)
                  {
                     allFiles.Add(new FileInternal(fileInfo.OldPath, baseToHeadInfo.Base.Sha));
                     allFiles.Add(new FileInternal(fileInfo.NewPath, baseToHeadInfo.Head.Sha));
                  }
                  return;
               }

               exception = ex;
               cancelled = true;
               return;
            }

            IEnumerable<DiffStruct> filteredDiffs = filterDiffs(isAwaitedUpdate, comparison.Diffs,
               baseToHeadInfo.Base.Sha, baseToHeadInfo.Head.Sha, baseToHeadInfo.Files);
            if (filteredDiffs != null && filteredDiffs.Any())
            {
               comparisons.Add(new ComparisonInternal(filteredDiffs, baseToHeadInfo.Base.Sha, baseToHeadInfo.Head.Sha));
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(context.BaseToHeads.Flatten(), doFetch,
            () => getComparisonBatchLimits(isAwaitedUpdate), () => cancelled);
         if (exception != null)
         {
            throw exception;
         }

         if (cancelled)
         {
            return null;
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

         allFiles.AddRange(comparisons.SelectMany(x => extractFilesFromComparison(x)));
         return allFiles;
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

      /// <summary>
      /// Removes unwanted files from a Diffs collection if needed and suppresses too big comparisons in non-awaited mode
      /// </summary>
      private IEnumerable<DiffStruct> filterDiffs(bool isAwaitedUpdate,
         IEnumerable<DiffStruct> diffs, string baseSha, string headSha,
         IEnumerable<BaseToHeadsCollection.RelativeFileInfo> filter)
      {
         bool doesDiffMatchFileInfo(DiffStruct diff, BaseToHeadsCollection.RelativeFileInfo fileInfo)
         {
            bool doesNewPathMatch = diff.New_Path == fileInfo.NewPath;
            bool doesOldPathMatch = diff.Old_Path == fileInfo.OldPath;

            // When file is renamed, Comparison has different old_path and new_path values but
            // Discussion Position has the same old_path and new_path values.
            return diff.Renamed_File ? doesNewPathMatch : doesNewPathMatch && doesOldPathMatch;
         }

         IEnumerable<DiffStruct> filteredDiffs = diffs
            .Where(diff => filter?.Any(fileInfo => doesDiffMatchFileInfo(diff, fileInfo)) ?? true)
            .ToArray();
         if (!isAwaitedUpdate && filteredDiffs.Count() > Constants.MaxAllowedDiffsInBackgroundComparison)
         {
            traceInformation(String.Format("Comparison between {0} and {1} contains too many files ({2}) and skipped",
               baseSha, headSha, filteredDiffs.Count()));
            return null;
         }
         return filteredDiffs;
      }

      private static async Task suspendProcessingOfNonAwaitedUpdate(bool isAwaitedUpdate)
      {
         // suspend all background work while processing `awaited' requests
         await TaskUtils.WhileAsync(() => !isAwaitedUpdate && _activeAwaitedUpdateRequestCount > 0);
      }

      private Task<Comparison> fetchSingleComparison(string baseSha, string headSha)
      {
         return _repositoryAccessor?.Compare(baseSha, headSha, _fileStorage.ComparisonCache);
      }

      private async Task processComparisonsAsync(bool isAwaitedUpdate,
         Action<string> onProgressChange, IEnumerable<FileInternal> allFiles)
      {
         FileInternal[] initialMissingFiles = selectMissingFiles(allFiles).ToArray();
         int initialMissingCount = initialMissingFiles.Length;
         if (initialMissingCount == 0)
         {
            return;
         }

         if (isAwaitedUpdate && initialMissingCount >= Constants.MinDiffsInComparisonToNotifyUser)
         {
            string message = String.Format(
               "This operation requires downloading {0} files and may take a few minutes. " +
               "Do you really want to continue?", initialMissingCount);
            if (MessageBox.Show(message, "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
               == DialogResult.No)
            {
               throw new LocalCommitStorageUpdaterLimitException("Too many files in diff");
            }
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
            () => getFileBatchLimits(isAwaitedUpdate),
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
         if (_repositoryAccessor == null)
         {
            return false;
         }

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
         return isAwaitedUpdate
            ? _properties.GetComparisonBatchLimitsForAwaitedUpdate()
            : _properties.GetComparisonBatchLimitsForNonAwaitedUpdate();
      }

      private TaskUtils.BatchLimits getFileBatchLimits(bool isAwaitedUpdate)
      {
         return isAwaitedUpdate
            ? _properties.GetFileBatchLimitsForAwaitedUpdate()
            : _properties.GetFileBatchLimitsForNonAwaitedUpdate();
      }

      private readonly ISynchronizeInvoke _synchronizeInvoke;
      private readonly IFileStorage _fileStorage;
      private RepositoryAccessor _repositoryAccessor;
      private readonly IFileStorageProperties _properties;

      private bool _isDisposed;

      /// <summary>
      /// Number of awaited requests in all storages (it is static!)
      /// </summary>
      private static int _activeAwaitedUpdateRequestCount;
   }
}

