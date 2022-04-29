using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using static mrHelper.App.Helpers.ConfigurationHelper;
using mrHelper.Common.Constants;
using mrHelper.CustomActions;

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

      private bool isMergeRequestCached(EDataCacheType type, int mergeRequestId)
      {
         IMergeRequestCache mergeRequestCache = getDataCache(type)?.MergeRequestCache;
         return mergeRequestCache != null && mergeRequestCache.GetProjects()
            .SelectMany(projectKey => mergeRequestCache.GetMergeRequests(projectKey))
            .Any(mergeRequest => mergeRequest.Id == mergeRequestId);
      }

      private bool isMergeRequestHidden(EDataCacheType type, MergeRequest mergeRequest)
      {
         return mergeRequest != null && isMergeRequestHidden(type, mergeRequest.Id);
      }

      private bool isMergeRequestHidden(EDataCacheType type, int mergeRequestId)
      {
         return getHiddenMergeRequestIds(type).Any(id => mergeRequestId == id);
      }

      private IEnumerable<int> getHiddenMergeRequestIds(EDataCacheType type)
      {
         return getKeywordCollection(type).GetExcluded()
            .Where(keyword => int.TryParse(keyword, out int _))
            .Select(keyword => int.Parse(keyword));
      }

      private IEnumerable<int> selectNotCachedMergeRequestIds(EDataCacheType type,
         IEnumerable<int> mergeRequestIds)
      {
         return mergeRequestIds.Where(id => !isMergeRequestCached(type, id));
      }

      // List View

      private void initializeListViewGroups(EDataCacheType mode)
      {
         Controls.MergeRequestListView listView = getListView(mode);
         listView.Items.Clear();
         listView.Groups.Clear();

         IEnumerable<string> projectnames = ConfigurationHelper.GetEnabledProjects(HostName, Program.Settings);
         IEnumerable<ProjectKey> projectKeys = projectnames.Select(name => new ProjectKey(HostName, name));
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
            cleanUpLastMergeRequestByHost(listView);
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
         descriptionSplitContainerSite.UpdateData(fmk, dataCache);
         revisionSplitContainerSite.SetData(mrk, dataCache);
         updateConnectedToLabel(fmk);

         string status = _latestStorageUpdateStatus.TryGetValue(mrk, out string value) ? value : String.Empty;
         StorageStatusChanged?.Invoke(this);
         onMergeRequestActionsEnabled();

         setLastMergeRequestByHost(mrk);
      }

      private void cleanUpLastMergeRequestByHost(MergeRequestListView listView)
      {
         if (getCurrentTabDataCacheType() != EDataCacheType.Live)
         {
            return;
         }

         if (_lastMergeRequestsByHosts.TryGetValue(HostName, out MergeRequestKey lastMrk))
         {
            if (listView.IsGroupCollapsed(lastMrk.ProjectKey))
            {
               _lastMergeRequestsByHosts.Remove(HostName);
            }
         }
      }

      private void setLastMergeRequestByHost(MergeRequestKey mrk)
      {
         if (getCurrentTabDataCacheType() != EDataCacheType.Live)
         {
            return;
         }

         _lastMergeRequestsByHosts[HostName] = mrk;
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
         listView.UpdateGroups();
         listView.UpdateItems();

         switch (mode)
         {
            case EDataCacheType.Live:
               if (listView.Items.Count > 0 || Program.Settings.DisplayFilterEnabled)
               {
                  enableMergeRequestFilterControls(mode, true);
                  listView.Enabled = true;
               }
               SummaryColorChanged?.Invoke(this);
               onLiveMergeRequestListRefreshed();
               break;

            case EDataCacheType.Recent:
               if (listView.Items.Count > 0 || Program.Settings.DisplayFilterRecentEnabled)
               {
                  enableMergeRequestFilterControls(mode, true);
                  listView.Enabled = true;
               }
               break;

            case EDataCacheType.Search:
               if (listView.Items.Count > 0)
               {
                  listView.Enabled = true;
               }
               break;

            default:
               Debug.Assert(false);
               break;
         }
      }

      // Revision Browser

      private RevisionBrowser getRevisionBrowser()
      {
         return revisionSplitContainerSite.RevisionBrowser;
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
         return _userIsMovingSplitter.TryGetValue(splitter.Name, out bool value) && value;
      }

      private void onUserIsMovingSplitter(SplitContainer splitter, bool value)
      {
         if (!value) // move is finished
         {
            Trace.TraceInformation("[ConnectionPage] onUserIsMovingSplitter({0}, false)", splitter.Name);
            saveSplitterDistanceToConfig(splitter);
         }
         _userIsMovingSplitter[splitter.Name] = value;
      }

      private bool setSplitterDistanceSafe(SplitContainer splitContainer, int distance)
      {
         bool ok = false;
         switch (splitContainer.Orientation)
         {
            case Orientation.Vertical:
               if (distance >= splitContainer.Panel1MinSize
                && distance <= splitContainer.Width - splitContainer.Panel2MinSize)
               {
                  ok = true;
               }
               break;

            case Orientation.Horizontal:
               if (distance >= splitContainer.Panel1MinSize
                && distance <= splitContainer.Height - splitContainer.Panel2MinSize)
               {
                  ok = true;
               }
               break;
         }

         Trace.TraceInformation(
            "[ConnectionPage] setSplitterDistanceSafe({0}, {1}): {2}",
            splitContainer.Name, distance, ok.ToString());

         if (ok)
         {
            splitContainer.SplitterDistance = distance;
         }
         return ok;
      }

      enum ResetSplitterDistanceMode
      {
         Minimum,
         Middle,
         UserDefined
      }

      private int readSplitterDistanceFromConfig(SplitContainer splitContainer)
      {
         int result = 0;
         if (splitContainer == splitContainerPrimary)
         {
            result = Program.Settings.PrimarySplitContainerDistance;
         }
         else if (splitContainer == splitContainerSecondary)
         {
            result = Program.Settings.SecondarySplitContainerDistance;
         }
         else if (splitContainer == descriptionSplitContainerSite.SplitContainer)
         {
            result = Program.Settings.DescriptionSplitContainerDistance;
         }
         else if (splitContainer == revisionSplitContainerSite.SplitContainer)
         {
            result = Program.Settings.RevisionSplitContainerDistance;
         }
         else
         {
            Debug.Assert(false);
         }

         Trace.TraceInformation(
            "[ConnectionPage] readSplitterDistanceFromConfig({0}): {1}",
            splitContainer.Name, result);

         return result;
      }

      private void saveSplitterDistanceToConfig(SplitContainer splitContainer)
      {
         Trace.TraceInformation(
            "[ConnectionPage] saveSplitterDistanceToConfig({0}, {1})",
            splitContainer.Name, splitContainer.SplitterDistance);

         if (splitContainer == splitContainerPrimary)
         {
            Program.Settings.PrimarySplitContainerDistance = splitContainer.SplitterDistance;
         }
         else if (splitContainer == splitContainerSecondary)
         {
            Program.Settings.SecondarySplitContainerDistance = splitContainer.SplitterDistance;
         }
         else if (splitContainer == descriptionSplitContainerSite.SplitContainer)
         {
            Program.Settings.DescriptionSplitContainerDistance = splitContainer.SplitterDistance;
         }
         else if (splitContainer == revisionSplitContainerSite.SplitContainer)
         {
            Program.Settings.RevisionSplitContainerDistance = splitContainer.SplitterDistance;
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

         bool ok = setSplitterDistanceSafe(splitContainer, splitterDistance);
         Trace.TraceInformation(
            "[ConnectionPage] resetSplitterDistance({0}, {1}): {2}, splitContainer Orientation={3}, Width/Height={4}",
            splitContainer.Name, mode.ToString(), ok.ToString(),
            splitContainer.Orientation.ToString(),
            splitContainer.Orientation == Orientation.Vertical ? splitContainer.Width : splitContainer.Height);

         if (ok)
         {
            saveSplitterDistanceToConfig(splitContainer);
         }
         return ok;
      }

      private void updateSplitterOrientation()
      {
         Orientation primarySplitterOldOrientation = splitContainerPrimary.Orientation;
         Orientation secondarySplitterOldOrientation = splitContainerSecondary.Orientation;

         Orientation primarySplitterNewOrientation = primarySplitterOldOrientation;
         Orientation secondarySplitterNewOrientation = secondarySplitterOldOrientation;

         switch (ConfigurationHelper.GetMainWindowLayout(Program.Settings))
         {
            case ConfigurationHelper.MainWindowLayout.Horizontal:
               primarySplitterNewOrientation = Orientation.Vertical;
               secondarySplitterNewOrientation = Orientation.Horizontal;
               break;

            case ConfigurationHelper.MainWindowLayout.Vertical:
               primarySplitterNewOrientation = Orientation.Horizontal;
               secondarySplitterNewOrientation = Orientation.Vertical;
               break;

            default:
               Debug.Assert(false);
               break;
         }

         splitContainerPrimary.Orientation = primarySplitterNewOrientation;
         splitContainerSecondary.Orientation = secondarySplitterNewOrientation;

         Trace.TraceInformation(
            "[ConnectionPage] updateSplitterOrientation(): " +
            "Primary splitter orientation: {0} -> {1}, " +
            "Secondary splitter orientation: {2} -> {3}",
            primarySplitterOldOrientation.ToString(), primarySplitterNewOrientation.ToString(),
            secondarySplitterOldOrientation.ToString(), secondarySplitterNewOrientation.ToString());
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

      private void updateConnectedToLabel(FullMergeRequestKey? fmkOpt)
      {
         if (!fmkOpt.HasValue)
         {
            linkLabelConnectedTo.Text = String.Empty;
         }
         else
         {
            FullMergeRequestKey fmk = fmkOpt.Value;
            linkLabelConnectedTo.Text = fmk.MergeRequest.Web_Url;
         }
         _toolTip.SetToolTip(linkLabelConnectedTo, linkLabelConnectedTo.Text);
      }

      private void disableLiveTabControls()
      {
         getListView(EDataCacheType.Live).DisableListView();
         enableMergeRequestFilterControls(EDataCacheType.Live, false);
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
         enableMergeRequestFilterControls(EDataCacheType.Recent, false);
      }

      private void disableSelectedMergeRequestControls()
      {
         descriptionSplitContainerSite.ClearData();
         revisionSplitContainerSite.ClearData();
         updateConnectedToLabel(null);

         StorageStatusChanged?.Invoke(this);
         onMergeRequestActionsEnabled();
      }

      private void enableMergeRequestFilterControls(EDataCacheType type, bool enabled)
      {
         switch (type)
         {
            case EDataCacheType.Live:
               checkBoxDisplayFilter.Enabled = enabled;
               textBoxDisplayFilter.Enabled = enabled;
               break;
            case EDataCacheType.Recent:
               checkBoxDisplayFilterRecent.Enabled = enabled;
               textBoxDisplayFilterRecent.Enabled = enabled;
               break;
            case EDataCacheType.Search:
            default:
               break;
         }
      }

      private void onRedrawTimer(object sender, EventArgs e)
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            getListView(mode).Refresh();
         }

         getRevisionBrowser().Refresh();

         LatestListRefreshTimestampChanged?.Invoke(this);
      }

      // Properties

      private void onWrapLongRowsChanged()
      {
         updateMergeRequestList(getCurrentTabDataCacheType());
      }

      private void onMainWindowLayoutChanged()
      {
         bool ignoreChange = splitContainerPrimary.Width == 0 || splitContainerSecondary.Width == 0;
         Trace.TraceInformation("[ConnectionPage] onMainWindowLayoutChanged(): ignoreChange={0}", ignoreChange);
         if (ignoreChange)
         {
            return;
         }

         splitContainerPrimary.SuspendLayout();
         splitContainerSecondary.SuspendLayout();
         descriptionSplitContainerSite.SplitContainer.SuspendLayout();
         revisionSplitContainerSite.SplitContainer.SuspendLayout();

         resetSplitterDistance(splitContainerPrimary, ResetSplitterDistanceMode.Minimum);
         resetSplitterDistance(splitContainerSecondary, ResetSplitterDistanceMode.Minimum);
         resetSplitterDistance(descriptionSplitContainerSite.SplitContainer, ResetSplitterDistanceMode.Minimum);
         resetSplitterDistance(revisionSplitContainerSite.SplitContainer, ResetSplitterDistanceMode.Minimum);

         updateSplitterOrientation();

         resetSplitterDistance(splitContainerPrimary, ResetSplitterDistanceMode.Middle);
         resetSplitterDistance(splitContainerSecondary, ResetSplitterDistanceMode.Middle);
         resetSplitterDistance(descriptionSplitContainerSite.SplitContainer, ResetSplitterDistanceMode.Middle);
         resetSplitterDistance(revisionSplitContainerSite.SplitContainer, ResetSplitterDistanceMode.Middle);

         splitContainerPrimary.ResumeLayout();
         splitContainerSecondary.ResumeLayout();
         descriptionSplitContainerSite.SplitContainer.ResumeLayout();
         revisionSplitContainerSite.SplitContainer.ResumeLayout();
      }

      // Filter

      private void setFilterText(string text)
      {
         textBoxDisplayFilter.SetTextImmediately(text);
      }

      private void setRecentFilterText(string text)
      {
         textBoxDisplayFilterRecent.SetTextImmediately(text);
      }

      private void setFilterState(bool state)
      {
         checkBoxDisplayFilter.Checked = Program.Settings.DisplayFilterEnabled;
      }

      private void setRecentFilterState(bool state)
      {
         checkBoxDisplayFilterRecent.Checked = Program.Settings.DisplayFilterRecentEnabled;
      }

      private void readFilterFromConfig()
      {
         setFilterText(Program.Settings.DisplayFilter);
         setFilterState(Program.Settings.DisplayFilterEnabled);
         setRecentFilterText(Program.Settings.DisplayFilterRecent);
         setRecentFilterState(Program.Settings.DisplayFilterRecentEnabled);
      }

      private void onTextBoxDisplayFilterUpdate(EDataCacheType type)
      {
         switch (type)
         {
            case EDataCacheType.Live:
               setFilter(type, textBoxDisplayFilter.Text);
               break;

            case EDataCacheType.Recent:
               setFilter(type, textBoxDisplayFilterRecent.Text);
               break;

            case EDataCacheType.Search:
            default:
               Debug.Assert(false);
               return;
         }

         applyFilterChange(type);
      }

      private void setFilter(EDataCacheType type, string text)
      {
         switch (type)
         {
            case EDataCacheType.Live:
               Program.Settings.DisplayFilter = text;
               break;

            case EDataCacheType.Recent:
               Program.Settings.DisplayFilterRecent = text;
               break;

            case EDataCacheType.Search:
            default:
               Debug.Assert(false);
               return;
         }
      }

      private void onCheckBoxDisplayFilterUpdate(EDataCacheType type, bool enabled)
      {
         switch (type)
         {
            case EDataCacheType.Live:
               Program.Settings.DisplayFilterEnabled = enabled;
               break;

            case EDataCacheType.Recent:
               Program.Settings.DisplayFilterRecentEnabled = enabled;
               break;

            case EDataCacheType.Search:
            default:
               Debug.Assert(false);
               return;
         }

         applyFilterChange(type);
      }

      private void applyFilterChange(EDataCacheType type)
      {
         switch (type)
         {
            case EDataCacheType.Live:
               if (_mergeRequestFilter != null)
               {
                  _mergeRequestFilter.Filter = createMergeRequestFilterState(EDataCacheType.Live);
               }
               break;

            case EDataCacheType.Recent:
               if (_mergeRequestFilterRecent != null)
               {
                  _mergeRequestFilterRecent.Filter = createMergeRequestFilterState(EDataCacheType.Recent);
               }
               break;

            case EDataCacheType.Search:
            default:
               Debug.Assert(false);
               break;
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
            projectName, null, null, currentUser.Username, true, true, Array.Empty<string>());
         return _newMergeRequestDialogStatesByHosts.Data.TryGetValue(hostname, out var value) ? value : factoryProperties;
      }

      private void requestUpdates(EDataCacheType mode, MergeRequestKey? mrk, int[] intervals)
      {
         DataCache dataCache = getDataCache(mode);
         dataCache?.MergeRequestCache?.RequestUpdate(mrk, intervals);
         dataCache?.DiscussionCache?.RequestUpdate(mrk, intervals);
      }

      private void showHintAboutIntegrationWithGitUI()
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
         Trace.TraceInformation("[ConnectionPage] onLongCachesReady()");

         CanEditChanged?.Invoke(this);
         CanMergeChanged?.Invoke(this);
         CanCreateNewChanged?.Invoke(this);
      }

      private CommandState isCommandEnabledInDiscussionsView(MergeRequestKey mrk, ICommand command)
      {
         // In Discussions view we cannot say what DataCache contains MR in advance,
         // because during Discussions view lifetime, MR can be removed from the original DataCache.
         // Find out Data Cache with the latest MRRefreshTime for mrk.

         EDataCacheType bestMode = EDataCacheType.Live;
         DateTime bestRefreshTime = DateTime.MinValue;
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            DataCache dataCache = getDataCache(mode);
            IMergeRequestCache mergeRequestCache = dataCache?.MergeRequestCache;
            if (mergeRequestCache != null)
            {
               DateTime refreshTime = mergeRequestCache.GetMergeRequestRefreshTime(mrk);
               if (refreshTime > bestRefreshTime)
               {
                  bestMode = mode;
                  bestRefreshTime = refreshTime;
               }
            }
         }

         CommandState? commandStateOpt = isCommandEnabledInDiscussionsView(bestMode, mrk, command);
         if (commandStateOpt.HasValue)
         {
            return commandStateOpt.Value;
         }
         return new CommandState(false, false);
      }

      private void reloadByDiscussionsViewRequest(MergeRequestKey mrk)
      {
         // In Discussions view we cannot say what DataCache contains MR in advance,
         // because during Discussions view lifetime, MR can be removed from the original DataCache.
         // ReloadOne() updates all Data Caches.

         ReloadOne(mrk);
      }
   }
}

