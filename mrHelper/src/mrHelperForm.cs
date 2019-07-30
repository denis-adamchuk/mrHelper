using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using GitLabSharp;
using mrCore;
using mrCustomActions;
using mrDiffTool;
using Version = GitLabSharp.Version;

namespace mrHelperUI
{

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
      private const string CustomActionsFileName = "CustomActions.xml";
      private const string ProjectListFileName = "projects.json";
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
         integrateDiffTool();
         onApplicationStarted();
      }

      private void MrHelperForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         if (checkBoxMinimizeOnClose.Checked && !_exiting)
         {
            onHideToTray(e);
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

      private void ButtonToggleTimer_Click(object sender, EventArgs e)
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
         catch (Exception ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot change color scheme");
            comboBoxColorSchemes.SelectedIndex = 0; // recursive
         }

         _settings.ColorSchemeFileName = (sender as ComboBox).Text;
      }

      private void ComboBoxHost_SelectedIndexChanged(object sender, EventArgs e)
      {
         _gitRepository = null;
         updateProjectsDropdownList(getAllProjects());
         _settings.LastSelectedHost = (sender as ComboBox).Text;
      }

      private void ComboBoxProjects_SelectedIndexChanged(object sender, EventArgs e)
      {
         _gitRepository = null;
         updateMergeRequestsDropdownList(getAllProjectMergeRequests(comboBoxProjects.Text), false);
         _settings.LastSelectedProject = (sender as ComboBox).Text;
      }

      private void ComboBoxFilteredMergeRequests_SelectedIndexChanged(object sender, EventArgs e)
      {
         onMergeRequestSelected();
      }

      private void ButtonApplyLabels_Click(object sender, EventArgs e)
      {
         updateMergeRequestsDropdownList(getAllProjectMergeRequests(comboBoxProjects.Text), false);
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
         catch (Exception ex)
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

      private void CheckBoxShowPublicOnly_CheckedChanged(object sender, EventArgs e)
      {
         updateProjectsDropdownList(getAllProjects());
         _settings.ShowPublicOnly = (sender as CheckBox).Checked;
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
      private void comboBoxDCDepth_SelectedIndexChanged(object sender, EventArgs e)
      {
         _settings.DiffContextDepth = (sender as ComboBox).Text;
      }

      private void ButtonDiscussions_Click(object sender, EventArgs e)
      {
         onShowDiscussionsForm();
      }

      private void addCustomActions()
      {
         List<ICommand> commands = null;
         CustomCommandLoader loader = new CustomCommandLoader(this);
         try
         {
            commands = loader.LoadCommands(CustomActionsFileName);
         }
         catch (ArgumentException ex)
         {
            // If file doesn't exist the loader throws, leaving the app in an undesirable state.
            // Do not try to load custom actions if they don't exist.
            ExceptionHandlers.Handle(ex, "Cannot load custom actions");
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
            button.Click += (x, y) =>
            {
               try
               {
                  command.Run();
               }
               catch (Exception ex)
               {
                  ExceptionHandlers.Handle(ex, "Custom action failed");
               }
            };
            groupBoxActions.Controls.Add(button);
            offSetFromGroupBoxTopLeft.X += typicalSize.Width + 10;
            id++;
         }
      }

      private void onShowDiscussionsForm()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return;
         }

         updateGitRepository();

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         var mergeRequestDetails = new MergeRequestDetails
         {
            Host = item.Host,
            AccessToken = item.AccessToken,
            ProjectId = comboBoxProjects.Text,
            MergeRequestIId = mergeRequest.Value.IId,
            Author = mergeRequest.Value.Author
         };
         var form = new DiscussionsForm(mergeRequestDetails, _gitRepository, int.Parse(comboBoxDCDepth.Text),
            _colorScheme);
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
      /// is needed, getMergeRequest() method should be called
      /// </summary>
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
            return null;
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         List<Project> projects = null;

         // Check if file exists. If it does not, it is not an error.
         if (File.Exists(ProjectListFileName))
         {
            try
            {
               projects = loadProjectsFromFile(item.Host, ProjectListFileName);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle(ex, "Cannot load projects from file");
            }
         }

         if (projects != null && projects.Count != 0)
         {
            return projects;
         }

         GitLab gl = new GitLab(item.Host, item.AccessToken);
         try
         {
            projects = gl.Projects.LoadAll(new ProjectsFilter { PublicOnly = checkBoxShowPublicOnly.Checked });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load projects from GitLab");
         }

         return projects;
      }

      private List<MergeRequest> getAllProjectMergeRequests(string project)
      {
         if (comboBoxHost.SelectedItem == null)
         {
            return null;
         }

         List<MergeRequest> mergeRequests = null;
         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLab gl = new GitLab(item.Host, item.AccessToken);
         try
         {
            mergeRequests = gl.Projects.Get(comboBoxProjects.Text).MergeRequests.LoadAll(new MergeRequestsFilter());
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge requests from GitLab");
         }

         return mergeRequests;
      }

      /// <summary>
      /// Unlike getSelectedMergeRequest(), this method returns a MergeRequest object with all fields.
      /// </summary>
      private MergeRequest? getMergeRequest()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return null;
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLab gl = new GitLab(item.Host, item.AccessToken);
         try
         {
            mergeRequest = gl.Projects.Get(comboBoxProjects.Text).MergeRequests.Get(mergeRequest.Value.IId).Load();
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge request from GitLab");
         }

         return mergeRequest;
      }

      private List<Version> getVersions()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return null;
         }

         List<Version> versions = null;
         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLab gl = new GitLab(item.Host, item.AccessToken);
         try
         {
            versions = gl.Projects.Get(comboBoxProjects.Text).MergeRequests.Get(mergeRequest.Value.IId).Versions.LoadAll();
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot load merge request versions from GitLab");
         }

         return versions;
      }

      private void sendTrackedTimeSpan(TimeSpan span)
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return;
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLab gl = new GitLab(item.Host, item.AccessToken);
         try
         {
            gl.Projects.Get(comboBoxProjects.Text).MergeRequests.Get(mergeRequest.Value.IId).AddSpentTime(
               new AddSpentTimeParameters { Span = span });
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot send tracked time to GitLab");
         }
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
         catch (Exception)
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

      /// <summary>
      /// Throws GitOperationException in case of problems with git.
      /// </summary>
      private void onLaunchDiffTool()
      {
         _diffToolArgs = null;

         string leftSHA = getGitTag(true /* left */);
         string rightSHA = getGitTag(false /* right */);

         updateGitRepository();
         if (_gitRepository == null)
         {
            // User declined to create a repository
            MessageBox.Show("Cannot launch a diff tool without git repository", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
         }

         try
         {
            _gitRepository.DiffTool(GitDiffToolName, leftSHA, rightSHA);
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot launch diff tool");

            updateInterprocessSnapshot(); // to purge serialized snapshot
            return;
         }

         _diffToolArgs = new DiffToolArguments
         {
            LeftSHA = leftSHA,
            RightSHA = rightSHA
         };

         updateInterprocessSnapshot();
      }

      /// <summary>
      /// Checks if there is a version in GitLab which is newer than latest Git Repository update.
      /// Returns 'true' if there is a newer version.
      /// </summary>
      private bool checkForRepositoryUpdates()
      {
         Debug.Assert(_gitRepository != null);

         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return false;
         }

         List<Version> versions = null;
         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLab gl = new GitLab(item.Host, item.AccessToken);
         try
         {
            versions = gl.Projects.Get(comboBoxProjects.Text).MergeRequests.Get(mergeRequest.Value.IId).Versions.LoadAll();
         }
         catch (GitLabRequestException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot check GitLab for updates");
         }

         return versions != null && versions.Count > 0
            && versions[0].Created_At.ToLocalTime() > _gitRepository.LastUpdateTime;
      }

      private void onMergeRequestCheckTimer(object sender, EventArgs e)
      {
         List<MergeRequest> newMergeRequests = new List<MergeRequest>();
         List<MergeRequest> updatedMergeRequests = new List<MergeRequest>();
         collectMergeRequestUpdates(out newMergeRequests, out updatedMergeRequests);
         notifyOnMergeRequestUpdates(newMergeRequests, updatedMergeRequests);

         // This will automatically update version list for the currently selected MR (if there are new).
         // This will also remove merged merge requests from the list.
         updateMergeRequestsDropdownList(getAllProjectMergeRequests(comboBoxProjects.Text), true);
      }

      /// <summary>
      /// Collects requests that have been created or updated later than _lastCheckTime.
      /// By 'updated' we mean that 'merge request has a version with a timestamp later than ...'.
      /// Checks all the hosts.
      /// Checks all the projects if project filtering is not used, otherwise checks only filtered project.
      /// Includes only those merge requests that match Labels filters.
      /// </summary>
      private void collectMergeRequestUpdates(out List<MergeRequest> newMergeRequests, out List<MergeRequest> updatedMergeRequests)
      {
         newMergeRequests = new List<MergeRequest>();
         updatedMergeRequests = new List<MergeRequest>();

         if (comboBoxHost.SelectedItem == null)
         {
            return;
         }

         HostComboBoxItem hostItem = (HostComboBoxItem)(comboBoxHost.SelectedItem); 
         GitLab gl = new GitLab(hostItem.Host, hostItem.AccessToken);

         List<Project> projectsToCheck = new List<Project>();

         // If project list is filtered, check all filtered, otherwise check the selected only
         if (File.Exists(ProjectListFileName))
         {
            foreach (var itemProject in comboBoxProjects.Items)
            {
               projectsToCheck.Add((Project)(itemProject));
            }
         }
         else if (comboBoxProjects.SelectedItem != null)
         {
            projectsToCheck.Add((Project)(comboBoxProjects.SelectedItem));
         }

         foreach (var project in projectsToCheck)
         {
            List<MergeRequest> mergeRequests = new List<MergeRequest>();
            try
            {
               mergeRequests = gl.Projects.Get(project.Path_With_Namespace).MergeRequests.LoadAll(new MergeRequestsFilter());
            }
            catch (GitLabRequestException ex)
            {
               ExceptionHandlers.Handle(ex, "Cannot load merge requests on auto-update", false);
               continue;
            }

            foreach (var mergeRequest in mergeRequests)
            {
               if (!doesMergeRequestMatchLabels(mergeRequest))
               {
                  continue;
               }

               if (mergeRequest.Created_At.ToLocalTime() > _lastCheckTime)
               {
                  newMergeRequests.Add(mergeRequest);
               }
               else if (mergeRequest.Updated_At.ToLocalTime() > _lastCheckTime)
               {
                  List<Version> versions = new List<Version>();
                  try
                  {
                     versions = gl.Projects.Get(project.Path_With_Namespace).MergeRequests.
                        Get(mergeRequest.IId).Versions.LoadAll();
                  }
                  catch (GitLabRequestException ex)
                  {
                     ExceptionHandlers.Handle(ex, "Cannot load merge request versions on auto-update", false);
                     continue;
                  }

                  if (versions.Count == 0)
                  {
                     continue;
                  }

                  Version latestVersion = versions[0];
                  if (latestVersion.Created_At.ToLocalTime() > _lastCheckTime)
                  {
                     updatedMergeRequests.Add(mergeRequest);
                  }
               }
            }
         }

         _lastCheckTime = DateTime.Now;
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

      private void notifyOnMergeRequestUpdates(List<MergeRequest> newMergeRequests, List<MergeRequest> updatedMergeRequests)
      {
         foreach (MergeRequest mergeRequest in newMergeRequests)
         {
            notifyOnMergeRequestEvent(mergeRequest, "New merge request");
         }

         foreach (MergeRequest mergeRequest in updatedMergeRequests)
         {
            notifyOnMergeRequestEvent(mergeRequest, "New commit in merge request");
         }
      }

      private void updateGitRepository()
      {
         if (_gitRepository != null)
         {
            if (checkForRepositoryUpdates())
            {
               try
               {
                  _gitRepository.Fetch();
               }
               catch (GitOperationException ex)
               {
                  ExceptionHandlers.Handle(ex, "Cannot update git repository");
               }
            }
            return;
         }

         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null)
         {
            return;
         }

         string projectWithNamespace = comboBoxProjects.Text;
         string localGitFolder = textBoxLocalGitFolder.Text;
         string host = ((HostComboBoxItem)(comboBoxHost.SelectedItem)).Host;

         if (!Directory.Exists(localGitFolder))
         {
            if (MessageBox.Show("Path " + localGitFolder + " does not exist. Do you want to create it?",
               "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
               Directory.CreateDirectory(localGitFolder);
            }
            else
            {
               return;
            }
         }

         bool clone = false;

         string project = projectWithNamespace.Split('/')[1];
         string path = Path.Combine(localGitFolder, project);
         if (!Directory.Exists(path))
         {
            if (MessageBox.Show("There is no project \"" + project + "\" repository in " + localGitFolder +
               ". Do you want to clone git repository?", "Information", MessageBoxButtons.YesNo,
               MessageBoxIcon.Information) != DialogResult.Yes)
            {
               return;
            }
            clone = true;
         }

         try
         {
            _gitRepository = clone ? new GitRepository(host, projectWithNamespace, path) : new GitRepository(path);
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot initialize git repository");
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

      private static void formatHostListItem(ListControlConvertEventArgs e)
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

      private void updateInterprocessSnapshot()
      {
         // delete old snapshot first
         InterprocessSnapshotSerializer serializer = new InterprocessSnapshotSerializer();
         serializer.PurgeSerialized();

         bool allowReportingIssues = !checkBoxRequireTimer.Checked || _timeTrackingTimer.Enabled;
         if (!allowReportingIssues || !_diffToolArgs.HasValue
          || comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null)
         {
            return;
         }

         MergeRequest? mergeRequest = getMergeRequest();
         if (!mergeRequest.HasValue)
         {
            return;
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);

         InterprocessSnapshot snapshot;
         snapshot.AccessToken = item.AccessToken;
         snapshot.Refs.LeftSHA = _diffToolArgs.Value.LeftSHA;     // Base commit SHA in the source branch
         snapshot.Refs.RightSHA = _diffToolArgs.Value.RightSHA;   // SHA referencing HEAD of this merge request
         snapshot.Host = item.Host;
         snapshot.MergeRequestId = mergeRequest.Value.IId;
         snapshot.Project = comboBoxProjects.Text;
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
         _timeTrackingTimer = new Timer
         {
            Interval = timeTrackingTimerInterval
         };
         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);

         _mergeRequestCheckTimer = new Timer
         {
            Interval = mergeRequestCheckTimerInterval
         };
         _mergeRequestCheckTimer.Tick += new System.EventHandler(onMergeRequestCheckTimer);
         _mergeRequestCheckTimer.Start();

         updateHostsDropdownList();
      }

      private void integrateDiffTool()
      {
         IntegratedDiffTool diffTool = new BC3Tool();
         DiffToolIntegration integration = new DiffToolIntegration(diffTool);

         try
         {
            integration.RegisterInGit(GitDiffToolName);
         }
         catch (GitOperationException ex)
         {
            ExceptionHandlers.Handle(ex,
               String.Format("Cannot integrate \"{0}\" in git", diffTool.GetToolName()), true);
         }

         try
         {
            integration.RegisterInTool();
         }
         catch (DiffToolIntegrationException ex)
         {
            ExceptionHandlers.Handle(ex,
               String.Format("Cannot integrate the application in \"{0}\"", diffTool.GetToolName()), true);
         }
      }

      private void updateHostsDropdownList()
      {
         int? lastSelectedHostIndex = new Nullable<int>();
         comboBoxHost.Items.Clear();
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
         if (projects == null)
         {
            return;
         }

         // dealing with 'SelectedItem' and not 'SelectedIndex' here because projects combobox id Sorted
         Project? lastSelectedProject = null;
         comboBoxProjects.Items.Clear();
         foreach (var project in projects)
         {
            comboBoxProjects.Items.Add(project);
            if (project.Path_With_Namespace == _settings.LastSelectedProject)
            {
               lastSelectedProject = project;
            }
         }
         if (lastSelectedProject != null)
         {
            comboBoxProjects.SelectedItem = lastSelectedProject;
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

      private void updateMergeRequestsDropdownList(List<MergeRequest> mergeRequests, bool keepPosition)
      {
         if (mergeRequests == null)
         {
            return;
         }

         keepPosition &= (comboBoxFilteredMergeRequests.SelectedItem != null);
         MergeRequest? currentItem =
            keepPosition ? (MergeRequest)comboBoxFilteredMergeRequests.SelectedItem : new Nullable<MergeRequest>();

         comboBoxFilteredMergeRequests.Items.Clear();
         foreach (var mergeRequest in mergeRequests)
         {
            if (doesMergeRequestMatchLabels(mergeRequest))
            {
               comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
            }
         }
         if (comboBoxFilteredMergeRequests.Items.Count > 0)
         {
            if (currentItem.HasValue)
            {
               bool found = false;
               for (int iItem = 0; iItem < comboBoxFilteredMergeRequests.Items.Count; ++iItem)
               {
                  MergeRequest mr = (MergeRequest)(comboBoxFilteredMergeRequests.Items[iItem]);
                  if (mr.Id == currentItem.Value.Id)
                  {
                     comboBoxFilteredMergeRequests.SelectedIndex = iItem;
                     found = true;
                     break;
                  }
               }

               if (!found)
               {
                  comboBoxFilteredMergeRequests.SelectedIndex = 0;
               }
            }
            else
            {
               comboBoxFilteredMergeRequests.SelectedIndex = 0;
            }
         }
         else
         {
            // this case means that selection was reset. It needs a special handling.
            comboBoxFilteredMergeRequests.SelectedIndex = -1;

            // call it manually because events are not called on -1
            onMergeRequestSelected();
         }
      }

      private bool doesMergeRequestMatchLabels(MergeRequest mergeRequest)
      {
         // TODO This can be cached
         List<string> labelsRequested = new List<string>();
         if (checkBoxLabels.Checked && textBoxLabels.Text != null)
         {
            foreach (var item in textBoxLabels.Text.Split(','))
            {
               labelsRequested.Add(item.Trim(' '));
            }
         }

         if (labelsRequested.Count > 0)
         {
            foreach (var label in labelsRequested)
            {
               if (mergeRequest.Labels.Contains(label))
               {
                  return true;
               }
            }
         }
         else
         {
            return true;
         }

         return false;
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

         MergeRequest? mergeRequest = getMergeRequest();
         if (!mergeRequest.HasValue)
         {
            return;
         }

         // 1. Update status, add merge request url
         linkLabelConnectedTo.Visible = true;
         linkLabelConnectedTo.Text = mergeRequest.Value.Web_Url;

         // 2. Populate edit boxes with merge request details
         textBoxMergeRequestName.Text = mergeRequest.Value.Title;
         richTextBoxMergeRequestDescription.Text = mergeRequest.Value.Description;

         // 3. Add versions to combo-boxes
         updateVersions(mergeRequest.Value);

         // 4. Toggle state of  buttons
         buttonToggleTimer.Enabled = true;
         buttonDiffTool.Enabled = true;
         buttonDiscussions.Enabled = true;
         foreach (Control control in groupBoxActions.Controls)
         {
            control.Enabled = true;
         }
      }

      private void updateVersions(MergeRequest mergeRequest)
      {
         comboBoxLeftVersion.Items.Clear();
         comboBoxRightVersion.Items.Clear();

         var versions = getVersions();
         if (versions == null || versions.Count == 0)
         {
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
            new VersionComboBoxItem(mergeRequest.Diff_Refs.Base_SHA, mergeRequest.Target_Branch, null);
         comboBoxRightVersion.Items.Add(targetBranch);

         comboBoxLeftVersion.SelectedIndex = 0;
         comboBoxRightVersion.SelectedIndex = 0;
      }

      private void onStartTimer()
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
         updateInterprocessSnapshot();
      }

      private void onStopTimer(bool sendTrackedTime)
      {
         // 1. Stop stopwatch
         _stopWatch.Stop();

         // 2. Stop timer
         _timeTrackingTimer.Stop();

         // 3. Update information available to other instances
         updateInterprocessSnapshot();

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
            Project project = (Project)comboBoxProjects.SelectedItem;
            return project.Path_With_Namespace;
         }
         return null;
      }

      public int GetCurrentMergeRequestId()
      {
         if (comboBoxFilteredMergeRequests.SelectedItem != null)
         {
            return ((MergeRequest)(comboBoxFilteredMergeRequests.SelectedItem)).IId;
         }
         return 0;
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

      private Timer _timeTrackingTimer;

      private bool _exiting = false;
      private bool _requireShowingTooltipOnHideToTray = true;
      private UserDefinedSettings _settings;
      private GitRepository _gitRepository;

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

      private Timer _mergeRequestCheckTimer;
      private DateTime _lastCheckTime = DateTime.Now;

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
