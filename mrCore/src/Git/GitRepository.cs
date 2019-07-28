using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace mrCore
{
   public class GitRepository
   {
      // Constructor expects a valid git repository as input argument
      public GitRepository(string path, DateTime? lastUpdateTime = null)
      {
         if (!Directory.Exists(path) || !isGitRepository(path))
         {
            throw new ArgumentException("There is no a valid repository at path " + path);
         }

         _path = path;
         _cachedDiffs = new Dictionary<DiffCacheKey, List<string>>();
         _cachedRevisions = new Dictionary<RevisionCacheKey, List<string>>();
         LastUpdateTime = lastUpdateTime ?? new DateTime();
      }

      public DateTime LastUpdateTime { get; private set; }

      // Creates a new git repository by cloning a passed project into a dir with the same name at the given path
      static public GitRepository CreateByClone(string host, string project, string path)
      {
         List<string> output = null;
         List<string> errors = null;
         string arguments = "clone " + "https://" + host + "/" + project + " " + path;

         int code = gatherStdOutputLines(arguments, true, out output, out errors);
         if (code != 0)
         {
            throw new GitOperationException(code, errors);
         }

         return new GitRepository(path, DateTime.Now);
      }

      public bool Fetch()
      {
         run_in_path(() =>
         {
            List<string> output = null;
            List<string> errors = null;
            string arguments = "fetch";

            int code = gatherStdOutputLines(arguments, true, out output, out errors);
            if (code != 0)
            {
               throw new GitOperationException(code, errors);
            }
         });

         LastUpdateTime = DateTime.Now;
      }

      public void DiffTool(string name, string leftCommit, string rightCommit)
      {
         run_in_path(() =>
         {
            List<string> output = null;
            List<string> errors = null;
            string arguments = "difftool --dir-diff --tool=" + name + " " + leftCommit + " " + rightCommit;

            int code = gatherStdOutputLines(arguments, false, out output, out errors);
            if (code != 0)
            {
               throw new GitOperationException(code, errors);
            }
         });
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

         List<string> result = (List<string>)exec(() =>
         {
            var arguments = "diff -U" + context.ToString() + " " + leftcommit + " " + rightcommit
            + " -- " + (filename1 ?? "") + " " + (filename2 ?? "");
            return gatherStdOutputLines(arguments);
         });

         _cachedDiffs[key] = result;
         return result;
      }

      public List<string> GetListOfRenames(string leftcommit, string rightcommit)
      {
         return (List<string>)exec(() =>
         {
            var arguments = "diff " + leftcommit + " " + rightcommit + " --numstat --diff-filter=R";
            return gatherStdOutputLines(arguments);
         });
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

         List<string> result = (List<string>)exec(() =>
         {
            return gatherStdOutputLines("show " + sha + ":" + filename);
         });

         _cachedRevisions[key] = result;
         return result;
      }

      static private bool isGitRepository(string dir)
      {
         Debug.Assert(Directory.Exists(dir));

         return (bool)exec(() =>
         {
            List<string> output = null;
            List<string> errors = null;
            var arguments = "rev-parse --is-inside-work-tree";

            return gatherStdOutputLines(arguments, output, errors) == 0;
         }, dir);
      }

      static private int gatherStdOutputLines(string arguments, bool wait, out output, out errors)
      {
         output = new List<string>();
         errors = new List<string>();

         var proc = new Process
         {
            StartInfo = new ProcessStartInfo
            {
               FileName = "git",
               Arguments = arguments,
               UseShellExecute = false,
               RedirectStandardOutput = true,
               CreateNoWindow = true
            }
         };

         proc.Start();

         process.OutputDataReceived += (sender, args) => output.Add(args.Data);
         process.ErrorDataReceived += (sender, args) => errors.Add(args.Data);

         process.BeginOutputReadLine();
         process.BeginErrorReadLine();

         if (wait)
         {
            process.WaitForExit();
         }
         else
         {
            Thread.Sleep(500); // ms
            if (process.HasExited)
            {
               return process.ExitCode;
            }
            else
            {
               return 0;
            }
         }

         return process.ExitCode;
      }

      private delegate object command();

      static private object run_in_path(command cmd, string path)
      {
         var cwd = Directory.GetCurrentDirectory();
         try
         {
            Directory.SetCurrentDirectory(path);
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

      private readonly Dictionary<DiffCacheKey, List<string>> _cachedDiffs;

      private struct RevisionCacheKey
      {
         public string sha;
         public string filename;
      }

      private readonly Dictionary<RevisionCacheKey, List<string>> _cachedRevisions;
   }
}

