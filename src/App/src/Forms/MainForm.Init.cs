using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Tools;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
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
               Enabled = false,
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

               ITotalTimeCache totalTimeCache = getDataCache(!isSearchMode())?.TotalTimeCache;

               labelWorkflowStatus.Text = "Command " + name + " is in progress";
               try
               {
                  await command.Run();
               }
               catch (Exception ex) // Whatever happened in Run()
               {
                  string errorMessage = "Custom action failed";
                  ExceptionHandlers.Handle(errorMessage, ex);
                  MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  labelWorkflowStatus.Text = "Command " + name + " failed";
                  return;
               }

               string statusMessage = String.Format("Command {0} completed for merge request !{1} in project {2}",
                  name, mergeRequestKey.Value.IId, mergeRequestKey.Value.ProjectKey.ProjectName);
               labelWorkflowStatus.Text = statusMessage;
               Trace.TraceInformation(String.Format("[MainForm] {0}", statusMessage));

               if (command.GetStopTimer())
               {
                  await onStopTimer(true);
                  onTimerStopped(totalTimeCache);
               }

               bool reload = command.GetReload();
               if (reload)
               {
                  requestUpdates(mergeRequestKey, new int[] {
                     Program.Settings.OneShotUpdateFirstChanceDelayMs,
                     Program.Settings.OneShotUpdateSecondChanceDelayMs });
               }
            };
            groupBoxActions.Controls.Add(button);
            id++;
         }
      }

      private void loadConfiguration()
      {
         _loadingConfiguration = true;
         Trace.TraceInformation("[MainForm] Loading configuration");
         Program.Settings.PropertyChanged += onSettingsPropertyChanged;

         Debug.Assert(Program.Settings.KnownHosts.Count() == Program.Settings.KnownAccessTokens.Count());
         // Remove all items except header
         for (int iListViewItem = 1; iListViewItem < listViewKnownHosts.Items.Count; ++iListViewItem)
         {
            listViewKnownHosts.Items.RemoveAt(iListViewItem);
         }

         List<string> newKnownHosts = new List<string>();
         List<string> newAccessTokens = new List<string>();
         for (int iKnownHost = 0; iKnownHost < Program.Settings.KnownHosts.Count(); ++iKnownHost)
         {
            // Upgrade from old versions which did not have prefix
            string host = StringUtils.GetHostWithPrefix(Program.Settings.KnownHosts[iKnownHost]);
            string accessToken = Program.Settings.KnownAccessTokens.Length > iKnownHost
               ? Program.Settings.KnownAccessTokens[iKnownHost]
               : String.Empty;
            if (addKnownHost(host, accessToken))
            {
               newKnownHosts.Add(host);
               newAccessTokens.Add(accessToken);
            }
         }
         Program.Settings.KnownHosts = newKnownHosts.ToArray();
         Program.Settings.KnownAccessTokens = newAccessTokens.ToArray();

         if (Program.Settings.ColorSchemeFileName == String.Empty)
         {
            // Upgrade from old versions which did not have a separate file for Default color scheme
            Program.Settings.ColorSchemeFileName = getDefaultColorSchemeFileName();
         }

         textBoxStorageFolder.Text = Program.Settings.LocalGitFolder;
         checkBoxDisplayFilter.Checked = Program.Settings.DisplayFilterEnabled;
         textBoxDisplayFilter.Text = Program.Settings.DisplayFilter;
         checkBoxMinimizeOnClose.Checked = Program.Settings.MinimizeOnClose;
         checkBoxRunWhenWindowsStarts.Checked = Program.Settings.RunWhenWindowsStarts;
         applyAutostartSetting(Program.Settings.RunWhenWindowsStarts);
         checkBoxDisableSplitterRestrictions.Checked = Program.Settings.DisableSplitterRestrictions;
         checkBoxNewDiscussionIsTopMostForm.Checked = Program.Settings.NewDiscussionIsTopMostForm;
         checkBoxDisableSpellChecker.Checked = Program.Settings.DisableSpellChecker;
         checkBoxFlatReplies.Checked = !Program.Settings.NeedShiftReplies;

         var diffContextPosition = ConfigurationHelper.GetDiffContextPosition(Program.Settings);
         switch (diffContextPosition)
         {
            case ConfigurationHelper.DiffContextPosition.Top:
               radioButtonDiffContextPositionTop.Checked = true;
               break;

            case ConfigurationHelper.DiffContextPosition.Left:
               radioButtonDiffContextPositionLeft.Checked = true;
               break;

            case ConfigurationHelper.DiffContextPosition.Right:
               radioButtonDiffContextPositionRight.Checked = true;
               break;
         }

         var discussionColumnWidth = ConfigurationHelper.GetDiscussionColumnWidth(Program.Settings);
         switch (discussionColumnWidth)
         {
            case ConfigurationHelper.DiscussionColumnWidth.Narrow:
               radioButtonDiscussionColumnWidthNarrow.Checked = true;
               break;

            case ConfigurationHelper.DiscussionColumnWidth.Medium:
               radioButtonDiscussionColumnWidthMedium.Checked = true;
               break;

            case ConfigurationHelper.DiscussionColumnWidth.Wide:
               radioButtonDiscussionColumnWidthWide.Checked = true;
               break;
         }

         var showWarningsOnFileMismatchMode = ConfigurationHelper.GetShowWarningsOnFileMismatchMode(Program.Settings);
         switch (showWarningsOnFileMismatchMode)
         {
            case ConfigurationHelper.ShowWarningsOnFileMismatchMode.Always:
               radioButtonShowWarningsAlways.Checked = true;
               break;

            case ConfigurationHelper.ShowWarningsOnFileMismatchMode.Never:
               radioButtonShowWarningsNever.Checked = true;
               break;

            case ConfigurationHelper.ShowWarningsOnFileMismatchMode.UntilUserIgnoresFile:
               radioButtonShowWarningsOnce.Checked = true;
               break;
         }

         RevisionType defaultRevisionType = ConfigurationHelper.GetDefaultRevisionType(Program.Settings);
         switch (defaultRevisionType)
         {
            case RevisionType.Commit:
               radioButtonCommits.Checked = true;
               break;

            case RevisionType.Version:
               radioButtonVersions.Checked = true;
               break;
         }

         _mergeRequestFilter = new MergeRequestFilter(createMergeRequestFilterState());
         _mergeRequestFilter.FilterChanged += updateVisibleMergeRequests;

         checkBoxShowNewMergeRequests.Checked = Program.Settings.Notifications_NewMergeRequests;
         checkBoxShowMergedMergeRequests.Checked = Program.Settings.Notifications_MergedMergeRequests;
         checkBoxShowUpdatedMergeRequests.Checked = Program.Settings.Notifications_UpdatedMergeRequests;
         checkBoxShowResolvedAll.Checked = Program.Settings.Notifications_AllThreadsResolved;
         checkBoxShowOnMention.Checked = Program.Settings.Notifications_OnMention;
         checkBoxShowKeywords.Checked = Program.Settings.Notifications_Keywords;
         checkBoxShowMyActivity.Checked = Program.Settings.Notifications_MyActivity;
         checkBoxShowServiceNotifications.Checked = Program.Settings.Notifications_Service;

         if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            radioButtonSelectByProjects.Checked = true;
         }
         else
         {
            radioButtonSelectByUsernames.Checked = true;
         }

         LocalCommitStorageType type = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
         switch (type)
         {
            case LocalCommitStorageType.FileStorage:
               radioButtonDontUseGit.Checked = true;
               break;
            case LocalCommitStorageType.FullGitRepository:
               radioButtonUseGitFullClone.Checked = true;
               break;
            case LocalCommitStorageType.ShallowGitRepository:
               radioButtonUseGitShallowClone.Checked = true;
               break;
         }

         if (comboBoxDCDepth.Items.Contains(Program.Settings.DiffContextDepth))
         {
            comboBoxDCDepth.Text = Program.Settings.DiffContextDepth;
         }
         else
         {
            comboBoxDCDepth.SelectedIndex = 0;
         }

         loadColumnWidths(listViewMergeRequests, Program.Settings.ListViewMergeRequestsColumnWidths);
         loadColumnWidths(listViewFoundMergeRequests, Program.Settings.ListViewFoundMergeRequestsColumnWidths);

         loadColumnIndices(listViewMergeRequests, Program.Settings.ListViewMergeRequestsDisplayIndices,
            x => Program.Settings.ListViewMergeRequestsDisplayIndices = x);
         loadColumnIndices(listViewFoundMergeRequests, Program.Settings.ListViewFoundMergeRequestsDisplayIndices,
            x => Program.Settings.ListViewFoundMergeRequestsDisplayIndices = x);

         WinFormsHelpers.FillComboBox(comboBoxFonts,
            Constants.MainWindowFontSizeChoices, Program.Settings.MainWindowFontSizeName);
         applyFont(Program.Settings.MainWindowFontSizeName);

         WinFormsHelpers.FillComboBox(comboBoxThemes,
            Constants.ThemeNames, Program.Settings.VisualThemeName);
         applyTheme(Program.Settings.VisualThemeName);

         Trace.TraceInformation("[MainForm] Configuration loaded");
         _loadingConfiguration = false;
      }

      private void loadColumnWidths(ListView listView, Dictionary<string, int> storedWidths)
      {
         foreach (ColumnHeader column in listView.Columns)
         {
            string columnName = (string)column.Tag;
            if (storedWidths.ContainsKey(columnName))
            {
               column.Width = storedWidths[columnName];
            }
         }
      }

      private void loadColumnIndices(ListView listView, Dictionary<string, int> storedIndices,
         Action<Dictionary<string, int>> storeDefaults)
      {
         try
         {
            WinFormsHelpers.ReorderListViewColumns(listView, storedIndices);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("[MainForm] Cannot restore list view column display indices", ex);
            storeDefaults(WinFormsHelpers.GetListViewDisplayIndices(listView));
         }
      }


      private void initializeWork()
      {
         restoreState();
         prepareFormToStart();

         createLiveDataCacheAndDependencies();
         subscribeToLiveDataCache();
         createSearchDataCache();

         initializeColorScheme();
         initializeIconScheme();
         initializeBadgeScheme();
      }

      private void finalizeWork()
      {
         _requestedDiff.Clear();
         _requestedUrl.Clear();

         Program.Settings.PropertyChanged -= onSettingsPropertyChanged;

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
         addCustomActions();
         loadConfiguration();
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

      private void prepareControlsToStart()
      {
         buttonTimeTrackingStart.Text = buttonStartTimerDefaultText;
         labelWorkflowStatus.Text = String.Empty;
         labelStorageStatus.Text = String.Empty;

         if (_keywords == null)
         {
            checkBoxShowKeywords.Enabled = false;
         }
         else
         {
            checkBoxShowKeywords.Text = "Keywords: " + String.Join(", ", _keywords);
         }

         if (Program.ServiceManager.GetHelpUrl() != String.Empty)
         {
            linkLabelHelp.Visible = true;
            toolTip.SetToolTip(linkLabelHelp, Program.ServiceManager.GetHelpUrl());
            toolTip.SetToolTip(linkLabelCommitStorageDescription, Program.ServiceManager.GetHelpUrl());
            toolTip.SetToolTip(linkLabelWorkflowDescription, Program.ServiceManager.GetHelpUrl());
         }

         if (Program.ServiceManager.GetBugReportEmail() != String.Empty)
         {
            linkLabelSendFeedback.Visible = true;
            toolTip.SetToolTip(linkLabelSendFeedback, Program.ServiceManager.GetBugReportEmail());
         }

         toolTip.SetToolTip(radioButtonSearchByTitleAndDescription,
            String.Format("{0} (up to {1} results)",
               toolTip.GetToolTip(radioButtonSearchByTitleAndDescription),
               Constants.MaxSearchByTitleAndDescriptionResults));

         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);

         _projectAndUserCacheCheckActions.Add(isCacheReady => buttonCreateNew.Enabled = isCacheReady);

         startClipboardCheckTimer();
         startProjectAndUserCachesCheckTimer();
         createListViewContextMenu();
      }

      private void startClipboardCheckTimer()
      {
         _clipboardCheckingTimer.Tick += new EventHandler(onClipboardCheckingTimer);
         _clipboardCheckingTimer.Start();
      }

      private void startListViewRefreshTimer()
      {
         _listViewRefreshTimer.Tick += (s, e) => listViewMergeRequests.Invalidate();
         _listViewRefreshTimer.Start();
      }

      private void stopListViewRefreshTimer()
      {
         _listViewRefreshTimer.Stop();
      }

      private void createListViewContextMenu()
      {
         listViewMergeRequests.ContextMenuStrip = new ContextMenuStrip();
         ToolStripItemCollection items = listViewMergeRequests.ContextMenuStrip.Items;
         items.Add("&Refresh selected", null, ListViewMergeRequests_Refresh);
         items.Add("-", null, null);
         ToolStripItem editItem = items.Add("&Edit...", null, ListViewMergeRequests_Edit);
         editItem.Enabled = false;
         ToolStripItem mergeItem = items.Add("&Merge...", null, ListViewMergeRequests_Accept);
         mergeItem.Enabled = false;
         items.Add("&Close", null, ListViewMergeRequests_Close);

         _projectAndUserCacheCheckActions.Add(isCacheReady => mergeItem.Enabled = editItem.Enabled = isCacheReady);
      }

      private void startProjectAndUserCachesCheckTimer()
      {
         _projectAndUserCacheCheckTimer.Tick +=
            (s, e) =>
            {
               bool isProjectCacheReady = _liveDataCache?.ProjectCache?.GetProjects().Any() ?? false;
               bool isUserCacheReady = _liveDataCache?.UserCache?.GetUsers().Any() ?? false;
               _projectAndUserCacheCheckActions.ForEach(
                  action => action?.Invoke(isProjectCacheReady && isUserCacheReady));
            };
         _projectAndUserCacheCheckTimer.Start();
      }

      private void prepareSizeToStart()
      {
         if (_startMinimized)
         {
            _forceMaximizeOnNextRestore = Program.Settings.WasMaximizedBeforeClose;
            _applySplitterDistanceOnNextRestore = true;
            WindowState = FormWindowState.Minimized;
         }
         else
         {
            WindowState = Program.Settings.WasMaximizedBeforeClose ? FormWindowState.Maximized : FormWindowState.Normal;
            applySavedSplitterDistance();
         }
      }

      private void applySavedSplitterDistance()
      {
         if (Program.Settings.MainWindowSplitterDistance != 0
            && splitContainer1.Panel1MinSize < Program.Settings.MainWindowSplitterDistance
            && splitContainer1.Width - splitContainer1.Panel2MinSize > Program.Settings.MainWindowSplitterDistance)
         {
            splitContainer1.SplitterDistance = Program.Settings.MainWindowSplitterDistance;
         }

         if (Program.Settings.RightPaneSplitterDistance != 0
            && splitContainer2.Panel1MinSize < Program.Settings.RightPaneSplitterDistance
            && splitContainer2.Width - splitContainer2.Panel2MinSize > Program.Settings.RightPaneSplitterDistance)
         {
            splitContainer2.SplitterDistance = Program.Settings.RightPaneSplitterDistance;
         }
      }

      private void reconnect(string url = null)
      {
         enqueueUrl(url);
      }

      private void createLiveDataCacheAndDependencies()
      {
         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilter, _keywords);
         _liveDataCache = new DataCache(dataCacheContext, _modificationNotifier);
         _expressionResolver = new ExpressionResolver(_liveDataCache);
         _eventFilter = new EventFilter(Program.Settings, _liveDataCache, _mergeRequestFilter);
         _userNotifier = new UserNotifier(_liveDataCache, _eventFilter, _trayIcon);
      }

      private void createSearchDataCache()
      {
         DataCacheContext dataCacheContext = new DataCacheContext(this, _mergeRequestFilter, _keywords);
         _searchDataCache = new DataCache(dataCacheContext, _modificationNotifier);
      }

      private void disposeLiveDataCacheDependencies()
      {
         _userNotifier?.Dispose();
         _eventFilter?.Dispose();
         _expressionResolver?.Dispose();
      }

      private void subscribeToLiveDataCache()
      {
         if (_liveDataCache != null)
         {
            _liveDataCache.Disconnected += liveDataCacheDisconnected;
            _liveDataCache.Connected += liveDataCacheConnected;
         }
      }

      private void subscribeToLiveDataCacheInternalEvents()
      {
         if (_liveDataCache?.MergeRequestCache != null)
         {
            _liveDataCache.MergeRequestCache.MergeRequestEvent += onMergeRequestEvent;
            _liveDataCache.MergeRequestCache.MergeRequestListRefreshed += onMergeRequestListRefreshed;
            _liveDataCache.MergeRequestCache.MergeRequestRefreshed += onMergeRequestRefreshed;
         }

         if (_liveDataCache?.TotalTimeCache != null)
         {
            _liveDataCache.TotalTimeCache.TotalTimeLoading += onPreLoadTrackedTime;
            _liveDataCache.TotalTimeCache.TotalTimeLoaded += onPostLoadTrackedTime;
         }

         if (_liveDataCache?.DiscussionCache != null)
         {
            _liveDataCache.DiscussionCache.DiscussionsLoading += onPreLoadDiscussions;
            _liveDataCache.DiscussionCache.DiscussionsLoaded += onPostLoadDiscussions;
         }
      }

      private void unsubscribeFromLiveDataCache()
      {
         if (_liveDataCache != null)
         {
            _liveDataCache.Disconnected -= liveDataCacheDisconnected;
            _liveDataCache.Connected -= liveDataCacheConnected;
         }
      }

      private void unsubscribeFromLiveDataCacheInternalEvents()
      {
         if (_liveDataCache?.MergeRequestCache != null)
         {
            _liveDataCache.MergeRequestCache.MergeRequestEvent -= onMergeRequestEvent;
            _liveDataCache.MergeRequestCache.MergeRequestListRefreshed -= onMergeRequestListRefreshed;
            _liveDataCache.MergeRequestCache.MergeRequestRefreshed -= onMergeRequestRefreshed;
         }

         if (_liveDataCache?.TotalTimeCache != null)
         {
            _liveDataCache.TotalTimeCache.TotalTimeLoading -= onPreLoadTrackedTime;
            _liveDataCache.TotalTimeCache.TotalTimeLoaded -= onPostLoadTrackedTime;
         }

         if (_liveDataCache?.DiscussionCache != null)
         {
            _liveDataCache.DiscussionCache.DiscussionsLoading -= onPreLoadDiscussions;
            _liveDataCache.DiscussionCache.DiscussionsLoaded -= onPostLoadDiscussions;
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
         listViewMergeRequests.Invalidate();
      }

      private void onPreLoadTrackedTime(ITotalTimeCache totalTimeCache, MergeRequestKey mrk)
      {
         onTrackedTimeManagerEvent(totalTimeCache, mrk);
      }

      private void onPostLoadTrackedTime(ITotalTimeCache totalTimeCache, MergeRequestKey mrk)
      {
         onTrackedTimeManagerEvent(totalTimeCache, mrk);
      }

      private void onTrackedTimeManagerEvent(ITotalTimeCache totalTimeCache, MergeRequestKey mrk)
      {
         MergeRequestKey? currentMergeRequestKey = getMergeRequestKey(null);
         if (currentMergeRequestKey.HasValue && currentMergeRequestKey.Value.Equals(mrk))
         {
            MergeRequest currentMergeRequest = getMergeRequest(null);
            if (currentMergeRequest != null)
            {
               // change control enabled state
               updateTotalTime(currentMergeRequestKey,
                  currentMergeRequest.Author, currentMergeRequestKey.Value.ProjectKey.HostName, totalTimeCache);
            }
         }

         // Update total time column in the table
         listViewMergeRequests.Invalidate();
      }

      private void onPreLoadDiscussions(MergeRequestKey mrk)
      {
         onDiscussionManagerEvent();
      }

      private void onPostLoadDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions)
      {
         onDiscussionManagerEvent();
      }

      private void onDiscussionManagerEvent()
      {
         // Update Discussions column in the table
         listViewMergeRequests.Invalidate();
      }

      private void setupDefaultProjectList()
      {
         // Check if file exists. If it does not, it is not an error.
         if (!System.IO.File.Exists(ProjectListFileName))
         {
            return;
         }

         try
         {
            ConfigurationHelper.InitializeSelectedProjects(JsonUtils.
               LoadFromFile<IEnumerable<ConfigurationHelper.HostInProjectsFile>>(
                  ProjectListFileName), Program.Settings);
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle("Cannot load projects from file", ex);
         }
      }
   }
}

