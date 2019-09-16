using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
using mrHelper.Common.Interfaces;
using mrHelper.Core;
using mrHelper.Client.Tools;
using mrHelper.Client.TimeTracking;
using System.Drawing;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      /// <summary>
      /// All exceptions thrown within this method are fatal errors, just pass them to upper level handler
      /// </summary>
      async private void MrHelperForm_Load(object sender, EventArgs e)
      {
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
            Core.Interprocess.SnapshotSerializer.CleanUpSnapshots();

            if (_workflow != null)
            {
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
                  MergeRequestDescriptor mrd = _workflow.State.MergeRequestDescriptor;

                  await _timeTrackingManager.AddSpanAsync(add, diff, mrd);

                  updateTotalTime(_workflow.State.MergeRequestDescriptor);
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
         _settings.LastSelectedHost = hostname;

         await changeHostAsync(hostname);
      }

      async private void ComboBoxProjects_SelectionChangeCommited(object sender, EventArgs e)
      {
         string projectname = (sender as ComboBox).Text;
         _settings.LastSelectedProject = projectname;

         await changeProjectAsync(projectname);
      }

      async private void ComboBoxFilteredMergeRequests_SelectionChangeCommited(object sender, EventArgs e)
      {
         ComboBox comboBox = (sender as ComboBox);
         MergeRequest mergeRequest = (MergeRequest)comboBox.SelectedItem;

         await changeMergeRequestAsync(mergeRequest.IId);
      }

      private void ComboBoxFilteredMergeRequests_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
      {
         if (e.Index < 0)
         {
            return;
         }

         ComboBox comboBox = sender as ComboBox;
         MergeRequest mergeRequest = (MergeRequest)(comboBox.Items[e.Index]);
         System.Drawing.Color itemBackground = getMergeRequestColor(mergeRequest);
         string itemText = formatMergeRequestForDropdown(mergeRequest);

         e.DrawBackground();

         if ((e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit)
         {
            using (Brush brush = new SolidBrush(Color.FromArgb(225, 225, 225)))
            {
               e.Graphics.FillRectangle(brush, e.Bounds);
            }
            e.Graphics.DrawString(itemText, comboBox.Font, SystemBrushes.ControlText, e.Bounds);
         }
         else if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
         {
            e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
            e.Graphics.DrawString(itemText, comboBox.Font, SystemBrushes.HighlightText, e.Bounds);
         }
         else
         {
            using (Brush brush = new SolidBrush(itemBackground))
            {
               e.Graphics.FillRectangle(brush, e.Bounds);
            }
            e.Graphics.DrawString(itemText, comboBox.Font, SystemBrushes.ControlText, e.Bounds);
         }

         e.DrawFocusRectangle();
      }

      private void ButtonApplyLabels_Click(object sender, EventArgs e)
      {
         _settings.LastUsedLabels = textBoxLabels.Text;
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

      private void ComboBoxCommit_Format(object sender, ListControlConvertEventArgs e)
      {
         formatCommitComboboxItem(e);
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

      private void CheckBoxShowPublicOnly_CheckedChanged(object sender, EventArgs e)
      {
         _settings.ShowPublicOnly = (sender as CheckBox).Checked;
      }

      private void CheckBoxMinimizeOnClose_CheckedChanged(object sender, EventArgs e)
      {
         _settings.MinimizeOnClose = (sender as CheckBox).Checked;
      }

      private void CheckBoxLabels_CheckedChanged(object sender, EventArgs e)
      {
         _settings.CheckedLabelsFilter = (sender as CheckBox).Checked;
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

      private static void formatCommitComboboxItem(ListControlConvertEventArgs e)
      {
         CommitComboBoxItem item = (CommitComboBoxItem)(e.ListItem);
         e.Value = item.Text + (item.IsLatest ? " [Latest]" : String.Empty);
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
         labelTimeTrackingTrackedTime.Text = _timeTracker.Elapsed.ToString(@"hh\:mm\:ss");
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

         // Stop stopwatch and send tracked time
         if (send)
         {
            TimeSpan span = _timeTracker.Elapsed;
            if (span.TotalSeconds > 1)
            {
               labelWorkflowStatus.Text = "Sending tracked time...";
               string duration = span.ToString("hh") + "h " + span.ToString("mm") + "m " + span.ToString("ss") + "s";
               string status = String.Format("Tracked time {0} sent successfully", duration);
               try
               {
                  await _timeTracker.StopAsync();
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
            _timeTracker.Cancel();
            labelWorkflowStatus.Text = "Time tracking cancelled";
         }
         _timeTracker = null;

         // Stop timer
         _timeTrackingTimer.Stop();

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
   }
}

