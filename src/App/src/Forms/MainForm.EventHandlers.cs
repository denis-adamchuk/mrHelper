using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Client.Types;
using mrHelper.Client.TimeTracking;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.CommonNative;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      /// <summary>
      /// All exceptions thrown within this method are fatal errors, just pass them to upper level handler
      /// </summary>
      async private void MrHelperForm_Load(object sender, EventArgs e)
      {
         Win32Tools.EnableCopyDataMessageHandling(this.Handle);

         cleanUpInstallers();

         addCustomActions();
         loadSettings();
         if (!integrateInTools())
         {
            Close();
            return;
         }

         this.WindowState = FormWindowState.Maximized;

         if (Program.Settings.MainWindowSplitterDistance != 0)
         {
            splitContainer1.SplitterDistance = Program.Settings.MainWindowSplitterDistance;
         }

         if (Program.Settings.RightPaneSplitterDistance != 0)
         {
            splitContainer2.SplitterDistance = Program.Settings.RightPaneSplitterDistance;
         }

         await onApplicationStarted();
      }

      async private void MrHelperForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         if (checkBoxMinimizeOnClose.Checked && !_exiting)
         {
            onHideToTray(e);
         }
         else
         {
            _exiting = true;

            if (_workflow != null)
            {
               Hide();

               Trace.TraceInformation(String.Format("[MainForm] User decided to close the app, finalizing work"));

               try
               {
                  _persistentStorage.Serialize();
               }
               catch (PersistenceStateSerializationException ex)
               {
                  ExceptionHandlers.Handle(ex, "Cannot serialize the state");
               }

               Interprocess.SnapshotSerializer.CleanUpSnapshots();

               e.Cancel = true;
               await _workflow.CancelAsync();
               _workflow = null;
               if (_gitClientFactory != null)
               {
                  await _gitClientFactory.DisposeAsync();
               }

               Trace.TraceInformation(String.Format("[MainForm] Work finalized. Exiting."));

               Close();
            }
         }
      }

      private void NotifyIcon_DoubleClick(object sender, EventArgs e)
      {
         Win32Tools.ForceWindowIntoForeground(this.Handle);
      }

      async private void ButtonDifftool_Click(object sender, EventArgs e)
      {
         await onLaunchDiffToolAsync();
      }

      async private void ButtonAddComment_Click(object sender, EventArgs e)
      {
         await onAddCommentAsync();
      }

      async private void ButtonNewDiscussion_Click(object sender, EventArgs e)
      {
         await onNewDiscussionAsync();
      }

      async private void ButtonTimeTrackingStart_Click(object sender, EventArgs e)
      {
         if (isTrackingTime())
         {
            await onStopTimer(true);
         }
         else
         {
            onStartTimer();
         }
      }

      async private void ButtonTimeTrackingCancel_Click(object sender, EventArgs e)
      {
         Debug.Assert(isTrackingTime());
         await onStopTimer(false);
      }

      async private void ButtonTimeEdit_Click(object sender, EventArgs s)
      {
         if (!TimeSpan.TryParse(labelTimeTrackingTrackedTime.Text, out TimeSpan oldSpan))
         {
            oldSpan = TimeSpan.Zero; // e.g. "Not Started"
         }

         // Store data before opening a modal dialog
         Debug.Assert(getMergeRequestKey().HasValue);
         MergeRequestKey mrk = getMergeRequestKey().Value;

         using (EditTimeForm form = new EditTimeForm(oldSpan))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               TimeSpan newSpan = form.GetTimeSpan();
               bool add = newSpan > oldSpan;
               TimeSpan diff = add ? newSpan - oldSpan : oldSpan - newSpan;
               if (diff != TimeSpan.Zero)
               {
                  await _timeTrackingManager.AddSpanAsync(add, diff, mrk);

                  updateTotalTime(mrk);
                  labelWorkflowStatus.Text = "Total spent time updated";

                  Trace.TraceInformation(String.Format("[MainForm] Total time for MR {0} (project {1}) changed to {2}",
                     mrk.IId, mrk.ProjectKey.ProjectName, diff.ToString()));
               }
            }
         }
      }

      private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         _exiting = true;
         this.Close();
      }

      async private void ButtonBrowseLocalGitFolder_Click(object sender, EventArgs e)
      {
         localGitFolderBrowser.SelectedPath = textBoxLocalGitFolder.Text;
         if (localGitFolderBrowser.ShowDialog() == DialogResult.OK)
         {
            string newFolder = localGitFolderBrowser.SelectedPath;
            Trace.TraceInformation(String.Format("[MainForm] User decided to change parent folder to {0}", newFolder));

            if (getGitClientFactory(newFolder) != null)
            {
               textBoxLocalGitFolder.Text = localGitFolderBrowser.SelectedPath;
               Program.Settings.LocalGitFolder = localGitFolderBrowser.SelectedPath;

               MessageBox.Show("Git folder is changed. It is recommended to restart Diff Tool and"
                             + " reopen Discussions views if you have already opened them",
                  "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

               labelWorkflowStatus.Text = "Parent folder for git repositories changed";
               Trace.TraceInformation(String.Format("[MainForm] Parent folder changed to {0}",
                  newFolder));

               if (getHostName() != String.Empty)
               {
                  // Emulating a host switch here to trigger RevisionCacher to work at the new location
                  Trace.TraceInformation(String.Format("[MainForm] Emulating host switch on parent folder change"));
                  await switchHostAsync(getHostName());
               }
            }
         }
      }

      private void ComboBoxColorSchemes_SelectionChangeCommited(object sender, EventArgs e)
      {
         initializeColorScheme();
         Program.Settings.ColorSchemeFileName = (sender as ComboBox).Text;
      }

      async private void ComboBoxHost_SelectionChangeCommited(object sender, EventArgs e)
      {
         string hostname = (sender as ComboBox).Text;

         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change host to {0}", hostname));

         onHostSelected();
         await switchHostToSelected();
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

      Graphics GetGraphics(DrawItemEventArgs e) => e.Graphics;
      Graphics GetGraphics(DrawListViewSubItemEventArgs e) => e.Graphics;

      Rectangle GetBounds(DrawItemEventArgs e) => e.Bounds;
      Rectangle GetBounds(DrawListViewSubItemEventArgs e) => e.Bounds;

      private void fillRectangle<T>(T e, Color backColor, bool isSelected)
      {
         if (isSelected)
         {
            GetGraphics((dynamic)e).FillRectangle(SystemBrushes.Highlight, GetBounds((dynamic)e));
         }
         else
         {
            using (Brush brush = new SolidBrush(backColor))
            {
               GetGraphics((dynamic)e).FillRectangle(brush, GetBounds((dynamic)e));
            }
         }
      }

      private void ListViewMergeRequests_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
      {
         if (e.Item.ListView == null)
         {
            return; // is being removed
         }

         FullMergeRequestKey fmk = (FullMergeRequestKey)(e.Item.Tag);

         bool isSelected = e.Item.Selected;
         fillRectangle(e, getMergeRequestColor(fmk.MergeRequest, Color.Transparent), isSelected);

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
               e.Graphics.DrawString(text, font, brush, e.Bounds, format);
            }
         }
         else
         {
            if (isSelected && e.ColumnIndex == 3)
            {
               using (Brush brush = new SolidBrush(getMergeRequestColor(fmk.MergeRequest, SystemColors.Window)))
               {
                  e.Graphics.DrawString(text, e.Item.ListView.Font, brush, e.Bounds, format);
               }
            }
            else
            {
               e.Graphics.DrawString(text, e.Item.ListView.Font, textBrush, e.Bounds, format);
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

      async private void ListViewMergeRequests_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
      {
         ListView listView = (sender as ListView);
         listView.Refresh();

         if (listView.SelectedItems.Count < 1)
         {
            // had to use this hack, because it is not possible to prevent deselect on a click on empty area in ListView
            await switchMergeRequestByUserAsync(default(ProjectKey), 0);
            return;
         }

         FullMergeRequestKey key = (FullMergeRequestKey)(listView.SelectedItems[0].Tag);
         if (await switchMergeRequestByUserAsync(key.ProjectKey, key.MergeRequest.IId))
         {
            Debug.Assert(getMergeRequestKey().HasValue);
            _lastMergeRequestsByHosts[key.ProjectKey.HostName] = getMergeRequestKey().Value;
            return;
         }
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
            fillRectangle(e, getCommitComboBoxItemColor(item), isSelected);

            Brush textBrush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawString(formatCommitComboboxItem(item), comboBox.Font, textBrush, e.Bounds);
         }

         e.DrawFocusRectangle();
      }

      private void ComboBoxLeftCommit_SelectedIndexChanged(object sender, EventArgs e)
      {
         checkComboboxCommitsOrder(true /* I'm left one */);
         setCommitComboboxTooltipText(sender as ComboBox, toolTip);
         setCommitComboboxLabels(sender as ComboBox, labelLeftCommitTimestamp);
      }

      private void ComboBoxRightCommit_SelectedIndexChanged(object sender, EventArgs e)
      {
         checkComboboxCommitsOrder(false /* because I'm the right one */);
         setCommitComboboxTooltipText(sender as ComboBox, toolTip);
         setCommitComboboxLabels(sender as ComboBox, labelRightCommitTimestamp);
      }

      private void ComboBoxHost_Format(object sender, ListControlConvertEventArgs e)
      {
         formatHostListItem(e);
      }

      private void LinkLabelConnectedTo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         openBrowser(linkLabelConnectedTo.Text);
      }

      async private void ButtonAddKnownHost_Click(object sender, EventArgs e)
      {
         using (AddKnownHostForm form = new AddKnownHostForm())
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               return;
            }

            string hostname = getHostWithPrefix(form.Host);
            if (!addKnownHost(hostname, form.AccessToken))
            {
               MessageBox.Show("Such host is already in the list", "Host will not be added",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
               return;
            }

            Program.Settings.KnownHosts = listViewKnownHosts.Items.Cast<ListViewItem>().Select(i => i.Text).ToArray();
            Program.Settings.KnownAccessTokens = listViewKnownHosts.Items.Cast<ListViewItem>()
               .Select(i => i.SubItems[1].Text).ToArray();

            updateHostsDropdownList();
            selectHost(PreferredSelection.Latest);
            await switchHostToSelected();
         }
      }

      async private void ButtonRemoveKnownHost_Click(object sender, EventArgs e)
      {
         bool removeCurrent =
               listViewKnownHosts.SelectedItems.Count > 0 && getHostName() != String.Empty
            && getHostName() == listViewKnownHosts.SelectedItems[0].Text;

         if (!removeKnownHost())
         {
            return;
         }

         Program.Settings.KnownHosts = listViewKnownHosts.Items.Cast<ListViewItem>().Select(i => i.Text).ToArray();
         Program.Settings.KnownAccessTokens = listViewKnownHosts.Items.Cast<ListViewItem>()
            .Select(i => i.SubItems[1].Text).ToArray();

         updateHostsDropdownList();
         if (removeCurrent)
         {
            selectHost(PreferredSelection.Latest);
            await switchHostToSelected();
         }
      }

      private void CheckBoxMinimizeOnClose_CheckedChanged(object sender, EventArgs e)
      {
         Program.Settings.MinimizeOnClose = (sender as CheckBox).Checked;
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
         Dictionary<string, int> columnWidths = new Dictionary<string, int>();
         foreach (ColumnHeader column in listViewMergeRequests.Columns)
         {
            columnWidths[(string)column.Tag] = column.Width;
         }
         Program.Settings.ListViewMergeRequestsColumnWidths = columnWidths;
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

      private void textBoxLabels_TextChanged(object sender, EventArgs e)
      {
         onTextBoxLabelsUpdate();
      }

      private void TextBoxLabels_LostFocus(object sender, EventArgs e)
      {
         onTextBoxLabelsUpdate();
      }

      private void onTextBoxLabelsUpdate()
      {
         Program.Settings.LastUsedLabels = textBoxLabels.Text;

         if (Program.Settings.CheckedLabelsFilter)
         {
            updateVisibleMergeRequests();
         }
      }

      private void CheckBoxLabels_CheckedChanged(object sender, EventArgs e)
      {
         Program.Settings.CheckedLabelsFilter = (sender as CheckBox).Checked;

         if (_workflow != null)
         {
            updateVisibleMergeRequests();
         }
      }

      async private void ButtonReloadList_Click(object sender, EventArgs e)
      {
         if (getHostName() != String.Empty)
         {
            Trace.TraceInformation(String.Format("[MainForm] User decided to Reload List"));
            await switchHostToSelected();
         }
      }

      private void comboBoxDCDepth_SelectedIndexChanged(object sender, EventArgs e)
      {
         Program.Settings.DiffContextDepth = (sender as ComboBox).Text;
      }

      async private void ButtonDiscussions_Click(object sender, EventArgs e)
      {
         await showDiscussionsFormAsync();
      }

      async private void LinkLabelAbortGit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         Trace.TraceInformation("[MainForm] User decided to abort git update");

         Debug.Assert(getMergeRequestKey().HasValue);

         GitClient client = await getGitClient(getMergeRequestKey().Value.ProjectKey, false);
         if (client == null)
         {
            return;
         }

         await client.Updater.CancelUpdateAsync();
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
            ExceptionHandlers.Handle(ex, "[CheckForUpdates] Cannot launch installer");
         }

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
            ExceptionHandlers.Handle(ex, "Cannot send feedback");
         }
      }

      protected override void WndProc(ref Message rMessage)
      {
         if (rMessage.Msg == NativeMethods.WM_COPYDATA)
         {
            string argumentString = Win32Tools.ConvertMessageToText(rMessage.LParam);

            BeginInvoke(new Action(
               async () =>
               {
                  string[] arguments = argumentString.Split('|');
                  if (arguments.Length < 2)
                  {
                     Debug.Assert(false);
                     Trace.TraceError(String.Format("Invalid WM_COPYDATA message content: {0}", argumentString));
                     return;
                  }

                  if (arguments[1] == "diff")
                  {
                     await onDiffCommand(argumentString);
                  }
                  else
                  {
                     await onOpenCommand(argumentString);
                  }
               }));
         }

         base.WndProc(ref rMessage);
      }

      private static string formatCommitComboboxItem(CommitComboBoxItem item)
      {
         return item.Text + (item.IsLatest ? " [Latest]" : String.Empty);
      }

      private static void setCommitComboboxTooltipText(ComboBox comboBox, ToolTip tooltip)
      {
         tooltip.SetToolTip(comboBox, String.Empty);

         if (comboBox.SelectedItem == null)
         {
            return;
         }

         CommitComboBoxItem item = (CommitComboBoxItem)(comboBox.SelectedItem);
         if (item.IsBase)
         {
            return;
         }

         tooltip.SetToolTip(comboBox, String.Format("{0}", item.Message));
      }

      private static void setCommitComboboxLabels(ComboBox comboBox, Label labelTimestamp)
      {
         labelTimestamp.Text = "N/A";

         if (comboBox.SelectedItem == null)
         {
            return;
         }

         CommitComboBoxItem item = (CommitComboBoxItem)(comboBox.SelectedItem);
         if (item.IsBase)
         {
            return;
         }

         if (item.TimeStamp != null)
         {
            labelTimestamp.Text = String.Format("{0}", item.TimeStamp.Value.ToLocalTime().ToString());
         }
      }

      private void formatHostListItem(ListControlConvertEventArgs e)
      {
         HostComboBoxItem item = (HostComboBoxItem)(e.ListItem);
         e.Value = item.Host;
      }

      private static void formatProjectsListItem(ListControlConvertEventArgs e)
      {
         Project item = (Project)(e.ListItem);
         e.Value = item.Path_With_Namespace;
      }

      private void onSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         Program.Settings.Update();
      }

      private void onHideToTray(FormClosingEventArgs e)
      {
         e.Cancel = true;
         if (_requireShowingTooltipOnHideToTray)
         {
            // TODO: Maybe it's a good idea to save the requireShowingTooltipOnHideToTray state
            // so it's only shown once in a lifetime
            _trayIcon.ShowTooltipBalloon(
               new TrayIcon.BalloonText { Title = "Information", Text = "I will now live in your tray" });
            _requireShowingTooltipOnHideToTray = false;
         }
         Hide();
      }

      private void onTimer(object sender, EventArgs e)
      {
         if (_timeTracker != null)
         {
            labelTimeTrackingTrackedTime.Text = _timeTracker.Elapsed.ToString(@"hh\:mm\:ss");
         }
      }

      private void onTimerCheckForUpdates(object sender, EventArgs e)
      {
         Trace.TraceInformation("[CheckForUpdates] Checking for updates on timer");

         checkForApplicationUpdates();
      }

      private void onStartTimer()
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
         Debug.Assert(getMergeRequestKey().HasValue);
         _timeTracker = _timeTrackingManager.GetTracker(getMergeRequestKey().Value);
         _timeTracker.Start();

         // Take care of controls that 'time tracking' mode shares with normal mode
         updateTotalTime(null);
         labelTimeTrackingTrackedTime.Text = labelSpentTimeDefaultText;

         updateTrayIcon();
      }

      async private Task onStopTimer(bool send)
      {
         if (!isTrackingTime())
         {
            return;
         }

         // Stop timer
         _timeTrackingTimer.Stop();

         // Reset member right now to not send tracked time again on re-entrance
         TimeTracker timeTracker = _timeTracker;
         _timeTracker = null;

         // Stop stopwatch and send tracked time
         if (send)
         {
            TimeSpan span = timeTracker.Elapsed;
            if (span.TotalSeconds > 1)
            {
               labelWorkflowStatus.Text = "Sending tracked time...";
               string duration = span.ToString("hh") + "h " + span.ToString("mm") + "m " + span.ToString("ss") + "s";
               string status = String.Format("Tracked time {0} sent successfully", duration);
               try
               {
                  await timeTracker.StopAsync();
               }
               catch (TimeTrackerException)
               {
                  status = "Error occurred. Tracked time is not sent!";
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
         bool isMergeRequestSelected = listViewMergeRequests.SelectedItems.Count > 0;
         updateTimeTrackingMergeRequestDetails(
            isMergeRequestSelected ? getMergeRequest().Value : new Nullable<MergeRequest>());

         // Take care of controls that 'time tracking' mode shares with normal mode
         updateTotalTime(isMergeRequestSelected ? getMergeRequestKey().Value : new Nullable<MergeRequestKey>());

         updateTrayIcon();
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
            _initialHostName = hostname;
         }

         Dictionary<string, object> reviewedCommits = (Dictionary<string, object>)reader.Get("ReviewedCommits");
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
                  return new MergeRequestKey
                  {
                     ProjectKey = new ProjectKey { HostName = host, ProjectName = projectName },
                     IId = iid
                  };
               },
               item =>
               {
                  HashSet<string> commits = new HashSet<string>();
                  foreach (string commit in (ArrayList)item.Value)
                  {
                     commits.Add(commit);
                  }
                  return commits;
               });
         }

         Dictionary<string, object> lastMergeRequestsByHosts =
            (Dictionary<string, object>)reader.Get("MergeRequestsByHosts");
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

                  string hostname2 = splitted[0];
                  string projectname = splitted[1];
                  int iid = int.Parse((string)item.Value);
                  return new MergeRequestKey
                  {
                     ProjectKey = new ProjectKey { HostName = hostname2, ProjectName = projectname },
                     IId = iid
                  };
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

      async private void buttonEditProjects_Click(object sender, EventArgs e)
      {
         string host = getHostName();
         if (host == String.Empty)
         {
            return;
         }

         IEnumerable<Tuple<string, bool>> projects = ConfigurationHelper.GetProjectsForHost(host, Program.Settings);
         Debug.Assert(projects != null);

         using (EditProjectsForm form = new EditProjectsForm(projects))
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               return;
            }

            if (!Enumerable.SequenceEqual(projects, form.Projects))
            {
               ConfigurationHelper.SetProjectsForHost(host, form.Projects, Program.Settings);
               updateProjectsListView();

               Trace.TraceInformation(String.Format("[MainForm] Reloading merge request list after project list change"));
               await switchHostToSelected();
            }
         }
      }

      private void groupBoxActions_SizeChanged(object sender, EventArgs e)
      {
         repositionCustomCommands(); // update position of custom actions
      }

      protected override void OnFontChanged(EventArgs e)
      {
         base.OnFontChanged(e);

         repositionCustomCommands(); // update position of custom actions
         updateVisibleMergeRequests(); // update row height of List View
         applyTheme(Program.Settings.VisualThemeName); // update CSS in MR Description
      }

      private void groupBoxTimeTracking_SizeChanged(object sender, EventArgs e)
      {
         labelTimeTrackingMergeRequestName.Width = groupBoxTimeTracking.Width;
      }
   }
}

