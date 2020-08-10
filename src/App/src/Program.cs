using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using mrHelper.App.Forms;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.CommonNative;

namespace mrHelper.App
{
   internal static class Program
   {
      private static void HandleUnhandledException(Exception ex)
      {
         Debug.Assert(false);
         Trace.TraceError("Unhandled exception: [{0}] {1}\nCallstack:\n{2}",
            ex.GetType().ToString(), ex.Message, ex.StackTrace);
         if (ServiceManager != null && FeedbackReporter != null)
         {
            if (MessageBox.Show("Fatal error occurred, see details in logs. Do you want to report this problem?",
               "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
            {
               try
               {
                  Program.FeedbackReporter.SendEMail("Merge Request Helper error report",
                     "Please provide some details about the problem here",
                     Program.ServiceManager.GetBugReportEmail(), Constants.BugReportLogArchiveName);
               }
               catch (FeedbackReporterException ex2)
               {
                  ExceptionHandlers.Handle("Cannot send a bug report", ex2);
               }
            }
         }
         Application.Exit();
      }

      internal static UserDefinedSettings Settings = new UserDefinedSettings();
      internal static ServiceManager ServiceManager;
      internal static FeedbackReporter FeedbackReporter;

      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      private static void Main()
      {
         using (LaunchContext context = new LaunchContext())
         {
            Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);

            string currentLogFileName = getLogFileName(context);
            CustomTraceListener listener = null;
            try
            {
                listener = new CustomTraceListener(currentLogFileName,
                  String.Format("Merge Request Helper {0} started. PID {1}",
                     Application.ProductVersion, Process.GetCurrentProcess().Id));
               Trace.Listeners.Add(listener);
            }
            catch (ArgumentException)
            {
               // Cannot do anything good here
               return;
            }

            Directory.SetCurrentDirectory(Path.GetDirectoryName(context.CurrentProcess.MainModule.FileName));
            ServiceManager = new ServiceManager();

            FeedbackReporter = new FeedbackReporter(
               () =>
            {
               listener.Flush();
               listener.Close();
               Trace.Listeners.Remove(listener);
            },
               () =>
            {
               try
               {
                  listener = new CustomTraceListener(currentLogFileName, null);
                  Trace.Listeners.Add(listener);
               }
               catch (Exception)
               {
                  // Cannot do anything good here
               }
            },
            getApplicationDataPath());

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
                  cleanupLogFiles(Settings);
               }
               catch (Exception ex)
               {
                  ExceptionHandlers.Handle("Failed to clean-up log files", ex);
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

            Version currentVersion = Environment.OSVersion.Version;
            Trace.TraceInformation(String.Format("OS Version is {0}", currentVersion.ToString()));

            bool startMinimized = context.Arguments.Length == 2 && context.Arguments[1] == "-m";
            Application.Run(new MainForm(startMinimized));
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

            IntPtr mainWindow = context.GetWindowByCaption(Constants.MainWindowCaption, true);
            if (mainWindow != IntPtr.Zero)
            {
               if (context.Arguments.Length > 1)
               {
                  string message = String.Join("|", context.Arguments);
                  Win32Tools.SendMessageToWindow(mainWindow, message);
               }
               Win32Tools.ForceWindowIntoForeground(mainWindow);
            }
            else
            {
               // This may happen if a custom protocol link is quickly clicked more than once in a row

               Trace.TraceInformation(String.Format("Cannot find Main Window"));

               // bring to front any window
               IntPtr window = context.GetWindowByCaption(String.Empty, true);
               if (window != IntPtr.Zero)
               {
                  Win32Tools.ForceWindowIntoForeground(window);
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

         IntPtr concurrentDiscussionWindow = context.GetWindowByCaption(Constants.StartNewThreadCaption, false);
         if (concurrentDiscussionWindow != IntPtr.Zero)
         {
            Trace.TraceWarning(String.Format("Found a concurrent {0} window", Constants.StartNewThreadCaption));
            Win32Tools.ForceWindowIntoForeground(concurrentDiscussionWindow);
            return;
         }

         int parentToolPID = -1;
         try
         {
            // TODO Don't create BC3Tool explicitly here and inside MainForm, use a factory
            string diffToolName = System.IO.Path.GetFileNameWithoutExtension(new DiffTool.BC3Tool().GetToolCommand());
            StorageSupport.LocalCommitStorageType type = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
            string toolProcessName = type == StorageSupport.LocalCommitStorageType.FileStorage ? diffToolName : "git";
            parentToolPID = getParentProcessId(context.CurrentProcess, toolProcessName);
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle("Cannot find parent diff tool process", ex);
         }

         if (parentToolPID == -1)
         {
            Trace.TraceError("Cannot find parent diff tool process");
            MessageBox.Show(
               "Cannot find parent diff tool process. Discussion cannot be created. Is Merge Request Helper running?",
               "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
         }

         string[] argumentsEx = new string[context.Arguments.Length + 1];
         Array.Copy(context.Arguments, 0, argumentsEx, 0, context.Arguments.Length);
         argumentsEx[argumentsEx.Length - 1] = parentToolPID.ToString();

         string message = String.Join("|", argumentsEx);
         IntPtr mainWindow = context.GetWindowByCaption(Constants.MainWindowCaption, true);
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

            string arguments = String.Join(" ", context.Arguments);
            Trace.TraceError(String.Format("Invalid arguments {0}", arguments));
            MessageBox.Show("Invalid arguments", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
         }
         else if (context.Arguments.Length == 2)
         {
            if (context.Arguments[1] == "-m")
            {
               return true;
            }

            if (!context.Arguments[1].StartsWith(Constants.CustomProtocolName + "://"))
            {
               string message = String.Format("Unsupported protocol found in URL {0}", context.Arguments[1]);
               Trace.TraceError(message);
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return false;
            }
         }
         return true;
      }

      /// <summary>
      /// Traverse process tree up to a process with the same name as the current process.
      /// Return process id of a process with a given name that is a child of a found process and parent of the current one.
      /// </summary>
      private static int getParentProcessId(Process currentProcess, string parentProcessName)
      {
         Process previousParent = null;
         Process parent = ParentProcessUtilities.GetParentProcess(currentProcess);

         while (parent != null && parent.ProcessName != currentProcess.ProcessName)
         {
            previousParent = parent;
            parent = ParentProcessUtilities.GetParentProcess(parent);
         }

         if (previousParent == null || previousParent.ProcessName != parentProcessName)
         {
            return -1;
         }

         return previousParent.Id;
      }

      private static string getApplicationDataPath()
      {
         string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
         return System.IO.Path.Combine(appData, Constants.ApplicationDataFolderName);
      }

      private static string getLogFileName(LaunchContext context)
      {
         bool isMainInstance = context.IsRunningSingleInstance;

         string filenamePrefix = isMainInstance ? "mrhelper.main" : "mrhelper.second.instance";
         string filenameSuffix = isMainInstance ? String.Empty : ".id" + context.CurrentProcess.Id.ToString();
         string filename = String.Format("{0}.{1}{2}.log", filenamePrefix,
            DateTime.Now.ToString(Constants.TimeStampLogFilenameFormat), filenameSuffix);

         return Path.Combine(getApplicationDataPath(), filename);
      }

      private static void cleanupLogFiles(UserDefinedSettings settings)
      {
         string path = getApplicationDataPath();
         if (!Directory.Exists(path))
         {
            return;
         }

         // erase old style log files
         string oldStyleLogPath = Path.Combine(path, "mrHelper.main.log");
         if (File.Exists(oldStyleLogPath))
         {
            File.Delete(oldStyleLogPath);
         }

         foreach (string filename in Directory.GetFiles(path, "mrHelper.secondary.*.log"))
         {
            File.Delete(Path.Combine(path, filename));
         }

         // erase all log files except N most recent ones
         string[] logfilemasks = { "mrHelper.main.*.log", "mrHelper.second.instance.*.log" };
         foreach (string mask in logfilemasks)
         {
            string[] files = Directory.GetFiles(path, mask);
            Array.Sort(files);

            foreach (string filename in
               files
               .Except(
                  files
                  .Reverse().
                  Take(settings.LogFilesToKeep)))
            {
               File.Delete(Path.Combine(path, filename));
            }
         }
      }
   }
}

