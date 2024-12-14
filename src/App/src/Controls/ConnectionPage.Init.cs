using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using mrHelper.StorageSupport;
using mrHelper.CustomActions;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
   {
      public ConnectionPage(
         string hostname,
         DictionaryWrapper<MergeRequestKey, DateTime> recentMergeRequests,
         DictionaryWrapper<MergeRequestKey, HashSet<string>> reviewedRevisions,
         DictionaryWrapper<string, MergeRequestKey> lastMergeRequestsByHosts,
         DictionaryWrapper<string, NewMergeRequestProperties> newMergeRequestDialogStatesByHosts,
         HashSetWrapper<ProjectKey> collapsedProjectsLive,
         HashSetWrapper<ProjectKey> collapsedProjectsRecent,
         HashSetWrapper<ProjectKey> collapsedProjectsSearch,
         DictionaryWrapper<MergeRequestKey, DateTime> mutedMergeRequests,
         DictionaryWrapper<string, MergeRequestFilterState> filtersByHostsLive,
         DictionaryWrapper<string, MergeRequestFilterState> filtersByHostsRecent,
         IEnumerable<string> keywords,
         TrayIcon trayIcon,
         ThemedToolTip toolTip,
         bool integratedInGitExtensions,
         bool integratedInSourceTree,
         UserDefinedSettings.OldFilterSettings oldFilter,
         ITimeTrackerHolder timeTrackerHolder,
         Action<string> onOpenUrl,
         Func<ICommand, MergeRequestKey, ConnectionPage, System.Threading.Tasks.Task> onCommand)
      {
         HostName = hostname;
         _keywords = keywords;
         _trayIcon = trayIcon;
         _integratedInGitExtensions = integratedInGitExtensions;
         _integratedInSourceTree = integratedInSourceTree;
         _toolTip = toolTip;
         _timeTrackerHolder = timeTrackerHolder;
         _recentMergeRequests = recentMergeRequests;
         _reviewedRevisions = reviewedRevisions;
         _lastMergeRequestsByHosts = lastMergeRequestsByHosts;
         _newMergeRequestDialogStatesByHosts = newMergeRequestDialogStatesByHosts;
         _filtersByHostsLive = filtersByHostsLive;
         _filtersByHostsRecent = filtersByHostsRecent;
         _onOpenUrl = onOpenUrl;
         _onCommand = onCommand;

         InitializeComponent();
         updateSplitterOrientation();

         listViewLiveMergeRequests.SetIdentity(Constants.LiveListViewName);
         listViewLiveMergeRequests.SetCollapsedProjects(collapsedProjectsLive);
         listViewLiveMergeRequests.SetMutedMergeRequests(mutedMergeRequests);
         listViewLiveMergeRequests.SetOpenMergeRequestUrlCallback(openBrowserForMergeRequest);
         listViewLiveMergeRequests.SetTimeTrackingCheckingCallback(isTrackingTime);
         listViewLiveMergeRequests.SetPinChecker(isMergeRequestPinned);
         listViewLiveMergeRequests.SetPinText("Pin", "Unpin");
         listViewLiveMergeRequests.Initialize(hostname, doesSupportPin: true);

         listViewFoundMergeRequests.SetIdentity(Constants.SearchListViewName);
         listViewFoundMergeRequests.SetCollapsedProjects(collapsedProjectsSearch);
         listViewFoundMergeRequests.SetTimeTrackingCheckingCallback(isTrackingTime);
         listViewFoundMergeRequests.SetPinChecker(isMergeRequestPinned);
         listViewFoundMergeRequests.SetPinText("Pin to Live tab", "Unpin from Live tab");
         listViewFoundMergeRequests.Initialize(hostname, doesSupportPin: false);

         listViewRecentMergeRequests.SetIdentity(Constants.RecentListViewName);
         listViewRecentMergeRequests.SetCollapsedProjects(collapsedProjectsRecent);
         listViewRecentMergeRequests.SetTimeTrackingCheckingCallback(isTrackingTime);
         listViewRecentMergeRequests.SetPinChecker(isMergeRequestPinned);
         listViewRecentMergeRequests.SetPinText("Pin to Live tab", "Unpin from Live tab");
         listViewRecentMergeRequests.Initialize(hostname, doesSupportPin: false);

         _mdPipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());

         Program.Settings.MainWindowLayoutChanged += onMainWindowLayoutChanged;
         Program.Settings.WordWrapLongRowsChanged += onWrapLongRowsChanged;
         Program.Settings.ShowHiddenMergeRequestIdsChanged += onShowHiddenMergeRequestIdsChanged;

         ColorScheme.Modified += onColorSchemeModified;

         moveFilterFromConfigToStorage(oldFilter);
      }

      private void initializeWork()
      {
         preparePageToStart();

         createLiveDataCacheAndDependencies();
         subscribeToLiveDataCache();

         createSearchDataCacheAndDependencies();
         subscribeToSearchDataCache();

         createRecentDataCacheAndDependencies();
         subscribeToRecentDataCache();
      }

      private void finalizeWork()
      {
         unsubscribeFromRecentDataCache();
         unsubscribeFromSearchDataCache();
         unsubscribeFromLiveDataCache();
      }

      private void preparePageToStart()
      {
         createMessageFiltersFromSettings();
         preparePageControlsToStart();
      }

      private void preparePageControlsToStart()
      {
         disableLiveTabControls();
         disableSearchTabControls();
         disableRecentTabControls();
         disableSelectedMergeRequestControls();
         setConnectionStatus(null);

         createListViewContextMenu();
         createRevisionBrowserContextMenu();

         forEachListView(listView => listView.SetCurrentUserGetter(() => CurrentUser));
         forEachListView(listView => listView.ContentChanged += listViewMergeRequests_ContentChanged);

         linkLabelConnectedTo.SetLinkLabelClicked(openBrowserForSelectedMergeRequest);
         linkLabelEnvironment.SetLinkLabelClicked(openBrowserForSelectedMergeRequest);

         descriptionSplitContainerSite.Initialize(_keywords, _mdPipeline);
         descriptionSplitContainerSite.SplitContainer.SplitterMoving +=
            new System.Windows.Forms.SplitterCancelEventHandler(this.splitContainer_SplitterMoving);
         descriptionSplitContainerSite.SplitContainer.SplitterMoved +=
            new System.Windows.Forms.SplitterEventHandler(this.splitContainer_SplitterMoved);

         revisionSplitContainerSite.Initialize(
            pk => getCommitStorage(pk, false), getRepositoryAccessor, getReviewedRevisions);
         revisionSplitContainerSite.SplitContainer.SplitterMoving +=
            new System.Windows.Forms.SplitterCancelEventHandler(this.splitContainer_SplitterMoving);
         revisionSplitContainerSite.SplitContainer.SplitterMoved +=
            new System.Windows.Forms.SplitterEventHandler(this.splitContainer_SplitterMoved);
         revisionSplitContainerSite.RevisionBrowser.SelectionChanged +=
            new System.EventHandler(this.revisionBrowser_SelectionChanged);

         prepareFilterControls();
      }

      private void startRedrawTimer()
      {
         _redrawTimer.Tick += onRedrawTimer;
         _redrawTimer.Start();
      }

      private void stopRedrawTimer()
      {
         _redrawTimer.Tick -= onRedrawTimer;
         _redrawTimer.Stop();
      }

      private void createMessageFiltersFromSettings()
      {
         _mergeRequestFilter = new MergeRequestFilter(getOrCreateMergeRequestFilterState(EDataCacheType.Live));
         _mergeRequestFilter.FilterChanged += () => updateMergeRequestList(EDataCacheType.Live);

         _mergeRequestFilterRecent = new MergeRequestFilter(getOrCreateMergeRequestFilterState(EDataCacheType.Recent));
         _mergeRequestFilterRecent.FilterChanged += () => updateMergeRequestList(EDataCacheType.Recent);
      }

      private MergeRequestFilterState getOrCreateMergeRequestFilterState(EDataCacheType type)
      {
         DictionaryWrapper<string, MergeRequestFilterState> filtersByHosts;
         switch (type)
         {
            case EDataCacheType.Live:
               filtersByHosts = _filtersByHostsLive;
               break;

            case EDataCacheType.Recent:
               filtersByHosts = _filtersByHostsRecent;
               break;

            case EDataCacheType.Search:
            default:
               Debug.Assert(false);
               return default(MergeRequestFilterState);
         }

         if (!filtersByHosts.Data.ContainsKey(HostName))
         {
            filtersByHosts.Add(HostName, new MergeRequestFilterState(String.Empty, FilterState.Disabled));
         }
         return filtersByHosts[HostName];
      }

      private void createListViewContextMenu()
      {
         getListView(EDataCacheType.Live).AssignContextMenu(new MergeRequestListViewContextMenu(
            this,
            showDiscussionsForSelectedMergeRequest,
            () => reloadMergeRequestsByUserRequest(getDataCache(EDataCacheType.Live)),
            refreshSelectedMergeRequest,
            editSelectedMergeRequest,
            acceptSelectedMergeRequest,
            closeSelectedMergeRequest,
            launchDiffWithBaseForSelectedMergeRequest,
            launchDiffToolForSelectedMergeRequest,
            muteSelectedMergeRequestUntilTomorrow,
            muteSelectedMergeRequestUntilMonday,
            muteSelectedMergeRequest,
            unMuteSelectedMergeRequest,
            toggleSelectedMergeRequestExclusion,
            openSelectedAuthorProfile,
            toggleSelectedMergeRequestPinState,
            showDiscussionsForSelectedMergeRequest));

         foreach (EDataCacheType mode in new EDataCacheType[] { EDataCacheType.Recent, EDataCacheType.Search })
         {
            getListView(mode).AssignContextMenu(new MergeRequestListViewContextMenu(
               this,
               showDiscussionsForSelectedMergeRequest,
               null,
               mode == EDataCacheType.Search ? (null as Action) : refreshSelectedMergeRequest,
               null,
               null,
               null,
               launchDiffWithBaseForSelectedMergeRequest,
               launchDiffToolForSelectedMergeRequest,
               null,
               null,
               null,
               null,
               mode == EDataCacheType.Search ? (null as Action) : toggleSelectedMergeRequestExclusion,
               openSelectedAuthorProfile,
               toggleSelectedMergeRequestPinState,
               showDiscussionsForSelectedMergeRequest));
         }
      }

      private void createRevisionBrowserContextMenu()
      {
         void defaultAction() => launchDiffTool(DiffToolMode.DiffSelectedToBase);
         getRevisionBrowser().AssignContextMenu(new RevisionBrowserContextMenu(
            this,
            () => launchDiffTool(DiffToolMode.DiffBetweenSelected),
            defaultAction,
            () => launchDiffTool(DiffToolMode.DiffSelectedToParent),
            () => launchDiffTool(DiffToolMode.DiffLatestToBase),
            defaultAction));
      }

      private void startEventPendingTimer(Func<bool> onCheck, int checkInterval, Action onEvent)
      {
         Timer cacheCheckTimer = new Timer
         {
            Interval = checkInterval
         };
         cacheCheckTimer.Tick +=
            (s, e) =>
            {
               void stopTimer()
               {
                  cacheCheckTimer.Stop();
                  cacheCheckTimer.Dispose();
               }

               if (IsDisposed)
               {
                  stopTimer();
               }
               else if (onCheck())
               {
                  stopTimer();
                  onEvent?.Invoke();
               }
            };
         cacheCheckTimer.Start();
      }

      private void createLiveDataCacheAndDependencies()
      {
         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilter, _keywords,
            Program.Settings.UpdateManagerExtendedLogging, "Live",
            new DataCacheCallbacks(onForbiddenProject, onNotFoundProject, isEnvironmentStatusSupported),
            getDataCacheUpdateRules(EDataCacheType.Live), true, true);
         _liveDataCache = new DataCache(dataCacheContext);
         getListView(EDataCacheType.Live).SetDataCache(_liveDataCache);
         getListView(EDataCacheType.Live).SetFilter(_mergeRequestFilter);

         DataCache dataCache = getDataCache(EDataCacheType.Live);
         _expressionResolver = new ExpressionResolver(dataCache);
         forEachListView(listView => listView.SetExpressionResolver(_expressionResolver));

         _eventFilter = new EventFilter(Program.Settings, dataCache, _mergeRequestFilter);
         _userNotifier = new UserNotifier(dataCache, _eventFilter, _trayIcon);

         _avatarImageCache[EDataCacheType.Live] = new AvatarImageCache(dataCache);
         getListView(EDataCacheType.Live).SetAvatarImageCache(_avatarImageCache[EDataCacheType.Live]);
      }

      private void subscribeToLiveDataCache()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         dataCache.Disconnected += onLiveDataCacheDisconnected;
         dataCache.Connecting += onLiveDataCacheConnecting;
         dataCache.Connected += onLiveDataCacheConnected;
      }

      private void disposeLiveDataCacheDependencies()
      {
         _avatarImageCache[EDataCacheType.Live]?.Dispose();
         _avatarImageCache.Remove(EDataCacheType.Live);
         _userNotifier?.Dispose();
         _userNotifier = null;
         _eventFilter?.Dispose();
         _eventFilter = null;
         _expressionResolver?.Dispose();
         _expressionResolver = null;
      }

      private void disposeSearchDataCacheDependencies()
      {
         _avatarImageCache[EDataCacheType.Search]?.Dispose();
         _avatarImageCache.Remove(EDataCacheType.Search);
      }

      private void disposeRecentDataCacheDependencies()
      {
         _avatarImageCache[EDataCacheType.Recent]?.Dispose();
         _avatarImageCache.Remove(EDataCacheType.Recent);
      }

      private void subscribeToLiveDataCacheInternalEvents()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         if (dataCache?.MergeRequestCache != null)
         {
            dataCache.MergeRequestCache.MergeRequestEvent += onLiveMergeRequestEvent;
            dataCache.MergeRequestCache.MergeRequestListRefreshed += onLiveMergeRequestListRefreshed;
            dataCache.MergeRequestCache.MergeRequestRefreshed += onMergeRequestRefreshed;
         }

         if (dataCache?.TotalTimeCache != null)
         {
            dataCache.TotalTimeCache.TotalTimeLoading += onPreLoadTrackedTime;
            dataCache.TotalTimeCache.TotalTimeLoaded += onPostLoadTrackedTime;
         }

         if (dataCache?.DiscussionCache != null)
         {
            dataCache.DiscussionCache.DiscussionsLoading += onPreLoadDiscussions;
            dataCache.DiscussionCache.DiscussionsLoaded += onPostLoadDiscussions;
         }
      }

      private void unsubscribeFromLiveDataCache()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         dataCache.Disconnected -= onLiveDataCacheDisconnected;
         dataCache.Connecting -= onLiveDataCacheConnecting;
         dataCache.Connected -= onLiveDataCacheConnected;
      }

      private void unsubscribeFromLiveDataCacheInternalEvents()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         if (dataCache?.MergeRequestCache != null)
         {
            dataCache.MergeRequestCache.MergeRequestEvent -= onLiveMergeRequestEvent;
            dataCache.MergeRequestCache.MergeRequestListRefreshed -= onLiveMergeRequestListRefreshed;
            dataCache.MergeRequestCache.MergeRequestRefreshed -= onMergeRequestRefreshed;
         }

         if (dataCache?.TotalTimeCache != null)
         {
            dataCache.TotalTimeCache.TotalTimeLoading -= onPreLoadTrackedTime;
            dataCache.TotalTimeCache.TotalTimeLoaded -= onPostLoadTrackedTime;
         }

         if (dataCache?.DiscussionCache != null)
         {
            dataCache.DiscussionCache.DiscussionsLoading -= onPreLoadDiscussions;
            dataCache.DiscussionCache.DiscussionsLoaded -= onPostLoadDiscussions;
         }
      }

      private DataCacheUpdateRules getDataCacheUpdateRules(EDataCacheType mode)
      {
         switch (mode)
         {
            case EDataCacheType.Live:
            case EDataCacheType.Recent:
               return new DataCacheUpdateRules(Program.Settings.AutoUpdatePeriodMs,
                                               Program.Settings.AutoUpdatePeriodMs);

            case EDataCacheType.Search:
               return new DataCacheUpdateRules(Program.Settings.AutoUpdatePeriodMs,
                                               int.MaxValue);

            default:
               Debug.Assert(false);
               break;
         }
         return null;
      }

      private void createSearchDataCacheAndDependencies()
      {
         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilter, _keywords,
            Program.Settings.UpdateManagerExtendedLogging, "Search",
            new DataCacheCallbacks(null, null, isEnvironmentStatusSupported),
            getDataCacheUpdateRules(EDataCacheType.Search), false, false);
         _searchDataCache = new DataCache(dataCacheContext);
         getListView(EDataCacheType.Search).SetDataCache(_searchDataCache);

         _avatarImageCache[EDataCacheType.Search] = new AvatarImageCache(_searchDataCache);
         getListView(EDataCacheType.Search).SetAvatarImageCache(_avatarImageCache[EDataCacheType.Search]);
      }

      private void subscribeToSearchDataCacheInternalEvents()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Search);
         if (dataCache?.MergeRequestCache != null)
         {
            dataCache.MergeRequestCache.MergeRequestEvent += onSearchMergeRequestEvent;
         }

         if (dataCache?.TotalTimeCache != null)
         {
            dataCache.TotalTimeCache.TotalTimeLoading += onPreLoadTrackedTime;
            dataCache.TotalTimeCache.TotalTimeLoaded += onPostLoadTrackedTime;
         }

         if (dataCache?.DiscussionCache != null)
         {
            dataCache.DiscussionCache.DiscussionsLoading += onPreLoadDiscussions;
            dataCache.DiscussionCache.DiscussionsLoaded += onPostLoadDiscussions;
         }
      }

      private void subscribeToSearchDataCache()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Search);
         dataCache.Disconnected += onSearchDataCacheDisconnected;
         dataCache.Connecting += onSearchDataCacheConnecting;
         dataCache.Connected += onSearchDataCacheConnected;
      }

      private void unsubscribeFromSearchDataCache()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Search);
         dataCache.Disconnected -= onSearchDataCacheDisconnected;
         dataCache.Connecting -= onSearchDataCacheConnecting;
         dataCache.Connected -= onSearchDataCacheConnected;
      }

      private void unsubscribeFromSearchDataCacheInternalEvents()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Search);
         if (dataCache?.MergeRequestCache != null)
         {
            dataCache.MergeRequestCache.MergeRequestEvent -= onSearchMergeRequestEvent;
         }

         if (dataCache?.TotalTimeCache != null)
         {
            dataCache.TotalTimeCache.TotalTimeLoading -= onPreLoadTrackedTime;
            dataCache.TotalTimeCache.TotalTimeLoaded -= onPostLoadTrackedTime;
         }

         if (dataCache?.DiscussionCache != null)
         {
            dataCache.DiscussionCache.DiscussionsLoading -= onPreLoadDiscussions;
            dataCache.DiscussionCache.DiscussionsLoaded -= onPostLoadDiscussions;
         }
      }

      private void createRecentDataCacheAndDependencies()
      {
         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilterRecent, _keywords,
            Program.Settings.UpdateManagerExtendedLogging, "Recent",
            new DataCacheCallbacks(null, null, isEnvironmentStatusSupported),
            getDataCacheUpdateRules(EDataCacheType.Recent), false, false);
         _recentDataCache = new DataCache(dataCacheContext);
         getListView(EDataCacheType.Recent).SetDataCache(_recentDataCache);
         getListView(EDataCacheType.Recent).SetFilter(_mergeRequestFilterRecent);

         _avatarImageCache[EDataCacheType.Recent] = new AvatarImageCache(_recentDataCache);
         getListView(EDataCacheType.Recent).SetAvatarImageCache(_avatarImageCache[EDataCacheType.Recent]);
      }

      private void subscribeToRecentDataCacheInternalEvents()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Recent);
         if (dataCache?.MergeRequestCache != null)
         {
            dataCache.MergeRequestCache.MergeRequestEvent += onRecentMergeRequestEvent;
         }

         if (dataCache?.TotalTimeCache != null)
         {
            dataCache.TotalTimeCache.TotalTimeLoading += onPreLoadTrackedTime;
            dataCache.TotalTimeCache.TotalTimeLoaded += onPostLoadTrackedTime;
         }

         if (dataCache?.DiscussionCache != null)
         {
            dataCache.DiscussionCache.DiscussionsLoading += onPreLoadDiscussions;
            dataCache.DiscussionCache.DiscussionsLoaded += onPostLoadDiscussions;
         }
      }

      private void subscribeToRecentDataCache()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Recent);
         if (dataCache != null)
         {
            dataCache.Disconnected += onRecentDataCacheDisconnected;
            dataCache.Connecting += onRecentDataCacheConnecting;
            dataCache.Connected += onRecentDataCacheConnected;
         }
      }

      private void unsubscribeFromRecentDataCache()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Recent);
         if (dataCache != null)
         {
            dataCache.Disconnected -= onRecentDataCacheDisconnected;
            dataCache.Connecting -= onRecentDataCacheConnecting;
            dataCache.Connected -= onRecentDataCacheConnected;
         }
      }

      private void unsubscribeFromRecentDataCacheInternalEvents()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Recent);
         if (dataCache?.MergeRequestCache != null)
         {
            dataCache.MergeRequestCache.MergeRequestEvent -= onRecentMergeRequestEvent;
         }

         if (dataCache?.TotalTimeCache != null)
         {
            dataCache.TotalTimeCache.TotalTimeLoading -= onPreLoadTrackedTime;
            dataCache.TotalTimeCache.TotalTimeLoaded -= onPostLoadTrackedTime;
         }

         if (dataCache?.DiscussionCache != null)
         {
            dataCache.DiscussionCache.DiscussionsLoading -= onPreLoadDiscussions;
            dataCache.DiscussionCache.DiscussionsLoaded -= onPostLoadDiscussions;
         }
      }

      private void createGitHelpers(DataCache dataCache, ILocalCommitStorageFactory factory)
      {
         if (dataCache.MergeRequestCache == null || dataCache.DiscussionCache == null)
         {
            return;
         }

         LocalCommitStorageType storageType = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
         bool isGitStorageUsed = storageType == LocalCommitStorageType.FullGitRepository
                              || storageType == LocalCommitStorageType.ShallowGitRepository;

         _gitDataUpdater = Program.Settings.CacheRevisionsPeriodMs > 0
            ? new GitDataUpdater(
               dataCache.MergeRequestCache, dataCache.DiscussionCache, this, factory,
               Program.Settings.CacheRevisionsPeriodMs, _mergeRequestFilter, isGitStorageUsed)
            : null;

         if (Program.Settings.UseGitBasedSizeCollection)
         {
            _diffStatProvider = new GitBasedDiffStatProvider(
               dataCache.MergeRequestCache, dataCache.DiscussionCache, this, factory);
         }
         else
         {
            _diffStatProvider = new DiscussionBasedDiffStatProvider(dataCache.DiscussionCache);
         }
         _diffStatProvider.Update += onGitStatisticManagerUpdate;
         getListView(EDataCacheType.Live).SetDiffStatisticProvider(_diffStatProvider);
      }

      private void disposeGitHelpers()
      {
         _gitDataUpdater?.Dispose();
         _gitDataUpdater = null;

         if (_diffStatProvider != null)
         {
            _diffStatProvider.Update -= onGitStatisticManagerUpdate;
            if (_diffStatProvider is IDisposable disposableDiffStatProvider)
            {
               disposableDiffStatProvider.Dispose();
            }
            _diffStatProvider = null;
         }
      }

      private void initializeGitLabInstance()
      {
         if (_gitLabInstance == null)
         {
            Trace.TraceInformation("[ConnectionPage] Initializing GitLabInstance for {0}", HostName);
            _gitLabInstance = new GitLabInstance(HostName, Program.Settings, this);
            _gitLabInstance.ConnectionLost += onConnectionLost;
            _gitLabInstance.ConnectionRestored += onConnectionRestored;
            _shortcuts = new Shortcuts(_gitLabInstance);
         }
      }

      private void disposeGitLabInstance()
      {
         if (_gitLabInstance != null)
         {
            _gitLabInstance.ConnectionLost -= onConnectionLost;
            _gitLabInstance.ConnectionRestored -= onConnectionRestored;
            _gitLabInstance.Dispose();
            _gitLabInstance = null;
            _shortcuts = null;
         }
      }
   }
}
