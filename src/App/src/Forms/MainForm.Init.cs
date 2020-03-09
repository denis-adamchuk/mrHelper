using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
using mrHelper.DiffTool;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Types;
using mrHelper.Client.Workflow;
using mrHelper.Client.Discussions;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.MergeRequests;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;

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
                  await onStopTimer(true);
               }

               bool reload = command.GetReload();
               if (reload)
               {
                  enqueueCheckForUpdates(mergeRequestKey.Value,
                     Program.Settings.OneShotUpdateFirstChanceDelayMs,
                     Program.Settings.OneShotUpdateSecondChanceDelayMs);
               }
            };
            groupBoxActions.Controls.Add(button);
            id++;
         }
      }

      private void loadConfiguration()
      {
         Trace.TraceInformation("[MainForm] Loading configuration");
         Program.Settings.PropertyChanged += onSettingsPropertyChanged;

         Debug.Assert(Program.Settings.KnownHosts.Count() == Program.Settings.KnownAccessTokens.Count());
         // Remove all items except header
         for (int iListViewItem = 1; iListViewItem < listViewKnownHosts.Items.Count; ++iListViewItem)
         {
            listViewKnownHosts.Items.RemoveAt(iListViewItem);
         }

         List<string> newKnownHosts = new List<string>();
         for (int iKnownHost = 0; iKnownHost < Program.Settings.KnownHosts.Count(); ++iKnownHost)
         {
            // Upgrade from old versions which did not have prefix
            string host = getHostWithPrefix(Program.Settings.KnownHosts[iKnownHost]);
            string accessToken = Program.Settings.KnownAccessTokens.Length > iKnownHost
               ? Program.Settings.KnownAccessTokens[iKnownHost]
               : String.Empty;
            addKnownHost(host, accessToken);
            newKnownHosts.Add(host);
         }
         Program.Settings.KnownHosts = newKnownHosts.ToArray();

         if (Program.Settings.ColorSchemeFileName == String.Empty)
         {
            // Upgrade from old versions which did not have a separate file for Default color scheme
            Program.Settings.ColorSchemeFileName = getDefaultColorSchemeFileName();
         }

         textBoxLocalGitFolder.Text = Program.Settings.LocalGitFolder;
         checkBoxLabels.Checked = Program.Settings.CheckedLabelsFilter;
         textBoxLabels.Text = Program.Settings.LastUsedLabels;
         checkBoxMinimizeOnClose.Checked = Program.Settings.MinimizeOnClose;
         checkBoxDisableSplitterRestrictions.Checked = Program.Settings.DisableSplitterRestrictions;
         checkBoxShowNewMergeRequests.Checked = Program.Settings.Notifications_NewMergeRequests;
         checkBoxShowMergedMergeRequests.Checked = Program.Settings.Notifications_MergedMergeRequests;
         checkBoxShowUpdatedMergeRequests.Checked = Program.Settings.Notifications_UpdatedMergeRequests;
         checkBoxShowResolvedAll.Checked = Program.Settings.Notifications_AllThreadsResolved;
         checkBoxShowOnMention.Checked = Program.Settings.Notifications_OnMention;
         checkBoxShowKeywords.Checked = Program.Settings.Notifications_Keywords;
         checkBoxShowMyActivity.Checked = Program.Settings.Notifications_MyActivity;
         checkBoxShowServiceNotifications.Checked = Program.Settings.Notifications_Service;

         if (comboBoxDCDepth.Items.Contains(Program.Settings.DiffContextDepth))
         {
            comboBoxDCDepth.Text = Program.Settings.DiffContextDepth;
         }
         else
         {
            comboBoxDCDepth.SelectedIndex = 0;
         }

         Dictionary<string, int> columnWidths = Program.Settings.ListViewMergeRequestsColumnWidths;
         foreach (ColumnHeader column in listViewMergeRequests.Columns)
         {
            string columnName = (string)column.Tag;
            if (columnWidths.ContainsKey(columnName))
            {
               column.Width = columnWidths[columnName];
            }
         }

         try
         {
            WinFormsHelpers.ReorderListViewColumns(listViewMergeRequests,
               Program.Settings.ListViewMergeRequestsDisplayIndices);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("[MainForm] Cannot restore list view column display indices", ex);
            Program.Settings.ListViewMergeRequestsDisplayIndices =
               WinFormsHelpers.GetListViewDisplayIndices(listViewMergeRequests);
         }

         WinFormsHelpers.FillComboBox(comboBoxFonts,
            Constants.MainWindowFontSizeChoices, Program.Settings.MainWindowFontSizeName);
         applyFont(Program.Settings.MainWindowFontSizeName);

         WinFormsHelpers.FillComboBox(comboBoxThemes,
            Constants.ThemeNames, Program.Settings.VisualThemeName);
         applyTheme(Program.Settings.VisualThemeName);

         if (!Program.Settings.HasSelectedProjects())
         {
            setupDefaultProjectList();
         }

         Trace.TraceInformation("[MainForm] Configuration loaded");
      }

      private bool integrateInTools()
      {
         string gitPath = AppFinder.GetInstallPath(new string[] { "Git version 2" });
         if (String.IsNullOrEmpty(gitPath))
         {
            MessageBox.Show(
               "Git for Windows (version 2) is not installed. It must be installed at least for the current user. Application cannot start.",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
         }

         string gitBinaryFolder = Path.Combine(gitPath, "bin");
         string pathEV = System.Environment.GetEnvironmentVariable("PATH");
         System.Environment.SetEnvironmentVariable("PATH", pathEV + ";" + gitBinaryFolder);
         Trace.TraceInformation(String.Format("Updated PATH variable: {0}",
            System.Environment.GetEnvironmentVariable("PATH")));

         IIntegratedDiffTool diffTool = new BC3Tool();
         DiffToolIntegration integration = new DiffToolIntegration();

         try
         {
            integration.Integrate(diffTool);
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

         return true;
      }

      async private Task initializeWork()
      {
         cleanUpInstallers();
         checkForApplicationUpdates();

         restoreState();
         prepareFormToStart();

         _gitClientUpdater = new GitInteractiveUpdater();
         createWorkflowAndDependencies();
         createSearchWorkflow();

         subscribeToWorkflowAndDependencies();
         subscribeToSearchWorkflow();
         _gitClientUpdater.InitializationStatusChange += onGitInitStatusChange;

         initializeColorScheme();
         initializeIconScheme();

         await connectOnStartup();
      }

      async private Task finalizeWork()
      {
         _exiting = true; // to prevent execution of Dispose() which is called while we are in 'await'

         Program.Settings.PropertyChanged -= onSettingsPropertyChanged;

         _gitClientUpdater.InitializationStatusChange -= onGitInitStatusChange;
         unsubscribeFromWorkflowAndDependencies();
         unsubscribeFromSearchWorkflow();

         await disposeLocalGitRepositoryFactory();
         await _workflowManager.CancelAsync();

         saveState();
         Interprocess.SnapshotSerializer.CleanUpSnapshots();

         _exiting = false; // now we can Dispose()
         Dispose();

         Trace.TraceInformation(String.Format("[MainForm] Form disposed. Work finalized. Exiting."));
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
         updateCaption();
         updateTabControlSelection();
         updateHostsDropdownList();
         fillColorSchemesList();
         prepareControlsToStart();
      }

      private void prepareControlsToStart()
      {
         WindowState = FormWindowState.Maximized;

         buttonTimeTrackingStart.Text = buttonStartTimerDefaultText;
         labelWorkflowStatus.Text = String.Empty;
         labelGitStatus.Text = String.Empty;

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
         }

         if (Program.ServiceManager.GetBugReportEmail() != String.Empty)
         {
            linkLabelSendFeedback.Visible = true;
         }

         if (Program.Settings.MainWindowSplitterDistance != 0)
         {
            splitContainer1.SplitterDistance = Program.Settings.MainWindowSplitterDistance;
         }

         if (Program.Settings.RightPaneSplitterDistance != 0)
         {
            splitContainer2.SplitterDistance = Program.Settings.RightPaneSplitterDistance;
         }

         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);
      }

      async private Task connectOnStartup()
      {
         try
         {
            string[] arguments = Environment.GetCommandLineArgs();
            string url = arguments.Length > 1 ? arguments[1] : String.Empty;

            if (url != String.Empty)
            {
               await connectToUrlAsync(url);
            }
            else
            {
               selectHost(PreferredSelection.Initial);
               await switchHostToSelected();
            }
         }
         catch (WorkflowException ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void createWorkflowAndDependencies()
      {
         _workflowManager = new WorkflowManager(Program.Settings);
         _expressionResolver = new ExpressionResolver(this);
         _mergeRequestCache = new MergeRequestCache(this, this, Program.Settings,
            Program.Settings.AutoUpdatePeriodMs);
         _discussionManager = new DiscussionManager(Program.Settings, this, _mergeRequestCache, this, _keywords,
            Program.Settings.AutoUpdatePeriodMs);
         _eventFilter = new EventFilter(Program.Settings, this, _mergeRequestCache);
         _userNotifier = new UserNotifier(_trayIcon, Program.Settings, _mergeRequestCache, _discussionManager,
            _eventFilter);
         _gitDataUpdater = Program.Settings.CacheRevisionsInBackground
            ? new GitDataUpdater(this, this, Program.Settings, this, _mergeRequestCache, _mergeRequestCache)
            : null;
         _gitStatManager = new GitStatisticManager(this, this, this, _mergeRequestCache, _mergeRequestCache);
         _timeTrackingManager = new TimeTrackingManager(Program.Settings, this, _discussionManager);
      }

      private void disposeWorkflowDependencies()
      {
         _timeTrackingManager.Dispose();
         _gitDataUpdater?.Dispose();
         _gitStatManager.Dispose();
         _userNotifier.Dispose();
         _eventFilter.Dispose();
         _discussionManager.Dispose();
         _mergeRequestCache.Dispose();
         _expressionResolver.Dispose();
      }

      private void subscribeToWorkflowAndDependencies()
      {
         subscribeToWorkflow();

         _mergeRequestCache.MergeRequestEvent += processUpdate;
         _gitStatManager.Update += onGitStatisticManagerUpdate;

         _timeTrackingManager.PreLoadTotalTime += onPreLoadTrackedTime;
         _timeTrackingManager.PostLoadTotalTime += onPostLoadTrackedTime;
      }

      private void unsubscribeFromWorkflowAndDependencies()
      {
         unsubscribeFromWorkflow();

         _mergeRequestCache.MergeRequestEvent -= processUpdate;
         _gitStatManager.Update -= onGitStatisticManagerUpdate;

         _timeTrackingManager.PreLoadTotalTime -= onPreLoadTrackedTime;
         _timeTrackingManager.PostLoadTotalTime -= onPostLoadTrackedTime;
      }

      private void onGitInitStatusChange(string status)
      {
         labelWorkflowStatus.Text = status;
         labelWorkflowStatus.Update();
      }

      private void onGitStatisticManagerUpdate()
      {
         listViewMergeRequests.Invalidate();
      }

      private void onPreLoadTrackedTime(MergeRequestKey mrk)
      {
         MergeRequestKey? currentMergeRequest = getMergeRequestKey(null);
         if (currentMergeRequest.HasValue && currentMergeRequest.Value.Equals(mrk))
         {
            // change control enabled state
            updateTotalTime(mrk);
         }
      }

      private void onPostLoadTrackedTime(MergeRequestKey mrk)
      {
         MergeRequestKey? currentMergeRequest = getMergeRequestKey(null);
         if (currentMergeRequest.HasValue && currentMergeRequest.Value.Equals(mrk))
         {
            // change control enabled state and update text
            updateTotalTime(mrk);
         }

         // Update total time column in the table
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
            ConfigurationHelper.SetupProjects(JsonFileReader.
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

