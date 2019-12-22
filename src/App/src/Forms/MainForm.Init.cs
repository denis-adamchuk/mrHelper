using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
using mrHelper.DiffTool;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Core;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Client.Workflow;
using mrHelper.Client.Discussions;
using mrHelper.Client.TimeTracking;
using mrHelper.CommonTools.Persistence;
using mrHelper.Client.MergeRequests;
using mrHelper.Core.Git;

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
            ExceptionHandlers.Handle(ex, "Cannot load custom actions");
         }

         if (_customCommands == null)
         {
            return;
         }

         int id = 0;
         int offsetX = 6;
         System.Drawing.Point offSetFromGroupBoxTopLeft = new System.Drawing.Point
         {
            X = offsetX,
            Y = 17
         };
         System.Drawing.Size typicalSize = new System.Drawing.Size(80, 32);
         foreach (ICommand command in _customCommands)
         {
            string name = command.GetName();
            var button = new System.Windows.Forms.Button
            {
               Name = "customAction" + id,
               Location = offSetFromGroupBoxTopLeft,
               Size = typicalSize,
               Text = name,
               UseVisualStyleBackColor = true,
               Enabled = false,
               TabStop = false,
               Tag = command.GetDependency()
            };
            button.Click += async (x, y) =>
            {
               MergeRequestKey? mergeRequestKey = getMergeRequestKey();

               labelWorkflowStatus.Text = "Command " + name + " is in progress";
               try
               {
                  await command.Run();
               }
               catch (Exception ex) // Whatever happened in Run()
               {
                  ExceptionHandlers.Handle(ex, "Custom action failed");
                  MessageBox.Show("Custom action failed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
               if (reload && mergeRequestKey.HasValue)
               {
                  _mergeRequestManager.CheckForUpdates(mergeRequestKey.Value, Program.Settings.OneShotUpdateDelayMs);
                  _discussionManager.CheckForUpdates(mergeRequestKey.Value, Program.Settings.OneShotUpdateDelayMs);
               }
            };
            groupBoxActions.Controls.Add(button);
            offSetFromGroupBoxTopLeft.X += typicalSize.Width + offsetX;
            groupBoxActions.Size =
               new System.Drawing.Size((offsetX + typicalSize.Width) * (id + 1) + offsetX, groupBoxActions.Height);
            id++;
         }
      }

      private void loadConfiguration()
      {
         Trace.TraceInformation("[MainForm] Loading configuration");

         Debug.Assert(Program.Settings.KnownHosts.Count == Program.Settings.KnownAccessTokens.Count);
         // Remove all items except header
         for (int iListViewItem = 1; iListViewItem < listViewKnownHosts.Items.Count; ++iListViewItem)
         {
            listViewKnownHosts.Items.RemoveAt(iListViewItem);
         }

         List<string> newKnownHosts = new List<string>();
         for (int iKnownHost = 0; iKnownHost < Program.Settings.KnownHosts.Count; ++iKnownHost)
         {
            // Upgrade from old versions which did not have prefix
            string host = getHostWithPrefix(Program.Settings.KnownHosts[iKnownHost]);
            string accessToken = Program.Settings.KnownAccessTokens[iKnownHost];
            addKnownHost(host, accessToken);
            newKnownHosts.Add(host);
         }
         Program.Settings.KnownHosts = newKnownHosts;

         if (Program.Settings.ColorSchemeFileName == String.Empty)
         {
            // Upgrade from old versions which did not have a separate file for Default color scheme
            Program.Settings.ColorSchemeFileName = getDefaultColorSchemeFileName();
         }

         textBoxLocalGitFolder.Text = Program.Settings.LocalGitFolder;
         checkBoxLabels.Checked = Program.Settings.CheckedLabelsFilter;
         textBoxLabels.Text = Program.Settings.LastUsedLabels;
         checkBoxMinimizeOnClose.Checked = Program.Settings.MinimizeOnClose;
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

         comboBoxThemes.Items.Add("Default");
         comboBoxThemes.Items.Add("New Year 2020");

         string selectedTheme = null;
         foreach (string theme in comboBoxThemes.Items.Cast<string>())
         {
            if (theme == Program.Settings.VisualThemeName)
            {
               selectedTheme = theme;
            }
         }

         if (selectedTheme != null)
         {
            comboBoxThemes.SelectedItem = selectedTheme;
         }
         else
         {
            comboBoxThemes.SelectedIndex = 0;
         }

         applyTheme(comboBoxThemes.SelectedItem.ToString());

         if (!Program.Settings.HasSelectedProjects())
         {
            setupDefaultProjectList();
         }

         // TODO Load Program.Settings.SelectedProjects into UI

         Trace.TraceInformation("[MainForm] Configuration loaded");
      }

      private bool integrateInTools()
      {
         string gitPath = CommonTools.AppFinder.GetInstallPath(new string[] { "Git version 2" });
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
         DiffToolIntegration integration = new DiffToolIntegration(new GlobalGitConfiguration());

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
               ExceptionHandlers.Handle(ex, String.Format("Cannot integrate \"{0}\"", diffTool.GetToolName()));
            }
            return false;
         }

         return true;
      }

      private void loadSettings()
      {
         Program.Settings.PropertyChanged += onSettingsPropertyChanged;
         loadConfiguration();

         labelTimeTrackingTrackedTime.Text = labelSpentTimeDefaultText;
         buttonTimeTrackingStart.Text = buttonStartTimerDefaultText;
         labelWorkflowStatus.Text = String.Empty;
         labelGitStatus.Text = String.Empty;
         updateCaption();

         bool configured = listViewKnownHosts.Items.Count > 0
                        && textBoxLocalGitFolder.Text.Length > 0;
         if (configured)
         {
            tabControl.SelectedTab = tabPageMR;
         }
         else
         {
            tabControl.SelectedTab = tabPageSettings;
         }
      }

      async private Task onApplicationStarted()
      {
         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);
         _checkForUpdatesTimer.Tick += new System.EventHandler(onTimerCheckForUpdates);

         _persistentStorage = new PersistentStorage();
         _persistentStorage.OnSerialize += (writer) => onPersistentStorageSerialize(writer);
         _persistentStorage.OnDeserialize += (reader) => onPersistentStorageDeserialize(reader);

         _gitClientUpdater = new GitClientInteractiveUpdater();
         _gitClientUpdater.InitializationStatusChange +=
            (status) =>
         {
            labelWorkflowStatus.Text = status;
            labelWorkflowStatus.Update();
         };

         createWorkflow();

         // Expression resolver requires Workflow 
         _expressionResolver = new ExpressionResolver(_workflow);

         // Color Scheme requires Expression Resolver
         fillColorSchemesList();
         initializeColorScheme();
         initializeIconScheme();

         _mergeRequestManager = new MergeRequestManager(_workflow, this, Program.Settings);
         _mergeRequestManager.MergeRequestEvent += e => processUpdate(e);

         // Discussions Manager subscribers to Workflow and UpdateManager notifications
         IEnumerable<string> keywords = _customCommands ?
            .Where(x => x is SendNoteCommand)
            .Select(x => (x as SendNoteCommand).GetBody()) ?? null;
         checkBoxShowKeywords.Text = "Keywords: " + String.Join(", ", keywords);
         _discussionManager = new DiscussionManager(Program.Settings, _workflow, _mergeRequestManager, this, keywords);

         EventFilter eventFilter = new EventFilter(Program.Settings, _workflow, _mergeRequestManager);
         _userNotifier = new UserNotifier(_trayIcon, Program.Settings, _mergeRequestManager, _discussionManager, eventFilter);

         // Revision Cacher subscribes to Workflow notifications
         if (Program.Settings.CacheRevisionsInBackground)
         {
            _revisionCacher = new RevisionCacher(_workflow, this, Program.Settings,
               projectKey => getGitClient(projectKey, false), _mergeRequestManager);
         }

         // Time Tracking Manager requires Workflow and Discussion Manager
         createTimeTrackingManager();

         try
         {
            _persistentStorage.Deserialize();
         }
         catch (PersistenceStateDeserializationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot deserialize the state");
         }

         updateHostsDropdownList();

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

         checkForApplicationUpdates();
         _checkForUpdatesTimer.Start();

         if (Program.ServiceManager.GetHelpUrl() != String.Empty)
         {
            linkLabelHelp.Visible = true;
         }

         if (Program.ServiceManager.GetBugReportEmail() != String.Empty)
         {
            linkLabelSendFeedback.Visible = true;
         }
      }

      private void createTimeTrackingManager()
      {
         _timeTrackingManager = new TimeTrackingManager(Program.Settings, _workflow, _discussionManager);
         _timeTrackingManager.PreLoadTotalTime +=
            (mrk) =>
         {
            MergeRequestKey? currentMergeRequest = getMergeRequestKey();
            if (currentMergeRequest.HasValue && currentMergeRequest.Value.Equals(mrk))
            {
               // change control enabled state
               updateTotalTime(mrk);
            }
         };
         _timeTrackingManager.PostLoadTotalTime +=
            (mrk) =>
         {
            MergeRequestKey? currentMergeRequest = getMergeRequestKey();
            if (currentMergeRequest.HasValue && currentMergeRequest.Value.Equals(mrk))
            {
               // change control enabled state and update text
               updateTotalTime(mrk);
            }

            // Update total time column in the table
            listViewMergeRequests.Invalidate();
         };
      }

      private void setupDefaultProjectList()
      {
         // Check if file exists. If it does not, it is not an error.
         if (!System.IO.File.Exists(mrHelper.Common.Constants.Constants.ProjectListFileName))
         {
            return;
         }

         try
         {
            ConfigurationHelper.SetupProjects(CommonTools.JsonFileReader.
               LoadFromFile<List<ConfigurationHelper.HostInProjectsFile>>(
                  mrHelper.Common.Constants.Constants.ProjectListFileName), Program.Settings);
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle(ex, "Cannot load projects from file");
            return;
         }
      }
   }
}

