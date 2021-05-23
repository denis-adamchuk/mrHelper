using System;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.CommonNative;
using mrHelper.CommonControls.Tools;
using Microsoft.Win32;
using mrHelper.App.Controls;
using mrHelper.App.Helpers;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);
         Win32Tools.EnableCopyDataMessageHandling(this.Handle);
         initializeWork();
      }

      protected override void OnFormClosing(FormClosingEventArgs e)
      {
         base.OnFormClosing(e);

         Trace.TraceInformation(String.Format("[MainForm] Requested to close the Main Form. Reason: {0}",
            e.CloseReason.ToString()));

         if (e.CloseReason == CloseReason.ApplicationExitCall)
         {
            // abnormal exit
            return;
         }

         if (Program.Settings.MinimizeOnClose && !_exiting && e.CloseReason == CloseReason.UserClosing)
         {
            e.Cancel = true;
            onHideToTray();
            return;
         }

         saveSizeAndLocation();
         setExitingFlag();
         Hide();

         WinFormsHelpers.CloseAllFormsExceptOne("MainForm");

         finalizeWork();
      }

      protected override void OnResize(EventArgs e)
      {
         base.OnResize(e);
         if (this.WindowState != _prevWindowState)
         {
            onWindowStateChanged();
         }
      }

      protected override void OnResizeEnd(EventArgs e)
      {
         base.OnResizeEnd(e);
         getCurrentConnectionPage()?.StoreSplitterDistance();
      }

      private void notifyIcon_DoubleClick(object sender, EventArgs e)
      {
         onRestoreFromTray();
      }

      private void exitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         Trace.TraceInformation("[MainForm] User selected Exit in tray menu");
         doClose();
      }

      private void openFromClipboardMenuItem_Click(object sender, EventArgs e)
      {
         onRestoreFromTray();
         connectToUrlFromClipboard();
      }

      private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
      {
         if (e.Reason == SessionSwitchReason.SessionLock)
         {
            if (isTrackingTime())
            {
               stopTimeTrackingTimer();
               MessageBox.Show("mrHelper stopped time tracking because workstation was locked", "Warning",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
               Trace.TraceInformation("[MainForm] Time tracking stopped because workstation was locked");
            }
         }
      }

      private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
      {
         WinFormsHelpers.LogScreenResolution(this);
      }

      private void sendFeedbackToolStripMenuItem_Click(object sender, EventArgs e)
      {
         sendFeedback();
      }

      private void configureNotificationsToolStripMenuItem_Click(object sender, EventArgs e)
      {
         ConfigureNotificationsForm form = new ConfigureNotificationsForm(_keywords);
         form.ShowDialog();
      }

      private void configureHostsToolStripMenuItem_Click(object sender, EventArgs e)
      {
         ConfigureHostsForm form = new ConfigureHostsForm();
         if (form.ShowDialog() == DialogResult.OK && form.Changed)
         {
            Trace.TraceInformation("[MainForm] Reconnecting after workflow type change");
            reconnect();
         }
      }

      private void configureStorageToolStripMenuItem_Click(object sender, EventArgs e)
      {
         string oldPath = Program.Settings.LocalStorageFolder;
         ConfigureStorageForm form = new ConfigureStorageForm();
         if (form.ShowDialog() == DialogResult.OK && form.Changed)
         {
            string newPath = Program.Settings.LocalStorageFolder;
            if (newPath != oldPath)
            {
               Trace.TraceInformation("[MainForm] User decided to change file storage to {0}", newPath);
               MessageBox.Show("Storage folder is changed.\n Please restart Diff Tool if you have already launched it.",
                  "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

               addOperationRecord(String.Format("[MainForm] File storage path has changed to {0}", newPath));
            }

            Trace.TraceInformation("[MainForm] Reconnecting after storage settings change");
            reconnect();
         }
      }

      private void configureColorsToolStripMenuItem_Click(object sender, EventArgs e)
      {
         void onColorSchemeChanged(ColorScheme colorScheme)
         {
            _colorScheme = colorScheme;
            getConnectionPages()?
               .ToList()
               .ForEach(connectionPage => connectionPage.SetColorScheme(_colorScheme));
         };

         ConfigureColorsForm form = new ConfigureColorsForm(_iconCache,
            () =>
            {
               updateTrayAndTaskBar();
               getConnectionPages().ToList().ForEach(page => updateToolbarHostIcon(page.GetCurrentHostName()));
            },
            onColorSchemeChanged);
         form.ShowDialog();
      }

      private void showHelpToolStripMenuItem_Click(object sender, EventArgs e)
      {
         showHelp();
      }

      private void updateToolStripMenuItem_Click(object sender, EventArgs e)
      {
         upgradeApplicationByUserRequest();
      }

      private void radioButtonRevisionType_CheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(defaultRevisionTypeToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         applyRevisionTypeChange();
      }

      private void radioButtonMainWindowLayout_CheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(layoutToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         applyMainWindowLayoutChange();
      }

      private void radioButtonAutoSelectionMode_CheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(revisionAutoselectionModeToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         applyAutoSelectionModeChange();
      }

      private void radioButtonShowWarningsOnFileMismatchMode_CheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(showWarningsOnFileMismatchToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         applyShowWarningsOnFileMismatchChange();
      }

      private void checkBoxMinimizeOnClose_CheckedChanged(object sender, EventArgs e)
      {
         applyMinimizeOnCloseChange((sender as ToolStripMenuItem).Checked);
      }

      private void checkBoxWordWrapLongRows_CheckedChanged(object sender, EventArgs e)
      {
         applyWordWrapLongRows((sender as ToolStripMenuItem).Checked);
      }

      private void checkBoxRemindAboutAvailableNewVersion_CheckedChanged(object sender, EventArgs e)
      {
         applyRemindAboutAvailableNewVersionChange((sender as ToolStripMenuItem).Checked);
      }

      private void checkBoxNewDiscussionIsTopMostForm_CheckedChanged(object sender, EventArgs e)
      {
         applyNewDiscussionIsTopMostFormChange((sender as ToolStripMenuItem).Checked);
      }

      private void checkBoxRunWhenWindowsStarts_CheckedChanged(object sender, EventArgs e)
      {
         applyAutostartSettingChange((sender as ToolStripMenuItem).Checked);
      }

      private void checkBoxDisableSplitterRestrictions_CheckedChanged(object sender, EventArgs e)
      {
         applyDisableSplitterRestrictionsChange((sender as ToolStripMenuItem).Checked);
      }

      private void linkLabelAbortGitClone_Click(object sender, EventArgs e)
      {
         Debug.Assert(getCurrentConnectionPage().CanAbortClone());
         getCurrentConnectionPage().AbortClone();
      }

      private void tabControlHost_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (tabControlHost.SelectedTab == null || _exiting)
         {
            return;
         }

         onHostTabSelected();
      }

      private void ConnectionPage_CanTrackTimeChanged(ConnectionPage connectionPage)
      {
         onCanTrackTimeChanged(connectionPage);
      }

      private void ConnectionPage_CanAbortCloneChanged(ConnectionPage connectionPage)
      {
         onCanAbortCloneChanged(connectionPage);
      }

      private void ConnectionPage_CanSearchChanged(ConnectionPage connectionPage)
      {
         onCanSearchChanged(connectionPage);
      }

      private void ConnectionPage_CanDiffToolChanged(ConnectionPage connectionPage)
      {
         onCanDiffToolChanged(connectionPage);
      }

      private void ConnectionPage_CanDiscussionsChanged(ConnectionPage connectionPage)
      {
         onCanDiscussionsChanged(connectionPage);
      }

      private void ConnectionPage_CanNewThreadChanged(ConnectionPage connectionPage)
      {
         onCanNewThreadChanged(connectionPage);
      }

      private void ConnectionPage_CanAddCommentChanged(ConnectionPage connectionPage)
      {
         onCanAddCommentChanged(connectionPage);
      }

      private void ConnectionPage_CanCreateNewChanged(ConnectionPage connectionPage)
      {
         onCanCreateNewChanged(connectionPage);
      }

      private void ConnectionPage_CanReloadAllChanged(ConnectionPage connectionPage)
      {
         onCanReloadAllChanged(connectionPage);
      }

      private void ConnectionPage_StatusChange(string obj)
      {
         addOperationRecord(obj);
      }

      private void ConnectionPage_StorageStatusChange(ConnectionPage connectionPage)
      {
         onStorageStatusChanged(connectionPage);
      }

      private void ConnectionPage_ConnectionStatusChange(ConnectionPage connectionPage)
      {
         onConnectionStatusChanged(connectionPage);
      }

      private void ConnectionPage_ListRefreshed(ConnectionPage connectionPage)
      {
         onListRefreshed(connectionPage);
      }

      private void ConnectionPage_SummaryColorChanged(ConnectionPage connectionPage)
      {
         onSummaryColorChanged(connectionPage);
      }

      private void ConnectionPage_EnabledCustomActionsChanged(ConnectionPage connectionPage)
      {
         onEnabledCustomActionsChanged(connectionPage);
      }

      private void ConnectionPage_RequestLive(ConnectionPage connectionPage)
      {
         if (connectionPage != getCurrentConnectionPage())
         {
            emulateClickOnHostToolbarButton(connectionPage.GetCurrentHostName());
         }
         toolStripButtonLive.PerformClick();
      }

      private void ConnectionPage_RequestRecent(ConnectionPage connectionPage)
      {
         if (connectionPage != getCurrentConnectionPage())
         {
            emulateClickOnHostToolbarButton(connectionPage.GetCurrentHostName());
         }
         toolStripButtonRecent.PerformClick();
      }

      private void ConnectionPage_RequestSearch(ConnectionPage connectionPage)
      {
         if (connectionPage != getCurrentConnectionPage())
         {
            emulateClickOnHostToolbarButton(connectionPage.GetCurrentHostName());
         }
         toolStripButtonSearch.PerformClick();
      }

      private void ConnectionPage_CustomActionListChanged(ConnectionPage connectionPage)
      {
         onCustomActionListChanged(connectionPage);
      }

      private void toolStripButton_CheckedChanged(object sender, System.EventArgs e)
      {
         ToolStripButton button = sender as ToolStripButton;
         if (!button.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(
            new ToolStripButton[] { toolStripButtonLive, toolStripButtonRecent, toolStripButtonSearch },
            button);
         button.CheckOnClick = false;
         if (button == toolStripButtonLive)
         {
            getCurrentConnectionPage()?.GoLive();
         }
         else if (button == toolStripButtonRecent)
         {
            getCurrentConnectionPage()?.GoRecent();
         }
         else if (button == toolStripButtonSearch)
         {
            getCurrentConnectionPage()?.GoSearch();
         }
         else
         {
            Debug.Assert(false);
         }
      }

      private void toolStripButtonDiffTool_Click(object sender, System.EventArgs e)
      {
         ConnectionPage connectionPage = getCurrentConnectionPage();
         if (connectionPage == null)
         {
            return;
         }

         if (connectionPage.CanDiffTool(DiffToolMode.DiffBetweenSelected))
         {
            connectionPage.DiffTool(DiffToolMode.DiffBetweenSelected);
         }
         else if (connectionPage.CanDiffTool(DiffToolMode.DiffSelectedToBase))
         {
            connectionPage.DiffTool(DiffToolMode.DiffSelectedToBase);
         }
         else
         {
            Debug.Assert(false); // toolbar button shall be grayed out
         }
      }

      private void toolStripButtonDiscussions_Click(object sender, System.EventArgs e)
      {
         getCurrentConnectionPage()?.Discussions();
      }

      private void toolStripButtonAddComment_Click(object sender, System.EventArgs e)
      {
         getCurrentConnectionPage()?.AddComment();
      }

      private void toolStripButtonNewThread_Click(object sender, System.EventArgs e)
      {
         getCurrentConnectionPage()?.NewThread();
      }

      private void toolStripButtonRefreshList_Click(object sender, System.EventArgs e)
      {
         getCurrentConnectionPage()?.ReloadAll();
      }

      private void toolStripButtonOpenFromClipboard_Click(object sender, System.EventArgs e)
      {
         connectToUrlFromClipboard();
      }

      private void toolStripButtonCreateNew_Click(object sender, System.EventArgs e)
      {
         getCurrentConnectionPage()?.CreateNew();
      }

      private void toolStripButtonEditTrackedTime_Click(object sender, EventArgs e)
      {
         getCurrentConnectionPage()?.EditTime();
      }

      private void toolStripButtonCancelTimer_Click(object sender, EventArgs e)
      {
         if (WinFormsHelpers.ShowConfirmationDialog("Tracked time will be lost, are you sure?"))
         {
            cancelTimeTrackingTimer();
         }
      }

      private void toolStripButtonGoToTimeTracking_Click(object sender, EventArgs e)
      {
         gotoTimeTrackingMergeRequest();
      }

      private void toolStripButtonStartStopTimer_Click(object sender, System.EventArgs e)
      {
         if (!isTrackingTime())
         {
            startTimeTrackingTimer();
         }
         else
         {
            stopTimeTrackingTimer();
         }
      }

      private void diffToBaseToolStripMenuItem_Click(object sender, EventArgs e)
      {
         ConnectionPage connectionPage = getCurrentConnectionPage();
         if (connectionPage == null)
         {
            return;
         }

         // menu item shall be grayed out if diff selected to base is not available
         Debug.Assert(connectionPage.CanDiffTool(DiffToolMode.DiffSelectedToBase));
         connectionPage.DiffTool(DiffToolMode.DiffSelectedToBase);
      }

      private void refreshSelectedToolStripMenuItem_Click(object sender, EventArgs e)
      {
         getCurrentConnectionPage()?.ReloadSelected();
      }

      protected override void WndProc(ref Message rMessage)
      {
         if (rMessage.Msg == NativeMethods.WM_COPYDATA)
         {
            string argumentString = Win32Tools.ConvertMessageToText(rMessage.LParam);
            if (String.IsNullOrEmpty(argumentString))
            {
               Debug.Assert(false);
               Trace.TraceError(String.Format("Invalid WM_COPYDATA message content: {0}", argumentString));
               return;
            }

            string[] arguments = argumentString.Split('|');
            if (arguments[0] == "show")
            {
               onRestoreFromTray();
            }
            else if (arguments[0] == "diff")
            {
               onDiffCommand(argumentString);
            }
            else if (App.Helpers.UrlHelper.Parse(arguments[0]) != null)
            {
               onOpenCommand(arguments[0]);
            }
         }

         base.WndProc(ref rMessage);
      }

      protected override void OnDpiChanged(DpiChangedEventArgs e)
      {
         base.OnDpiChanged(e);

         processDpiChange();
      }
   }
}

