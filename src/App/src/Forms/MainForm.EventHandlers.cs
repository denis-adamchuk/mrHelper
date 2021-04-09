using System;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.CommonNative;
using mrHelper.CommonControls.Tools;
using Microsoft.Win32;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      // General

      /// <summary>
      /// All exceptions thrown within this method are fatal errors, just pass them to upper level handler
      /// </summary>
      private void mainForm_Load(object sender, EventArgs e)
      {
         Win32Tools.EnableCopyDataMessageHandling(this.Handle);
         initializeWork();
      }

      private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         Trace.TraceInformation(String.Format("[MainForm] Requested to close the Main Form. Reason: {0}",
            e.CloseReason.ToString()));

         if (e.CloseReason == CloseReason.ApplicationExitCall)
         {
            // abnormal exit
            return;
         }

         if (checkBoxMinimizeOnClose.Checked && !_exiting && e.CloseReason == CloseReason.UserClosing)
         {
            e.Cancel = true;
            onHideToTray();
            return;
         }

         Program.Settings.WasMaximizedBeforeClose = WindowState == FormWindowState.Maximized;
         setExitingFlag();
         Hide();

         WinFormsHelpers.CloseAllFormsExceptOne(this);

         finalizeWork();
      }

      private void mainForm_Resize(object sender, EventArgs e)
      {
         if (this.WindowState == _prevWindowState)
         {
            return;
         }

         onWindowStateChanged();
      }

      private void notifyIcon_DoubleClick(object sender, EventArgs e)
      {
         Win32Tools.ForceWindowIntoForeground(this.Handle);
      }

      private void exitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         Trace.TraceInformation("[MainForm] User selected Exit in tray menu");
         doClose();
      }

      private void openFromClipboardMenuItem_Click(object sender, EventArgs e)
      {
         Win32Tools.ForceWindowIntoForeground(this.Handle);
         connectToUrlFromClipboard();
      }

      private void linkLabelNewVersion_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         upgradeApplicationByUserRequest();
      }

      private void linkLabelHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         showHelp();
      }

      private void linkLabelSendFeedback_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         sendFeedback();
      }

      private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
      {
         if (e.Reason == SessionSwitchReason.SessionLock)
         {
            onWorkstationLocked();
         }
      }

      // Settings & Workflow

      private void comboBoxThemes_SelectionChangeCommitted(object sender, EventArgs e)
      {
         if (comboBoxThemes.SelectedItem == null)
         {
            return;
         }

         applyThemeChange();
      }

      private void comboBoxFonts_SelectionChangeCommitted(object sender, EventArgs e)
      {
         if (comboBoxFonts.SelectedItem == null)
         {
            return;
         }

         applyFontChange();
      }

      private void linkLabelCommitStorageDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         Trace.TraceInformation("[MainForm] Clicked on link label for commit storage selection");
         showHelp();
      }

      private void linkLabelWorkflowDescription_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         Trace.TraceInformation("[MainForm] Clicked on link label for workflow type selection");
         showHelp();
      }

      private void buttonEditProjects_Click(object sender, EventArgs e)
      {
         launchEditProjectListDialog();
      }

      private void buttonEditUsers_Click(object sender, EventArgs e)
      {
         launchEditUserListDialog();
      }

      private void radioButtonWorkflowType_CheckedChanged(object sender, EventArgs e)
      {
         if (!(sender as RadioButton).Checked)
         {
            return;
         }

         applyWorkflowTypeChange();
      }

      private void radioButtonUseGit_CheckedChanged(object sender, EventArgs e)
      {
         if (!(sender as RadioButton).Checked)
         {
            return;
         }

         applyGitUsageChange();
      }

      private void radioButtonRevisionType_CheckedChanged(object sender, EventArgs e)
      {
         if (!(sender as RadioButton).Checked)
         {
            return;
         }

         applyRevisionTypeChange();
      }

      private void radioButtonAutoSelectionMode_CheckedChanged(object sender, EventArgs e)
      {
         if (!(sender as RadioButton).Checked)
         {
            return;
         }

         applyAutoSelectionModeChange();
      }

      private void radioButtonShowWarningsOnFileMismatchMode_CheckedChanged(object sender, EventArgs e)
      {
         if (!(sender as RadioButton).Checked)
         {
            return;
         }

         applyShowWarningsOnFileMismatchChange();
      }

      private void radioButtonDiffContextPosition_CheckedChanged(object sender, EventArgs e)
      {
         if (!(sender as RadioButton).Checked)
         {
            return;
         }

         applyDiffContextPositionChange();
      }

      private void radioButtonDiscussionColumnWidth_CheckedChanged(object sender, EventArgs e)
      {
         if (!(sender as RadioButton).Checked)
         {
            return;
         }

         applyDiscussionColumnWidthChange();
      }

      private void buttonBrowseStorageFolder_Click(object sender, EventArgs e)
      {
         launchStorageFolderChangeDialog();
      }

      private void comboBoxColorSchemes_SelectedIndexChanged(object sender, EventArgs e)
      {
         applyColorSchemeChange((sender as ComboBox).Text);
      }

      private void comboBoxHost_SelectionChangeCommited(object sender, EventArgs e)
      {
         applyHostChange(comboBoxHost.Text);
      }

      private void comboBoxHost_Format(object sender, ListControlConvertEventArgs e)
      {
         formatHostListItem(e);
      }

      private void listViewKnownHosts_SelectedIndexChanged(object sender, EventArgs e)
      {
         applyKnownHostSelectionChange();
      }

      private void buttonAddKnownHost_Click(object sender, EventArgs e)
      {
         launchAddKnownHostDialog();
      }

      private void buttonRemoveKnownHost_Click(object sender, EventArgs e)
      {
         onRemoveSelectedHost();
      }

      private void checkBoxMinimizeOnClose_CheckedChanged(object sender, EventArgs e)
      {
         applyMinimizeOnCloseChange((sender as CheckBox).Checked);
      }

      private void checkBoxWordWrapLongRows_CheckedChanged(object sender, EventArgs e)
      {
         applyWordWrapLongRows((sender as CheckBox).Checked);
      }

      private void checkBoxRemindAboutAvailableNewVersion_CheckedChanged(object sender, EventArgs e)
      {
         applyRemindAboutAvailableNewVersionChange((sender as CheckBox).Checked);
      }

      private void checkBoxNewDiscussionIsTopMostForm_CheckedChanged(object sender, EventArgs e)
      {
         applyNewDiscussionIsTopMostFormChange((sender as CheckBox).Checked);
      }

      private void checkBoxDisableSpellChecker_CheckedChanged(object sender, EventArgs e)
      {
         applyDisableSpellCheckerChange((sender as CheckBox).Checked);
      }

      private void checkBoxRunWhenWindowsStarts_CheckedChanged(object sender, EventArgs e)
      {
         applyAutostartSettingChange((sender as CheckBox).Checked);
      }

      private void checkBoxDisableSplitterRestrictions_CheckedChanged(object sender, EventArgs e)
      {
         applyDisableSplitterRestrictionsChange((sender as CheckBox).Checked);
      }

      private void checkBoxNotifications_CheckedChanged(object sender, EventArgs e)
      {
         applyNotificationTypeChange(sender as CheckBox);
      }

      private void comboBoxDCDepth_SelectedIndexChanged(object sender, EventArgs e)
      {
         applyDiffContextDepthChange();
      }

      private void comboBoxRecentMergeRequestsPerProjectCount_SelectedIndexChanged(object sender, EventArgs e)
      {
         applyRecentMergeRequestsPerProjectCount();
      }

      private void checkBoxFlatReplies_CheckedChanged(object sender, EventArgs e)
      {
         applyNeedShiftRepliesChange(!(sender as CheckBox).Checked);
      }

      private void checkBoxEmulateNativeLineBreaks_CheckedChanged(object sender, EventArgs e)
      {
         applyEmulateNativeLineBreaks((sender as CheckBox).Checked);
      }

      private void checkBoxDiscussionColumnFixedWidth_CheckedChanged(object sender, EventArgs e)
      {
         applyIsFixedWidthChange((sender as CheckBox).Checked);
      }

      private void listBoxColorSchemeItemSelector_Format(object sender, ListControlConvertEventArgs e)
      {
         formatColorSchemeItemSelectorItem(e);
      }

      private void listBoxColorSchemeItemSelector_SelectedIndexChanged(object sender, EventArgs e)
      {
         object selectedItem = (sender as ListBox).SelectedItem;
         if (selectedItem != null)
         {
            onListBoxColorSelected(selectedItem as string);
         }
      }

      private void comboBoxColorSelector_SelectedIndexChanged(object sender, EventArgs e)
      {
         ColorSelectorComboBoxItem selectedItem = ((sender as ComboBox).SelectedItem) as ColorSelectorComboBoxItem; 
         if (selectedItem != null)
         {
            onComboBoxColorSelected(selectedItem.Color);
         }
      }

      private void listBoxColorSchemeItemSelector_DrawItem(object sender, DrawItemEventArgs e)
      {
         onDrawListBoxColorSchemeItemSelectorItem(e);
      }

      private void listBoxColorSchemeItemSelector_MeasureItem(object sender, MeasureItemEventArgs e)
      {
         onMeasureListBoxColorSchemeItemSelectorItem(e);
      }

      private void comboBoxColorSelector_DrawItem(object sender, DrawItemEventArgs e)
      {
         onDrawComboBoxColorSelectorItem(e);
      }

      private void linkLabelResetAllColors_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onResetColorSchemeToFactoryValues();
      }

      private void linkLabelResetToFactoyValue_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onResetColorSchemeItemToFactoryValue();
      }

      // Merge Requests

      private void splitContainer_SplitterMoved(object sender, SplitterEventArgs e)
      {
         SplitContainer splitter = sender as SplitContainer;
         if (isUserMovingSplitter(splitter))
         {
            onUserIsMovingSplitter(splitter, false);
         }
      }

      private void splitContainer_SplitterMoving(object sender, SplitterCancelEventArgs e)
      {
         onUserIsMovingSplitter(sender as SplitContainer, true);
      }

      private void buttonDifftool_Click(object sender, EventArgs e)
      {
         launchDiffToolForSelectedMergeRequest();
      }

      private void buttonAddComment_Click(object sender, EventArgs e)
      {
         addCommentForSelectedMergeRequest();
      }

      private void buttonNewDiscussion_Click(object sender, EventArgs e)
      {
         newDiscussionForSelectedMergeRequest();
      }

      private void buttonTimeTrackingStart_Click(object sender, EventArgs e)
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

      private void buttonTimeTrackingCancel_Click(object sender, EventArgs e)
      {
         cancelTimeTrackingTimer();
      }

      private void buttonTimeEdit_Click(object sender, EventArgs s)
      {
         editTimeOfSelectedMergeRequest();
      }

      private void listViewMergeRequests_ContentChanged(object sender)
      {
         updateMergeRequestList(getListViewType(sender as Controls.MergeRequestListView));
         saveState();
      }

      private void listViewMergeRequests_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
      {
         onMergeRequestSelectionChanged(getListViewType(sender as Controls.MergeRequestListView));
      }

      private void textBoxSearchText_KeyDown(object sender, KeyEventArgs e)
      {
         onSearchTextBoxKeyDown(e.KeyCode);
      }

      private void textBoxSearchTargetBranch_KeyDown(object sender, KeyEventArgs e)
      {
         onSearchTextBoxKeyDown(e.KeyCode);
      }

      private void textBoxSearchText_TextChanged(object sender, EventArgs e)
      {
         onSearchTextBoxTextChanged(textBoxSearchText, checkBoxSearchByTitleAndDescription);
      }

      private void textBoxSearchTargetBranch_TextChanged(object sender, EventArgs e)
      {
         onSearchTextBoxTextChanged(textBoxSearchTargetBranch, checkBoxSearchByTargetBranch);
      }

      private void comboBoxUser_SelectionChangeCommitted(object sender, EventArgs e)
      {
         onSearchComboBoxSelectionChangeCommitted(checkBoxSearchByAuthor);
      }

      private void comboBoxProjectName_SelectionChangeCommitted(object sender, EventArgs e)
      {
         onSearchComboBoxSelectionChangeCommitted(checkBoxSearchByProject);
      }

      private void linkLabelFindMe_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         selectCurrentUserInSearchDropdown();
      }

      private void checkBoxSearch_CheckedChanged(object sender, EventArgs e)
      {
         updateSearchButtonState();
      }

      private void comboBoxProjectName_Format(object sender, ListControlConvertEventArgs e)
      {
         formatProjectListItem(e);
      }

      private void comboBoxUser_Format(object sender, ListControlConvertEventArgs e)
      {
         formatUserListItem(e);
      }

      private void textBoxDisplayFilter_TextChanged(object sender, EventArgs e)
      {
         onTextBoxDisplayFilterUpdate();
      }

      private void textBoxDisplayFilter_Leave(object sender, EventArgs e)
      {
         onTextBoxDisplayFilterUpdate();
      }

      private void checkBoxDisplayFilter_CheckedChanged(object sender, EventArgs e)
      {
         applyFilterChange((sender as CheckBox).Checked);
      }

      private void buttonReloadList_Click(object sender, EventArgs e)
      {
         reloadMergeRequestsByUserRequest(getDataCache(EDataCacheType.Live));
      }

      private void buttonSearch_Click(object sender, EventArgs e)
      {
         onStartSearch();
      }

      private void buttonDiscussions_Click(object sender, EventArgs e)
      {
         showDiscussionsForSelectedMergeRequest();
      }

      private void linkLabelAbortGitClone_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onAbortGitByUserRequest();
      }

      private void linkLabelTimeTrackingMergeRequest_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         gotoTimeTrackingMergeRequest();
      }

      private void groupBoxActions_SizeChanged(object sender, EventArgs e)
      {
         repositionCustomCommands();
      }

      private void groupBoxActions_VisibleChanged(object sender, EventArgs e)
      {
         repositionCustomCommands();
      }

      private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
      {
         initializeMergeRequestTabMinimumSizes();
      }

      private void tabControlMode_SelectedIndexChanged(object sender, EventArgs e)
      {
         onDataCacheSelectionChanged();
      }

      private void tabControl_Selecting(object sender, TabControlCancelEventArgs e)
      {
         if (!_canSwitchTab)
         {
            e.Cancel = true;
         }
      }

      private void RevisionBrowser_SelectionChanged(object sender, EventArgs e)
      {
         updateStorageDependentControlState(getMergeRequestKey(null));
      }

      private void buttonCreateNew_Click(object sender, EventArgs e)
      {
         createNewMergeRequestByUserRequest();
      }

      private void tabControlMode_SizeChanged(object sender, EventArgs e)
      {
         placeControlNearToRightmostTab(tabControlMode, linkLabelFromClipboard, 20);
      }

      private void LinkLabelFromClipboard_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         connectToUrlFromClipboard();
      }

      // Other

      protected override void WndProc(ref Message rMessage)
      {
         if (rMessage.Msg == NativeMethods.WM_COPYDATA)
         {
            string argumentString = Win32Tools.ConvertMessageToText(rMessage.LParam);

            string[] arguments = argumentString.Split('|');
            if (arguments.Length < 2)
            {
               Debug.Assert(false);
               Trace.TraceError(String.Format("Invalid WM_COPYDATA message content: {0}", argumentString));
               return;
            }

            if (arguments[1] == "diff")
            {
               onDiffCommand(argumentString);
            }
            else
            {
               onOpenCommand(argumentString);
            }
         }

         base.WndProc(ref rMessage);
      }

      protected override void OnFontChanged(EventArgs e)
      {
         base.OnFontChanged(e);

         processFontChange();
      }

      protected override void OnDpiChanged(DpiChangedEventArgs e)
      {
         base.OnDpiChanged(e);

         processDpiChange();
      }

   }
}

