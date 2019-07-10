using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace mrCore
{
   public class GitRepository
   {
      // Constructor expects a valid git repository as input argument
      public GitRepository(string path)
      {
         if (!Directory.Exists(path) || !IsGitRepository(path))
         {
            throw new ApplicationException("There is no a valid repository at path " + path);
         }

         _path = path;
         _cachedDiffs = new Dictionary<DiffCacheKey, List<string>>();
         _cachedRevisions = new Dictionary<RevisionCacheKey, List<string>>();
      }

      // Creates a new git repository by cloning a passed project into a dir with the same name at the given path
      static public GitRepository CreateByClone(string host, string project, string path)
      {
         // TODO Use shallow clone
         var process = Process.Start("git", "clone " + "https://" + host + "/" + project + " " + path);
         process.WaitForExit();
         if (process.ExitCode == 0)
         {
            return new GitRepository(path);
         }
         return null;
      }

      static public bool IsGitRepository(string dir)
      {
         var cwd = Directory.GetCurrentDirectory();
         List<string> output = null;

         try
         {
            Directory.SetCurrentDirectory(dir);
            var arguments = "rev-parse --is-inside-work-tree";
            output = gatherStdOutputLines(arguments);
         }
         catch (System.Exception)
         {
            // something went wrong
            return false;
         }
         finally
         {
            // revert anyway
            Directory.SetCurrentDirectory(cwd);
         }

         // success path
         return output != null && output.Count > 0 && output[0] == "true";
      }

      public bool Fetch()
      {
         return (bool)exec(() =>
         {
            var process = Process.Start("git", "fetch");
            process.WaitForExit();
            return process.ExitCode == 0;
         });
      }

      public Process DiffTool(string name, string leftCommit, string rightCommit)
      {
         return (Process)exec(() =>
         {
            return Process.Start("git", "difftool --dir-diff --tool=" + name + " " + leftCommit + " " + rightCommit);
         });
      }

      static public bool SetGlobalDiffTool(string name, string command)
      {
         // No need to change current directory because we're changing a global setting (not a repo one)
         var process = Process.Start("git", "config --global difftool." + name + "" + ".cmd " + command);
         process.WaitForExit();
         return process.ExitCode == 0;
      }

      public List<string> Diff(string leftcommit, string rightcommit, string filename, int context)
      {
         DiffCacheKey key = new DiffCacheKey();
         key.sha1 = leftcommit;
         key.sha2 = rightcommit;
         key.filename = filename;
         key.context = context;

         if (_cachedDiffs.ContainsKey(key))
         {
            return _cachedDiffs[key];
         }

         List<string> result = (List<string>)exec(() =>
         {
            var arguments = "diff -U" + context.ToString() + " " + leftcommit + " " + rightcommit + " -- " + filename;
            return gatherStdOutputLines(arguments);
         });

         _cachedDiffs[key] = result;
         return result;
      }

      public List<string> ShowFileByRevision(string filename, string sha)
      {
         RevisionCacheKey key = new RevisionCacheKey();
         key.filename = filename;
         key.sha = sha;

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

      static private List<string> gatherStdOutputLines(string arguments)
      {
         List<string> result = new List<string>();

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
         while (!proc.StandardOutput.EndOfStream)
         {
            result.Add(proc.StandardOutput.ReadLine());
         }

         return result;
      }

      private delegate object command();

      private object exec(command cmd)
      {
         var cwd = Directory.GetCurrentDirectory();
         try
         {
            Directory.SetCurrentDirectory(_path);
            return cmd();
         }
         finally
         {
            Directory.SetCurrentDirectory(cwd);
         }
      }

      readonly string _path; // Path to repository

      private struct DiffCacheKey
      {
         public string sha1;
         public string sha2;
         public string filename;
         public int context;
      }

      private Dictionary<DiffCacheKey, List<string>> _cachedDiffs;

      private struct RevisionCacheKey
      {
         public string sha;
         public string filename;
      }

      private Dictionary<RevisionCacheKey, List<string>> _cachedRevisions;
   }
}

