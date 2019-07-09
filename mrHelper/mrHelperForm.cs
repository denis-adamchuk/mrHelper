using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using mrCore;
using mrCustomActions;
using mrDiffTool;

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
      // }

      public const string GitDiffToolName = "mrhelperdiff";
      private const string CustomActionsFileName = "CustomActions.xml";
      
      public mrHelperForm()
      {
         InitializeComponent();
      }

      private void addCustomActions()
      {
         CustomCommandLoader loader = new CustomCommandLoader(this);
         List<ICommand> commands = loader.LoadCommands(CustomActionsFileName);
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
            if (!_exiting)
            {
               onHideToTray(e);
               return;
            }
            saveConfiguration();
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
            _gitRepository = null;
            updateProjectsDropdownList(getAllProjects());
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
            _gitRepository = null;
            updateMergeRequestsDropdownList(getAllProjectMergeRequests(comboBoxProjects.Text));
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
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonDiscussions_Click(object sender, EventArgs e)
      {
         try
         {
            onShowDiscussionsForm();
            return;
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void onShowDiscussionsForm()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return;
         }

         if (_gitRepository == null)
         {
            _gitRepository = initializeGitRepository();
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         var form = new DiscussionsForm(item.Host, item.AccessToken, comboBoxProjects.Text, mergeRequest.Value.Id,
            _gitRepository);
         form.Show(this);
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
         if (_gitRepository == null)
         {
            _gitRepository = initializeGitRepository();
         }

         _difftool = null; // in case the next line throws
         _difftool = _gitRepository.DiffTool(GitDiffToolName, getGitTag(true /* left */), getGitTag(false /* right */));
         updateInterprocessSnapshot();
      }

      GitRepository initializeGitRepository()
      {
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null)
         {
            return null;
         }

         string projectWithNamespace = comboBoxProjects.Text;
         string localGitFolder = textBoxLocalGitFolder.Text;
         string host = ((HostComboBoxItem)(comboBoxHost.SelectedItem)).Host;

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
         string path = Path.Combine(localGitFolder, project);
         if (!Directory.Exists(path))
         {
            if (MessageBox.Show("There is no project " + project + " repository within folder " + localGitFolder +
               ". Do you want to clone git repository?", informationMessageBoxText, MessageBoxButtons.YesNo,
               MessageBoxIcon.Information) == DialogResult.Yes)
            {
               return GitRepository.CreateByClone(host, projectWithNamespace, localGitFolder);
            }
            else
            {
               throw new ApplicationException(errorNoValidRepository);
            }
         }

         GitRepository gitRepository = new GitRepository(path);
         gitRepository.Fetch();

         return gitRepository;
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
            e.Value += " (" + item.TimeStamp.Value.ToLocalTime().ToString("g") + ")";
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
         checkBoxRequireTimer.Checked = _settings.RequireTimeTracking == "true";
         checkBoxLabels.Checked = _settings.CheckedLabelsFilter == "true";
         textBoxLabels.Text = _settings.LastUsedLabels;
         checkBoxShowPublicOnly.Checked = _settings.ShowPublicOnly == "true";

         _loadingConfiguration = false;
      }

      private void saveConfiguration()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         List<string> hosts = new List<string>();
         List<string> accessTokens = new List<string>();
         foreach (ListViewItem hostListViewItem in listViewKnownHosts.Items)
         {
            hosts.Add(hostListViewItem.Text);
            accessTokens.Add(hostListViewItem.SubItems[1].Text);
         }
         _settings.KnownHosts = hosts;
         _settings.KnownAccessTokens = accessTokens;
         _settings.LocalGitFolder = textBoxLocalGitFolder.Text;
         _settings.RequireTimeTracking = checkBoxRequireTimer.Checked ? "true" : "false";
         _settings.CheckedLabelsFilter = checkBoxLabels.Checked ? "true" : "false";
         _settings.LastUsedLabels = textBoxLabels.Text;
         _settings.LastSelectedHost = comboBoxHost.Text;
         _settings.LastSelectedProject = comboBoxProjects.Text;
         _settings.ShowPublicOnly = checkBoxShowPublicOnly.Checked ? "true" : "false";
         _settings.Update();
      }

      private void updateInterprocessSnapshot()
      {
         // delete old snapshot first
         InterprocessSnapshotSerializer serializer = new InterprocessSnapshotSerializer();
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

         InterprocessSnapshot snapshot;
         snapshot.AccessToken = item.AccessToken;
         snapshot.Refs.BaseSHA = baseSHA;                       // Base commit SHA in the source branch
         snapshot.Refs.HeadSHA = headSHA;                       // SHA referencing HEAD of this merge request
         snapshot.Refs.StartSHA = baseSHA; 
         snapshot.Host = item.Host;
         snapshot.Id = mergeRequest.Id;
         snapshot.Project = comboBoxProjects.Text;
         snapshot.TempFolder = textBoxLocalGitFolder.Text;
         
         serializer.SerializeToDisk(snapshot);
      }

      private void onApplicationStarted()
      {
         _settings = new UserDefinedSettings();
         loadConfiguration();
         
         _timeTrackingTimer = new Timer();
         _timeTrackingTimer.Interval = timeTrackingTimerInterval;
         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);

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
            buttonDiscussions.Enabled = false;
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
               new VersionComboBoxItem(version.Refs.HeadSHA, version.Refs.HeadSHA.Substring(0, 10), version.CreatedAt);
            comboBoxLeftVersion.Items.Add(item);
            comboBoxRightVersion.Items.Add(item);
         }

         // Add target branch to the right combo-box
         VersionComboBoxItem targetBranch =
            new VersionComboBoxItem(mergeRequest.Refs.BaseSHA, mergeRequest.TargetBranch, null);
         comboBoxRightVersion.Items.Add(targetBranch);

         comboBoxLeftVersion.SelectedIndex = 0;
         comboBoxRightVersion.SelectedIndex = 0;

         // 5. Toggle state of  buttons
         buttonToggleTimer.Enabled = true;
         buttonDiffTool.Enabled = true;
         buttonDiscussions.Enabled = true;
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

         // 5. Update information available to other instances
         updateInterprocessSnapshot();
      }

      private void onStopTimer(bool sendTrackedTime)
      {
         // 1. Stop timer
         _timeTrackingTimer.Stop();

         // 2. Update information available to other instances
         updateInterprocessSnapshot();

         // 3. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 4. Update button text
         buttonToggleTimer.Text = buttonStartTimerDefaultText;

         // 5. Send tracked time to server
         if (sendTrackedTime)
         {
            sendTrackedTimeSpan(DateTime.Now - _lastStartTimeStamp);
         }

         // 6. Allow others to track time
         timeTrackingMutex.ReleaseMutex();
      }

      private void onTimer(object sender, EventArgs e)
      {
         // TODO Handle overflow
         var span = DateTime.Now - _lastStartTimeStamp;
         labelSpentTime.Text = span.ToString(@"hh\:mm\:ss");
      }

      private void onExitingByUser()
      {
         _exiting = true;
         this.Close();
      }

      private void onHideToTray(FormClosingEventArgs e)
      {
         e.Cancel = true;
         Hide();
         ShowInTaskbar = false;
      }

      private void onRestoreWindow()
      {
         ShowInTaskbar = true;
         Show();
      }

      private void onGitFolderSelected()
      {
         textBoxLocalGitFolder.Text = localGitFolderBrowser.SelectedPath;
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

      UserDefinedSettings _settings;
      GitRepository _gitRepository;

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
