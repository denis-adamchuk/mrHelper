using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace mrHelper
{
   public partial class mrHelperForm : Form
   {
      static private string timeTrackingMutexGuid = "{f0b3cbf0-e022-468b-aeb6-db0417a12379}";
      static System.Threading.Mutex timeTrackingMutex =
          new System.Threading.Mutex(false, timeTrackingMutexGuid);

      // TODO Move to resources
      // {
      static private string buttonStartTimerDefaultText = "Start Timer";
      static private string buttonStartTimerTrackingText = "Send Spent";
      static private string labelSpentTimeDefaultText = "00:00:00";
      static private int timeTrackingTimerInterval = 1000; // ms

      static private string buttonConnectText = "Connect";
      static private string buttonDisconnectText = "Disconnect";
      static private string statusConnectedText = "Connected to:";
      static private string statusNotConnectedText = "Not connected";

      static private string errorMessageBoxText = "Error";
      static private string warningMessageBoxText = "Warning";
      static private string informationMessageBoxText = "Information";

      static private string remoteRepositoryDefaultName = "origin";

      static private string errorTrackedTimeNotSet = "Tracked time was not sent to server";
      static private string errorUnsupportedState = "Unsupported State value";
      static private string errorUnsupportedWip = "Unsupported WIP value";
      static private string errorNoValidRepository = "Cannot launch difftool because there is no valid repository";
      // }

      public const string InterprocessSnapshotFilename = "details.json";

      public mrHelperForm()
      {
         InitializeComponent();
      }

      private void MrHelperForm_Load(object sender, EventArgs e)
      {
         try
         {
            loadConfiguration();
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

      private void ButtonConnect_Click(object sender, EventArgs e)
      {
         try
         {
            if (isConnected())
            {
               onDisconnected();
               return;
            }

            onConnected();
         }
         catch (Exception ex)
         {
            onDisconnected();
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonDifftool_Click(object sender, EventArgs e)
      {
         string currentDirectory = Directory.GetCurrentDirectory();
         try
         {
            string repository = initializeGitRepository();
            Directory.SetCurrentDirectory(repository);
            _difftool = gitClient.DiffTool(getGitTag(true /* left */), getGitTag(false /* right */));
            updateDetailsSnapshot();
         }
         catch (Exception ex)
         {
            _difftool = null;
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         finally
         {
            Directory.SetCurrentDirectory(currentDirectory);
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

      private void RestoreToolStripMenuItem_Click(object sender, EventArgs e)
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
               onSelectedGitFolder();
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonSearchByLabel_Click(object sender, EventArgs e)
      {
         try
         {
            labelSearchStatus.Visible = true;
            labelSearchStatus.Text = "Search in progress";
            List<MergeRequest> mergeRequests = getAllMergeRequests();
            labelSearchStatus.Text = "Found " + mergeRequests.Count.ToString() + " result(s)";
            onLoadedListOfMergeRequests(mergeRequests);
         }
         catch (Exception ex)
         {
            labelSearchStatus.Visible = false;
            MessageBox.Show(ex.Message, errorMessageBoxText, MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ComboBoxFilteredMergeRequests_Format(object sender, ListControlConvertEventArgs e)
      {
         formatMergeRequestListItem(e);
      }

      private void ComboBoxLeftVersion_Format(object sender, ListControlConvertEventArgs e)
      {
         formatVersionComboboxItem(e);
      }

      private void ComboBoxRightVersion_Format(object sender, ListControlConvertEventArgs e)
      {
         formatVersionComboboxItem(e);
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

      private void checkComboboxVersionsOrder(bool shouldReorderRightCombobox)
      {
         if (comboBoxLeftVersion.SelectedItem == null || comboBoxRightVersion.SelectedItem == null)
         {
            return;
         }

         // Left combobox cannot select a version older than in right combobox (gitlab web ui)
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

      private string getSelectedMergeRequestUrl()
      {
         if (radioButtonSelectMR_Filter.Checked)
         {
            if (comboBoxFilteredMergeRequests.SelectedItem == null)
            {
               throw new ApplicationException("Merge request is not selected");
            }
            return ((MergeRequest)comboBoxFilteredMergeRequests.SelectedItem).WebUrl;
         }
         return textBoxMrURL.Text;
      }

      private ParsedMergeRequestUrl parseAndValidateUrl(string url)
      {
         MergeRequestUrlParser parser = new MergeRequestUrlParser(url);
         return parser.Parse();
      }

      private List<MergeRequest> getAllMergeRequests()
      {
         StateFilter state;
         if (!Enum.TryParse(getCheckedRadioButton(groupBoxState).Text, out state))
         {
            throw new NotImplementedException(errorUnsupportedState);
         }

         WorkInProgressFilter wip;
         if (!Enum.TryParse(getCheckedRadioButton(groupBoxWIP).Text, out wip))
         {
            throw new NotImplementedException(errorUnsupportedWip);
         }

         gitlabClient client = new gitlabClient(textBoxHost.Text, textBoxAccessToken.Text);
         return client.GetAllMergeRequests(state, textBoxLabels.Text, textBoxAuthor.Text, wip);
      }

      private MergeRequest getMergeRequest()
      {
         ParsedMergeRequestUrl parsed = parseAndValidateUrl(getSelectedMergeRequestUrl());
         gitlabClient client = new gitlabClient(parsed.Host, textBoxAccessToken.Text);
         return client.GetSingleMergeRequest(parsed.Project, parsed.Id);
      }

      private List<Version> getVersions()
      {
         ParsedMergeRequestUrl parsed = parseAndValidateUrl(getSelectedMergeRequestUrl());
         gitlabClient client = new gitlabClient(parsed.Host, textBoxAccessToken.Text);
         return client.GetMergeRequestVersions(parsed.Project, parsed.Id);
      }

      void sendTrackedTimeSpan(TimeSpan span)
      {
         ParsedMergeRequestUrl parsed = parseAndValidateUrl(getSelectedMergeRequestUrl());
         gitlabClient client = new gitlabClient(parsed.Host, textBoxAccessToken.Text);
         client.AddSpentTimeForMergeRequest(parsed.Project, parsed.Id, ref span);
      }

      string initializeGitRepository()
      {
         string localGitFolder = textBoxLocalGitFolder.Text;
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

         ParsedMergeRequestUrl parsed = parseAndValidateUrl(getSelectedMergeRequestUrl());
         string project = parsed.Project.Split('/')[1];
         string repository = localGitFolder + "/" + project;
         if (!Directory.Exists(repository))
         {
            if (MessageBox.Show("There is no project " + project + " repository within folder " + localGitFolder +
               ". Do you want to clone git repository?", informationMessageBoxText, MessageBoxButtons.YesNo,
               MessageBoxIcon.Information) == DialogResult.Yes)
            {
               gitClient.CloneRepo(parsed.Host, parsed.Project, repository);
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
            gitClient.Fetch();
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
               ((VersionComboBoxItem)comboBoxLeftVersion.SelectedItem).Text : "";
         }
         else
         {
            return comboBoxRightVersion.SelectedItem != null ?
               ((VersionComboBoxItem)comboBoxRightVersion.SelectedItem).Text : "";
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

      private void loadConfiguration()
      {
         _settings = new UserDefinedSettings();
         onConfigurationLoaded();
      }

      private void saveConfiguration()
      {
         _settings.Host = textBoxHost.Text;
         _settings.AccessToken = textBoxAccessToken.Text;
         _settings.LocalGitFolder = textBoxLocalGitFolder.Text;
         _settings.Update();
      }

      private bool isConnected()
      {
         return buttonConnect.Text == buttonDisconnectText;
      }

      private void updateDetailsSnapshot()
      {
         string snapshotPath = Environment.GetEnvironmentVariable("TEMP");

         if (/*_timeTrackingTimer.Enabled && */_difftool != null && !_difftool.HasExited)
         {
            string[] diffArgs = _difftool.StartInfo.Arguments.Split(' ');
            if (diffArgs.Length < 2)
            {
               return;
            }

            string headSHA = trimRemoteRepositoryName(diffArgs[diffArgs.Length - 1]);
            string startSHA = trimRemoteRepositoryName(diffArgs[diffArgs.Length - 2]);

            ParsedMergeRequestUrl parsed = parseAndValidateUrl(getSelectedMergeRequestUrl());
            MergeRequest mergeRequest = getMergeRequest();

            MergeRequestDetails details;
            details.AccessToken = textBoxAccessToken.Text;
            details.BaseSHA = mergeRequest.BaseSHA;    // Base commit SHA in the source branch
            details.HeadSHA = headSHA;                 // SHA referencing HEAD of this merge request
            details.StartSHA = startSHA;               // SHA referencing commit in target branch
            details.Host = parsed.Host;
            details.Id = parsed.Id;
            details.Project = parsed.Project;
            details.TempFolder = textBoxLocalGitFolder.Text;

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(details);
            System.IO.File.WriteAllText(System.IO.Path.Combine(snapshotPath, InterprocessSnapshotFilename), json);
         }
         else
         {
            System.IO.File.Delete(System.IO.Path.Combine(snapshotPath, InterprocessSnapshotFilename));
         }
      }

      private static string trimRemoteRepositoryName(string sha)
      {
         if (sha.StartsWith(remoteRepositoryDefaultName))
         {
            sha = sha.Substring(remoteRepositoryDefaultName.Length + 1,
               sha.Length - remoteRepositoryDefaultName.Length - 1);
         }

         return sha;
      }

      private void onApplicationStarted()
      {
         buttonToggleTimer.Text = buttonStartTimerDefaultText;
         labelSpentTime.Text = labelSpentTimeDefaultText;

         bool configured = _settings.Host.Length > 0
                        && _settings.AccessToken.Length > 0
                        && _settings.LocalGitFolder.Length > 0;
         if (configured)
         {
            tabPageMR.Select();
         }
         else
         {
            tabPageSettings.Select();
         }
      }

      private void onLoadedListOfMergeRequests(List<MergeRequest> mergeRequests)
      {
         comboBoxFilteredMergeRequests.Items.Clear();
         foreach (var mergeRequest in mergeRequests)
         {
            comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
         }
         if (comboBoxFilteredMergeRequests.Items.Count > 0)
         {
            comboBoxFilteredMergeRequests.SelectedIndex = 0;
         }
      }

      private void onConnected()
      {
         MergeRequest mergeRequest = getMergeRequest();

         // 1. Disable all UI elements that belong to Select Merge Requests groupbox
         foreach (Control item in groupBoxSelectMergeRequest.Controls)
         {
            item.Enabled = false;
         }

         // 2. Update status, add merge request url
         labelCurrentStatus.Text = statusConnectedText;
         linkLabelConnectedTo.Visible = true;
         linkLabelConnectedTo.Text = mergeRequest.WebUrl;

         // 3. Update text at Connect button
         buttonConnect.Text = buttonDisconnectText;

         // 4. Switch to Diff tab
         tabPageDiff.Select();

         // 5. Populate edit boxes with merge request details
         textBoxMergeRequestName.Text = mergeRequest.Title;
         richTextBoxMergeRequestDescription.Text = mergeRequest.Description;

         // 6. Add version information to combo boxes
         foreach (var version in getVersions())
         {
            VersionComboBoxItem item =
               new VersionComboBoxItem(version.HeadSHA.Substring(0, 10), version.CreatedAt);
            comboBoxLeftVersion.Items.Add(item);
            comboBoxRightVersion.Items.Add(item);
         }

         // 7. Add target branch to the right combo-box
         VersionComboBoxItem targetBranch = new VersionComboBoxItem(
            remoteRepositoryDefaultName + "/" + mergeRequest.TargetBranch, null);
         comboBoxRightVersion.Items.Add(targetBranch);

         comboBoxLeftVersion.SelectedIndex = 0;
         comboBoxRightVersion.SelectedIndex = 0;

         // 8. Toggle state of rest buttons
         buttonToggleTimer.Enabled = true;
         buttonDifftool.Enabled = true;

         // 9. Good moment to update settings
         saveConfiguration();
      }

      private void onDisconnected()
      {
         // 1. Stop timer
         if (_timeTrackingTimer != null && _timeTrackingTimer.Enabled)
         {
            onStopTimer(false /* don't send time to server */);
         }

         // 2. Toggle state of buttons
         buttonDifftool.Enabled = false;
         buttonToggleTimer.Enabled = false;

         // 3. Clean-up lists of versions
         comboBoxRightVersion.Items.Clear();
         comboBoxLeftVersion.Items.Clear();

         // 4. Clean-up textboxes with merge request details
         textBoxMergeRequestName.Text = null;
         richTextBoxMergeRequestDescription.Text = null;

         // 6. Switch to Merge Requests tab, just for consistency with onConnected()
         tabPageMR.Select();

         // 7. Change Connect button text
         buttonConnect.Text = buttonConnectText;

         // 8. Update status
         labelCurrentStatus.Text = statusNotConnectedText;
         linkLabelConnectedTo.Visible = false;
         linkLabelConnectedTo.Text = null;

         // 9. Enable all UI elements that belong to Select Merge Requests groupbox
         foreach (Control item in groupBoxSelectMergeRequest.Controls)
         {
            item.Enabled = true;
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
         
         // 1. Create and initialize timer if does not exist
         if (_timeTrackingTimer == null)
         {
            _timeTrackingTimer = new Timer();
            _timeTrackingTimer.Interval = timeTrackingTimerInterval;
            _timeTrackingTimer.Tick += new System.EventHandler(onTimer);
         }

         // 2. Update button text
         buttonToggleTimer.Text = buttonStartTimerTrackingText;
         
         // 3. Set default text to tracked time label
         labelSpentTime.Text = labelSpentTimeDefaultText;

         // 4. Store current time
         _lastStartTimeStamp = DateTime.Now;

         // 5. Start timer
         _timeTrackingTimer.Start();

         // 6. Update information available to other instances
         updateDetailsSnapshot();
      }

      private void onStopTimer(bool sendTrackedTime)
      {
         // 1. Stop timer
         _timeTrackingTimer.Stop();

         // 2. Update information available to other instances
         updateDetailsSnapshot();

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

      private void onConfigurationLoaded()
      {
         textBoxHost.Text = _settings.Host;
         textBoxAccessToken.Text = _settings.AccessToken;
         textBoxLocalGitFolder.Text = _settings.LocalGitFolder;
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

      private void onSelectedGitFolder()
      {
         textBoxLocalGitFolder.Text = localGitFolderBrowser.SelectedPath;
      }

      private RadioButton getCheckedRadioButton(Control container)
      {
         foreach (var control in container.Controls)
         {
            var radioButton = control as RadioButton;
            if (radioButton != null && radioButton.Checked)
            {
               return radioButton;
            }
         }

         return null;
      }

      private DateTime _lastStartTimeStamp;
      private Timer _timeTrackingTimer;

      private bool _exiting = false;

      UserDefinedSettings _settings;

      // Last launched instance of a diff tool
      Process _difftool;
   }

   struct VersionComboBoxItem
   {
      public string Text;
      public DateTime? TimeStamp;

      public VersionComboBoxItem(string text, DateTime? timeStamp)
      {
         Text = text;
         TimeStamp = timeStamp;
      }
   }
}
