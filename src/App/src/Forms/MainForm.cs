﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.CustomActions;
using mrHelper.Common.Interfaces;
using mrHelper.Core;
using mrHelper.Client;
using mrHelper.Forms;

namespace mrHelper.App.Forms
{
   delegate void UpdateTextCallback(string text);
   delegate Task Command();

   public partial class mrHelperForm : Form, ICommandCallback
   {
      private static readonly string buttonStartTimerDefaultText = "Start Timer";
      private static readonly string buttonStartTimerTrackingText = "Send Spent";
      private static readonly string labelSpentTimeDefaultText = "00:00:00";
      private static readonly int timeTrackingTimerInterval = 1000; // ms

      /// <summary>
      /// Tooltip timeout in seconds
      /// </summary>
      private const int notifyTooltipTimeout = 5;

      public const string GitDiffToolName = "mrhelperdiff";
      private const string DefaultColorSchemeName = "Default";
      private const string ColorSchemeFileNamePrefix = "colors.json";

      public mrHelperForm()
      {
         InitializeComponent();
      }

      /// <summary>
      /// All exceptions thrown within this method are fatal errors, just pass them to upper level handler
      /// </summary>
      async private void MrHelperForm_Load(object sender, EventArgs e)
      {
         loadSettings();
         addCustomActions();
         integrateInTools();
         await onApplicationStarted();
      }

      async private void MrHelperForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         if (checkBoxMinimizeOnClose.Checked && !_exiting)
         {
            onHideToTray(e);
         }
         else if (_workflow != null)
         {
            Hide();
            e.Cancel = true;
            await _workflow.CancelAsync();
            _workflow.Dispose();
            _workflow = null;
            Close();
         }
      }

      private void NotifyIcon_DoubleClick(object sender, EventArgs e)
      {
         onRestoreWindow();
      }

      private void ButtonDifftool_Click(object sender, EventArgs e)
      {
         onLaunchDiffTool();
      }

      async private void ButtonToggleTimer_Click(object sender, EventArgs e)
      {
         if (_timeTrackingTimer.Enabled)
         {
            await onStopTimer();
         }
         else
         {
            await onStartTimer();
         }
      }

