using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace mrCore
{
   public class GitOperationException : Exception
   {
      public GitOperationException(string command, int exitcode, List<string> errorOutput)
         : base(String.Format("command \"{0}\" exited with code {1}", command, exitcode.ToString()))
      {
         Details = String.Join("\n", errorOutput);
      }

      public string Details { get; }
   }

   /// <summary>
   /// Provides access to git repository.
   /// All methods throw GitOperationException if corresponding git command exited with a not-zero code.
   /// </summary>
   public class GitRepository
   {
      // Timestamp of the most recent fetch/clone, by default it is empty
      public DateTime? LastUpdateTime { get; private set; }

      public event EventHandler<GitUtils.OperationStatusChangeArgs> OnOperationStatusChange;

      /// <summary>
      /// Constructor expects a valid git repository as input argument
      /// Throws ArgumentException
      /// </summary>
      public GitRepository(string path, bool check)
      {
         if (check && (!Directory.Exists(path) || !IsGitRepository(path)))
         {
            throw new ArgumentException("There is no a valid repository at path " + path);
         }

         _path = path;
      }

      public Task<int> CloneAsync(string host, string project, string path)
      {
         if (_asyncPid.HasValue)
         {
            throw new InvalidOperationException("Another acync operation is running");
         }

         string arguments = "clone --progress " + host + "/" + project + " " + path;
         return (Task<int>)run_tracked_progress_async(arguments, true);
      }

      public Task<int> FetchAsync()
      {
         if (_asyncPid.HasValue)
         {
            throw new InvalidOperationException("Another acync operation is running");
         }

         return (Task<int>)run_in_path(() =>
         {
            string arguments = "fetch --progress";
            return run_tracked_progress_async(arguments, true);
         }, _path);
      }

      public Task<int> DiffToolAsync(string name, string leftCommit, string rightCommit)
      {
         if (_asyncPid.HasValue)
         {
            throw new InvalidOperationException("Another acync operation is running");
         }

         return (Task<int>)run_in_path(() =>
         {
            string arguments = "difftool --dir-diff --tool=" + name + " " + leftCommit + " " + rightCommit;
            return GitUtils.gitAsync(arguments, 500, null);
         }, _path);
      }

      /// <summary>
      /// Cancels currently running git async operation
      /// Throws:
      /// InvalidOperationException if no async operation is running
      /// </summary>
      public void CancelAsyncOperation()
      {
         if (!_asyncPid.HasValue)
         {
            throw new InvalidOperationException("No acync operation is running");
         }

         Process process = null;
         try
         {
            process = Process.GetProcessById(_asyncPid.Value);
         }
         catch (ArgumentException ex)
         {
            // no longer running, nothing to do
            return;
         }
         catch (InvalidOperationException)
         {
            Debug.Assert(false);
            Trace.TraceWarning(String.Format("[InvalidOperationException] Bad git PID {0}", _asyncPid.Value));
            return;
         }

         try
         {
            process.Kill();
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

         List<string> result = (List<string>)run_in_path(() =>
         {
            string arguments =
               "diff -U" + context.ToString() + " " + leftcommit + " " + rightcommit
               + " -- " + (filename1 ?? "") + " " + (filename2 ?? "");
            return GitUtils.git(arguments);
         }, _path);

         _cachedDiffs[key] = result;
         return result;
      }

      public List<string> GetListOfRenames(string leftcommit, string rightcommit)
      {
         return (List<string>)run_in_path(() =>
         {
            string arguments = "diff " + leftcommit + " " + rightcommit + " --numstat --diff-filter=R";
            return GitUtils.git(arguments);
         }, _path);
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

         List<string> result = (List<string>)run_in_path(() =>
         {
            string arguments = "show " + sha + ":" + filename;
            return GitUtils.git(arguments);
         }, _path);

         _cachedRevisions[key] = result;
         return result;
      }

      static public bool IsGitRepository(string dir)
      {
         Debug.Assert(Directory.Exists(dir));

         return (bool)run_in_path(() =>
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

      private delegate object command();

      static private object run_in_path(command cmd, string path)
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

      private Task<int> run_tracked_progress_async(string arguments, bool updateTimestamp)
      {
         Progress<string> progress = new Progress<string>();

         progress.ProgressChanged += (sender, status) =>
         {
            OnOperationStatusChange?.Invoke(sender, new GitUtils.OperationStatusChangeArgs(status));

            // TODO It is not an elegant way to report task completion
            if (updateTimestamp && status == String.Empty)
            {
               LastUpdateTime = DateTime.Now;
               _asyncPid = 0;
            }
         };

         GitUtils.GitAsyncTaskDescriptor d = GitUtils.gitAsync(arguments, null, progress);
         _asyncPid = d.ProcessId;
         return d.Task;
      }

      private readonly string _path; // Path to repository

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

      private List<Task> _tasks = new List<Task>();

      private int? _asyncPid;
   }
}

