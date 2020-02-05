using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using mrHelper.Common.Exceptions;
using mrHelper.CommonNative;

namespace mrHelper.Common.Tools
{
   public static class ExternalProcess
   {
      public class Result
      {
         public int ExitCode;
         public IEnumerable<string> StdOut;
         public IEnumerable<string> StdErr;
      };

      /// <summary>
      /// Launch a process with arguments passed and waits for process completion if needed.
      /// Return StdOutput content if process exited with exit code 0, otherwise throws.
      /// </summary>
      static public Result Start(string name, string arguments, bool wait, string path)
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
            List<string> standardOutput = new List<string>();
            process.OutputDataReceived +=
               (sender, args) =>
            {
               if (args.Data != null)
               {
                  standardOutput.Add(args.Data);
               }
            };

            List<string> standardError = new List<string>();
            process.ErrorDataReceived +=
               (sender, args) =>
            {
               if (args.Data != null)
               {
                  standardError.Add(args.Data);
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
               else
               {
                  process.CancelOutputRead();
                  process.CancelErrorRead();
               }
            }

            if (exitcode != 0)
            {
               throw new ExternalProcessException(arguments, exitcode,
                  standardError?.ToArray() ?? Array.Empty<string>());
            }

            return new Result
            {
               ExitCode = process.HasExited ? -1 : process.Id,
               StdOut = standardOutput,
               StdErr = standardError
            };
         }
      }

      public class AsyncTaskDescriptor
      {
         public Action<string> OnProgressChange;
         public Task<object[]> Task;
         public Process Process;
         public bool Cancelled;
         public IEnumerable<string> StdOut;
         public IEnumerable<string> StdErr;
      }

      /// <summary>
      /// Create a task to start a process asynchronously
      /// </summary>
      static public AsyncTaskDescriptor StartAsync(string name, string arguments, string path,
         Action<string> onProgressChange, ISynchronizeInvoke synchronizeInvoke)
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

         Progress<string> progress = new Progress<string>();

         Func<List<string>, Action<DataReceivedEventHandler>, Action, TaskCompletionSource<object>> addStdHandler =
            (std, addHandler, removeHandler) =>
         {
            TaskCompletionSource<object> tcsStd = new TaskCompletionSource<object>();
            DataReceivedEventHandler onDataReceived = null;
            onDataReceived = new DataReceivedEventHandler(
               (sender, args) =>
            {
               if (args.Data != null)
               {
                  std?.Add(args.Data);
                  (progress as IProgress<string>).Report(getStatus(arguments, args.Data));
               }
               else
               {
                  removeHandler();
                  tcsStd.SetResult(null);
               }
            });

            addHandler(onDataReceived);
            return tcsStd;
         };

         TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
         List<Task<object>> tasks = new List<Task<object>>{ tcs.Task };

         List<string> standardOutput = new List<string>();
         tasks.Add(addStdHandler(standardOutput,
            x => process.OutputDataReceived += x, () => process.CancelOutputRead()).Task);

         List<string> standardError = new List<string>();
         tasks.Add(addStdHandler(standardError,
            x => process.ErrorDataReceived += x, () => process.CancelErrorRead()).Task);

         EventHandler onExited = null;
         onExited = new EventHandler(
            (sender, args) =>
         {
            process.EnableRaisingEvents = false;
            process.Exited -= onExited;
            if (process.ExitCode != 0)
            {
               tcs.SetException(new ExternalProcessException(arguments, process.ExitCode, standardError.ToArray()));
            }
            else
            {
               tcs.SetResult(null);
            }
         });
         process.Exited += onExited;

         AsyncTaskDescriptor descriptor = new AsyncTaskDescriptor
         {
            Task = Task.WhenAll(tasks),
            Process = process,
            StdOut = standardOutput,
            StdErr = standardError,
            OnProgressChange = onProgressChange
         };

         progress.ProgressChanged += (sender, status) =>
         {
            descriptor?.OnProgressChange?.Invoke(status);
         };

         (progress as IProgress<string>).Report(getStatus(arguments, "in progress..."));
         process.Start();

         process.BeginOutputReadLine();
         process.BeginErrorReadLine();

         return descriptor;
      }

      /// <summary>
      /// from https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c
      /// </summary>
      public static void Cancel(Process process)
      {
         bool attachedToConsole = NativeMethods.AttachConsole((uint)process.Id);
         Trace.TraceInformation(String.Format("AttachConsole() finished with {0}", attachedToConsole));
         if (!attachedToConsole)
         {
            return;
         }

         try
         {
            NativeMethods.SetConsoleCtrlHandler(null, true);

            bool sendCtrlC = NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CTRL_C_EVENT, 0);
            Trace.TraceInformation(String.Format("GenerateConsoleCtrlEvent() returned {0}", sendCtrlC));
         }
         finally
         {
            bool consoleFreed = NativeMethods.FreeConsole();
            Trace.TraceInformation(String.Format("FreeConsole() finished with {0}", consoleFreed));

            NativeMethods.SetConsoleCtrlHandler(null, false);
         }
      }
   }
}

