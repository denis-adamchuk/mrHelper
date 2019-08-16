using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Git
{
   public class NoGitRepository : Exception {}

   /// <summary>
   /// Provides access to git repository.
   /// All methods throw GitOperationException if corresponding git command exited with a not-zero code.
   /// All methods throw NoGitRepository if default constructor is called without succeeding Clone.
   /// </summary>
   public class GitClient : IGitRepository
   {
      // Timestamp of the most recent fetch/clone, by default it is empty
      public DateTime? LastUpdateTime { get; private set; }

      public event EventHandler<string> OnOperationStatusChange;

      /// <summary>
      /// Constructor that creates an object that cannot be user before running Clone.
      /// On attempt to use such an object, NoGitRepository is thrown.
      /// </summary>
      internal GitClient()
      {
      }

      /// <summary>
      /// Constructor that expects a valid git repository as input argument.
      /// Throws ArgumentException
      /// </summary>
      internal GitClient(string path, bool enableUpdates)
      {
         if (!IsGitClient(path))
         {
            throw new ArgumentException("There is no a valid repository at path " + path);
         }

         Path = path;

         if (enableUpdates)
         {
            Updater = new GitClientUpdater(this);
         }
      }

      /// <summary>
      /// Create an asynchronous task for 'git close' command
      /// Throws:
      /// InvalidOperationException if another async operation is running
      /// </summary>
      async public Task CloneAsync(string host, string project, string path, bool enableUpdates)
      {
         if (IsGitClient(Path))
         {
            throw new InvalidOperationException("Multiple Clone not supported");
         }

         if (_descriptor != null)
         {
            Debug.Assert(false);
            return;
         }

         string arguments = "clone --progress " + host + "/" + project + " " + path;
         await run_async(arguments, null, true, true);

         Debug.Assert(IsGitClient(path));
         Path = path;

         if (enableUpdates)
         {
            Updater = new GitClientUpdater(this);
         }
      }

      /// <summary>
      /// Create an asyncronous task for 'git fetch' command
      /// Throws:
      /// InvalidOperationException if another async operation is running
      /// </summary>
      async public Task FetchAsync(bool reportProgress)
      {
         if (!IsGitClient(Path))
         {
            throw new NoGitRepository();
         }

         if (_descriptor != null)
         {
            Debug.Assert(false);
            return;
         }

         await (Task)run_inPath(() =>
         {
            string arguments = "fetch --progress";
            return run_async(arguments, null, true, reportProgress);
         }, Path);
      }

      /// <summary>
      /// Create an asyncronous task for 'git difftool --dir-diff' command
      /// Throws:
      /// InvalidOperationException if another async operation is running
      /// </summary>
      async public Task DiffToolAsync(string name, string leftCommit, string rightCommit)
      {
         if (!IsGitClient(Path))
         {
            throw new NoGitRepository();
         }

         if (_descriptor != null)
         {
            Debug.Assert(false);
            return;
         }

         await (Task)run_inPath(() =>
         {
            string arguments = "difftool --dir-diff --tool=" + name + " " + leftCommit + " " + rightCommit;
            return run_async(arguments, 500, false, false);
         }, Path);
      }

      /// <summary>
      /// Cancel currently running git async operation
      /// Throws:
      /// InvalidOperationException if no async operation is running
      /// </summary>
      public void CancelAsyncOperation()
      {
         if (_descriptor == null)
         {
            Debug.Assert(false);
            return;
         }

         Process process = null;
         try
         {
            process = Process.GetProcessById(_descriptor.ProcessId);
         }
         catch (ArgumentException)
         {
            // no longer running, nothing to do
            return;
         }
         catch (InvalidOperationException)
         {
            Debug.Assert(false);
            Trace.TraceWarning(String.Format("[InvalidOperationException] Bad git PID {0}", _descriptor.ProcessId));
            return;
         }
         finally
         {
            _descriptor = null;
         }

         try
         {
            process.Kill();
            process.WaitForExit();
         }
         catch (Exception)
         {
            // most likely the process already exited, nothing to do
         }
      }

      // 'null' filename strings will be replaced with empty strings
      public List<string> Diff(string leftcommit, string rightcommit, string filename1, string filename2, int context)
      {
         if (!IsGitClient(Path))
         {
            throw new NoGitRepository();
         }

         DiffCacheKey key = new DiffCacheKey
         {
            sha1 = leftcommit,
            sha2 = rightcommit,
            filename1 = filename1,
            filename2 = filename2,
            context = context
         };

         if (_cachedDiffs.ContainsKey(key))
         {
            return _cachedDiffs[key];
         }

         List<string> result = (List<string>)run_inPath(() =>
         {
            string arguments =
               "diff -U" + context.ToString() + " " + leftcommit + " " + rightcommit
               + " -- " + (filename1 ?? "") + " " + (filename2 ?? "");
            return GitUtils.git(arguments);
         }, Path);

         _cachedDiffs[key] = result;
         return result;
      }

      public List<string> GetListOfRenames(string leftcommit, string rightcommit)
      {
         if (!IsGitClient(Path))
         {
            throw new NoGitRepository();
         }

         return (List<string>)run_inPath(() =>
         {
            string arguments = "diff " + leftcommit + " " + rightcommit + " --numstat --diff-filter=R";
            return GitUtils.git(arguments);
         }, Path);
      }

      public List<string> ShowFileByRevision(string filename, string sha)
      {
         if (!IsGitClient(Path))
         {
            throw new NoGitRepository();
         }

         RevisionCacheKey key = new RevisionCacheKey
         {
            filename = filename,
            sha = sha
         };

         if (_cachedRevisions.ContainsKey(key))
         {
            return _cachedRevisions[key];
         }

         List<string> result = (List<string>)run_inPath(() =>
         {
            string arguments = "show " + sha + ":" + filename;
            return GitUtils.git(arguments);
         }, Path);

         _cachedRevisions[key] = result;
         return result;
      }

      static public bool IsGitClient(string dir)
      {
         if (dir == null || !Directory.Exists(dir))
         {
            return false;
         }

         return (bool)run_inPath(() =>
         {
            try
            {
               var arguments = "rev-parse --is-inside-work-tree";
               GitUtils.git(arguments);
               return true;
            }
            catch (GitOperationException)
            {
               return false;
            }
         }, dir);
      }

      public string Path { get; set; }
      public GitClientUpdater Updater { get => _updater; private set => _updater = value; }

      private delegate object command();

      static private object run_inPath(command cmd, string path)
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

      async private Task run_async(string arguments, int? timeout, bool updateTimeStamp, bool reportProgress)
      {
         Progress<string> progress = reportProgress ? new Progress<string>() : null;
         if (reportProgress)
         {
            progress.ProgressChanged += (sender, status) =>
            {
               OnOperationStatusChange?.Invoke(sender, status);
            };
         }

         _descriptor = GitUtils.gitAsync(arguments, timeout, progress);
         await _descriptor.TaskCompletionSource.Task;
         _descriptor = null;

         if (updateTimeStamp)
         {
            LastUpdateTime = DateTime.Now;
         }
      }

      private struct DiffCacheKey
      {
         public string sha1;
         public string sha2;
         public string filename1;
         public string filename2;
         public int context;
      }

      private readonly Dictionary<DiffCacheKey, List<string>> _cachedDiffs =
         new Dictionary<DiffCacheKey, List<string>>();

      private struct RevisionCacheKey
      {
         public string sha;
         public string filename;
      }

      private readonly Dictionary<RevisionCacheKey, List<string>> _cachedRevisions =
         new Dictionary<RevisionCacheKey, List<string>>();

      private GitUtils.GitAsyncTaskDescriptor _descriptor = null;
      private GitClientUpdater _updater;
   }
}

