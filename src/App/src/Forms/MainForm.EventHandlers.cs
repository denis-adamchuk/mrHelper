using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Persistence;
using mrHelper.Client.TimeTracking;
using mrHelper.Core.Interprocess;
using mrHelper.Client.Discussions;
using mrHelper.Client.Workflow;
using mrHelper.Client.Git;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      /// <summary>
      /// All exceptions thrown within this method are fatal errors, just pass them to upper level handler
      /// </summary>
      async private void MrHelperForm_Load(object sender, EventArgs e)
      {
         CommonTools.Win32Tools.EnableCopyDataMessageHandling(this.Handle);

         loadSettings();
         addCustomActions();
         integrateInTools();
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
            if (_workflow != null)
            {
               try
               {
                  _persistentStorage.Serialize();
               }
               catch (PersistenceStateSerializationException ex)
               {
                  ExceptionHandlers.Handle(ex, "Cannot serialize the state");
               }

               Core.Interprocess.SnapshotSerializer.CleanUpSnapshots();

               Hide();
               e.Cancel = true;
               await _workflow.CancelAsync();
               _workflow.Dispose();
               _workflow = null;
               Close();
            }
         }
      }

      private void NotifyIcon_DoubleClick(object sender, EventArgs e)
      {
         ShowInTaskbar = true;
         Show();
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
         // Store data before opening a modal dialog
         MergeRequestDescriptor mrd = _workflow.State.MergeRequestDescriptor;

         TimeSpan oldSpan = TimeSpan.Parse(labelTimeTrackingTrackedTime.Text);
         using (EditTimeForm form = new EditTimeForm(oldSpan))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               TimeSpan newSpan = form.GetTimeSpan();
               bool add = newSpan > oldSpan;
               TimeSpan diff = add ? newSpan - oldSpan : oldSpan - newSpan;
               if (diff != TimeSpan.Zero)
               {
                  await _timeTrackingManager.AddSpanAsync(add, diff, mrd);

                  updateTotalTime(mrd);
                  labelWorkflowStatus.Text = "Total spent time updated";

                  Trace.TraceInformation(String.Format("[MainForm] Total time for MR {0} (project {1}) changed to {2}",
                     mrd.IId, mrd.ProjectName, diff.ToString()));
               }
            }
         }
      }

      private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         _exiting = true;
         this.Close();
      }

      private void ButtonBrowseLocalGitFolder_Click(object sender, EventArgs e)
      {
         localGitFolderBrowser.SelectedPath = textBoxLocalGitFolder.Text;
         if (localGitFolderBrowser.ShowDialog() == DialogResult.OK)
         {
            string newFolder = localGitFolderBrowser.SelectedPath;
            if (getGitClientFactory(newFolder) != null)
            {
               textBoxLocalGitFolder.Text = localGitFolderBrowser.SelectedPath;
               _settings.LocalGitFolder = localGitFolderBrowser.SelectedPath;

               MessageBox.Show("Git folder is changed, but it will not affect already opened Diff Tool and Discussions views",
                  "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

               labelWorkflowStatus.Text = "Parent folder for git repositories changed";
               Trace.TraceInformation(String.Format("[MainForm] Parent folder changed to {0}",
                  newFolder));
            }
         }
      }

      private void ComboBoxColorSchemes_SelectionChangeCommited(object sender, EventArgs e)
      {
         initializeColorScheme();
         _settings.ColorSchemeFileName = (sender as ComboBox).Text;
      }

      async private void ComboBoxHost_SelectionChangeCommited(object sender, EventArgs e)
      {
         string hostname = (sender as ComboBox).Text;
         await changeHostAsync(hostname);
      }

      async private void ComboBoxProjects_SelectionChangeCommited(object sender, EventArgs e)
      {
         string projectname = (sender as ComboBox).Text;
         await changeProjectAsync(projectname);
      }

      async private void ComboBoxFilteredMergeRequests_SelectionChangeCommited(object sender, EventArgs e)
      {
         ComboBox comboBox = (sender as ComboBox);
         MergeRequest mergeRequest = (MergeRequest)comboBox.SelectedItem;

         await changeMergeRequestAsync(mergeRequest.IId);
      }

      private void ComboBoxFilteredMergeRequests_MeasureItem(object sender, System.Windows.Forms.MeasureItemEventArgs e)
      {
         if (e.Index < 0)
         {
            return;
         }

         ComboBox comboBox = sender as ComboBox;
         e.ItemHeight = comboBox.Font.Height * 2 + 2;
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

      private void fillComboboxItemRectangle(DrawItemEventArgs e, Color backColor, bool isSelected)
      {
         if (isSelected)
         {
            e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
         }
         else
         {
            using (Brush brush = new SolidBrush(backColor))
            {
               e.Graphics.FillRectangle(brush, e.Bounds);
            }
         }
      }

      private void ComboBoxFilteredMergeRequests_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
      {
         if (e.Index < 0)
         {
            return;
         }

         ComboBox comboBox = sender as ComboBox;
         MergeRequest mergeRequest = (MergeRequest)(comboBox.Items[e.Index]);

         e.DrawBackground();

         if ((e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit)
         {
            drawComboBoxEdit(e, comboBox, getMergeRequestColor(mergeRequest), mergeRequest.Title);
         }
         else
         {
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            fillComboboxItemRectangle(e, getMergeRequestColor(mergeRequest), isSelected);

            string labels = String.Join(", ", mergeRequest.Labels.ToArray());
            string authorText = "Author: " + mergeRequest.Author.Name;
            Brush textBrush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (Font boldFont = new Font(comboBox.Font, FontStyle.Bold))
            {
               // first row
               e.Graphics.DrawString(mergeRequest.Title, boldFont, textBrush, new PointF(e.Bounds.X, e.Bounds.Y));

               // second row
               SizeF authorTextSize = e.Graphics.MeasureString(authorText, comboBox.Font);

               e.Graphics.DrawString(authorText, comboBox.Font, textBrush,
                  new PointF(e.Bounds.X, e.Bounds.Y + e.Bounds.Height / (float)2));

               e.Graphics.DrawString(" [" + labels + "]", comboBox.Font, textBrush,
                  new PointF(e.Bounds.X + authorTextSize.Width, e.Bounds.Y + e.Bounds.Height / (float)2));
            }
         }

         e.DrawFocusRectangle();
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
            fillComboboxItemRectangle(e, getCommitComboBoxItemColor(item), isSelected);

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
      }

      private void ComboBoxRightCommit_SelectedIndexChanged(object sender, EventArgs e)
      {
         checkComboboxCommitsOrder(false /* because I'm the right one */);
         setCommitComboboxTooltipText(sender as ComboBox, toolTip);
      }

      private void ComboBoxHost_Format(object sender, ListControlConvertEventArgs e)
      {
         formatHostListItem(e);
      }

      private void ComboBoxProjects_Format(object sender, ListControlConvertEventArgs e)
      {
         formatProjectsListItem(e);
      }

      private void LinkLabelConnectedTo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         try
         {
            // this should open a browser
            Process.Start(linkLabelConnectedTo.Text);
         }
         catch (Exception ex) // see Process.Start exception list
         {
            ExceptionHandlers.Handle(ex, "Cannot open URL");
            MessageBox.Show("Cannot open URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      async private void ButtonAddKnownHost_Click(object sender, EventArgs e)
      {
         using (AddKnownHostForm form = new AddKnownHostForm())
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (!onAddKnownHost(form.Host, form.AccessToken))
               {
                  MessageBox.Show("Such host is already in the list", "Host will not be added",
                     MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  return;
               }

               _settings.KnownHosts = listViewKnownHosts.Items.Cast<ListViewItem>().Select(i => i.Text).ToList();
               _settings.KnownAccessTokens = listViewKnownHosts.Items.Cast<ListViewItem>()
                  .Select(i => i.SubItems[1].Text).ToList();

               await changeHostAsync(getInitialHostName());
            }
         }
      }

      async private void ButtonRemoveKnownHost_Click(object sender, EventArgs e)
      {
         if (onRemoveKnownHost())
         {
            _settings.KnownHosts = listViewKnownHosts.Items.Cast<ListViewItem>().Select(i => i.Text).ToList();
            _settings.KnownAccessTokens = listViewKnownHosts.Items.Cast<ListViewItem>()
               .Select(i => i.SubItems[1].Text).ToList();

            await changeHostAsync(getInitialHostName());
         }
      }

      async private void CheckBoxShowPublicOnly_CheckedChanged(object sender, EventArgs e)
      {
         _settings.ShowPublicOnly = (sender as CheckBox).Checked;

         if (_workflow != null)
         {
            // emulate host change to reload project list
            await changeHostAsync(_workflow.State.HostName);
         }
      }

      private void CheckBoxMinimizeOnClose_CheckedChanged(object sender, EventArgs e)
      {
         _settings.MinimizeOnClose = (sender as CheckBox).Checked;
      }

      async private void TextBoxLabels_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyData == Keys.Enter)
         {
            await onTextBoxLabelsUpdate();
         }
      }

      async private void TextBoxLabels_LostFocus(object sender, EventArgs e)
      {
         await onTextBoxLabelsUpdate();
      }

      async private Task onTextBoxLabelsUpdate()
      {
         _settings.LastUsedLabels = textBoxLabels.Text;

         if (_workflow != null && _settings.CheckedLabelsFilter)
         {
            // emulate project change to reload merge request list
            await changeProjectAsync(_workflow.State.Project.Path_With_Namespace);
         }
      }

      async private void CheckBoxLabels_CheckedChanged(object sender, EventArgs e)
      {
         _settings.CheckedLabelsFilter = (sender as CheckBox).Checked;

         if (_workflow != null)
         {
            // emulate project change to reload merge request list
            await changeProjectAsync(_workflow.State.Project.Path_With_Namespace);
         }
      }

      private void comboBoxDCDepth_SelectedIndexChanged(object sender, EventArgs e)
      {
         _settings.DiffContextDepth = (sender as ComboBox).Text;
      }

      async private void ButtonDiscussions_Click(object sender, EventArgs e)
      {
         await showDiscussionsFormAsync();
      }

      private void LinkLabelAbortGit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         getGitClient(GetCurrentHostName(), GetCurrentProjectName())?.CancelAsyncOperation();
      }

      protected override void WndProc(ref Message rMessage)
      {
         if (rMessage.Msg == CommonTools.NativeMethods.WM_COPYDATA)
         {
            string argumentString = CommonTools.Win32Tools.ConvertMessageToText(rMessage.LParam);

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
         if (comboBox.SelectedItem == null)
         {
            tooltip.SetToolTip(comboBox, String.Empty);
            return;
         }

         CommitComboBoxItem item = (CommitComboBoxItem)(comboBox.SelectedItem);

         string timestampText = String.Empty;
         if (item.TimeStamp != null)
         {
            timestampText = String.Format("({0})", item.TimeStamp.Value.ToLocalTime().ToString());
         }
         string tooltipText = String.Format("{0} {1} {2}",
            item.Text, timestampText, (item.IsLatest ? "[Latest]" : String.Empty));

         tooltip.SetToolTip(comboBox, tooltipText);
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
         _settings.Update();
      }

      private void onHideToTray(FormClosingEventArgs e)
      {
         e.Cancel = true;
         if (_requireShowingTooltipOnHideToTray)
         {
            // TODO: Maybe it's a good idea to save the requireShowingTooltipOnHideToTray state
            // so it's only shown once in a lifetime
            showTooltipBalloon("Information", "I will now live in your tray");
            _requireShowingTooltipOnHideToTray = false;
         }
         Hide();
         ShowInTaskbar = false;
      }

      private void onTimer(object sender, EventArgs e)
      {
         if (_timeTracker != null)
         {
            labelTimeTrackingTrackedTime.Text = _timeTracker.Elapsed.ToString(@"hh\:mm\:ss");
         }
      }

      private bool onAddKnownHost(string host, string accessToken)
      {
         if (!addKnownHost(host, accessToken))
         {
            return false;
         }

         updateHostsDropdownList();
         return true;
      }

      private bool onRemoveKnownHost()
      {
         if (listViewKnownHosts.SelectedItems.Count > 0)
         {
            Trace.TraceInformation(String.Format("[MainForm] Removing host name {0}",
               listViewKnownHosts.SelectedItems[0].ToString()));

            listViewKnownHosts.Items.Remove(listViewKnownHosts.SelectedItems[0]);
            updateHostsDropdownList();
            return true;
         }
         return false;
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
         Debug.Assert(_workflow.State.MergeRequestDescriptor.IId != default(MergeRequest).IId);
         _timeTracker = _timeTrackingManager.GetTracker(_workflow.State.MergeRequestDescriptor);
         _timeTracker.Start();

         // Take care of controls that 'time tracking' mode shares with normal mode
         updateTotalTime(null);
         labelTimeTrackingTrackedTime.Text = labelSpentTimeDefaultText;
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
         bool isMergeRequestSelected = _workflow.State.MergeRequest.IId != default(MergeRequest).IId;
         updateTimeTrackingMergeRequestDetails(
            isMergeRequestSelected ? _workflow.State.MergeRequest : new Nullable<MergeRequest>());

         // Take care of controls that 'time tracking' mode shares with normal mode
         updateTotalTime(isMergeRequestSelected ?
            _workflow.State.MergeRequestDescriptor : new Nullable<MergeRequestDescriptor>());
      }

      private void onPersistentStorageSerialize(IPersistentStateSetter writer)
      {
         writer.Set("SelectedHost", _workflow.State.HostName);

         Dictionary<string, HashSet<string>> reviewedCommits = _reviewedCommits.ToDictionary(
               item => item.Key.HostName + "|" + item.Key.ProjectName + "|" + item.Key.IId.ToString(),
               item => item.Value);
         writer.Set("ReviewedCommits", reviewedCommits);
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
                  return new MergeRequestDescriptor{ HostName = host, ProjectName = projectName, IId = iid };
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
      }
   }
}

