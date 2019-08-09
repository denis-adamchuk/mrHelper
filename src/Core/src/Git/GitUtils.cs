using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace mrHelper.Core.Git
{
   public static class GitUtils
   {
      public class OperationStatusChangeArgs : EventArgs
      {
         public OperationStatusChangeArgs(string status)
         {
            Status = status;
         }

         public string Status { get; }
      }

      public class GitAsyncTaskDescriptor
      {
         public TaskCompletionSource<int> TaskCompletionSource;
         public int ProcessId;
      }

      /// <summary>
      /// Launch 'git' with arguments passed and waits for process completion if needed.
      /// Return StdOutput content if process exited with exit code 0, otherwise throws.
      /// </summary>
      static internal List<string> git(string arguments)
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

         process.OutputDataReceived += (sender, args) => { if (args.Data != null) output.Add(args.Data); };
         process.ErrorDataReceived += (sender, args) => { if (args.Data != null) errors.Add(args.Data); };

         process.Start();

         process.BeginOutputReadLine();
         process.BeginErrorReadLine();

         process.WaitForExit();

         if (process.ExitCode != 0)
         {
            throw new GitOperationException(arguments, process.ExitCode, errors);
         }
         else if (errors.Count > 0)
         {
            Trace.TraceWarning(String.Format("\"git {0}\" returned exit code 0, but stderr is not empty:\n{1}",
               arguments, String.Join("\n", errors)));
         }
         return output;
      }

      /// <summary>
      /// Create a task to 'git' with arguments passed asynchronously
      /// </summary>
      static internal GitAsyncTaskDescriptor gitAsync(string arguments, int? timeout, IProgress<string> progress)
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
            },
            EnableRaisingEvents = true
         };

         string getStatus(string fullCommand, string details)
         {
            int cmdnamelen = fullCommand.IndexOf(' ');
            string cmdName = fullCommand.Substring(0, cmdnamelen >= 0 ? cmdnamelen : fullCommand.Length);
            return String.Format("git {0} is in progress {1}{2}",
               cmdName, (details.Length > 0 ? ": " : String.Empty), details.ToString());
         };

         progress?.Report(getStatus(arguments, String.Empty));

         process.OutputDataReceived +=
            (sender, args) =>
         {
            if (args.Data != null)
            {
               output.Add(args.Data);
               progress?.Report(getStatus(arguments, output[output.Count - 1]));
            }
         };

         process.ErrorDataReceived +=
            (sender, args) =>
         {
            if (args.Data != null)
            {
               errors.Add(args.Data);
               progress?.Report(getStatus(arguments, errors[errors.Count - 1]));
            }
         };

         TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

         process.Exited +=
            (sender, args) =>
         {
            if (!tcs.Task.IsCompleted)
            {
               process.CancelOutputRead();
               process.CancelErrorRead();
               if (process.ExitCode == 0)
               {
                  tcs.SetResult(process.ExitCode);
               }
               else
               {
                  tcs.SetException(new GitOperationException(arguments, process.ExitCode, errors));
               }
               process.Dispose();
            }
         };

         if (timeout.HasValue)
         {
            Timer timer = new Timer { Interval = timeout.Value };
            timer.Elapsed +=
               (sender, args) =>
            {
               timer.Stop();
               if (!tcs.Task.IsCompleted)
               {
                  process.CancelOutputRead();
                  process.CancelErrorRead();
                  tcs.SetResult(0);
                  process.Dispose();
               }
            };
            timer.Start();
         }

         process.Start();

         process.BeginOutputReadLine();
         process.BeginErrorReadLine();

         GitAsyncTaskDescriptor d = new GitAsyncTaskDescriptor
         {
            TaskCompletionSource = tcs,
            ProcessId = process.Id
         };
         return d;
      }

      /// <summary>
      /// Adds a difftool with the given name and command to the global git configuration.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      static public void SetGlobalDiffTool(string name, string command)
      {
         // No need to change current directory because we're changing a global setting
         string arguments = "config --global difftool." + name + ".cmd " + command;
         git(arguments);
      }

      /// <summary>
      /// Removes a section for the difftool with the passed name from the global git configuration.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      static public void RemoveGlobalDiffTool(string name)
      {
         // No need to change current directory because we're changing a global setting
         string arguments = "config --global --remove-section difftool." + name;
         git(arguments);
      }

      /// <summary>
      /// Sets http.sslVerify flag in the global git configuration to the given value.
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      static public void SetGlobalSSLVerify(bool flag)
      {
         // No need to change current directory because we're changing a global setting
         string arguments = "config --global http.sslVerify " + flag.ToString().ToLower();
         git(arguments);
      }
   }
}

