using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace mrHelper.Common.Tools
{
   public static class ExternalProcess
   {
      public class AsyncTaskDescriptor
      {
         public TaskCompletionSource<Output> TaskCompletionSource;
         public Process Process;
         public bool Cancelled;
      }

      public struct Output
      {
         public List<string> StdOut;
         public List<string> StdErr;
         public int PID;
      }

      /// <summary>
      /// Launch a process with arguments passed and waits for process completion if needed.
      /// Return StdOutput content if process exited with exit code 0, otherwise throws.
      /// </summary>
      static public Output Start(string name, string arguments, bool wait = true)
      {
         List<string> output = new List<string>();
         List<string> errors = new List<string>();

         using (Process process = new Process
         {
            StartInfo = new ProcessStartInfo
            {
               FileName = name,
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
            return new Output { StdOut = output, StdErr = errors, PID = process.HasExited ? -1 : process.Id };
         }
      }

      /// <summary>
      /// Create a task to start a process asynchronously
      /// </summary>
      static public AsyncTaskDescriptor StartAsync(string name, string arguments, IProgress<string> progress)
      {
         List<string> output = new List<string>();
         List<string> errors = new List<string>();

         var process = new Process
         {
            StartInfo = new ProcessStartInfo
            {
               FileName = name,
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
            return String.Format("{0} {1}{2}{3}",
               name, cmdName, (details.Length > 0 ? ": " : String.Empty), details.ToString());
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

         TaskCompletionSource<Output> tcs = new TaskCompletionSource<Output>();

         process.Exited +=
            (sender, args) =>
         {
            process.EnableRaisingEvents = false;
            if (!tcs.Task.IsCompleted)
            {
               try
               {
                  checkGitExitCode(arguments, process.ExitCode, errors);
               }
               catch (Exception ex)
               {
                  tcs.SetException(ex);
                  try
                  {
                     process.CancelOutputRead();
                     process.CancelErrorRead();
                  }
                  catch (InvalidOperationException)
                  {
                     Debug.Assert(false);
                  }
                  return;
               }

               try
               {
                  Debug.Assert(process.ExitCode == 0);
                  process.WaitForExit();
                  try
                  {
                     process.CancelOutputRead();
                     process.CancelErrorRead();
                  }
                  catch (InvalidOperationException)
                  {
                     Debug.Assert(false);
                  }
               }
               catch (InvalidOperationException)
               {
                  Debug.Assert(false);
               }

               tcs.SetResult(new Output { StdOut = output, StdErr = errors, PID = -1 });
               process.Dispose();
            }
         };

         progress?.Report(getStatus(arguments, "in progress..."));
         process.Start();

         process.BeginOutputReadLine();
         process.BeginErrorReadLine();

         AsyncTaskDescriptor d = new AsyncTaskDescriptor
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
         else if (errors.Count > 0 && errors[0].StartsWith("fatal:"))
         {
            string reasons =
               "Possible reasons:\n"
               + "-Git repository is not up-to-date\n"
               + "-Given commit is no longer in the repository (force push?)";
            string message = String.Format("git returned \"{0}\". {1}", errors[0], reasons);
            throw new GitObjectException(message, exitcode);
         }
      }

      /// <summary>
      /// from https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c
      /// </summary>
      public static void Cancel(Process process)
      {
         if (NativeMethods.AttachConsole((uint)process.Id))
         {
            NativeMethods.SetConsoleCtrlHandler(null, true);
            try
            {
               if (!NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CTRL_C_EVENT, 0))
               {
                  return;
               }
               process.WaitForExit(2000);
            }
            finally
            {
               NativeMethods.FreeConsole();
               NativeMethods.SetConsoleCtrlHandler(null, false);
            }
         }
      }
   }
}
