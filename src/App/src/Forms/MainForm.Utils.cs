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

namespace mrHelper.App.Forms
{
   internal partial class mrHelperForm
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

      private void checkComboboxVersionsOrder(bool shouldReorderRightCombobox)
      {
         if (comboBoxLeftVersion.SelectedItem == null || comboBoxRightVersion.SelectedItem == null)
         {
            return;
         }

         // Left combobox cannot select a version older than in right combobox (replicating gitlab web ui behavior)
         VersionComboBoxItem leftItem = (VersionComboBoxItem)(comboBoxLeftVersion.SelectedItem);
         VersionComboBoxItem rightItem = (VersionComboBoxItem)(comboBoxRightVersion.SelectedItem);
         Debug.Assert(leftItem.TimeStamp.HasValue);

         if (rightItem.TimeStamp.HasValue)
         {
            // Check if order is broken
            if (leftItem.TimeStamp.Value < rightItem.TimeStamp.Value)
            {
               if (shouldReorderRightCombobox)
               {
                  comboBoxRightVersion.SelectedIndex = comboBoxLeftVersion.SelectedIndex;
               }
               else
               {
                  comboBoxLeftVersion.SelectedIndex = comboBoxRightVersion.SelectedIndex;
               }
            }
         }
         else
         {
            // It is ok because a version w/o timestamp is the oldest one
         }
      }

      private string getGitTag(bool left)
      {
         // swap sides to be consistent with gitlab web ui
         if (!left)
         {
            Debug.Assert(comboBoxLeftVersion.SelectedItem != null);
            return ((VersionComboBoxItem)comboBoxLeftVersion.SelectedItem).SHA;
         }
         else
         {
            Debug.Assert(comboBoxRightVersion.SelectedItem != null);
            return ((VersionComboBoxItem)comboBoxRightVersion.SelectedItem).SHA;
         }
      }

      private void showTooltipBalloon(string title, string text)
      {
         notifyIcon.BalloonTipTitle = title;
         notifyIcon.BalloonTipText = text;
         notifyIcon.ShowBalloonTip(notifyTooltipTimeout);
      }

      private bool addKnownHost(string host, string accessToken)
      {
         foreach (ListViewItem listItem in listViewKnownHosts.Items)
         {
            if (listItem.Text == host)
            {
               return false;
            }
         }

         // Add prefix automatically
         if (!host.StartsWith("http://") && !host.StartsWith("https://"))
         {
            host = "https://" + host;
         }

         var item = new ListViewItem(host);
         item.SubItems.Add(accessToken);
         listViewKnownHosts.Items.Add(item);
         return true;
      }

      private void fillColorSchemesList()
      {
         comboBoxColorSchemes.Items.Clear();
         comboBoxColorSchemes.Items.Add(DefaultColorSchemeName);

         string selectedScheme = null;
         string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
         foreach (string file in files)
         {
            if (file.EndsWith(ColorSchemeFileNamePrefix))
            {
               string scheme = Path.GetFileName(file);
               comboBoxColorSchemes.Items.Add(scheme);
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
         else
         {
            comboBoxColorSchemes.SelectedIndex = 0;
         }
      }

      private void disableComboBox(SelectionPreservingComboBox comboBox, string text)
      {
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

      private void preGitClientInitialize()
      {
         linkLabelAbortGit.Visible = true;
         buttonDiffTool.Enabled = false;
         buttonDiscussions.Enabled = false;
         comboBoxHost.Enabled = false;
         comboBoxProjects.Enabled = false;
      }

      private void postGitClientInitialize()
      {
         linkLabelAbortGit.Visible = false;
         buttonDiffTool.Enabled = true;
         buttonDiscussions.Enabled = true;
         comboBoxHost.Enabled = true;
         comboBoxProjects.Enabled = true;
         updateGitStatusText(this, String.Empty);
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

      private void enableMergeRequestActions(bool enabled)
      {
         linkLabelConnectedTo.Visible = enabled;
         buttonDiscussions.Enabled = enabled;
         buttonToggleTimer.Enabled = enabled;
         buttonDiffTool.Enabled = enabled;
         enableCustomActions(enabled);
      }

      private void enableCustomActions(bool flag)
      {
         foreach (Control control in groupBoxActions.Controls)
         {
            control.Enabled = flag;
         }
      }

      private void addVersionsToComboBoxes(List<GitLabSharp.Entities.Version> versions, string mrBaseSha, string mrTargetBranch)
      {
         var latest = new VersionComboBoxItem(versions[0]);
         latest.IsLatest = true;
         comboBoxLeftVersion.Items.Add(latest);
         for (int i = 1; i < versions.Count; i++)
         {
            VersionComboBoxItem item = new VersionComboBoxItem(versions[i]);
            if (comboBoxLeftVersion.Items.Cast<VersionComboBoxItem>().Any(x => x.SHA == item.SHA))
            {
               continue;
            }
            comboBoxLeftVersion.Items.Add(item);
            comboBoxRightVersion.Items.Add(item);
         }

         // Add target branch to the right combo-box
         VersionComboBoxItem targetBranch =
            new VersionComboBoxItem(mrBaseSha, mrTargetBranch, null);
         comboBoxRightVersion.Items.Add(targetBranch);

         comboBoxLeftVersion.SelectedIndex = 0;
         comboBoxRightVersion.SelectedIndex = 0;
      }

      private static string formatMergeRequestForDropdown(MergeRequest mergeRequest)
      {
         return mergeRequest.Title + "    " + "[" + mergeRequest.Author.Username + "]";
      }

      /// <summary>
      /// Typically called from another thread
      /// </summary>
      private void updateGitStatusText(object sender, string text)
      {
         if (labelGitStatus.InvokeRequired)
         {
            UpdateTextCallback fn = new UpdateTextCallback(updateGitStatusText);
            Invoke(fn, new object [] { sender, text });
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
         foreach (MergeRequest mergeRequest in updates.NewMergeRequests)
         {
            notifyOnMergeRequestEvent(mergeRequest, "New merge request");
         }

         foreach (MergeRequest mergeRequest in updates.UpdatedMergeRequests)
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
               _gitClientFactory = new GitClientFactory(localFolder);
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
      private GitClient getGitClient()
      {
         GitClientFactory factory = getGitClientFactory(_settings.LocalGitFolder);
         if (factory == null)
         {
            return null;
         }

         GitClient client = null;
         try
         {
            client = factory.GetClient(GetCurrentHostName(), GetCurrentProjectName());
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(ex, String.Format("Cannot create GitClient"));
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
         }

         Debug.Assert(client != null);
         client.OperationStatusChange += updateGitStatusText;

         return client;
      }

      /// <summary>
      /// Bind GitClient to the selected merge request
      /// </summary>
      private void setCommitChecker()
      {
         if (_gitClientFactory == null)
         {
            return;
         }

         getGitClient()?.Updater.SetCommitChecker(
            _updateManager.GetCommitChecker(_workflow.State.MergeRequestDescriptor));
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
   }
}

