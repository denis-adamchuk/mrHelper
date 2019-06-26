using System.Collections.Generic;
using System.Diagnostics;

namespace mrHelper
{
   class gitClient
   {
      static public void CloneRepo(string host, string project, string localDir)
      {
         // TODO Use shallow clone
         Process.Start("git", "clone " + "https://" + host + "/" + project + " " + localDir);
      }

      static public void Fetch()
      {
         Process.Start("git", "fetch");
      }

      static public Process DiffTool(string leftCommit, string rightCommit)
      {
         return Process.Start("git", "difftool --dir-diff --tool=beyondcompare3dd " + leftCommit + " " + rightCommit);
      }

      static public List<string> Diff(string leftCommit, string rightCommit, string filename)
      {
         List<string> result = new List<string>();

         var proc = new Process
         {
            StartInfo = new ProcessStartInfo
            {
               FileName = "git",
               // U0 gives no context, which means to not have unchanged lines in diff snippets.
               // This is needed to unambiguously treat lines missing in diff as unchanged lines to
               // send both old and new lines to gitlab.
               Arguments = "diff -U0 " + leftCommit + " " + rightCommit + " -- " + filename,
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
   }
}
