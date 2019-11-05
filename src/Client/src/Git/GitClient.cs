using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Core.Git;
using static mrHelper.Core.Git.GitUtils;
using static mrHelper.Client.Git.Types;

namespace mrHelper.Client.Git
{
   // TODO Split GitClient and IGitRepository

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

      public event Action<GitClient, DateTime> Updated;
      public event Action<GitClient> Disposed;

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
               string arguments = "clone --progress " + ProjectKey.HostName + "/" + ProjectKey.ProjectName + " " + Path;
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
            return GitUtils.git(arguments, false).PID;
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

      private bool cancelDescriptor(GitAsyncTaskDescriptor descriptor)
      {
         if (descriptor == null)
         {
            return false;
         }

         Process p = descriptor.Process;
         descriptor.Cancelled = true;
         try
         {
            GitUtils.cancelGit(descriptor.Process);
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

      public List<string> Diff(string leftcommit, string rightcommit, string filename1, string filename2, int context)
      {
         filename1 = fixupFilename(filename1);
         filename2 = fixupFilename(filename2);

         DiffCacheKey key = new DiffCacheKey { sha1 = leftcommit, sha2 = rightcommit,
            filename1 = filename1, filename2 = filename2, context = context };
         if (_cachedDiffs.ContainsKey(key))
         {
            return _cachedDiffs[key];
         }

         List<string> result = (List<string>)changeCurrentDirectoryAndRun(() =>
         {
            string arguments =
               "diff -U" + context.ToString() + " " + leftcommit + " " + rightcommit
               + " -- " + filename1 + " " + filename2;
            return GitUtils.git(arguments).Output;
         }, Path);

         _cachedDiffs[key] = result;
         return result;
      }

      async public Task<List<string>> DiffAsync(string leftcommit, string rightcommit,
         string filename1, string filename2, int context)
      {
         filename1 = fixupFilename(filename1);
         filename2 = fixupFilename(filename2);

         DiffCacheKey key = new DiffCacheKey { sha1 = leftcommit, sha2 = rightcommit,
            filename1 = filename1, filename2 = filename2, context = context };
         if (_cachedDiffs.ContainsKey(key))
         {
            return _cachedDiffs[key];
         }

         GitOutput gitOutput = await (Task<GitOutput>)(changeCurrentDirectoryAndRun(() =>
         {
            string arguments =
               "diff -U" + context.ToString() + " " + leftcommit + " " + rightcommit
               + " -- " + filename1 + " " + filename2;
            return executeLiteGitCommandAsync(arguments);
         }, Path));

         _cachedDiffs[key] = gitOutput.Output;
         return gitOutput.Output;
      }

      public List<string> GetListOfRenames(string leftcommit, string rightcommit)
      {
         ListOfRenamesCacheKey key = new ListOfRenamesCacheKey { sha1 = leftcommit, sha2 = rightcommit };
         if (_cachedListOfRenames.ContainsKey(key))
         {
            return _cachedListOfRenames[key];
         }

         List<string> result = (List<string>)changeCurrentDirectoryAndRun(() =>
         {
            string arguments = "diff " + leftcommit + " " + rightcommit + " --numstat --diff-filter=R";
            return GitUtils.git(arguments).Output;
         }, Path);

         _cachedListOfRenames[key] = result;
         return result;
      }

      async public Task<List<string>> GetListOfRenamesAsync(string leftcommit, string rightcommit)
      {
         ListOfRenamesCacheKey key = new ListOfRenamesCacheKey { sha1 = leftcommit, sha2 = rightcommit };
         if (_cachedListOfRenames.ContainsKey(key))
         {
            return _cachedListOfRenames[key];
         }

         GitOutput gitOutput = await (Task<GitOutput>)changeCurrentDirectoryAndRun(() =>
         {
            string arguments = "diff " + leftcommit + " " + rightcommit + " --numstat --diff-filter=R";
            return executeLiteGitCommandAsync(arguments);
         }, Path);

         _cachedListOfRenames[key] = gitOutput.Output;
         return gitOutput.Output;
      }

      public List<string> ShowFileByRevision(string filename, string sha)
      {
         filename = fixupFilename(filename);

         RevisionCacheKey key = new RevisionCacheKey { filename = filename, sha = sha };
         if (_cachedRevisions.ContainsKey(key))
         {
            return _cachedRevisions[key];
         }

         List<string> result = (List<string>)changeCurrentDirectoryAndRun(() =>
         {
            string arguments = "show " + sha + ":" + filename;
            return GitUtils.git(arguments).Output;
         }, Path);

         _cachedRevisions[key] = result;
         return result;
      }

      async public Task<List<string>> ShowFileByRevisionAsync(string filename, string sha)
      {
         filename = fixupFilename(filename);

         RevisionCacheKey key = new RevisionCacheKey { filename = filename, sha = sha };
         if (_cachedRevisions.ContainsKey(key))
         {
            return _cachedRevisions[key];
         }

         GitOutput gitOutput = await (Task<GitOutput>)changeCurrentDirectoryAndRun(() =>
         {
            string arguments = "show " + sha + ":" + filename;
            return executeLiteGitCommandAsync(arguments);
         }, Path);

         _cachedRevisions[key] = gitOutput.Output;
         return gitOutput.Output;
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
               var arguments = "rev-parse --is-inside-work-tree";
               GitUtils.GitOutput output = GitUtils.git(arguments);
               return output.Errors.Count == 0;
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

      async private Task<GitOutput> executeLiteGitCommandAsync(string arguments)
      {
         if (_isDisposed)
         {
            throw new GitClientDisposedException(String.Format("GitClient {0} disposed", ProjectKey.ProjectName));
         }

         GitAsyncTaskDescriptor descriptor = null;
         try
         {
            descriptor = GitUtils.gitAsync(arguments, null);
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

      async private Task<GitOutput> executeGitCommandAsync(string arguments, Action<string> onProgressChange)
      {
         if (_isDisposed)
         {
            throw new GitClientDisposedException(String.Format("GitClient {0} disposed", ProjectKey.ProjectName));
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
         _descriptor = GitUtils.gitAsync(arguments, progress);

         try
         {
            GitOutput gitOutput = await _descriptor.TaskCompletionSource.Task;
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

      private string fixupFilename(string filename)
      {
         return filename.Contains(' ') ? '"' + filename + '"' : filename;
      }

      private readonly Dictionary<DiffCacheKey, List<string>> _cachedDiffs =
         new Dictionary<DiffCacheKey, List<string>>();

      private readonly Dictionary<RevisionCacheKey, List<string>> _cachedRevisions =
         new Dictionary<RevisionCacheKey, List<string>>();

      private readonly Dictionary<ListOfRenamesCacheKey, List<string>> _cachedListOfRenames =
         new Dictionary<ListOfRenamesCacheKey, List<string>>();

      private bool _isDisposed = false;
      private GitAsyncTaskDescriptor _descriptor;
      private readonly List<GitAsyncTaskDescriptor> _liteDescriptors = new List<GitAsyncTaskDescriptor>();

      private Action<string> _onProgressChange;
   }
}

