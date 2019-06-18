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
      }
 
      private void MrHelperForm_FormClosing(object sender, FormClosingEventArgs e)
      {
         if (!_exiting)
         {
            hideToTray(e);
            return;
         }
         saveConfiguration();
      }

      private void NotifyIcon_DoubleClick(object sender, EventArgs e)
      {
         restoreWindow();
      }

      private void ButtonConnect_Click(object sender, EventArgs e)
      {
         if (isConnected())
         {
            onDisconnected();
            return;
         }

         onConnecting();

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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }

         onConnected(parsed, mergeRequest, commits, gitRepository);
      }

      private void ButtonDifftool_Click(object sender, EventArgs e)
      {
         // TODO Add error handling and work with TEMP directory
         Directory.SetCurrentDirectory(_gitRepository);
         gitClient.DiffTool(comboBoxLeftCommit.Text, comboBoxRightCommit.Text);
         Directory.SetCurrentDirectory("..");
      }

      private void ButtonStartTimer_Click(object sender, EventArgs e)
      {
         if (_isTrackingTime)
         {
            stopTimer(true);
         }
         else
         {
            startTimer();
         }
      }

      private void RestoreToolStripMenuItem_Click(object sender, EventArgs e)
      {
         restoreWindow();
      }

      private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
      {
         exit();
      }

      private void ButtonBrowseLocalGitFolder_Click(object sender, EventArgs e)
      {
         localGitFolderBrowser.SelectedPath = textBoxLocalGitFolder.Text;
         if (localGitFolderBrowser.ShowDialog() == DialogResult.OK)
         {
            textBoxLocalGitFolder.Text = localGitFolderBrowser.SelectedPath;
         }
      }

      private void stopTimer(bool sendTrackedTime)
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

      private void startTimer()
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

      private void _oneSecondTimer_Tick(object sender, EventArgs e)
      {
         // TODO Handle overflow
         var span = DateTime.Now - _lastStartTimeStamp;
         labelSpentTime.Text = span.ToString(@"hh\:mm\:ss");
      }

      private string getSelectedMergeRequestUrl()
      {
         if (false) // mode with multiple URL
         {
            // 1. get index of a selected row in the combo box
            // 2. return _cachedMergeRequests[index].WebUrl;
         }
         return textBoxMrURL.Text;
      }

      ParsedMergeRequestUrl parseAndValidateUrl(string url)
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

      MergeRequest getMergeRequest(ParsedMergeRequestUrl parsed)
      {
         gitlabClient client = new gitlabClient(parsed.Host, textBoxAccessToken.Text);
         var mergeRequest = client.GetSingleMergeRequest(parsed.Project, parsed.Id);
         return mergeRequest;
      }

      List<Commit> getCommits(ParsedMergeRequestUrl parsed)
      {
         gitlabClient client = new gitlabClient(parsed.Host, textBoxAccessToken.Text);
         var commits = client.GetMergeRequestCommits(parsed.Project, parsed.Id);
         return commits;
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
            gitClient.CloneRepo(parsed.Host, parsed.Project, localDir);
         }
         else
         {
            // TODO Add error handling etc
            string currentDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(localDir);
            gitClient.Fetch();
            Directory.SetCurrentDirectory(currentDir);
         }

         return localDir;
      }

      private bool isConnected()
      {
         return _connected;
      }

      private void onConnecting()
      {
         buttonConnect.Text = "Connecting...";
      }

      private void onConnected(ParsedMergeRequestUrl parsed, MergeRequest mergeRequest,
         List<Commit> commits, string gitRepository)
      {
         _connected = true;
         _gitRepository = gitRepository;
         _parsedMergeRequestUrl = parsed;

         buttonConnect.Text = "Disconnect";

         string tooltipText =
            "Source branch: " + mergeRequest.SourceBranch + "\n" +
            "Target branch: " + mergeRequest.TargetBranch + "\n" +
            "Title: "         + mergeRequest.Title + "\n" +
            "Description: "   + mergeRequest.Description;
         toolTipOnURL.SetToolTip(textBoxMrURL, tooltipText);

         foreach (var commit in commits)
         {
            comboBoxLeftCommit.Items.Add(commit.ShortId);
            comboBoxRightCommit.Items.Add(commit.ShortId);
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

      private void hideToTray(FormClosingEventArgs e)
      {
         e.Cancel = true;
         Hide();
         ShowInTaskbar = false;
      }

      private void restoreWindow()
      {
         ShowInTaskbar = true;
         Show();
      }

      private void exit()
      {
         _exiting = true;
         this.Close();
      }

      private void loadConfiguration()
      {
         _settings = new UserDefinedSettings();
         textBoxHost.Text = _settings.Host;
         textBoxAccessToken.Text = _settings.AccessToken;
         textBoxLocalGitFolder.Text = _settings.LocalGitFolder;
      }

      private void saveConfiguration()
      {
         _settings.Host = textBoxHost.Text;
         _settings.AccessToken = textBoxAccessToken.Text;
         _settings.LocalGitFolder = textBoxLocalGitFolder.Text;
         _settings.Update();
      }

      private void onDisconnected()
      {
         if (_isTrackingTime)
         {
            stopTimer(false);
         }

         buttonDifftool.Enabled = false;
         buttonStartTimer.Enabled = false;

         comboBoxRightCommit.Items.Clear();
         comboBoxRightCommit.Text = null;
         comboBoxLeftCommit.Items.Clear();
         comboBoxLeftCommit.Text = null;

         toolTipOnURL.RemoveAll();

         buttonConnect.Text = "Connect";

         _parsedMergeRequestUrl = new ParsedMergeRequestUrl();
         _gitRepository = null;
         _connected = false;
      }

      private List<MergeRequest> _cachedMergeRequests;

      private bool _connected;
      private ParsedMergeRequestUrl _parsedMergeRequestUrl;
      private string _gitRepository;

      private bool _isTrackingTime;
      private DateTime _lastStartTimeStamp;
      private Timer _timeTrackingTimer;

      private bool _exiting = false;

      UserDefinedSettings _settings;

      private void RadioButtonURL_CheckedChanged(object sender, EventArgs e)
      {
         radioButtonListMR.Checked = false;
         textBoxLabel.Enabled = false;
         buttonSearchByLabel.Enabled = false;
         comboBoxMrByLabel.Enabled = false;
      }

      private void RadioButtonListMR_CheckedChanged(object sender, EventArgs e)
      {
         radioButtonURL.Checked = false;
         textBoxMrURL.Enabled = false;
      }

      private void ButtonSearchByLabel_Click(object sender, EventArgs e)
      {
         getMergeRequests()
      }
   }
}
