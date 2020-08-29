using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using Markdig;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using mrHelper.Integration.GitUI;

namespace mrHelper.App.Forms
{
   internal class UnsupportedMergeMethodException : Exception
   {
      internal UnsupportedMergeMethodException(string message)
         : base(message)
      {
      }
   }

   public partial class AcceptMergeRequestForm : CustomFontForm
   {
      internal AcceptMergeRequestForm(MergeRequestKey mrk, string repositoryPath,
         IMergeRequestCache dataCache, GitLabClient.MergeRequestAccessor mergeRequestAccessor,
         Func<MergeRequestKey, string, User, Task> onOpenDiscussions, string mergeMethod)
      {
         if (dataCache == null)
         {
            throw new ArgumentException("dataCache argument cannot be null");
         }

         if (mergeMethod != "ff")
         {
            throw new UnsupportedMergeMethodException(
               "Current version supports projects with Fast Forward merge method only");
         }

         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         applyFont(Program.Settings.MainWindowFontSizeName);

         _mergeRequestKey = mrk;
         _repositoryPath = repositoryPath ?? throw new ArgumentException("repositoryPath argument cannot be null");
         _mergeRequestAccessor = mergeRequestAccessor ?? throw new ArgumentException("mergeRequestAccessor argument cannot be null");
         _onOpenDiscussions = onOpenDiscussions;

         _mdPipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());

         htmlPanelTitle.BaseStylesheet = String.Format("{0}", mrHelper.App.Properties.Resources.Common_CSS);

         initializeGitUILinks();
         setInitialState(dataCache);

         createSynchronizationWithGitLabTimer();
         startSynchronizationTimer();
      }

      private void buttonDiscussions_Click(object sender, EventArgs e)
      {
         BeginInvoke(new Action(async () => await _onOpenDiscussions?.Invoke(_mergeRequestKey, _title, _author)));
      }

      async private void buttonToggleWIP_Click(object sender, EventArgs e)
      {
         setTitle(StringUtils.ToggleWorkInProgressTitle(_title));
         try
         {
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
            bool squash = checkBoxSquash.Checked;
            bool deleteSourceBranch = checkBoxDeleteSourceBranch.Checked;
            MergeRequest mergeRequest = await mergeAsync(getSquashCommitMessage(), squash, deleteSourceBranch);
            postProcessMerge(mergeRequest);
         }
         catch (MergeRequestEditorException ex)
         {
            // TODO WTF - Test this!
            if (!areConflictsFoundAtMerge(ex))
            {
               reportErrorToUser(ex);
            }
         }
      }

      private void postProcessMerge(MergeRequest mergeRequest)
      {
         if (mergeRequest.State == "merged")
         {
            Close();
            return;
         }

         MessageBox.Show("Something went wrong at GitLab during merge, try again at Web UI", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
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
         ExternalProcess.Start("explorer", _repositoryPath, false, ".");
      }

      private void checkBoxSquash_CheckedChanged(object sender, EventArgs e)
      {
         Debug.Assert(sender == checkBoxSquash);
         bool isSquashCommitSelected = checkBoxSquash.Checked;
         textBoxCommitMessage.Enabled = !isSquashCommitSelected;
         comboBoxCommit.Enabled = !isSquashCommitSelected;
      }

      private void comboBoxCommit_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (comboBoxCommit.SelectedItem == null)
         {
            return;
         }
         textBoxCommitMessage.Text = (comboBoxCommit.SelectedItem as Commit).Message;
      }

      private void buttonClose_Click(object sender, EventArgs e)
      {
         Close();
      }

      private Task<MergeRequest> loadMergeRequestFromServerAsync()
      {
         return _mergeRequestAccessor.SearchMergeRequestAsync(_mergeRequestKey.IId);
      }

      private void setInitialState(IMergeRequestCache dataCache)
      {
         MergeRequest mergeRequest = dataCache.GetMergeRequest(_mergeRequestKey);
         applyMergeRequest(mergeRequest);
         checkBoxSquash.Checked = mergeRequest.Squash;
         checkBoxDeleteSourceBranch.Checked = mergeRequest.Force_Remove_Source_Branch;

         IEnumerable<Commit> commits = dataCache.GetCommits(_mergeRequestKey);
         comboBoxCommit.Items.AddRange(commits.ToArray());
      }

      private void applyMergeRequest(MergeRequest mergeRequest)
      {
         if (mergeRequest == null)
         {
            return;
         }

         updateMergeRequestInformation(mergeRequest);
         updateWorkInProgressStatus(mergeRequest);
         updateRebaseStatus(mergeRequest);
         updateDiscussionState(mergeRequest);
         updateControls();
      }

      private void applyMergeRequestRebaseResponse(MergeRequestRebaseResponse response)
      {
         updateRebaseStatus(response);
         updateControls();
      }

