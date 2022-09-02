using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using mrHelper.Integration.GitUI;

namespace mrHelper.App.Forms
{
   partial class AcceptMergeRequestForm
   {
      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);
         subscribeToTimer();
         invokeFetchAndApplyOnInitialize();
         applySquashCommitMessageVisibility();
      }

      protected override void OnClosing(CancelEventArgs e)
      {
         base.OnClosing(e);
         e.Cancel = _isAwaiting;
      }

      protected override void OnClosed(EventArgs e)
      {
         base.OnClosed(e);
         unsubscribeFromTimer();
      }

      private void buttonDiscussions_Click(object sender, EventArgs e)
      {
         BeginInvoke(new Action(async () => await _onOpenDiscussions?.Invoke(_mergeRequestKey, _title, _author, _webUrl)));
      }

      async private void buttonToggleDraft_Click(object sender, EventArgs e)
      {
         traceInformation("Changing Draft by user request");
         try
         {
            buttonToggleDraft.Enabled = false;
            MergeRequest mergeRequest = await toggleDraftAsync();
            applyMergeRequest(mergeRequest);
         }
         catch (MergeRequestEditorException ex)
         {
            reportErrorToUser(ex);
         }
      }

      async private void buttonRebase_Click(object sender, EventArgs e)
      {
         traceInformation("Starting Rebase by user request");
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
         string traceMessage = String.Format(
            "Starting Merge by user request. _isSquashNeeded: {0}, _isRemoteBranchDeletionNeeded: {1}",
            _isSquashNeeded?.ToString() ?? "N/A", _isRemoteBranchDeletionNeeded.ToString());
         traceInformation(traceMessage);

         int attempts = 0;
         try
         {
            showMergeInProgress();
            await fixupSquashFlagAsync();
            if (!IsHandleCreated)
            {
               return; // if a window is closed by now
            }

            MergeRequest mergeRequest;
            try
            {
               attempts++;
               mergeRequest = await mergeAsync(getSquashCommitMessage(), _isRemoteBranchDeletionNeeded.Value);
            }
            catch (MergeRequestEditorException ex)
            {
               // sometimes when conflicts found merge succeeds on 2nd attempt
               if (areConflictsFoundAtMerge(ex))
               {
                  attempts++;
                  mergeRequest = await mergeAsync(getSquashCommitMessage(), _isRemoteBranchDeletionNeeded.Value);
               }
               else
               {
                  throw;
               }
            }
            if (mergeRequest != null)
            {
               postProcessMerge(mergeRequest);
            }
            traceInformation(String.Format("attempts: {0}", attempts));
         }
         catch (MergeRequestEditorException ex)
         {
            ExceptionHandlers.Handle(String.Format("Failed to merge (attempts: {0})", attempts), ex);
            if (areConflictsFoundAtMerge(ex))
            {
               disableProcessingTimer();
               MessageBox.Show("GitLab was unable to complete the merge. Rebase branch locally and try again",
                  "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
               enableProcessingTimer();
               return;
            }
            reportErrorToUser(ex);
         }
      }

      private void linkLabelOpenGitExtensions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         traceInformation(String.Format("Launch Git Extensions for {0}", _repositoryPath));
         GitExtensionsIntegrationHelper.Browse(_repositoryPath);
      }

      private void linkLabelOpenSourceTree_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         traceInformation(String.Format("Launch Source Tree for {0}", _repositoryPath));
         SourceTreeIntegrationHelper.Browse(_repositoryPath);
      }

      private void linkLabelOpenExplorer_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         traceInformation(String.Format("Launch Explorer for {0}", _repositoryPath));
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
         Debug.Assert(_isSquashNeeded.HasValue);
         _isSquashNeeded = checkBoxSquash.Checked;
         applySquashCommitMessageVisibility();
      }

      private void applySquashCommitMessageVisibility()
      {
         if (!_isSquashNeeded.GetValueOrDefault(false))
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

         Commit commit = comboBoxCommit.SelectedItem as Commit;
         textBoxCommitMessage.Text = StringUtils.ConvertNewlineUnixToWindows(commit.Message);
      }

      private void comboBoxCommit_Format(object sender, ListControlConvertEventArgs e)
      {
         Commit item = (Commit)(e.ListItem);
         e.Value = item.Title;
      }

      private void buttonClose_Click(object sender, EventArgs e)
      {
         traceInformation("[AcceptMergeRequestForm] User cancelled merge");
         Close();
      }

      private void initializeGitUILinks()
      {
         var storageType = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
         bool isGitRepositoryUsed = storageType == StorageSupport.LocalCommitStorageType.FullGitRepository;
         bool isRepositoryAvailable = isGitRepositoryUsed && !String.IsNullOrEmpty(_repositoryPath);

         linkLabelOpenGitExtensions.Visible = isRepositoryAvailable && GitExtensionsIntegrationHelper.IsInstalled();
         linkLabelOpenSourceTree.Visible = isRepositoryAvailable && SourceTreeIntegrationHelper.IsInstalled();
         linkLabelOpenExplorer.Visible = isRepositoryAvailable;

         traceInformation(String.Format(
            "GitUI link label visibility: Git Extensions: {0}, Source Tree: {1}, Explorer: {2}",
            linkLabelOpenGitExtensions.Visible.ToString(),
            linkLabelOpenSourceTree.Visible.ToString(),
            linkLabelOpenExplorer.Visible.ToString()));
      }

      private void updateControls()
      {
         htmlPanelTitle.Text = convertTextToHtml(_title, htmlPanelTitle);
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
         bool isLocalRebaseNeeded = _rebaseState == RemoteRebaseState.Failed
                                || _rebaseState == RemoteRebaseState.RequiredLocalRebase;
         bool isRebaseNotAvailable = _rebaseState == RemoteRebaseState.NotAvailable
                                  || _rebaseState == RemoteRebaseState.InProgress
                                  || _rebaseState == RemoteRebaseState.CheckingHierarchy;
         bool areConflictsPossible = isRemoteRebaseNeeded || isLocalRebaseNeeded || isRebaseNotAvailable;
         bool areDependenciesResolved = !isWIP && !areUnresolvedDiscussions && !areConflictsPossible;
         updateMergeControls(areDependenciesResolved);
         updateCommitList();

         if (_isSquashNeeded.HasValue)
         {
            checkBoxSquash.Checked = _isSquashNeeded.Value;
         }
         checkBoxSquash.Visible = _isSquashNeeded.HasValue;
         checkBoxSquash.Enabled = !_isAwaiting;
         checkBoxDeleteSourceBranch.Checked = _isRemoteBranchDeletionNeeded.Value;
         checkBoxDeleteSourceBranch.Enabled = !_isAwaiting;
         if (_sourceBranchName  == "master")
         {
            checkBoxDeleteSourceBranch.Enabled = false;
            checkBoxDeleteSourceBranch.Checked = false;
         }

         string urlTooltip = String.IsNullOrEmpty(_webUrl) ? String.Empty : _webUrl;
         toolTip.SetToolTip(linkLabelOpenAtGitLab, urlTooltip);

         buttonClose.Enabled = !_isAwaiting;
      }

      private void updateWorkInProgressControls(bool isWIP)
      {
         labelDraftStatus.Text = isWIP ? "This is WIP/Draft" : "This is not WIP/Draft";
         labelDraftStatus.ForeColor = isWIP ? Color.Red : Color.Green;
         buttonToggleDraft.Enabled = isWIP && !_isAwaiting;
      }

      private void updateDiscussionControls(bool areUnresolvedDiscussions)
      {
         labelDiscussionStatus.Text = areUnresolvedDiscussions
            ? "Please resolve unresolved threads" : "All discussions resolved";
         labelDiscussionStatus.ForeColor = areUnresolvedDiscussions ? Color.Red : Color.Green;
         buttonDiscussions.Enabled = areUnresolvedDiscussions && !_isAwaiting;
      }

      private void updateMergeControls(bool areDependenciesResolved)
      {
         Debug.Assert(_state == "merged" || _state == "opened"); // see updateMergeRequestInformation()
         if (_state == "merged")
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
            case MergeStatus.Unchecked:
               labelMergeStatus.Text = "Checking for conflicts...";
               labelMergeStatus.ForeColor = Color.Blue;
               buttonMerge.Enabled = false;
               break;

            case MergeStatus.CanBeMerged:
               labelMergeStatus.Text = "Can be merged. Merge type: Fast-forward merge without a merge commit";
               labelMergeStatus.ForeColor = Color.Green;
               buttonMerge.Enabled = !_isAwaiting;
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
         if (_state == "merged")
         {
            showRebaseUnneeded();
            return;
         }

         switch (_rebaseState)
         {
            case RemoteRebaseState.NotAvailable:
               labelRebaseStatus.Text = "Cannot obtain a state of rebase operation from GitLab";
               labelRebaseStatus.ForeColor = Color.Red;
               buttonRebase.Enabled = false;
               break;

            case RemoteRebaseState.CheckingHierarchy:
               labelRebaseStatus.Text = "Checking if Rebase is needed...";
               labelRebaseStatus.ForeColor = Color.Blue;
               buttonRebase.Enabled = false;
               break;

            case RemoteRebaseState.Required:
               labelRebaseStatus.Text = "Fast-forward merge is not possible";
               labelRebaseStatus.ForeColor = Color.Red;
               buttonRebase.Enabled = !_isAwaiting;
               break;

            case RemoteRebaseState.RequiredLocalRebase:
               labelRebaseStatus.Text = "Rebase branch locally";
               labelRebaseStatus.ForeColor = Color.Red;
               buttonRebase.Enabled = false;
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
               showRebaseUnneeded();
               break;
         }
      }

      private void updateCommitList()
      {
         if (!_invalidateCommitList)
         {
            return;
         }

         string prevSelectedSha = comboBoxCommit.SelectedIndex >= 0
            ? (comboBoxCommit.Items[comboBoxCommit.SelectedIndex] as Commit).Id
            : null;

         comboBoxCommit.Items.Clear();
         comboBoxCommit.Items.AddRange(_commits.ToArray());

         if (comboBoxCommit.Items.Count > 0)
         {
            bool prevCommitSelected = false;
            if (prevSelectedSha != null)
            {
               Commit prevSelectedCommit = comboBoxCommit.Items.Cast<Commit>()
                  .FirstOrDefault(commit => commit.Id == prevSelectedSha);
               if (prevSelectedCommit != null)
               {
                  int indexOfPrevSelectedCommit = comboBoxCommit.Items.IndexOf(prevSelectedCommit);
                  if (indexOfPrevSelectedCommit != -1)
                  {
                     comboBoxCommit.SelectedIndex = indexOfPrevSelectedCommit;
                     prevCommitSelected = true;
                  }
               }
            }

            if (!prevCommitSelected)
            {
               comboBoxCommit.SelectedIndex = comboBoxCommit.Items.Count - 1;
            }
         }

         _invalidateCommitList = false;
      }

      private void showRebaseInProgress()
      {
         labelRebaseStatus.Text = "Rebase is in progress...";
         labelRebaseStatus.ForeColor = Color.Blue;
         buttonRebase.Enabled = false;
      }

      private void showRebaseUnneeded()
      {
         labelRebaseStatus.Text = "Rebase is unneeded";
         labelRebaseStatus.ForeColor = Color.Green;
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

