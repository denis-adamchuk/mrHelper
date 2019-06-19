using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace mrHelper
{
   public partial class mrHelperForm : Form
   {
      static private string buttonStartTimerDefaultText = "Start Timer";
      static private string buttonStartTimerTrackingText = "Send Spent";
      static private string labelSpentTimeDefaultText = "00:00:00";
      static private int timeTrackingTimerInterval = 1000; // ms

      public mrHelperForm()
      {
         InitializeComponent();
         buttonToggleTimer.Text = buttonStartTimerDefaultText;
         labelSpentTime.Text = labelSpentTimeDefaultText;
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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonDifftool_Click(object sender, EventArgs e)
      {
         string currentDirectory = Directory.GetCurrentDirectory();
         try
         {
            string repository = initializeGitRepository();
            Directory.SetCurrentDirectory(repository);
            gitClient.DiffTool(getGitTag(true /* left */), getGitTag(false /* right */));
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (_isTrackingTime)
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
            MessageBox.Show(ex.Message + " Tracked time was not sent to server", "Error",
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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void RadioButtonURL_CheckedChanged(object sender, EventArgs e)
      {
         try
         {
            onChangeMrSelectingType(MrSelectingType.SearchByFilter);
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void RadioButtonListMR_CheckedChanged(object sender, EventArgs e)
      {
         try
         {
            onChangeMrSelectingType(MrSelectingType.DirectURL);
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void ButtonSearchByLabel_Click(object sender, EventArgs e)
      {
         try
         {
            List<MergeRequest> mergeRequests = getAllMergeRequests();
            onLoadedListOfMergeRequests(mergeRequests);
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void TabControl_Selecting(object sender, TabControlCancelEventArgs e)
      {
         if (e.TabPageIndex < 0)
         {
            return;
         }
         e.Cancel = !e.TabPage.Enabled;
      }

      private void ComboBoxFilteredMergeRequests_Format(object sender, ListControlConvertEventArgs e)
      {
         e.Value = ((MergeRequest)e.ListItem).Title + "    " + "[" + ((MergeRequest)e.ListItem).Author + "]";
      }

      private void ComboBoxLeftCommit_Format(object sender, ListControlConvertEventArgs e)
      {
         formatCommitComboboxItem(e);
      }

      private void ComboBoxRightCommit_Format(object sender, ListControlConvertEventArgs e)
      {
         formatCommitComboboxItem(e);
      }

      private string getSelectedMergeRequestUrl()
      {
         if (radioButtonSelectMR_Filter.Checked)
         {
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
            throw new NotImplementedException("Unsupported State value");
         }

         WorkInProgressFilter wip;
         if (!Enum.TryParse(getCheckedRadioButton(groupBoxWIP).Text, out wip))
         {
            throw new NotImplementedException("Unsupported WIP value");
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

      private List<Commit> getCommits()
      {
         ParsedMergeRequestUrl parsed = parseAndValidateUrl(getSelectedMergeRequestUrl());
         gitlabClient client = new gitlabClient(parsed.Host, textBoxAccessToken.Text);
         return client.GetMergeRequestCommits(parsed.Project, parsed.Id);
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
               "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
               Directory.CreateDirectory(localGitFolder);
            }
            else
            {
               throw new ApplicationException("Cannot launch difftool because there is no valid repository");
            }
         }

         ParsedMergeRequestUrl parsed = parseAndValidateUrl(getSelectedMergeRequestUrl());
         string project = parsed.Project.Split('/')[1];
         string repository = localGitFolder + "/" + project;
         if (!Directory.Exists(repository))
         {
            if (MessageBox.Show("There is no project " + project + " repository within folder " + localGitFolder +
               ". Do you want to clone git repository?", "Information", MessageBoxButtons.YesNo,
               MessageBoxIcon.Information) == DialogResult.Yes)
            {
               gitClient.CloneRepo(parsed.Host, parsed.Project, repository);
            }
            else
            {
               throw new ApplicationException("Cannot launch difftool because there is no valid repository");
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
         if (left)
         {
            if (comboBoxLeftCommit.SelectedItem != null)
            {
               return ((CommitComboBoxItem)comboBoxLeftCommit.SelectedItem).Text;
            }
            return "";
         }
         else
         {
            if (comboBoxRightCommit.SelectedItem != null)
            {
               return ((CommitComboBoxItem)comboBoxRightCommit.SelectedItem).Text;
            }
            return "";
         }
      }

      private static void formatCommitComboboxItem(ListControlConvertEventArgs e)
      {
         formatCommitComboboxItem(e);
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
         return buttonConnect.Text == "Disconnect";
      }

      private void onApplicationStarted()
      {
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
         tabPageDiff.Enabled = false;
         tabPageDiff.ToolTipText = "You need to select a merge request first";
      }

      private void onChangeMrSelectingType(MrSelectingType type)
      {
         switch (type)
         {
            case MrSelectingType.SearchByFilter:
               // Enable all elements except a text box for MR URL
               foreach (Control item in groupBoxSelectMergeRequest.Controls)
               {
                  item.Enabled = true;
               }
               textBoxMrURL.Enabled = false;
               break;

            case MrSelectingType.DirectURL:
               // Disable all elements except a text box for MR URL
               foreach (Control item in groupBoxSelectMergeRequest.Controls)
               {
                  item.Enabled = false;
               }
               textBoxMrURL.Enabled = false;

               // Radio-buttons should also be enabled
               radioButtonSelectMR_Filter.Enabled = true;
               radioButtonSelectMR_URL.Enabled = true;
               break;
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
         // 1. Disable all UI elements that belong to Select Merge Requests groupbox
         foreach (Control item in groupBoxSelectMergeRequest.Controls)
         {
            item.Enabled = false;
         }

         // 2. Update status, add merge request url
         MergeRequest mergeRequest = getMergeRequest();
         labelCurrentStatus.Text = "Connected to:";
         linkLabelConnectedTo.Visible = true;
         linkLabelConnectedTo.Text = mergeRequest.WebUrl;

         // 3. Update text at Connect button
         buttonConnect.Text = "Disconnect";

         // 4. Enable Diff tab and switch to it
         tabPageDiff.Enabled = true;
         tabPageDiff.Select();

         // 5. Populate edit boxes with merge request details
         textBoxMergeRequestName.Text = mergeRequest.Title;
         richTextBoxMergeRequestDescription.Text = mergeRequest.Description;

         // 6. Add commit information to combo boxes
         foreach (var commit in getCommits())
         {
            CommitComboBoxItem item = new CommitComboBoxItem(commit.ShortId, commit.CommitedDate);
            comboBoxLeftCommit.Items.Add(commit);
            comboBoxRightCommit.Items.Add(commit);
         }

         // 7. Add two special rows to each of combo-boxes
         CommitComboBoxItem sourceBranch = new CommitComboBoxItem("origin/" + mergeRequest.SourceBranch, null);
         comboBoxLeftCommit.Items.Add(sourceBranch);
         comboBoxRightCommit.Items.Add(sourceBranch);

         CommitComboBoxItem targetBranch = new CommitComboBoxItem("origin/" + mergeRequest.TargetBranch, null);
         comboBoxLeftCommit.Items.Add(targetBranch);
         comboBoxRightCommit.Items.Add(targetBranch);

         comboBoxLeftCommit.SelectedIndex = 0;
         comboBoxRightCommit.SelectedIndex = comboBoxRightCommit.Items.Count - 1;

         // 8. Toggle state of rest buttons
         buttonToggleTimer.Enabled = true;
         buttonDifftool.Enabled = true;
      }

      private void onDisconnected()
      {
         // 1. Stop timer
         if (_isTrackingTime)
         {
            onStopTimer(false /* don't send time to server */);
         }

         // 2. Toggle state of buttons
         buttonDifftool.Enabled = false;
         buttonToggleTimer.Enabled = false;

         // 3. Clean-up lists of commits
         comboBoxRightCommit.Items.Clear();
         comboBoxLeftCommit.Items.Clear();

         // 4. Clean-up textboxes with merge request details
         textBoxMergeRequestName.Text = null;
         richTextBoxMergeRequestDescription.Text = null;

         // 6. Disable Diff tab
         tabPageDiff.Enabled = false;

         // 7. Switch to Merge Requests tab, just for consistency with onConnected()
         tabPageMR.Select();

         // 8. Change Connect button text
         buttonConnect.Text = "Connect";

         // 9. Update status
         labelCurrentStatus.Text = "Not connected";
         linkLabelConnectedTo.Visible = false;
         linkLabelConnectedTo.Text = null;

         // 10. Enable all UI elements that belong to Select Merge Requests groupbox
         foreach (Control item in groupBoxSelectMergeRequest.Controls)
         {
            item.Enabled = true;
         }
      }

      private void onStartTimer()
      {
         if (_timeTrackingTimer == null)
         {
            _timeTrackingTimer = new Timer();
         }

         buttonToggleTimer.Text = buttonStartTimerTrackingText;
         labelSpentTime.Text = labelSpentTimeDefaultText;
         _lastStartTimeStamp = DateTime.Now;
         _timeTrackingTimer.Interval = timeTrackingTimerInterval;
         _timeTrackingTimer.Tick += new System.EventHandler(onTimer);
         _timeTrackingTimer.Start();
         _isTrackingTime = true;
      }

      private void onStopTimer(bool sendTrackedTime)
      {
         var timeSpan = DateTime.Now - _lastStartTimeStamp;
         buttonToggleTimer.Text = buttonStartTimerDefaultText;
         labelSpentTime.Text = labelSpentTimeDefaultText;
         _lastStartTimeStamp = DateTime.MinValue;
         _timeTrackingTimer.Stop();
         _isTrackingTime = false;

         if (sendTrackedTime)
         {
            sendTrackedTimeSpan(timeSpan);
         }
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

      private bool _isTrackingTime;
      private DateTime _lastStartTimeStamp;
      private Timer _timeTrackingTimer;

      private bool _exiting = false;

      UserDefinedSettings _settings;
   }

   enum MrSelectingType
   {
      SearchByFilter,
      DirectURL
   }

   struct CommitComboBoxItem
   {
      public string Text;
      public DateTime? TimeStamp;

      public CommitComboBoxItem(string text, DateTime? timeStamp)
      {
         Text = text;
         TimeStamp = timeStamp;
      }
   }
}
