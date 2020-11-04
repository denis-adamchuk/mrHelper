using System;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.App.Forms;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;

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

         launchInstallerWithUI(newVersionFilePath);
         return true;
      }

      internal static bool ShowCheckForUpdatesDialog()
      {
         Trace.TraceInformation("[ApplicationUpdateHelper] ShowCheckForUpdatesDialog()");
         CheckForUpdatesForm checkForUpdatesForm = new CheckForUpdatesForm();
         return showDialogAndLaunchInstaller(checkForUpdatesForm);
      }

      internal static bool RemindAboutAvailableVersion()
      {
         Trace.TraceInformation("[ApplicationUpdateHelper] RemindAboutAvailableVersion()");
         RemindAboutUpdateForm remindForm = new RemindAboutUpdateForm();
         return showDialogAndLaunchInstaller(remindForm);
      }

      private static bool showDialogAndLaunchInstaller(CheckForUpdatesForm form)
      {
         if (form.ShowDialog() != DialogResult.OK)
         {
            Trace.TraceInformation("[ApplicationUpdateHelper] User discarded to install a new version");
            return false;
         }
         Debug.Assert(!String.IsNullOrEmpty(form.NewVersionFilePath));
         launchInstallerWithoutUI(form.NewVersionFilePath);
         return true;
      }

      private static void launchInstallerWithUI(string newVersionFilePath)
      {
         Trace.TraceInformation(String.Format(
            "[ApplicationUpdateHelper] Launching installer with UI: \"{0}\"", newVersionFilePath));
         try
         {
            Process.Start(newVersionFilePath);
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle("[ApplicationUpdateHelper] Cannot launch installer", ex);
         }
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
         catch (Exception ex)
         {
            ExceptionHandlers.Handle("[ApplicationUpdateHelper] Cannot launch installer", ex);
         }
      }
   }
}

