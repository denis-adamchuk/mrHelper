using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;
using mrHelper.App.Controls;
using Microsoft.Win32;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      internal MainForm(bool startMinimized, bool runningAsUwp, string startUrl, bool integratedInGitExtensions,
         bool integratedInSourceTree)
      {
         _startMinimized = startMinimized;
         _startUrl = startUrl;
         _integratedInGitExtensions = integratedInGitExtensions;
         _integratedInSourceTree = integratedInSourceTree;

         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this, (float)Constants.FontSizeChoices["Design"]);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);
         CommonControls.Tools.WinFormsHelpers.LogScreenResolution(this);

         _allowAutoStartApplication = runningAsUwp;
         checkBoxRunWhenWindowsStarts.Enabled = !_allowAutoStartApplication;

         _trayIcon = new TrayIcon(notifyIcon);
         _mdPipeline =
            MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());

         columnHeaderName.Width = this.listViewProjects.Width - SystemInformation.VerticalScrollBarWidth - 5;
         linkLabelConnectedTo.Text = String.Empty;
         linkLabelConnectedTo.SetLinkLabelClicked(UrlHelper.OpenBrowser);

         foreach (Control control in CommonControls.Tools.WinFormsHelpers.GetAllSubControls(this))
         {
            if (control.Anchor.HasFlag(AnchorStyles.Right)
               && (control.MinimumSize.Width != 0 || control.MinimumSize.Height != 0))
            {
               Debug.Assert(false);
            }
         }

         buttonTimeTrackingCancel.ConfirmationCondition = () => true;
         buttonTimeTrackingCancel.ConfirmationText = "Tracked time will be lost, are you sure?";

         listViewLiveMergeRequests.Tag = Constants.LiveListViewName;
         listViewFoundMergeRequests.Tag = Constants.SearchListViewName;
         listViewRecentMergeRequests.Tag = Constants.RecentListViewName;

         forEachListView(listView => listView.ContentChanged += listViewMergeRequests_ContentChanged);
         forEachListView(listView => listView.Initialize());

         SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
         SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
         _applicationUpdateChecker = new PeriodicUpdateChecker(this);

         _redrawTimer.Tick += onRedrawTimer;
         _redrawTimer.Start();
      }

      private void initializeWork()
      {
         restoreState();
         prepareFormToStart();

         createLiveDataCacheAndDependencies();
         subscribeToLiveDataCache();

         createSearchDataCache();
         subscribeToSearchDataCache();

         createRecentDataCache();
         subscribeToRecentDataCache();

         Trace.TraceInformation(String.Format(
            "[Mainform] Connecting to URL on startup {0}", _startUrl?.ToString() ?? "null"));
         reconnect(_startUrl);
      }

      private void finalizeWork()
      {
         _requestedDiff.Clear();
         _requestedUrl.Clear();

         Program.Settings.PropertyChanged -= onSettingsPropertyChanged;

         unsubscribeFromRecentDataCache();
         unsubscribeFromSearchDataCache();
         unsubscribeFromLiveDataCache();

         saveState();
         Interprocess.SnapshotSerializer.CleanUpSnapshots();

         Trace.TraceInformation("[MainForm] Work finalized.");
      }

      private void restoreState()
      {
         _persistentStorage = new PersistentStorage();
         _persistentStorage.OnSerialize += onPersistentStorageSerialize;
         _persistentStorage.OnDeserialize += onPersistentStorageDeserialize;
         forEachListView(listView => listView.SetPersistentStorage(_persistentStorage));

         try
         {
            _persistentStorage.Deserialize();
         }
         catch (PersistenceStateDeserializationException ex)
         {
            ExceptionHandlers.Handle("Cannot deserialize the state", ex);
         }
      }

      private void saveState()
      {
         try
         {
            _persistentStorage?.Serialize();
         }
         catch (PersistenceStateSerializationException ex)
         {
            ExceptionHandlers.Handle("Cannot serialize the state", ex);
         }
      }

      private void prepareFormToStart()
      {
         subscribeToApplicationUpdatesAndRequestThem();
         initializeKeywords();
         setControlStateFromConfiguration();
         applyAutostartSetting(Program.Settings.RunWhenWindowsStarts);
         disableSSLVerification();
         updateCaption();
         updateTabControlSelection();
         updateHostsDropdownList();
         fillColorList();
         fillColorSchemeList();
         prepareControlsToStart();
         prepareSizeToStart();
         selectHost(PreferredSelection.Initial);
      }

      private void initializeKeywords()
      {
         string filepath = Path.Combine(
            Directory.GetCurrentDirectory(), Constants.KeywordsFileName);
         if (System.IO.File.Exists(filepath))
         {
            try
            {
               _keywords = JsonUtils.LoadFromFile<string[]>(filepath);
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle("Cannot load keywords from file", ex);
            }
         }
      }

      private void setTooltipsForSearchOptions()
      {
         void extendControlTooltip(Control control, int searchLimit) =>
            toolTip.SetToolTip(control, String.Format(
               "{0} (up to {1} results)", toolTip.GetToolTip(control), searchLimit));

         extendControlTooltip(checkBoxSearchByTitleAndDescription,
            Constants.MaxSearchResults);
         extendControlTooltip(checkBoxSearchByTargetBranch,
            Constants.MaxSearchResults);
         extendControlTooltip(checkBoxSearchByProject,
            Constants.MaxSearchResults);
         extendControlTooltip(checkBoxSearchByAuthor,
            Constants.MaxSearchResults);
      }

      private void startLostConnectionIndicatorTimer()
      {
         if (_lostConnectionInfo.HasValue)
         {
            _lostConnectionInfo.Value.IndicatorTimer.Tick += new EventHandler(onLostConnectionIndicatorTimer);
            _lostConnectionInfo.Value.IndicatorTimer.Start();
         }
      }

      private void stopAndDisposeLostConnectionIndicatorTimer()
      {
         if (_lostConnectionInfo.HasValue)
         {
            _lostConnectionInfo.Value.IndicatorTimer.Stop();
            _lostConnectionInfo.Value.IndicatorTimer.Dispose();
         }
      }

      private void startClipboardCheckTimer()
      {
         _clipboardCheckingTimer.Tick += new EventHandler(onClipboardCheckingTimer);
         _clipboardCheckingTimer.Start();
      }

      private void stopClipboardCheckTimer()
      {
         _clipboardCheckingTimer.Stop();
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

      private void subscribeToNewVersionReminderTimer()
      {
         _newVersionReminderTimer.Tick += (s, e) => remindAboutNewVersion();
      }

      private void startNewVersionReminderTimer()
      {
         _newVersionReminderTimer.Start();
      }

      private void stopNewVersionReminderTimer()
      {
         _newVersionReminderTimer.Stop();
      }

      private void subscribeToApplicationUpdatesAndRequestThem()
      {
         _applicationUpdateChecker.NewVersionAvailable += onNewVersionAvailable;
      }

      private void unsubscribeFromApplicationUpdates()
      {
         _applicationUpdateChecker.NewVersionAvailable -= onNewVersionAvailable;
      }

      private void createListViewContextMenu()
      {
         getListView(EDataCacheType.Live).AssignContextMenu(new MergeRequestListViewContextMenu(
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
         selectColorScheme(); // requires ExpressionResolver

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

      private void onGitStatisticManagerUpdate()
      {
         getListView(EDataCacheType.Live).Invalidate();
      }
   }
}

