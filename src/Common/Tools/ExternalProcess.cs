using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using mrHelper.Common.Exceptions;

namespace mrHelper.Common.Tools
{
   public static class ExternalProcess
   {
      public class AsyncTaskDescriptor
      {
         public Task<object[]> Task;
         public Process Process;
         public bool Cancelled;
      }

      public struct Output
      {
         public IEnumerable<string> StdOut;
         public IEnumerable<string> StdErr;
         public int PID;
      }

      /// <summary>
      /// Launch a process with arguments passed and waits for process completion if needed.
      /// Return StdOutput content if process exited with exit code 0, otherwise throws.
      /// </summary>
      static public Output Start(string name, string arguments, bool wait, string path)
      {
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
               CreateNoWindow = true,
               WorkingDirectory = path
            }
         })
         {
            List<string> output = new List<string>();
            process.OutputDataReceived +=
               (sender, args) =>
            {
               if (args.Data != null)
               {
                  output.Add(args.Data);
               }
            };

            List<string> errors = new List<string>();
            process.ErrorDataReceived +=
               (sender, args) =>
            {
               if (args.Data != null)
               {
                  errors.Add(args.Data);
               }
            };

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
               process.WaitForExit(500); // ms
               if (process.HasExited)
               {
                  exitcode = process.ExitCode;
               }
            }

            if (exitcode != 0)
            {
               throw new ExternalProcessException(arguments, exitcode, errors);
            }
            return new Output { StdOut = output, StdErr = errors, PID = process.HasExited ? -1 : process.Id };
         }
      }

      /// <summary>
      /// Create a task to start a process asynchronously
      /// </summary>
      static public AsyncTaskDescriptor StartAsync(string name, string arguments, IProgress<string> progress,
         ISynchronizeInvoke synchronizeInvoke, string path, List<string> standardOutput, List<string> standardError)
      {
         Process process = new Process
         {
            StartInfo = new ProcessStartInfo
            {
               FileName = name,
               Arguments = arguments,
               UseShellExecute = false,
               RedirectStandardInput = true,
               RedirectStandardOutput = true,
               RedirectStandardError = true,
               CreateNoWindow = true,
               WorkingDirectory = path
            },
            EnableRaisingEvents = true,
            SynchronizingObject = synchronizeInvoke
         };

         string getStatus(string fullCommand, string details)
         {
            int cmdnamelen = fullCommand.IndexOf(' ');
            string cmdName = fullCommand.Substring(0, cmdnamelen >= 0 ? cmdnamelen : fullCommand.Length);
            return String.Format("{0} {1}{2}{3}",
               name, cmdName, (details.Length > 0 ? ": " : String.Empty), details.ToString());
         };

         Func<List<string>, Action<DataReceivedEventHandler>, Action<DataReceivedEventHandler>,
            TaskCompletionSource<object>> addStdHandler =
            (std, addHandler, removeHandler) =>
         {
            if (std == null)
            {
               return null;
            }

            TaskCompletionSource<object> tcsStd = new TaskCompletionSource<object>();
            DataReceivedEventHandler onDataReceived = null;
            onDataReceived = new DataReceivedEventHandler(
               (sender, args) =>
            {
               if (args.Data != null)
               {
                  std.Add(args.Data);
                  progress?.Report(getStatus(arguments, args.Data));
               }
               else
               {
                  removeHandler(onDataReceived);
                  tcsStd.SetResult(null);
               }
            });

            addHandler(onDataReceived);
            return tcsStd;
         };

         TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
         List<Task<object>> tasks = new List<Task<object>>{ tcs.Task };

         EventHandler onExited = null;
         onExited = new EventHandler(
            (sender, args) =>
         {
            process.EnableRaisingEvents = false;
            process.Exited -= onExited;
            if (process.ExitCode != 0)
            {
               tcs.SetException(new ExternalProcessException(arguments, process.ExitCode, standardError));
            }
            else
            {
               tcs.SetResult(null);
            }
         });
         process.Exited += onExited;

         TaskCompletionSource<object> tcsStdOut = addStdHandler(standardOutput,
            x => process.OutputDataReceived += x, x => process.OutputDataReceived -= x);
         if (tcsStdOut != null)
         {
            tasks.Add(tcsStdOut.Task);
         }

         TaskCompletionSource<object> tcsStdErr = addStdHandler(standardError,
            x => process.ErrorDataReceived += x, x => process.ErrorDataReceived -= x);
         if (tcsStdErr != null)
         {
            tasks.Add(tcsStdErr.Task);
         }

         progress?.Report(getStatus(arguments, "in progress..."));
         process.Start();

         if (standardOutput != null)
         {
            process.BeginOutputReadLine();
         }

         if (standardError != null)
         {
            process.BeginErrorReadLine();
         }

         return new AsyncTaskDescriptor
         {
            Task = Task.WhenAll(tasks),
            Process = process
         };
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
