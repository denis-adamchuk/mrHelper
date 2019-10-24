using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using mrHelper.Common.Exceptions;

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

      public class GitAsyncTaskDescriptor
      {
         public TaskCompletionSource<GitOutput> TaskCompletionSource;
         public Process Process;
         public bool Cancelled;
      }

      public struct GitOutput
      {
         public List<string> Output;
         public List<string> Errors;
         public int PID;
      }

      /// <summary>
      /// Launch 'git' with arguments passed and waits for process completion if needed.
      /// Return StdOutput content if process exited with exit code 0, otherwise throws.
      /// </summary>
      static public GitOutput git(string arguments, bool wait = true)
      {
         List<string> output = new List<string>();
         List<string> errors = new List<string>();

         using (Process process = new Process
         {
            StartInfo = new ProcessStartInfo
            {
               FileName = "git",
               Arguments = arguments,
               UseShellExecute = false,
               RedirectStandardInput = true,
               RedirectStandardOutput = true,
               RedirectStandardError = true,
               CreateNoWindow = true
            }
         })
         {
            process.OutputDataReceived += (sender, args) => { if (args.Data != null) output.Add(args.Data); };
            process.ErrorDataReceived += (sender, args) => { if (args.Data != null) errors.Add(args.Data); };

            process.Start();

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

            checkGitExitCode(arguments, exitcode, errors);
            return new GitOutput { Output = output, Errors = errors, PID = process.HasExited ? -1 : process.Id };
         }
      }

      /// <summary>
      /// Create a task to 'git' with arguments passed asynchronously
      /// </summary>
      static public GitAsyncTaskDescriptor gitAsync(string arguments, IProgress<string> progress)
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
               RedirectStandardInput = true,
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
            return String.Format("git {0}{1}{2}",
               cmdName, (details.Length > 0 ? ": " : String.Empty), details.ToString());
         };

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

         TaskCompletionSource<GitOutput> tcs = new TaskCompletionSource<GitOutput>();

         process.Exited +=
            (sender, args) =>
         {
            process.WaitForExit();
            if (!tcs.Task.IsCompleted)
            {
               process.CancelOutputRead();
               process.CancelErrorRead();
               try
               {
                  checkGitExitCode(arguments, process.ExitCode, errors);
                  tcs.SetResult(new GitOutput { Output = output, Errors = errors, PID = -1 });
               }
               catch (Exception ex)
               {
                  tcs.SetException(ex);
               }
               process.Dispose();
            }
         };

         progress?.Report(getStatus(arguments, "in progress..."));
         process.Start();

         process.BeginOutputReadLine();
         process.BeginErrorReadLine();

         GitAsyncTaskDescriptor d = new GitAsyncTaskDescriptor
         {
            TaskCompletionSource = tcs,
            Process = process
         };
         return d;
      }

      static private void checkGitExitCode(string arguments, int exitcode, List<string> errors)
      {
         if (exitcode != 0)
         {
            throw new GitOperationException(arguments, exitcode, errors);
         }
         else if (errors.Count > 0)
         {
            Trace.TraceWarning(String.Format("\"git {0}\" returned exit code 0, but stderr is not empty:\n{1}",
                     arguments, String.Join("\n", errors)));
            if (errors[0].StartsWith("fatal:"))
            {
               string reasons =
                  "Possible reasons:\n"
                  + "-Git repository is not up-to-date\n"
                  + "-Given commit is no longer in the repository (force push?)";
               string message = String.Format("git returned \"{0}\". {1}", errors[0], reasons);
               throw new GitObjectException(message, exitcode);
            }
         }
      }

      /// <summary>
      /// from https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c
      /// </summary>
      public static void cancelGit(Process process)
      {
         if (AttachConsole((uint)process.Id))
         {
            SetConsoleCtrlHandler(null, true);
            try
            {
               if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
               {
                  return;
               }
               process.WaitForExit(2000);
            }
            finally
            {
               FreeConsole();
               SetConsoleCtrlHandler(null, false);
            }
         }
      }

      internal const int CTRL_C_EVENT = 0;
      [DllImport("kernel32.dll")]
      internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
      [DllImport("kernel32.dll", SetLastError = true)]
      internal static extern bool AttachConsole(uint dwProcessId);
      [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
      internal static extern bool FreeConsole();
      [DllImport("kernel32.dll")]
      static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
      // Delegate type to be used as the Handler Routine for SCCH
      delegate Boolean ConsoleCtrlDelegate(uint CtrlType);
   }
}

