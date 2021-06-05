using System;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.App.Forms;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Helpers
{
   internal static class ApplicationUpdateHelper
   {
      internal static bool InstallUpdate(string newVersionFilePath)
      {
         if (MessageBox.Show("Do you want to close the application and install a new version?", "Confirmation",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
         {
            return false;
         }

         launchInstallerWithoutUI(newVersionFilePath);
         return true;
      }

      internal static bool ShowCheckForUpdatesDialog()
      {
         Trace.TraceInformation("[ApplicationUpdateHelper] ShowCheckForUpdatesDialog()");
         using (CheckForUpdatesForm checkForUpdatesForm = new CheckForUpdatesForm())
         {
            return showDialogAndLaunchInstaller(checkForUpdatesForm);
         }
      }

      internal static bool RemindAboutAvailableVersion()
      {
         Trace.TraceInformation("[ApplicationUpdateHelper] RemindAboutAvailableVersion()");
         using (RemindAboutUpdateForm remindForm = new RemindAboutUpdateForm())
         {
            return showDialogAndLaunchInstaller(remindForm);
         }
      }

      private static bool showDialogAndLaunchInstaller(CheckForUpdatesForm form)
      {
         if (WinFormsHelpers.ShowDialogOnControl(form, WinFormsHelpers.FindMainForm()) != DialogResult.OK)
         {
            Trace.TraceInformation("[ApplicationUpdateHelper] User discarded to install a new version");
            return false;
         }
         Debug.Assert(!String.IsNullOrEmpty(form.NewVersionFilePath));
         launchInstallerWithoutUI(form.NewVersionFilePath);
         return true;
      }

      private static void launchInstallerWithoutUI(string newVersionFilePath)
      {
         Trace.TraceInformation(String.Format(
            "[ApplicationUpdateHelper] Launching installer without UI: \"{0}\"", newVersionFilePath));
         string msiExecArguments = String.Format("/i {0} {1}",
            newVersionFilePath, Constants.MsiExecSilentLaunchArguments);
         try
         {
            Process.Start(Constants.MsiExecName, msiExecArguments);
         }
         catch (Exception ex) // Any exception from Process.Start()
         {
            ExceptionHandlers.Handle("[ApplicationUpdateHelper] Cannot launch installer", ex);
         }
      }
   }
}

