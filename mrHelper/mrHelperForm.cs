using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using mrCore;
using mrCustomActions;
using mrDiffTool;
using System.Linq;

namespace mrHelperUI
{

   public partial class mrHelperForm : Form, ICommandCallback
   {
      static private string timeTrackingMutexGuid = "{f0b3cbf1-e022-468b-aeb6-db0417a12379}";
      static System.Threading.Mutex timeTrackingMutex =
          new System.Threading.Mutex(false, timeTrackingMutexGuid);

      // TODO Move to resources
      // {
      static private string buttonStartTimerDefaultText = "Start Timer";
      static private string buttonStartTimerTrackingText = "Send Spent";
      static private string labelSpentTimeDefaultText = "00:00:00";
      static private int timeTrackingTimerInterval = 1000; // ms

      static private string errorMessageBoxText = "Error";
      static private string warningMessageBoxText = "Warning";
      static private string informationMessageBoxText = "Information";

      static private string errorTrackedTimeNotSet = "Tracked time was not sent to server";
      static private string errorNoValidRepository = "Cannot launch difftool because there is no valid repository";

      /// <summary>
      /// Tooltip timeout in seconds
      /// </summary>
      private const int notifyTooltipTimeout = 5;
      // }

      public const string GitDiffToolName = "mrhelperdiff";
      private const string CustomActionsFilename = "CustomActions.xml";

      public mrHelperForm()
      {
         InitializeComponent();
      }

