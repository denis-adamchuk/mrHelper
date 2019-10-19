using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using mrHelper.Core.Interprocess;
using mrHelper.App.Forms;
using mrHelper.App.Helpers;
using mrHelper.Client.Tools;
using mrHelper.Client.Git;
using mrHelper.Common.Interfaces;
using mrHelper.Core.Matching;
using mrHelper.CommonTools;

namespace mrHelper.App
{
   internal static class Program
   {
      private static void HandleUnhandledException(Exception ex)
      {
         Debug.Assert(false);
         MessageBox.Show("Fatal error occurred, see details in logs",
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         Trace.TraceError("Unhandled exception: [{0}] {1}\nCallstack:\n{2}",
            ex.GetType().ToString(), ex.Message, ex.StackTrace);
         Application.Exit();
      }

      private static readonly Regex url_re = new Regex( String.Format(
         @"^({0}:\/\/)?((http[s]?:\/\/)?[^:\/\s]+)\/(api\/v4\/projects\/)?(\w+\/\w+)\/merge_requests\/(\d*)",
            mrHelper.Common.Constants.Constants.CustomProtocolName), RegexOptions.Compiled | RegexOptions.IgnoreCase);

      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      private static void Main()
      {
         using (LaunchContext context = new LaunchContext())
         {
            Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
            Trace.Listeners.Add(new CustomTraceListener(getLogFileName(context),
               String.Format("Merge Request Helper {0} started", Application.ProductVersion)));

            if (context.IsRunningSingleInstance)
            {
               onLaunchMainInstace(context);
            }
            else
            {
               onLaunchAnotherInstance(context);
            }
         }
      }

      private static void onLaunchMainInstace(LaunchContext context)
      {
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);

         try
         {
            System.Threading.Tasks.Task.Run(
               () =>
            {
               try
               {
                  cleanupLogFiles();
               }
               catch (Exception ex)
               {
                  ExceptionHandlers.Handle(ex, "Failed to clean-up log files");
               }
            });

            if (!checkArguments(context))
            {
               return;
            }

            if (context.Arguments.Length > 2 && context.Arguments[1] == "diff")
            {
               onLaunchFromDiffTool(context);
               return;
            }

            Directory.SetCurrentDirectory(Path.GetDirectoryName(context.CurrentProcess.MainModule.FileName));
            Application.Run(new MainForm());
         }
         catch (Exception ex) // whatever unhandled exception
         {
            HandleUnhandledException(ex);
         }
      }

      private static void onLaunchAnotherInstance(LaunchContext context)
      {
         try
         {
            if (!checkArguments(context))
            {
               return;
            }

            if (context.Arguments.Length > 2 && context.Arguments[1] == "diff")
            {
               onLaunchFromDiffTool(context);
               return;
            }

            IntPtr mainWindow = context.GetWindowByCaption(
               mrHelper.Common.Constants.Constants.MainWindowCaption, true);
            if (mainWindow != IntPtr.Zero)
            {
               if (context.Arguments.Length > 1)
               {
                  string message = String.Join("|", context.Arguments);
                  Win32Tools.SendMessageToWindow(mainWindow, message);
               }
               Win32Tools.ActivateWindow(mainWindow);
            }
            else
            {
               // This may happen if a custom protocol link is quickly clicked more than once in a row

               Trace.TraceInformation(String.Format("Cannot find Main Window"));

               // bring to front any window
               IntPtr window = context.GetWindowByCaption(String.Empty, true);
               if (window != IntPtr.Zero)
               {
                  Win32Tools.ActivateWindow(window);
               }
               else
               {
                  Trace.TraceInformation(String.Format("Cannot find application windows"));
               }
            }
         }
         catch (Exception ex) // whatever unhandled exception
         {
            HandleUnhandledException(ex);
         }
      }

