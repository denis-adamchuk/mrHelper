using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
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
         void trace(Exception exception)
         {
            if (exception != null)
            {
               Trace.TraceError("Unhandled exception (nested): [{0}] {1}\nCallstack:\n{2}",
                  exception.GetType().ToString(), exception.Message, exception.StackTrace);
            }
         }

         AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
         Debug.Assert(false);
         trace(ex);
         trace(ex.InnerException);
         if (ServiceManager != null && FeedbackReporter != null)
         {
            if (MessageBox.Show("Fatal error occurred, see details in logs. Do you want to report this problem?",
               "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
            {
               try
               {
                  Program.FeedbackReporter.SendEMail("Merge Request Helper error report",
                     "Please provide some details about the problem here",
                     Program.ServiceManager.GetBugReportEmail(),
                     Constants.BugReportLogArchiveName,
                     Constants.BugReportDumpArchiveName);
               }
               catch (FeedbackReporterException ex2)
               {
                  ExceptionHandlers.Handle("Cannot send a bug report", ex2);
               }
            }
         }
      }

      internal static UserDefinedSettings Settings;
      internal static ServiceManager ServiceManager;
      internal static FeedbackReporter FeedbackReporter;

      [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
      [STAThread]
      private static void Main()
      {
         Common.Tools.HtmlUtils.Test_AddWidthAttributeToCodeElements();
         Common.Tools.HtmlUtils.Test_CalcWidthAttributeToCodeElements();
         Common.Tools.HtmlUtils.Test_WrapImageIntoTables();

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
            adjustCultureInfo();

            // This should redirect exceptions from UI events to the global try/catch
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

            // Handle exceptions from MainForm.OnLoad() etc (not events)
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

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
            catch (Exception ex) // Any unhandled exception, including CSE
            {
               HandleUnhandledException(ex);
               throw; // pass exception to WER to have a dump
            }
         }
      }

      [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
      private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
      {
         HandleUnhandledException((Exception)e.ExceptionObject);

         // and then - exception is caught by WER and we have a dump
      }

      private static void initializeGitLabSharpLibrary()
      {
         GitLabSharp.LibraryContext context = new GitLabSharp.LibraryContext(
            Settings.ServicePointConnectionLimit, Settings.AsyncOperationTimeOutSeconds);
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
               catch (ArgumentException)
               {
                  // Cannot do anything good here
               }
            },
         getApplicationDataPath(),
         PathFinder.DumpStorage);
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
            string toolCommand = getDiffToolCommand();
            if (toolCommand == null)
            {
               Trace.TraceError("Cannot obtain diff tool command");
               return;
            }
            string diffToolName = Path.GetFileNameWithoutExtension(toolCommand);
            StorageSupport.LocalCommitStorageType type = ConfigurationHelper.GetPreferredStorageType(Settings);
            string toolProcessName = type == StorageSupport.LocalCommitStorageType.FileStorage ? diffToolName : "git";
            parentToolPID = getParentProcessId(context.CurrentProcess, toolProcessName);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot obtain diff tool file name", ex);
         }
         catch (Win32Exception ex)
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
         string installPath = GitTools.GetInstallPath();
         if (!GitTools.IsGit2AvailableAtPath())
         {
            if (String.IsNullOrEmpty(installPath))
            {
               MessageBox.Show(
                  "Git for Windows (version 2) is not installed. "
                + "It must be installed at least for the current user. Application cannot start.",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return false;
            }

            string pathEV = System.Environment.GetEnvironmentVariable("PATH");
            System.Environment.SetEnvironmentVariable("PATH", pathEV + ";" + installPath);
            Debug.Assert(GitTools.IsGit2AvailableAtPath());
         }

         Trace.TraceInformation(String.Format("git install path: {0}", installPath));
         Trace.TraceInformation(String.Format("git binary path: {0}", whereIsFile("git")));
         Trace.TraceInformation(String.Format("git bash path: {0}", GitTools.GetGitBashPath()));
         System.Environment.SetEnvironmentVariable("GIT_TERMINAL_PROMPT", "0");
         Trace.TraceInformation("Set GIT_TERMINAL_PROMPT=0");
         Trace.TraceInformation(String.Format("PATH variable: {0}",
            System.Environment.GetEnvironmentVariable("PATH")));
         Trace.TraceInformation(String.Format("TEMP variable: {0}",
            System.Environment.GetEnvironmentVariable("TEMP")));
         return true;
      }

      private static string whereIsFile(string filename)
      {
         string utilityName = "where";
         try
         {
            IEnumerable<string> stdOut = ExternalProcess.Start(utilityName, filename, true, String.Empty).StdOut;
            if (stdOut.Any())
            {
               return stdOut.First();
            }
         }
         catch (Common.Exceptions.ExternalProcessException ex)
         {
            string msg = String.Format("Cannot determine filename \"{0}\" location using \"{1}\" utility",
               filename, utilityName);
            ExceptionHandlers.Handle(msg, ex);
         }
         return String.Empty;
      }

      private static bool integrateInDiffTool(string applicationFullPath)
      {
         try
         {
            if (_toolDescriptions.Any(d => { return integrateInTool(d, applicationFullPath); }))
            {
               return true; 
            }
            reportNotInstalledDiffTool();
            return false;
         }
         finally
         {
            GitTools.TraceGitConfiguration();
         }
      }

      private static void reportNotInstalledDiffTool()
      {
         string message =
            "None of supported diff tools is installed. Application cannot start. Do you want to report this problem?";
         if (MessageBox.Show(message, "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
         {
            try
            {
               Program.FeedbackReporter.SendEMail("Merge Request Helper error report",
                  "Application cannot start because non of supported diff tools is installed",
                  Program.ServiceManager.GetBugReportEmail(),
                  Constants.BugReportLogArchiveName,
                  Constants.BugReportDumpArchiveName);
            }
            catch (FeedbackReporterException ex)
            {
               ExceptionHandlers.Handle("Cannot send a bug report", ex);
            }
         }
      }

      private class ToolDescription
      {
         internal Type Type;
         internal string Name;
         internal string Command;
         internal bool IsPortable;

         public ToolDescription(Type type, string name, string command, bool isPortable)
         {
            Type = type;
            Name = name;
            Command = command;
            IsPortable = isPortable;
         }
      }

      private static List<ToolDescription> _toolDescriptions = new List<ToolDescription> {
         new ToolDescription(typeof(BC5PortableTool), BC5PortableTool.Name, BC5PortableTool.Command, true),
         new ToolDescription(typeof(BC4PortableTool), BC4PortableTool.Name, BC4PortableTool.Command, true),
         new ToolDescription(typeof(BC5Tool), BC5Tool.Name, BC5Tool.Command, false),
         new ToolDescription(typeof(BC4Tool), BC4Tool.Name, BC4Tool.Command, false),
         new ToolDescription(typeof(BC3Tool), BC3Tool.Name, BC3Tool.Command, false)
      };

      private static bool integrateInTool(ToolDescription description, string applicationFullPath)
      {
         string diffToolName = Program.Settings.DiffToolName;
         string diffToolPath = Program.Settings.DiffToolPath;
         if (description.IsPortable)
         {
            // Portable tool name and path can only be specified by user manually in config file,
            if (diffToolName == description.Name &&
                  !String.IsNullOrWhiteSpace(diffToolPath) && System.IO.Directory.Exists(diffToolPath))
            {
               IIntegratedDiffTool diffTool =
                  (IIntegratedDiffTool)Activator.CreateInstance(description.Type, diffToolPath);
               return doIntegrateInDiffTool(diffTool, applicationFullPath);
            }
            return false;
         }
         else
         {
            // Non-portable version will be integrated automatically and order of ToolDescriptions
            // is important to integrate the newest version from all available ones.
            IIntegratedDiffTool diffTool = (IIntegratedDiffTool)Activator.CreateInstance(description.Type);
            if (doIntegrateInDiffTool(diffTool, applicationFullPath))
            {
               Program.Settings.DiffToolName = description.Name;
               Program.Settings.DiffToolPath = String.Empty;
               return true;
            }
         }
         return false;
      }

      private static bool doIntegrateInDiffTool(IIntegratedDiffTool diffTool, string applicationFullPath)
      {
         DiffToolIntegration integration = new DiffToolIntegration();

         try
         {
            integration.Integrate(diffTool, applicationFullPath);
         }
         catch (DiffToolNotInstalledException ex)
         {
            ExceptionHandlers.Handle(String.Format("Cannot integrate \"{0}\"", diffTool.GetToolName()), ex);
            return false;
         }
         catch (DiffToolIntegrationException ex)
         {
            ExceptionHandlers.Handle(String.Format("Cannot integrate \"{0}\"", diffTool.GetToolName()), ex);
            return false;
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

      private static void cleanUpTempFolder(string tempFolder, string template)
      {
         if (!System.IO.Directory.Exists(tempFolder))
         {
            return;
         }

         foreach (string f in System.IO.Directory.EnumerateFiles(tempFolder, template))
         {
            try
            {
               System.IO.File.Delete(f);
            }
            catch (Exception ex) // Any exception from System.IO.File.Delete()
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
         catch (Exception ex) // Any exception on I/O operations
         {
            ExceptionHandlers.Handle("Failed to clean-up log files", ex);
         }

         cleanUpTempFolder(PathFinder.InstallerStorage, "mrHelper.*.msi");
         cleanUpTempFolder(PathFinder.InstallerStorage, "mrHelper.*.msix");
         cleanUpTempFolder(PathFinder.LogArchiveStorage, "mrHelper.logs.*.zip");
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

      static private string getDiffToolCommand()
      {
         ToolDescription tool = _toolDescriptions.FirstOrDefault(t => t.Name == Program.Settings.DiffToolName);
         if (tool == null)
         {
            Debug.Assert(false);
            return null;
         }
         return tool.Command;
      }

      static private void adjustCultureInfo()
      {
         System.Globalization.CultureInfo customCultureInfo =
            (System.Globalization.CultureInfo)Application.CurrentCulture.Clone();

         string customSeparator = ".";
         customCultureInfo.NumberFormat.NumberDecimalSeparator = customSeparator;
         customCultureInfo.NumberFormat.PercentDecimalSeparator = customSeparator;
         customCultureInfo.NumberFormat.CurrencyDecimalSeparator = customSeparator;

         // Override Region Settings to format doubles properly across the application
         Application.CurrentCulture = customCultureInfo;
      }
   }
}

