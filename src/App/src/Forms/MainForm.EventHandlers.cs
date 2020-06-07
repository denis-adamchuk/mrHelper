using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.App.Forms.Helpers;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Client.Session;
using mrHelper.Client.TimeTracking;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.CommonNative;
using mrHelper.Common.Interfaces;
using mrHelper.GitClient;
using mrHelper.CommonControls.Tools;
using static mrHelper.App.Controls.MergeRequestListView;
using Newtonsoft.Json.Linq;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      /// <summary>
      /// All exceptions thrown within this method are fatal errors, just pass them to upper level handler
      /// </summary>
      private void MainForm_Load(object sender, EventArgs e)
      {
         Win32Tools.EnableCopyDataMessageHandling(this.Handle);

         if (_runningAsUwp)
         {
            revertOldInstallations();
         }

         if (!integrateInTools())
         {
            doClose();
            return;
         }

         cleanUpTempFolder("mrHelper.*.msi");
         cleanUpTempFolder("mrHelper.*.msix");
         cleanUpTempFolder("mrHelper.logs.*.zip");
         checkForApplicationUpdates();

         initializeWork();
         connectOnStartup();
      }

      private void closeAllFormsExceptMain()
      {
         for (int iForm = Application.OpenForms.Count - 1; iForm >= 0; --iForm)
         {
            if (Application.OpenForms[iForm] != this)
            {
               Application.OpenForms[iForm].Close();
            }
         }
      }

      private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
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
         Hide();

         closeAllFormsExceptMain();

         finalizeWork();

         // This allows to handle all pending invocations that other threads are
         // already ready to make before we dispose ourselves
         Application.DoEvents();
      }

      private void MainForm_Resize(object sender, EventArgs e)
      {
         if (this.WindowState == _prevWindowState)
         {
            return;
         }

         if (this.WindowState != FormWindowState.Minimized)
         {
            if (_prevWindowState == FormWindowState.Minimized)
            {
               if (this.WindowState == FormWindowState.Normal && _forceMaximizeOnNextRestore)
               {
                  _forceMaximizeOnNextRestore = false;
                  _prevWindowState = FormWindowState.Maximized; // prevent re-entrance on next line
                  this.WindowState = FormWindowState.Maximized;
               }

               resetMinimumSizes();
               updateMinimumSizes();
            }
         }

         _prevWindowState = WindowState;
      }

      private void NotifyIcon_DoubleClick(object sender, EventArgs e)
      {
         Win32Tools.ForceWindowIntoForeground(this.Handle);
      }

      async private void ButtonDifftool_Click(object sender, EventArgs e)
      {
         Debug.Assert(getMergeRequestKey(null).HasValue);

         MergeRequestKey mrk = getMergeRequestKey(null).Value;

         await onLaunchDiffToolAsync(mrk);
      }

      async private void ButtonAddComment_Click(object sender, EventArgs e)
      {
         Debug.Assert(getMergeRequestKey(null).HasValue);
         Debug.Assert(getMergeRequest(null) != null);

         MergeRequest mergeRequest = getMergeRequest(null);
         MergeRequestKey mrk = getMergeRequestKey(null).Value;

         await onAddCommentAsync(mrk, mergeRequest.Title);
      }

      async private void ButtonNewDiscussion_Click(object sender, EventArgs e)
      {
         Debug.Assert(getMergeRequestKey(null).HasValue);
         Debug.Assert(getMergeRequest(null) != null);

         MergeRequest mergeRequest = getMergeRequest(null);
         MergeRequestKey mrk = getMergeRequestKey(null).Value;

         await onNewDiscussionAsync(mrk, mergeRequest.Title);
      }

      async private void ButtonTimeTrackingStart_Click(object sender, EventArgs e)
      {
         ISession session = getSession(!isSearchMode());

         if (isTrackingTime())
         {
            await onStopTimer(true, session?.TotalTimeCache);
         }
         else
         {
            onStartTimer(session);
         }
      }

      async private void ButtonTimeTrackingCancel_Click(object sender, EventArgs e)
      {
         Debug.Assert(isTrackingTime());
         await onStopTimer(false, getSession(!isSearchMode())?.TotalTimeCache);
      }

      async private void ButtonTimeEdit_Click(object sender, EventArgs s)
      {
         // Store data before opening a modal dialog
         Debug.Assert(getMergeRequestKey(null).HasValue);
         MergeRequestKey mrk = getMergeRequestKey(null).Value;

         Debug.Assert(getMergeRequest(null) != null);
         MergeRequest mr = getMergeRequest(null);

         Debug.Assert(!isSearchMode());
         ISession session = getSession(true /* supported in Live only */);
         ITotalTimeCache totalTimeCache = session?.TotalTimeCache;
         if (totalTimeCache == null)
         {
            return;
         }

         TimeSpan oldSpan = getTotalTime(mrk, totalTimeCache) ?? TimeSpan.Zero;
         using (EditTimeForm form = new EditTimeForm(oldSpan))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               TimeSpan newSpan = form.TimeSpan;
               bool add = newSpan > oldSpan;
               TimeSpan diff = add ? newSpan - oldSpan : oldSpan - newSpan;
               if (diff != TimeSpan.Zero)
               {
                  try
                  {
                     await totalTimeCache.AddSpan(add, diff, mrk);
                  }
                  catch (TimeTrackingException ex)
                  {
                     string message = "Cannot edit total tracked time";
                     ExceptionHandlers.Handle(message, ex);
                     MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                     return;
                  }

                  updateTotalTime(mrk, mr.Author, mrk.ProjectKey.HostName, totalTimeCache);

                  labelWorkflowStatus.Text = "Total spent time updated";

                  Trace.TraceInformation(String.Format("[MainForm] Total time for MR {0} (project {1}) changed to {2}",
                     mrk.IId, mrk.ProjectKey.ProjectName, diff.ToString()));
               }
            }
         }
      }

      private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         Trace.TraceInformation("[MainForm] User selected Exit in tray menu");
         doClose();
      }

      private void ButtonBrowseLocalGitFolder_Click(object sender, EventArgs e)
      {
         localGitFolderBrowser.SelectedPath = textBoxLocalGitFolder.Text;
         if (localGitFolderBrowser.ShowDialog() == DialogResult.OK)
         {
            string newFolder = localGitFolderBrowser.SelectedPath;
            Trace.TraceInformation(String.Format("[MainForm] User decided to change parent folder to {0}", newFolder));

            if (_gitClientFactory == null || _gitClientFactory.ParentFolder != newFolder)
            {
               textBoxLocalGitFolder.Text = localGitFolderBrowser.SelectedPath;
               Program.Settings.LocalGitFolder = localGitFolderBrowser.SelectedPath;

               MessageBox.Show("Git folder is changed.\n" +
                               "It is recommended to restart Diff Tool if you have already launched it.",
                  "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

               labelWorkflowStatus.Text = "Parent folder for git repositories changed";
               Trace.TraceInformation(String.Format("[MainForm] Parent folder changed to {0}",
                  newFolder));

               // Emulating a host switch here to trigger GitDataUpdater to work at the new location
               Trace.TraceInformation(String.Format("[MainForm] Emulating host switch on parent folder change"));
               switchHostToSelected();
            }
         }
      }

      private void ComboBoxColorSchemes_SelectionChangeCommited(object sender, EventArgs e)
      {
         initializeColorScheme();
         Program.Settings.ColorSchemeFileName = (sender as ComboBox).Text;
      }

      private void ComboBoxHost_SelectionChangeCommited(object sender, EventArgs e)
      {
         string hostname = (sender as ComboBox).Text;

         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change host to {0}", hostname));

         updateProjectsListView();
         updateUsersListView();
         switchHostToSelected();
      }

      private void drawComboBoxEdit(DrawItemEventArgs e, ComboBox comboBox, Color backColor, string text)
      {
         if (backColor == SystemColors.Window)
         {
            backColor = Color.FromArgb(225, 225, 225); // Gray shade similar to original one
         }
         using (Brush brush = new SolidBrush(backColor))
         {
            e.Graphics.FillRectangle(brush, e.Bounds);
         }

         e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
         e.Graphics.DrawString(text, comboBox.Font, SystemBrushes.ControlText, e.Bounds);
      }

      private void ListViewMergeRequests_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
      {
         if (e.Item.ListView == null)
         {
            return; // is being removed
         }

         Rectangle bounds = e.Bounds;
         if (e.ColumnIndex == 0 && e.Item.ListView.Columns[0].DisplayIndex != 0)
         {
            bounds = WinFormsHelpers.GetFirstColumnCorrectRectangle(e.Item.ListView, e.Item);
         }

         FullMergeRequestKey fmk = (FullMergeRequestKey)(e.Item.Tag);

         bool isSelected = e.Item.Selected;
         WinFormsHelpers.FillRectangle(e, bounds, getMergeRequestColor(fmk.MergeRequest, Color.Transparent), isSelected);

         Brush textBrush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;

         string text = ((ListViewSubItemInfo)(e.SubItem.Tag)).Text;
         bool isClickable = ((ListViewSubItemInfo)(e.SubItem.Tag)).Clickable;

         StringFormat format =
            new StringFormat
            {
               Trimming = StringTrimming.EllipsisCharacter,
               FormatFlags = StringFormatFlags.NoWrap
            };

         if (isClickable)
         {
            using (Font font = new Font(e.Item.ListView.Font, FontStyle.Underline))
            {
               Brush brush = Brushes.Blue;
               e.Graphics.DrawString(text, font, brush, bounds, format);
            }
         }
         else
         {
            if (isSelected && e.ColumnIndex == columnHeaderLabels.Index)
            {
               using (Brush brush = new SolidBrush(getMergeRequestColor(fmk.MergeRequest, SystemColors.Window)))
               {
                  e.Graphics.DrawString(text, e.Item.ListView.Font, brush, bounds, format);
               }
            }
            else if (e.ColumnIndex == columnHeaderResolved.Index)
            {
               using (Brush brush = new SolidBrush(getDiscussionCountColor(fmk, isSelected)))
               {
                  e.Graphics.DrawString(text, e.Item.ListView.Font, brush, bounds, format);
               }
            }
            else
            {
               e.Graphics.DrawString(text, e.Item.ListView.Font, textBrush, bounds, format);
            }
         }
      }

      private void ListViewMergeRequests_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
      {
         e.DrawDefault = true;
      }

      private void ListViewMergeRequests_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
      {
         ListView listView = (sender as ListView);

         ListViewHitTestInfo hit = listView.HitTest(e.Location);
         bool clickable = hit.SubItem != null && ((ListViewSubItemInfo)(hit.SubItem.Tag)).Clickable;
         listView.Cursor = clickable ? Cursors.Hand : Cursors.Default;
      }

      private void ListViewMergeRequests_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
      {
         ListView listView = (sender as ListView);

         ListViewHitTestInfo hit = listView.HitTest(e.Location);
         bool clickable = hit.SubItem != null && ((ListViewSubItemInfo)(hit.SubItem.Tag)).Clickable;
         if (clickable)
         {
            openBrowser(((ListViewSubItemInfo)(hit.SubItem.Tag)).Url);
         }
      }

      private void ListViewMergeRequests_Deselected(object sender)
      {
         ListView listView = (sender as ListView);
         Debug.Assert(listView.SelectedItems.Count < 1);

         disableCommonUIControls();
         updateGitAbortState(false);
      }

      private void ListViewMergeRequests_ItemSelectionChanged(
         object sender, ListViewItemSelectionChangedEventArgs e)
      {
         ListView listView = (sender as ListView);
         Debug.Assert(listView.SelectedItems.Count > 0);
         listView.EnsureVisible(listView.SelectedIndices[0]);

         FullMergeRequestKey fmk = (FullMergeRequestKey)(listView.SelectedItems[0].Tag);

         if (isSearchMode())
         {
            switchSearchMergeRequestByUser(fmk, checkBoxShowVersions.Checked);
         }
         else
         {
            switchMergeRequestByUser(fmk, checkBoxShowVersions.Checked);
            if (getMergeRequestKey(listViewMergeRequests) != null)
            {
               _lastMergeRequestsByHosts[fmk.ProjectKey.HostName] = getMergeRequestKey(listViewMergeRequests).Value;
            }
         }
      }

      private void TextBoxSearch_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter)
         {
            if (radioButtonSearchByTargetBranch.Checked)
            {
               searchMergeRequests(new SearchByTargetBranch(textBoxSearch.Text), null);
            }
            else if (radioButtonSearchByTitleAndDescription.Checked)
            {
               // See restrictions at https://docs.gitlab.com/ee/api/README.html#offset-based-pagination
               Debug.Assert(Constants.MaxSearchByTitleAndDescriptionResults <= 100);

               searchMergeRequests(new SearchByText(textBoxSearch.Text),
                  Constants.MaxSearchByTitleAndDescriptionResults);
            }
            else
            {
               Debug.Assert(false);
            }
         }
      }

      private void checkBoxShowVersionsByDefault_CheckedChanged(object sender, EventArgs e)
      {
         Program.Settings.ShowVersionsByDefault = (sender as CheckBox).Checked;
      }

      private void checkBoxShowVersions_CheckedChanged(object sender, EventArgs e)
      {
         bool showVersions = (sender as CheckBox).Checked;
         groupBoxSelectCommits.Text = showVersions ? "Select versions for diff tool" : "Select commits for diff tool";

         if ((isSearchMode() && listViewFoundMergeRequests.SelectedItems.Count < 1) ||
            (!isSearchMode() && listViewMergeRequests.SelectedItems.Count < 1))
         {
            return;
         }

         Trace.TraceInformation(String.Format(
            "[MainForm] User changed Show Versions to {0}. Reloading current Merge Request",
            showVersions.ToString()));

         ListViewItem selected = isSearchMode() ?
            listViewFoundMergeRequests.SelectedItems[0] : listViewMergeRequests.SelectedItems[0];
         selected.Selected = false;
         selected.Selected = true;
      }

      private void ComboBoxCommits_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
      {
         if (e.Index < 0)
         {
            return;
         }

         ComboBox comboBox = sender as ComboBox;
         CommitComboBoxItem item = (CommitComboBoxItem)(comboBox.Items[e.Index]);

         e.DrawBackground();

         if ((e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit)
         {
            drawComboBoxEdit(e, comboBox, getCommitComboBoxItemColor(item), formatCommitComboboxItem(item));
         }
         else
         {
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            WinFormsHelpers.FillRectangle(e, e.Bounds, getCommitComboBoxItemColor(item), isSelected);

            Brush textBrush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawString(formatCommitComboboxItem(item), comboBox.Font, textBrush, e.Bounds);
         }

         e.DrawFocusRectangle();
      }

      private void ComboBoxLatestCommit_SelectedIndexChanged(object sender, EventArgs e)
      {
         checkComboboxCommitsOrder(comboBoxLatestCommit, comboBoxEarliestCommit, true /* I'm the latest one */);
         setCommitComboboxTooltipText(sender as ComboBox, toolTip);
         setCommitComboboxLabels(sender as ComboBox, getLabelForComboBox(sender as ComboBox));
      }

      private void ComboBoxEarliestCommit_SelectedIndexChanged(object sender, EventArgs e)
      {
         checkComboboxCommitsOrder(comboBoxLatestCommit, comboBoxEarliestCommit, false /* because I'm the earliest one */);
         setCommitComboboxTooltipText(sender as ComboBox, toolTip);
         setCommitComboboxLabels(sender as ComboBox, getLabelForComboBox(sender as ComboBox));
      }

      private Label getLabelForComboBox(ComboBox box)
      {
         if (box == comboBoxLatestCommit)
         {
            return labelLatestCommitTimestampLabel;
         }
         else if (box == comboBoxEarliestCommit)
         {
            return labelEarliestCommitTimestampLabel;
         }
         return null;
      }

      private void ComboBoxHost_Format(object sender, ListControlConvertEventArgs e)
      {
         formatHostListItem(e);
      }

      private void LinkLabelConnectedTo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         openBrowser((sender as LinkLabel).Text);
      }

      private void listViewKnownHosts_SelectedIndexChanged(object sender, EventArgs e)
      {
         bool enableRemoveButton = listViewKnownHosts.SelectedItems.Count > 0;
         buttonRemoveKnownHost.Enabled = enableRemoveButton;
      }

      async private void ButtonAddKnownHost_Click(object sender, EventArgs e)
      {
         using (AddKnownHostForm form = new AddKnownHostForm())
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               return;
            }

            string hostname = StringUtils.GetHostWithPrefix(form.Host);
            GitLabClientManager.EConnectionCheckStatus status =
               await _gitlabClientManager.VerifyConnection(hostname, form.AccessToken);
            if (status != GitLabClientManager.EConnectionCheckStatus.OK)
            {
               string message =
                  status == GitLabClientManager.EConnectionCheckStatus.BadAccessToken
                     ? "Bad access token"
                     : "Invalid hostname";
               MessageBox.Show(message, "Cannot connect to the host",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }

            if (!addKnownHost(hostname, form.AccessToken))
            {
               MessageBox.Show("Such host is already in the list", "Host will not be added",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
               return;
            }

            updateKnownHostAndTokensInSettings();
            updateHostsDropdownList();
            selectHost(PreferredSelection.Latest);
            switchHostToSelected();
         }
      }

      private void ButtonRemoveKnownHost_Click(object sender, EventArgs e)
      {
         bool removeCurrent =
               listViewKnownHosts.SelectedItems.Count > 0 && getHostName() != String.Empty
            && getHostName() == listViewKnownHosts.SelectedItems[0].Text;

         string removedHostName = listViewKnownHosts.SelectedItems.Count > 0
            ? listViewKnownHosts.SelectedItems[0].Text
            : String.Empty;

         if (!removeKnownHost())
         {
            return;
         }

         Debug.Assert(!String.IsNullOrEmpty(removedHostName));

         _currentUser.Remove(removedHostName);
         updateKnownHostAndTokensInSettings();
         updateHostsDropdownList();
         if (removeCurrent)
         {
            if (comboBoxHost.Items.Count == 0)
            {
               updateProjectsListView();
               updateUsersListView();
            }
            else
            {
               selectHost(PreferredSelection.Latest);
            }

            // calling this unconditionally to drop current sessions and disable UI
            switchHostToSelected();
         }
      }

      private void updateKnownHostAndTokensInSettings()
      {
         Program.Settings.KnownHosts = listViewKnownHosts
            .Items
            .Cast<ListViewItem>()
            .Select(i => i.Text)
            .ToArray();
         Program.Settings.KnownAccessTokens = listViewKnownHosts
            .Items
            .Cast<ListViewItem>()
            .Select(i => i.SubItems[1].Text)
            .ToArray();
      }

      private void CheckBoxMinimizeOnClose_CheckedChanged(object sender, EventArgs e)
      {
         Program.Settings.MinimizeOnClose = (sender as CheckBox).Checked;
      }

      private void checkBoxRunWhenWindowsStarts_CheckedChanged(object sender, EventArgs e)
      {
         Program.Settings.RunWhenWindowsStarts = (sender as CheckBox).Checked;

         if (!_loadingConfiguration)
         {
            applyAutostartSetting(Program.Settings.RunWhenWindowsStarts);
         }
      }

      private void CheckBoxDisableSplitterRestrictions_CheckedChanged(object sender, EventArgs e)
      {
         Program.Settings.DisableSplitterRestrictions = (sender as CheckBox).Checked;
         resetMinimumSizes();
      }

      private void checkBoxAutoSelectNewestCommit_CheckedChanged(object sender, EventArgs e)
      {
         Program.Settings.AutoSelectNewestCommit = (sender as CheckBox).Checked;

         MergeRequestKey? mrk = getMergeRequestKey(null);
         if (mrk.HasValue)
         {
            selectNotReviewedCommits(isSearchMode(),
               comboBoxLatestCommit, comboBoxEarliestCommit, getReviewedCommits(mrk.Value));
         }
      }

      private void checkBoxUseShallowClone_CheckedChanged(object sender, EventArgs e)
      {
         if (_loadingConfiguration)
         {
            return;
         }

         Program.Settings.UseShallowClone = checkBoxUseShallowClone.Checked;

         Trace.TraceInformation(String.Format("[MainForm] Shallow clone setting has been selected"));
         switchHostToSelected();
      }

      private void checkBoxNotifications_CheckedChanged(object sender, EventArgs e)
      {
         bool state = (sender as CheckBox).Checked;
         if (sender == checkBoxShowNewMergeRequests)
         {
            Program.Settings.Notifications_NewMergeRequests = state;
         }
         else if (sender == checkBoxShowMergedMergeRequests)
         {
            Program.Settings.Notifications_MergedMergeRequests = state;
         }
         else if (sender == checkBoxShowUpdatedMergeRequests)
         {
            Program.Settings.Notifications_UpdatedMergeRequests = state;
         }
         else if (sender == checkBoxShowResolvedAll)
         {
            Program.Settings.Notifications_AllThreadsResolved = state;
         }
         else if (sender == checkBoxShowOnMention)
         {
            Program.Settings.Notifications_OnMention = state;
         }
         else if (sender == checkBoxShowKeywords)
         {
            Program.Settings.Notifications_Keywords = state;
         }
         else if (sender == checkBoxShowMyActivity)
         {
            Program.Settings.Notifications_MyActivity = state;
         }
         else if (sender == checkBoxShowServiceNotifications)
         {
            Program.Settings.Notifications_Service = state;
         }
      }

      private void listViewMergeRequests_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
      {
         Debug.Assert(sender == listViewMergeRequests || sender == listViewFoundMergeRequests);
         Action<Dictionary<string, int>> propertyChange = sender == listViewMergeRequests
            ? new Action<Dictionary<string, int>>(x => Program.Settings.ListViewMergeRequestsColumnWidths = x)
            : new Action<Dictionary<string, int>>(x => Program.Settings.ListViewFoundMergeRequestsColumnWidths = x);
         saveColumnWidths(sender as ListView, propertyChange);
      }

      private void listViewMergeRequests_ColumnReordered(object sender, ColumnReorderedEventArgs e)
      {
         Debug.Assert(sender == listViewMergeRequests || sender == listViewFoundMergeRequests);
         if (sender == listViewMergeRequests)
         {
            Program.Settings.ListViewMergeRequestsDisplayIndices =
               WinFormsHelpers.GetListViewDisplayIndicesOnColumnReordered(listViewMergeRequests,
                  e.OldDisplayIndex, e.NewDisplayIndex);
            return;
         }

         Program.Settings.ListViewFoundMergeRequestsDisplayIndices =
            WinFormsHelpers.GetListViewDisplayIndicesOnColumnReordered(listViewFoundMergeRequests,
               e.OldDisplayIndex, e.NewDisplayIndex);
      }

      private bool isUserMovingSplitter(SplitContainer splitter)
      {
         Debug.Assert(splitter == splitContainer1 || splitter == splitContainer2);

         return splitter == splitContainer1 ? _userIsMovingSplitter1 : _userIsMovingSplitter2;
      }

      private void onUserIsMovingSplitter(SplitContainer splitter, bool value)
      {
         Debug.Assert(splitter == splitContainer1 || splitter == splitContainer2);

         if (splitter == splitContainer1)
         {
            if (!value)
            {
               // move is finished, store the value
               Program.Settings.MainWindowSplitterDistance = splitter.SplitterDistance;
            }
            _userIsMovingSplitter1 = value;
         }
         else
         {
            if (!value)
            {
               // move is finished, store the value
               Program.Settings.RightPaneSplitterDistance = splitter.SplitterDistance;
            }
            _userIsMovingSplitter2 = value;
         }
      }

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
         SplitContainer splitter = sender as SplitContainer;

         onUserIsMovingSplitter(splitter, true);
      }

      private void textBoxDisplayFilter_TextChanged(object sender, EventArgs e)
      {
         onTextBoxDisplayFilterUpdate();
      }

      private void textBoxDisplayFilter_Leave(object sender, EventArgs e)
      {
         onTextBoxDisplayFilterUpdate();
      }

      private void onTextBoxDisplayFilterUpdate()
      {
         Program.Settings.DisplayFilter = textBoxDisplayFilter.Text;
         if (_mergeRequestFilter != null)
         {
            _mergeRequestFilter.Filter = createMergeRequestFilterState();
         }
      }

      private void CheckBoxDisplayFilter_CheckedChanged(object sender, EventArgs e)
      {
         Program.Settings.DisplayFilterEnabled = (sender as CheckBox).Checked;
         if (_mergeRequestFilter != null)
         {
            _mergeRequestFilter.Filter = createMergeRequestFilterState();
         }
      }

      private void ButtonReloadList_Click(object sender, EventArgs e)
      {
         if (Program.Settings.ShowWarningOnReloadList)
         {
            int autoUpdateMs = Program.Settings.AutoUpdatePeriodMs;
            double oneMinuteMs = 60000;
            double autoUpdateMinutes = autoUpdateMs / oneMinuteMs;

            string periodicity = autoUpdateMs > oneMinuteMs
               ? (autoUpdateMs % Convert.ToInt32(oneMinuteMs) == 0
                  ? String.Format("{0} minutes", autoUpdateMinutes)
                  : String.Format("{0:F1} minutes", autoUpdateMinutes))
               : String.Format("{0} seconds", autoUpdateMs / 1000);

            string message = String.Format(
               "Merge Request list updates each {0} and you don't usually need to update it manually", periodicity);
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Program.Settings.ShowWarningOnReloadList = false;
         }

         if (getHostName() != String.Empty)
         {
            Trace.TraceInformation(String.Format("[MainForm] User decided to Reload List"));

            string oldButtonText = buttonReloadList.Text;
            onUpdating();
            requestUpdates(null, new int[] { Constants.ReloadListPseudoTimerInterval }, () => onUpdated(oldButtonText));
         }
      }

      private void comboBoxDCDepth_SelectedIndexChanged(object sender, EventArgs e)
      {
         Program.Settings.DiffContextDepth = (sender as ComboBox).Text;
      }

      async private void ButtonDiscussions_Click(object sender, EventArgs e)
      {
         Debug.Assert(getMergeRequestKey(null).HasValue);
         Debug.Assert(getMergeRequest(null) != null);

         MergeRequest mergeRequest = getMergeRequest(null);
         MergeRequestKey mrk = getMergeRequestKey(null).Value;

         await showDiscussionsFormAsync(mrk, mergeRequest.Title, mergeRequest.Author);
      }

      private void LinkLabelAbortGit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         MergeRequestKey? mrk = getMergeRequestKey(null);
         if (!mrk.HasValue)
         {
            Debug.Assert(mrk.HasValue);
            return;
         }

         ILocalGitRepository repo = getRepository(mrk.Value.ProjectKey, false);
         if (repo == null || repo.Updater == null || !repo.Updater.CanBeStopped())
         {
            Debug.Assert(mrk.HasValue);
            return;
         }

         string message = String.Format("Do you really want to abort current git update operation for {0}?",
            mrk.Value.ProjectKey.ProjectName);
         if (MessageBox.Show(message, "Confirmation",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
         {
            Trace.TraceInformation(String.Format("[MainForm] User declined to abort current operation for project {0}",
               mrk.Value.ProjectKey.ProjectName));
            return;
         }

         Trace.TraceInformation(String.Format("[MainForm] User decided to abort current operation for project {0}",
            mrk.Value.ProjectKey.ProjectName));
         repo.Updater.StopUpdate();
      }

      private void linkLabelNewVersion_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         if (String.IsNullOrEmpty(_newVersionFilePath))
         {
            Debug.Assert(false);
            return;
         }

         if (MessageBox.Show("Do you want to close the application and install a new version?", "Confirmation",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
         {
            Trace.TraceInformation("[CheckForUpdates] User discarded to install a new version");
            return;
         }

         try
         {
            Process.Start(_newVersionFilePath);
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle("[CheckForUpdates] Cannot launch installer", ex);
         }

         doClose();
      }

      private void doClose()
      {
         Trace.TraceInformation(String.Format("[MainForm] Set _exiting flag"));
         _exiting = true;
         Close();
      }

      private void linkLabelHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         string helpUrl = Program.ServiceManager.GetHelpUrl();
         if (helpUrl != String.Empty)
         {
            openBrowser(helpUrl);
         }
      }

      private void linkLabelSendFeedback_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         try
         {
            if (Program.ServiceManager.GetBugReportEmail() != String.Empty)
            {
               Program.FeedbackReporter.SendEMail("Merge Request Helper Feedback Report",
                  "Please provide your feedback here", Program.ServiceManager.GetBugReportEmail(),
                  Constants.BugReportLogArchiveName);
            }
         }
         catch (FeedbackReporterException ex)
         {
            string message = "Cannot send feedback";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(ex.InnerException?.Message ?? "Unknown error", message,
               MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

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

      private static void setCommitComboboxTooltipText(ComboBox comboBox, ToolTip tooltip)
      {
         tooltip.SetToolTip(comboBox, String.Empty);

         if (comboBox.SelectedItem == null)
         {
            return;
         }

         CommitComboBoxItem item = (CommitComboBoxItem)(comboBox.SelectedItem);
         if (item.Status == ECommitComboBoxItemStatus.Base)
         {
            return;
         }

         tooltip.SetToolTip(comboBox, String.Format("{0}", item.Message));
      }

      private void formatHostListItem(ListControlConvertEventArgs e)
      {
         HostComboBoxItem item = (HostComboBoxItem)(e.ListItem);
         e.Value = item.Host;
      }

      private void onSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         Program.Settings.Update();
      }

      private void onHideToTray()
      {
         if (_requireShowingTooltipOnHideToTray)
         {
            // TODO: Maybe it's a good idea to save the requireShowingTooltipOnHideToTray state
            // so it's only shown once in a lifetime
            _trayIcon.ShowTooltipBalloon(
               new TrayIcon.BalloonText("Information", "I will now live in your tray"));
            _requireShowingTooltipOnHideToTray = false;
         }
         Hide();
      }

      private void onTimer(object sender, EventArgs e)
      {
         if (isTrackingTime())
         {
            updateTotalTime(null, null, null, null);
         }
      }

      private void onTimerCheckForUpdates(object sender, EventArgs e)
      {
         Trace.TraceInformation("[CheckForUpdates] Checking for updates on timer");

         checkForApplicationUpdates();
      }

      private void onStartTimer(ISession session)
      {
         Debug.Assert(!isTrackingTime());

         // Update button text and enabled state
         buttonTimeTrackingStart.Text = buttonStartTimerTrackingText;
         buttonTimeTrackingStart.BackColor = System.Drawing.Color.LightGreen;
         buttonTimeTrackingCancel.Enabled = true;
         buttonTimeTrackingCancel.BackColor = System.Drawing.Color.Tomato;

         // Start timer
         _timeTrackingTimer.Start();

         // Reset and start stopwatch
         Debug.Assert(getMergeRequestKey(null).HasValue);
         _timeTracker = session?.GetTimeTracker(getMergeRequestKey(null).Value);
         _timeTracker?.Start();

         // Take care of controls that 'time tracking' mode shares with normal mode
         updateTotalTime(null, null, null, null);

         updateTrayIcon();
         updateTaskbarIcon();
      }

      async private Task onStopTimer(bool send, ITotalTimeCache totalTimeCache)
      {
         if (!isTrackingTime())
         {
            return;
         }

         // Stop timer
         _timeTrackingTimer.Stop();

         // Reset member right now to not send tracked time again on re-entrance
         ITimeTracker timeTracker = _timeTracker;
         _timeTracker = null;

         // Stop stopwatch and send tracked time
         if (send)
         {
            TimeSpan span = timeTracker.Elapsed;
            if (span.TotalSeconds > 1)
            {
               labelWorkflowStatus.Text = "Sending tracked time...";
               string duration = String.Format("{0}h {1}m {2}s",
                  span.ToString("hh"), span.ToString("mm"), span.ToString("ss"));
               string status = String.Format("Tracked time {0} sent successfully", duration);
               try
               {
                  await timeTracker.Stop();
               }
               catch (TimeTrackerException ex)
               {
                  status = "Error occurred. Tracked time is not sent!";
                  ExceptionHandlers.Handle(status, ex);
                  MessageBox.Show(status, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
               labelWorkflowStatus.Text = status;
            }
            else
            {
               labelWorkflowStatus.Text = "Tracked time less than 1 second is ignored";
            }
         }
         else
         {
            timeTracker.Cancel();
            labelWorkflowStatus.Text = "Time tracking cancelled";
         }

         // Update button text and enabled state
         buttonTimeTrackingStart.Text = buttonStartTimerDefaultText;
         buttonTimeTrackingStart.BackColor = System.Drawing.Color.Transparent;
         buttonTimeTrackingCancel.Enabled = false;
         buttonTimeTrackingCancel.BackColor = System.Drawing.Color.Transparent;

         // Show actual merge request details
         bool isMergeRequestSelected = getMergeRequest(null) != null && getMergeRequestKey(null).HasValue;
         if (isMergeRequestSelected)
         {
            MergeRequest mergeRequest = getMergeRequest(null);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;

            updateTimeTrackingMergeRequestDetails(true, mergeRequest.Title, mrk.ProjectKey, mergeRequest.Author);

            // Take care of controls that 'time tracking' mode shares with normal mode
            updateTotalTime(mrk, mergeRequest.Author, mrk.ProjectKey.HostName, totalTimeCache);
         }
         else
         {
            updateTimeTrackingMergeRequestDetails(false, null, default(ProjectKey), null);
            updateTotalTime(null, null, null, null);
         }

         updateTrayIcon();
         updateTaskbarIcon();
      }

      private void onPersistentStorageSerialize(IPersistentStateSetter writer)
      {
         writer.Set("SelectedHost", getHostName());

         Dictionary<string, HashSet<string>> reviewedCommits = _reviewedCommits.ToDictionary(
               item => item.Key.ProjectKey.HostName
               + "|" + item.Key.ProjectKey.ProjectName
               + "|" + item.Key.IId.ToString(),
               item => item.Value);
         writer.Set("ReviewedCommits", reviewedCommits);

         Dictionary<string, string> mergeRequestsByHosts = _lastMergeRequestsByHosts.ToDictionary(
               item => item.Value.ProjectKey.HostName + "|" + item.Value.ProjectKey.ProjectName,
               item => item.Value.IId.ToString());
         writer.Set("MergeRequestsByHosts", mergeRequestsByHosts);
      }

      private void onPersistentStorageDeserialize(IPersistentStateGetter reader)
      {
         string hostname = (string)reader.Get("SelectedHost");
         if (hostname != null)
         {
            _initialHostName = StringUtils.GetHostWithPrefix(hostname);
         }

         JObject reviewedCommitsObj = (JObject)reader.Get("ReviewedCommits");
         Dictionary<string, object> reviewedCommits =
            reviewedCommitsObj.ToObject<Dictionary<string, object>>();
         if (reviewedCommits != null)
         {
            _reviewedCommits = reviewedCommits.ToDictionary(
               item =>
               {
                  string[] splitted = item.Key.Split('|');

                  Debug.Assert(splitted.Length == 3);

                  string host = splitted[0];
                  string projectName = splitted[1];
                  int iid = int.Parse(splitted[2]);
                  return new MergeRequestKey(new ProjectKey(host, projectName), iid);
               },
               item =>
               {
                  HashSet<string> commits = new HashSet<string>();
                  foreach (string commit in (JArray)item.Value)
                  {
                     commits.Add(commit);
                  }
                  return commits;
               });
         }

         JObject lastMergeRequestsByHostsObj = (JObject)reader.Get("MergeRequestsByHosts");
         Dictionary<string, object> lastMergeRequestsByHosts =
            lastMergeRequestsByHostsObj.ToObject<Dictionary<string, object>>();
         if (lastMergeRequestsByHosts != null)
         {
            _lastMergeRequestsByHosts = lastMergeRequestsByHosts.ToDictionary(
               item =>
               {
                  string[] splitted = item.Key.Split('|');
                  Debug.Assert(splitted.Length == 2);
                  return splitted[0];
               },
               item =>
               {
                  string[] splitted = item.Key.Split('|');
                  Debug.Assert(splitted.Length == 2);

                  string hostname2 = StringUtils.GetHostWithPrefix(splitted[0]);
                  string projectname = splitted[1];
                  int iid = int.Parse((string)item.Value);
                  return new MergeRequestKey(new ProjectKey(hostname2, projectname), iid);
               });
         }
      }

      private void comboBoxThemes_SelectionChangeCommitted(object sender, EventArgs e)
      {
         if (comboBoxThemes.SelectedItem == null)
         {
            return;
         }

         string theme = comboBoxThemes.SelectedItem.ToString();
         Program.Settings.VisualThemeName = theme;
         applyTheme(theme);
         resetMinimumSizes();
      }

      private void comboBoxFonts_SelectionChangeCommitted(object sender, EventArgs e)
      {
         if (comboBoxFonts.SelectedItem == null)
         {
            return;
         }

         string font = comboBoxFonts.SelectedItem.ToString();
         Program.Settings.MainWindowFontSizeName = font;
         applyFont(font);
      }

      private void buttonEditProjects_Click(object sender, EventArgs e)
      {
         string host = getHostName();
         if (host == String.Empty)
         {
            return;
         }

         IEnumerable<Tuple<string, bool>> projects = ConfigurationHelper.GetProjectsForHost(host, Program.Settings);
         Debug.Assert(projects != null);

         using (EditOrderedListViewForm form = new EditOrderedListViewForm("Edit Projects",
            "Add project", "Type project name in group/project format",
            projects, new EditProjectsListViewCallback(host, _gitlabClientManager.SearchManager), true))
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               return;
            }

            if (!Enumerable.SequenceEqual(projects, form.Items))
            {
               ConfigurationHelper.SetProjectsForHost(host, form.Items, Program.Settings);
               updateProjectsListView();

               if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
               {
                  Trace.TraceInformation("[MainForm] Reloading merge request list after project list change");
                  switchHostToSelected();
               }
            }
         }
      }

      private void buttonEditUsers_Click(object sender, EventArgs e)
      {
         string host = getHostName();
         if (host == String.Empty)
         {
            return;
         }

         IEnumerable<Tuple<string, bool>> users = ConfigurationHelper.GetUsersForHost(host, Program.Settings);
         Debug.Assert(users != null);

         using (EditOrderedListViewForm form = new EditOrderedListViewForm("Edit Users",
            "Add username", "Type a name of GitLab user, teams allowed",
            users, new EditUsersListViewCallback(host, _gitlabClientManager.SearchManager), false))
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               return;
            }

            if (!Enumerable.SequenceEqual(users, form.Items))
            {
               ConfigurationHelper.SetUsersForHost(host, form.Items, Program.Settings);
               updateUsersListView();

               if (!ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
               {
                  Trace.TraceInformation("[MainForm] Reloading merge request list after user list change");
                  switchHostToSelected();
               }
            }
         }
      }

      private void groupBoxActions_SizeChanged(object sender, EventArgs e)
      {
         repositionCustomCommands(); // update position of custom actions
      }

      private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
      {
         updateMinimumSizes();
      }

      private void tabControlMode_SelectedIndexChanged(object sender, EventArgs e)
      {
         deselectAllListViewItems(listViewMergeRequests);
         deselectAllListViewItems(listViewFoundMergeRequests);

         bool isLiveMode = tabControlMode.SelectedTab == tabPageLive;
         labelTimeTrackingTrackedLabel.Visible = isLiveMode;
         buttonEditTime.Visible = isLiveMode;
         labelWorkflowStatus.Text = String.Empty;
         disableCommonUIControls();
      }

      private void tabControl_Selecting(object sender, TabControlCancelEventArgs e)
      {
         if (!_canSwitchTab)
         {
            e.Cancel = true;
         }
      }

      private void radioButtonMergeRequestSelectingMode_CheckedChanged(object sender, EventArgs e)
      {
         if (!(sender as RadioButton).Checked)
         {
            return;
         }

         listViewUsers.Enabled = radioButtonSelectByUsernames.Checked;
         listViewProjects.Enabled = radioButtonSelectByProjects.Checked;

         if (!_loadingConfiguration)
         {
            if (radioButtonSelectByProjects.Checked)
            {
               ConfigurationHelper.SelectProjectBasedWorkflow(Program.Settings);
            }
            else
            {
               ConfigurationHelper.SelectUserBasedWorkflow(Program.Settings);
            }

            Trace.TraceInformation("[MainForm] Reloading merge request list after mode change");
            switchHostToSelected();
         }
      }

      protected override void OnFontChanged(EventArgs e)
      {
         base.OnFontChanged(e);

         if (!this.Created)
         {
            return;
         }

         Trace.TraceInformation(String.Format("[MainForm] Font changed, new emSize = {0}", this.Font.Size));
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         // see 9b65d7413c
         if (richTextBoxMergeRequestDescription.Location.X < 0
          || richTextBoxMergeRequestDescription.Location.Y < 0)
         {
            Trace.TraceWarning(
                  "Detected negative Location of Html Panel. "
                + "Location: {{{0}, {1}}}, Size: {{{2}, {3}}}. GroupBox Size: {{{4}, {5}}}",
               richTextBoxMergeRequestDescription.Location.X,
               richTextBoxMergeRequestDescription.Location.Y,
               richTextBoxMergeRequestDescription.Size.Width,
               richTextBoxMergeRequestDescription.Size.Height,
               groupBoxSelectedMR.Size.Width,
               groupBoxSelectedMR.Size.Height);
            Debug.Assert(false);
         }

         updateVisibleMergeRequests(); // update row height of List View
         applyTheme(Program.Settings.VisualThemeName); // update CSS in MR Description
         resetMinimumSizes();
      }

      protected override void OnDpiChanged(DpiChangedEventArgs e)
      {
         base.OnDpiChanged(e);

         Trace.TraceInformation(String.Format("[MainForm] DPI changed, new DPI = {0}", this.DeviceDpi));
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         _trayIcon.ShowTooltipBalloon(new TrayIcon.BalloonText
         (
            "System DPI has changed",
            "It is recommended to restart application to update layout"
         ));
      }
   }
}

