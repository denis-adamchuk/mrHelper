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
         public Result(int exitCode, IEnumerable<string> stdOut, IEnumerable<string> stdErr)
         {
            ExitCode = exitCode;
            StdOut = stdOut;
            StdErr = stdErr;
         }

         public int ExitCode { get; }
         public IEnumerable<string> StdOut { get; }
         public IEnumerable<string> StdErr { get; }
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

            int exitcode = 0;
            try
            {
               process.Start();

               process.BeginOutputReadLine();
               process.BeginErrorReadLine();

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
            }
            catch (Win32Exception ex)
            {
               throw new ExternalProcessSystemException(ex);
            }

            if (exitcode != 0)
            {
               throw new ExternalProcessFailureException(name, arguments, exitcode, standardError.ToArray());
            }

            return new Result(process.HasExited ? -1 : process.Id, standardOutput, standardError);
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

         List<string> standardOutput = new List<string>();
         List<string> standardError = new List<string>();
         TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
         TaskCompletionSource<object> tcsStdOut = new TaskCompletionSource<object>();
         TaskCompletionSource<object> tcsStdErr = new TaskCompletionSource<object>();
         IEnumerable<Task<object>> tasks = new List<Task<object>>{ tcs.Task, tcsStdOut.Task, tcsStdErr.Task };

         EventHandler onExited = null;
         onExited = new EventHandler(
            (sender, args) =>
         {
            process.EnableRaisingEvents = false;
            process.Exited -= onExited;
            if (process.ExitCode != 0)
            {
               tcs.SetException(new ExternalProcessFailureException(name, arguments, process.ExitCode,
                  standardError)); // don't copy standardError because it might be not ready yet
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

         void setStdHandler(TaskCompletionSource<object> tcsStd,
            List<string> std, Action<DataReceivedEventHandler> addHandler, Action removeHandler)
         {
            DataReceivedEventHandler onDataReceived = new DataReceivedEventHandler(
               (sender, args) =>
            {
               if (args.Data != null)
               {
                  std?.Add(args.Data);
                  descriptor?.OnProgressChange?.Invoke(getStatus(arguments, args.Data));
               }
               else
               {
                  removeHandler();
                  tcsStd.SetResult(null);
               }
            });

            addHandler(onDataReceived);
         }

         setStdHandler(tcsStdOut, standardOutput,
            x => process.OutputDataReceived += x, () => process.CancelOutputRead());
         setStdHandler(tcsStdErr, standardError,
            x => process.ErrorDataReceived += x, () => process.CancelErrorRead());

         descriptor?.OnProgressChange?.Invoke(getStatus(arguments, "in progress..."));

         try
         {
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
         }
         catch (Win32Exception ex)
         {
            throw new ExternalProcessSystemException(ex);
         }

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

