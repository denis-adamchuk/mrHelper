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
         if (!checkAutoScaleDimensions())
         {
            return;
         }
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
         bool hideBeforeSizeRestored = 
            WindowState != FormWindowState.Minimized
            && _prevWindowState == FormWindowState.Minimized
            && !_loadingConfiguration
            && !_inRestoringSize
            && _restoreSizeOnNextRestore;
         if (hideBeforeSizeRestored)
         {
            Trace.TraceInformation("[MainForm] Hide() before size restored");
            Hide();
         }

         base.OnResize(e);
         if (WindowState != _prevWindowState)
         {
            onWindowStateChanged();
         }

         if (hideBeforeSizeRestored)
         {
            Trace.TraceInformation("[MainForm] Show() after size restored");
            Show();
         }

         // Doing this outside of onWindowStateChanged() because sometimes we receive
         // more than once Resize event on Maximize and/or Restore actions
         if (WindowState != FormWindowState.Minimized
            && !_loadingConfiguration
            && !_inRestoringSize)
         {
            Trace.TraceInformation("[MainForm] OnResize() calling StoreSplitterDistance()");
            getCurrentConnectionPage()?.StoreSplitterDistance();
         }

         initToolBars();
      }

      protected override void OnResizeEnd(EventArgs e)
      {
         base.OnResizeEnd(e);

         Trace.TraceInformation("[MainForm] OnResizeEnd() Size={0}x{1}, Location={2}x{3}",
            Width, Height, Location.X, Location.Y);
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
         switch (e.Reason)
         {
            case SessionSwitchReason.SessionLock:
               pauseTimeTrackingTimer();
               startSessionLockCheckTimer();
               Trace.TraceInformation("[MainForm] Workstation locked");
               break;

            case SessionSwitchReason.SessionUnlock:
               resumeTimeTrackingTimer();
               stopSessionLockCheckTimer();
               Trace.TraceInformation("[MainForm] Workstation unlocked");
               break;
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
         using (ConfigureNotificationsForm form = new ConfigureNotificationsForm(_keywords))
         {
            WinFormsHelpers.ShowDialogOnControl(form, this);
         }
      }

      private void configureHostsToolStripMenuItem_Click(object sender, EventArgs e)
      {
         using (ConfigureHostsForm form = new ConfigureHostsForm())
         {
            if (WinFormsHelpers.ShowDialogOnControl(form, WinFormsHelpers.FindMainForm()) == DialogResult.OK
               && form.Changed)
            {
               Trace.TraceInformation("[MainForm] Reconnecting after workflow type change");
               reconnect();
            }
         }
      }

      private void configureStorageToolStripMenuItem_Click(object sender, EventArgs e)
      {
         string oldPath = Program.Settings.LocalStorageFolder;
         using (ConfigureStorageForm form = new ConfigureStorageForm())
         {
            if (WinFormsHelpers.ShowDialogOnControl(form, WinFormsHelpers.FindMainForm()) == DialogResult.OK
               && form.Changed)
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
      }

      private void configureColorsToolStripMenuItem_Click(object sender, EventArgs e)
      {
         using (ConfigureColorsForm form = new ConfigureColorsForm(DefaultCategory.General))
         {
            WinFormsHelpers.ShowDialogOnControl(form, this);
         }
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

      private void radioButtonTheme_CheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(themeToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         applyThemeChange();
      }

      private void radioButtonToolbarLayout_CheckedChanged(object sender, EventArgs e)
      {
         ToolStripMenuItem checkBox = sender as ToolStripMenuItem;
         if (!checkBox.Checked)
         {
            return;
         }

         WinFormsHelpers.UncheckAllExceptOne(toolbarPositionToolStripMenuItem.DropDownItems
            .Cast<ToolStripMenuItem>().ToArray(), checkBox);
         checkBox.CheckOnClick = false;
         applyToolbarLayoutChange();
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

      private void checkBoxShowHiddenMergeRequestIds_CheckedChanged(object sender, EventArgs e)
      {
         applyShowHiddenMergeRequestIds((sender as ToolStripMenuItem).Checked);
      }

      private void checkBoxFlatRevisionPreview_CheckedChanged(object sender, EventArgs e)
      {
         applyFlatRevisionPreview((sender as ToolStripMenuItem).Checked);
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

      private void ConnectionPage_CanEditChanged(ConnectionPage connectionPage)
      {
         onCanEditChanged(connectionPage);
      }

      private void ConnectionPage_CanMergeChanged(ConnectionPage connectionPage)
      {
         onCanMergeChanged(connectionPage);
      }

      private void ConnectionPage_CanToggleHideStatusChanged(ConnectionPage connectionPage)
      {
         onCanToggleHideStatusChanged(connectionPage);
      }

      private void ConnectionPage_CanTogglePinStatusChanged(ConnectionPage connectionPage)
      {
         onCanTogglePinStatusChanged(connectionPage);
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
         toolStripButtonLive.PerformClick();
      }

      private void ConnectionPage_RequestRecent(ConnectionPage connectionPage)
      {
         toolStripButtonRecent.PerformClick();
      }

      private void ConnectionPage_RequestSearch(ConnectionPage connectionPage)
      {
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
         synchronizePageWithSelectedMode();
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

      private void toolStripButtonEditMergeRequest_Click(object sender, System.EventArgs e)
      {
         getCurrentConnectionPage()?.EditMergeRequest();
      }

      private void toolStripButtonMergeMergeRequest_Click(object sender, System.EventArgs e)
      {
         getCurrentConnectionPage()?.MergeMergeRequest();
      }

      private void toolStripButtonHideMergeRequest_Click(object sender, System.EventArgs e)
      {
         getCurrentConnectionPage()?.ToggleHideState();
      }

      private void toolStripButtonPinMergeRequest_Click(object sender, System.EventArgs e)
      {
         getCurrentConnectionPage()?.TogglePinState();
      }

      private void toolStripButtonRefreshList_Click(object sender, System.EventArgs e)
      {
         showWarningOnReloadList();
         getCurrentConnectionPage()?.ReloadLive();
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
            else if (Common.Tools.UrlHelper.Parse(arguments[0], getSourceBranchTemplates()) != null)
            {
               // put the string into the queue if Parse() considered it a valid url
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

