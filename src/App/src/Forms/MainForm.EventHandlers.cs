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

namespace mrHelper.App.Forms
{
   internal partial class mrHelperForm
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
         else if (_workflow != null)
         {
            Hide();
            e.Cancel = true;
            await _workflow.CancelAsync();
            _workflow.Dispose();
            _workflow = null;
            Close();
         }
      }

      private void NotifyIcon_DoubleClick(object sender, EventArgs e)
      {
         ShowInTaskbar = true;
         Show();
      }

      private void ButtonDifftool_Click(object sender, EventArgs e)
      {
         onLaunchDiffTool();
      }

      async private void ButtonToggleTimer_Click(object sender, EventArgs e)
      {
         if (_timeTrackingTimer.Enabled)
         {
            await onStopTimer();
         }
         else
         {
            onStartTimer();
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

               updateGitStatusText(this, String.Empty);
            }
         }
      }

      private void ComboBoxColorSchemes_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (comboBoxColorSchemes.SelectedItem.ToString() == DefaultColorSchemeName)
         {
            _colorScheme = new ColorScheme();
            return;
         }

         try
         {
            _colorScheme = new ColorScheme(comboBoxColorSchemes.SelectedItem.ToString());
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle(ex, "Cannot change color scheme");
            MessageBox.Show("Cannot change color scheme", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            comboBoxColorSchemes.SelectedIndex = 0; // recursive
         }

         _settings.ColorSchemeFileName = (sender as ComboBox).Text;
      }

      async private void ComboBoxHost_SelectionChangeCommited(object sender, EventArgs e)
      {
         updateGitStatusText(this, String.Empty);

         string hostname = (sender as ComboBox).Text;
         _settings.LastSelectedHost = hostname;

         Debug.WriteLine("UI: Selected host " + hostname);
         await changeHostAsync(hostname);
      }

      async private void ComboBoxProjects_SelectionChangeCommited(object sender, EventArgs e)
      {
         updateGitStatusText(this, String.Empty);

         string projectname = (sender as ComboBox).Text;
         _settings.LastSelectedProject = projectname;

         Debug.WriteLine("UI: Selected project " + projectname);
         await changeProjectAsync(projectname);
      }

      async private void ComboBoxFilteredMergeRequests_SelectionChangeCommited(object sender, EventArgs e)
      {
         ComboBox comboBox = (sender as ComboBox);
         MergeRequest mergeRequest = (MergeRequest)comboBox.SelectedItem;

         Debug.WriteLine("UI: Selected mr iid " + mergeRequest.IId.ToString());
         await changeMergeRequestAsync(mergeRequest.IId);
      }

      private void ButtonApplyLabels_Click(object sender, EventArgs e)
      {
         _settings.LastUsedLabels = textBoxLabels.Text;
      }

      private void ComboBoxLeftVersion_SelectedIndexChanged(object sender, EventArgs e)
      {
         checkComboboxVersionsOrder(true /* I'm left one */);
      }

      private void ComboBoxRightVersion_SelectedIndexChanged(object sender, EventArgs e)
      {
         checkComboboxVersionsOrder(false /* because I'm the right one */);
      }

      private void ComboBoxHost_Format(object sender, ListControlConvertEventArgs e)
      {
         formatHostListItem(e);
      }

      private void ComboBoxProjects_Format(object sender, ListControlConvertEventArgs e)
      {
         formatProjectsListItem(e);
      }

      private void ComboBoxFilteredMergeRequests_Format(object sender, ListControlConvertEventArgs e)
      {
         formatMergeRequestListItem(e);
      }

      private void ComboBoxVersion_Format(object sender, ListControlConvertEventArgs e)
      {
         formatVersionComboboxItem(e);
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

      private void ButtonAddKnownHost_Click(object sender, EventArgs e)
      {
         AddKnownHostForm form = new AddKnownHostForm();
         if (form.ShowDialog() == DialogResult.OK)
         {
            if (!onAddKnownHost(form.Host, form.AccessToken))
            {
               MessageBox.Show("Such host is already in the list", "Host will not be added",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            _settings.KnownHosts = listViewKnownHosts.Items.Cast<ListViewItem>().Select(i => i.Text).ToList();
            _settings.KnownAccessTokens = listViewKnownHosts.Items.Cast<ListViewItem>()
               .Select(i => i.SubItems[1].Text).ToList();
         }
      }

      private void ButtonRemoveKnownHost_Click(object sender, EventArgs e)
      {
         onRemoveKnownHost();
      }

      private void CheckBoxShowPublicOnly_CheckedChanged(object sender, EventArgs e)
      {
         _settings.ShowPublicOnly = (sender as CheckBox).Checked;
      }

      private void CheckBoxRequireTimer_CheckedChanged(object sender, EventArgs e)
      {
         _settings.RequireTimeTracking = (sender as CheckBox).Checked;
         updateInterprocessSnapshot();
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
         getGitClient()?.CancelAsyncOperation();
      }

      private static void formatMergeRequestListItem(ListControlConvertEventArgs e)
      {
         MergeRequest item = ((MergeRequest)e.ListItem);
         e.Value = formatMergeRequestForDropdown(item);
      }

      private static void formatVersionComboboxItem(ListControlConvertEventArgs e)
      {
         VersionComboBoxItem item = (VersionComboBoxItem)(e.ListItem);
         e.Value = item.Text;
         if(item.IsLatest)
         {
            e.Value = "Latest";
         }
         else if (item.TimeStamp.HasValue)
         {
            e.Value += " (" + item.TimeStamp.Value.ToLocalTime().ToString("g") + ")";
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
         labelSpentTime.Text = _timeTracker.Elapsed.ToString(@"hh\:mm\:ss");
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

      private void onRemoveKnownHost()
      {
         if (listViewKnownHosts.SelectedItems.Count > 0)
         {
            listViewKnownHosts.Items.Remove(listViewKnownHosts.SelectedItems[0]);
         }
         updateHostsDropdownList();
      }

      private void onStartTimer()
      {
         // 1. Update button text
         buttonToggleTimer.Text = buttonStartTimerTrackingText;

         // 2. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 3. Start timer
         _timeTrackingTimer.Start();

         // 4. Reset and start stopwatch
         _timeTracker = _timeTrackingManager.GetTracker(_workflow.State.MergeRequestDescriptor);
         _timeTracker.Start();

         // 5. Update information available to other instances
         updateInterprocessSnapshot();
      }

      async private Task onStopTimer()
      {
         // 1. Stop stopwatch and send tracked time
         TimeSpan span = _timeTracker.Elapsed;
         if (span.Seconds > 1)
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
            }
            labelWorkflowStatus.Text = status;
         }
         _timeTracker = null;

         // 2. Stop timer
         _timeTrackingTimer.Stop();

         // 3. Update information available to other instances
         updateInterprocessSnapshot();

         // 4. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 5. Update button text
         buttonToggleTimer.Text = buttonStartTimerDefaultText;
      }
   }
}

