using System;
using System.Collections.Generic;
using System.Configuration;
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
         buttonStartTimer.Text = buttonStartTimerDefaultText;
         labelSpentTime.Text = labelSpentTimeDefaultText;
      }

      private void MrHelperForm_Load(object sender, EventArgs e)
      {
         loadConfiguration();

         onApplicationStarted();

      }

      private void MrHelperForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         if (!_exiting)
         {
            onHideToTray(e);
            return;
         }
         saveConfiguration();
      }

      private void NotifyIcon_DoubleClick(object sender, EventArgs e)
      {
         onRestoreWindow();
      }

      private void ButtonConnect_Click(object sender, EventArgs e)
      {
         if (isConnected())
         {
            onDisconnected();
            return;
         }

         onStartConnectionProcess();

         ParsedMergeRequestUrl parsed;
         MergeRequest mergeRequest;
         List<Commit> commits;
         string gitRepository;
         try
         {
            string url = getSelectedMergeRequestUrl();
            parsed = parseAndValidateUrl(url);
            mergeRequest = getMergeRequest(parsed);
            commits = getCommits(parsed);
            gitRepository = initializeGitRepository(parsed);
         }
         catch (Exception ex)
         {
            onDisconnected();
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }

         onConnected(parsed, mergeRequest, commits, gitRepository);
      }

      private void ButtonDifftool_Click(object sender, EventArgs e)
      {
         // TODO Add error handling and work with TEMP directory
         Directory.SetCurrentDirectory(_gitRepository);
         gitClient.DiffTool(getGitTag(true), getGitTag(false));
         Directory.SetCurrentDirectory("..");
      }

      private string getGitTag(bool left)
      {
         // TODO This is not very safe and not very maintainable
         if (left)
         {
            if (_cachedCommits.Count < comboBoxLeftCommit.SelectedIndex)
            {
               return comboBoxLeftCommit.Text;
            }
            else;
            {
               return _cachedCommits[comboBoxLeftCommit.SelectedIndex].Id;
            }
         }
         else
         {
            if (_cachedCommits.Count < comboBoxRightCommit.SelectedIndex)
            {
               return comboBoxRightCommit.Text;
            }
            else
            {
               return _cachedCommits[comboBoxRightCommit.SelectedIndex].Id;
            }
         }
      }

      private void ButtonStartTimer_Click(object sender, EventArgs e)
      {
         if (_isTrackingTime)
         {
            onStopTimer(true);
         }
         else
         {
            onStartTimer();
         }
      }

      private void RestoreToolStripMenuItem_Click(object sender, EventArgs e)
      {
         onRestoreWindow();
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
            textBoxLocalGitFolder.Text = localGitFolderBrowser.SelectedPath;
         }
      }

      private void RadioButtonURL_CheckedChanged(object sender, EventArgs e)
      {
         onChangeMrSelectingType(MrSelectingType.SearchByFilter);
      }

      private void RadioButtonListMR_CheckedChanged(object sender, EventArgs e)
      {
         onChangeMrSelectingType(MrSelectingType.DirectURL);
      }

      private void ButtonSearchByLabel_Click(object sender, EventArgs e)
      {
         List<MergeRequest> mergeRequests = getMergeRequests();
         onLoadedListOfMergeRequests(mergeRequests);
      }

      private void TabControl_Selecting(object sender, TabControlCancelEventArgs e)
      {
         if (e.TabPageIndex < 0)
         {
            return;
         }
         e.Cancel = !e.TabPage.Enabled;
      }

      private void _oneSecondTimer_Tick(object sender, EventArgs e)
      {
         // TODO Handle overflow
         var span = DateTime.Now - _lastStartTimeStamp;
         labelSpentTime.Text = span.ToString(@"hh\:mm\:ss");
      }

      private string getSelectedMergeRequestUrl()
      {
         if (radioButtonSelectMR_Filter.Checked) // mode with multiple URL
         {
            return _cachedMergeRequests[comboBoxFilteredMergeRequests.SelectedIndex].WebUrl;
         }
         return textBoxMrURL.Text;
      }

      private ParsedMergeRequestUrl parseAndValidateUrl(string url)
      {
         ParsedMergeRequestUrl parsed = new ParsedMergeRequestUrl();
         mrUrlParser parser = new mrUrlParser(url);
         parsed = parser.Parse();

         if (parsed.Host != textBoxHost.Text)
         {
            // TODO Error handling
            throw new NotImplementedException();
         }

         return parsed;
      }

      private MergeRequest getMergeRequest(ParsedMergeRequestUrl parsed)
      {
         labelCurrentStatus.Text = "Loading merge request details...";

         gitlabClient client = new gitlabClient(parsed.Host, textBoxAccessToken.Text);
         var mergeRequest = client.GetSingleMergeRequest(parsed.Project, parsed.Id);
         return mergeRequest;
      }

      private List<Commit> getCommits(ParsedMergeRequestUrl parsed)
      {
         labelCurrentStatus.Text = "Loading list of commits...";

         gitlabClient client = new gitlabClient(parsed.Host, textBoxAccessToken.Text);
         var commits = client.GetMergeRequestCommits(parsed.Project, parsed.Id);
         _cachedCommits = commits;
         return commits;
      }

      private List<MergeRequest> getMergeRequests()
      {
         // TODO Error handling
         StateFilter state;
         Enum.TryParse(getCheckedRadioButton(groupBoxState).Text, out state);

         WorkInProgressFilter wip;
         Enum.TryParse(getCheckedRadioButton(groupBoxWIP).Text, out wip);

         gitlabClient client = new gitlabClient(textBoxHost.Text, textBoxAccessToken.Text);
         var mergeRequests = client.GetAllMergeRequests(state, textBoxLabels.Text, textBoxAuthor.Text, wip);
         return mergeRequests;
      }

      void sendTrackedTimeSpan(ParsedMergeRequestUrl parsed, TimeSpan span)
      {
         gitlabClient client = new gitlabClient(parsed.Host, textBoxAccessToken.Text);
         client.AddSpentTimeForMergeRequest(parsed.Project, parsed.Id, ref span);
      }

      string initializeGitRepository(ParsedMergeRequestUrl parsed)
      {
         string localDir = textBoxLocalGitFolder.Text + "/" + parsed.Project.Split('/')[1];

         if (!Directory.Exists(localDir))
         {
            labelCurrentStatus.Text = "Cloning git repository...";
            gitClient.CloneRepo(parsed.Host, parsed.Project, localDir);
         }
         else
         {
            labelCurrentStatus.Text = "Fetching changes...";

            // TODO Add error handling etc
            string currentDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(localDir);
            gitClient.Fetch();
            Directory.SetCurrentDirectory(currentDir);
         }

         return localDir;
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
         return _connected;
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
               radioButtonSelectMR_Filter.Checked = false;
               textBoxLabels.Enabled = false;
               buttonSearchByLabel.Enabled = false;
               comboBoxFilteredMergeRequests.Enabled = false;
               break;

            case MrSelectingType.DirectURL:
               radioButtonSelectMR_URL.Checked = false;
               textBoxMrURL.Enabled = false;
               break;
         }
      }

      private void onLoadedListOfMergeRequests(List<MergeRequest> mergeRequests)
      {
         comboBoxFilteredMergeRequests.Items.Clear();
         foreach (var mergeRequest in mergeRequests)
         {
            comboBoxFilteredMergeRequests.Items.Add(
               mergeRequest.Title + "      " + "(" + mergeRequest.Author.Username + ")");
         }
         if (comboBoxFilteredMergeRequests.Items.Count > 0)
         {
            comboBoxFilteredMergeRequests.SelectedItem = 0;
         }
         _cachedMergeRequests = mergeRequests;
      }

      private void onStartConnectionProcess()
      {
         labelCurrentStatus.Text = "Connecting...";
         buttonConnect.Enabled = false;
      }

      private void onConnected(ParsedMergeRequestUrl parsed, MergeRequest mergeRequest,
         List<Commit> commits, string gitRepository)
      {
         _connected = true;
         _gitRepository = gitRepository;
         _parsedMergeRequestUrl = parsed;

         labelCurrentStatus.Text = "Connected to:";
         linkLabelConnectedTo.Visible = true;
         linkLabelConnectedTo.Text = mergeRequest.WebUrl;
         buttonConnect.Enabled = true;
         buttonConnect.Text = "Disconnect";

         foreach (Control item in groupBoxSelectMergeRequest.Controls)
         {
            item.Enabled = false;
         }
         tabPageDiff.Select();

         textBoxMergeRequestName.Text = mergeRequest.Title;
         richTextBoxMergeRequestDescription.Text = mergeRequest.Description;

         foreach (var commit in commits)
         {
            comboBoxLeftCommit.Items.Add(commit.ShortId + "   " + commit.CommitedDate.ToString("u"));
            comboBoxRightCommit.Items.Add(commit.ShortId + "   " + commit.CommitedDate.ToString("u"));
         }

         comboBoxLeftCommit.SelectedIndex = comboBoxLeftCommit.Items.Count - 1;

         comboBoxLeftCommit.Items.Add("origin/" + mergeRequest.SourceBranch);
         comboBoxRightCommit.Items.Add("origin/" + mergeRequest.SourceBranch);

         comboBoxLeftCommit.Items.Add("origin/" + mergeRequest.TargetBranch);
         comboBoxRightCommit.Items.Add("origin/" + mergeRequest.TargetBranch);

         comboBoxRightCommit.SelectedIndex = comboBoxRightCommit.Items.Count - 1;

         buttonStartTimer.Enabled = true;
         buttonDifftool.Enabled = true;
      }

      private void onDisconnected()
      {
         if (_isTrackingTime)
         {
            onStopTimer(false);
         }

         buttonDifftool.Enabled = false;
         buttonStartTimer.Enabled = false;

         comboBoxRightCommit.Items.Clear();
         comboBoxRightCommit.Text = null;
         comboBoxLeftCommit.Items.Clear();
         comboBoxLeftCommit.Text = null;

         foreach (Control item in groupBoxSelectMergeRequest.Controls)
         {
            item.Enabled = true;
         }
         tabPageDiff.Enabled = false;

         labelCurrentStatus.Text = "Not connected";
         linkLabelConnectedTo.Visible = false;
         linkLabelConnectedTo.Text = "";
         buttonConnect.Text = "Connect";

         _parsedMergeRequestUrl = new ParsedMergeRequestUrl();
         _gitRepository = null;
         _connected = false;
      }

      private void onStartTimer()
      {
         if (_timeTrackingTimer == null)
         {
            _timeTrackingTimer = new Timer();
         }

         buttonStartTimer.Text = buttonStartTimerTrackingText;
         labelSpentTime.Text = labelSpentTimeDefaultText;
         _lastStartTimeStamp = DateTime.Now;
         _timeTrackingTimer.Interval = timeTrackingTimerInterval;
         _timeTrackingTimer.Tick += new System.EventHandler(_oneSecondTimer_Tick);
         _timeTrackingTimer.Start();
         _isTrackingTime = true;
      }

      private void onStopTimer(bool sendTrackedTime)
      {
         var timeSpan = DateTime.Now - _lastStartTimeStamp;
         buttonStartTimer.Text = buttonStartTimerDefaultText;
         labelSpentTime.Text = labelSpentTimeDefaultText;
         _lastStartTimeStamp = DateTime.MinValue;
         _timeTrackingTimer.Stop();
         _isTrackingTime = false;

         if (sendTrackedTime)
         {
            sendTrackedTimeSpan(_parsedMergeRequestUrl, timeSpan);
         }
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

      private List<MergeRequest> _cachedMergeRequests;

      private bool _connected;
      private ParsedMergeRequestUrl _parsedMergeRequestUrl;
      private string _gitRepository;
      private List<Commit> _cachedCommits;

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
}
