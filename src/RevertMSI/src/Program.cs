using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using mrHelper.Common.Tools;

namespace mrHelper.RevertMSI
{
   class Program
   {
      private static readonly string logfilename = "mrHelper.revertMSI.log";

      static void Main(string[] args)
      {
         Application.ThreadException += (sender, e) => HandleUnhandledException(e.Exception);
         Trace.Listeners.Add(new CustomTraceListener(Path.Combine(getFullLogPath(), logfilename),
            String.Format("Merge Request Helper Revert MSI Tool {0} started. PID {1}",
               Application.ProductVersion, Process.GetCurrentProcess().Id)));

         DesktopBridge.Helpers helpers = new DesktopBridge.Helpers();
         if (!helpers.IsRunningAsUwp())
         {
            return;
         }

         try
         {
            revert();
         }
         catch (Exception ex)
         {
            HandleUnhandledException(ex);
         }
      }

      private static string getFullLogPath()
      {
         string logFolderName = "mrHelper";
         string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
         return System.IO.Path.Combine(appData, logFolderName);
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
         if (appInfo == null)
         {
            return;
         }

         uninstall(appInfo);
         cleanupBinaries(appInfo);
         removeProtocolFromRegistry();
      }

      private static void uninstall(AppFinder.AppInfo appInfo)
      {
         string msiExecProcessName = "msiexec";
         string arguments = String.Format("-quiet -x {0}", appInfo.ProductCode);
         Process msiExecProcess = Process.Start(msiExecProcessName, arguments);
         msiExecProcess.WaitForExit();
         if (msiExecProcess.ExitCode != 0)
         {
            Trace.TraceWarning(String.Format("{0} exited with code {1}",
               msiExecProcessName, msiExecProcess.ExitCode));
         }
      }

      private static void cleanupBinaries(AppFinder.AppInfo appInfo)
      {
         string applicationPath = appInfo.InstallPath;
         if (!String.IsNullOrWhiteSpace(applicationPath))
         {
            try
            {
               IEnumerable<string> files = System.IO.Directory.EnumerateFiles(applicationPath);
               foreach (string file in files)
               {
                  System.IO.File.Delete(System.IO.Path.Combine(applicationPath, file));
               }
               System.IO.Directory.Delete(applicationPath);
            }
            catch (Exception ex)
            {
               Trace.TraceError(String.Format(
                  "Could not delete clean-up installation folder. Exception message: {0}\nCallstack:\n{1}",
                  ex.Message, ex.StackTrace));
            }
         }
      }

      private static void removeProtocolFromRegistry()
      {
         string currentPackagePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
         string integrationProjectFolderName = "mrHelper.RevertMSI";
         string integrationExecutableName = "mrHelper.RevertMSI.exe";
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

