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
   public class GitClient : IGitRepository, IDisposable
   {
      // Host Name and Project Name
      internal ProjectKey ProjectKey { get; }

      // Path of this git repository
      public string Path { get; }

      // Object which keeps this git repository up-to-date
      public GitClientUpdater Updater { get; }

      public event Action<IGitRepository, DateTime> Updated;
      public event Action<IGitRepository> Disposed;

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

         ProjectKey = projectKey;
         Path = path;
         Updater = new GitClientUpdater(projectWatcher,
            async (reportProgress, latestChange) =>
         {
            if (_descriptor != null)
            {
               await pickupGitCommandAsync(reportProgress);
               return;
            }

            if (canClone(Path))
            {
               string arguments =
                  "clone --progress " + ProjectKey.HostName + "/" + ProjectKey.ProjectName + " " + escapeSpaces(Path);
               await executeGitCommandAsync(arguments, reportProgress);
            }
            else
            {
               await (Task)changeCurrentDirectoryAndRun(() =>
               {
                  string arguments = "fetch --progress";
                  return executeGitCommandAsync(arguments, reportProgress);
               }, Path);
            }

            Updated?.Invoke(this, latestChange);
         },
         projectKeyToCheck => ProjectKey.Equals(projectKeyToCheck),
         synchronizeInvoke);

         Trace.TraceInformation(String.Format("[GitClient] Created GitClient at path {0} for host {1} and project {2}",
            path, ProjectKey.HostName, ProjectKey.ProjectName));
      }

      public void Dispose()
      {
         _isDisposed = true;
         Trace.TraceInformation(String.Format("[GitClient] Disposing GitClient at path {0}", Path));
         CancelAsyncOperation();
         while (_liteDescriptors.Count > 0)
         {
            if (!cancelDescriptor(_liteDescriptors[0]))
            {
               _liteDescriptors.RemoveAt(0);
            }
         }
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

      /// <summary>
      /// Launches 'git difftool --dir-diff' command
      /// </summary>
      public int DiffTool(string name, string leftCommit, string rightCommit)
      {
         return (int)changeCurrentDirectoryAndRun(() =>
         {
            string arguments = "difftool --dir-diff --tool=" + name + " " + leftCommit + " " + rightCommit;
            return ExternalProcess.Start("git", arguments, false).PID;
         }, Path);
      }

      /// <summary>
      /// Cancel currently running git async operation
      /// InvalidOperationException if no async operation is running
      /// </summary>
      public void CancelAsyncOperation()
      {
         cancelDescriptor(_descriptor);
      }

      private bool cancelDescriptor(ExternalProcess.AsyncTaskDescriptor descriptor)
      {
         if (descriptor == null)
         {
            return false;
         }

         Process p = descriptor.Process;
         descriptor.Cancelled = true;
         try
         {
            ExternalProcess.Cancel(descriptor.Process);
            return true;
         }
         catch (InvalidOperationException)
         {
            // already exited
            return false;
         }
         finally
         {
            p.Dispose();
         }
      }

      public IEnumerable<string> Diff(GitDiffArguments arguments)
      {
         arguments.filename1 = escapeSpaces(arguments.filename1);
         arguments.filename2 = escapeSpaces(arguments.filename2);

         if (_cachedDiffs.ContainsKey(arguments))
         {
            return _cachedDiffs[arguments];
         }

         IEnumerable<string> result = (IEnumerable<string>)changeCurrentDirectoryAndRun(() =>
         {
            string argString =
               "diff -U" + arguments.context.ToString() + " " + arguments.sha1 + " " + arguments.sha2
               + " -- " + arguments.filename1 + " " + arguments.filename2;
            return ExternalProcess.Start("git", argString).StdOut;
         }, Path);

         _cachedDiffs[arguments] = result;
         return result;
      }

      async public Task<IEnumerable<string>> DiffAsync(GitDiffArguments arguments)
      {
         arguments.filename1 = escapeSpaces(arguments.filename1);
         arguments.filename2 = escapeSpaces(arguments.filename2);

         if (_cachedDiffs.ContainsKey(arguments))
         {
            return _cachedDiffs[arguments];
         }

         ExternalProcess.Output gitOutput = await (Task<ExternalProcess.Output>)(changeCurrentDirectoryAndRun(() =>
         {
            string argString =
               "diff -U" + arguments.context.ToString() + " " + arguments.sha1 + " " + arguments.sha2
               + " -- " + arguments.filename1 + " " + arguments.filename2;
            return executeLiteGitCommandAsync(argString);
         }, Path));

         _cachedDiffs[arguments] = gitOutput.StdOut;
         return gitOutput.StdOut;
      }

      public IEnumerable<string> GetListOfRenames(GitListOfRenamesArguments arguments)
      {
         if (_cachedListOfRenames.ContainsKey(arguments))
         {
            return _cachedListOfRenames[arguments];
         }

         IEnumerable<string> result = (IEnumerable<string>)changeCurrentDirectoryAndRun(() =>
         {
            string argString = "diff " + arguments.sha1 + " " + arguments.sha2 + " --numstat --diff-filter=R";
            return ExternalProcess.Start("git", argString).StdOut;
         }, Path);

         _cachedListOfRenames[arguments] = result;
         return result;
      }

      async public Task<IEnumerable<string>> GetListOfRenamesAsync(GitListOfRenamesArguments arguments)
      {
         if (_cachedListOfRenames.ContainsKey(arguments))
         {
            return _cachedListOfRenames[arguments];
         }

         ExternalProcess.Output gitOutput = await (Task<ExternalProcess.Output>)changeCurrentDirectoryAndRun(() =>
         {
            string argString = "diff " + arguments.sha1 + " " + arguments.sha2 + " --numstat --diff-filter=R";
            return executeLiteGitCommandAsync(argString);
         }, Path);

         _cachedListOfRenames[arguments] = gitOutput.StdOut;
         return gitOutput.StdOut;
      }

      public IEnumerable<string> ShowFileByRevision(GitRevisionArguments arguments)
      {
         arguments.filename = escapeSpaces(arguments.filename);

         if (_cachedRevisions.ContainsKey(arguments))
         {
            return _cachedRevisions[arguments];
         }

         IEnumerable<string> result = (IEnumerable<string>)changeCurrentDirectoryAndRun(() =>
         {
            string argString = "show " + arguments.sha + ":" + arguments.filename;
            return ExternalProcess.Start("git", argString).StdOut;
         }, Path);

         _cachedRevisions[arguments] = result;
         return result;
      }

      async public Task<IEnumerable<string>> ShowFileByRevisionAsync(GitRevisionArguments arguments)
      {
         arguments.filename = escapeSpaces(arguments.filename);

         if (_cachedRevisions.ContainsKey(arguments))
         {
            return _cachedRevisions[arguments];
         }

         ExternalProcess.Output gitOutput = await (Task<ExternalProcess.Output>)changeCurrentDirectoryAndRun(() =>
         {
            string argString = "show " + arguments.sha + ":" + arguments.filename;
            return executeLiteGitCommandAsync(argString);
         }, Path);

         _cachedRevisions[arguments] = gitOutput.StdOut;
         return gitOutput.StdOut;
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

         return (bool)changeCurrentDirectoryAndRun(() =>
         {
            try
            {
               ExternalProcess.Output output = ExternalProcess.Start("git", "rev-parse --is-inside-work-tree");
               return output.StdErr.Count() == 0;
            }
            catch (GitOperationException)
            {
               return false;
            }
         }, path);
      }

      static private object changeCurrentDirectoryAndRun(Func<object> cmd, string path)
      {
         var cwd = Directory.GetCurrentDirectory();
         try
         {
            if (path != null)
            {
               Directory.SetCurrentDirectory(path);
            }
            return cmd();
         }
         finally
         {
            Directory.SetCurrentDirectory(cwd);
         }
      }

      async private Task<ExternalProcess.Output> executeLiteGitCommandAsync(string arguments)
      {
         if (_isDisposed)
         {
            throw new GitRepositoryDisposedException(String.Format("GitClient {0} disposed", ProjectKey.ProjectName));
         }

         ExternalProcess.AsyncTaskDescriptor descriptor = null;
         try
         {
            descriptor = ExternalProcess.StartAsync("git", arguments, null);
            _liteDescriptors.Add(descriptor);
            return await descriptor.TaskCompletionSource.Task;
         }
         catch (GitOperationException ex)
         {
            string status = ex.ExitCode == cancellationExitCode ? "cancel" : "error";
            Trace.TraceInformation(String.Format("[GitClient] async operation -- {2} --  {0}: {1}",
               ProjectKey.ProjectName, arguments, status));
            ExceptionHandlers.Handle(ex, "Git operation failed");
            ex.Cancelled = ex.ExitCode == cancellationExitCode;
            throw;
         }
         finally
         {
            _liteDescriptors.Remove(descriptor);
         }
      }

      async private Task<ExternalProcess.Output> executeGitCommandAsync(string arguments, Action<string> onProgressChange)
      {
         if (_isDisposed)
         {
            throw new GitRepositoryDisposedException(String.Format("GitClient {0} disposed", ProjectKey.ProjectName));
         }

         // If _descriptor is non-empty, it must be a non-exclusive operation, otherwise pickup should have caught it
         Debug.Assert(_descriptor == null);

         _onProgressChange = onProgressChange;

         Progress<string> progress = new Progress<string>();
         progress.ProgressChanged += (sender, status) =>
         {
            _onProgressChange?.Invoke(status);
         };

         Trace.TraceInformation(String.Format("[GitClient] async operation -- begin -- {0}: {1}",
            ProjectKey.ProjectName, arguments));
         _descriptor = ExternalProcess.StartAsync("git", arguments, progress);

         try
         {
            ExternalProcess.Output gitOutput = await _descriptor.TaskCompletionSource.Task;
            Trace.TraceInformation(String.Format("[GitClient] async operation -- end --  {0}: {1}",
               ProjectKey.ProjectName, arguments));
            return gitOutput;
         }
         catch (GitOperationException ex)
         {
            string status = ex.ExitCode == cancellationExitCode ? "cancel" : "error";
            Trace.TraceInformation(String.Format("[GitClient] async operation -- {2} --  {0}: {1}",
               ProjectKey.ProjectName, arguments, status));
            ExceptionHandlers.Handle(ex, "Git operation failed");
            ex.Cancelled = ex.ExitCode == cancellationExitCode;
            throw;
         }
         finally
         {
            _descriptor = null;
         }
      }

      async private Task pickupGitCommandAsync(Action<string> onProgressChange)
      {
         Debug.Assert(_descriptor != null);

         Trace.TraceInformation(String.Format("[GitClient] async operation -- picking up -- start -- {0}",
            ProjectKey.ProjectName));

         _onProgressChange = onProgressChange;

         try
         {
            await _descriptor.TaskCompletionSource.Task;
         }
         catch (GitOperationException ex)
         {
            string status = ex.ExitCode == cancellationExitCode ? "cancel" : "error";
            Trace.TraceInformation(String.Format("[GitClient] async operation -- picking up -- {1} --  {0}",
               ProjectKey.ProjectName, status));
            ExceptionHandlers.Handle(ex, "Git operation failed");
            ex.Cancelled = ex.ExitCode == cancellationExitCode;
            throw;
         }

         Trace.TraceInformation(String.Format("[GitClient] async operation -- picking up -- end -- {0}",
            ProjectKey.ProjectName));
      }

      private static string escapeSpaces(string unescaped)
      {
         return unescaped.Contains(' ') ? '"' + unescaped + '"' : unescaped;
      }

      private readonly Dictionary<GitDiffArguments, IEnumerable<string>> _cachedDiffs =
         new Dictionary<GitDiffArguments, IEnumerable<string>>();

      private readonly Dictionary<GitRevisionArguments, IEnumerable<string>> _cachedRevisions =
         new Dictionary<GitRevisionArguments, IEnumerable<string>>();

      private readonly Dictionary<GitListOfRenamesArguments, IEnumerable<string>> _cachedListOfRenames =
         new Dictionary<GitListOfRenamesArguments, IEnumerable<string>>();

      private bool _isDisposed = false;
      private ExternalProcess.AsyncTaskDescriptor _descriptor;
      private readonly List<ExternalProcess.AsyncTaskDescriptor> _liteDescriptors =
         new List<ExternalProcess.AsyncTaskDescriptor>();

      private Action<string> _onProgressChange;
   }
}