      private static void onLaunchFromDiffTool(LaunchContext context)
      {
         if (context.IsRunningSingleInstance)
         {
            Trace.TraceWarning("Merge Request Helper is not running");
            MessageBox.Show("Merge Request Helper is not running. Discussion cannot be created", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
         }

         IntPtr concurrentDiscussionWindow = context.GetWindowByCaption(
            mrHelper.Common.Constants.Constants.NewDiscussionCaption, false);
         if (concurrentDiscussionWindow != IntPtr.Zero)
         {
            Trace.TraceWarning("Found a concurrent Create New Discussion window");
            CommonTools.Win32Tools.ActivateWindow(concurrentDiscussionWindow);
            return;
         }

         int gitPID = -1;
         try
         {
            gitPID = getGitParentProcessId(context.CurrentProcess);
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot find parent git process");
         }

         if (gitPID == -1)
         {
            Trace.TraceError("Cannot find parent git process");
            MessageBox.Show(
               "Cannot find parent git process. Discussion cannot be created. Is Merge Request Helper running?",
               "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
         }

         string[] argumentsEx = new string[context.Arguments.Length + 1];
         Array.Copy(context.Arguments, 0, argumentsEx, 0, context.Arguments.Length);
         argumentsEx[argumentsEx.Length - 1] = gitPID.ToString();

         string message = String.Join("|", argumentsEx);
         IntPtr mainWindow = context.GetWindowByCaption(
            mrHelper.Common.Constants.Constants.MainWindowCaption, true);
         if (mainWindow == IntPtr.Zero)
         {
            Debug.Assert(false);

            Trace.TraceWarning("Cannot find Main Window");
            return;
         }

         Win32Tools.SendMessageToWindow(mainWindow, message);
      }

      private static bool checkArguments(LaunchContext context)
      {
         if (context.Arguments.Length > 2)
         {
            if (context.Arguments[1] == "diff")
            {
               return true;
            }
            else
            {
               string arguments = String.Join(" ", context.Arguments);
               Trace.TraceError(String.Format("Invalid arguments {0}", arguments));
               MessageBox.Show("Invalid arguments", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return false;
            }
         }
         else if (context.Arguments.Length == 2)
         {
            Match m = url_re.Match(context.Arguments[1]);
            if (!m.Success)
            {
               Trace.TraceError(String.Format("Invalid URL {0}", context.Arguments[1]));
               MessageBox.Show("Invalid URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return false;
            }
         }
         return true;
      }

      /// <summary>
      /// Traverse process tree up to a process with the same name as the current process.
      /// Return process id of `git` process that is a child of a found process and parent of the current one.
      /// </summary>
      private static int getGitParentProcessId(Process currentProcess)
      {
         Process previousParent = null;
         Process parent = ParentProcessUtilities.GetParentProcess(currentProcess);

         while (parent != null && parent.ProcessName != currentProcess.ProcessName)
         {
            previousParent = parent;
            parent = ParentProcessUtilities.GetParentProcess(parent);
         }

         if (previousParent == null || previousParent.ProcessName != "git")
         {
            return -1;
         }

         return previousParent.Id;
      }

      private static string getFullLogPath()
      {
         string logFolderName = "mrHelper";
         string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
         return System.IO.Path.Combine(appData, logFolderName);
      }

      private static string getLogFileName(LaunchContext context)
      {
         bool isMainInstance = context.IsRunningSingleInstance;

         string format = "yyyy_MM_dd_HHmmss";

         string filenamePrefix = isMainInstance ? "mrhelper.main" : "mrhelper.second.instance";
         string filenameSuffix = isMainInstance ? String.Empty : ".id" + context.CurrentProcess.Id.ToString();
         string filename = String.Format("{0}.{1}{2}.log", filenamePrefix, DateTime.Now.ToString(format), filenameSuffix);

         return Path.Combine(getFullLogPath(), filename);
      }

      private static void cleanupLogFiles()
      {
         string path = getFullLogPath();

         // erase old style log files
         File.Delete(Path.Combine(path, "mrHelper.main.log"));
         Directory.GetFiles(path, "mrHelper.secondary.*.log").ToList().ForEach(
            x => File.Delete(Path.Combine(path, x)));

         // erase all log files except N most recent ones
         string[] logfilemasks = { "mrHelper.main.*.log", "mrHelper.second.instance.*.log" };
         foreach (string mask in logfilemasks)
         {
            string[] files = Directory.GetFiles(path, mask);
            Array.Sort(files);

            int logFilesToKeep = 10;
            files.Except(files.Reverse().Take(logFilesToKeep)).ToList().ForEach(
               x => File.Delete(Path.Combine(path, x)));
         }
      }
   }
}

