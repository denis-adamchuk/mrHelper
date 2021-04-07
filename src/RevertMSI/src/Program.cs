using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;

namespace mrHelper.RevertMSI
{
   class Program
   {
      private static readonly string logfilename = "mrHelper.revertMSI.log";

      [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
      [STAThread]
      static void Main()
      {
         Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
         try
         {
            Trace.Listeners.Add(new CustomTraceListener(Path.Combine(getApplicationDataPath(), logfilename),
               String.Format("Merge Request Helper Revert MSI Tool {0} started. PID {1}",
                  Application.ProductVersion, Process.GetCurrentProcess().Id)));
         }
         catch (ArgumentException)
         {
            return;
         }

         DesktopBridge.Helpers helpers = new DesktopBridge.Helpers();
         if (!helpers.IsRunningAsUwp())
         {
            return;
         }

         try
         {
            revert();
         }
         catch (Exception ex) // Any unhandled exception, including CSE
         {
            HandleUnhandledException(ex);
         }
      }

      private static string getApplicationDataPath()
      {
         string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
         return System.IO.Path.Combine(appData, Constants.ApplicationDataFolderName);
      }

      private static void HandleUnhandledException(Exception ex)
      {
         MessageBox.Show("Fatal error occurred on attempt to revert Merge Request Helper, see details in logs",
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         Trace.TraceError("Unhandled exception: {0}\nCallstack:\n{1}", ex.Message, ex.StackTrace);
         Application.Exit();
      }

      private static void revert()
      {
         AppFinder.AppInfo appInfo = AppFinder.GetApplicationInfo(new string[] { "mrHelper" });
         if (appInfo != null)
         {
            uninstall(appInfo.ProductCode);
         }

         string defaultInstallLocation = StringUtils.GetDefaultInstallLocation(
            Windows.ApplicationModel.Package.Current.PublisherDisplayName);
         cleanupBinaries(appInfo == null ? defaultInstallLocation : appInfo.InstallPath);

         cleanupShortcut(StringUtils.GetShortcutFilePath());

         removeProtocolFromRegistry();
      }

      private static void uninstall(string productCode)
      {
         string msiExecProcessName = "msiexec";
         string arguments = String.Format("-quiet -x {0}", productCode);
         Process msiExecProcess = Process.Start(msiExecProcessName, arguments);
         msiExecProcess.WaitForExit();
         if (msiExecProcess.ExitCode != 0)
         {
            Trace.TraceWarning(String.Format("{0} exited with code {1}",
               msiExecProcessName, msiExecProcess.ExitCode));
         }
      }

      private static void cleanupBinaries(string installLocation)
      {
         if (String.IsNullOrWhiteSpace(installLocation) || !Directory.Exists(installLocation))
         {
            return;
         }

         try
         {
            IEnumerable<string> files = System.IO.Directory.EnumerateFiles(installLocation);
            foreach (string file in files)
            {
               System.IO.File.Delete(System.IO.Path.Combine(installLocation, file));
            }
            System.IO.Directory.Delete(installLocation);
         }
         catch (Exception ex) // Any exception from System.IO.Directory.Delete()
         {
            Trace.TraceError(String.Format(
               "Could not delete clean-up installation folder. Exception message: {0}\nCallstack:\n{1}",
               ex.Message, ex.StackTrace));
         }
      }

      private static void cleanupShortcut(string shortcutFilePath)
      {
         if (!File.Exists(shortcutFilePath))
         {
            return;
         }

         try
         {
            System.IO.File.Delete(shortcutFilePath);
         }
         catch (Exception ex) // Any exception from System.IO.Directory.Delete()
         {
            Trace.TraceError(String.Format(
               "Could not delete shortcut. Exception message: {0}\nCallstack:\n{1}",
               ex.Message, ex.StackTrace));
         }
      }

      private static void removeProtocolFromRegistry()
      {
         string currentPackagePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
         string integrationProjectFolderName = "mrHelper.Integration";
         string integrationExecutableName = "mrHelper.Integration.exe";

         ProcessStartInfo startInfo = new ProcessStartInfo
         {
            FileName = System.IO.Path.Combine(currentPackagePath, integrationProjectFolderName, integrationExecutableName),
            WorkingDirectory = System.IO.Path.Combine(currentPackagePath, integrationProjectFolderName),
            Arguments = "-x",
            Verb = "runas", // revert implies work with registry
         };

         Process integrationProcess = Process.Start(startInfo);
         integrationProcess.WaitForExit();

         if (integrationProcess.ExitCode != 0)
         {
            Trace.TraceWarning(String.Format("{0} exited with code {1}",
               integrationExecutableName, integrationProcess.ExitCode));
         }
      }
   }
}

