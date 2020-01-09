using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Types;
using mrHelper.Client.MergeRequests;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Provides access to git repository.
   /// All methods throw GitOperationException if corresponding git command exited with a not-zero code.
   /// </summary>
   public class GitClient : IGitRepository
   {
      // Host Name and Project Name
      internal ProjectKey ProjectKey { get; }

      // Path of this git repository
      public string Path { get; }

      // Object which keeps this git repository up-to-date
      public GitClientUpdater Updater { get; }

      public event Action<GitClient, DateTime> Updated;
      public event Action<GitClient> Disposed;

      public string HostName { get { return ProjectKey.HostName; } }
      public string ProjectName { get { return ProjectKey.ProjectName; } }

      private static readonly int cancellationExitCode = 130;

      /// <summary>
      /// Construct GitClient with a path that either does not exist or it is empty or points to a valid git repository
      /// Throws ArgumentException if requirements on `path` argument are not met
      /// </summary>
      internal GitClient(ProjectKey projectKey, string path, IProjectWatcher projectWatcher,
         ISynchronizeInvoke synchronizeInvoke)
      {
         if (!canClone(path) && !isValidRepository(path))
         {
            throw new ArgumentException("Path \"" + path + "\" already exists but it is not a valid git repository");
         }

         _synchronizeInvoke = synchronizeInvoke;
         ProjectKey = projectKey;
         Path = path;
         Updater = new GitClientUpdater(projectWatcher,
            async (reportProgress, latestChange) =>
         {
            if (_updateOperationDescriptor != null)
            {
               await pickupGitCommandAsync(reportProgress);
               return;
            }

            string arguments = canClone(Path) ?
               "clone --progress " +
               ProjectKey.HostName + "/" + ProjectKey.ProjectName + " " +
               StringUtils.EscapeSpaces(Path) : "fetch --progress";
            await executeGitCommandAsync(arguments, reportProgress, canClone(Path) ? String.Empty : Path);

            Updated?.Invoke(this, latestChange);
         },
         projectKeyToCheck => ProjectKey.Equals(projectKeyToCheck),
         _synchronizeInvoke,
         () => cancelUpdateOperationAsync());

         Trace.TraceInformation(String.Format("[GitClient] Created GitClient at path {0} for host {1} and project {2}",
            path, ProjectKey.HostName, ProjectKey.ProjectName));
      }

      async public Task DisposeAsync()
      {
         Trace.TraceInformation(String.Format("[GitClient] Disposing GitClient at path {0}", Path));
         _isDisposed = true;
         await Task.WhenAll(new List<Task>{ cancelUpdateOperationAsync(), cancelRepositoryOperationsAsync() });
         Updater.Dispose();
         Disposed?.Invoke(this);
      }

      /// <summary>
      /// Check if this repository needs cloning before use
      /// </summary>
      public bool DoesRequireClone()
      {
         Debug.Assert(canClone(Path) || isValidRepository(Path));
         return !isValidRepository(Path);
      }

      public IEnumerable<string> Diff(GitDiffArguments arguments)
      {
         return executeCachedOperation(arguments, _cachedDiffs);
      }

      public Task<IEnumerable<string>> DiffAsync(GitDiffArguments arguments)
      {
         return executeCachedAsyncOperation(arguments, _cachedDiffs);
      }

      public IEnumerable<string> GetListOfRenames(GitListOfRenamesArguments arguments)
      {
         return executeCachedOperation(arguments, _cachedListOfRenames);
      }

      public Task<IEnumerable<string>> GetListOfRenamesAsync(GitListOfRenamesArguments arguments)
      {
         return executeCachedAsyncOperation(arguments, _cachedListOfRenames);
      }

      public IEnumerable<string> ShowFileByRevision(GitRevisionArguments arguments)
      {
         return executeCachedOperation(arguments, _cachedRevisions);
      }

      public Task<IEnumerable<string>> ShowFileByRevisionAsync(GitRevisionArguments arguments)
      {
         return executeCachedAsyncOperation(arguments, _cachedRevisions);
      }

      /// <summary>
      /// Check if Clone can be called for this GitClient
      /// </summary>
      static private bool canClone(string path)
      {
         return !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any();
      }

      static private bool isValidRepository(string path)
      {
         if (!Directory.Exists(path))
         {
            return false;
         }

         try
         {
            return ExternalProcess.Start("git", "rev-parse --is-inside-work-tree", true, path).StdErr.Count() == 0;
         }
         catch (ExternalProcessException)
         {
            return false;
         }
      }

      public IEnumerable<string> executeCachedOperation<T>(
         T arguments, Dictionary<T, IEnumerable<string>> cache)
      {
         if (cache.ContainsKey(arguments))
         {
            return cache[arguments];
         }

         ExternalProcess.Output gitOutput = ExternalProcess.Start("git", arguments.ToString(), true, Path);
         cache[arguments] = gitOutput.StdOut;
         return gitOutput.StdOut;
      }

      async public Task<IEnumerable<string>> executeCachedAsyncOperation<T>(
         T arguments, Dictionary<T, IEnumerable<string>> cache)
      {
         if (cache.ContainsKey(arguments))
         {
            return cache[arguments];
         }

         cache[arguments] = await executeLiteGitCommandAsync(arguments.ToString(), Path);
         return cache[arguments];
      }

      async private Task<IEnumerable<string>> executeLiteGitCommandAsync(string arguments, string path)
      {
         if (_isDisposed)
         {
            throw new GitClientDisposedException(String.Format("GitClient {0} disposed", ProjectKey.ProjectName));
         }

         List<string> stdOut = new List<string>();
         List<string> stdErr = new List<string>();
         ExternalProcess.AsyncTaskDescriptor descriptor = ExternalProcess.StartAsync(
            "git", arguments, null, _synchronizeInvoke, path, stdOut, stdErr);

         _repositoryOperationDescriptor.Add(descriptor);

         try
         {
            await descriptor.Task;
            checkForSpecificError(arguments, stdErr);
            return stdOut;
         }
         catch (ExternalProcessException ex)
         {
            throw convertToGitOperationException(ex, arguments);
         }
         finally
         {
            descriptor.Process.Dispose();
            _repositoryOperationDescriptor.Remove(descriptor);
         }
      }

      async private Task<IEnumerable<string>> executeGitCommandAsync(
         string arguments, Action<string> onProgressChange, string path)
      {
         if (_isDisposed)
         {
            throw new GitClientDisposedException(String.Format("GitClient {0} disposed", ProjectKey.ProjectName));
         }

         Debug.Assert(_updateOperationDescriptor == null);

         _onProgressChange = onProgressChange;

         Progress<string> progress = new Progress<string>();
         progress.ProgressChanged += (sender, status) =>
         {
            _onProgressChange?.Invoke(status);
         };

         traceOperationStatus(arguments, "start");

         List<string> stdOut = new List<string>();
         List<string> stdErr = new List<string>();
         _updateOperationDescriptor = ExternalProcess.StartAsync(
            "git", arguments, progress, _synchronizeInvoke, path, stdOut, stdErr);

         try
         {
            await _updateOperationDescriptor.Task;
            traceOperationStatus(arguments, "end");
            checkForSpecificError(arguments, stdErr);
            return stdOut;
         }
         catch (ExternalProcessException ex)
         {
            throw convertToGitOperationException(ex, arguments);
         }
         finally
         {
            _updateOperationDescriptor.Process.Dispose();
            _updateOperationDescriptor = null;
         }
      }

      async private Task pickupGitCommandAsync(Action<string> onProgressChange)
      {
         Debug.Assert(_updateOperationDescriptor != null);

         traceOperationStatus("pick-up", "start");

         _onProgressChange = onProgressChange;

         try
         {
            await _updateOperationDescriptor.Task;
            traceOperationStatus("pick-up", "end");
         }
         catch (ExternalProcessException ex)
         {
            throw convertToGitOperationException(ex, "pick-up");
         }
      }

      static private void checkForSpecificError(string arguments, IEnumerable<string> errors)
      {
         if (errors.Count() > 0 && errors.First().StartsWith("fatal:"))
         {
            // TODO This is specific to git and not to any External Process
            string reasons =
               "Possible reasons:\n"
               + "-Git repository is not up-to-date\n"
               + "-Given commit is no longer in the repository (force push?)";
            string message = String.Format("git returned \"{0}\". {1}", errors.First(), reasons);
            throw new GitObjectException(message, 0);
         }
      }

      private GitOperationException convertToGitOperationException(ExternalProcessException ex, string operation)
      {
         Debug.Assert(ex.ExitCode != 0);

         string status = ex.ExitCode == cancellationExitCode ? "cancel" : "error";
         traceOperationStatus(operation, status);
         ExceptionHandlers.Handle(ex, "Git operation failed");

         GitOperationException gitEx = new GitOperationException(ex.Command, ex.ExitCode, ex.Errors);
         gitEx.Cancelled = ex.ExitCode == cancellationExitCode;
         return gitEx;
      }

      private void traceOperationStatus(string operation, string status)
      {
         Trace.TraceInformation(String.Format("[GitClient] async operation -- {0} -- {1} for {2}",
            status, operation, ProjectKey.ProjectName));
      }

      async private Task cancelUpdateOperationAsync()
      {
         cancelOperation(_updateOperationDescriptor);
         while (_updateOperationDescriptor != null)
         {
            await Task.Delay(50);
         }
      }

      async private Task cancelRepositoryOperationsAsync()
      {
         _repositoryOperationDescriptor.ForEach(x => cancelOperation(x));
         while (_repositoryOperationDescriptor.Count > 0)
         {
            await Task.Delay(50);
         }
      }

      private void cancelOperation(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         if (descriptor == null)
         {
            return;
         }

         Process p = descriptor.Process;
         descriptor.Cancelled = true;
         try
         {
            ExternalProcess.Cancel(descriptor.Process);
         }
         catch (InvalidOperationException)
         {
            // process already exited
         }
      }

      private readonly Dictionary<GitDiffArguments, IEnumerable<string>> _cachedDiffs =
         new Dictionary<GitDiffArguments, IEnumerable<string>>();

      private readonly Dictionary<GitRevisionArguments, IEnumerable<string>> _cachedRevisions =
         new Dictionary<GitRevisionArguments, IEnumerable<string>>();

      private readonly Dictionary<GitListOfRenamesArguments, IEnumerable<string>> _cachedListOfRenames =
         new Dictionary<GitListOfRenamesArguments, IEnumerable<string>>();

      private bool _isDisposed = false;
      private ExternalProcess.AsyncTaskDescriptor _updateOperationDescriptor;
      private readonly List<ExternalProcess.AsyncTaskDescriptor> _repositoryOperationDescriptor =
         new List<ExternalProcess.AsyncTaskDescriptor>();

      private Action<string> _onProgressChange;
      private ISynchronizeInvoke _synchronizeInvoke;
   }
}

