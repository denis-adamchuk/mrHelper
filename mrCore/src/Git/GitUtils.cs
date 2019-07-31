using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace mrCore
{
   public class GitUtils
   {
      /// <summary>
      /// Launches 'git' with arguments passed and waits for process completion if needed.
      /// Returns StdOutput content if process exited with exit code 0, otherwise throws.
      /// </summary>
      static internal List<string> git(string arguments, bool wait)
      {
         List<string> output = new List<string>();
         List<string> errors = new List<string>();

         var process = new Process
         {
            StartInfo = new ProcessStartInfo
            {
               FileName = "git",
               Arguments = arguments,
               UseShellExecute = false,
               RedirectStandardOutput = true,
               RedirectStandardError = true,
               CreateNoWindow = true
            }
         };

         process.Start();

         process.OutputDataReceived += (sender, args) => { if (args.Data != null) output.Add(args.Data); };
         process.ErrorDataReceived += (sender, args) => { if (args.Data != null) errors.Add(args.Data); };

         process.BeginOutputReadLine();
         process.BeginErrorReadLine();

         int exitcode = 0;
         if (wait)
         {
            process.WaitForExit();
         }
         else
         {
            System.Threading.Thread.Sleep(500); // ms
            if (process.HasExited)
            {
               exitcode = process.ExitCode;
            }
         }

         if (exitcode != 0)
         {
            throw new GitOperationException(arguments, exitcode, errors);
         }
         return output;
      }

      /// <summary>
      /// Adds a difftool with the given name and command to the global git configuration.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      static public void SetGlobalDiffTool(string name, string command)
      {
         // No need to change current directory because we're changing a global setting (not a repo one)
         string arguments = "config --global difftool." + name + "" + ".cmd " + command;
         git(arguments, true);
      }
   }
}

