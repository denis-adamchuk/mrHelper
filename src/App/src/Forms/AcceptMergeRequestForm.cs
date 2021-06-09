using System;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using Markdig;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal partial class AcceptMergeRequestForm : CustomFontForm
   {
      public AcceptMergeRequestForm(
         MergeRequestKey mrk,
         string repositoryPath,
         Action onMerged,
         Func<MergeRequestKey, string, User, string, Task> onOpenDiscussions,
         Func<DataCache> getCache,
         Func<Task<DataCache>> fetchCache,
         Func<GitLabClient.MergeRequestAccessor> getMergeRequestAccessor)
      {
         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
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
         _getCache = getCache;
         _fetchCache = fetchCache;
         _getMergeRequestAccessor = getMergeRequestAccessor;

         initializeGitUILinks();
      }

      private void postProcessMerge(MergeRequest mergeRequest)
      {
         if (mergeRequest.State == "merged")
         {
            traceInformation("Merge completed successfully");
            Close();
            _onMerged();
            return;
         }

         string warningMessage = "Something went wrong at GitLab during merge, try again at Web UI";
         if (!String.IsNullOrEmpty(mergeRequest.Merge_Error))
         {
            warningMessage = String.Format(
               "GitLab reported error: \"{0}\". Try to resolve it and repeat operation.", mergeRequest.Merge_Error);
         }

         traceWarning(warningMessage);
         disableProcessingTimer();
         MessageBox.Show(warningMessage, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
         enableProcessingTimer();
      }

      private void refreshCommits(IMergeRequestCache mergeRequestCache)
      {
         int prevCommitCount = _commits?.Length ?? 0;

         _commits = mergeRequestCache.GetCommits(_mergeRequestKey).ToArray();

         if (prevCommitCount != _commits.Length)
         {
            traceInformation(String.Format("Changed _commits.Length to {0}", _commits.Length.ToString()));
         }
      }

      private void checkProjectProperties(IProjectCache projectCache)
      {
         IEnumerable<Project> fullProjectList = projectCache.GetProjects();
         Project selectedProject = fullProjectList
            .SingleOrDefault(project => project.Path_With_Namespace == _mergeRequestKey.ProjectKey.ProjectName);
         if (selectedProject.Merge_Method != "ff")
         {
            traceError(String.Format("Unsupported merge method {0} detected in project {1}",
               selectedProject.Merge_Method, selectedProject.Path_With_Namespace));
            string message = "Current version supports projects with Fast Forward merge method only";
            disableProcessingTimer();
            MessageBox.Show(message, "Unsupported project merge method", MessageBoxButtons.OK, MessageBoxIcon.Error);
            enableProcessingTimer();
            Close();
         }
      }

      private void applyMergeRequest(MergeRequest mergeRequest)
      {
         if (mergeRequest == null)
         {
            return;
         }

         updateStaticMergeRequestProperties(mergeRequest);
         updateMergeRequestTitle(mergeRequest);
         updateMergeRequestState(mergeRequest);
         updateMergeRequestBranches(mergeRequest);
         updateMergeRequestMergeFlags(mergeRequest);
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

      private void updateStaticMergeRequestProperties(MergeRequest mergeRequest)
      {
         _author = mergeRequest.Author;
         _webUrl = mergeRequest.Web_Url;
      }

      private void updateMergeRequestTitle(MergeRequest mergeRequest)
      {
         string prevTitle = _title;

         _title = mergeRequest.Title ?? String.Empty;

         if (prevTitle != _title)
         {
            traceInformation(String.Format("Changed _title to {0}", _title.ToString()));
         }
      }

      private void updateMergeRequestState(MergeRequest mergeRequest)
      {
         if (mergeRequest.State != "opened" && mergeRequest.State != "merged")
         {
            traceWarning(String.Format("Unexpected merge request state {0}", mergeRequest.State.ToString()));

            string message = "Only Open Merge Requests can be merged";
            disableProcessingTimer();
            MessageBox.Show(message, "Unexpected Merge Request state", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            enableProcessingTimer();
            Close();
            return;
         }

         string prevState = _state;

         _state = mergeRequest.State;

         if (prevState != _state)
         {
            traceInformation(String.Format("Changed _state to {0}", _state.ToString()));
         }
      }

      private void updateMergeRequestBranches(MergeRequest mergeRequest)
      {
         string prevSourceBranch = _sourceBranchName;
         string prevTargetBranch = _targetBranchName;

         _sourceBranchName = mergeRequest.Source_Branch ?? String.Empty;
         _targetBranchName = mergeRequest.Target_Branch ?? String.Empty;

         if (prevSourceBranch != _sourceBranchName)
         {
            traceInformation(String.Format("Changed _sourceBranchName to {0}", _sourceBranchName.ToString()));
         }

         if (prevTargetBranch != _targetBranchName)
         {
            traceInformation(String.Format("Changed _targetBranchName to {0}", _targetBranchName.ToString()));
         }
      }

      private void updateMergeRequestMergeFlags(MergeRequest mergeRequest)
      {
         bool? prevSquashNeeded = _isSquashNeeded;
         bool? prevRemoteBranchDeletionNeeded = _isRemoteBranchDeletionNeeded;

         if (!_isSquashNeeded.HasValue)
         {
            _isSquashNeeded = mergeRequest.Squash;
            applySquashCommitMessageVisibility();
         }

         if (!_isRemoteBranchDeletionNeeded.HasValue)
         {
            _isRemoteBranchDeletionNeeded = mergeRequest.Force_Remove_Source_Branch;
         }

         if (prevSquashNeeded != _isSquashNeeded)
         {
            traceInformation(String.Format("Changed _isSquashNeeded to {0}", _isSquashNeeded.ToString()));
         }

         if (prevRemoteBranchDeletionNeeded != _isRemoteBranchDeletionNeeded)
         {
            traceInformation(String.Format("Changed _isRemoteBranchDeletionNeeded to {0}", _isRemoteBranchDeletionNeeded.ToString()));
         }
      }

      private void updateWorkInProgressStatus(MergeRequest mergeRequest)
      {
         WorkInProgressState prevState = _wipStatus;

         _wipStatus = StringUtils.IsWorkInProgressTitle(mergeRequest.Title)
            ? WorkInProgressState.Yes : WorkInProgressState.No;

         if (prevState != _wipStatus)
         {
            traceInformation(String.Format("Changed _wipStatus to {0}", _wipStatus.ToString()));
         }
      }

      private void updateDiscussionState(MergeRequest mergeRequest)
      {
         DiscussionsState prevState = _discussionState;

         _discussionState = mergeRequest.Blocking_Discussions_Resolved
            ? DiscussionsState.Resolved : DiscussionsState.NotResolved;

         if (prevState != _discussionState)
         {
            traceInformation(String.Format("Changed _discussionState to {0}", _discussionState.ToString()));
         }
      }

      private void updateRebaseStatus(MergeRequest mergeRequest)
      {
         RemoteRebaseState prevState = _rebaseState;
         string prevError = _rebaseError;

         _rebaseError = String.Empty;
         if (mergeRequest.Rebase_In_Progress.HasValue && mergeRequest.Rebase_In_Progress.Value)
         {
            _rebaseState = RemoteRebaseState.InProgress;
         }
         else if (mergeRequest.Merge_Error == null)
         {
            if (mergeRequest.Has_Conflicts)
            {
               _rebaseState = RemoteRebaseState.Required;
            }
            else if (_rebaseState == RemoteRebaseState.InProgress)
            {
               _rebaseState = RemoteRebaseState.SucceededOrNotNeeded;
            }
            else if (_rebaseState == RemoteRebaseState.NotAvailable || _rebaseState == RemoteRebaseState.Failed)
            {
               // We have to set initial state of a dialog to Required because it is not possible to tell from GitLab
               // API if MR needs Rebase or not because has_conflicts flag value can be false even when
               // GitLab Web UI tells that fast-forwarding merge is not possible and Rebase is needed.
               _rebaseState = RemoteRebaseState.Required;
            }
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

         if (prevState != _rebaseState)
         {
            traceInformation(String.Format("Changed _rebaseState to {0}", _rebaseState.ToString()));
         }

         if (prevError != _rebaseError)
         {
            traceInformation(String.Format("Changed _rebaseError to {0}", _rebaseError.ToString()));
         }
      }

      private void updateRebaseStatus(MergeRequestRebaseResponse rebaseResponse)
      {
         RemoteRebaseState prevState = _rebaseState;

         _rebaseError = String.Empty;
         if (rebaseResponse.Rebase_In_Progress)
         {
            _rebaseState = RemoteRebaseState.InProgress;
         }

         if (prevState != _rebaseState)
         {
            traceInformation(String.Format("Changed _rebaseState to {0}", _rebaseState.ToString()));
         }
      }

      private void updateMergeStatus(MergeRequest mergeRequest)
      {
         MergeStatus prevStatus = _mergeStatus;

         if (mergeRequest.Merge_Status == null)
         {
            _mergeStatus = MergeStatus.NotAvailable;
         }
         else
         {
            if (mergeRequest.Merge_Status == "can_be_merged")
            {
               _mergeStatus = MergeStatus.CanBeMerged;
            }
            else if (mergeRequest.Merge_Status == "cannot_be_merged")
            {
               _mergeStatus = MergeStatus.CannotBeMerged;
            }
            else if (mergeRequest.Merge_Status == "unchecked"
                  || mergeRequest.Merge_Status == "checking")
            {
               _mergeStatus = MergeStatus.Unchecked;
            }
            else
            {
               Debug.Assert(false); // unknown Merge_Status
               _mergeStatus = MergeStatus.NotAvailable;
            }
         }

         if (prevStatus != _mergeStatus)
         {
            traceInformation(String.Format("Changed _mergeStatus to {0}", _mergeStatus.ToString()));
         }
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
         if (!_isSquashNeeded.HasValue || !_isSquashNeeded.Value)
         {
            return null;
         }

         return StringUtils.ConvertNewlineWindowsToUnix(textBoxCommitMessage.Text);
      }

      private void subscribeToTimer()
      {
         if (_synchronizationTimerUsers == 0)
         {
            _synchronizationTimer = new Timer
            {
               Interval = mergeRequestUpdateInterval
            };
            _synchronizationTimer.Start();
         }
         ++_synchronizationTimerUsers;

         _synchronizationTimer.Tick += onSynchronizationTimer;
         enableProcessingTimer();
      }

      private void unsubscribeFromTimer()
      {
         disableProcessingTimer();
         _synchronizationTimer.Tick -= onSynchronizationTimer;

         --_synchronizationTimerUsers;
         if (_synchronizationTimerUsers == 0)
         {
            _synchronizationTimer.Stop();
            _synchronizationTimer.Dispose();
            _synchronizationTimer = null;
         }
      }

      private void enableProcessingTimer()
      {
         if (_synchronizationTimerSuspendCount > 0)
         {
            _synchronizationTimerSuspendCount--;
         }
      }

      private void disableProcessingTimer()
      {
         _synchronizationTimerSuspendCount++;
      }

      private void onSynchronizationTimer(object sender, EventArgs e)
      {
         if (IsHandleCreated)
         {
            invokeFetchAndApplyOnTimer();
         }
      }

      private void invokeFetchAndApplyOnInitialize()
      {
         BeginInvoke(new Action(() => applyMergeRequest(fetchUpdatedMergeRequest(_getCache()))), null);
      }

      private void invokeFetchAndApplyOnTimer()
      {
         BeginInvoke(new Action(async () =>
         {
            if (_synchronizationTimerSuspendCount != 0)
            {
               return;
            }

            MergeRequest mergeRequest = await fetchUpdatedMergeRequestAsync();
            if (_synchronizationTimerSuspendCount == 0)
            {
               applyMergeRequest(mergeRequest);
            }
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

      async private Task<MergeRequest> fetchUpdatedMergeRequestAsync()
      {
         disableProcessingTimer();
         try
         {
            DataCache dataCache = await _fetchCache();
            return fetchUpdatedMergeRequest(dataCache);
         }
         finally
         {
            enableProcessingTimer();
         }
      }

      private MergeRequest fetchUpdatedMergeRequest(DataCache dataCache)
      {
         if (dataCache == null || dataCache.MergeRequestCache == null || dataCache.ProjectCache == null)
         {
            Debug.Assert(false);
            return null;
         }

         checkProjectProperties(dataCache.ProjectCache);
         refreshCommits(dataCache.MergeRequestCache);
         return dataCache.MergeRequestCache.GetMergeRequest(_mergeRequestKey);
      }

      async private Task<MergeRequestRebaseResponse> rebaseAsync()
      {
         traceInformation("[AcceptMergeRequestForm] Starting Rebase operation...");
         disableProcessingTimer();
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
            BeginInvoke(new Action(async () =>
            {
               // Don't enable timer processing immediately to prevent flickering of Rebase state:
               // a timer might bring us an outdated state of rebase_in_progress flag.
               // This delay is a way to skip one timer occurrence.
               await Task.Delay(mergeRequestUpdateInterval);
               enableProcessingTimer();
            }), null);
            traceInformation("[AcceptMergeRequestForm] Rebase operation finished");
         }
      }

      async private Task<MergeRequest> mergeAsync(string squashCommitMessage, bool shouldRemoveSourceBranch)
      {
         AcceptMergeRequestParameters parameters = new AcceptMergeRequestParameters(
            null, squashCommitMessage, null, shouldRemoveSourceBranch, null, null);

         traceInformation("[AcceptMergeRequestForm] Starting Merge operation...");
         disableProcessingTimer();
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
            enableProcessingTimer();
            traceInformation("[AcceptMergeRequestForm] Merge operation finished");
         }
      }

      async private Task fixupSquashFlagAsync()
      {
         // Modify MR manually here because for some reason "squash" query parameter
         // sometimes does not affect the merge. For instance, this occurs when
         // Merge_Error is already set to "Failed to squash", in this case simply
         // set "squash=false" has no effect.
         MergeRequest mergeRequest = await setSquashAsync(_isSquashNeeded.Value);
         Debug.Assert(mergeRequest.Squash == _isSquashNeeded.Value);
      }

      async private Task<MergeRequest> toggleDraftAsync()
      {
         string newTitle = StringUtils.ToggleDraftTitle(_title);
         UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
            null, newTitle, null, null, null, null, null, null);
         traceInformation("[AcceptMergeRequestForm] Toggling Draft status...");
         return await applyModification(updateMergeRequestParameters);
      }

      async private Task<MergeRequest> setSquashAsync(bool squash)
      {
         UpdateMergeRequestParameters updateMergeRequestParameters = new UpdateMergeRequestParameters(
            null, null, null, null, null, null, squash, null);
         traceInformation(String.Format("[AcceptMergeRequestForm] Applying Squash={0}...", squash.ToString()));
         return await applyModification(updateMergeRequestParameters);
      }

      async private Task<MergeRequest> applyModification(UpdateMergeRequestParameters parameters)
      {
         disableProcessingTimer();
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
            enableProcessingTimer();
            traceInformation("[AcceptMergeRequestForm] Modification applied");
         }
      }

      private void reportErrorToUser(MergeRequestEditorException ex)
      {
         if (ex is MergeRequestEditorCancelledException)
         {
            return;
         }

         void showDialogAndLogError(string message = "Unknown")
         {
            string defaultMessage = "GitLab could not perform a requested operation. Reason: ";
            disableProcessingTimer();
            MessageBox.Show(defaultMessage + message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            enableProcessingTimer();
            traceError(defaultMessage + message);
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

      private void traceInformation(string message)
      {
         Trace.TraceInformation(getCommonTraceMessage(message));
      }

      private void traceWarning(string message)
      {
         Trace.TraceWarning(getCommonTraceMessage(message));
      }

      private void traceError(string message)
      {
         Trace.TraceError(getCommonTraceMessage(message));
      }

      private string getCommonTraceMessage(string message)
      {
         return String.Format("[AcceptMergeRequestForm] {0}. MRK: IId={1}, Project={2}",
            message, _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName);
      }

      private static readonly int mergeRequestUpdateInterval = 1000; // ms
      private static Timer _synchronizationTimer;
      private static int _synchronizationTimerUsers = 0;
      private int _synchronizationTimerSuspendCount = 0;

      private readonly string _repositoryPath;
      private readonly MergeRequestKey _mergeRequestKey;
      private readonly Func<MergeRequestKey, string, User, string, Task> _onOpenDiscussions;
      private readonly Action _onMerged;
      private readonly MarkdownPipeline _mdPipeline;
      private readonly Func<Task<DataCache>> _fetchCache;
      private readonly Func<DataCache> _getCache;
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
         CannotBeMerged,
         Unchecked
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

