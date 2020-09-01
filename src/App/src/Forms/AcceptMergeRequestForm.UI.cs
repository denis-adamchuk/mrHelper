using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using mrHelper.Integration.GitUI;

namespace mrHelper.App.Forms
{
   partial class AcceptMergeRequestForm
   {
      private void buttonDiscussions_Click(object sender, EventArgs e)
      {
         BeginInvoke(new Action(async () => await _onOpenDiscussions?.Invoke(_mergeRequestKey, _title, _author)));
      }

      async private void buttonToggleWIP_Click(object sender, EventArgs e)
      {
         setTitle(StringUtils.ToggleWorkInProgressTitle(_title));
         try
         {
            buttonToggleWIP.Enabled = false;
            MergeRequest mergeRequest = await toggleWipAsync();
            applyMergeRequest(mergeRequest);
         }
         catch (MergeRequestEditorException ex)
         {
            reportErrorToUser(ex);
         }
      }

      async private void buttonRebase_Click(object sender, EventArgs e)
      {
         try
         {
            showRebaseInProgress();
            MergeRequestRebaseResponse response = await rebaseAsync();
            applyMergeRequestRebaseResponse(response);
         }
         catch (MergeRequestEditorException ex)
         {
            reportErrorToUser(ex);
         }
      }

      async private void buttonMerge_Click(object sender, EventArgs e)
      {
         try
         {
            showMergeInProgress();
            // Modify MR manually here because for some reason "squash" query parameter
            // sometimes does not affect the merge. For instance, this occurs when
            // Merge_Error is already set to "Failed to squash", in this case simply
            // set "squash=false" has no effect.
            MergeRequest mergeRequest = await setSquashAsync(_isSquashNeeded);
            Debug.Assert(mergeRequest.Squash == _isSquashNeeded);
            mergeRequest = await mergeAsync(getSquashCommitMessage(), _isRemoteBranchDeletionNeeded);
            postProcessMerge(mergeRequest);
         }
         catch (MergeRequestEditorException ex)
         {
            if (!areConflictsFoundAtMerge(ex))
            {
               reportErrorToUser(ex);
            }
            else
            {
               MessageBox.Show("GitLab was unable to complete the merge. Rebase branch locally and try again",
                  "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
         }
      }

      private void linkLabelOpenGitExtensions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         GitExtensionsIntegrationHelper.Browse(_repositoryPath);
      }

      private void linkLabelOpenSourceTree_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         SourceTreeIntegrationHelper.Browse(_repositoryPath);
      }

      private void linkLabelOpenExplorer_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         ExternalProcess.Start("explorer", StringUtils.EscapeSpaces(_repositoryPath), false, ".");
      }

