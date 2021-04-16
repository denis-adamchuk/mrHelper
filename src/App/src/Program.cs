using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.App.Forms;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.CommonNative;
using System.Collections.Generic;
using mrHelper.Integration.GitUI;
using mrHelper.Integration.DiffTool;
using mrHelper.Integration.CustomProtocol;

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

      internal static UserDefinedSettings Settings;
      internal static ServiceManager ServiceManager;
      internal static FeedbackReporter FeedbackReporter;

      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
      [STAThread]
      private static void Main()
      {
         try
         {
            Settings = new UserDefinedSettings();
         }
         catch (CorruptedSettingsException ex)
         {
            string message = String.Format(
               "Cannot launch mrHelper because application configuration file at \"{0}\" could not be loaded. " +
               "It's probably corrupted. Try deleting the file or contact developers.", ex.ConfigFilePath);
            MessageBox.Show(message, "Fatal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }

         using (LaunchContext context = new LaunchContext())
         {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);

            try
            {
               initializeGitLabSharpLibrary();

               string currentLogFileName = getLogFileName(context);
               CustomTraceListener listener = createTraceListener(currentLogFileName);
               if (listener == null)
               {
                  // Cannot do anything good here
                  return;
               }

               Directory.SetCurrentDirectory(Path.GetDirectoryName(context.CurrentProcess.MainModule.FileName));
               ServiceManager = new ServiceManager();
               createFeedbackReporter(currentLogFileName, listener);

               launchFromContext(context);
            }
            catch (Exception ex) // whatever unhandled exception
            {
               HandleUnhandledException(ex);
            }
         }
      }

      private static void initializeGitLabSharpLibrary()
      {
         GitLabSharp.LibraryContext context = new GitLabSharp.LibraryContext(Settings.ServicePointConnectionLimit);
         GitLabSharp.GitLabSharp.Initialize(context);
      }

      private static CustomTraceListener createTraceListener(string currentLogFileName)
      {
         CustomTraceListener listener;
         try
         {
            listener = new CustomTraceListener(currentLogFileName,
              String.Format("Merge Request Helper {0} started. PID {1}",
                 Application.ProductVersion, Process.GetCurrentProcess().Id));
            Trace.Listeners.Add(listener);
         }
         catch (ArgumentException)
         {
            listener = null;
         }

         return listener;
      }

      private static void launchFromContext(LaunchContext context)
      {
         LaunchOptions options = LaunchOptions.FromContext(context);
         switch (options.Mode)
         {
            case LaunchOptions.LaunchMode.DiffTool:
               onLaunchFromDiffTool(options);
               break;

            case LaunchOptions.LaunchMode.Register:
               if (registerCustomProtocol())
               {
                  integrateInGitExtensions();
                  integrateInSourceTree();
               }
               break;

            case LaunchOptions.LaunchMode.Unregister:
               unregisterCustomProtocol();
               break;

            case LaunchOptions.LaunchMode.Normal:
               if (context.IsRunningSingleInstance)
               {
                  onLaunchMainInstance(options);
               }
               else
               {
                  onLaunchAnotherInstance(context);
               }
               break;

            default:
               Debug.Assert(false);
               break;
         }
      }

      private static void createFeedbackReporter(string currentLogFileName, CustomTraceListener listener)
      {
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
      }

      private static void onLaunchMainInstance(LaunchOptions options)
      {
         cleanUpOldFiles();
         if (ApplicationUpdateHelper.ShowCheckForUpdatesDialog())
         {
            Trace.TraceInformation("Application is exiting to install a new version");
            return;
         }

         bool runningAsUwp = new DesktopBridge.Helpers().IsRunningAsUwp();
         Trace.TraceInformation(String.Format("Running as UWP = {0}", runningAsUwp ? "Yes" : "No"));
         if (runningAsUwp)
         {
            revertOldInstallations();
         }

         string path = runningAsUwp ? Constants.UWP_Launcher_Name : Process.GetCurrentProcess().MainModule.FileName;
         if (!prepareGitEnvironment() || !integrateInDiffTool(path))
         {
            return;
         }

         bool integratedInGitExtensions = integrateInGitExtensions();
         bool integratedInSourceTree = integrateInSourceTree();
         Version osVersion = Environment.OSVersion.Version;
         Trace.TraceInformation(String.Format("OS version is {0}", osVersion.ToString()));
         Version clrVersion = Environment.Version;
         Trace.TraceInformation(String.Format("CLR version is {0}", clrVersion.ToString()));
         Trace.TraceInformation(String.Format(".NET Framework version is {0}", typeof(string).Assembly.ImageRuntimeVersion));

         LaunchOptions.NormalModeOptions normalOptions = options.SpecialOptions as LaunchOptions.NormalModeOptions;
         Application.Run(new MainForm(normalOptions.StartMinimized, runningAsUwp, normalOptions.StartUrl,
            integratedInGitExtensions, integratedInSourceTree));
      }

      private static void onLaunchAnotherInstance(LaunchContext context)
      {
         IntPtr mainWindow = context.GetWindowByCaption(Constants.MainWindowCaption, true);
         if (mainWindow != IntPtr.Zero)
         {
            if (context.Arguments.Length > 1)
            {
               string message = String.Join("|", context.Arguments.Skip(1)); // skip executable path
               Win32Tools.SendMessageToWindow(mainWindow, message);
            }
            Win32Tools.SendMessageToWindow(mainWindow, "show");
         }
         else
         {
            // This may happen if a custom protocol link is quickly clicked more than once in a row.
            // In the scope of #453 decided that it is simpler to not handle this case at all.
         }
      }

      private static void onLaunchFromDiffTool(LaunchOptions launchOptions)
      {
         LaunchContext context = (launchOptions.SpecialOptions as LaunchOptions.DiffToolModeOptions).LaunchContext;
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
            string diffToolName = Path.GetFileNameWithoutExtension(createDiffTool().GetToolCommand());
            StorageSupport.LocalCommitStorageType type = ConfigurationHelper.GetPreferredStorageType(Settings);
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

         string message = String.Join("|", argumentsEx.Skip(1)); // skip executable path
         IntPtr mainWindow = context.GetWindowByCaption(Constants.MainWindowCaption, true);
         if (mainWindow == IntPtr.Zero)
         {
            Debug.Assert(false);
            Trace.TraceWarning("Cannot find Main Window");
            return;
         }

         Win32Tools.SendMessageToWindow(mainWindow, message);
      }

      private static bool prepareGitEnvironment()
      {
         if (!GitTools.IsGit2Installed())
         {
            MessageBox.Show(
               "Git for Windows (version 2) is not installed. "
             + "It must be installed at least for the current user. Application cannot start.",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
         }

         string pathEV = System.Environment.GetEnvironmentVariable("PATH");
         System.Environment.SetEnvironmentVariable("PATH", pathEV + ";" + GitTools.GetBinaryFolder());
         Trace.TraceInformation(String.Format("Updated PATH variable: {0}",
            System.Environment.GetEnvironmentVariable("PATH")));
         System.Environment.SetEnvironmentVariable("GIT_TERMINAL_PROMPT", "0");
         Trace.TraceInformation("Set GIT_TERMINAL_PROMPT=0");
         Trace.TraceInformation(String.Format("TEMP variable: {0}",
            System.Environment.GetEnvironmentVariable("TEMP")));
         return true;
      }

      private static bool integrateInDiffTool(string applicationFullPath)
      {
         IIntegratedDiffTool diffTool = createDiffTool();
         DiffToolIntegration integration = new DiffToolIntegration();

         try
         {
            integration.Integrate(diffTool, applicationFullPath);
         }
         catch (Exception ex)
         {
            if (ex is DiffToolNotInstalledException)
            {
               string message = String.Format(
                  "{0} is not installed. It must be installed at least for the current user. Application cannot start",
                  diffTool.GetToolName());
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
               string message = String.Format("{0} integration failed. Application cannot start. See logs for details",
                  diffTool.GetToolName());
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               ExceptionHandlers.Handle(String.Format("Cannot integrate \"{0}\"", diffTool.GetToolName()), ex);
            }
            return false;
         }
         finally
         {
            GitTools.TraceGitConfiguration();
         }

         return true;
      }

      private static void revertOldInstallations()
      {
         string defaultInstallLocation = StringUtils.GetDefaultInstallLocation(
            Windows.ApplicationModel.Package.Current.PublisherDisplayName);
         AppFinder.AppInfo appInfo = AppFinder.GetApplicationInfo(new string[] { "mrHelper" });
         if (appInfo != null
          || Directory.Exists(defaultInstallLocation)
          || System.IO.File.Exists(StringUtils.GetShortcutFilePath()))
         {
            MessageBox.Show("mrHelper needs to uninstall an old version of itself on this launch. "
              + "It takes a few seconds, please wait...", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

            string currentPackagePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            string revertMsiProjectFolder = "mrHelper.RevertMSI";
            string revertMsiProjectName = "mrHelper.RevertMSI.exe";
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
               FileName = System.IO.Path.Combine(currentPackagePath, revertMsiProjectFolder, revertMsiProjectName),
               WorkingDirectory = System.IO.Path.Combine(currentPackagePath, revertMsiProjectFolder),
               Verb = "runas", // revert implies work with registry
            };
            Process p = Process.Start(startInfo);
            p.WaitForExit();
            Trace.TraceInformation(String.Format("{0} exited with code {1}", revertMsiProjectName, p.ExitCode));
         }
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

      private static void cleanUpTempFolder(string template)
      {
         string tempFolder = Environment.GetEnvironmentVariable("TEMP");
         foreach (string f in System.IO.Directory.EnumerateFiles(tempFolder, template))
         {
            try
            {
               System.IO.File.Delete(f);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle(String.Format("Cannot delete file \"{0}\"", f), ex);
            }
         }
      }

      private static void cleanUpOldFiles()
      {
         try
         {
            cleanupLogFiles(Settings);
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle("Failed to clean-up log files", ex);
         }

         cleanUpTempFolder("mrHelper.*.msi");
         cleanUpTempFolder("mrHelper.*.msix");
         cleanUpTempFolder("mrHelper.logs.*.zip");
      }

      static private bool registerCustomProtocol()
      {
         string binaryFilePath = Process.GetCurrentProcess().MainModule.FileName;
         string defaultIconString = String.Format("\"{0}\", 0", binaryFilePath);
         string commandString = String.Format("\"{0}\" \"%1\"", binaryFilePath);

         const string ProtocolDescription = "Merge Request Helper for GitLab link protocol";
         CustomProtocol protocol = new CustomProtocol(Constants.CustomProtocolName);
         Dictionary<string, string> commands = new Dictionary<string, string> { { "open", commandString } };
         try
         {
            protocol.RegisterInRegistry(ProtocolDescription, commands, defaultIconString);
         }
         catch (UnauthorizedAccessException ex)
         {
            MessageBox.Show("Operation failed. Run mrHelper as Administrator to register or unregister Custom Protocol.",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ExceptionHandlers.Handle("Cannot register custom protocol", ex);
            return false;
         }
         return true;
      }

      static private bool unregisterCustomProtocol()
      {
         try
         {
            new CustomProtocol(Constants.CustomProtocolName).RemoveFromRegistry();
         }
         catch (UnauthorizedAccessException ex)
         {
            MessageBox.Show("Operation failed. Run mrHelper as Administrator to register or unregister Custom Protocol.",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ExceptionHandlers.Handle("Cannot register custom protocol", ex);
            return false;
         }
         return true;
      }

      static private bool integrateInGitExtensions()
      {
         string scriptPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

         try
         {
            GitExtensionsIntegrationHelper.AddCustomActions(scriptPath);
         }
         catch (GitExtensionsIntegrationHelperException ex)
         {
            ExceptionHandlers.Handle("Cannot integrate mrHelper in Git Extensions", ex);
            return false;
         }
         return true;
      }

      static private bool integrateInSourceTree()
      {
         string scriptPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

         try
         {
            SourceTreeIntegrationHelper.AddCustomActions(scriptPath);
         }
         catch (SourceTreeIntegrationHelperException ex)
         {
            ExceptionHandlers.Handle("Cannot integrate mrHelper in Source Tree", ex);
            return false;
         }
         return true;
      }

      static private IIntegratedDiffTool createDiffTool()
      {
         return new BC3Tool();
      }
   }
}

