using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.App.Helpers.GitLab;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using mrHelper.StorageSupport;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
   {
      public ConnectionPage(
         string hostname,
         PersistentStorage persistentStorage,
         DictionaryWrapper<MergeRequestKey, DateTime> recentMergeRequests,
         DictionaryWrapper<MergeRequestKey, HashSet<string>> reviewedRevisions,
         DictionaryWrapper<string, MergeRequestKey> lastMergeRequestsByHosts,
         DictionaryWrapper<string, NewMergeRequestProperties> newMergeRequestDialogStatesByHosts,
         IEnumerable<string> keywords,
         TrayIcon trayIcon,
         ToolTip toolTip,
         bool integratedInGitExtensions,
         bool integratedInSourceTree)
      {
         HostName = hostname;
         _keywords = keywords;
         _trayIcon = trayIcon;
         _integratedInGitExtensions = integratedInGitExtensions;
         _integratedInSourceTree = integratedInSourceTree;
         _toolTip = toolTip;

         _recentMergeRequests = recentMergeRequests;
         _reviewedRevisions = reviewedRevisions;
         _lastMergeRequestsByHosts = lastMergeRequestsByHosts;
         _newMergeRequestDialogStatesByHosts = newMergeRequestDialogStatesByHosts;

         InitializeComponent();
         updateSplitterOrientation();

         forEachListView(listView => listView.SetPersistentStorage(persistentStorage));

         _redrawTimer.Tick += onRedrawTimer;
         _redrawTimer.Start();

         _mdPipeline = MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());

         Program.Settings.MainWindowLayoutChanged += onMainWindowLayoutChanged;
         Program.Settings.WordWrapLongRowsChanged += onWrapLongRowsChanged;
      }

      private void initializeWork()
      {
         textBoxDisplayFilter.Text = Program.Settings.DisplayFilter;
         checkBoxDisplayFilter.Checked = Program.Settings.DisplayFilterEnabled;

         preparePageToStart();

         createLiveDataCacheAndDependencies();
         subscribeToLiveDataCache();

         createSearchDataCache();
         subscribeToSearchDataCache();

         createRecentDataCache();
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
         preparePageControlsToStart();
         createMessageFilterFromSettings();
      }

      private void preparePageControlsToStart()
      {
         disableLiveTabControls();
         disableSearchTabControls();
         disableRecentTabControls();
         disableSelectedMergeRequestControls();
         setConnectionStatus(null);

         listViewLiveMergeRequests.Tag = Constants.LiveListViewName;
         listViewFoundMergeRequests.Tag = Constants.SearchListViewName;
         listViewRecentMergeRequests.Tag = Constants.RecentListViewName;

         createListViewContextMenu();
         createRevisionBrowserContextMenu();

         forEachListView(listView => listView.SetCurrentUserGetter(() => CurrentUser));
         forEachListView(listView => listView.ContentChanged += listViewMergeRequests_ContentChanged);

         linkLabelConnectedTo.Text = String.Empty;
         linkLabelConnectedTo.SetLinkLabelClicked(UrlHelper.OpenBrowser);

         setFontSizeInMergeRequestDescriptionBox();
      }

      private void setFontSizeInMergeRequestDescriptionBox()
      {
         string cssEx = String.Format("body div {{ font-size: {0}px; }}",
            CommonControls.Tools.WinFormsHelpers.GetFontSizeInPixels(richTextBoxMergeRequestDescription));
         richTextBoxMergeRequestDescription.BaseStylesheet =
            String.Format("{0}{1}", mrHelper.App.Properties.Resources.Common_CSS, cssEx);
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

      private void createMessageFilterFromSettings()
      {
         _mergeRequestFilter = new MergeRequestFilter(createMergeRequestFilterState());
         _mergeRequestFilter.FilterChanged += () => updateMergeRequestList(EDataCacheType.Live);
      }

      private MergeRequestFilterState createMergeRequestFilterState()
      {
         return new MergeRequestFilterState
         (
            ConfigurationHelper.GetDisplayFilterKeywords(Program.Settings),
            Program.Settings.DisplayFilterEnabled
         );
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
            unMuteSelectedMergeRequest,
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
               showDiscussionsForSelectedMergeRequest));
         }
      }

      private void createRevisionBrowserContextMenu()
      {
         Action defaultAction = () => launchDiffTool(DiffToolMode.DiffSelectedToBase);
         revisionBrowser.AssignContextMenu(new RevisionBrowserContextMenu(
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
         // The idea is that:
         // 1. Already cached MR that became closed remotely will not be removed from the cache
         // 2. Open MR that are missing in the cache, will be added to the cache
         // 3. Open MR that exist in the cache, will be updated
         // 4. Non-cached MR that are closed remotely, will not be added to the cache even if directly requested by IId
         bool updateOnlyOpened = true;

         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilter, _keywords,
            Program.Settings.UpdateManagerExtendedLogging, "Live",
            new DataCacheCallbacks(onForbiddenProject, onNotFoundProject),
            new DataCacheUpdateRules(Program.Settings.AutoUpdatePeriodMs, Program.Settings.AutoUpdatePeriodMs,
            updateOnlyOpened), true, true);
         _liveDataCache = new DataCache(dataCacheContext);
         getListView(EDataCacheType.Live).SetDataCache(_liveDataCache);
         getListView(EDataCacheType.Live).SetFilter(_mergeRequestFilter);

         DataCache dataCache = getDataCache(EDataCacheType.Live);
         _expressionResolver = new ExpressionResolver(dataCache);
         forEachListView(listView => listView.SetExpressionResolver(_expressionResolver));

         _eventFilter = new EventFilter(Program.Settings, dataCache, _mergeRequestFilter);
         _userNotifier = new UserNotifier(dataCache, _eventFilter, _trayIcon);
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
         _userNotifier?.Dispose();
         _userNotifier = null;
         _eventFilter?.Dispose();
         _eventFilter = null;
         _expressionResolver?.Dispose();
         _expressionResolver = null;
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
            case EDataCacheType.Recent:
               return new DataCacheUpdateRules(Program.Settings.AutoUpdatePeriodMs,
                                               Program.Settings.AutoUpdatePeriodMs,
                                               false);

            case EDataCacheType.Search:
               return new DataCacheUpdateRules(Program.Settings.AutoUpdatePeriodMs, null, false);

            default:
               Debug.Assert(false);
               break;
         }
         return null;
      }

      private void createSearchDataCache()
      {
         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilter, _keywords,
            Program.Settings.UpdateManagerExtendedLogging, "Search", new DataCacheCallbacks(null, null),
            getDataCacheUpdateRules(EDataCacheType.Search), false, false);
         _searchDataCache = new DataCache(dataCacheContext);
         getListView(EDataCacheType.Search).SetDataCache(_searchDataCache);
      }

      private void subscribeToSearchDataCacheInternalEvents()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Search);
         if (dataCache?.TotalTimeCache != null)
         {
            dataCache.TotalTimeCache.TotalTimeLoading += onPreLoadTrackedTime;
            dataCache.TotalTimeCache.TotalTimeLoaded += onPostLoadTrackedTime;
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
         if (dataCache?.TotalTimeCache != null)
         {
            dataCache.TotalTimeCache.TotalTimeLoading -= onPreLoadTrackedTime;
            dataCache.TotalTimeCache.TotalTimeLoaded -= onPostLoadTrackedTime;
         }
      }

      private void createRecentDataCache()
      {
         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilter, _keywords,
            Program.Settings.UpdateManagerExtendedLogging, "Recent", new DataCacheCallbacks(null, null),
            getDataCacheUpdateRules(EDataCacheType.Recent), false, false);
         _recentDataCache = new DataCache(dataCacheContext);
         getListView(EDataCacheType.Recent).SetDataCache(_recentDataCache);
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
      }

      private void subscribeToRecentDataCache()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Recent);
         dataCache.Disconnected += onRecentDataCacheDisconnected;
         dataCache.Connecting += onRecentDataCacheConnecting;
         dataCache.Connected += onRecentDataCacheConnected;
      }

      private void unsubscribeFromRecentDataCache()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Recent);
         dataCache.Disconnected -= onRecentDataCacheDisconnected;
         dataCache.Connecting -= onRecentDataCacheConnecting;
         dataCache.Connected -= onRecentDataCacheConnected;
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
         }
      }
   }
}
