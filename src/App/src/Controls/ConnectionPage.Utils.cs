using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SearchQuery = mrHelper.GitLabClient.SearchQuery;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using static mrHelper.App.Helpers.ConfigurationHelper;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
   {
      public string GetCurrentHostName()
      {
         return HostName;
      }

      public string GetCurrentAccessToken()
      {
         return Program.Settings.GetAccessToken(HostName);
      }

      public string GetCurrentProjectName()
      {
         return getMergeRequestKey(null)?.ProjectKey.ProjectName ?? String.Empty;
      }

      public int GetCurrentMergeRequestIId()
      {
         return getMergeRequestKey(null)?.IId ?? 0;
      }

      // Helpers

      private DataCache getDataCacheByName(string name)
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            if (name == mode.ToString())
            {
               return getDataCache(mode);
            }
         }
         Debug.Assert(false);
         return null;
      }

      private string getDataCacheName(DataCache dataCache)
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            if (getDataCache(mode) == dataCache)
            {
               return mode.ToString();
            }
         }
         Debug.Assert(false);
         return String.Empty;
      }

      private DataCache getDataCache(EDataCacheType mode)
      {
         switch (mode)
         {
            case EDataCacheType.Live:
               return _liveDataCache;

            case EDataCacheType.Search:
               return _searchDataCache;

            case EDataCacheType.Recent:
               return _recentDataCache;
         }

         Debug.Assert(false);
         return null;
      }

      private EDataCacheType getCurrentTabDataCacheType()
      {
         if (tabControlMode.SelectedTab == tabPageSearch)
         {
            return EDataCacheType.Search;
         }
         else if (tabControlMode.SelectedTab == tabPageRecent)
         {
            return EDataCacheType.Recent;
         }

         Debug.Assert(tabControlMode.SelectedTab == tabPageLive);
         return EDataCacheType.Live;
      }

      private void forEachListView(Action<MergeRequestListView> action)
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            action(getListView(mode));
         }
      }

      private MergeRequestListView getListView(EDataCacheType mode)
      {
         switch (mode)
         {
            case EDataCacheType.Live:
               return listViewLiveMergeRequests;

            case EDataCacheType.Search:
               return listViewFoundMergeRequests;

            case EDataCacheType.Recent:
               return listViewRecentMergeRequests;
         }

         Debug.Assert(false);
         return null;
      }

      private EDataCacheType getListViewType(MergeRequestListView listView)
      {
         if (listView == listViewLiveMergeRequests)
         {
            return EDataCacheType.Live;
         }
         else if (listView == listViewFoundMergeRequests)
         {
            return EDataCacheType.Search;
         }
         else if (listView == listViewRecentMergeRequests)
         {
            return EDataCacheType.Recent;
         }

         Debug.Assert(false);
         return EDataCacheType.Live;
      }

      private MergeRequest getMergeRequest(MergeRequestListView proposedListView)
      {
         MergeRequestListView listView = proposedListView ?? getListView(getCurrentTabDataCacheType());
         FullMergeRequestKey? fmk = listView.GetSelectedMergeRequest();
         return fmk.HasValue ? fmk.Value.MergeRequest : null;
      }

      private MergeRequestKey? getMergeRequestKey(MergeRequestListView proposedListView)
      {
         MergeRequestListView listView = proposedListView ?? getListView(getCurrentTabDataCacheType());
         FullMergeRequestKey? fmk = listView.GetSelectedMergeRequest();
         return fmk.HasValue && fmk.Value.MergeRequest != null
            ? new MergeRequestKey(fmk.Value.ProjectKey, fmk.Value.MergeRequest.IId)
            : new Nullable<MergeRequestKey>();
      }

      // List View

      private bool doesRequireFixedGroupCollection(EDataCacheType mode)
      {
         return ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings) && mode == EDataCacheType.Live;
      }

      private void initializeListViewGroups(EDataCacheType mode, string hostname)
      {
         Controls.MergeRequestListView listView = getListView(mode);
         listView.Items.Clear();
         listView.Groups.Clear();

         IEnumerable<ProjectKey> projectKeys = getEnabledProjects(hostname);
         foreach (ProjectKey projectKey in projectKeys)
         {
            listView.CreateGroupForProject(projectKey, false);
         }
      }

      private void onDataCacheSelectionChanged()
      {
         forEachListView(listView => listView.DeselectAllListViewItems());
      }

      private void onMergeRequestSelectionChanged(EDataCacheType mode)
      {
         MergeRequestListView listView = getListView(mode);
         FullMergeRequestKey? fmkOpt = listView.GetSelectedMergeRequest();
         if (!fmkOpt.HasValue)
         {
            Trace.TraceInformation(String.Format(
               "[ConnectionPage] User deselected merge request. Mode={0}",
               getCurrentTabDataCacheType().ToString()));
            disableSelectedMergeRequestControls();
            return;
         }

         FullMergeRequestKey fmk = fmkOpt.Value;
         if (fmk.MergeRequest == null)
         {
            return; // List view item with summary information for a collapsed group
         }

         Trace.TraceInformation(String.Format(
            "[ConnectionPage] User requested to change merge request to IId {0}, mode = {1}",
            fmk.MergeRequest.IId.ToString(), getCurrentTabDataCacheType().ToString()));

         DataCache dataCache = getDataCache(mode);
         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         updateMergeRequestDetails(fmk);
         updateRevisionBrowserTree(dataCache, mrk);

         string status = _latestStorageUpdateStatus.TryGetValue(mrk, out string value) ? value : String.Empty;
         StorageStatusChanged?.Invoke(this);
         onMergeRequestActionsEnabled();

         if (mode == EDataCacheType.Live)
         {
            _lastMergeRequestsByHosts[fmk.ProjectKey.HostName] = mrk;
         }
      }

      private void updateMergeRequestList(EDataCacheType mode)
      {
         DataCache dataCache = getDataCache(mode);
         IMergeRequestCache mergeRequestCache = dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return;
         }

         MergeRequestListView listView = getListView(mode);
         if (!doesRequireFixedGroupCollection(mode))
         {
            listView.UpdateGroups();
         }
         listView.UpdateItems();

         if (mode == EDataCacheType.Live)
         {
            if (listView.Items.Count > 0 || Program.Settings.DisplayFilterEnabled)
            {
               enableMergeRequestFilterControls(true);
               listView.Enabled = true;
            }
            SummaryColorChanged?.Invoke(this);
            onLiveMergeRequestListRefreshed();
         }
         else if (listView.Items.Count > 0)
         {
            listView.Enabled = true;
         }
      }

      // Revision Browser

      private void updateRevisionBrowserTree(DataCache dataCache, MergeRequestKey mrk)
      {
         IMergeRequestCache cache = dataCache.MergeRequestCache;
         if (cache != null)
         {
            GitLabSharp.Entities.Version latestVersion = cache.GetLatestVersion(mrk);
            IEnumerable<GitLabSharp.Entities.Version> versions = cache.GetVersions(mrk);
            IEnumerable<Commit> commits = cache.GetCommits(mrk);

            bool hasObjects = commits.Any() || versions.Any();
            if (hasObjects)
            {
               RevisionBrowserModelData data = new RevisionBrowserModelData(latestVersion?.Base_Commit_SHA,
                  commits, versions, getReviewedRevisions(mrk));
               revisionBrowser.SetData(data, ConfigurationHelper.GetDefaultRevisionType(Program.Settings));
            }
            else
            {
               clearRevisionBrowser();
            }
         }
      }

      private void clearRevisionBrowser()
      {
         revisionBrowser.ClearData(ConfigurationHelper.GetDefaultRevisionType(Program.Settings));
      }

      private void getShaForDiffBetweenSelected(out string left, out string right,
         out IEnumerable<string> included, out RevisionType? type)
      {
         string[] selected = revisionBrowser.GetSelectedSha(out type);
         if (selected.Count() != 2)
         {
            left = String.Empty;
            right = String.Empty;
            included = new List<string>();
            Debug.Assert(false); // shall be not available
            return;
         }

         left = selected[0];
         right = selected[1];
         included = revisionBrowser.GetIncludedBySelectedSha();
      }

      private void getShaForDiffWithBase(out string left, out string right,
         out IEnumerable<string> included, out RevisionType? type)
      {
         string[] selected = revisionBrowser.GetSelectedSha(out type);
         if (selected.Count() != 1)
         {
            left = String.Empty;
            right = String.Empty;
            included = new List<string>();
            Debug.Assert(false); // shall be not available
            return;
         }

         left = revisionBrowser.GetBaseCommitSha();
         right =  selected[0];
         included = revisionBrowser.GetIncludedBySelectedSha();
      }

      private bool checkIfMergeRequestCanBeCreated()
      {
         string hostname = HostName;
         User currentUser = CurrentUser;
         if (hostname == String.Empty || currentUser == null || _expressionResolver == null)
         {
            Debug.Assert(false);
            MessageBox.Show("Cannot create a merge request", "Internal error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("Unexpected application state." +
               "hostname is empty string={0}, currentUser is null={1}, _expressionResolver is null={2}",
               hostname == String.Empty, currentUser == null, _expressionResolver == null);
            return false;
         }
         return true;
      }

      // Splitter

      private bool isUserMovingSplitter(SplitContainer splitter)
      {
         Debug.Assert(splitter == splitContainerPrimary || splitter == splitContainerSecondary);

         return splitter == splitContainerPrimary ? _userIsMovingSplitter1 : _userIsMovingSplitter2;
      }

      private void onUserIsMovingSplitter(SplitContainer splitter, bool value)
      {
         Debug.Assert(splitter == splitContainerPrimary || splitter == splitContainerSecondary);

         if (splitter == splitContainerPrimary)
         {
            if (!value)
            {
               // move is finished, store the value
               saveSplitterDistanceToConfig(splitContainerPrimary);
            }
            _userIsMovingSplitter1 = value;
         }
         else
         {
            if (!value)
            {
               // move is finished, store the value
               saveSplitterDistanceToConfig(splitContainerSecondary);
            }
            _userIsMovingSplitter2 = value;
         }
      }

      private bool setSplitterDistanceSafe(SplitContainer splitContainer, int distance)
      {
         switch (splitContainer.Orientation)
         {
            case Orientation.Vertical:
               if (distance >= splitContainer.Panel1MinSize
                && distance <= splitContainer.Width - splitContainer.Panel2MinSize)
               {
                  splitContainer.SplitterDistance = distance;
                  return true;
               }
               break;
            case Orientation.Horizontal:
               if (distance >= splitContainer.Panel1MinSize
                && distance <= splitContainer.Height - splitContainer.Panel2MinSize)
               {
                  splitContainer.SplitterDistance = distance;
                  return true;
               }
               break;
         }
         return false;
      }

      enum ResetSplitterDistanceMode
      {
         Minimum,
         Middle,
         UserDefined
      }

      private int readSplitterDistanceFromConfig(SplitContainer splitContainer)
      {
         if (splitContainer == splitContainerPrimary)
         {
            return Program.Settings.PrimarySplitContainerDistance;
         }
         else if (splitContainer == splitContainerSecondary)
         {
            return Program.Settings.SecondarySplitContainerDistance;
         }
         Debug.Assert(false);
         return 0;
      }

      private void saveSplitterDistanceToConfig(SplitContainer splitContainer)
      {
         if (splitContainer == splitContainerPrimary)
         {
            Program.Settings.PrimarySplitContainerDistance = splitContainer.SplitterDistance;
         }
         else if (splitContainer == splitContainerSecondary)
         {
            Program.Settings.SecondarySplitContainerDistance = splitContainer.SplitterDistance;
         }
         else
         {
            Debug.Assert(false);
         }
      }

      private bool resetSplitterDistance(SplitContainer splitContainer, ResetSplitterDistanceMode mode)
      {
         int splitterDistance = 0;
         switch (mode)
         {
            case ResetSplitterDistanceMode.Minimum:
               splitterDistance = splitContainer.Panel1MinSize;
               break;

            case ResetSplitterDistanceMode.Middle:
               switch (splitContainer.Orientation)
               {
                  case Orientation.Vertical: splitterDistance = splitContainer.Width / 2; break;
                  case Orientation.Horizontal: splitterDistance = splitContainer.Height / 2; break;
               }
               break;

            case ResetSplitterDistanceMode.UserDefined:
               splitterDistance = readSplitterDistanceFromConfig(splitContainer);
               if (splitterDistance == 0)
               {
                  return resetSplitterDistance(splitContainer, ResetSplitterDistanceMode.Middle);
               }
               break;
         }
         if (setSplitterDistanceSafe(splitContainer, splitterDistance))
         {
            saveSplitterDistanceToConfig(splitContainer);
            return true;
         }
         return false;
      }

      private void updateSplitterOrientation()
      {
         switch (ConfigurationHelper.GetMainWindowLayout(Program.Settings))
         {
            case ConfigurationHelper.MainWindowLayout.Horizontal:
               splitContainerPrimary.Orientation = Orientation.Vertical;
               splitContainerSecondary.Orientation = Orientation.Horizontal;
               break;

            case ConfigurationHelper.MainWindowLayout.Vertical:
               splitContainerPrimary.Orientation = Orientation.Horizontal;
               splitContainerSecondary.Orientation = Orientation.Vertical;
               break;
         }
      }

      // Modes

      private void selectTab(EDataCacheType mode)
      {
         switch (mode)
         {
            case EDataCacheType.Live:
               tabControlMode.SelectedTab = tabPageLive;
               break;

            case EDataCacheType.Search:
               tabControlMode.SelectedTab = tabPageSearch;
               break;

            case EDataCacheType.Recent:
               tabControlMode.SelectedTab = tabPageRecent;
               break;

            default:
               Debug.Assert(false);
               break;
         }
      }

      private void switchTabAndSelectMergeRequestOrAnythingElse(EDataCacheType mode, MergeRequestKey? mrk)
      {
         switchMode(mode).SelectMergeRequest(mrk, false);
      }

      private bool switchTabAndSelectMergeRequest(EDataCacheType mode, MergeRequestKey? mrk)
      {
         return switchMode(mode).SelectMergeRequest(mrk, true);
      }

      private MergeRequestListView switchMode(EDataCacheType mode)
      {
         switch (mode)
         {
            case EDataCacheType.Live:
               RequestLive?.Invoke(this);
               return listViewLiveMergeRequests;

            case EDataCacheType.Search:
               RequestSearch?.Invoke(this);
               return listViewFoundMergeRequests;

            case EDataCacheType.Recent:
               RequestRecent?.Invoke(this);
               return listViewRecentMergeRequests;

            default:
               Debug.Assert(false);
               break;
         }
         return getListView(getCurrentTabDataCacheType());
      }

      // Status

      private void addOperationRecord(string text)
      {
         StatusChanged?.Invoke(text);
      }

      private bool canUpdateStorageForMergeRequest(MergeRequestKey? mrk)
      {
         return mrk.HasValue
            && !_mergeRequestsUpdatingByUserRequest.Contains(mrk.Value)
            &&  _mergeRequestsUpdatingByUserRequest.Count() < Constants.MaxMergeRequestStorageUpdatesInParallel;
      }

      private void onStorageUpdateStateChange()
      {
         CanAbortCloneChanged?.Invoke(this);
      }

      private void onStorageUpdateProgressChange(string text, MergeRequestKey mrk)
      {
         if (InvokeRequired)
         {
            Invoke(new Action<string, MergeRequestKey>(onStorageUpdateProgressChange), new object[] { text, mrk });
         }
         else
         {
            _latestStorageUpdateStatus[mrk] = text;
            StorageStatusChanged?.Invoke(this);
         }
      }

      private void setConnectionStatus(EConnectionStateInternal? status)
      {
         Trace.TraceInformation(
            "[ConnectionPage] Set connection status to {0}. Old status is {1}. Lost connection info has value: {2}.",
            !status.HasValue ? "null" : status.Value.ToString(),
            !_connectionStatus.HasValue ? "null" : _connectionStatus.Value.ToString(),
            isConnectionLost().ToString());

         _connectionStatus = status;
         onConnectionStatusChanged();
      }

      private void createLostConnectionInfo()
      {
         _lostConnectionInfo = new LostConnectionInfo(DateTime.Now);
      }

      private void resetLostConnectionInfo()
      {
         _lostConnectionInfo = null;
      }

      private void onConnectionStatusChanged()
      {
         ConnectionStatusChanged?.Invoke(this);
      }

      private void onConnectionLost()
      {
         if (!isConnectionLost())
         {
            createLostConnectionInfo();
            onConnectionStatusChanged();
         }
      }

      private void onConnectionRestored()
      {
         if (!isConnectionLost())
         {
            return;
         }

         resetLostConnectionInfo();
         onConnectionStatusChanged();
         if (!_connectionStatus.HasValue) // AKA Not Connected
         {
            Connect(null);
         }
         else if (_connectionStatus.Value == EConnectionStateInternal.Connected)
         {
            ReloadAllOnConnectionRestore();
         }
         else if (_connectionStatus.Value == EConnectionStateInternal.ConnectingLive)
         {
            Connect(null);
         }
         else if (_connectionStatus.Value == EConnectionStateInternal.ConnectingRecent)
         {
            loadRecentMergeRequests();
         }
         else
         {
            Debug.Assert(false);
         }
      }

      private bool isConnectionLost()
      {
         return _lostConnectionInfo.HasValue;
      }

      // Controls

      private void updateMergeRequestDetails(FullMergeRequestKey? fmkOpt)
      {
         if (!fmkOpt.HasValue)
         {
            richTextBoxMergeRequestDescription.Text = String.Empty;
            linkLabelConnectedTo.Text = String.Empty;
         }
         else
         {
            FullMergeRequestKey fmk = fmkOpt.Value;

            string rawTitle = !String.IsNullOrEmpty(fmk.MergeRequest.Title) ? fmk.MergeRequest.Title : "Title is empty";
            string title = MarkDownUtils.ConvertToHtml(rawTitle, String.Empty, _mdPipeline);

            string rawDescription = !String.IsNullOrEmpty(fmk.MergeRequest.Description)
               ? fmk.MergeRequest.Description : "Description is empty";
            string uploadsPrefix = StringUtils.GetUploadsPrefix(fmk.ProjectKey);
            string description = MarkDownUtils.ConvertToHtml(rawDescription, uploadsPrefix, _mdPipeline);

            string body = String.Format("<b>Title</b><br>{0}<br><b>Description</b><br>{1}", title, description);
            richTextBoxMergeRequestDescription.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
            linkLabelConnectedTo.Text = fmk.MergeRequest.Web_Url;
         }

         richTextBoxMergeRequestDescription.Update();
         _toolTip.SetToolTip(linkLabelConnectedTo, linkLabelConnectedTo.Text);
      }

      private void disableLiveTabControls()
      {
         getListView(EDataCacheType.Live).DisableListView();
         enableMergeRequestFilterControls(false);
      }

      private void disableSearchTabControls()
      {
         linkLabelNewSearch.Enabled = false;
         getListView(EDataCacheType.Search).DisableListView();
      }

      private void enableSearchTabControls()
      {
         linkLabelNewSearch.Enabled = true;
      }

      private void disableRecentTabControls()
      {
         getListView(EDataCacheType.Recent).DisableListView();
      }

      private void disableSelectedMergeRequestControls()
      {
         updateMergeRequestDetails(null);
         clearRevisionBrowser();

         StorageStatusChanged?.Invoke(this);
         onMergeRequestActionsEnabled();
      }

      private void enableMergeRequestFilterControls(bool enabled)
      {
         checkBoxDisplayFilter.Enabled = enabled;
         textBoxDisplayFilter.Enabled = enabled;
      }

      private void onRedrawTimer(object sender, EventArgs e)
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            getListView(mode).Refresh();
         }

         revisionBrowser.Refresh();

         LatestListRefreshTimestampChanged?.Invoke(this);
      }

      // Properties

      private void onWrapLongRowsChanged()
      {
         updateMergeRequestList(getCurrentTabDataCacheType());
      }

      private void onMainWindowLayoutChanged()
      {
         if (splitContainerPrimary.Width == 0 || splitContainerSecondary.Width == 0)
         {
            return;
         }

         splitContainerPrimary.SuspendLayout();
         splitContainerSecondary.SuspendLayout();

         resetSplitterDistance(splitContainerPrimary, ResetSplitterDistanceMode.Minimum);
         resetSplitterDistance(splitContainerSecondary, ResetSplitterDistanceMode.Minimum);

         updateSplitterOrientation();

         resetSplitterDistance(splitContainerPrimary, ResetSplitterDistanceMode.Middle);
         resetSplitterDistance(splitContainerSecondary, ResetSplitterDistanceMode.Middle);

         splitContainerPrimary.ResumeLayout();
         splitContainerSecondary.ResumeLayout();
      }

      // Filter

      private void onTextBoxDisplayFilterUpdate()
      {
         Program.Settings.DisplayFilter = textBoxDisplayFilter.Text;
         if (_mergeRequestFilter != null)
         {
            _mergeRequestFilter.Filter = createMergeRequestFilterState();
         }
      }

      private void applyFilterChange(bool enabled)
      {
         Program.Settings.DisplayFilterEnabled = enabled;
         if (_mergeRequestFilter != null)
         {
            _mergeRequestFilter.Filter = createMergeRequestFilterState();
         }
      }

      // Misc

      private void onMergeRequestActionsEnabled()
      {
         CanDiffToolChanged?.Invoke(this);
         CanDiscussionsChanged?.Invoke(this);
         CanAddCommentChanged?.Invoke(this);
         CanNewThreadChanged?.Invoke(this);
         CanAbortCloneChanged?.Invoke(this);
         CanTrackTimeChanged?.Invoke(this);
         EnabledCustomActionsChanged?.Invoke(this);
      }

      private string getDefaultProjectName()
      {
         MergeRequestListView listView = getListView(EDataCacheType.Live);
         MergeRequestKey? currentMergeRequestKey = getMergeRequestKey(listView);
         if (currentMergeRequestKey.HasValue)
         {
            return currentMergeRequestKey.Value.ProjectKey.ProjectName;
         }

         if (listView.Groups.Count > 0)
         {
            return listView.Groups[0].Name;
         }

         ProjectKey? project = getDataCache(EDataCacheType.Live)?.MergeRequestCache?.GetProjects()?.FirstOrDefault();
         if (project.HasValue)
         {
            return project.Value.ProjectName;
         }

         return String.Empty;
      }

      private void changeProjectEnabledState(ProjectKey projectKey, bool state)
      {
         Dictionary<string, bool> projects = ConfigurationHelper.GetProjectsForHost(
            projectKey.HostName, Program.Settings).ToDictionary(item => item.Item1, item => item.Item2);
         Debug.Assert(projects.ContainsKey(projectKey.ProjectName));
         projects[projectKey.ProjectName] = state;

         ConfigurationHelper.SetProjectsForHost(
            projectKey.HostName,
            new StringToBooleanCollection(Enumerable.Zip(
               projects.Keys, projects.Values, (x, y) => new Tuple<string, bool>(x, y))),
            Program.Settings);
      }

      private NewMergeRequestProperties getDefaultNewMergeRequestProperties(string hostname,
         User currentUser, string projectName)
      {
         // This state is expected to be used only once, next times 'persistent storage' is used
         NewMergeRequestProperties factoryProperties = new NewMergeRequestProperties(
            projectName, null, null, currentUser.Username, true, true);
         return _newMergeRequestDialogStatesByHosts.Data.TryGetValue(hostname, out var value) ? value : factoryProperties;
      }

      private void requestUpdates(EDataCacheType mode, MergeRequestKey? mrk, int[] intervals)
      {
         DataCache dataCache = getDataCache(mode);
         dataCache?.MergeRequestCache?.RequestUpdate(mrk, intervals);
         dataCache?.DiscussionCache?.RequestUpdate(mrk, intervals);
      }

      private void showWarningAboutIntegrationWithGitUI()
      {
         if (Program.Settings.ShowWarningOnCreateMergeRequest && (_integratedInGitExtensions || _integratedInSourceTree))
         {
            string tools = "Git Extensions or Source Tree";
            if (_integratedInSourceTree && !_integratedInGitExtensions)
            {
               tools = "Source Tree";
            }
            else if (_integratedInGitExtensions && !_integratedInSourceTree)
            {
               tools = "Git Extensions";
            }

            string message = String.Format(
               "Note: It is much easier to create a new merge request using integration of mrHelper with {0}. " +
               "Just select a commit of a remote branch in {0} and press Create Merge Request in menu.", tools);
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Program.Settings.ShowWarningOnCreateMergeRequest = false;
         }
      }

      private bool areLongCachesReady(DataCache dataCache)
      {
         return (dataCache?.ProjectCache?.GetProjects()?.Any() ?? false)
             && (dataCache?.UserCache?.GetUsers()?.Any() ?? false);
      }

      private void onLongCachesReady()
      {
         CanEditChanged?.Invoke(this);
         CanMergeChanged?.Invoke(this);
         CanCreateNewChanged?.Invoke(this);
         CanSearchChanged?.Invoke(this);
         enableSearchTabControls();
      }
   }
}

