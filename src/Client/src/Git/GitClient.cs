using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Updates;

namespace mrHelper.Client.Git
{
   /// <summary>
   /// Provides access to git repository.
   /// All methods throw GitOperationException if corresponding git command exited with a not-zero code.
   /// </summary>
   public class GitClient : IGitRepository, IDisposable
   {
      // Path of this git repository
      public string Path { get; }

      // Object which keeps this git repository up-to-date
      public GitClientUpdater Updater { get; }

      /// <summary>
      /// Construct GitClient with a path that either does not exist or it is empty or points to a valid git repository
      /// Throws ArgumentException if requirements on `path` argument are not met
      /// </summary>
      internal GitClient(string hostname, string projectname, string path, IProjectWatcher projectWatcher)
      {
         if (!canClone(path) && !isValidRepository(path))
         {
            throw new ArgumentException("Path \"" + path + "\" already exists but it is not a valid git repository");
         }

         _hostName = hostname;
         _projectName = projectname;
         Path = path;
         Updater = new GitClientUpdater(projectWatcher,
            async (reportProgress) =>
         {
            if (_descriptor != null)
            {
               CancelAsyncOperation();

               while (_descriptor != null)
               {
                  await Task.Delay(50);
               }
            }

            if (canClone(Path))
            {
               string arguments = "clone --progress " + _hostName + "/" + _projectName + " " + Path;
               await run_async(arguments, reportProgress);
               return;
            }

            await (Task)run_in_path(() =>
            {
               string arguments = "fetch --progress";
               return run_async(arguments, reportProgress);
            }, Path);
         },
            (hostNameToCheck, projectNameToCheck) =>
         {
            return _hostName == hostNameToCheck && _projectName == projectNameToCheck;
         });

         Trace.TraceInformation(String.Format("[GitClient] Created GitClient at path {0} for host {1} and project {2}",
            path, hostname, projectname));
      }

      public void Dispose()
      {
         Trace.TraceInformation(String.Format("[GitClient] Disposing GitClient at path {0}", Path));
         CancelAsyncOperation();
         Updater.Dispose();
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
         return (int)run_in_path(() =>
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
         if (_descriptor == null)
         {
            return;
         }

         Process p = _descriptor.Process;
         _descriptor.Cancelled = true;
         try
         {
            GitUtils.cancelGit(_descriptor.Process);
         }
         catch (InvalidOperationException)
         {
            // already exited
         }

         p.Dispose();
      }

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
               + " -- " + filename1 + " " + filename2;
            return GitUtils.git(arguments).Output;
         }, Path);

         _cachedDiffs[key] = result;
         return result;
      }

      public List<string> GetListOfRenames(string leftcommit, string rightcommit)
      {
         return (List<string>)run_in_path(() =>
         {
            string arguments = "diff " + leftcommit + " " + rightcommit + " --numstat --diff-filter=R";
            return GitUtils.git(arguments).Output;
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

         List<string> result = (List<string>)run_in_path(() =>
         {
            string arguments = "show " + sha + ":" + filename;
            return GitUtils.git(arguments).Output;
         }, Path);

         _cachedRevisions[key] = result;
         return result;
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

         return (bool)run_in_path(() =>
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

      static private object run_in_path(Func<object> cmd, string path)
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

      async private Task run_async(string arguments, Action<string> onProgressChange)
      {
         Progress<string> progress = onProgressChange != null ? new Progress<string>() : null;
         if (onProgressChange != null)
         {
            progress.ProgressChanged += (sender, status) =>
            {
               onProgressChange(status);
            };
         }

         Trace.TraceInformation(String.Format("[GitClient] async operation -- begin -- {0}: {1}",
            _projectName, arguments));
         _descriptor = GitUtils.gitAsync(arguments, progress);
         try
         {
            await _descriptor.TaskCompletionSource.Task;
            Trace.TraceInformation(String.Format("[GitClient] async operation -- end --  {0}: {1}",
               _projectName, arguments));
         }
         catch (GitOperationException ex)
         {
            int cancellationExitCode = 130;
            string status = ex.ExitCode == cancellationExitCode ? "cancel" : "error";
            Trace.TraceInformation(String.Format("[GitClient] async operation -- {2} --  {0}: {1}",
               _projectName, arguments, status));
            ex.Cancelled = ex.ExitCode == cancellationExitCode;
            throw;
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

