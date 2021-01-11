using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
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

         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this, (float)Constants.FontSizeChoices["Design"], 96);
         InitializeComponent();
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         _allowAutoStartApplication = runningAsUwp;
         checkBoxRunWhenWindowsStarts.Enabled = !_allowAutoStartApplication;

         _trayIcon = new TrayIcon(notifyIcon);
         _mdPipeline =
            MarkDownUtils.CreatePipeline(Program.ServiceManager.GetJiraServiceUrl());

         this.columnHeaderName.Width = this.listViewProjects.Width - SystemInformation.VerticalScrollBarWidth - 5;
         this.linkLabelConnectedTo.Text = String.Empty;

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

         forEachListView(listView => listView.CollapsingToggled += listViewMergeRequests_CollapsingToggled);
         forEachListView(listView => listView.Initialize());

         SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
         _applicationUpdateChecker = new PeriodicUpdateChecker(this);

         _connectionChecker = new GitLabClient.ConnectionChecker(Program.Settings, this);
         _connectionChecker.ConnectionLost += connectionChecker_ConnectionLost;
         _connectionChecker.ConnectionRestored += connectionChecker_ConnectionRestored;
      }

      private void addCustomActions()
      {
         CustomCommandLoader loader = new CustomCommandLoader(this);
         try
         {
            _customCommands = loader.LoadCommands(Constants.CustomActionsFileName);
         }
         catch (CustomCommandLoaderException ex)
         {
            // If file doesn't exist the loader throws, leaving the app in an undesirable state.
            // Do not try to load custom actions if they don't exist.
            ExceptionHandlers.Handle("Cannot load custom actions", ex);
         }

         _keywords = _customCommands?
            .Where(x => x is SendNoteCommand)
            .Select(x => (x as SendNoteCommand).GetBody()) ?? null;

         if (_customCommands == null)
         {
            return;
         }

         int id = 0;
         foreach (ICommand command in _customCommands)
         {
            string name = command.GetName();
            var button = new System.Windows.Forms.Button
            {
               Name = "customAction" + id,
               Location = new System.Drawing.Point { X = 0, Y = 19 },
               Size = new System.Drawing.Size { Width = 72, Height = 32 },
               MinimumSize = new System.Drawing.Size { Width = 72, Height = 0 },
               Text = name,
               UseVisualStyleBackColor = true,
               TabStop = false,
               Tag = command.GetDependency()
            };
            toolTip.SetToolTip(button, command.GetHint());
            button.Click += async (x, y) =>
            {
               MergeRequestKey? mergeRequestKey = getMergeRequestKey(null);
               if (!mergeRequestKey.HasValue)
               {
                  return;
               }

               ITotalTimeCache totalTimeCache = getDataCache(getCurrentTabDataCacheType())?.TotalTimeCache;

               addOperationRecord(String.Format("Command {0} execution has started", name));
               try
               {
                  await command.Run();
               }
               catch (Exception ex) // Whatever happened in Run()
               {
                  string errorMessage = "Custom action failed";
                  ExceptionHandlers.Handle(errorMessage, ex);
                  MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  addOperationRecord(String.Format("Command {0} failed", name));
                  return;
               }

               string statusMessage = String.Format(
                  "Command {0} execution has completed for merge request !{1} in project {2}",
                  name, mergeRequestKey.Value.IId, mergeRequestKey.Value.ProjectKey.ProjectName);
               addOperationRecord(statusMessage);

               if (command.GetStopTimer())
               {
                  await stopTimeTrackingTimerAsync(totalTimeCache);
               }

               bool reload = command.GetReload();
               if (reload)
               {
                  requestUpdates(EDataCacheType.Live, mergeRequestKey, new int[] {
                     Program.Settings.OneShotUpdateFirstChanceDelayMs,
                     Program.Settings.OneShotUpdateSecondChanceDelayMs });
               }

               ensureMergeRequestInRecentDataCache(mergeRequestKey.Value);
            };
            groupBoxActions.Controls.Add(button);
            id++;
         }
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

         initializeColorScheme();
         initializeIconScheme();
         initializeBadgeScheme();

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
         addCustomActions();
         setControlStateFromConfiguration();
         applyAutostartSetting(Program.Settings.RunWhenWindowsStarts);
         resetMergeRequestTabMinimumSizes();
         disableSSLVerification();
         updateCaption();
         updateTabControlSelection();
         updateHostsDropdownList();
         fillColorSchemesList();
         prepareControlsToStart();
         prepareSizeToStart();
         selectHost(PreferredSelection.Initial);
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

      private void startClipboardCheckTimer()
      {
         _clipboardCheckingTimer.Tick += new EventHandler(onClipboardCheckingTimer);
         _clipboardCheckingTimer.Start();
      }

      private void stopClipboardCheckTimer()
      {
         _clipboardCheckingTimer.Stop();
      }

      private void startListViewRefreshTimer()
      {
         _listViewRefreshTimer.Tick += (s, e) => getListView(EDataCacheType.Live).Invalidate();
         _listViewRefreshTimer.Start();
      }

      private void stopListViewRefreshTimer()
      {
         _listViewRefreshTimer.Stop();
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
            refreshSelectedMergeRequest,
            editSelectedMergeRequest,
            acceptSelectedMergeRequest,
            closeSelectedMergeRequest,
            showDiscussionsForSelectedMergeRequest));

         foreach (EDataCacheType mode in new EDataCacheType[] { EDataCacheType.Recent, EDataCacheType.Search })
         {
            getListView(mode).AssignContextMenu(new MergeRequestListViewContextMenu(
               showDiscussionsForSelectedMergeRequest,
               mode == EDataCacheType.Search ? (null as Action) : refreshSelectedMergeRequest,
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
         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilter, _keywords,
            Program.Settings.UpdateManagerExtendedLogging, "Live");
         _liveDataCache = new DataCache(dataCacheContext, _modificationNotifier, _connectionChecker);
         getListView(EDataCacheType.Live).SetDataCache(_liveDataCache);
         getListView(EDataCacheType.Live).SetFilter(_mergeRequestFilter);

         DataCache dataCache = getDataCache(EDataCacheType.Live);
         _expressionResolver = new ExpressionResolver(dataCache);
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
         _eventFilter?.Dispose();
         _expressionResolver?.Dispose();
      }

      private void subscribeToLiveDataCacheInternalEvents()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         if (dataCache?.MergeRequestCache != null)
         {
            dataCache.MergeRequestCache.MergeRequestEvent += onMergeRequestEvent;
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
            dataCache.MergeRequestCache.MergeRequestEvent -= onMergeRequestEvent;
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

      private void createSearchDataCache()
      {
         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilter, _keywords,
            Program.Settings.UpdateManagerExtendedLogging, "Search");
         _searchDataCache = new DataCache(dataCacheContext, _modificationNotifier, _connectionChecker);
         getListView(EDataCacheType.Search).SetDataCache(_searchDataCache);
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

      private void createRecentDataCache()
      {
         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilter, _keywords,
            Program.Settings.UpdateManagerExtendedLogging, "Recent");
         _recentDataCache = new DataCache(dataCacheContext, _modificationNotifier, _connectionChecker);
         getListView(EDataCacheType.Recent).SetDataCache(_recentDataCache);
      }

      private void subscribeToRecentDataCacheInternalEvents()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Recent);
         if (dataCache?.MergeRequestCache != null)
         {
            dataCache.MergeRequestCache.MergeRequestEvent += onRecentMergeRequestEvent;
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

