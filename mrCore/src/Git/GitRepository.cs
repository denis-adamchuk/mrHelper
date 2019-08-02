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
      // Timestamp of the most recent fetch/clone
      public DateTime LastUpdateTime { get; private set; }

      public event EventHandler<GitUtils.OperationStatusChangeArgs> OnOperationStatusChange;
      public event EventHandler<EventArgs> OnOperationCompleted;

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
         LastUpdateTime = DateTime.MinValue;
      }

      public async void CloneAsync(string host, string project, string path)
      {
         string arguments = "clone " + host + "/" + project + " " + path;

         Progress<string> progress = new Progress<string>();

         progress.ProgressChanged += (sender, status) =>
         {
            OnOperationStatusChange?.Invoke(sender, new GitUtils.OperationStatusChangeArgs(status));
         };

         Task<List<string>> task = Task.Factory.StartNew(() => GitUtils.git(arguments, true, progress)); 
         _tasks.Add(task);
         List<string> r = await task;
         _tasks.Remove(task);

         OnOperationCompleted?.Invoke(this, null);

         LastUpdateTime = DateTime.Now;
      }

      public async void FetchAsync()
      {
         Progress<string> progress = new Progress<string>();

         progress.ProgressChanged += (sender, status) =>
         {
            OnOperationStatusChange?.Invoke(sender, new GitUtils.OperationStatusChangeArgs(status));
         };

         Task task = Task.Factory.StartNew(() =>
            run_in_path(() =>
            {
               string arguments = "fetch";
               return GitUtils.git(arguments, true, progress);
            }, _path));
         _tasks.Add(task);
         await task;
         _tasks.Remove(task);

         OnOperationCompleted?.Invoke(this, null);

         LastUpdateTime = DateTime.Now;
      }

      public async void DiffToolAsync(string name, string leftCommit, string rightCommit)
      {
         Task task = Task.Factory.ContinueWhenAll(_tasks.ToArray(), (x) =>
            run_in_path(() =>
            {
               string arguments = "difftool --dir-diff --tool=" + name + " " + leftCommit + " " + rightCommit;
               return GitUtils.git(arguments, true/*, OnOperationStatusChange, OnOperationCompleted*/);
            }, _path));
         _tasks.Add(task);
         await task;
         _tasks.Remove(task);
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
            return GitUtils.git(arguments, true);
         }, _path);

         _cachedDiffs[key] = result;
         return result;
      }

      public List<string> GetListOfRenames(string leftcommit, string rightcommit)
      {
         return (List<string>)run_in_path(() =>
         {
            string arguments = "diff " + leftcommit + " " + rightcommit + " --numstat --diff-filter=R";
            return GitUtils.git(arguments, true);
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
            return GitUtils.git(arguments, true);
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
               GitUtils.git(arguments, true);
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
   }
}

