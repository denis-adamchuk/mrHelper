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
      private static readonly string timeTrackingMutexGuid = "{f0b3cbf1-e022-468b-aeb6-db0417a12379}";
      private static readonly System.Threading.Mutex timeTrackingMutex =
          new System.Threading.Mutex(false, timeTrackingMutexGuid);

      // TODO Move to resources
      // {
      private static readonly string buttonStartTimerDefaultText = "Start Timer";
      private static readonly string buttonStartTimerTrackingText = "Send Spent";
      private static readonly string labelSpentTimeDefaultText = "00:00:00";
      private static readonly int timeTrackingTimerInterval = 1000; // ms

      private static readonly string errorMessageBoxText = "Error";
      private static readonly string warningMessageBoxText = "Warning";
      private static readonly string informationMessageBoxText = "Information";

      private static readonly string errorTrackedTimeNotSet = "Tracked time was not sent to server";

      /// <summary>
      /// Tooltip timeout in seconds
      /// </summary>
      private const int notifyTooltipTimeout = 5;
      // }

      public const string GitDiffToolName = "mrhelperdiff";
      private const string CustomActionsFileName = "CustomActions.xml";
      private const string ProjectListFileName = "projects.json";
      private const string DefaultColorSchemeName = "Default";
      private const string ColorSchemeFileNamePrefix = "colors.json";

      public mrHelperForm()
      {
         InitializeComponent();
      }

      private void addCustomActions()
      {
         if (!File.Exists(CustomActionsFileName))
         {
            // If file doesn't exist the loader throws, leaving the app in an undesirable state
            // Do not try to load custom actions if they don't exist
            return;
         }
         CustomCommandLoader loader = new CustomCommandLoader(this);
         List<ICommand> commands = loader.LoadCommands(CustomActionsFileName);
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

      private void ComboBoxColorSchemes_SelectedIndexChanged(object sender, EventArgs e)
      {
         try
         {
            if (comboBoxColorSchemes.SelectedItem.ToString() == DefaultColorSchemeName)
            {
               _colorScheme = new ColorScheme();
            }
            else
            {
               _colorScheme = new ColorScheme(comboBoxColorSchemes.SelectedItem.ToString());
            }
            _settings.ColorSchemeFileName = (sender as ComboBox).Text;
         }
         catch (Exception ex)
         {
            comboBoxColorSchemes.SelectedIndex = 0;
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ComboBoxHost_SelectedIndexChanged(object sender, EventArgs e)
      {
         try
         {
            _gitRepository = null;
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
            _gitRepository = null;
            updateMergeRequestsDropdownList(getAllProjectMergeRequests(comboBoxProjects.Text), false);
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
            updateMergeRequestsDropdownList(getAllProjectMergeRequests(comboBoxProjects.Text), false);
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
      private void comboBoxDCDepth_SelectedIndexChanged(object sender, EventArgs e)
      {
         _settings.DiffContextDepth = (sender as ComboBox).Text;
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

         checkForUpdates();

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
         List<Project> projects = null;
         if (File.Exists(ProjectListFileName))
         {
            try
            {
               projects = loadProjectsFromFile(item.Host, ProjectListFileName);
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
         }
         if (projects == null || projects.Count == 0)
         {
            GitLab gl = new GitLab(item.Host, item.AccessToken);
            projects = gl.Projects.LoadAll(new ProjectsFilter { PublicOnly = checkBoxShowPublicOnly.Checked });
         }
         return projects;
      }

      private List<MergeRequest> getAllProjectMergeRequests(string project)
      {
         if (comboBoxHost.SelectedItem == null)
         {
            return new List<MergeRequest>();
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLab gl = new GitLab(item.Host, item.AccessToken);
         return gl.Projects.Get(comboBoxProjects.Text).MergeRequests.LoadAll(new MergeRequestsFilter());
      }

      private MergeRequest getMergeRequest()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return new MergeRequest();
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLab gl = new GitLab(item.Host, item.AccessToken);
         return gl.Projects.Get(comboBoxProjects.Text).MergeRequests.Get(mergeRequest.Value.IId).Load();
      }

      private List<Version> getVersions()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return new List<Version>();
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLab gl = new GitLab(item.Host, item.AccessToken);
         return gl.Projects.Get(comboBoxProjects.Text).MergeRequests.Get(mergeRequest.Value.IId).Versions.LoadAll();
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
         gl.Projects.Get(comboBoxProjects.Text).MergeRequests.Get(mergeRequest.Value.IId).AddSpentTime(
            new AddSpentTimeParameters { Span = span });
      }

      private class HostInProjectsFile
      {
         public string Name = null;
         public List<Project> Projects = null;
      }

      /// <summary>
      /// Loads project list from file with JSON format
      /// </summary>
      /// <param name="hostname">Host name to look up projects for</param>
      /// <param name="filename">Name of JSON file with project list</param>
      /// <param name="projects">Output list of projects</param>
      /// <returns>false if given file does not have projects for the given Host, otherwise true</returns>
      private List<Project> loadProjectsFromFile(string hostname, string filename)
      {
         Debug.Assert(File.Exists(filename));

         try
         {
            string json = System.IO.File.ReadAllText(filename);

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            List<HostInProjectsFile> hosts = serializer.Deserialize<List<HostInProjectsFile>>(json);
            foreach (var host in hosts)
            {
               if (host.Name == hostname)
               {
                  return host.Projects;
               }
            }
         }
         catch (Exception)
         {
            // Bad JSON
            throw new ApplicationException("Unexpected format of project list file. File content is ignored.");
         }

         return null;
      }

      private void onLaunchDiffTool()
      {
         if (_gitRepository == null)
         {
            _gitRepository = initializeGitRepository();
         }

         if (_gitRepository == null)
         {
            throw new ApplicationException("Cannot launch a diff tool because of a problem with git repository");
         }

         checkForUpdates();

         _difftool = null; // in case the next line throws
         _difftool = _gitRepository.DiffTool(GitDiffToolName, getGitTag(true /* left */), getGitTag(false /* right */));
         updateInterprocessSnapshot();
      }

      // git repository may be not up-to-date. Check if there is a version in GitLab which is newer than latest update.
      private void checkForUpdates()
      {
         MergeRequest? mergeRequest = getSelectedMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null || !mergeRequest.HasValue)
         {
            return;
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);
         GitLab gl = new GitLab(item.Host, item.AccessToken);
         var versions = gl.Projects.Get(comboBoxProjects.Text).MergeRequests.Get(mergeRequest.Value.IId).Versions.LoadAll();
         if (versions.Count == 0)
         {
            return;
         }

         Version latestVersion = versions[0];
         if (latestVersion.Created_At.ToLocalTime() > _gitRepository.LastUpdateTime)
         {
            _gitRepository.Fetch();
         }
      }

      private void onTimer()
      {
         var newMergeRequests = new List<MergeRequest>();
         var updatedMergeRequests = new List<MergeRequest>();
         collectUpdates(out newMergeRequests, out updatedMergeRequests);
         notifyOnUpdates(newMergeRequests, updatedMergeRequests);

         // Update lists in combo-boxes
         if (comboBoxProjects.SelectedItem != null)
         {
            int selectedProjectId = ((Project)(comboBoxProjects.SelectedItem)).Id;

            // Update list of available merge requests
            List<MergeRequest> mergeRequestsOfSelectedProject = new List<MergeRequest>();
            foreach (var mergeRequest in updatedMergeRequests)
            {
               if (mergeRequest.Project_Id == selectedProjectId)
               {
                  mergeRequestsOfSelectedProject.Add(mergeRequest);
               }
            }
            updateMergeRequestsDropdownList(mergeRequestsOfSelectedProject, true);

            // Update list of versions
            foreach (var mergeRequest in mergeRequestsOfSelectedProject)
            {
               if (mergeRequest.IId == ((MergeRequest)(comboBoxFilteredMergeRequests.SelectedItem)).IId)
               {
                  updateVersions(mergeRequest, true);
               }
            }
         }
      }

      /// <summary>
      /// Collects requests that have been created or updated later than _lastCheckTime.
      /// By 'updated' we mean that 'merge request has a version with a timestamp later than ...'.
      /// Checks all the hosts.
      /// Checks all the projects if project filtering is not used, otherwise checks only filtered project.
      /// Includes only those merge requests that match Labels filters.
      /// </summary>
      private void collectUpdates(out List<MergeRequest> newMergeRequests, out List<MergeRequest> updatedMergeRequests)
      {
         newMergeRequests = new List<MergeRequest>();
         updatedMergeRequests = new List<MergeRequest>();

         foreach (var item in comboBoxHost.Items)
         {
            HostComboBoxItem hostItem = (HostComboBoxItem)(item); 
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
            else
            {
               projectsToCheck.Add((Project)(comboBoxProjects.SelectedItem));
            }

            foreach (var project in projectsToCheck)
            {
               List<MergeRequest> mergeRequests =
                  gl.Projects.Get(project.Path_With_Namespace).MergeRequests.LoadAll(new MergeRequestsFilter());

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
                     var versions = gl.Projects.Get(project.Path_With_Namespace).
                        MergeRequests.Get(mergeRequest.IId).Versions.LoadAll();
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
         }
      }

      private void notifyOnUpdates(List<MergeRequest> newMergeRequests, List<MergeRequest> updatedMergeRequests)
      {
         throw new NotImplementedException();
      }

      private GitRepository initializeGitRepository()
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
               return null;
            }
         }

         string project = projectWithNamespace.Split('/')[1];
         string path = Path.Combine(localGitFolder, project);
         if (!Directory.Exists(path))
         {
            if (MessageBox.Show("There is no project \"" + project + "\" repository in " + localGitFolder +
               ". Do you want to clone git repository?", informationMessageBoxText, MessageBoxButtons.YesNo,
               MessageBoxIcon.Information) == DialogResult.Yes)
            {
               return GitRepository.CreateByClone(host, projectWithNamespace, path);
            }
            else
            {
               return null; 
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

         string leftSHA = diffArgs[diffArgs.Length - 2];
         string rightSHA = diffArgs[diffArgs.Length - 1];

         MergeRequest mergeRequest = getMergeRequest();
         if (comboBoxHost.SelectedItem == null || comboBoxProjects.SelectedItem == null)
         {
            return;
         }

         HostComboBoxItem item = (HostComboBoxItem)(comboBoxHost.SelectedItem);

         InterprocessSnapshot snapshot;
         snapshot.AccessToken = item.AccessToken;
         snapshot.Refs.LeftSHA = leftSHA;                       // Base commit SHA in the source branch
         snapshot.Refs.RightSHA = rightSHA;                       // SHA referencing HEAD of this merge request
         snapshot.Host = item.Host;
         snapshot.MergeRequestId = mergeRequest.IId;
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
         // dealing with 'SelectedItem' and not 'SelectedIndex' here because projects combobox id Sorted
         string lastSelectedProjectName = null;
         comboBoxProjects.Items.Clear();
         foreach (var project in projects)
         {
            comboBoxProjects.Items.Add(project.Path_With_Namespace);
            if (project.Path_With_Namespace == _settings.LastSelectedProject)
            {
               lastSelectedProjectName = project.Path_With_Namespace;
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

      private void updateMergeRequestsDropdownList(List<MergeRequest> mergeRequests, bool keepPosition)
      {
         if (keepPosition) throw new NotImplementedException();

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
            comboBoxFilteredMergeRequests.SelectedIndex = 0;
         }
         else
         {
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

         MergeRequest mergeRequest = getMergeRequest();

         // 1. Update status, add merge request url
         linkLabelConnectedTo.Visible = true;
         linkLabelConnectedTo.Text = mergeRequest.Web_Url;

         // 2. Populate edit boxes with merge request details
         textBoxMergeRequestName.Text = mergeRequest.Title;
         richTextBoxMergeRequestDescription.Text = mergeRequest.Description;

         // 3. Add versions to combo-boxes
         updateVersions(mergeRequest, false);

         // 4. Toggle state of  buttons
         buttonToggleTimer.Enabled = true;
         buttonDiffTool.Enabled = true;
         buttonDiscussions.Enabled = true;
         foreach (Control control in groupBoxActions.Controls)
         {
            control.Enabled = true;
         }
      }

      private void updateVersions(MergeRequest mergeRequest, bool keepPosition)
      {
         comboBoxLeftVersion.Items.Clear();
         comboBoxRightVersion.Items.Clear();

         var versions = getVersions();
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

         if (!keepPosition)
         {
            comboBoxLeftVersion.SelectedIndex = 0;
            comboBoxRightVersion.SelectedIndex = 0;
         }
         else
         {
            throw new NotImplementedException();
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
      private bool _requireShowingTooltip = true;
      private UserDefinedSettings _settings;
      private GitRepository _gitRepository;

      // For accurate time tracking
      private readonly Stopwatch _stopWatch = new Stopwatch();

      // Last launched instance of a diff tool
      private Process _difftool;

      private ColorScheme _colorScheme = new ColorScheme();

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
