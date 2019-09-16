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
using mrHelper.CustomActions;
using mrHelper.Common.Interfaces;
using mrHelper.Core;
using mrHelper.Client;
using mrHelper.App.Controls;
using mrHelper.Client.Updates;
using mrHelper.Client.Tools;
using mrHelper.Client.Git;
using System.Drawing;
using mrHelper.App.Helpers;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      /// <summary>
      /// Populates host list with list of known hosts from Settings
      /// </summary>
      private void updateHostsDropdownList()
      {
         if (listViewKnownHosts.Items.Count == 0)
         {
            disableComboBox(comboBoxHost, String.Empty);
            return;
         }
         enableComboBox(comboBoxHost);

         comboBoxHost.SelectedIndex = -1;
         comboBoxHost.Items.Clear();

         foreach (ListViewItem item in listViewKnownHosts.Items)
         {
            HostComboBoxItem hostItem = new HostComboBoxItem
            {
               Host = item.Text,
               AccessToken = item.SubItems[1].Text
            };
            comboBoxHost.Items.Add(hostItem);
         }
      }

      private void checkComboboxCommitsOrder(bool shouldReorderRightCombobox)
      {
         if (comboBoxLeftCommit.SelectedItem == null || comboBoxRightCommit.SelectedItem == null)
         {
            return;
         }

         // Left combobox cannot select a commit older than in right combobox (replicating gitlab web ui behavior)
         CommitComboBoxItem leftItem = (CommitComboBoxItem)(comboBoxLeftCommit.SelectedItem);
         CommitComboBoxItem rightItem = (CommitComboBoxItem)(comboBoxRightCommit.SelectedItem);
         Debug.Assert(leftItem.TimeStamp.HasValue);

         if (rightItem.TimeStamp.HasValue)
         {
            // Check if order is broken
            if (leftItem.TimeStamp.Value <= rightItem.TimeStamp.Value)
            {
               if (shouldReorderRightCombobox)
               {
                  comboBoxRightCommit.SelectedIndex = comboBoxLeftCommit.SelectedIndex;
               }
               else
               {
                  comboBoxLeftCommit.SelectedIndex = comboBoxRightCommit.SelectedIndex;
               }
            }
         }
         else
         {
            // It is ok because a commit w/o timestamp is the oldest one
         }
      }

      private string getGitTag(bool left)
      {
         // swap sides to be consistent with gitlab web ui
         if (!left)
         {
            Debug.Assert(comboBoxLeftCommit.SelectedItem != null);
            return ((CommitComboBoxItem)comboBoxLeftCommit.SelectedItem).SHA;
         }
         else
         {
            Debug.Assert(comboBoxRightCommit.SelectedItem != null);
            return ((CommitComboBoxItem)comboBoxRightCommit.SelectedItem).SHA;
         }
      }

      private void showTooltipBalloon(string title, string text)
      {
         notifyIcon.BalloonTipTitle = title;
         notifyIcon.BalloonTipText = text;
         notifyIcon.ShowBalloonTip(notifyTooltipTimeout);

         Trace.TraceInformation(String.Format("Tooltip: Title \"{0}\" Text \"{1}\"", title, text)); 
      }

      private bool addKnownHost(string host, string accessToken)
      {
         Trace.TraceInformation(String.Format("[MainForm] Adding host {0} with token {1}",
            host, accessToken));

         foreach (ListViewItem listItem in listViewKnownHosts.Items)
         {
            if (listItem.Text == host)
            {
               Trace.TraceInformation(String.Format("[MainForm] Host name already exists"));
               return false;
            }
         }

         var item = new ListViewItem(getHostWithPrefix(host));
         item.SubItems.Add(accessToken);
         listViewKnownHosts.Items.Add(item);
         return true;
      }

      private string getHostWithPrefix(string host)
      {
         if (!host.StartsWith("http://") && !host.StartsWith("https://"))
         {
            return "https://" + host;
         }
         return host;
      }

      private string getDefaultColorSchemeFileName()
      {
         return String.Format("{0}.{1}", DefaultColorSchemeName, ColorSchemeFileNamePrefix);
      }

      private void fillColorSchemesList()
      {
         string defaultFileName = getDefaultColorSchemeFileName();
         string defaultFilePath = Path.Combine(Directory.GetCurrentDirectory(), defaultFileName);

         comboBoxColorSchemes.Items.Clear();

         string selectedScheme = null;
         string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
         if (files.Contains(defaultFilePath))
         {
            // put Default scheme first in the list
            comboBoxColorSchemes.Items.Add(defaultFileName);
         }

         foreach (string file in files)
         {
            if (file.EndsWith(ColorSchemeFileNamePrefix))
            {
               string scheme = Path.GetFileName(file);
               if (file != defaultFilePath)
               {
                  comboBoxColorSchemes.Items.Add(scheme);
               }
               if (scheme == _settings.ColorSchemeFileName)
               {
                  selectedScheme = scheme;
               }
            }
         }

         if (selectedScheme != null)
         {
            comboBoxColorSchemes.SelectedItem = selectedScheme;
         }
         else if (comboBoxColorSchemes.Items.Count > 0)
         {
            comboBoxColorSchemes.SelectedIndex = 0;
         }
      }

      private void initializeColorScheme()
      {
         Func<string, bool> createColorScheme =
            (filename) =>
         {
            try
            {
               _colorScheme = new ColorScheme(filename, _expressionResolver);
               return true;
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle(ex, "Cannot create a color scheme");
            }
            return false;
         };

         if (comboBoxColorSchemes.SelectedIndex < 0 || comboBoxColorSchemes.Items.Count < 1)
         {
            // nothing is selected or list is empty, create an empty scheme
            _colorScheme = new ColorScheme();
         }

         // try to create a scheme for the selected item
         else if (!createColorScheme(comboBoxColorSchemes.Text))
         {
            _colorScheme = new ColorScheme();
            MessageBox.Show("Cannot initialize color scheme", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void disableComboBox(SelectionPreservingComboBox comboBox, string text)
      {
         comboBox.DroppedDown = false;
         comboBox.SelectedIndex = -1;
         comboBox.Items.Clear();
         comboBox.Enabled = false;

         comboBox.DropDownStyle = ComboBoxStyle.DropDown;
         comboBox.Text = text;
      }

      private void enableComboBox(SelectionPreservingComboBox comboBox)
      {
         comboBox.Enabled = true;
         comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      }

      private void enableControlsOnGitAsyncOperation(bool enabled)
      {
         linkLabelAbortGit.Visible = !enabled;
         buttonDiffTool.Enabled = enabled;
         buttonDiscussions.Enabled = enabled;
         comboBoxHost.Enabled = enabled;
         comboBoxProjects.Enabled = enabled;
         comboBoxFilteredMergeRequests.Enabled = enabled;
         buttonApplyLabels.Enabled = enabled;
         tabPageSettings.Controls.Cast<Control>().ToList().ForEach((x) => x.Enabled = enabled);

         if (enabled)
         {
            updateGitStatusText(String.Empty);
         }
      }

      private void enableMergeRequestFilterControls(bool enabled)
      {
         buttonApplyLabels.Enabled = enabled;
         checkBoxLabels.Enabled = enabled;
         textBoxLabels.Enabled = enabled;
      }

      private void updateMergeRequestDetails(MergeRequest? mergeRequest)
      {
         toolTip.SetToolTip(comboBoxFilteredMergeRequests,
            mergeRequest.HasValue ? formatMergeRequestForDropdown(mergeRequest.Value) : String.Empty);
         richTextBoxMergeRequestDescription.Text =
            mergeRequest.HasValue ? mergeRequest.Value.Description : String.Empty;
         richTextBoxMergeRequestDescription.Update();
         linkLabelConnectedTo.Text = mergeRequest.HasValue ? mergeRequest.Value.Web_Url : String.Empty;
      }

      private void updateTimeTrackingMergeRequestDetails(MergeRequest? mergeRequest)
      {
         if (isTrackingTime())
         {
            return;
         }

         labelTimeTrackingMergeRequestName.Visible = mergeRequest.HasValue;
         buttonTimeTrackingStart.Enabled = mergeRequest.HasValue;

         if (mergeRequest.HasValue)
         {
            labelTimeTrackingMergeRequestName.Text =
               mergeRequest.Value.Title + "   " + "[" + _workflow.State.Project.Path_With_Namespace + "]";
         }
      }

      private void updateTotalTime(MergeRequestDescriptor? mrd)
      {
         if (isTrackingTime())
         {
            labelTimeTrackingTrackedLabel.Text = "Tracked Time:";
            buttonEditTime.Enabled = false;
            return;
         }

         if (!mrd.HasValue)
         {
            labelTimeTrackingTrackedLabel.Text = String.Empty;
            labelTimeTrackingTrackedTime.Text = String.Empty;
            buttonEditTime.Enabled = false;
         }
         else
         {
            labelTimeTrackingTrackedLabel.Text = "Total Time:";
            labelTimeTrackingTrackedTime.Text = _timeTrackingManager.GetTotalTime(mrd.Value).ToString(@"hh\:mm\:ss");
            buttonEditTime.Enabled = true;
         }
      }

      private void enableMergeRequestActions(bool enabled)
      {
         linkLabelConnectedTo.Visible = enabled;
         buttonDiscussions.Enabled = enabled;
         buttonAddComment.Enabled = enabled;
      }

      private void enableCommitActions(bool enabled)
      {
         buttonDiffTool.Enabled = enabled;
         groupBoxActions.Controls.Cast<Control>().ToList().ForEach((x) => x.Enabled = enabled);
      }

      private void addCommitsToComboBoxes(List<Commit> commits, string baseSha, string targetBranch)
      {
         var latest = new CommitComboBoxItem(commits[0])
         {
            IsLatest = true
         };
         comboBoxLeftCommit.Items.Add(latest);
         for (int i = 1; i < commits.Count; i++)
         {
            CommitComboBoxItem item = new CommitComboBoxItem(commits[i]);
            if (comboBoxLeftCommit.Items.Cast<CommitComboBoxItem>().Any(x => x.SHA == item.SHA))
            {
               continue;
            }
            comboBoxLeftCommit.Items.Add(item);
            comboBoxRightCommit.Items.Add(item);
         }

         // Add target branch to the right combo-box
         CommitComboBoxItem targetBranchItem = new CommitComboBoxItem(baseSha, targetBranch + " [Base]", null);
         comboBoxRightCommit.Items.Add(targetBranchItem);

         comboBoxLeftCommit.SelectedIndex = 0;
         comboBoxRightCommit.SelectedIndex = 0;
      }

      private static string formatMergeRequestForDropdown(MergeRequest mergeRequest)
      {
         return String.Format("{0} [{1}] [{2}]",
            mergeRequest.Title, mergeRequest.Author.Username, String.Join(", ", mergeRequest.Labels.ToArray()));
      }

      /// <summary>
      /// Typically called from another thread
      /// </summary>
      private void updateGitStatusText(string text)
      {
         if (labelGitStatus.InvokeRequired)
         {
            UpdateTextCallback fn = new UpdateTextCallback(updateGitStatusText);
            Invoke(fn, new object [] { text });
         }
         else
         {
            labelGitStatus.Text = text;
         }
      }

      private void notifyOnMergeRequestEvent(MergeRequest mergeRequest, string title)
      {
         string projectName = String.Empty;
         foreach (var item in comboBoxProjects.Items)
         {
            Project project = (Project)(item);
            if (project.Id == mergeRequest.Project_Id)
            {
               projectName = project.Path_With_Namespace;
            }
         }

         showTooltipBalloon(title, "\""
            + mergeRequest.Title
            + "\" from "
            + mergeRequest.Author.Name
            + " in project "
            + (projectName == String.Empty ? "N/A" : projectName));
      }

      private void notifyOnMergeRequestUpdates(MergeRequestUpdates updates)
      {
         List<MergeRequest> newMergeRequests = Tools.FilterMergeRequests(updates.NewMergeRequests, _settings);
         foreach (MergeRequest mergeRequest in newMergeRequests)
         {
            notifyOnMergeRequestEvent(mergeRequest, "New merge request");
         }

         List<MergeRequest> updatedMergeRequests = Tools.FilterMergeRequests(updates.UpdatedMergeRequests, _settings);
         foreach (MergeRequest mergeRequest in updatedMergeRequests)
         {
            notifyOnMergeRequestEvent(mergeRequest, "New commit in merge request");
         }
      }

      private GitClientFactory getGitClientFactory(string localFolder)
      {
         if (_gitClientFactory == null || _gitClientFactory.ParentFolder != localFolder)
         {
            _gitClientFactory?.Dispose();

            try
            {
               _gitClientFactory = new GitClientFactory(localFolder, _updateManager.GetProjectWatcher());
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle(ex, String.Format("Cannot create GitClientFactory"));

               try
               {
                  Directory.CreateDirectory(localFolder);
               }
               catch (Exception ex2)
               {
                  string message = String.Format("Cannot create folder \"{0}\"", localFolder);
                  ExceptionHandlers.Handle(ex2, message);
                  MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return null;
               }
            }
         }
         return _gitClientFactory;
      }

      /// <summary>
      /// Make some checks and create a Client
      /// </summary>
      /// <returns>null if could not create a GitClient</returns>
      private GitClient getGitClient(string hostname, string projectname)
      {
         GitClientFactory factory = getGitClientFactory(_settings.LocalGitFolder);
         if (factory == null)
         {
            return null;
         }

         GitClient client;
         try
         {
            client = factory.GetClient(hostname, projectname);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(ex, String.Format("Cannot create GitClient"));
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
         }

         Debug.Assert(client != null);
         return client;
      }

      private string getInitialHostName()
      {
         // If Last Selected Host is in the list, select it as initial host.
         // Otherwise, select the first host from the list.
         for (int iKnownHost = 0; iKnownHost < _settings.KnownHosts.Count; ++iKnownHost)
         {
            if (_settings.KnownHosts[iKnownHost] == _settings.LastSelectedHost)
            {
               return _settings.LastSelectedHost;
            }
         }
         return _settings.KnownHosts.Count > 0 ? _settings.KnownHosts[0] : String.Empty;
      }

      private bool isTrackingTime()
      {
         return _timeTracker != null;
      }

      System.Drawing.Color getMergeRequestColor(MergeRequest mergeRequest)
      {
         foreach (KeyValuePair<string, Color> color in _colorScheme)
         {
            foreach (string label in mergeRequest.Labels)
            {
               string colorName = String.Format("MergeRequests_{{Label:{0}}}", label);
               if (colorName == color.Key)
               {
                  return color.Value;
               }
            }
         }
         return System.Drawing.Color.Transparent;
      }
   }
}