      private void addCustomActions()
      {
         if (!File.Exists(CustomActionsFilename))
         {
            // If file doesn't exist the loader throws, leaving the app in an undesirable state
            // Do not try to load custom actions if they don't exist
            return;
         }
         CustomCommandLoader loader = new CustomCommandLoader(this);
         List<ICommand> commands = loader.LoadCommands(CustomActionsFilename);
         int id = 0;
         System.Drawing.Point offSetFromGroupBoxTopLeft = new System.Drawing.Point();
         offSetFromGroupBoxTopLeft.X = 10;
         offSetFromGroupBoxTopLeft.Y = 17;
         System.Drawing.Size typicalSize = new System.Drawing.Size(83, 27);
         foreach (var command in commands)
         {
            string name = command.GetName();
            var button = new System.Windows.Forms.Button();
            button.Name = "customAction" + id;
            button.Location = offSetFromGroupBoxTopLeft;
            button.Size = typicalSize;
            button.Text = name;
            button.UseVisualStyleBackColor = true;
            button.Enabled = false;
            button.TabStop = false;
            button.Click += (x, y) =>
            {
               try
               {
                  command.Run();
               }
               catch (Exception ex)
               {
                  MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
            };
            groupBoxActions.Controls.Add(button);
            offSetFromGroupBoxTopLeft.X += typicalSize.Width + 10;
            id++;
         }
      }

      private void MrHelperForm_Load(object sender, EventArgs e)
      {
         loadSettings();
         try
         {
            addCustomActions();
            onApplicationStarted();
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void MrHelperForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         try
         {
            if (checkBoxMinimizeOnClose.Checked && !_exiting)
            {
               onHideToTray(e);
               return;
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void NotifyIcon_DoubleClick(object sender, EventArgs e)
      {
         try
         {
            onRestoreWindow();
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonDifftool_Click(object sender, EventArgs e)
      {
         try
         {
            onLaunchDiffTool();
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonToggleTimer_Click(object sender, EventArgs e)
      {
         try
         {
            if (_timeTrackingTimer.Enabled)
            {
               onStopTimer(true /* send tracked time to server */);
            }
            else
            {
               onStartTimer();
            }
         }
         catch (Exception ex)
         {
            onStopTimer(false);
            MessageBox.Show(ex.Message + " " + errorTrackedTimeNotSet, errorMessageBoxText,
               MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         try
         {
            onExitingByUser();
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonBrowseLocalGitFolder_Click(object sender, EventArgs e)
      {
         try
         {
            localGitFolderBrowser.SelectedPath = textBoxLocalGitFolder.Text;
            if (localGitFolderBrowser.ShowDialog() == DialogResult.OK)
            {
               onGitFolderSelected();
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ComboBoxHost_SelectedIndexChanged(object sender, EventArgs e)
      {
         try
         {
            updateProjectsDropdownList(getAllProjects());
            _settings.LastSelectedHost = (sender as ComboBox).Text;
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ComboBoxProjects_SelectedIndexChanged(object sender, EventArgs e)
      {
         try
         {
            updateMergeRequestsDropdownList(getAllProjectMergeRequests(comboBoxProjects.Text));
            _settings.LastSelectedProject = (sender as ComboBox).Text;
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ComboBoxFilteredMergeRequests_SelectedIndexChanged(object sender, EventArgs e)
      {
         try
         {
            onMergeRequestSelected();
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonApplyLabels_Click(object sender, EventArgs e)
      {
         try
         {
            updateMergeRequestsDropdownList(getAllProjectMergeRequests(comboBoxProjects.Text));
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ComboBoxLeftVersion_SelectedIndexChanged(object sender, EventArgs e)
      {
         try
         {
            checkComboboxVersionsOrder(true /* I'm left one */);
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ComboBoxRightVersion_SelectedIndexChanged(object sender, EventArgs e)
      {
         try
         {
            checkComboboxVersionsOrder(false /* because I'm the right one */);
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ComboBoxHost_Format(object sender, ListControlConvertEventArgs e)
      {
         formatHostListItem(e);
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
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonAddKnownHost_Click(object sender, EventArgs e)
      {
         try
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
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonRemoveKnownHost_Click(object sender, EventArgs e)
      {
         try
         {
            onRemoveKnownHost();
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void CheckBoxShowPublicOnly_CheckedChanged(object sender, EventArgs e)
      {
         try
         {
            updateProjectsDropdownList(getAllProjects());
            _settings.ShowPublicOnly = (sender as CheckBox).Checked;
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void CheckBoxRequireTimer_CheckedChanged(object sender, EventArgs e)
      {
         _settings.RequireTimeTracking = (sender as CheckBox).Checked;
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

      private void checkComboboxVersionsOrder(bool shouldReorderRightCombobox)
      {
         if (comboBoxLeftVersion.SelectedItem == null || comboBoxRightVersion.SelectedItem == null)
         {
            return;
         }

         // Left combobox cannot select a version older than in right combobox (replicating gitlab web ui behavior)
         VersionComboBoxItem leftItem = (VersionComboBoxItem)(comboBoxLeftVersion.SelectedItem);
         VersionComboBoxItem rightItem = (VersionComboBoxItem)(comboBoxRightVersion.SelectedItem);
         if (!leftItem.TimeStamp.HasValue)
         {
            throw new NotImplementedException("Left combobox cannot have an item w/o timestamp");
         }

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

      private MergeRequest? getSelectedMergeRequest()
      {
         if (comboBoxFilteredMergeRequests.SelectedItem == null)
         {
            return new Nullable<MergeRequest>();
         }
         return ((MergeRequest)comboBoxFilteredMergeRequests.SelectedItem);
      }

      private List<Project> getAllProjects()
      {
         if (comboBoxHost.SelectedItem == null)
         {
            return new List<Project>();
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLabClient client = new GitLabClient(item.Host, item.AccessToken);
         return client.GetAllProjects(checkBoxShowPublicOnly.Checked);
      }

      private List<MergeRequest> getAllProjectMergeRequests(string project)
      {
         if (comboBoxHost.SelectedItem == null)
         {
            return new List<MergeRequest>();
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLabClient client = new GitLabClient(item.Host, item.AccessToken);
         return client.GetAllProjectMergeRequests(project);
      }

      private MergeRequest getMergeRequest()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return new MergeRequest();
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLabClient client = new GitLabClient(item.Host, item.AccessToken);
         return client.GetSingleMergeRequest(comboBoxProjects.Text, mergeRequest.Value.Id);
      }

      private List<mrCore.Version> getVersions()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return new List<mrCore.Version>();
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLabClient client = new GitLabClient(item.Host, item.AccessToken);
         return client.GetMergeRequestVersions(comboBoxProjects.Text, mergeRequest.Value.Id);
      }

      void sendTrackedTimeSpan(TimeSpan span)
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return;
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLabClient client = new GitLabClient(item.Host, item.AccessToken);
         client.AddSpentTimeForMergeRequest(comboBoxProjects.Text, mergeRequest.Value.Id, ref span);
      }

      private void onLaunchDiffTool()
      {
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null)
         {
            return;
         }

         string currentDirectory = Directory.GetCurrentDirectory();
         try
         {
            string project = comboBoxProjects.Text;
            string localGitFolder = textBoxLocalGitFolder.Text;
            HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
            string repository = initializeGitRepository(localGitFolder, item.Host, project);

            Directory.SetCurrentDirectory(repository);
            _difftool = GitClient.DiffTool(GitDiffToolName, getGitTag(true /* left */), getGitTag(false /* right */));
         }
         catch (Exception ex)
         {
            _difftool = null;
            throw ex;
         }
         finally
         {
            Directory.SetCurrentDirectory(currentDirectory);
         }

         updateDetailsSnapshot();
      }

      string initializeGitRepository(string localGitFolder, string host, string projectWithNamespace)
      {
         if (!Directory.Exists(localGitFolder))
         {
            if (MessageBox.Show("Path " + localGitFolder + " does not exist. Do you want to create it?",
               warningMessageBoxText, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
               Directory.CreateDirectory(localGitFolder);
            }
            else
            {
               throw new ApplicationException(errorNoValidRepository);
            }
         }

         string project = projectWithNamespace.Split('/')[1];
         string repository = localGitFolder + "/" + project;
         if (!Directory.Exists(repository))
         {
            if (MessageBox.Show("There is no project " + project + " repository within folder " + localGitFolder +
               ". Do you want to clone git repository?", informationMessageBoxText, MessageBoxButtons.YesNo,
               MessageBoxIcon.Information) == DialogResult.Yes)
            {
               GitClient.CloneRepo(host, projectWithNamespace, repository);
            }
            else
            {
               throw new ApplicationException(errorNoValidRepository);
            }
         }
         else
         {
            string currentDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(repository);
            GitClient.Fetch();
            Directory.SetCurrentDirectory(currentDir);
         }

         return repository;
      }

      private string getGitTag(bool left)
      {
         // swap sides to be consistent with gitlab web ui
         if (!left)
         {
            return comboBoxLeftVersion.SelectedItem != null ?
               ((VersionComboBoxItem)comboBoxLeftVersion.SelectedItem).SHA : "";
         }
         else
         {
            return comboBoxRightVersion.SelectedItem != null ?
               ((VersionComboBoxItem)comboBoxRightVersion.SelectedItem).SHA : "";
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
         if (item.TimeStamp.HasValue)
         {
            e.Value += " (" + item.TimeStamp.Value.ToString("u") + ")";
         }
      }

      private static void formatHostListItem(ListControlConvertEventArgs e)
      {
         HostComboBoxItem item = (HostComboBoxItem)(e.ListItem);
         e.Value = item.Host;
      }

      private void loadConfiguration()
      {
         _loadingConfiguration = true;

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
         checkBoxMinimizeOnClose.Checked = _settings.MinimizeOnClose;

         _loadingConfiguration = false;
      }

      private void saveConfiguration()
      {
         _settings.Update();
      }

      private void updateDetailsSnapshot()
      {
         // delete old snapshot first
         DetailedSnapshotSerializer serializer = new DetailedSnapshotSerializer();
         serializer.PurgeSerialized();

         bool diffToolIsRunning = _difftool != null && !_difftool.HasExited;
         bool allowReportingIssues = !checkBoxRequireTimer.Checked || _timeTrackingTimer.Enabled;
         if (!allowReportingIssues || !diffToolIsRunning)
         {
            return;
         }

         string[] diffArgs = _difftool.StartInfo.Arguments.Split(' ');
         if (diffArgs.Length < 2)
         {
            return;
         }

         string headSHA = diffArgs[diffArgs.Length - 1];
         string baseSHA = diffArgs[diffArgs.Length - 2];

         MergeRequest mergeRequest = getMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null)
         {
            return;
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);

         MergeRequestDetails details;
         details.AccessToken = item.AccessToken;
         details.BaseSHA = baseSHA;                       // Base commit SHA in the source branch
         details.HeadSHA = headSHA;                       // SHA referencing HEAD of this merge request
         details.StartSHA = baseSHA;
         details.Host = item.Host;
         details.Id = mergeRequest.Id;
         details.Project = comboBoxProjects.Text;
         details.TempFolder = textBoxLocalGitFolder.Text;

         serializer.SerializeToDisk(details);
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
            tabPageMR.Select();
         }
         else
         {
            tabPageSettings.Select();
         }
      }

      private void onSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         saveConfiguration();
      }

      private void onApplicationStarted()
      {

         _timeTrackingTimer = new Timer();
         _timeTrackingTimer.Interval = timeTrackingTimerInterval;
         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);

         DiffToolIntegration integration = new DiffToolIntegration(new BC3Tool());
         integration.RegisterInGit(GitDiffToolName);
         integration.RegisterInTool();

         updateHostsDropdownList();
      }

      private void updateHostsDropdownList()
      {
         int? lastSelectedHostIndex = new Nullable<int>();
         comboBoxHost.Items.Clear();
         foreach (ListViewItem item in listViewKnownHosts.Items)
         {
            HostComboBoxItem hostItem = new HostComboBoxItem();
            hostItem.Host = item.Text;
            hostItem.AccessToken = item.SubItems[1].Text;
            comboBoxHost.Items.Add(hostItem);
            if (hostItem.Host == _settings.LastSelectedHost)
            {
               lastSelectedHostIndex = comboBoxHost.Items.Count - 1;
            }
         }
         if (lastSelectedHostIndex.HasValue)
         {
            comboBoxHost.SelectedIndex = lastSelectedHostIndex.Value;
         }
         else if (comboBoxHost.Items.Count > 0)
         {
            comboBoxHost.SelectedIndex = 0;
         }
         else
         {
            comboBoxHost.SelectedIndex = -1;
         }
      }

      private void updateProjectsDropdownList(List<Project> projects)
      {
         // dealing with 'SelectedItem' and not 'SelectedIndex' here because projects combobox id Sorted
         string lastSelectedProjectName = null;
         comboBoxProjects.Items.Clear();
         foreach (var project in projects)
         {
            comboBoxProjects.Items.Add(project.NameWithNamespace);
            if (project.NameWithNamespace == _settings.LastSelectedProject)
            {
               lastSelectedProjectName = project.NameWithNamespace;
            }
         }
         if (lastSelectedProjectName != null)
         {
            comboBoxProjects.SelectedItem = lastSelectedProjectName;
         }
         else if (comboBoxProjects.Items.Count > 0)
         {
            comboBoxProjects.SelectedIndex = 0;
         }
         else
         {
            comboBoxProjects.SelectedIndex = -1;
         }
      }

      private void updateMergeRequestsDropdownList(List<MergeRequest> mergeRequests)
      {
         List<string> labelsRequested = new List<string>();
         if (checkBoxLabels.Checked && textBoxLabels.Text != null)
         {
            foreach (var item in textBoxLabels.Text.Split(','))
            {
               labelsRequested.Add(item.Trim(' '));
            }
         }
         comboBoxFilteredMergeRequests.Items.Clear();
         foreach (var mergeRequest in mergeRequests)
         {
            if (labelsRequested.Count > 0)
            {
               foreach (var label in labelsRequested)
               {
                  if (mergeRequest.Labels.Contains(label))
                  {
                     comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
                  }
               }
            }
            else
            {
               comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
            }
         }
         if (comboBoxFilteredMergeRequests.Items.Count > 0)
         {
            comboBoxFilteredMergeRequests.SelectedIndex = 0;
         }
         else
         {
            comboBoxFilteredMergeRequests.SelectedIndex = -1;

            // call it manually because an event is not called on -1
            onMergeRequestSelected();
         }
      }

      private void onMergeRequestSelected()
      {
         if (_timeTrackingTimer != null && _timeTrackingTimer.Enabled)
         {
            bool sendTrackedTime =
               MessageBox.Show("Send tracked time?", "Selecting another merge request",
                  MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
            onStopTimer(sendTrackedTime);
         }

         if (comboBoxFilteredMergeRequests.SelectedIndex == -1)
         {
            // 1. Update status
            linkLabelConnectedTo.Visible = false;

            // 2. Clean-up textboxes with merge request details
            textBoxMergeRequestName.Text = null;
            richTextBoxMergeRequestDescription.Text = null;

            // 3. Clean-up lists of versions
            comboBoxRightVersion.Items.Clear();
            comboBoxLeftVersion.Items.Clear();

            // 4. Toggle state of buttons
            buttonDiffTool.Enabled = false;
            buttonToggleTimer.Enabled = false;
            foreach (Control control in groupBoxActions.Controls)
            {
               control.Enabled = false;
            }
            return;
         }

         MergeRequest mergeRequest = getMergeRequest();

         // 1. Update status, add merge request url
         linkLabelConnectedTo.Visible = true;
         linkLabelConnectedTo.Text = mergeRequest.WebUrl;

         // 2. Populate edit boxes with merge request details
         textBoxMergeRequestName.Text = mergeRequest.Title;
         richTextBoxMergeRequestDescription.Text = mergeRequest.Description;

         // 3. Add version information to combo boxes
         comboBoxLeftVersion.Items.Clear();
         comboBoxRightVersion.Items.Clear();

         foreach (var version in getVersions())
         {
            VersionComboBoxItem item =
               new VersionComboBoxItem(version.HeadSHA, version.HeadSHA.Substring(0, 10), version.CreatedAt);
            comboBoxLeftVersion.Items.Add(item);
            comboBoxRightVersion.Items.Add(item);
         }

         // Add target branch to the right combo-box
         VersionComboBoxItem targetBranch =
            new VersionComboBoxItem(mergeRequest.BaseSHA, mergeRequest.TargetBranch, null);
         comboBoxRightVersion.Items.Add(targetBranch);

         comboBoxLeftVersion.SelectedIndex = 0;
         comboBoxRightVersion.SelectedIndex = 0;

         // 5. Toggle state of  buttons
         buttonToggleTimer.Enabled = true;
         buttonDiffTool.Enabled = true;
         foreach (Control control in groupBoxActions.Controls)
         {
            control.Enabled = true;
         }
      }

      private void onStartTimer()
      {
         // Try to lock a mutex so that another instance cannot track time simultaneously with this one
         if (!timeTrackingMutex.WaitOne(TimeSpan.Zero))
         {
            // Another instance is currently tracking time
            throw new ApplicationException("Another instance is tracking time");
         }

         // 1. Update button text
         buttonToggleTimer.Text = buttonStartTimerTrackingText;

         // 2. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 3. Store current time
         _lastStartTimeStamp = DateTime.Now;

         // 4. Start timer
         _timeTrackingTimer.Start();

         // 5. Reset and start stopwatch
         _stopWatch.Reset();
         _stopWatch.Start();

         // 6. Update information available to other instances
         updateDetailsSnapshot();
      }

      private void onStopTimer(bool sendTrackedTime)
      {
         // 1. Stop stopwatch
         _stopWatch.Stop();

         // 2. Stop timer
         _timeTrackingTimer.Stop();

         // 3. Update information available to other instances
         updateDetailsSnapshot();

         // 4. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 5. Update button text
         buttonToggleTimer.Text = buttonStartTimerDefaultText;

         // 6. Send tracked time to server
         if (sendTrackedTime)
         {
            sendTrackedTimeSpan(_stopWatch.Elapsed);
         }

         // 7. Allow others to track time
         timeTrackingMutex.ReleaseMutex();
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
         if (_requireShowingTooltip)
         {
            showTooltipBalloon();
         }
         Hide();
         ShowInTaskbar = false;
      }

      private void showTooltipBalloon()
      {
         // TODO: Maybe it's a good idea to save the requireShowingTooltip state
         // so it's only shown once in a lifetime
         notifyIcon.ShowBalloonTip(notifyTooltipTimeout);
         _requireShowingTooltip = false;
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
         if (comboBoxHost.SelectedItem != null)
         {
            return ((HostComboBoxItem)(comboBoxHost.SelectedItem)).Host;
         }
         return null;
      }

      public string GetCurrentAccessToken()
      {
         if (comboBoxHost.SelectedItem != null)
         {
            return ((HostComboBoxItem)(comboBoxHost.SelectedItem)).AccessToken;
         }
         return null;
      }

      public string GetCurrentProjectName()
      {
         if (comboBoxProjects.SelectedItem != null)
         {
            return comboBoxProjects.SelectedItem.ToString();
         }
         return null;
      }

      public int GetCurrentMergeRequestId()
      {
         if (comboBoxFilteredMergeRequests.SelectedItem != null)
         {
            return ((MergeRequest)(comboBoxFilteredMergeRequests.SelectedItem)).Id;
         }
         return 0;
      }

      private DateTime _lastStartTimeStamp;
      private Timer _timeTrackingTimer;

      private bool _exiting = false;
      private bool _loadingConfiguration = false;
      private bool _requireShowingTooltip = true;

      UserDefinedSettings _settings;

      // For accurate time tracking
      Stopwatch _stopWatch = new Stopwatch();

      // Last launched instance of a diff tool
      Process _difftool;

      struct HostComboBoxItem
      {
         public string Host;
         public string AccessToken;
      }

      struct VersionComboBoxItem
      {
         public string SHA;
         public string Text;
         public DateTime? TimeStamp;

         public VersionComboBoxItem(string sha, string text, DateTime? timeStamp)
         {
            SHA = sha;
            Text = text;
            TimeStamp = timeStamp;
         }
      }
   }
}
