using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using Markdig;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using System.Collections.Generic;

namespace mrHelper.App.Forms
{
   public partial class AcceptMergeRequestForm : CustomFontForm
   {
      internal AcceptMergeRequestForm(
         MergeRequestKey mrk,
         string repositoryPath,
         Action onMerged,
         Func<MergeRequestKey, string, User, Task> onOpenDiscussions,
         Func<Task<DataCache>> fetchCache,
         Func<GitLabClient.MergeRequestAccessor> getMergeRequestAccessor)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         applyFont(Program.Settings.MainWindowFontSizeName);
         _formDefaultMinimumHeight = MinimumSize.Height;
         _groupBoxCommitMessageDefaultHeight = groupBoxMergeCommitMessage.Height;

         _mergeRequestKey = mrk;
         _repositoryPath = repositoryPath ?? throw new ArgumentException("repositoryPath argument cannot be null");
         _mdPipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());
         _onOpenDiscussions = onOpenDiscussions;
         _onMerged = onMerged;
         _fetchCache = fetchCache;
         _getMergeRequestAccessor = getMergeRequestAccessor;

         initializeGitUILinks();
         subscribeToTimer();
      }

      private void postProcessMerge(MergeRequest mergeRequest)
      {
         if (mergeRequest.State == "merged")
         {
            Close();
            _onMerged();
            return;
         }

         string errorMessage = "Something went wrong at GitLab during merge, try again at Web UI";
         if (!String.IsNullOrEmpty(mergeRequest.Merge_Error))
         {
            errorMessage = String.Format(
               "GitLab reported error: \"{0}\". Try to resolve it and repeat operation.", mergeRequest.Merge_Error);
         }

         MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }

      private void refreshCommits(IMergeRequestCache mergeRequestCache)
      {
         _commits = mergeRequestCache.GetCommits(_mergeRequestKey).ToArray();
      }

      private void checkProjectProperties(IProjectCache projectCache)
      {
         IEnumerable<Project> fullProjectList = projectCache.GetProjects();
         Project selectedProject = fullProjectList
            .SingleOrDefault(project => project.Path_With_Namespace == _mergeRequestKey.ProjectKey.ProjectName);
         if (selectedProject.Merge_Method != "ff")
         {
            Trace.TraceError("[AcceptMergeRequestForm] Unsupported merge method {0} detected in project {1}",
               selectedProject.Merge_Method, selectedProject.Path_With_Namespace);
            string message = "Current version supports projects with Fast Forward merge method only";
            MessageBox.Show(message, "Unsupported project merge method", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
         }
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
         updateMergeStatus(mergeRequest);
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
         if (mergeRequest.State != "opened")
         {
            string message = "Only Open Merge Requests can be merged";
            MessageBox.Show(message, "Bad Merge Request state", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
         }

         setTitle(mergeRequest.Title);
         _author = mergeRequest.Author;
         _sourceBranchName = mergeRequest.Source_Branch ?? String.Empty;
         _targetBranchName = mergeRequest.Target_Branch ?? String.Empty;
         _webUrl = mergeRequest.Web_Url;
         _state = mergeRequest.State;

         if (!_isSquashNeeded.HasValue)
         {
            _isSquashNeeded = mergeRequest.Squash;
         }
         if (!_isRemoteBranchDeletionNeeded.HasValue)
         {
            _isRemoteBranchDeletionNeeded = mergeRequest.Force_Remove_Source_Branch;
         }
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

      private void updateRebaseStatus(MergeRequest mergeRequest)
      {
         if (mergeRequest.Rebase_In_Progress.HasValue && mergeRequest.Rebase_In_Progress.Value)
         {
            _rebaseState = RemoteRebaseState.InProgress;
         }
         else if (mergeRequest.Merge_Error == null)
         {
            _rebaseState = mergeRequest.Has_Conflicts ? RemoteRebaseState.Required : RemoteRebaseState.SucceededOrNotNeeded;
         }
         else
         {
            if (mergeRequest.Has_Conflicts)
            {
               _rebaseState = RemoteRebaseState.Failed;
               _rebaseError = mergeRequest.Merge_Error;
            }
            else
            {
               // Seems we just have to ignore Merge_Error field which
               // remains filled in GitLab responses even after
               // local rebase is finished and Has_Conflicts is unset.
               _rebaseState = RemoteRebaseState.SucceededOrNotNeeded;
            }
         }
      }

      private void updateRebaseStatus(MergeRequestRebaseResponse rebaseResponse)
      {
         if (rebaseResponse.Rebase_In_Progress)
         {
            _rebaseState = RemoteRebaseState.InProgress;
         }
      }

      private void updateMergeStatus(MergeRequest mergeRequest)
      {
         if (mergeRequest.Merge_Status == null)
         {
            _mergeStatus = MergeStatus.NotAvailable;
            return;
         }

         _mergeStatus = mergeRequest.Merge_Status == "can_be_merged"
            ? MergeStatus.CanBeMerged : MergeStatus.CannotBeMerged;
      }

      private void setTitle(string title)
      {
         _title = title == null ? String.Empty : title;
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
         return _isSquashNeeded.HasValue && _isSquashNeeded.Value ? textBoxCommitMessage.Text : null;
      }

      private void subscribeToTimer()
      {
         if (_synchronizationTimerUsers == 0)
         {
            _synchronizationTimer = new Timer
            {
               Interval = rebaseStatusUpdateInterval
            };
            _synchronizationTimer.Start();
         }
         ++_synchronizationTimerUsers;
         startProcessingTimer();
      }

      private void unsubscribeFromTimer()
      {
         stopProcessingTimer();
         --_synchronizationTimerUsers;
         if (_synchronizationTimerUsers == 0)
         {
            _synchronizationTimer.Stop();
            _synchronizationTimer.Dispose();
            _synchronizationTimer = null;
         }
      }

      private void startProcessingTimer()
      {
         _synchronizationTimer.Tick += onSynchronizationTimer;
      }

      private void stopProcessingTimer()
      {
         _synchronizationTimer.Tick -= onSynchronizationTimer;
      }

      private void onSynchronizationTimer(object sender, EventArgs e)
      {
         BeginInvoke(new Action(async () =>
         {
            MergeRequest mergeRequest = await fetchUpdatedMergeRequest();
            applyMergeRequest(mergeRequest);
         }), null);
      }

      private IMergeRequestEditor getEditor()
      {
         GitLabClient.MergeRequestAccessor accessor = _getMergeRequestAccessor();
         if (accessor == null)
         {
            return null;
         }

         return accessor
            .GetSingleMergeRequestAccessor(_mergeRequestKey.IId)
            .GetMergeRequestEditor();
      }

      async private Task<MergeRequest> fetchUpdatedMergeRequest()
      {
         stopProcessingTimer();
         try
         {
            DataCache dataCache = await _fetchCache();
            if (dataCache == null || dataCache.MergeRequestCache == null || dataCache.ProjectCache == null)
            {
               Debug.Assert(false);
               return null;
            }

            checkProjectProperties(dataCache.ProjectCache);
            refreshCommits(dataCache.MergeRequestCache);
            return dataCache.MergeRequestCache.GetMergeRequest(_mergeRequestKey);
         }
         finally
         {
            startProcessingTimer();
         }
      }

      async private Task<MergeRequestRebaseResponse> rebaseAsync()
      {
         stopProcessingTimer();
         try
         {
            IMergeRequestEditor editor = getEditor();
            if (editor == null)
            {
               return null;
            }

            return await editor.Rebase(null);
         }
         finally
         {
            startProcessingTimer();
         }
      }

      async private Task<MergeRequest> mergeAsync(string squashCommitMessage, bool shouldRemoveSourceBranch)
      {
         AcceptMergeRequestParameters parameters = new AcceptMergeRequestParameters(
            null, squashCommitMessage, null, shouldRemoveSourceBranch, null, null);

         stopProcessingTimer();
         try
         {
            IMergeRequestEditor editor = getEditor();
            if (editor == null)
            {
               return null;
            }

            return await editor.Merge(parameters);
         }
         finally
         {
            startProcessingTimer();
         }
      }

      async private Task<MergeRequest> toggleWipAsync()
      {
         UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
            null, _title, null, null, null, null, null);
         return await applyModification(updateMergeRequestParameters);
      }

      async private Task<MergeRequest> setSquashAsync(bool squash)
      {
         UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
            null, null, null, null, null, null, squash);
         return await applyModification(updateMergeRequestParameters);
      }

      async private Task<MergeRequest> applyModification(UpdateMergeRequestParameters parameters)
      {
         stopProcessingTimer();
         try
         {
            IMergeRequestEditor editor = getEditor();
            if (editor == null)
            {
               return null;
            }

            return await editor.ModifyMergeRequest(parameters);
         }
         finally
         {
            startProcessingTimer();
         }
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
                     showDialogAndLogError("Access denied or source branch does not exist");
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

      private static readonly int rebaseStatusUpdateInterval = 1000; // ms
      private static Timer _synchronizationTimer;
      private static int _synchronizationTimerUsers = 0;

      private readonly string _repositoryPath;
      private readonly MergeRequestKey _mergeRequestKey;
      private readonly Func<MergeRequestKey, string, User, Task> _onOpenDiscussions;
      private readonly Action _onMerged;
      private readonly MarkdownPipeline _mdPipeline;
      private readonly Func<Task<DataCache>> _fetchCache;
      private readonly Func<GitLabClient.MergeRequestAccessor> _getMergeRequestAccessor;

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

      private enum RemoteRebaseState
      {
         NotAvailable,
         Required,
         Failed,
         InProgress,
         SucceededOrNotNeeded
      }

      private enum MergeStatus
      {
         NotAvailable,
         CanBeMerged,
         CannotBeMerged
      }

      private User _author;
      private string _sourceBranchName;
      private string _targetBranchName;
      private string _title;
      private DiscussionsState _discussionState;
      private WorkInProgressState _wipStatus;
      private RemoteRebaseState _rebaseState = RemoteRebaseState.NotAvailable;
      private string _rebaseError;
      private MergeStatus _mergeStatus = MergeStatus.NotAvailable;
      private bool? _isSquashNeeded;
      private bool? _isRemoteBranchDeletionNeeded;
      private Commit[] _commits;
      private string _webUrl;
      private string _state;

      private readonly int _formDefaultMinimumHeight;
      private readonly int _groupBoxCommitMessageDefaultHeight;
   }

   internal class UnsupportedMergeMethodException : Exception
   {
      internal UnsupportedMergeMethodException(string message)
         : base(message)
      {
      }
   }

   internal class InvalidMergeRequestState : Exception
   {
      internal InvalidMergeRequestState(string message)
         : base(message)
      {
      }
   }
}

