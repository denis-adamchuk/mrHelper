﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using GitLabSharp;
using mrHelper.Core;
using mrHelper.CustomActions;
using mrHelper.Common;
using mrHelper.Client;
using Version = GitLabSharp.Version;

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
      private static readonly int mergeRequestCheckTimerInterval = 60000; // ms

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
      private void MrHelperForm_Load(object sender, EventArgs e)
      {
         loadSettings();
         addCustomActions();
         DiffToolIntegrationHelper.IntegrateDiffTool(GitDiffToolName);
         onApplicationStarted();
      }

      async private void MrHelperForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         if (checkBoxMinimizeOnClose.Checked && !_exiting)
         {
            onHideToTray(e);
         }
         else if (_glTaskManager.AreRunningTasks())
         {
            Hide();
            e.Cancel = true;
            try
            {
               await _glTaskManager.WaitAllAsync();
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle(ex, "Cannot complete GitLab task(s)", false);
            }
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
            await onStopTimer(true /* send tracked time to server */);
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
            comboBoxColorSchemes.SelectedIndex = 0; // recursive
         }

         _settings.ColorSchemeFileName = (sender as ComboBox).Text;
      }

      async private void ComboBoxHost_SelectedIndexChanged(object sender, EventArgs e)
      {
         updateGitStatusText(String.Empty);

         _settings.LastSelectedHost = (sender as ComboBox).Text;
         await onChangeHost((HostComboBoxItem)((sender as ComboBox).SelectedItem));
         createHostUpdateChecker();
      }

      async private void ComboBoxProjects_SelectedIndexChanged(object sender, EventArgs e)
      {
         updateGitStatusText(String.Empty);

         _settings.LastSelectedProject = (sender as ComboBox).Text;
         await updateMergeRequestsDropdownList(false);
      }

      async private void ComboBoxFilteredMergeRequests_SelectedIndexChanged(object sender, EventArgs e)
      {
         await onMergeRequestSelected();
         createGitClient();
      }

      async private void ButtonApplyLabels_Click(object sender, EventArgs e)
      {
         _labelsRequested = new List<string>();
         if (checkBoxLabels.Checked && textBoxLabels.Text != null)
         {
            foreach (var item in textBoxLabels.Text.Split(','))
            {
               _labelsRequested.Add(item.Trim(' '));
            }
         }

         await updateMergeRequestsDropdownList(false);
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
         await onChangeHost(_model.State.Access.Host, _model.State.Access.AccessToken);
      }

      async private void CheckBoxRequireTimer_CheckedChanged(object sender, EventArgs e)
      {
         _settings.RequireTimeTracking = (sender as CheckBox).Checked;
         await updateInterprocessSnapshot();
      }

      private void CheckBoxMinimizeOnClose_CheckedChanged(object sender, EventArgs e)
      {
         _settings.MinimizeOnClose = (sender as CheckBox).Checked;
      }

      private void CheckBoxLabels_CheckedChanged(object sender, EventArgs e)
      {
         _settings.CheckedLabelsFilter = (sender as CheckBox).Checked;
      }

      private void TextBoxLabels_Leave(object sender, EventArgs e)
      {
         _settings.LastUsedLabels = textBoxLabels.Text;
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
         _gitClient.CancelOperation();
      }

      private void createGitClient()
      {
         _gitClient = null;

         try
         {
            _gitClient = new GitClient(textBoxLocalGitFolder, GetCurrentProjectName(), GetCurrentHostName(),
               createMergeRequstUpdateChecker());
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot create Git Client", false);
         }
      }

      private void createHostUpdateChecker()
      {
         _hostUpdateChecker = null;

         List<Project> projects = Tools.LoadProjectsFromFile();
         if (projects == null && GetCurrentProjectName() != null)
         {
            projects = new List<Project>();
            projects.Add((Project)(comboBoxProjects.SelectedItem));
         }

         _hostUpdateChecker = new SingleHostUpdateChecker(GetCurrentAccessToken(), GetCurrentHostName(), projects);
      }

      private void createMergeRequstUpdateChecker()
      {
         _singleMRUpdateChecker = new SingleMergeRequestUpdateChecker(GetCurrentAccessToken(), GetCurrentHostName(),
            GetCurrentProjectName(), GetCurrentMergeRequestIId())
      }

      private void addCustomActions()
      {
         List<ICommand> commands = Tools.LoadCustomActions();

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
               labelGitLabStatus.Text = "Command " + name + " is in progress";
               try
               {
                  await command.Run();
               }
               catch (Exception ex) // Whatever happened in Run()
               {
                  ExceptionHandlers.Handle(ex, "Custom action failed");
                  return;
               }
               labelGitLabStatus.Text = "Command " + name + " completed";
            };
            groupBoxActions.Controls.Add(button);
            offSetFromGroupBoxTopLeft.X += typicalSize.Width + 10;
            id++;
         }
      }

      async private Task showDiscussionsFormAsync()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (GetCurrentHostName() == null || GetCurrentProjectName() == null || !mergeRequest.HasValue)
         {
            return;
         }

         AsyncOperationResult result = AsyncOperationResult.Success;
         try
         {
            result = await _gitClient.InitializeAsync();
         }
         catch (Exception ex)
         {
            Debug.Assert(ex is ArgumentException || ex is GitOperationException);
            ExceptionHandlers.Handle(ex, "Cannot initialize/update git repository");
            return;
         }

         if (result == AsyncOperationResult.CancelledByUser)
         {
            if (MessageBox.Show("Without up-to-date git repository, some context code snippets might be missing. "
               + "Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
               return;
            }
         }

         Debug.Assert(result == AsyncOperationResult.Success);

         var mergeRequestDetails = new MergeRequestDetails
         {
            Host = GetCurrentHostName(),
            AccessToken = GetCurrentAccessToken(),
            ProjectId = GetCurrentProjectName(),
            MergeRequestIId = mergeRequest.Value.IId,
            Author = mergeRequest.Value.Author
         };

         User? currentUser = await loadCurrentUserAsync();
         List<Discussion> discussions = await loadDiscussionsAsync();
         if (!currentUser.HasValue || discussions == null)
         {
            return;
         }


         labelGitLabStatus.Text = "Rendering Discussions Form...";
         labelGitLabStatus.Update();

         DiscussionsForm form = null;
         try
         {
            form = new DiscussionsForm(mergeRequestDetails, _gitClient, int.Parse(comboBoxDCDepth.Text),
               _colorScheme, discussions, currentUser.Value);
         }
         catch (NoDiscussionsToShow)
         {
            MessageBox.Show("No discussions to show.", "Information",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot show discussions form", false);
            return;
         }
         finally
         {
            labelGitLabStatus.Text = String.Empty;
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

      /// <summary>
      /// Returns a merge request bound to a dropdown list item.
      /// This MergeRequest object is incomlpete (has some empty field) and when full MR
      /// is needed, loadMergeRequestAsync() method should be called
      /// </summary>
      private MergeRequest? getSelectedMergeRequest()
      {
         if (comboBoxFilteredMergeRequests.SelectedItem == null)
         {
            return new Nullable<MergeRequest>();
         }
         return ((MergeRequest)comboBoxFilteredMergeRequests.SelectedItem);
      }

      async private Task<User?> loadCurrentUserAsync()
      {
         Debug.Assert(GetCurrentHostName() != null);

         GitLab gl = new GitLab(GetCurrentHostName(), GetCurrentAccessToken());
         var task = _glTaskManager.CreateTask(gl.CurrentUser.LoadTaskAsync(), GitLabTaskType.CurrentUser);

         labelGitLabStatus.Text = "Loading current user...";
         try
         {
            var result = await _glTaskManager.RunAsync(task);
            return result.Equals(default(User)) ? null : new Nullable<User>(result);
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load current user from GitLab");
            return null;
         }
         finally
         {
            labelGitLabStatus.Text = String.Empty;
         }
      }

      async private Task<List<Discussion>> loadDiscussionsAsync()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (!mergeRequest.HasValue)
         {
            return null;
         }

         Debug.Assert(GetCurrentHostName() != null);
         Debug.Assert(GetCurrentProjectName() != null);

         GitLab gl = new GitLab(GetCurrentHostName(), GetCurrentAccessToken());
         var task = _glTaskManager.CreateTask(gl.Projects.Get(GetCurrentProjectName()).MergeRequests.
            Get(mergeRequest.Value.IId).Discussions.LoadAllTaskAsync(), GitLabTaskType.Discussions);

         labelGitLabStatus.Text = "Loading discussions...";
         try
         {
            return await _glTaskManager.RunAsync(task);
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load discussions from GitLab");
            return null;
         }
         finally
         {
            labelGitLabStatus.Text = String.Empty;
         }
      }

      async private void sendTrackedTimeSpan(TimeSpan span)
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (GetCurrentHostName() == null || GetCurrentProjectName() == null || !mergeRequest.HasValue
            || span.TotalSeconds < 1)
         {
            return;
         }

         GitLab gl = new GitLab(GetCurrentHostName(), GetCurrentAccessToken());
         labelGitLabStatus.Text = "Sending tracked time...";
         try
         {
            await gl.Projects.Get(GetCurrentProjectName()).MergeRequests.Get(mergeRequest.Value.IId).AddSpentTimeAsync(
               new AddSpentTimeParameters
               {
                  Span = span
               });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot send tracked time to GitLab");
         }
         string duration = span.ToString("hh") + "h " + span.ToString("mm") + "m " + span.ToString("ss") + "s";
         labelGitLabStatus.Text = String.Format("Tracked time {0} sent successfully", duration);
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
         await updateInterprocessSnapshot(); // to purge serialized snapshot

         AsyncOperationResult result = AsyncOperationResult.Success;
         try
         {
            result = await _gitClient.InitializeAsync();
         }
         catch (Exception ex)
         {
            Debug.Assert(ex is ArgumentException || ex is GitOperationException);
            ExceptionHandlers.Handle(ex, "Cannot initialize/update git repository");
            return;
         }

         if (result == AsyncOperationResult.CancelledByUser)
         {
            // User declined to create a repository
            MessageBox.Show("Cannot launch a diff tool without up-to-date git repository", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
         }

         Debug.Assert(result == AsyncOperationResult.Success);

         string leftSHA = getGitTag(true /* left */);
         string rightSHA = getGitTag(false /* right */);

         buttonDiffTool.Enabled = false;
         buttonDiscussions.Enabled = false;
         _gitClient.DiffToolAsync(GitDiffToolName, leftSHA, rightSHA);

         _diffToolArgs = new DiffToolArguments
         {
            LeftSHA = leftSHA,
            RightSHA = rightSHA
         };

         await updateInterprocessSnapshot();
      }

      async private void onMergeRequestCheckTimer(object sender, EventArgs e)
      {
         if (labelAutoUpdate.Visible)
         {
            return; // in auto-update already
         }

         labelAutoUpdate.Visible = true;
         labelAutoUpdate.Update();

         MergeRequestTimerUpdates updates = await collectMergeRequestUpdates();
         if (updates.NewMergeRequests.Count > 0 || updates.UpdatedMergeRequests.Count > 0)
         {
            notifyOnMergeRequestUpdates(updates);

            // This will automatically update version list for the currently selected MR (if there are new).
            // This will also remove merged merge requests from the list.
            await updateMergeRequestsDropdownList(true);
         }
         labelAutoUpdate.Visible = false;
      }

      async private Task<MergeRequestUpdates> collectMergeRequestUpdates()
      {
         MergeRequestUpdates updates = _hostUpdateChecker.GetUpdates(_lastCheckTime);
         _lastCheckTime = DateTime.Now;
         return updates;
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

      async private Task updateInterprocessSnapshot()
      {
         // first of all, delete old snapshot
         InterprocessSnapshotSerializer serializer = new InterprocessSnapshotSerializer();
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

         InterprocessSnapshot snapshot;
         snapshot.AccessToken = GetCurrentAccessToken();
         snapshot.Refs.LeftSHA = _diffToolArgs.Value.LeftSHA;     // Base commit SHA in the source branch
         snapshot.Refs.RightSHA = _diffToolArgs.Value.RightSHA;   // SHA referencing HEAD of this merge request
         snapshot.Host = GetCurrentHostName();
         snapshot.MergeRequestId = mergeRequest.Value.IId;
         snapshot.Project = GetCurrentProjectName();
         snapshot.TempFolder = textBoxLocalGitFolder.Text;

         serializer.SerializeToDisk(snapshot);
      }

      private void loadSettings()
      {
         _settings = new UserDefinedSettings();
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

      private void onApplicationStarted()
      {
         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);
         _mergeRequestCheckTimer.Tick += new System.EventHandler(onMergeRequestCheckTimer);
         _mergeRequestCheckTimer.Start();

         updateHostsDropdownList();
      }

      private void updateHostsDropdownList()
      {
         comboBoxHost.SelectedIndex = -1;
         comboBoxHost.Items.Clear();

         int? lastSelectedHostIndex = new Nullable<int>();
         foreach (ListViewItem item in listViewKnownHosts.Items)
         {
            HostComboBoxItem hostItem = new HostComboBoxItem
            {
               Host = "https://" + item.Text,
               AccessToken = item.SubItems[1].Text
            };
            comboBoxHost.Items.Add(hostItem);
            if (hostItem.Host == _settings.LastSelectedHost)
            {
               lastSelectedHostIndex = comboBoxHost.Items.Count - 1;
            }
         }

         if (comboBoxHost.Items.Count == 0)
         {
            return;
         }

         if (lastSelectedHostIndex.HasValue)
         {
            comboBoxHost.SelectedIndex = lastSelectedHostIndex.Value;
         }
         else
         {
            comboBoxHost.SelectedIndex = 0;
         }
      }

      async private Task onChangeHost(string hostName, string accessToken)
      {
         Debug.WriteLine("Update projects dropdown list");

         Debug.WriteLine("Disable projects combo box");
         prepareComboBoxToAsyncLoading(comboBoxProjects);

         labelGitLabStatus.Text = "Loading projects...";
         List<Project> projects = await _model.SwitchHostAsync(;
         labelGitLabStatus.Text = String.Empty;

         Debug.WriteLine("Enable projects combo box");
         fixComboBoxAfterAsyncLoading(comboBoxProjects);
      }

      async private Task updateMergeRequestsDropdownList(bool keepPosition)
      {
         Debug.WriteLine("Update merge requests dropdown list");

         keepPosition &= (comboBoxFilteredMergeRequests.SelectedItem != null);
         MergeRequest? currentItem =
            keepPosition ? (MergeRequest)comboBoxFilteredMergeRequests.SelectedItem : new Nullable<MergeRequest>();

         Debug.WriteLine("Disable merge requests combo box");
         prepareComboBoxToAsyncLoading(comboBoxFilteredMergeRequests);

         List<MergeRequest> mergeRequests = await loadAllProjectMergeRequestsAsync();

         Debug.WriteLine("Enable merge requests combo box");
         fixComboBoxAfterAsyncLoading(comboBoxFilteredMergeRequests);

         if (mergeRequests == null)
         {
            return;
         }

         foreach (var mergeRequest in mergeRequests)
         {
            if (_labelsRequested.Intersect(mergeRequest.Labels).Count > 0)
            {
               comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
            }
         }

         if (comboBoxFilteredMergeRequests.Items.Count == 0)
         {
            return;
         }

         if (currentItem.HasValue)
         {
            for (int iItem = 0; iItem < comboBoxFilteredMergeRequests.Items.Count; ++iItem)
            {
               MergeRequest mr = (MergeRequest)(comboBoxFilteredMergeRequests.Items[iItem]);
               if (mr.Id == currentItem.Value.Id)
               {
                  comboBoxFilteredMergeRequests.SelectedIndex = iItem;
                  break;
               }
            }

            if (comboBoxFilteredMergeRequests.SelectedIndex  == -1)
            {
               comboBoxFilteredMergeRequests.SelectedIndex = 0;
            }
         }
         else
         {
            comboBoxFilteredMergeRequests.SelectedIndex = 0;
         }
      }

      async private Task onMergeRequestSelected()
      {
         Debug.WriteLine("Let's handle merge request selection");

         Debug.WriteLine("Disable UI controls");
         onMergeRequestLoaded(null);
         addVersionsToComboBoxes(null, null, null);
         if (getSelectedMergeRequest() == null)
         {
            Debug.WriteLine("Switched to a missing merge request");
            return;
         }

         textBoxMergeRequestName.Text = "Loading...";
         richTextBoxMergeRequestDescription.Text = "Loading...";

         Debug.WriteLine("Let's load merge request with Id " + (getSelectedMergeRequest()?.IId.ToString() ?? ""));
         MergeRequest? mergeRequest = await loadMergeRequestAsync();
         Debug.WriteLine("Finished loading MR. Current selected MR is " + (getSelectedMergeRequest()?.IId.ToString() ?? ""));

         Debug.WriteLine("Enable UI controls according to selected MR");
         onMergeRequestLoaded(getSelectedMergeRequest());

         if (!mergeRequest.HasValue)
         {
            Debug.WriteLine("MR which has just loaded is cancelled");
            return;
         }

         prepareComboBoxToAsyncLoading(comboBoxLeftVersion);
         prepareComboBoxToAsyncLoading(comboBoxRightVersion);

         Debug.WriteLine("Let's load versions for MR " + getSelectedMergeRequest().Value.IId);
         List<Version> versions = await loadVersionsAsync();
         Debug.WriteLine("Finished loading MR Versions. Current selected MR is " + (getSelectedMergeRequest()?.IId.ToString() ?? ""));

         fixComboBoxAfterAsyncLoading(comboBoxLeftVersion);
         fixComboBoxAfterAsyncLoading(comboBoxRightVersion);

         Debug.WriteLine("Adding versions");
         addVersionsToComboBoxes(versions, mergeRequest.Value.Diff_Refs.Base_SHA, mergeRequest.Value.Target_Branch);
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

         comboBoxLeftVersion.SelectedIndex = 0;
         comboBoxRightVersion.SelectedIndex = 0;
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
         _stopWatch.Reset();
         _stopWatch.Start();

         // 5. Update information available to other instances
         await updateInterprocessSnapshot();
      }

      async private Task onStopTimer(bool sendTrackedTime)
      {
         // 1. Stop stopwatch
         _stopWatch.Stop();

         // 2. Stop timer
         _timeTrackingTimer.Stop();

         // 3. Update information available to other instances
         await updateInterprocessSnapshot();

         // 4. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 5. Update button text
         buttonToggleTimer.Text = buttonStartTimerDefaultText;

         // 6. Send tracked time to server
         if (sendTrackedTime)
         {
            sendTrackedTimeSpan(_stopWatch.Elapsed);
         }
      }

      private void onTimer(object sender, EventArgs e)
      {
         labelSpentTime.Text = _stopWatch.Elapsed.ToString(@"hh\:mm\:ss");
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
         createGitClient();
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
         return _model.GetState().HostName;
      }

      public string GetCurrentAccessToken()
      {
         return _model.GetState().AccessToken;
      }

      public string GetCurrentProjectName()
      {
         return _model.GetState().SelectedProject.Path_With_Namespace;
      }

      public int GetCurrentMergeRequestIId()
      {
         return _model.GetState().SelectedMergeRequest.IId;
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
      private GitClient _gitClient = null;
      private SingleHostUpdateChecker _hostUpdateChecker = null;
      private IUpdateChecker _singleMRUpdateChecker = null;
      private List<string> _labelsRequested = null;

      // For accurate time tracking
      private readonly Stopwatch _stopWatch = new Stopwatch();

      // Arguments passed to the last launched instance of a diff tool
      private struct DiffToolArguments
      {
         public string LeftSHA;
         public string RightSHA;
      }
      DiffToolArguments? _diffToolArgs;

      private ColorScheme _colorScheme = new ColorScheme();

      private System.Windows.Forms.Timer _mergeRequestCheckTimer = new System.Windows.Forms.Timer
         {
            Interval = mergeRequestCheckTimerInterval
         };
      private DateTime _lastCheckTime = DateTime.Now;

      private struct HostComboBoxItem
      {
         public string Host;
         public string AccessToken;
      }

      private GitLabTaskManager _glTaskManager = new GitLabTaskManager();

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