      private void linkLabelOpenAtGitLab_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         if (!String.IsNullOrWhiteSpace(_webUrl))
         {
            UrlHelper.OpenBrowser(_webUrl);
         }
      }

      private void checkBoxSquash_CheckedChanged(object sender, EventArgs e)
      {
         Debug.Assert(sender == checkBoxSquash);
         _isSquashNeeded = checkBoxSquash.Checked;
         if (_isSquashNeeded)
         {
            int newFormHeight = _formDefaultMinimumHeight - _groupBoxCommitMessageDefaultHeight;
            this.MinimumSize = new System.Drawing.Size(this.MinimumSize.Width, newFormHeight);
            this.Size = new System.Drawing.Size(this.Size.Width, newFormHeight);
            groupBoxMergeCommitMessage.Height = 0;
            groupBoxMergeCommitMessage.Visible = false;
         }
         else
         {
            int newFormHeight = _formDefaultMinimumHeight;
            this.MinimumSize = new System.Drawing.Size(this.MinimumSize.Width, newFormHeight);
            this.Size = new System.Drawing.Size(this.Size.Width, newFormHeight);
            groupBoxMergeCommitMessage.Height = _groupBoxCommitMessageDefaultHeight;
            groupBoxMergeCommitMessage.Visible = true;
         }
      }

      private void checkBoxDeleteSourceBranch_CheckedChanged(object sender, EventArgs e)
      {
         Debug.Assert(sender == checkBoxDeleteSourceBranch);
         _isRemoteBranchDeletionNeeded = checkBoxDeleteSourceBranch.Checked;
      }

      private void comboBoxCommit_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (comboBoxCommit.SelectedItem == null)
         {
            return;
         }
         textBoxCommitMessage.Text = (comboBoxCommit.SelectedItem as Commit).Message;
      }

      private void comboBoxCommit_Format(object sender, ListControlConvertEventArgs e)
      {
         Commit item = (Commit)(e.ListItem);
         e.Value = item.Title;
      }

      private void buttonClose_Click(object sender, EventArgs e)
      {
         Close();
      }

      private void initializeGitUILinks()
      {
         var storageType = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
         bool isGitRepositoryUsed = storageType != StorageSupport.LocalCommitStorageType.FileStorage;
         bool isRepositoryAvailable = isGitRepositoryUsed && !String.IsNullOrEmpty(_repositoryPath);
         linkLabelOpenGitExtensions.Visible = isRepositoryAvailable && GitExtensionsIntegrationHelper.IsInstalled();
         linkLabelOpenSourceTree.Visible = isRepositoryAvailable && SourceTreeIntegrationHelper.IsInstalled();
         linkLabelOpenExplorer.Visible = isRepositoryAvailable;
      }

      private void updateControls()
      {
         htmlPanelTitle.Text = convertTextToHtml(_title);
         labelAuthor.Text = _author?.Name ?? String.Empty;
         labelProject.Text = _mergeRequestKey.ProjectKey.ProjectName;
         labelSourceBranch.Text = _sourceBranchName;
         labelTargetBranch.Text = _targetBranchName;

         bool isWIP = _wipStatus == WorkInProgressState.Yes;
         updateWorkInProgressControls(isWIP);

         bool areUnresolvedDiscussions = _discussionState == DiscussionsState.NotResolved;
         updateDiscussionControls(areUnresolvedDiscussions);

         updateRebaseControls();

         bool isRemoteRebaseNeeded = _rebaseState == RemoteRebaseState.Required;
         bool isLocalRebaseNeded = _rebaseState == RemoteRebaseState.Failed;
         bool isRebaseNotAvailable = _rebaseState == RemoteRebaseState.NotAvailable
                                  || _rebaseState == RemoteRebaseState.InProgress;
         bool areConflictsPossible = isRemoteRebaseNeeded || isLocalRebaseNeded || isRebaseNotAvailable;
         bool areDependenciesResolved = !isWIP && !areUnresolvedDiscussions && !areConflictsPossible;
         updateMergeControls(areDependenciesResolved);

         if (comboBoxCommit.Items.Count == 0)
         {
            comboBoxCommit.Items.AddRange(_commits.ToArray());
            if (comboBoxCommit.Items.Count > 0)
            {
               comboBoxCommit.SelectedIndex = 0;
            }
         }

         checkBoxSquash.Checked = _isSquashNeeded;
         checkBoxDeleteSourceBranch.Checked = _isRemoteBranchDeletionNeeded;

         string urlTooltip = String.IsNullOrEmpty(_webUrl) ? String.Empty : _webUrl;
         toolTip.SetToolTip(linkLabelOpenAtGitLab, urlTooltip);
      }

      private void updateWorkInProgressControls(bool isWIP)
      {
         labelWIPStatus.Text = isWIP ? "This is a Work in Progress" : "This is not a Work in Progress";
         labelWIPStatus.ForeColor = isWIP ? Color.Red : Color.LightGreen;
         buttonToggleWIP.Enabled = isWIP;
      }

      private void updateDiscussionControls(bool areUnresolvedDiscussions)
      {
         labelDiscussionStatus.Text = areUnresolvedDiscussions
            ? "Please resolve unresolved threads" : "All discussions resolved";
         labelDiscussionStatus.ForeColor = areUnresolvedDiscussions ? Color.Red : Color.LightGreen;
         buttonDiscussions.Enabled = areUnresolvedDiscussions;
      }

      private void updateMergeControls(bool areDependenciesResolved)
      {
         if (_state != "opened")
         {
            labelMergeStatus.Text = "Already merged";
            labelMergeStatus.ForeColor = Color.Green;
            buttonMerge.Enabled = false;
            return;
         }

         if (!areDependenciesResolved)
         {
            labelMergeStatus.Text = "Please resolve warnings above to continue with merge";
            labelMergeStatus.ForeColor = Color.Red;
            buttonMerge.Enabled = false;
            return;
         }

         switch (_mergeStatus)
         {
            case MergeStatus.NotAvailable:
               Debug.Assert(false); // why dependencies resolved then?
               labelMergeStatus.Text = "Checking for conflicts...";
               labelMergeStatus.ForeColor = Color.Blue;
               buttonMerge.Enabled = false;
               break;

            case MergeStatus.CanBeMerged:
               labelMergeStatus.Text = "Can be merged. Merge type: Fast-forward merge without a merge commit";
               labelMergeStatus.ForeColor = Color.LightGreen;
               buttonMerge.Enabled = true;
               break;

            case MergeStatus.CannotBeMerged:
               Debug.Assert(false); // why dependencies resolved then?
               labelMergeStatus.Text = "Merge Request cannot be merged due to some GitLab issues";
               labelMergeStatus.ForeColor = Color.Red;
               buttonMerge.Enabled = false;
               break;
         }
      }

      private void updateRebaseControls()
      {
         switch (_rebaseState)
         {
            case RemoteRebaseState.NotAvailable:
               labelRebaseStatus.Text = "Cannot obtain a state of rebase operation from GitLab";
               labelRebaseStatus.ForeColor = Color.Red;
               buttonRebase.Enabled = false;
               break;

            case RemoteRebaseState.Required:
               labelRebaseStatus.Text = "Fast-forward merge is not possible";
               labelRebaseStatus.ForeColor = Color.Red;
               buttonRebase.Enabled = true;
               break;

            case RemoteRebaseState.InProgress:
               showRebaseInProgress();
               break;

            case RemoteRebaseState.Failed:
               labelRebaseStatus.Text = _rebaseError;
               labelRebaseStatus.ForeColor = Color.Red;
               buttonRebase.Enabled = false;
               break;

            case RemoteRebaseState.SucceededOrNotNeeded:
               labelRebaseStatus.Text = "Rebase is unneeded";
               labelRebaseStatus.ForeColor = Color.LightGreen;
               buttonRebase.Enabled = false;
               break;
         }
      }

      private void showRebaseInProgress()
      {
         labelRebaseStatus.Text = "Rebase is in progress...";
         labelRebaseStatus.ForeColor = Color.Blue;
         buttonRebase.Enabled = false;
      }

      private void showMergeInProgress()
      {
         labelMergeStatus.Text = "Merge in progress...";
         labelMergeStatus.ForeColor = Color.Blue;
         buttonMerge.Enabled = false;
      }
   }
}

