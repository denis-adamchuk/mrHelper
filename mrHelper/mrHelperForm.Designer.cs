namespace mrHelper
{
   partial class mrHelperForm
   {
      /// <summary>
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary>
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.components = new System.ComponentModel.Container();
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(mrHelperForm));
         this.groupBoxAuthorization = new System.Windows.Forms.GroupBox();
         this.textBoxAccessToken = new System.Windows.Forms.TextBox();
         this.labelAccessToken = new System.Windows.Forms.Label();
         this.textBoxHost = new System.Windows.Forms.TextBox();
         this.labelHost = new System.Windows.Forms.Label();
         this.groupBoxSelectMergeRequest = new System.Windows.Forms.GroupBox();
         this.radioButtonSelectMR_Filter = new System.Windows.Forms.RadioButton();
         this.radioButtonSelectMR_URL = new System.Windows.Forms.RadioButton();
         this.buttonSearchByLabel = new System.Windows.Forms.Button();
         this.comboBoxFilteredMergeRequests = new System.Windows.Forms.ComboBox();
         this.labelAuthor = new System.Windows.Forms.Label();
         this.textBoxMrURL = new System.Windows.Forms.TextBox();
         this.textBoxAuthor = new System.Windows.Forms.TextBox();
         this.textBoxLabels = new System.Windows.Forms.TextBox();
         this.labelLabel = new System.Windows.Forms.Label();
         this.groupBoxState = new System.Windows.Forms.GroupBox();
         this.radioButtonState_All = new System.Windows.Forms.RadioButton();
         this.radioButtonState_Closed = new System.Windows.Forms.RadioButton();
         this.radioButtonState_Merged = new System.Windows.Forms.RadioButton();
         this.radioButtonState_Open = new System.Windows.Forms.RadioButton();
         this.groupBoxWIP = new System.Windows.Forms.GroupBox();
         this.radioButtonWIP_All = new System.Windows.Forms.RadioButton();
         this.radioButtonWIP_No = new System.Windows.Forms.RadioButton();
         this.radioButtonWIP_Yes = new System.Windows.Forms.RadioButton();
         this.labelSpentTimeLabel = new System.Windows.Forms.Label();
         this.buttonToggleTimer = new System.Windows.Forms.Button();
         this.buttonConnect = new System.Windows.Forms.Button();
         this.buttonBrowseLocalGitFolder = new System.Windows.Forms.Button();
         this.textBoxLocalGitFolder = new System.Windows.Forms.TextBox();
         this.labelLocalGitFolder = new System.Windows.Forms.Label();
         this.buttonDifftool = new System.Windows.Forms.Button();
         this.labelRight = new System.Windows.Forms.Label();
         this.comboBoxRightCommit = new System.Windows.Forms.ComboBox();
         this.labeLeft = new System.Windows.Forms.Label();
         this.comboBoxLeftCommit = new System.Windows.Forms.ComboBox();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.linkLabelSeeDescriptionRight = new System.Windows.Forms.LinkLabel();
         this.linkLabelSeeDescriptionLeft = new System.Windows.Forms.LinkLabel();
         this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.restoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
         this.localGitFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
         this.tabControl = new System.Windows.Forms.TabControl();
         this.tabPageSettings = new System.Windows.Forms.TabPage();
         this.groupBoxGit = new System.Windows.Forms.GroupBox();
         this.tabPageMR = new System.Windows.Forms.TabPage();
         this.linkLabelConnectedTo = new System.Windows.Forms.LinkLabel();
         this.labelCurrentStatus = new System.Windows.Forms.Label();
         this.tabPageDiff = new System.Windows.Forms.TabPage();
         this.labelSpentTime = new System.Windows.Forms.Label();
         this.groupBoxDescription = new System.Windows.Forms.GroupBox();
         this.richTextBoxMergeRequestDescription = new System.Windows.Forms.RichTextBox();
         this.textBoxMergeRequestName = new System.Windows.Forms.TextBox();
         this.groupBoxAuthorization.SuspendLayout();
         this.groupBoxSelectMergeRequest.SuspendLayout();
         this.groupBoxState.SuspendLayout();
         this.groupBoxWIP.SuspendLayout();
         this.contextMenuStrip.SuspendLayout();
         this.tabControl.SuspendLayout();
         this.tabPageSettings.SuspendLayout();
         this.groupBoxGit.SuspendLayout();
         this.tabPageMR.SuspendLayout();
         this.tabPageDiff.SuspendLayout();
         this.groupBoxDescription.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxAuthorization
         // 
         this.groupBoxAuthorization.Controls.Add(this.textBoxAccessToken);
         this.groupBoxAuthorization.Controls.Add(this.labelAccessToken);
         this.groupBoxAuthorization.Controls.Add(this.textBoxHost);
         this.groupBoxAuthorization.Controls.Add(this.labelHost);
         this.groupBoxAuthorization.Location = new System.Drawing.Point(6, 6);
         this.groupBoxAuthorization.Name = "groupBoxAuthorization";
         this.groupBoxAuthorization.Size = new System.Drawing.Size(456, 68);
         this.groupBoxAuthorization.TabIndex = 0;
         this.groupBoxAuthorization.TabStop = false;
         this.groupBoxAuthorization.Text = "Authorization";
         // 
         // textBoxAccessToken
         // 
         this.textBoxAccessToken.Location = new System.Drawing.Point(259, 32);
         this.textBoxAccessToken.Name = "textBoxAccessToken";
         this.textBoxAccessToken.Size = new System.Drawing.Size(191, 20);
         this.textBoxAccessToken.TabIndex = 1;
         // 
         // labelAccessToken
         // 
         this.labelAccessToken.AutoSize = true;
         this.labelAccessToken.Location = new System.Drawing.Point(256, 16);
         this.labelAccessToken.Name = "labelAccessToken";
         this.labelAccessToken.Size = new System.Drawing.Size(73, 13);
         this.labelAccessToken.TabIndex = 2;
         this.labelAccessToken.Text = "AccessToken";
         // 
         // textBoxHost
         // 
         this.textBoxHost.Location = new System.Drawing.Point(6, 32);
         this.textBoxHost.Name = "textBoxHost";
         this.textBoxHost.Size = new System.Drawing.Size(191, 20);
         this.textBoxHost.TabIndex = 0;
         // 
         // labelHost
         // 
         this.labelHost.AutoSize = true;
         this.labelHost.Location = new System.Drawing.Point(6, 16);
         this.labelHost.Name = "labelHost";
         this.labelHost.Size = new System.Drawing.Size(29, 13);
         this.labelHost.TabIndex = 1;
         this.labelHost.Text = "Host";
         // 
         // groupBoxSelectMergeRequest
         // 
         this.groupBoxSelectMergeRequest.Controls.Add(this.radioButtonSelectMR_Filter);
         this.groupBoxSelectMergeRequest.Controls.Add(this.radioButtonSelectMR_URL);
         this.groupBoxSelectMergeRequest.Controls.Add(this.buttonSearchByLabel);
         this.groupBoxSelectMergeRequest.Controls.Add(this.comboBoxFilteredMergeRequests);
         this.groupBoxSelectMergeRequest.Controls.Add(this.labelAuthor);
         this.groupBoxSelectMergeRequest.Controls.Add(this.textBoxMrURL);
         this.groupBoxSelectMergeRequest.Controls.Add(this.textBoxAuthor);
         this.groupBoxSelectMergeRequest.Controls.Add(this.textBoxLabels);
         this.groupBoxSelectMergeRequest.Controls.Add(this.labelLabel);
         this.groupBoxSelectMergeRequest.Controls.Add(this.groupBoxState);
         this.groupBoxSelectMergeRequest.Controls.Add(this.groupBoxWIP);
         this.groupBoxSelectMergeRequest.Location = new System.Drawing.Point(6, 6);
         this.groupBoxSelectMergeRequest.Name = "groupBoxSelectMergeRequest";
         this.groupBoxSelectMergeRequest.Size = new System.Drawing.Size(456, 230);
         this.groupBoxSelectMergeRequest.TabIndex = 1;
         this.groupBoxSelectMergeRequest.TabStop = false;
         this.groupBoxSelectMergeRequest.Text = "Select Merge Request";
         // 
         // radioButtonSelectMR_Filter
         // 
         this.radioButtonSelectMR_Filter.AutoSize = true;
         this.radioButtonSelectMR_Filter.Checked = true;
         this.radioButtonSelectMR_Filter.Location = new System.Drawing.Point(6, 19);
         this.radioButtonSelectMR_Filter.Name = "radioButtonSelectMR_Filter";
         this.radioButtonSelectMR_Filter.Size = new System.Drawing.Size(98, 17);
         this.radioButtonSelectMR_Filter.TabIndex = 25;
         this.radioButtonSelectMR_Filter.TabStop = true;
         this.radioButtonSelectMR_Filter.Text = "Search by Filter";
         this.radioButtonSelectMR_Filter.UseVisualStyleBackColor = true;
         // 
         // radioButtonSelectMR_URL
         // 
         this.radioButtonSelectMR_URL.AutoSize = true;
         this.radioButtonSelectMR_URL.Location = new System.Drawing.Point(6, 181);
         this.radioButtonSelectMR_URL.Name = "radioButtonSelectMR_URL";
         this.radioButtonSelectMR_URL.Size = new System.Drawing.Size(78, 17);
         this.radioButtonSelectMR_URL.TabIndex = 26;
         this.radioButtonSelectMR_URL.Text = "Direct URL";
         this.radioButtonSelectMR_URL.UseVisualStyleBackColor = true;
         // 
         // buttonSearchByLabel
         // 
         this.buttonSearchByLabel.Location = new System.Drawing.Point(367, 98);
         this.buttonSearchByLabel.Name = "buttonSearchByLabel";
         this.buttonSearchByLabel.Size = new System.Drawing.Size(83, 27);
         this.buttonSearchByLabel.TabIndex = 10;
         this.buttonSearchByLabel.Text = "Search";
         this.toolTip.SetToolTip(this.buttonSearchByLabel, "Searches in all projects at the host specified at Settings tab");
         this.buttonSearchByLabel.UseVisualStyleBackColor = true;
         this.buttonSearchByLabel.Click += new System.EventHandler(this.ButtonSearchByLabel_Click);
         // 
         // comboBoxFilteredMergeRequests
         // 
         this.comboBoxFilteredMergeRequests.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxFilteredMergeRequests.FormattingEnabled = true;
         this.comboBoxFilteredMergeRequests.Location = new System.Drawing.Point(6, 144);
         this.comboBoxFilteredMergeRequests.Name = "comboBoxFilteredMergeRequests";
         this.comboBoxFilteredMergeRequests.Size = new System.Drawing.Size(444, 21);
         this.comboBoxFilteredMergeRequests.TabIndex = 8;
         this.comboBoxFilteredMergeRequests.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxFilteredMergeRequests_Format);
         // 
         // labelAuthor
         // 
         this.labelAuthor.AutoSize = true;
         this.labelAuthor.Location = new System.Drawing.Point(6, 86);
         this.labelAuthor.Name = "labelAuthor";
         this.labelAuthor.Size = new System.Drawing.Size(38, 13);
         this.labelAuthor.TabIndex = 13;
         this.labelAuthor.Text = "Author";
         // 
         // textBoxMrURL
         // 
         this.textBoxMrURL.ForeColor = System.Drawing.Color.Black;
         this.textBoxMrURL.Location = new System.Drawing.Point(6, 204);
         this.textBoxMrURL.Name = "textBoxMrURL";
         this.textBoxMrURL.Size = new System.Drawing.Size(444, 20);
         this.textBoxMrURL.TabIndex = 2;
         this.toolTip.SetToolTip(this.textBoxMrURL, "Something like https://gitlab-server.com/group/project/merge_requests/2");
         // 
         // textBoxAuthor
         // 
         this.textBoxAuthor.Location = new System.Drawing.Point(6, 102);
         this.textBoxAuthor.Name = "textBoxAuthor";
         this.textBoxAuthor.ReadOnly = true;
         this.textBoxAuthor.Size = new System.Drawing.Size(140, 20);
         this.textBoxAuthor.TabIndex = 11;
         this.toolTip.SetToolTip(this.textBoxAuthor, "Not implemented yet");
         // 
         // textBoxLabels
         // 
         this.textBoxLabels.Location = new System.Drawing.Point(6, 57);
         this.textBoxLabels.Name = "textBoxLabels";
         this.textBoxLabels.Size = new System.Drawing.Size(140, 20);
         this.textBoxLabels.TabIndex = 9;
         this.toolTip.SetToolTip(this.textBoxLabels, "Return merge requests matching a comma separated list of labels");
         // 
         // labelLabel
         // 
         this.labelLabel.AutoSize = true;
         this.labelLabel.Location = new System.Drawing.Point(6, 39);
         this.labelLabel.Name = "labelLabel";
         this.labelLabel.Size = new System.Drawing.Size(33, 13);
         this.labelLabel.TabIndex = 12;
         this.labelLabel.Text = "Label";
         // 
         // groupBoxState
         // 
         this.groupBoxState.Controls.Add(this.radioButtonState_All);
         this.groupBoxState.Controls.Add(this.radioButtonState_Closed);
         this.groupBoxState.Controls.Add(this.radioButtonState_Merged);
         this.groupBoxState.Controls.Add(this.radioButtonState_Open);
         this.groupBoxState.Location = new System.Drawing.Point(163, 39);
         this.groupBoxState.Name = "groupBoxState";
         this.groupBoxState.Size = new System.Drawing.Size(255, 45);
         this.groupBoxState.TabIndex = 23;
         this.groupBoxState.TabStop = false;
         this.groupBoxState.Text = "State";
         // 
         // radioButtonState_All
         // 
         this.radioButtonState_All.AutoSize = true;
         this.radioButtonState_All.Checked = true;
         this.radioButtonState_All.Location = new System.Drawing.Point(206, 13);
         this.radioButtonState_All.Name = "radioButtonState_All";
         this.radioButtonState_All.Size = new System.Drawing.Size(36, 17);
         this.radioButtonState_All.TabIndex = 22;
         this.radioButtonState_All.TabStop = true;
         this.radioButtonState_All.Text = "All";
         this.radioButtonState_All.UseVisualStyleBackColor = true;
         // 
         // radioButtonState_Closed
         // 
         this.radioButtonState_Closed.AutoSize = true;
         this.radioButtonState_Closed.Location = new System.Drawing.Point(76, 13);
         this.radioButtonState_Closed.Name = "radioButtonState_Closed";
         this.radioButtonState_Closed.Size = new System.Drawing.Size(57, 17);
         this.radioButtonState_Closed.TabIndex = 21;
         this.radioButtonState_Closed.Text = "Closed";
         this.radioButtonState_Closed.UseVisualStyleBackColor = true;
         // 
         // radioButtonState_Merged
         // 
         this.radioButtonState_Merged.AutoSize = true;
         this.radioButtonState_Merged.Location = new System.Drawing.Point(139, 13);
         this.radioButtonState_Merged.Name = "radioButtonState_Merged";
         this.radioButtonState_Merged.Size = new System.Drawing.Size(61, 17);
         this.radioButtonState_Merged.TabIndex = 20;
         this.radioButtonState_Merged.Text = "Merged";
         this.radioButtonState_Merged.UseVisualStyleBackColor = true;
         // 
         // radioButtonState_Open
         // 
         this.radioButtonState_Open.AutoSize = true;
         this.radioButtonState_Open.Location = new System.Drawing.Point(13, 14);
         this.radioButtonState_Open.Name = "radioButtonState_Open";
         this.radioButtonState_Open.Size = new System.Drawing.Size(51, 17);
         this.radioButtonState_Open.TabIndex = 19;
         this.radioButtonState_Open.Text = "Open";
         this.radioButtonState_Open.UseVisualStyleBackColor = true;
         // 
         // groupBoxWIP
         // 
         this.groupBoxWIP.Controls.Add(this.radioButtonWIP_All);
         this.groupBoxWIP.Controls.Add(this.radioButtonWIP_No);
         this.groupBoxWIP.Controls.Add(this.radioButtonWIP_Yes);
         this.groupBoxWIP.Location = new System.Drawing.Point(163, 87);
         this.groupBoxWIP.Name = "groupBoxWIP";
         this.groupBoxWIP.Size = new System.Drawing.Size(165, 47);
         this.groupBoxWIP.TabIndex = 24;
         this.groupBoxWIP.TabStop = false;
         this.groupBoxWIP.Text = "WIP";
         // 
         // radioButtonWIP_All
         // 
         this.radioButtonWIP_All.AutoSize = true;
         this.radioButtonWIP_All.Checked = true;
         this.radioButtonWIP_All.Location = new System.Drawing.Point(111, 15);
         this.radioButtonWIP_All.Name = "radioButtonWIP_All";
         this.radioButtonWIP_All.Size = new System.Drawing.Size(36, 17);
         this.radioButtonWIP_All.TabIndex = 25;
         this.radioButtonWIP_All.TabStop = true;
         this.radioButtonWIP_All.Text = "All";
         this.radioButtonWIP_All.UseVisualStyleBackColor = true;
         // 
         // radioButtonWIP_No
         // 
         this.radioButtonWIP_No.AutoSize = true;
         this.radioButtonWIP_No.Location = new System.Drawing.Point(66, 15);
         this.radioButtonWIP_No.Name = "radioButtonWIP_No";
         this.radioButtonWIP_No.Size = new System.Drawing.Size(39, 17);
         this.radioButtonWIP_No.TabIndex = 24;
         this.radioButtonWIP_No.Text = "No";
         this.radioButtonWIP_No.UseVisualStyleBackColor = true;
         // 
         // radioButtonWIP_Yes
         // 
         this.radioButtonWIP_Yes.AutoSize = true;
         this.radioButtonWIP_Yes.Location = new System.Drawing.Point(17, 15);
         this.radioButtonWIP_Yes.Name = "radioButtonWIP_Yes";
         this.radioButtonWIP_Yes.Size = new System.Drawing.Size(43, 17);
         this.radioButtonWIP_Yes.TabIndex = 23;
         this.radioButtonWIP_Yes.Text = "Yes";
         this.radioButtonWIP_Yes.UseVisualStyleBackColor = true;
         // 
         // labelSpentTimeLabel
         // 
         this.labelSpentTimeLabel.AutoSize = true;
         this.labelSpentTimeLabel.Location = new System.Drawing.Point(331, 246);
         this.labelSpentTimeLabel.Name = "labelSpentTimeLabel";
         this.labelSpentTimeLabel.Size = new System.Drawing.Size(64, 13);
         this.labelSpentTimeLabel.TabIndex = 4;
         this.labelSpentTimeLabel.Text = "Spent Time:";
         // 
         // buttonToggleTimer
         // 
         this.buttonToggleTimer.Enabled = false;
         this.buttonToggleTimer.Location = new System.Drawing.Point(242, 239);
         this.buttonToggleTimer.Name = "buttonToggleTimer";
         this.buttonToggleTimer.Size = new System.Drawing.Size(83, 27);
         this.buttonToggleTimer.TabIndex = 4;
         this.buttonToggleTimer.UseVisualStyleBackColor = true;
         this.buttonToggleTimer.Click += new System.EventHandler(this.ButtonToggleTimer_Click);
         // 
         // buttonConnect
         // 
         this.buttonConnect.Location = new System.Drawing.Point(373, 242);
         this.buttonConnect.Name = "buttonConnect";
         this.buttonConnect.Size = new System.Drawing.Size(83, 27);
         this.buttonConnect.TabIndex = 3;
         this.buttonConnect.Text = "Connect";
         this.buttonConnect.UseVisualStyleBackColor = true;
         this.buttonConnect.Click += new System.EventHandler(this.ButtonConnect_Click);
         // 
         // buttonBrowseLocalGitFolder
         // 
         this.buttonBrowseLocalGitFolder.Location = new System.Drawing.Point(367, 31);
         this.buttonBrowseLocalGitFolder.Name = "buttonBrowseLocalGitFolder";
         this.buttonBrowseLocalGitFolder.Size = new System.Drawing.Size(83, 27);
         this.buttonBrowseLocalGitFolder.TabIndex = 5;
         this.buttonBrowseLocalGitFolder.Text = "Browse...";
         this.buttonBrowseLocalGitFolder.UseVisualStyleBackColor = true;
         this.buttonBrowseLocalGitFolder.Click += new System.EventHandler(this.ButtonBrowseLocalGitFolder_Click);
         // 
         // textBoxLocalGitFolder
         // 
         this.textBoxLocalGitFolder.Location = new System.Drawing.Point(6, 35);
         this.textBoxLocalGitFolder.Name = "textBoxLocalGitFolder";
         this.textBoxLocalGitFolder.ReadOnly = true;
         this.textBoxLocalGitFolder.Size = new System.Drawing.Size(341, 20);
         this.textBoxLocalGitFolder.TabIndex = 9;
         this.textBoxLocalGitFolder.TabStop = false;
         // 
         // labelLocalGitFolder
         // 
         this.labelLocalGitFolder.AutoSize = true;
         this.labelLocalGitFolder.Location = new System.Drawing.Point(6, 19);
         this.labelLocalGitFolder.Name = "labelLocalGitFolder";
         this.labelLocalGitFolder.Size = new System.Drawing.Size(139, 13);
         this.labelLocalGitFolder.TabIndex = 8;
         this.labelLocalGitFolder.Text = "Local folder for git repository";
         // 
         // buttonDifftool
         // 
         this.buttonDifftool.Enabled = false;
         this.buttonDifftool.Location = new System.Drawing.Point(9, 239);
         this.buttonDifftool.Name = "buttonDifftool";
         this.buttonDifftool.Size = new System.Drawing.Size(83, 27);
         this.buttonDifftool.TabIndex = 8;
         this.buttonDifftool.Text = "Diff Tool";
         this.buttonDifftool.UseVisualStyleBackColor = true;
         this.buttonDifftool.Click += new System.EventHandler(this.ButtonDifftool_Click);
         // 
         // labelRight
         // 
         this.labelRight.AutoSize = true;
         this.labelRight.Location = new System.Drawing.Point(239, 170);
         this.labelRight.Name = "labelRight";
         this.labelRight.Size = new System.Drawing.Size(32, 13);
         this.labelRight.TabIndex = 3;
         this.labelRight.Text = "Right";
         // 
         // comboBoxRightCommit
         // 
         this.comboBoxRightCommit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxRightCommit.FormattingEnabled = true;
         this.comboBoxRightCommit.Location = new System.Drawing.Point(242, 186);
         this.comboBoxRightCommit.Name = "comboBoxRightCommit";
         this.comboBoxRightCommit.Size = new System.Drawing.Size(223, 21);
         this.comboBoxRightCommit.TabIndex = 7;
         this.comboBoxRightCommit.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxRightCommit_Format);
         // 
         // labeLeft
         // 
         this.labeLeft.AutoSize = true;
         this.labeLeft.Location = new System.Drawing.Point(9, 170);
         this.labeLeft.Name = "labeLeft";
         this.labeLeft.Size = new System.Drawing.Size(25, 13);
         this.labeLeft.TabIndex = 1;
         this.labeLeft.Text = "Left";
         // 
         // comboBoxLeftCommit
         // 
         this.comboBoxLeftCommit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxLeftCommit.FormattingEnabled = true;
         this.comboBoxLeftCommit.Location = new System.Drawing.Point(9, 186);
         this.comboBoxLeftCommit.Name = "comboBoxLeftCommit";
         this.comboBoxLeftCommit.Size = new System.Drawing.Size(223, 21);
         this.comboBoxLeftCommit.TabIndex = 6;
         this.comboBoxLeftCommit.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxLeftCommit_Format);
         // 
         // toolTip
         // 
         this.toolTip.AutoPopDelay = 10000;
         this.toolTip.InitialDelay = 10;
         this.toolTip.ReshowDelay = 10;
         // 
         // linkLabelSeeDescriptionRight
         // 
         this.linkLabelSeeDescriptionRight.AutoSize = true;
         this.linkLabelSeeDescriptionRight.Location = new System.Drawing.Point(377, 210);
         this.linkLabelSeeDescriptionRight.Name = "linkLabelSeeDescriptionRight";
         this.linkLabelSeeDescriptionRight.Size = new System.Drawing.Size(82, 13);
         this.linkLabelSeeDescriptionRight.TabIndex = 13;
         this.linkLabelSeeDescriptionRight.TabStop = true;
         this.linkLabelSeeDescriptionRight.Text = "See Description";
         this.toolTip.SetToolTip(this.linkLabelSeeDescriptionRight, "Not implemented yet");
         // 
         // linkLabelSeeDescriptionLeft
         // 
         this.linkLabelSeeDescriptionLeft.AutoSize = true;
         this.linkLabelSeeDescriptionLeft.Location = new System.Drawing.Point(124, 210);
         this.linkLabelSeeDescriptionLeft.Name = "linkLabelSeeDescriptionLeft";
         this.linkLabelSeeDescriptionLeft.Size = new System.Drawing.Size(82, 13);
         this.linkLabelSeeDescriptionLeft.TabIndex = 12;
         this.linkLabelSeeDescriptionLeft.TabStop = true;
         this.linkLabelSeeDescriptionLeft.Text = "See Description";
         this.toolTip.SetToolTip(this.linkLabelSeeDescriptionLeft, "Not implemented yet");
         // 
         // contextMenuStrip
         // 
         this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.restoreToolStripMenuItem,
            this.exitToolStripMenuItem});
         this.contextMenuStrip.Name = "contextMenuStrip1";
         this.contextMenuStrip.Size = new System.Drawing.Size(114, 48);
         // 
         // restoreToolStripMenuItem
         // 
         this.restoreToolStripMenuItem.Name = "restoreToolStripMenuItem";
         this.restoreToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
         this.restoreToolStripMenuItem.Text = "Restore";
         this.restoreToolStripMenuItem.Click += new System.EventHandler(this.RestoreToolStripMenuItem_Click);
         // 
         // exitToolStripMenuItem
         // 
         this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
         this.exitToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
         this.exitToolStripMenuItem.Text = "Exit";
         this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
         // 
         // notifyIcon
         // 
         this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
         this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
         this.notifyIcon.Text = "Merge Request Helper";
         this.notifyIcon.Visible = true;
         this.notifyIcon.DoubleClick += new System.EventHandler(this.NotifyIcon_DoubleClick);
         // 
         // localGitFolderBrowser
         // 
         this.localGitFolderBrowser.Description = "Select a folder where git repository will be stored locally";
         this.localGitFolderBrowser.RootFolder = System.Environment.SpecialFolder.MyComputer;
         // 
         // tabControl
         // 
         this.tabControl.Controls.Add(this.tabPageSettings);
         this.tabControl.Controls.Add(this.tabPageMR);
         this.tabControl.Controls.Add(this.tabPageDiff);
         this.tabControl.Location = new System.Drawing.Point(9, 12);
         this.tabControl.Name = "tabControl";
         this.tabControl.SelectedIndex = 0;
         this.tabControl.Size = new System.Drawing.Size(476, 325);
         this.tabControl.TabIndex = 11;
         // 
         // tabPageSettings
         // 
         this.tabPageSettings.Controls.Add(this.groupBoxGit);
         this.tabPageSettings.Controls.Add(this.groupBoxAuthorization);
         this.tabPageSettings.Location = new System.Drawing.Point(4, 22);
         this.tabPageSettings.Name = "tabPageSettings";
         this.tabPageSettings.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSettings.Size = new System.Drawing.Size(468, 299);
         this.tabPageSettings.TabIndex = 0;
         this.tabPageSettings.Text = "Settings";
         this.tabPageSettings.UseVisualStyleBackColor = true;
         // 
         // groupBoxGit
         // 
         this.groupBoxGit.Controls.Add(this.buttonBrowseLocalGitFolder);
         this.groupBoxGit.Controls.Add(this.labelLocalGitFolder);
         this.groupBoxGit.Controls.Add(this.textBoxLocalGitFolder);
         this.groupBoxGit.Location = new System.Drawing.Point(6, 80);
         this.groupBoxGit.Name = "groupBoxGit";
         this.groupBoxGit.Size = new System.Drawing.Size(456, 69);
         this.groupBoxGit.TabIndex = 1;
         this.groupBoxGit.TabStop = false;
         this.groupBoxGit.Text = "git";
         // 
         // tabPageMR
         // 
         this.tabPageMR.Controls.Add(this.linkLabelConnectedTo);
         this.tabPageMR.Controls.Add(this.labelCurrentStatus);
         this.tabPageMR.Controls.Add(this.buttonConnect);
         this.tabPageMR.Controls.Add(this.groupBoxSelectMergeRequest);
         this.tabPageMR.Location = new System.Drawing.Point(4, 22);
         this.tabPageMR.Name = "tabPageMR";
         this.tabPageMR.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageMR.Size = new System.Drawing.Size(468, 299);
         this.tabPageMR.TabIndex = 1;
         this.tabPageMR.Text = "Merge Requests";
         this.tabPageMR.UseVisualStyleBackColor = true;
         // 
         // linkLabelConnectedTo
         // 
         this.linkLabelConnectedTo.AutoSize = true;
         this.linkLabelConnectedTo.Location = new System.Drawing.Point(6, 273);
         this.linkLabelConnectedTo.Name = "linkLabelConnectedTo";
         this.linkLabelConnectedTo.Size = new System.Drawing.Size(42, 13);
         this.linkLabelConnectedTo.TabIndex = 5;
         this.linkLabelConnectedTo.TabStop = true;
         this.linkLabelConnectedTo.Text = "url-here";
         this.linkLabelConnectedTo.Visible = false;
         // 
         // labelCurrentStatus
         // 
         this.labelCurrentStatus.AutoSize = true;
         this.labelCurrentStatus.Location = new System.Drawing.Point(3, 249);
         this.labelCurrentStatus.Name = "labelCurrentStatus";
         this.labelCurrentStatus.Size = new System.Drawing.Size(78, 13);
         this.labelCurrentStatus.TabIndex = 4;
         this.labelCurrentStatus.Text = "Not connected";
         // 
         // tabPageDiff
         // 
         this.tabPageDiff.Controls.Add(this.labelSpentTime);
         this.tabPageDiff.Controls.Add(this.labelSpentTimeLabel);
         this.tabPageDiff.Controls.Add(this.linkLabelSeeDescriptionRight);
         this.tabPageDiff.Controls.Add(this.linkLabelSeeDescriptionLeft);
         this.tabPageDiff.Controls.Add(this.groupBoxDescription);
         this.tabPageDiff.Controls.Add(this.buttonToggleTimer);
         this.tabPageDiff.Controls.Add(this.comboBoxLeftCommit);
         this.tabPageDiff.Controls.Add(this.comboBoxRightCommit);
         this.tabPageDiff.Controls.Add(this.labelRight);
         this.tabPageDiff.Controls.Add(this.buttonDifftool);
         this.tabPageDiff.Controls.Add(this.labeLeft);
         this.tabPageDiff.Location = new System.Drawing.Point(4, 22);
         this.tabPageDiff.Name = "tabPageDiff";
         this.tabPageDiff.Size = new System.Drawing.Size(468, 299);
         this.tabPageDiff.TabIndex = 2;
         this.tabPageDiff.Text = "Diff";
         this.tabPageDiff.UseVisualStyleBackColor = true;
         // 
         // labelSpentTime
         // 
         this.labelSpentTime.AutoSize = true;
         this.labelSpentTime.Location = new System.Drawing.Point(401, 246);
         this.labelSpentTime.Name = "labelSpentTime";
         this.labelSpentTime.Size = new System.Drawing.Size(49, 13);
         this.labelSpentTime.TabIndex = 14;
         this.labelSpentTime.Text = "00:00:00";
         // 
         // groupBoxDescription
         // 
         this.groupBoxDescription.Controls.Add(this.richTextBoxMergeRequestDescription);
         this.groupBoxDescription.Controls.Add(this.textBoxMergeRequestName);
         this.groupBoxDescription.Location = new System.Drawing.Point(3, 3);
         this.groupBoxDescription.Name = "groupBoxDescription";
         this.groupBoxDescription.Size = new System.Drawing.Size(462, 162);
         this.groupBoxDescription.TabIndex = 11;
         this.groupBoxDescription.TabStop = false;
         this.groupBoxDescription.Text = "Merge Request";
         // 
         // richTextBoxMergeRequestDescription
         // 
         this.richTextBoxMergeRequestDescription.Location = new System.Drawing.Point(6, 45);
         this.richTextBoxMergeRequestDescription.Name = "richTextBoxMergeRequestDescription";
         this.richTextBoxMergeRequestDescription.ReadOnly = true;
         this.richTextBoxMergeRequestDescription.Size = new System.Drawing.Size(450, 110);
         this.richTextBoxMergeRequestDescription.TabIndex = 12;
         this.richTextBoxMergeRequestDescription.Text = "";
         // 
         // textBoxMergeRequestName
         // 
         this.textBoxMergeRequestName.Location = new System.Drawing.Point(6, 19);
         this.textBoxMergeRequestName.Name = "textBoxMergeRequestName";
         this.textBoxMergeRequestName.ReadOnly = true;
         this.textBoxMergeRequestName.Size = new System.Drawing.Size(450, 20);
         this.textBoxMergeRequestName.TabIndex = 10;
         // 
         // mrHelperForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(495, 349);
         this.Controls.Add(this.tabControl);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "mrHelperForm";
         this.Text = "Merge Requests Helper";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MrHelperForm_FormClosing);
         this.Load += new System.EventHandler(this.MrHelperForm_Load);
         this.groupBoxAuthorization.ResumeLayout(false);
         this.groupBoxAuthorization.PerformLayout();
         this.groupBoxSelectMergeRequest.ResumeLayout(false);
         this.groupBoxSelectMergeRequest.PerformLayout();
         this.groupBoxState.ResumeLayout(false);
         this.groupBoxState.PerformLayout();
         this.groupBoxWIP.ResumeLayout(false);
         this.groupBoxWIP.PerformLayout();
         this.contextMenuStrip.ResumeLayout(false);
         this.tabControl.ResumeLayout(false);
         this.tabPageSettings.ResumeLayout(false);
         this.groupBoxGit.ResumeLayout(false);
         this.groupBoxGit.PerformLayout();
         this.tabPageMR.ResumeLayout(false);
         this.tabPageMR.PerformLayout();
         this.tabPageDiff.ResumeLayout(false);
         this.tabPageDiff.PerformLayout();
         this.groupBoxDescription.ResumeLayout(false);
         this.groupBoxDescription.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxAuthorization;
      private System.Windows.Forms.TextBox textBoxAccessToken;
      private System.Windows.Forms.Label labelAccessToken;
      private System.Windows.Forms.Label labelHost;
      private System.Windows.Forms.TextBox textBoxHost;
      private System.Windows.Forms.GroupBox groupBoxSelectMergeRequest;
      private System.Windows.Forms.Button buttonConnect;
      private System.Windows.Forms.TextBox textBoxMrURL;
      private System.Windows.Forms.Button buttonDifftool;
      private System.Windows.Forms.Label labelRight;
      private System.Windows.Forms.ComboBox comboBoxRightCommit;
      private System.Windows.Forms.Label labeLeft;
      private System.Windows.Forms.ComboBox comboBoxLeftCommit;
      private System.Windows.Forms.Button buttonToggleTimer;
      private System.Windows.Forms.Label labelSpentTimeLabel;
      private System.Windows.Forms.ToolTip toolTip;
      private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
      private System.Windows.Forms.ToolStripMenuItem restoreToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
      private System.Windows.Forms.NotifyIcon notifyIcon;
      private System.Windows.Forms.Label labelLocalGitFolder;
      private System.Windows.Forms.FolderBrowserDialog localGitFolderBrowser;
      private System.Windows.Forms.Button buttonBrowseLocalGitFolder;
      private System.Windows.Forms.TextBox textBoxLocalGitFolder;
      private System.Windows.Forms.Button buttonSearchByLabel;
      private System.Windows.Forms.TextBox textBoxLabels;
      private System.Windows.Forms.ComboBox comboBoxFilteredMergeRequests;
      private System.Windows.Forms.Label labelAuthor;
      private System.Windows.Forms.TextBox textBoxAuthor;
      private System.Windows.Forms.Label labelLabel;
      private System.Windows.Forms.TabControl tabControl;
      private System.Windows.Forms.TabPage tabPageSettings;
      private System.Windows.Forms.GroupBox groupBoxGit;
      private System.Windows.Forms.TabPage tabPageMR;
      private System.Windows.Forms.TabPage tabPageDiff;
      private System.Windows.Forms.LinkLabel linkLabelSeeDescriptionRight;
      private System.Windows.Forms.LinkLabel linkLabelSeeDescriptionLeft;
      private System.Windows.Forms.GroupBox groupBoxDescription;
      private System.Windows.Forms.RichTextBox richTextBoxMergeRequestDescription;
      private System.Windows.Forms.TextBox textBoxMergeRequestName;
      private System.Windows.Forms.Label labelSpentTime;
      private System.Windows.Forms.GroupBox groupBoxState;
      private System.Windows.Forms.GroupBox groupBoxWIP;
      private System.Windows.Forms.RadioButton radioButtonState_All;
      private System.Windows.Forms.RadioButton radioButtonState_Closed;
      private System.Windows.Forms.RadioButton radioButtonState_Merged;
      private System.Windows.Forms.RadioButton radioButtonState_Open;
      private System.Windows.Forms.RadioButton radioButtonWIP_All;
      private System.Windows.Forms.RadioButton radioButtonWIP_No;
      private System.Windows.Forms.RadioButton radioButtonWIP_Yes;
      private System.Windows.Forms.RadioButton radioButtonSelectMR_Filter;
      private System.Windows.Forms.RadioButton radioButtonSelectMR_URL;
      private System.Windows.Forms.LinkLabel linkLabelConnectedTo;
      private System.Windows.Forms.Label labelCurrentStatus;
   }
}