      private void updateMergeRequestInformation(MergeRequest mergeRequest)
      {
         setTitle(mergeRequest.Title);
         _author = mergeRequest.Author;
         labelAuthor.Text = mergeRequest.Author.Name;
         labelProject.Text = _mergeRequestKey.ProjectKey.ProjectName;
         labelSourceBranch.Text = mergeRequest.Source_Branch;
         labelTargetBranch.Text = mergeRequest.Target_Branch;
      }

      private void updateWorkInProgressStatus(MergeRequest mergeRequest)
      {
         _wipStatus = StringUtils.IsWorkInProgressTitle(mergeRequest.Title)
            ? WorkInProgressState.Yes : WorkInProgressState.No;
      }

      private void updateDiscussionState(MergeRequest mergeRequest)
      {
         _discussionState = mergeRequest.Blocking_Discussions_Resolved
            ? DiscussionsState.Resolved : DiscussionsState.NotResolved;
      }

      private IMergeRequestEditor getEditor()
      {
         return _mergeRequestAccessor
            .GetSingleMergeRequestAccessor(_mergeRequestKey.IId)
            .GetMergeRequestEditor();
      }

      async private Task<MergeRequestRebaseResponse> rebaseAsync()
      {
         stopSynchronizationTimer();
         try
         {
            return await getEditor().Rebase(null);
         }
         finally
         {
            startSynchronizationTimer();
         }
      }

      async private Task<MergeRequest> mergeAsync(string squashCommitMessage, bool squash, bool deleteSourceBranch)
      {
         AcceptMergeRequestParameters parameters = new AcceptMergeRequestParameters(
            null, squashCommitMessage, squash, deleteSourceBranch, null, null);

         stopSynchronizationTimer();
         try
         {
            return await getEditor().Merge(parameters);
         }
         finally
         {
            startSynchronizationTimer();
         }
      }

      async private Task<MergeRequest> toggleWipAsync()
      {
         UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
            null, _title, null, null, null, null, null);