      private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         onExitingByUser();
      }

      private void ButtonBrowseLocalGitFolder_Click(object sender, EventArgs e)
      {
         localGitFolderBrowser.SelectedPath = textBoxLocalGitFolder.Text;
         if (localGitFolderBrowser.ShowDialog() == DialogResult.OK)
         {
            onGitFolderSelected();
         }
      }

      private void ComboBoxColorSchemes_SelectedIndexChanged(object sender, EventArgs e)
      {
         if (comboBoxColorSchemes.SelectedItem.ToString() == DefaultColorSchemeName)
         {
            _colorScheme = new ColorScheme();
            return;
         }

         try
         {
            _colorScheme = new ColorScheme(comboBoxColorSchemes.SelectedItem.ToString());
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle(ex, "Cannot change color scheme");
            MessageBox.Show("Cannot change color scheme", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            comboBoxColorSchemes.SelectedIndex = 0; // recursive
         }

         _settings.ColorSchemeFileName = (sender as ComboBox).Text;
      }

      async private void ComboBoxHost_SelectedIndexChanged(object sender, EventArgs e)
      {
         updateGitStatusText(String.Empty);

         _settings.LastSelectedHost = (sender as ComboBox).Text;
         await onChangeHost((HostComboBoxItem)((sender as ComboBox).SelectedItem).Host);
      }

      async private void ComboBoxProjects_SelectedIndexChanged(object sender, EventArgs e)
      {
         updateGitStatusText(String.Empty);

         _settings.LastSelectedProject = (sender as ComboBox).Text;
         await onChangeProject((sender as ComboBox).Text);
      }

      async private void ComboBoxFilteredMergeRequests_SelectedIndexChanged(object sender, EventArgs e)
      {
         await onChangeMergeRequest();
      }

      async private void ButtonApplyLabels_Click(object sender, EventArgs e)
      {
         _settings.LastUsedLabels = textBoxLabels.Text;
      }

      private void ComboBoxLeftVersion_SelectedIndexChanged(object sender, EventArgs e)
      {
         checkComboboxVersionsOrder(true /* I'm left one */);
      }

      private void ComboBoxRightVersion_SelectedIndexChanged(object sender, EventArgs e)
      {
         checkComboboxVersionsOrder(false /* because I'm the right one */);
      }

      private void ComboBoxHost_Format(object sender, ListControlConvertEventArgs e)
      {
         formatHostListItem(e);
      }

      private void ComboBoxProjects_Format(object sender, ListControlConvertEventArgs e)
      {
         formatProjectsListItem(e);
      }

      private void ComboBoxFilteredMergeRequests_Format(object sender, ListControlConvertEventArgs e)
      {
         formatMergeRequestListItem(e);
      }

      private void ComboBoxVersion_Format(object sender, ListControlConvertEventArgs e)
      {
         formatVersionComboboxItem(e);
      }

      private void LinkLabelConnectedTo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         try
         {
            // this should open a browser
            Process.Start(linkLabelConnectedTo.Text);
         }
         catch (Exception ex) // see Process.Start exception list
         {
            ExceptionHandlers.Handle(ex, "Cannot open URL");
            MessageBox.Show("Cannot open URL", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonAddKnownHost_Click(object sender, EventArgs e)
      {
         AddKnownHostForm form = new AddKnownHostForm();
         if (form.ShowDialog() == DialogResult.OK)
         {
            if (!onAddKnownHost(form.Host, form.AccessToken))
            {
               MessageBox.Show("Such host is already in the list", "Host will not be added",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            _settings.KnownHosts = listViewKnownHosts.Items.Cast<ListViewItem>().Select(i => i.Text).ToList();
            _settings.KnownAccessTokens = listViewKnownHosts.Items.Cast<ListViewItem>()
               .Select(i => i.SubItems[1].Text).ToList();
         }
      }

      private void ButtonRemoveKnownHost_Click(object sender, EventArgs e)
      {
         onRemoveKnownHost();
      }

      async private void CheckBoxShowPublicOnly_CheckedChanged(object sender, EventArgs e)
      {
         _settings.ShowPublicOnly = (sender as CheckBox).Checked;
      }

      async private void CheckBoxRequireTimer_CheckedChanged(object sender, EventArgs e)
      {
         _settings.RequireTimeTracking = (sender as CheckBox).Checked;
         await updateSnapshot();
      }

      private void CheckBoxMinimizeOnClose_CheckedChanged(object sender, EventArgs e)
      {
         _settings.MinimizeOnClose = (sender as CheckBox).Checked;
      }

      private void CheckBoxLabels_CheckedChanged(object sender, EventArgs e)
      {
         _settings.CheckedLabelsFilter = (sender as CheckBox).Checked;
      }

      private void comboBoxDCDepth_SelectedIndexChanged(object sender, EventArgs e)
      {
         _settings.DiffContextDepth = (sender as ComboBox).Text;
      }

      async private void ButtonDiscussions_Click(object sender, EventArgs e)
      {
         await showDiscussionsFormAsync();
      }

      private void LinkLabelAbortGit_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         _gitClientManager.CancelOperation();
      }

      private void createGitClientManager(string localFolder, string hostName)
      {
         _gitClientManager = null;

         try
         {
            _gitClientManager = new GitClientManager(localFolder, hostName);
            _gitClientManager.OnOperationStatusChange +=
               (sender, e) => updateGitStatusText(((GitUtils.OperationStatusChangeArgs)e).Status);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot create Git Client");
            MessageBox.Show("Cannot create Git Client, check temp folder path",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void addCustomActions()
      {
         List<ICommand> commands = Tools.LoadCustomActions();
         if (commands == null)
         {
            return;
         }

         int id = 0;
         System.Drawing.Point offSetFromGroupBoxTopLeft = new System.Drawing.Point
         {
            X = 10,
            Y = 17
         };
         System.Drawing.Size typicalSize = new System.Drawing.Size(83, 27);
         foreach (var command in commands)
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
               TabStop = false
            };
            button.Click += async (x, y) =>
            {
               labelWorkflowStatus.Text = "Command " + name + " is in progress";
               try
               {
                  await command.Run();
               }
               catch (Exception ex) // Whatever happened in Run()
               {
                  ExceptionHandlers.Handle(ex, "Custom action failed");
                  MessageBox.Show("Custom action failed", "Error",
                     MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return;
               }
               labelWorkflowStatus.Text = "Command " + name + " completed";
            };
            groupBoxActions.Controls.Add(button);
            offSetFromGroupBoxTopLeft.X += typicalSize.Width + 10;
            id++;
         }
      }

      async private Task showDiscussionsFormAsync()
      {
         GitClient gitClient = null;
         try
         {
            gitClient = await _gitClientManager.GetClientAsync(GetCurrentProjectName(), _commitChecker);
         }
         catch (Exception ex)
         {
            if (ex is RepeatOperationException)
            {
               return;
            }
            else if (ex is CancelledByUserException)
            {
               if (MessageBox.Show("Without up-to-date git repository, some context code snippets might be missing. "
                  + "Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                     DialogResult.No)
               {
                  return;
               }
            }
            else
            {
               Debug.Assert(ex is ArgumentException || ex is GitOperationException);
               ExceptionHandlers.Handle(ex, "Cannot initialize/update git repository");
               MessageBox.Show("Cannot initialize git repository",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }
         }

         User? currentUser = await loadCurrentUserAsync();
         List<Discussion> discussions = await loadDiscussionsAsync();
         if (!currentUser.HasValue || discussions == null)
         {
            return;
         }

         labelWorkflowStatus.Text = "Rendering Discussions Form...";
         labelWorkflowStatus.Update();

         DiscussionsForm form = null;
         try
         {
            form = new DiscussionsForm(_workflow.State.MergeRequestDescriptor, gitClient,
               int.Parse(comboBoxDCDepth.Text), _colorScheme, discussions, _discussionManager,
               currentUser.Value);
         }
         catch (NoDiscussionsToShow)
         {
            MessageBox.Show("No discussions to show.", "Information",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot show Discussions form");
            MessageBox.Show("Cannot show Discussions form", "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }
         finally
         {
            labelWorkflowStatus.Text = String.Empty;
         }

         form.Show();
      }

      private void checkComboboxVersionsOrder(bool shouldReorderRightCombobox)
      {
         if (comboBoxLeftVersion.SelectedItem == null || comboBoxRightVersion.SelectedItem == null)
         {
            return;
         }

         // Left combobox cannot select a version older than in right combobox (replicating gitlab web ui behavior)
         VersionComboBoxItem leftItem = (VersionComboBoxItem)(comboBoxLeftVersion.SelectedItem);
         VersionComboBoxItem rightItem = (VersionComboBoxItem)(comboBoxRightVersion.SelectedItem);
         Debug.Assert(leftItem.TimeStamp.HasValue);

         if (rightItem.TimeStamp.HasValue)
         {
            // Check if order is broken
            if (leftItem.TimeStamp.Value < rightItem.TimeStamp.Value)
            {
               if (shouldReorderRightCombobox)
               {
                  comboBoxRightVersion.SelectedIndex = comboBoxLeftVersion.SelectedIndex;
               }
               else
               {
                  comboBoxLeftVersion.SelectedIndex = comboBoxRightVersion.SelectedIndex;
               }
            }
         }
         else
         {
            // It is ok because a version w/o timestamp is the oldest one
         }
      }

      async private Task<User?> loadCurrentUserAsync()
      {
         labelWorkflowStatus.Text = "Loading current user...";
         User? currentUser = null;
         try
         {
            currentUser = await _workflow.GetCurrentUser();
         }
         catch (WorkflowException)
         {
            MessageBox.Show("Cannot load current user", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         labelWorkflowStatus.Text = String.Empty;
         return currentUser;
      }

      async private Task<List<Discussion>> loadDiscussionsAsync()
      {
         labelWorkflowStatus.Text = "Loading discussions...";
         List<Discussion> discussions;
         try
         {
            discussions = await _discussionManager.GetDiscussionsAsync(_workflow.State.MergeRequestDescriptor);
         }
         catch (DiscussionManagerException)
         {
            MessageBox.Show("Cannot load discussions", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         labelWorkflowStatus.Text = String.Empty;
         return discussions;
      }

      private class HostInProjectsFile
      {
         public string Name = null;
         public List<Project> Projects = null;
      }

      /// <summary>
      /// Loads project list from file with JSON format
      /// Throws ArgumentException
      /// Throws ArgumentNullException
      /// Throws InvalidOperationException
      /// </summary>
      private List<Project> loadProjectsFromFile(string hostname, string filename)
      {
         Debug.Assert(File.Exists(filename));

         string json = System.IO.File.ReadAllText(filename);

         JavaScriptSerializer serializer = new JavaScriptSerializer();

         List<HostInProjectsFile> hosts = null;
         try
         {
            hosts = serializer.Deserialize<List<HostInProjectsFile>>(json);
         }
         catch (Exception) // whatever de-serialization exception
         {
            throw;
         }

         foreach (var host in hosts)
         {
            if (host.Name == hostname)
            {
               return host.Projects;
            }
         }

         return null;
      }

      async private void onLaunchDiffTool()
      {
         _diffToolArgs = null;
         await updateSnapshot(); // to purge serialized snapshot

         GitClient gitClient = null;
         try
         {
            gitClient = await _gitClientManager.GetClientAsync(GetCurrentProjectName(), _commitChecker);
         }
         catch (Exception ex)
         {
            if (ex is CancelledByUserException)
            {
               // User declined to create a repository
               MessageBox.Show("Cannot launch a diff tool without up-to-date git repository", "Warning",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (!(ex is RepeatOperationException))
            {
               Debug.Assert(ex is ArgumentException || ex is GitOperationException);
               ExceptionHandlers.Handle(ex, "Cannot initialize/update git repository");
               MessageBox.Show("Cannot initialize git repository",
                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
         }

         string leftSHA = getGitTag(true /* left */);
         string rightSHA = getGitTag(false /* right */);

         buttonDiffTool.Enabled = false;
         buttonDiscussions.Enabled = false;

         try
         {
            await gitClient.DiffToolAsync(name, leftCommit, rightCommit);
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot launch diff tool");
            MessageBox.Show("Cannot launch diff tool", "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
         }

         _diffToolArgs = new DiffToolArguments
         {
            LeftSHA = leftSHA,
            RightSHA = rightSHA
         };

         await updateSnapshot();
      }

      private void notifyOnMergeRequestEvent(MergeRequest mergeRequest, string title)
      {
         string projectName = String.Empty;
         foreach (var item in comboBoxProjects.Items)
         {
            Project project = (Project)(item);
            if (project.Id == mergeRequest.Project_Id)
            {
               projectName = project.Path_With_Namespace;
            }
         }

         showTooltipBalloon(title, "\""
            + mergeRequest.Title
            + "\" from "
            + mergeRequest.Author.Name
            + " in project "
            + (projectName == String.Empty ? "N/A" : projectName));
      }

      private void notifyOnMergeRequestUpdates(MergeRequestTimerUpdates updates)
      {
         foreach (MergeRequest mergeRequest in updates.NewMergeRequests)
         {
            notifyOnMergeRequestEvent(mergeRequest, "New merge request");
         }

         foreach (MergeRequest mergeRequest in updates.UpdatedMergeRequests)
         {
            notifyOnMergeRequestEvent(mergeRequest, "New commit in merge request");
         }
      }

      /// <summary>
      /// Typically called from another thread
      /// </summary>
      private void updateGitStatusText(string text)
      {
         if (labelGitStatus.InvokeRequired)
         {
            UpdateTextCallback fn = new UpdateTextCallback(updateGitStatusText);
            Invoke(fn, new object [] { text });
         }
         else
         {
            labelGitStatus.Text = text;
         }
      }

      private string getGitTag(bool left)
      {
         // swap sides to be consistent with gitlab web ui
         if (!left)
         {
            Debug.Assert(comboBoxLeftVersion.SelectedItem != null);
            return ((VersionComboBoxItem)comboBoxLeftVersion.SelectedItem).SHA;
         }
         else
         {
            Debug.Assert(comboBoxRightVersion.SelectedItem != null);
            return ((VersionComboBoxItem)comboBoxRightVersion.SelectedItem).SHA;
         }
      }

      private static void formatMergeRequestListItem(ListControlConvertEventArgs e)
      {
         MergeRequest item = ((MergeRequest)e.ListItem);
         e.Value = item.Title + "    " + "[" + item.Author.Username + "]";
      }

      private static void formatVersionComboboxItem(ListControlConvertEventArgs e)
      {
         VersionComboBoxItem item = (VersionComboBoxItem)(e.ListItem);
         e.Value = item.Text;
         if(item.IsLatest)
         {
            e.Value = "Latest";
         }
         else if (item.TimeStamp.HasValue)
         {
            e.Value += " (" + item.TimeStamp.Value.ToLocalTime().ToString("g") + ")";
         }
      }

      private void formatHostListItem(ListControlConvertEventArgs e)
      {
         HostComboBoxItem item = (HostComboBoxItem)(e.ListItem);
         e.Value = item.Host;
      }

      private static void formatProjectsListItem(ListControlConvertEventArgs e)
      {
         Project item = (Project)(e.ListItem);
         e.Value = item.Path_With_Namespace;
      }

      private void loadConfiguration()
      {
         Debug.Assert(_settings.KnownHosts.Count == _settings.KnownAccessTokens.Count);
         // Remove all items except header
         for (int iListViewItem = 1; iListViewItem < listViewKnownHosts.Items.Count; ++iListViewItem)
         {
            listViewKnownHosts.Items.RemoveAt(iListViewItem);
         }
         for (int iKnownHost = 0; iKnownHost < _settings.KnownHosts.Count; ++iKnownHost)
         {
            string host = _settings.KnownHosts[iKnownHost];
            string accessToken = _settings.KnownAccessTokens[iKnownHost];
            addKnownHost(host, accessToken);
         }
         textBoxLocalGitFolder.Text = _settings.LocalGitFolder;
         checkBoxRequireTimer.Checked = _settings.RequireTimeTracking;
         checkBoxLabels.Checked = _settings.CheckedLabelsFilter;
         textBoxLabels.Text = _settings.LastUsedLabels;
         checkBoxShowPublicOnly.Checked = _settings.ShowPublicOnly;
         if (comboBoxDCDepth.Items.Contains(_settings.DiffContextDepth))
         {
            comboBoxDCDepth.Text = _settings.DiffContextDepth;
         }
         else
         {
            comboBoxDCDepth.SelectedIndex = 0;
         }
         checkBoxMinimizeOnClose.Checked = _settings.MinimizeOnClose;
         fillColorSchemesList();
      }

      private void saveConfiguration()
      {
         _settings.Update();
      }

      async private Task updateSnapshot()
      {
         // first of all, delete old snapshot
         SnapshotSerializer serializer = new SnapshotSerializer();
         serializer.PurgeSerialized();

         bool allowReportingIssues = !checkBoxRequireTimer.Checked || _timeTrackingTimer.Enabled;
         if (!allowReportingIssues || !_diffToolArgs.HasValue
          || GetCurrentHostName() == null || GetCurrentProjectName() == null)
         {
            return;
         }

         MergeRequest? mergeRequest = await loadMergeRequestAsync();
         if (!mergeRequest.HasValue)
         {
            return;
         }

         Snapshot snapshot;
         snapshot.AccessToken = GetCurrentAccessToken();
         snapshot.Refs.LeftSHA = _diffToolArgs.Value.LeftSHA;     // Base commit SHA in the source branch
         snapshot.Refs.RightSHA = _diffToolArgs.Value.RightSHA;   // SHA referencing HEAD of this merge request
         snapshot.Host = GetCurrentHostName();
         snapshot.MergeRequestIId = mergeRequest.Value.IId;
         snapshot.Project = GetCurrentProjectName();
         snapshot.TempFolder = textBoxLocalGitFolder.Text;

         serializer.SerializeToDisk(snapshot);
      }

      private void integrateInTools()
      {
         DiffToolIntegration integration = new DiffToolIntegration(new GlobalGitConfiguration());

         try
         {
            integration.Integrate(new BC3Tool());
         }
         catch (Exception ex)
         {
            MessageBox.Show("Diff tool integration failed. Application cannot start. See logs for details",
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            if (ex is DiffToolIntegrationException || ex is GitOperationException)
            {
               ExceptionHandlers.Handle(ex,
                  String.Format("Cannot integrate \"{0}\"", diffTool.GetToolName()));
               return;
            }
            throw;
         }
      }

      private void loadSettings()
      {
         _settings = new UserDefinedSettings(true);
         loadConfiguration();
         _settings.PropertyChanged += onSettingsPropertyChanged;

         labelSpentTime.Text = labelSpentTimeDefaultText;
         buttonToggleTimer.Text = buttonStartTimerDefaultText;
         this.Text += " (" + Application.ProductVersion + ")";

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

      private void onSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         saveConfiguration();
      }

      async private void onApplicationStarted()
      {
         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);

         _workflowManager = new WorkflowManager(_settings);
         _updateManager = new UpdateManager(_settings);
         _timeTrackingManager = new TimeTrackingManager(_settings);
         _discussionManager = new DiscussionManager(_settings);

         updateHostsDropdownList();

         try
         {
            await initializeWorkflow();
         }
         catch (WorkflowException)
         {
            MessageBox.Show("Cannot initialize the workflow. Application cannot start. See logs for details",
               "Error", MessageBoxButtons.OK, MessageBoxIcons.Error)";
         }
      }

      /// <summary>
      /// Populates host list with list of known hosts from Settings
      /// </summary>
      private void updateHostsDropdownList()
      {
         comboBoxHost.SelectedIndex = -1;
         comboBoxHost.Items.Clear();

         foreach (ListViewItem item in listViewKnownHosts.Items)
         {
            HostComboBoxItem hostItem = new HostComboBoxItem
            {
               Host = "https://" + item.Text,
               AccessToken = item.SubItems[1].Text
            };
            comboBoxHost.Items.Add(hostItem);
         }
      }

      async private Task initializeWorkflow()
      {
         Debug.WriteLine("Initializing workflow");

         _workflow = _workflowManager.CreateWorkflow(_settings);
         _workflow.HostSwitched += (sender, state) => onHostChanged(state);
         _workflow.ProjectSwitched += (sender, state) => onProjectChanged(state);
         _workflow.MergeRequestSwitched += (sender, state) => onMergeRequestChanged(state);

         _workflowUpdateChecker = _updateManager.GetWorkflowUpdateChecker(_workflow);
         _workflowUpdateChecker.OnUpdate += async (sender, updates) =>
         {
            notifyOnMergeRequestUpdates(updates);

            WorkflowState state = _workflow.State;
            if (updates.NewMergeRequests.Cast<MergeRequest>.Any(x => x.Project.Id == state.Project.Id)
             || updates.UpdatedMergeRequests.Cast<MergeRequest>.Any(x => x.Project.Id == state.Project.Id))
            {
               // emulate project change to reload merge request list
               // This will automatically update version list (if there are new ones).
               // This will also remove merged merge requests from the list.
               try
               {
                  await _workflow.SwitchProjectAsync(state.Project.Path_With_Namespace);
               }
               catch (WorkflowException ex)
               {
                  ExceptionHandlers.Handle(ex, "Workflow error occurred during auto-update");
               }
            }
         }

         Debug.WriteLine("Disable projects combo box");
         prepareComboBoxToAsyncLoading(comboBoxProjects);

         labelWorkflowStatus.Text = "Initializing...";
         await _workflow.SwitchHostAsync(() =>
         {
            // If Last Selected Host is in the list, select it as initial host.
            // Otherwise, select the first host from the list.
            for (int iKnownHost = 0; iKnownHost < _settings.KnownHosts.Count; ++iKnownHost)
            {
               if (_settings.KnownHosts[iKnownHost] == _settings.LastSelectedHost)
               {
                  return _settings.LastSelectedHost;
               }
            }
            return _settings.KnownHosts.Count > 0 ? _settings.KnownHosts[0] : String.Empty;
         }());
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("Enable projects combo box");
         fixComboBoxAfterAsyncLoading(comboBoxProjects);
      }

      async private Task onChangeHost(string hostName)
      {
         Debug.WriteLine("Update projects dropdown list");

         Debug.WriteLine("Disable projects combo box");
         prepareComboBoxToAsyncLoading(comboBoxProjects);

         labelWorkflowStatus.Text = "Loading projects...";
         try
         {
            await _workflow.SwitchHostAsync(hostName);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch host");
            MessageBox.Show("Cannot select this host", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("Enable projects combo box");
         fixComboBoxAfterAsyncLoading(comboBoxProjects);
      }

      private void onHostChanged()
      {
         comboBoxHost.SelectedText = state.HostName;
         foreach (var project in state.Projects)
         {
            comboBoxProjects.Add(project);
         }
         createGitClientManager(_settings.LocalGitFolder, state.HostName);
      }

      async private Task onChangeProject(string projectName)
      {
         Debug.WriteLine("Update merge requests dropdown list");

         Debug.WriteLine("Disable merge requests combo box");
         prepareComboBoxToAsyncLoading(comboBoxFilteredMergeRequests);

         labelWorkflowStatus.Text = "Loading merge requests...";
         try
         {
            await _workflow.SwitchProjectAsync(projectName);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch project");
            MessageBox.Show("Cannot select this project", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("Enable merge requests combo box");
         fixComboBoxAfterAsyncLoading(comboBoxFilteredMergeRequests);
      }

      private void onProjectChanged(WorkflowState state)
      {
         comboBoxProjects.SelectedText = state.Project.Path_With_Namespace;
         foreach (var mergeRequest in state.MergeRequests)
         {
            comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
         }
      }

      async private Task onChangeMergeRequest(int mergeRequestIId)
      {
         Debug.WriteLine("Let's handle merge request selection");

         Debug.WriteLine("Disable UI controls");
         onMergeRequestLoaded(null);
         addVersionsToComboBoxes(null, null, null);

         textBoxMergeRequestName.Text = "Loading...";
         richTextBoxMergeRequestDescription.Text = "Loading...";

         prepareComboBoxToAsyncLoading(comboBoxLeftVersion);
         prepareComboBoxToAsyncLoading(comboBoxRightVersion);

         Debug.WriteLine("Let's load merge request with Id " + mergeRequestIId.ToString());
         try
         {
            await _workflow.SwitchMergeRequestAsync(mergeRequestIId);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch merge request");
            MessageBox.Show("Cannot select this merge request", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         Debug.WriteLine("Finished loading MR. Current selected MR is " + _workflow.State.MergeRequest.IId);

         fixComboBoxAfterAsyncLoading(comboBoxLeftVersion);
         fixComboBoxAfterAsyncLoading(comboBoxRightVersion);
      }

      private void onMergeRequestChanged(WorkflowState state)
      {
         MergeRequest item = ((MergeRequest)e.ListItem);
         comboBoxFilteredMergeRequests.SelectedText =
            item.Title + "    " + "[" + item.Author.Username + "]";
         onMergeRequestLoaded(item);
         addVersionsToComboBoxes(state.Versions, item.Diff_Refs.Base_SHA, item.Target_Branch);

         _commitChecker = _updateManager.GetCommitChecker(_workflow.State.MergeRequestDescriptor);
      }

      private void onMergeRequestLoaded(MergeRequest? mergeRequest)
      {
         bool success = mergeRequest.HasValue;
         richTextBoxMergeRequestDescription.Text = mergeRequest.HasValue ? mergeRequest.Value.Description : String.Empty;
         textBoxMergeRequestName.Text = mergeRequest.HasValue ? mergeRequest.Value.Title : String.Empty;
         linkLabelConnectedTo.Text = mergeRequest.HasValue ? mergeRequest.Value.Web_Url : String.Empty;
         linkLabelConnectedTo.Visible = success;
         buttonDiscussions.Enabled = success;
         buttonToggleTimer.Enabled = success;
         buttonDiffTool.Enabled = success;
         enableCustomActions(success);
      }

      private void addVersionsToComboBoxes(List<Version> versions, string mrBaseSha, string mrTargetBranch)
      {
         if (versions == null || versions.Count == 0)
         {
            comboBoxLeftVersion.Items.Clear();
            comboBoxRightVersion.Items.Clear();
            return;
         }

         var latest = new VersionComboBoxItem(versions[0]);
         latest.IsLatest = true;
         comboBoxLeftVersion.Items.Add(latest);
         for (int i = 1; i < versions.Count; i++)
         {
            VersionComboBoxItem item = new VersionComboBoxItem(versions[i]);
            if (comboBoxLeftVersion.Items.Cast<VersionComboBoxItem>().Any(x => x.SHA == item.SHA))
            {
               continue;
            }
            comboBoxLeftVersion.Items.Add(item);
            comboBoxRightVersion.Items.Add(item);
         }

         // Add target branch to the right combo-box
         VersionComboBoxItem targetBranch =
            new VersionComboBoxItem(mrBaseSha, mrTargetBranch, null);
         comboBoxRightVersion.Items.Add(targetBranch);
      }

      async private Task onStartTimer()
      {
         // 1. Update button text
         buttonToggleTimer.Text = buttonStartTimerTrackingText;

         // 2. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 3. Start timer
         _timeTrackingTimer.Start();

         // 4. Reset and start stopwatch
         _timeTracker = _timeTrackingManager.GetTracker(_workflow.State.MergeRequestDescriptor);
         _timeTracker.Start();

         // 5. Update information available to other instances
         await updateSnapshot();
      }

      async private Task onStopTimer()
      {
         // 1. Stop stopwatch and send tracked time
         string span = _timeTracker.Elapsed;
         if (sendTrackedTime && span.Seconds > 1)
         {
            labelWorkflowStatus.Text = "Sending tracked time...";
            string duration = span.ToString("hh") + "h " + span.ToString("mm") + "m " + span.ToString("ss") + "s";
            string status = String.Format("Tracked time {0} sent successfully", duration);
            try
            {
               _timeTracker.Stop();
            }
            catch (TimeTrackerException)
            {
               status = "Error occurred. Tracked time is not sent!";
            }
            labelWorkflowStatus.Text = status;
         }
         _timeTracker = null;

         // 2. Stop timer
         _timeTrackingTimer.Stop();

         // 3. Update information available to other instances
         await updateSnapshot();

         // 4. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 5. Update button text
         buttonToggleTimer.Text = buttonStartTimerDefaultText;
      }

      private void onTimer(object sender, EventArgs e)
      {
         labelSpentTime.Text = _timeTracker.Elapsed.ToString(@"hh\:mm\:ss");
      }

      private void onExitingByUser()
      {
         _exiting = true;
         this.Close();
      }

      private void onHideToTray(FormClosingEventArgs e)
      {
         e.Cancel = true;
         if (_requireShowingTooltipOnHideToTray)
         {
            // TODO: Maybe it's a good idea to save the requireShowingTooltipOnHideToTray state
            // so it's only shown once in a lifetime
            showTooltipBalloon("Information", "I will now live in your tray");
            _requireShowingTooltipOnHideToTray = false;
         }
         Hide();
         ShowInTaskbar = false;
      }

      private void showTooltipBalloon(string title, string text)
      {
         notifyIcon.BalloonTipTitle = title;
         notifyIcon.BalloonTipText = text;
         notifyIcon.ShowBalloonTip(notifyTooltipTimeout);
      }

      private void onRestoreWindow()
      {
         ShowInTaskbar = true;
         Show();
      }

      private void onGitFolderSelected()
      {
         textBoxLocalGitFolder.Text = localGitFolderBrowser.SelectedPath;
         _settings.LocalGitFolder = localGitFolderBrowser.SelectedPath;

         MessageBox.Show("Git folder is changed, but it will not affect already opened Diff Tool and Discussions views",
            "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

         updateGitStatusText(String.Empty);
         createGitClientManager(_settings.LocalGitFolder, _workflow.State.HostName);
      }

      private bool addKnownHost(string host, string accessToken)
      {
         foreach (ListViewItem listItem in listViewKnownHosts.Items)
         {
            if (listItem.Text == host)
            {
               return false;
            }
         }

         var item = new ListViewItem(host);
         item.SubItems.Add(accessToken);
         listViewKnownHosts.Items.Add(item);
         return true;
      }

      private bool onAddKnownHost(string host, string accessToken)
      {
         if (!addKnownHost(host, accessToken))
         {
            return false;
         }

         updateHostsDropdownList();
         return true;
      }

      private void onRemoveKnownHost()
      {
         if (listViewKnownHosts.SelectedItems.Count > 0)
         {
            listViewKnownHosts.Items.Remove(listViewKnownHosts.SelectedItems[0]);
         }
         updateHostsDropdownList();
      }

      public string GetCurrentHostName()
      {
         return _workflow.State.HostName;
      }

      public string GetCurrentAccessToken()
      {
         return Tools.GetAccessToken(_workflow.State.HostName, _settings);
      }

      public string GetCurrentProjectName()
      {
         return _workflow.State.Project.Path_With_Namespace;
      }

      public int GetCurrentMergeRequestIId()
      {
         return _workflow.State.MergeRequest.IId;
      }

      private void fillColorSchemesList()
      {
         comboBoxColorSchemes.Items.Clear();
         comboBoxColorSchemes.Items.Add(DefaultColorSchemeName);

         string selectedScheme = null;
         string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
         foreach (string file in files)
         {
            if (file.EndsWith(ColorSchemeFileNamePrefix))
            {
               string scheme = Path.GetFileName(file);
               comboBoxColorSchemes.Items.Add(scheme);
               if (scheme == _settings.ColorSchemeFileName)
               {
                  selectedScheme = scheme;
               }
            }
         }

         if (selectedScheme != null)
         {
            comboBoxColorSchemes.SelectedItem = selectedScheme;
         }
         else
         {
            comboBoxColorSchemes.SelectedIndex = 0;
         }
      }

      private void prepareComboBoxToAsyncLoading(SelectionPreservingComboBox comboBox)
      {
         Debug.Assert(!comboBox.IsDisposed);

         comboBox.DroppedDown = false;
         comboBox.SelectedIndex = -1;
         comboBox.Items.Clear();
         comboBox.DropDownStyle = ComboBoxStyle.DropDown;
         comboBox.Enabled = false;
         comboBox.Text = "Loading...";
      }

      private void fixComboBoxAfterAsyncLoading(SelectionPreservingComboBox comboBox)
      {
         Debug.Assert(!comboBox.IsDisposed);

         comboBox.Enabled = true;
         comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      }

      private void prepareToAsyncGitOperation()
      {
         linkLabelAbortGit.Visible = true;
         buttonDiffTool.Enabled = false;
         buttonDiscussions.Enabled = false;
         comboBoxHost.Enabled = false;
         comboBoxProjects.Enabled = false;
      }

      private void fixAfterAsyncGitOperation()
      {
         linkLabelAbortGit.Visible = false;
         buttonDiffTool.Enabled = true;
         buttonDiscussions.Enabled = true;
         comboBoxHost.Enabled = true;
         comboBoxProjects.Enabled = true;
      }

      private void enableCustomActions(bool flag)
      {
         foreach (Control control in groupBoxActions.Controls)
         {
            control.Enabled = flag;
         }
      }


      private System.Windows.Forms.Timer _timeTrackingTimer = new System.Windows.Forms.Timer
         {
            Interval = timeTrackingTimerInterval
         };

      private bool _exiting = false;
      private bool _requireShowingTooltipOnHideToTray = true;
      private UserDefinedSettings _settings;

      private WorkflowManager _workflowManager;
      private UpdateManager _updateManager;
      private TimeTrackingManager _timeTrackingManager;
      private DiscussionManager _discussionManager;
      private GitClientManager _gitClientManager;

      private Workflow _workflow;
      private WorkflowUpdateChecker _workflowUpdateChecker;
      private CommitChecker _commitChecker;
      private TimeTracker _timeTracker;

      // Arguments passed to the last launched instance of a diff tool
      private struct DiffToolArguments
      {
         public string LeftSHA;
         public string RightSHA;
      }
      DiffToolArguments? _diffToolArgs;

      private ColorScheme _colorScheme = new ColorScheme();

      private struct HostComboBoxItem
      {
         public string Host;
         public string AccessToken;
      }

      private struct VersionComboBoxItem
      {
         public string SHA;
         public string Text;
         public bool IsLatest;
         public DateTime? TimeStamp;

         public override string ToString()
         {
            return Text;
         }

         public VersionComboBoxItem(string sha, string text, DateTime? timeStamp)
         {
            SHA = sha;
            Text = text;
            TimeStamp = timeStamp;
            IsLatest = false;
         }

         public VersionComboBoxItem(Version ver)
            : this(ver.Head_Commit_SHA, ver.Head_Commit_SHA.Substring(0, 10), ver.Created_At)
         {

         }
      }
   }
}

