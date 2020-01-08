using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Common.Exceptions;
using System.ComponentModel;

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

            checkExitCode(arguments, exitcode, errors);
            return new Output { StdOut = output, StdErr = errors, PID = process.HasExited ? -1 : process.Id };
         }
      }

      /// <summary>
      /// Create a task to start a process asynchronously
      /// </summary>
      static public AsyncTaskDescriptor StartAsync(string name, string arguments, IProgress<string> progress,
         ISynchronizeInvoke synchronizeInvoke, string path)
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

         List<string> output = new List<string>();
         DataReceivedEventHandler onOutputDataReceived =
            (sender, args) =>
         {
            if (args.Data != null)
            {
               output.Add(args.Data);
               progress?.Report(getStatus(arguments, output[output.Count - 1]));
            }
         };
         process.OutputDataReceived += onOutputDataReceived;

         List<string> errors = new List<string>();
         DataReceivedEventHandler onErrorDataReceived =
            (sender, args) =>
         {
            if (args.Data != null)
            {
               errors.Add(args.Data);
               progress?.Report(getStatus(arguments, errors[errors.Count - 1]));
            }
         };
         process.ErrorDataReceived += onErrorDataReceived;

         TaskCompletionSource<Output> tcs = new TaskCompletionSource<Output>();
         process.Exited +=
            (sender, args) =>
         {
            Debug.Assert(process.HasExited);

            process.OutputDataReceived -= onOutputDataReceived;
            process.ErrorDataReceived -= onErrorDataReceived;
            process.EnableRaisingEvents = false;
            if (!tcs.Task.IsCompleted)
            {
               process.CancelOutputRead();
               process.CancelErrorRead();

               try
               {
                  checkExitCode(arguments, process.ExitCode, errors);
               }
               catch (GitOperationException ex)
               {
                  synchronizeInvoke.BeginInvoke(new Action(() => tcs.SetException(ex)), null);
                  return;
               }

               Trace.TraceInformation("Exited process with Id = {0}", process.Id);
               synchronizeInvoke.BeginInvoke(new Action(
                  () => tcs.SetResult(new Output { StdOut = output, StdErr = errors, PID = -1 })), null);
               Trace.TraceInformation("Posted result process with Id = {0}", process.Id);
            }
            Trace.TraceInformation("Exiting from Exited handler Id = {0}", process.Id);
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

      static private void checkExitCode(string arguments, int exitcode, IEnumerable<string> errors)
      {
         if (exitcode != 0)
         {
            throw new GitOperationException(arguments, exitcode, errors);
         }
         else if (errors.Count() > 0 && errors.First().StartsWith("fatal:"))
         {
            // TODO This is specific to git and not to any External Process
            string reasons =
               "Possible reasons:\n"
               + "-Git repository is not up-to-date\n"
               + "-Given commit is no longer in the repository (force push?)";
            string message = String.Format("git returned \"{0}\". {1}", errors.First(), reasons);
            throw new GitObjectException(message, exitcode);
         }
      }

      /// <summary>
      /// from https://stackoverflow.com/questions/283128/how-do-i-send-ctrlc-to-a-process-in-c
      /// </summary>
      public static void Cancel(Process process)
      {
         int id = process.Id;
         Trace.TraceInformation("Cancelling process with Id {0}", id);

         if (NativeMethods.AttachConsole((uint)id))
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
