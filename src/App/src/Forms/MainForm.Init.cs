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
using mrHelper.DiffTool;
using mrHelper.Client.Types;
using mrHelper.Client.Session;
using mrHelper.Client.Common;
using mrHelper.Client.TimeTracking;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Tools;
using mrHelper.StorageSupport;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void addCustomActions()
      {
         CustomCommandLoader loader = new CustomCommandLoader(this);
         _customCommands = null;
         try
         {
            string CustomActionsFileName = "CustomActions.xml";
            _customCommands = loader.LoadCommands(CustomActionsFileName);
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
               Size = new System.Drawing.Size { Width = 96, Height = 32 },
               MinimumSize = new System.Drawing.Size { Width = 96, Height = 0 },
               Text = name,
               UseVisualStyleBackColor = true,
               Enabled = false,
               TabStop = false,
               Tag = command.GetDependency()
            };
            button.Click += async (x, y) =>
            {
               MergeRequestKey? mergeRequestKey = getMergeRequestKey(null);
               if (!mergeRequestKey.HasValue)
               {
                  return;
               }

               ITotalTimeCache totalTimeCache = getSession(!isSearchMode())?.TotalTimeCache;

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
               labelWorkflowStatus.Text = "Command " + name + " completed";

               Trace.TraceInformation(String.Format("Custom action {0} completed", name));

               if (command.GetStopTimer())
               {
                  await onStopTimer(true, totalTimeCache);
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
         checkBoxAutoSelectNewestRevision.Checked = Program.Settings.AutoSelectNewestRevision;
         checkBoxShowVersionsByDefault.Checked = Program.Settings.ShowVersionsByDefault;

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

         if (ConfigurationHelper.GetPreferredStorageType(Program.Settings) == LocalCommitStorageType.GitRepository)
         {
            if (ConfigurationHelper.IsShallowCloneAllowed(Program.Settings))
            {
               radioButtonUseGitShallowClone.Checked = true;
            }
            else
            {
               radioButtonUseGitFullClone.Checked = true;
            }
         }
         else
         {
            radioButtonDontUseGit.Checked = true;
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

      private bool integrateInTools()
      {
         if (!GitTools.IsGit2Installed())
         {
            MessageBox.Show(
               "Git for Windows (version 2) is not installed. "
             + "It must be installed at least for the current user. Application cannot start.",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
         }

         string pathEV = System.Environment.GetEnvironmentVariable("PATH");
         System.Environment.SetEnvironmentVariable("PATH", pathEV + ";" + GitTools.GetBinaryFolder());
         Trace.TraceInformation(String.Format("Updated PATH variable: {0}",
            System.Environment.GetEnvironmentVariable("PATH")));
         System.Environment.SetEnvironmentVariable("GIT_TERMINAL_PROMPT", "0");
         Trace.TraceInformation("Set GIT_TERMINAL_PROMPT=0");

         IIntegratedDiffTool diffTool = new BC3Tool();
         DiffToolIntegration integration = new DiffToolIntegration();

         string self = _runningAsUwp
            ? Constants.UWP_Launcher_Name
            : Process.GetCurrentProcess().MainModule.FileName;

         try
         {
            integration.Integrate(diffTool, self);
         }
         catch (Exception ex)
         {
            if (ex is DiffToolNotInstalledException)
            {
               MessageBox.Show(
                  "Beyond Compare 3 is not installed. It must be installed at least for the current user. " +
                  "Application cannot start", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
               MessageBox.Show("Beyond Compare 3 integration failed. Application cannot start. See logs for details",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               ExceptionHandlers.Handle(String.Format("Cannot integrate \"{0}\"", diffTool.GetToolName()), ex);
            }
            return false;
         }
         finally
         {
            GitTools.TraceGitConfiguration();
         }

         return true;
      }

      private void revertOldInstallations()
      {
         string defaultInstallLocation = StringUtils.GetDefaultInstallLocation(
            Windows.ApplicationModel.Package.Current.PublisherDisplayName);
         AppFinder.AppInfo appInfo = AppFinder.GetApplicationInfo(new string[] { "mrHelper" });
         if (appInfo != null
          || Directory.Exists(defaultInstallLocation)
          || System.IO.File.Exists(StringUtils.GetShortcutFilePath()))
         {
            MessageBox.Show("mrHelper needs to uninstall an old version of itself on this launch. "
              + "It takes a few seconds, please wait...", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

            string currentPackagePath = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
            string revertMsiProjectFolder = "mrHelper.RevertMSI";
            string revertMsiProjectName = "mrHelper.RevertMSI.exe";
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
               FileName = System.IO.Path.Combine(currentPackagePath, revertMsiProjectFolder, revertMsiProjectName),
               WorkingDirectory = System.IO.Path.Combine(currentPackagePath, revertMsiProjectFolder),
               Verb = "runas", // revert implies work with registry
            };
            Process p = Process.Start(startInfo);
            p.WaitForExit();
            Trace.TraceInformation(String.Format("[MainForm] {0} exited with code {1}", revertMsiProjectName, p.ExitCode));
         }
      }

      private void initializeWork()
      {
         restoreState();
         prepareFormToStart();

         createGitLabClientManager();
         createLiveSessionAndDependencies();
         subscribeToLiveSession();
         createSearchSession();

         initializeColorScheme();
         initializeIconScheme();
         initializeBadgeScheme();
      }

      private void createGitLabClientManager()
      {
         GitLabClientContext clientContext = new GitLabClientContext
            (this, Program.Settings, _mergeRequestFilter, _keywords, Program.Settings.AutoUpdatePeriodMs);
         _gitlabClientManager = new Client.Common.GitLabClientManager(clientContext);
      }

      private void finalizeWork()
      {
         _requestedDiff.Clear();
         _requestedUrl.Clear();

         Program.Settings.PropertyChanged -= onSettingsPropertyChanged;

         unsubscribeFromLiveSession();

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

      private void connectOnStartup()
      {
         // TODO Argument manipulation shall be rewritten to avoid copy/paste of option names
         string[] arguments = Environment.GetCommandLineArgs();
         string url = arguments.Length > 1 && arguments[1] != "-m" ? arguments[1] : String.Empty;
         enqueueUrlConnectionRequest(url, true);
      }

      private void createLiveSessionAndDependencies()
      {
         _liveSession = _gitlabClientManager.SessionManager.CreateSession();
         _expressionResolver = new ExpressionResolver(_liveSession);
         _eventFilter = new EventFilter(Program.Settings, _liveSession, _mergeRequestFilter);
         _userNotifier = new UserNotifier(_liveSession, _eventFilter, _trayIcon);
      }

      private void createSearchSession()
      {
         _searchSession = _gitlabClientManager.SessionManager.CreateSession();
      }

      private void disposeLiveSessionDependencies()
      {
         _userNotifier?.Dispose();
         _eventFilter?.Dispose();
         _expressionResolver?.Dispose();
      }

      private void subscribeToLiveSession()
      {
         if (_liveSession != null)
         {
            _liveSession.Stopped += liveSessionStopped;
            _liveSession.Started += liveSessionStarted;
         }
      }

      private void subscribeToLiveSessionInternalEvents()
      {
         if (_liveSession?.MergeRequestCache != null)
         {
            _liveSession.MergeRequestCache.MergeRequestEvent += processUpdate;
         }

         if (_liveSession?.TotalTimeCache != null)
         {
            _liveSession.TotalTimeCache.TotalTimeLoading += onPreLoadTrackedTime;
            _liveSession.TotalTimeCache.TotalTimeLoaded += onPostLoadTrackedTime;
         }

         if (_liveSession?.DiscussionCache != null)
         {
            _liveSession.DiscussionCache.DiscussionsLoading += onPreLoadDiscussions;
            _liveSession.DiscussionCache.DiscussionsLoaded += onPostLoadDiscussions;
         }
      }

      private void unsubscribeFromLiveSession()
      {
         if (_liveSession != null)
         {
            _liveSession.Stopped -= liveSessionStopped;
            _liveSession.Started -= liveSessionStarted;
         }
      }

      private void unsubscribeFromLiveSessionInternalEvents()
      {
         if (_liveSession?.MergeRequestCache != null)
         {
            _liveSession.MergeRequestCache.MergeRequestEvent -= processUpdate;
         }

         if (_liveSession?.TotalTimeCache != null)
         {
            _liveSession.TotalTimeCache.TotalTimeLoading -= onPreLoadTrackedTime;
            _liveSession.TotalTimeCache.TotalTimeLoaded -= onPostLoadTrackedTime;
         }

         if (_liveSession?.DiscussionCache != null)
         {
            _liveSession.DiscussionCache.DiscussionsLoading -= onPreLoadDiscussions;
            _liveSession.DiscussionCache.DiscussionsLoaded -= onPostLoadDiscussions;
         }
      }

      private void createGitHelpers(ISession session, ILocalCommitStorageFactory factory)
      {
         if (session.MergeRequestCache == null || session.DiscussionCache == null)
         {
            return;
         }

         _gitDataUpdater = Program.Settings.CacheRevisionsPeriodMs > 0
            ? new GitDataUpdater(
               session.MergeRequestCache, session.DiscussionCache, this, factory,
               Program.Settings.CacheRevisionsPeriodMs, _mergeRequestFilter)
            : null;

         if (Program.Settings.UseGitBasedSizeCollection)
         {
            _diffStatProvider = new GitBasedDiffStatProvider(
               session.MergeRequestCache, session.DiscussionCache, this, factory);
         }
         else
         {
            _diffStatProvider = new DiscussionBasedDiffStatProvider(session.DiscussionCache);
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
            _diffStatProvider.Dispose();
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
         if (!System.IO.File.Exists(Constants.ProjectListFileName))
         {
            return;
         }

         try
         {
            ConfigurationHelper.InitializeSelectedProjects(JsonUtils.
               LoadFromFile<IEnumerable<ConfigurationHelper.HostInProjectsFile>>(
                  Constants.ProjectListFileName), Program.Settings);
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle("Cannot load projects from file", ex);
         }
      }
   }
}

