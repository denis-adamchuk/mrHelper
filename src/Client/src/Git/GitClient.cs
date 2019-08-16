using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Git
{
   /// <summary>
   /// Provides access to git repository.
   /// All methods throw GitOperationException if corresponding git command exited with a not-zero code.
   /// </summary>
   public class GitClient : IGitRepository
   {
      // Path of this git repository
      public string Path { get; set; }

      // Object which keeps this git repository up-to-date
      public GitClientUpdater Updater { get; }

      public event EventHandler<string> OnOperationStatusChange;

      /// <summary>
      /// Construct GitClient with a path that either does not exist or it is empty or points to a valid git repository
      /// Throws ArgumentException if requirements on `path` argument are not met
      /// </summary>
      internal GitClient(string hostname, string projectname, string path)
      {
         if (!canClone(path) && !isValidRepository(path))
         {
            throw new ArgumentException();
         }

         _hostName = hostname;
         _projectName = projectname;
         Path = path;
         Updater = new GitClientUpdater(async () =>
         {
            if (_descriptor != null)
            {
               Debug.Assert(false);
               return;
            }

            string arguments = canClone(Path) ? "clone --progress " + _hostName + "/" + _projectName + " " + Path
                                              : "fetch --progress";
            await run_async(arguments, null, true);
         });
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
      /// Create an asyncronous task for 'git difftool --dir-diff' command
      /// </summary>
      async public Task DiffToolAsync(string name, string leftCommit, string rightCommit)
      {
         if (_descriptor != null)
         {
            Debug.Assert(false);
            return;
         }

         await (Task)run_inPath(() =>
         {
            string arguments = "difftool --dir-diff --tool=" + name + " " + leftCommit + " " + rightCommit;
            return run_async(arguments, 500, false);
         }, Path);
      }

      /// <summary>
      /// Cancel currently running git async operation
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
         return (List<string>)run_inPath(() =>
         {
            string arguments = "diff " + leftcommit + " " + rightcommit + " --numstat --diff-filter=R";
            return GitUtils.git(arguments);
         }, Path);
      }

      public List<string> ShowFileByRevision(string filename, string sha)
      {
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

      private delegate object command();

      /// <summary>
      /// Check if Clone can be called for this GitClient
      /// </summary>
      static private bool canClone(string path)
      {
         return !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any();
      }

      static private bool isValidRepository(string path)
      {
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
         }, path);
      }

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

      async private Task run_async(string arguments, int? timeout, bool reportProgress)
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
         try
         {
            await _descriptor.TaskCompletionSource.Task;
         }
         finally
         {
            _descriptor = null;
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

      private string _hostName { get; }
      private string _projectName { get; }
   }
}