         stopSynchronizationTimer();
         try
         {
            return await getEditor().ModifyMergeRequest(updateMergeRequestParameters);
         }
         finally
         {
            startSynchronizationTimer();
         }
      }

      private void updateRebaseStatus(MergeRequest mergeRequest)
      {
         if (mergeRequest.Rebase_In_Progress.HasValue && mergeRequest.Rebase_In_Progress.Value)
         {
            _rebaseState = RebaseState.InProgress;
         }
         else if (mergeRequest.Merge_Error == null)
         {
            _rebaseState = mergeRequest.Has_Conflicts ? RebaseState.Required : RebaseState.SucceededOrNotNeeded;
         }
         else
         {
            _rebaseState = RebaseState.Failure;
            _rebaseError = mergeRequest.Merge_Error;
         }
      }

      private void updateRebaseStatus(MergeRequestRebaseResponse rebaseResponse)
      {
         if (rebaseResponse.Rebase_In_Progress)
         {
            _rebaseState = RebaseState.InProgress;
         }
      }

      private void initializeGitUILinks()
      {
         bool repositoryAvailable = !String.IsNullOrEmpty(_repositoryPath);
         linkLabelOpenGitExtensions.Enabled = repositoryAvailable && GitExtensionsIntegrationHelper.IsInstalled();
         linkLabelOpenSourceTree.Enabled = repositoryAvailable && SourceTreeIntegrationHelper.IsInstalled();
      }

      private void updateControls()
      {
         bool isWIP = _wipStatus == WorkInProgressState.Yes;
         labelWIPStatus.Text = isWIP ? "This is a Work in Progress" : "This is not a Work in Progress";

         bool areUnresolvedDiscussions = _discussionState == DiscussionsState.NotResolved;
         labelDiscussionStatus.Text = areUnresolvedDiscussions
            ? "There are unresolved threads. Please resolve these threads." : "All discussions resolved.";

         switch (_rebaseState)
         {
            case RebaseState.NotAvailable:
               labelRebaseStatus.Text = "Cannot obtain a state of rebase operation from GitLab.";
               break;

            case RebaseState.Required:
               labelRebaseStatus.Text =
                  "Fast-forward merge is not possible. " +
                  "Rebase the source branch onto the target branch or merge target branch into source branch " +
                  "to allow this merge request to be merged.";
               break;

            case RebaseState.InProgress:
               labelRebaseStatus.Text = "Rebase is in progress...";
               break;

            case RebaseState.Failure:
               labelRebaseStatus.Text = _rebaseError;
               break;

            case RebaseState.SucceededOrNotNeeded:
               labelRebaseStatus.Text = "There are no conflicts.";
               break;
         }

         switch (_mergeStatus)
         {
            case MergeStatus.CanBeMerged:
               labelMergeStatus.Text = "Can be merged. Merge type: Fast-forward merge without a merge commit.";
               break;

            case MergeStatus.CannotBeMerged:
               labelMergeStatus.Text = "Cannot be merged.";
               break;
         }

         bool arePreconditionsMet = !isWIP && !areUnresolvedDiscussions;
         bool isRebaseAvailable = arePreconditionsMet && _rebaseState == RebaseState.Required;
         bool isMergeAvailable = arePreconditionsMet && _rebaseState == RebaseState.SucceededOrNotNeeded;
         buttonRebase.Enabled = isRebaseAvailable;
         buttonMerge.Enabled = isMergeAvailable;

         Debug.Assert(isMergeAvailable
            ? _mergeStatus == MergeStatus.CanBeMerged : _mergeStatus == MergeStatus.CannotBeMerged);
      }

      private void setTitle(string title)
      {
         htmlPanelTitle.Text = convertTextToHtml(title);
         _title = title;
      }

      private string convertTextToHtml(string text)
      {
         string hostname = _mergeRequestKey.ProjectKey.HostName;
         string projectname = _mergeRequestKey.ProjectKey.ProjectName;
         string prefix = StringUtils.GetGitLabAttachmentPrefix(hostname, projectname);
         string html = MarkDownUtils.ConvertToHtml(text, prefix, _mdPipeline);
         return String.Format(MarkDownUtils.HtmlPageTemplate, html);
      }

      private string getSquashCommitMessage()
      {
         bool squash = checkBoxSquash.Checked;
         return squash ? textBoxCommitMessage.Text : null;
      }

      private void createSynchronizationWithGitLabTimer()
      {
         _synchronizationTimer.Tick += async (s, a) => applyMergeRequest(await loadMergeRequestFromServerAsync());
      }

      private void startSynchronizationTimer()
      {
         _synchronizationTimer.Start();
      }

      private void stopSynchronizationTimer()
      {
         _synchronizationTimer.Stop();
      }

      private static void reportErrorToUser(MergeRequestEditorException ex)
      {
         if (ex is MergeRequestEditorCancelledException)
         {
            return;
         }

         void showDialogAndLogError(string message = "")
         {
            string defaultMessage = "GitLab could not perform a requested operation. Reason: ";
            MessageBox.Show(defaultMessage + message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("[RebaseMergeRequestForm] " + message);
         };

         if (ex.InnerException != null && (ex.InnerException is GitLabRequestException))
         {
            GitLabRequestException rx = ex.InnerException as GitLabRequestException;
            if (rx.InnerException is System.Net.WebException wx && wx.Response != null)
            {
               System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
               switch (response.StatusCode)
               {
                  case System.Net.HttpStatusCode.MethodNotAllowed:
                     showDialogAndLogError("Merge request in its current state cannot be merged.");
                     return;

                  case System.Net.HttpStatusCode.Unauthorized:
                  case System.Net.HttpStatusCode.Forbidden:
                     showDialogAndLogError("Access denied");
                     return;

                  case System.Net.HttpStatusCode.BadRequest:
                     showDialogAndLogError("Bad parameters");
                     return;
               }
            }
         }

         showDialogAndLogError();
      }

      private static bool areConflictsFoundAtMerge(MergeRequestEditorException ex)
      {
         if (ex.InnerException != null && (ex.InnerException is GitLabRequestException))
         {
            GitLabRequestException rx = ex.InnerException as GitLabRequestException;
            if (rx.InnerException is System.Net.WebException wx && wx.Response != null)
            {
               System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
               return response.StatusCode == System.Net.HttpStatusCode.NotAcceptable;
            }
         }
         return false;
      }

      private static readonly int rebaseStatusUpdateInterval = 500; // ms

      private readonly Timer _synchronizationTimer = new Timer
      {
         Interval = rebaseStatusUpdateInterval
      };

      private readonly string _repositoryPath;
      private readonly GitLabClient.MergeRequestAccessor _mergeRequestAccessor;
      private readonly MergeRequestKey _mergeRequestKey;
      private readonly Func<MergeRequestKey, string, User, Task> _onOpenDiscussions;
      private readonly MarkdownPipeline _mdPipeline;

      private enum DiscussionsState
      {
         Resolved,
         NotResolved
      }

      private enum WorkInProgressState
      {
         Yes,
         No
      }

      private enum RebaseState
      {
         NotAvailable,
         Required,
         InProgress,
         SucceededOrNotNeeded,
         Failure
      }

      private enum MergeStatus
      {
         NotAvailable,
         CanBeMerged,
         CannotBeMerged
      }

      private User _author;
      private string _title;
      private DiscussionsState _discussionState;
      private WorkInProgressState _wipStatus;
      private RebaseState _rebaseState = RebaseState.NotAvailable;
      private string _rebaseError;
      private MergeStatus _mergeStatus = MergeStatus.NotAvailable;

   }
}

