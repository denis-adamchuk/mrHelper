using mrHelper.CommonControls;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
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
         _checkForUpdatesTimer?.Dispose();
         _updateManager?.Dispose();
         _gitClientFactory?.Dispose();
         _timeTrackingTimer?.Dispose();
         _workflow?.Dispose();
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
         this.groupBoxKnownHosts = new System.Windows.Forms.GroupBox();
         this.buttonRemoveKnownHost = new System.Windows.Forms.Button();
         this.buttonAddKnownHost = new System.Windows.Forms.Button();
         this.listViewKnownHosts = new System.Windows.Forms.ListView();
         this.columnHeaderHost = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderAccessToken = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.buttonBrowseLocalGitFolder = new System.Windows.Forms.Button();
         this.textBoxLocalGitFolder = new System.Windows.Forms.TextBox();
         this.labelLocalGitFolder = new System.Windows.Forms.Label();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.comboBoxDCDepth = new System.Windows.Forms.ComboBox();
         this.textBoxLabels = new System.Windows.Forms.TextBox();
         this.buttonEditTime = new System.Windows.Forms.Button();
         this.buttonAddComment = new System.Windows.Forms.Button();
         this.buttonDiscussions = new System.Windows.Forms.Button();
         this.buttonNewDiscussion = new System.Windows.Forms.Button();
         this.buttonDiffTool = new System.Windows.Forms.Button();
         this.linkLabelSendFeedback = new System.Windows.Forms.LinkLabel();
         this.linkLabelNewVersion = new System.Windows.Forms.LinkLabel();
         this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.restoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
         this.localGitFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
         this.tabControl = new System.Windows.Forms.TabControl();
         this.tabPageSettings = new System.Windows.Forms.TabPage();
         this.groupBoxOther = new System.Windows.Forms.GroupBox();
         this.comboBoxColorSchemes = new System.Windows.Forms.ComboBox();
         this.labelColorScheme = new System.Windows.Forms.Label();
         this.labelDepth = new System.Windows.Forms.Label();
         this.checkBoxMinimizeOnClose = new System.Windows.Forms.CheckBox();
         this.groupBoxGit = new System.Windows.Forms.GroupBox();
         this.groupBoxHost = new System.Windows.Forms.GroupBox();
         this.comboBoxHost = new mrHelper.CommonControls.SelectionPreservingComboBox();
         this.tabPageMR = new System.Windows.Forms.TabPage();
         this.splitContainer1 = new System.Windows.Forms.SplitContainer();
         this.groupBoxSelectMergeRequest = new System.Windows.Forms.GroupBox();
         this.buttonReloadList = new System.Windows.Forms.Button();
         this.listViewMergeRequests = new mrHelper.CommonControls.ListViewEx();
         this.columnHeaderIId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderAuthor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderLabels = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderJira = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.checkBoxLabels = new System.Windows.Forms.CheckBox();
         this.panel2 = new System.Windows.Forms.Panel();
         this.linkLabelAbortGit = new System.Windows.Forms.LinkLabel();
         this.labelGitStatus = new System.Windows.Forms.Label();
         this.labelWorkflowStatus = new System.Windows.Forms.Label();
         this.groupBoxTimeTracking = new System.Windows.Forms.GroupBox();
         this.labelTimeTrackingMergeRequestName = new System.Windows.Forms.Label();
         this.labelTimeTrackingTrackedTime = new System.Windows.Forms.Label();
         this.labelTimeTrackingTrackedLabel = new System.Windows.Forms.Label();
         this.buttonTimeTrackingCancel = new System.Windows.Forms.Button();
         this.buttonTimeTrackingStart = new System.Windows.Forms.Button();
         this.panel1 = new System.Windows.Forms.Panel();
         this.groupBoxReview = new System.Windows.Forms.GroupBox();
         this.groupBoxActions = new System.Windows.Forms.GroupBox();
         this.panel3 = new System.Windows.Forms.Panel();
         this.groupBox3 = new System.Windows.Forms.GroupBox();
         this.comboBoxRightCommit = new mrHelper.CommonControls.SelectionPreservingComboBox();
         this.comboBoxLeftCommit = new mrHelper.CommonControls.SelectionPreservingComboBox();
         this.groupBox2 = new System.Windows.Forms.GroupBox();
         this.linkLabelConnectedTo = new System.Windows.Forms.LinkLabel();
         this.richTextBoxMergeRequestDescription = new System.Windows.Forms.RichTextBox();
         this.groupBoxKnownHosts.SuspendLayout();
         this.contextMenuStrip.SuspendLayout();
         this.tabControl.SuspendLayout();
         this.tabPageSettings.SuspendLayout();
         this.groupBoxOther.SuspendLayout();
         this.groupBoxGit.SuspendLayout();
         this.groupBoxHost.SuspendLayout();
         this.tabPageMR.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
         this.splitContainer1.Panel1.SuspendLayout();
         this.splitContainer1.Panel2.SuspendLayout();
         this.splitContainer1.SuspendLayout();
         this.groupBoxSelectMergeRequest.SuspendLayout();
         this.panel2.SuspendLayout();
         this.groupBoxTimeTracking.SuspendLayout();
         this.panel1.SuspendLayout();
         this.groupBoxReview.SuspendLayout();
         this.panel3.SuspendLayout();
         this.groupBox3.SuspendLayout();
         this.groupBox2.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxKnownHosts
         // 
         this.groupBoxKnownHosts.Controls.Add(this.buttonRemoveKnownHost);
         this.groupBoxKnownHosts.Controls.Add(this.buttonAddKnownHost);
         this.groupBoxKnownHosts.Controls.Add(this.listViewKnownHosts);
         this.groupBoxKnownHosts.Location = new System.Drawing.Point(6, 6);
         this.groupBoxKnownHosts.Name = "groupBoxKnownHosts";
         this.groupBoxKnownHosts.Size = new System.Drawing.Size(513, 135);
         this.groupBoxKnownHosts.TabIndex = 0;
         this.groupBoxKnownHosts.TabStop = false;
         this.groupBoxKnownHosts.Text = "Known Hosts";
         // 
         // buttonRemoveKnownHost
         // 
         this.buttonRemoveKnownHost.Location = new System.Drawing.Point(409, 72);
         this.buttonRemoveKnownHost.Name = "buttonRemoveKnownHost";
         this.buttonRemoveKnownHost.Size = new System.Drawing.Size(83, 27);
         this.buttonRemoveKnownHost.TabIndex = 3;
         this.buttonRemoveKnownHost.Text = "Remove";
         this.toolTip.SetToolTip(this.buttonRemoveKnownHost, "Remove a selected host from the list of known hosts");
         this.buttonRemoveKnownHost.UseVisualStyleBackColor = true;
         this.buttonRemoveKnownHost.Click += new System.EventHandler(this.ButtonRemoveKnownHost_Click);
         // 
         // buttonAddKnownHost
         // 
         this.buttonAddKnownHost.Location = new System.Drawing.Point(409, 39);
         this.buttonAddKnownHost.Name = "buttonAddKnownHost";
         this.buttonAddKnownHost.Size = new System.Drawing.Size(83, 27);
         this.buttonAddKnownHost.TabIndex = 2;
         this.buttonAddKnownHost.Text = "Add...";
         this.toolTip.SetToolTip(this.buttonAddKnownHost, "Add a host with Access Token to the list of known hosts");
         this.buttonAddKnownHost.UseVisualStyleBackColor = true;
         this.buttonAddKnownHost.Click += new System.EventHandler(this.ButtonAddKnownHost_Click);
         // 
         // listViewKnownHosts
         // 
         this.listViewKnownHosts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderHost,
            this.columnHeaderAccessToken});
         this.listViewKnownHosts.FullRowSelect = true;
         this.listViewKnownHosts.HideSelection = false;
         this.listViewKnownHosts.Location = new System.Drawing.Point(9, 19);
         this.listViewKnownHosts.MultiSelect = false;
         this.listViewKnownHosts.Name = "listViewKnownHosts";
         this.listViewKnownHosts.Size = new System.Drawing.Size(366, 110);
         this.listViewKnownHosts.TabIndex = 1;
         this.listViewKnownHosts.UseCompatibleStateImageBehavior = false;
         this.listViewKnownHosts.View = System.Windows.Forms.View.Details;
         // 
         // columnHeaderHost
         // 
         this.columnHeaderHost.Text = "Host";
         this.columnHeaderHost.Width = 180;
         // 
         // columnHeaderAccessToken
         // 
         this.columnHeaderAccessToken.Text = "AccessToken";
         this.columnHeaderAccessToken.Width = 180;
         // 
         // buttonBrowseLocalGitFolder
         // 
         this.buttonBrowseLocalGitFolder.Location = new System.Drawing.Point(409, 31);
         this.buttonBrowseLocalGitFolder.Name = "buttonBrowseLocalGitFolder";
         this.buttonBrowseLocalGitFolder.Size = new System.Drawing.Size(83, 27);
         this.buttonBrowseLocalGitFolder.TabIndex = 4;
         this.buttonBrowseLocalGitFolder.Text = "Browse...";
         this.toolTip.SetToolTip(this.buttonBrowseLocalGitFolder, "Select a folder where repositories will be stored");
         this.buttonBrowseLocalGitFolder.UseVisualStyleBackColor = true;
         this.buttonBrowseLocalGitFolder.Click += new System.EventHandler(this.ButtonBrowseLocalGitFolder_Click);
         // 
         // textBoxLocalGitFolder
         // 
         this.textBoxLocalGitFolder.Location = new System.Drawing.Point(6, 35);
         this.textBoxLocalGitFolder.Name = "textBoxLocalGitFolder";
         this.textBoxLocalGitFolder.ReadOnly = true;
         this.textBoxLocalGitFolder.Size = new System.Drawing.Size(375, 20);
         this.textBoxLocalGitFolder.TabIndex = 1;
         this.textBoxLocalGitFolder.TabStop = false;
         this.toolTip.SetToolTip(this.textBoxLocalGitFolder, "A folder where repositories are stored");
         // 
         // labelLocalGitFolder
         // 
         this.labelLocalGitFolder.AutoSize = true;
         this.labelLocalGitFolder.Location = new System.Drawing.Point(6, 19);
         this.labelLocalGitFolder.Name = "labelLocalGitFolder";
         this.labelLocalGitFolder.Size = new System.Drawing.Size(152, 13);
         this.labelLocalGitFolder.TabIndex = 8;
         this.labelLocalGitFolder.Text = "Parent folder for git repositories";
         // 
         // toolTip
         // 
         this.toolTip.AutoPopDelay = 5000;
         this.toolTip.InitialDelay = 500;
         this.toolTip.ReshowDelay = 100;
         // 
         // comboBoxDCDepth
         // 
         this.comboBoxDCDepth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxDCDepth.FormattingEnabled = true;
         this.comboBoxDCDepth.Items.AddRange(new object[] {
            "0",
            "1",
            "2",
            "3",
            "4"});
         this.comboBoxDCDepth.Location = new System.Drawing.Point(106, 44);
         this.comboBoxDCDepth.Name = "comboBoxDCDepth";
         this.comboBoxDCDepth.Size = new System.Drawing.Size(58, 21);
         this.comboBoxDCDepth.TabIndex = 8;
         this.toolTip.SetToolTip(this.comboBoxDCDepth, "Number of lines under the line the discussion was created for.");
         this.comboBoxDCDepth.SelectedIndexChanged += new System.EventHandler(this.comboBoxDCDepth_SelectedIndexChanged);
         // 
         // textBoxLabels
         // 
         this.textBoxLabels.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxLabels.Enabled = false;
         this.textBoxLabels.Location = new System.Drawing.Point(82, 17);
         this.textBoxLabels.Name = "textBoxLabels";
         this.textBoxLabels.Size = new System.Drawing.Size(709, 20);
         this.textBoxLabels.TabIndex = 1;
         this.toolTip.SetToolTip(this.textBoxLabels, "To select merge requests use comma-separated list of the following: #{username} o" +
        "r @{username} or {username}");
         this.textBoxLabels.TextChanged += new System.EventHandler(this.textBoxLabels_TextChanged);
         this.textBoxLabels.Leave += new System.EventHandler(this.TextBoxLabels_LostFocus);
         // 
         // buttonEditTime
         // 
         this.buttonEditTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonEditTime.Enabled = false;
         this.buttonEditTime.Location = new System.Drawing.Point(630, 19);
         this.buttonEditTime.Name = "buttonEditTime";
         this.buttonEditTime.Size = new System.Drawing.Size(96, 32);
         this.buttonEditTime.TabIndex = 4;
         this.buttonEditTime.Text = "Edit";
         this.toolTip.SetToolTip(this.buttonEditTime, "Edit total time tracked on this merge request");
         this.buttonEditTime.UseVisualStyleBackColor = true;
         this.buttonEditTime.Click += new System.EventHandler(this.ButtonTimeEdit_Click);
         // 
         // buttonAddComment
         // 
         this.buttonAddComment.Enabled = false;
         this.buttonAddComment.Location = new System.Drawing.Point(210, 19);
         this.buttonAddComment.Name = "buttonAddComment";
         this.buttonAddComment.Size = new System.Drawing.Size(96, 32);
         this.buttonAddComment.TabIndex = 2;
         this.buttonAddComment.Text = "Add comment";
         this.toolTip.SetToolTip(this.buttonAddComment, "Leave a comment (cannot be resolved and replied)");
         this.buttonAddComment.UseVisualStyleBackColor = true;
         this.buttonAddComment.Click += new System.EventHandler(this.ButtonAddComment_Click);
         // 
         // buttonDiscussions
         // 
         this.buttonDiscussions.Enabled = false;
         this.buttonDiscussions.Location = new System.Drawing.Point(108, 19);
         this.buttonDiscussions.Name = "buttonDiscussions";
         this.buttonDiscussions.Size = new System.Drawing.Size(96, 32);
         this.buttonDiscussions.TabIndex = 1;
         this.buttonDiscussions.Text = "Discussions";
         this.toolTip.SetToolTip(this.buttonDiscussions, "Show full list of Discussions");
         this.buttonDiscussions.UseVisualStyleBackColor = true;
         this.buttonDiscussions.Click += new System.EventHandler(this.ButtonDiscussions_Click);
         // 
         // buttonNewDiscussion
         // 
         this.buttonNewDiscussion.Enabled = false;
         this.buttonNewDiscussion.Location = new System.Drawing.Point(6, 19);
         this.buttonNewDiscussion.Name = "buttonNewDiscussion";
         this.buttonNewDiscussion.Size = new System.Drawing.Size(96, 32);
         this.buttonNewDiscussion.TabIndex = 0;
         this.buttonNewDiscussion.Text = "New discussion";
         this.toolTip.SetToolTip(this.buttonNewDiscussion, "Create a new resolvable discussion");
         this.buttonNewDiscussion.UseVisualStyleBackColor = true;
         this.buttonNewDiscussion.Click += new System.EventHandler(this.ButtonNewDiscussion_Click);
         // 
         // buttonDiffTool
         // 
         this.buttonDiffTool.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonDiffTool.Enabled = false;
         this.buttonDiffTool.Location = new System.Drawing.Point(630, 19);
         this.buttonDiffTool.Name = "buttonDiffTool";
         this.buttonDiffTool.Size = new System.Drawing.Size(96, 48);
         this.buttonDiffTool.TabIndex = 2;
         this.buttonDiffTool.Text = "Diff tool";
         this.toolTip.SetToolTip(this.buttonDiffTool, "Launch diff tool to review diff between selected commits");
         this.buttonDiffTool.UseVisualStyleBackColor = true;
         this.buttonDiffTool.Click += new System.EventHandler(this.ButtonDifftool_Click);
         // 
         // linkLabelSendFeedback
         // 
         this.linkLabelSendFeedback.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelSendFeedback.AutoSize = true;
         this.linkLabelSendFeedback.Location = new System.Drawing.Point(630, 10);
         this.linkLabelSendFeedback.Name = "linkLabelSendFeedback";
         this.linkLabelSendFeedback.Size = new System.Drawing.Size(80, 13);
         this.linkLabelSendFeedback.TabIndex = 6;
         this.linkLabelSendFeedback.TabStop = true;
         this.linkLabelSendFeedback.Text = "Send feedback";
         this.toolTip.SetToolTip(this.linkLabelSendFeedback, "Report a bug or suggestion to author. Logs are attached automatically.");
         this.linkLabelSendFeedback.Visible = false;
         this.linkLabelSendFeedback.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelSendFeedback_LinkClicked);
         // 
         // linkLabelNewVersion
         // 
         this.linkLabelNewVersion.AutoSize = true;
         this.linkLabelNewVersion.Location = new System.Drawing.Point(6, 10);
         this.linkLabelNewVersion.Name = "linkLabelNewVersion";
         this.linkLabelNewVersion.Size = new System.Drawing.Size(226, 13);
         this.linkLabelNewVersion.TabIndex = 5;
         this.linkLabelNewVersion.TabStop = true;
         this.linkLabelNewVersion.Text = "New version is available! Click here to install it.";
         this.toolTip.SetToolTip(this.linkLabelNewVersion, "New version is already downloaded. Click to install it.");
         this.linkLabelNewVersion.Visible = false;
         this.linkLabelNewVersion.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelNewVersion_LinkClicked);
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
         this.restoreToolStripMenuItem.Click += new System.EventHandler(this.NotifyIcon_DoubleClick);
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
         this.notifyIcon.BalloonTipText = "I will now live in your tray";
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
         this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tabControl.Location = new System.Drawing.Point(0, 0);
         this.tabControl.Name = "tabControl";
         this.tabControl.SelectedIndex = 0;
         this.tabControl.Size = new System.Drawing.Size(1704, 890);
         this.tabControl.TabIndex = 0;
         // 
         // tabPageSettings
         // 
         this.tabPageSettings.Controls.Add(this.groupBoxOther);
         this.tabPageSettings.Controls.Add(this.groupBoxGit);
         this.tabPageSettings.Controls.Add(this.groupBoxKnownHosts);
         this.tabPageSettings.Controls.Add(this.groupBoxHost);
         this.tabPageSettings.Location = new System.Drawing.Point(4, 22);
         this.tabPageSettings.Name = "tabPageSettings";
         this.tabPageSettings.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSettings.Size = new System.Drawing.Size(1696, 864);
         this.tabPageSettings.TabIndex = 0;
         this.tabPageSettings.Text = "Settings";
         this.tabPageSettings.UseVisualStyleBackColor = true;
         // 
         // groupBoxOther
         // 
         this.groupBoxOther.Controls.Add(this.comboBoxColorSchemes);
         this.groupBoxOther.Controls.Add(this.labelColorScheme);
         this.groupBoxOther.Controls.Add(this.labelDepth);
         this.groupBoxOther.Controls.Add(this.comboBoxDCDepth);
         this.groupBoxOther.Controls.Add(this.checkBoxMinimizeOnClose);
         this.groupBoxOther.Location = new System.Drawing.Point(525, 12);
         this.groupBoxOther.Name = "groupBoxOther";
         this.groupBoxOther.Size = new System.Drawing.Size(513, 129);
         this.groupBoxOther.TabIndex = 2;
         this.groupBoxOther.TabStop = false;
         this.groupBoxOther.Text = "Other";
         // 
         // comboBoxColorSchemes
         // 
         this.comboBoxColorSchemes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxColorSchemes.FormattingEnabled = true;
         this.comboBoxColorSchemes.Location = new System.Drawing.Point(106, 77);
         this.comboBoxColorSchemes.Name = "comboBoxColorSchemes";
         this.comboBoxColorSchemes.Size = new System.Drawing.Size(159, 21);
         this.comboBoxColorSchemes.TabIndex = 9;
         this.comboBoxColorSchemes.SelectionChangeCommitted += new System.EventHandler(this.ComboBoxColorSchemes_SelectionChangeCommited);
         // 
         // labelColorScheme
         // 
         this.labelColorScheme.AutoSize = true;
         this.labelColorScheme.Location = new System.Drawing.Point(6, 80);
         this.labelColorScheme.Name = "labelColorScheme";
         this.labelColorScheme.Size = new System.Drawing.Size(71, 13);
         this.labelColorScheme.TabIndex = 8;
         this.labelColorScheme.Text = "Color scheme";
         // 
         // labelDepth
         // 
         this.labelDepth.AutoSize = true;
         this.labelDepth.Location = new System.Drawing.Point(6, 47);
         this.labelDepth.Name = "labelDepth";
         this.labelDepth.Size = new System.Drawing.Size(94, 13);
         this.labelDepth.TabIndex = 5;
         this.labelDepth.Text = "Diff Context Depth";
         // 
         // checkBoxMinimizeOnClose
         // 
         this.checkBoxMinimizeOnClose.AutoSize = true;
         this.checkBoxMinimizeOnClose.Location = new System.Drawing.Point(6, 19);
         this.checkBoxMinimizeOnClose.Name = "checkBoxMinimizeOnClose";
         this.checkBoxMinimizeOnClose.Size = new System.Drawing.Size(109, 17);
         this.checkBoxMinimizeOnClose.TabIndex = 7;
         this.checkBoxMinimizeOnClose.Text = "Minimize on close";
         this.checkBoxMinimizeOnClose.UseVisualStyleBackColor = true;
         this.checkBoxMinimizeOnClose.CheckedChanged += new System.EventHandler(this.CheckBoxMinimizeOnClose_CheckedChanged);
         // 
         // groupBoxGit
         // 
         this.groupBoxGit.Controls.Add(this.buttonBrowseLocalGitFolder);
         this.groupBoxGit.Controls.Add(this.labelLocalGitFolder);
         this.groupBoxGit.Controls.Add(this.textBoxLocalGitFolder);
         this.groupBoxGit.Location = new System.Drawing.Point(6, 147);
         this.groupBoxGit.Name = "groupBoxGit";
         this.groupBoxGit.Size = new System.Drawing.Size(513, 69);
         this.groupBoxGit.TabIndex = 1;
         this.groupBoxGit.TabStop = false;
         this.groupBoxGit.Text = "git";
         // 
         // groupBoxHost
         // 
         this.groupBoxHost.Controls.Add(this.comboBoxHost);
         this.groupBoxHost.Location = new System.Drawing.Point(525, 147);
         this.groupBoxHost.Name = "groupBoxHost";
         this.groupBoxHost.Size = new System.Drawing.Size(265, 69);
         this.groupBoxHost.TabIndex = 3;
         this.groupBoxHost.TabStop = false;
         this.groupBoxHost.Text = "Select Host";
         // 
         // comboBoxHost
         // 
         this.comboBoxHost.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxHost.FormattingEnabled = true;
         this.comboBoxHost.Location = new System.Drawing.Point(9, 31);
         this.comboBoxHost.Name = "comboBoxHost";
         this.comboBoxHost.Size = new System.Drawing.Size(250, 21);
         this.comboBoxHost.TabIndex = 0;
         this.comboBoxHost.SelectionChangeCommitted += new System.EventHandler(this.ComboBoxHost_SelectionChangeCommited);
         this.comboBoxHost.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxHost_Format);
         // 
         // tabPageMR
         // 
         this.tabPageMR.Controls.Add(this.splitContainer1);
         this.tabPageMR.Location = new System.Drawing.Point(4, 22);
         this.tabPageMR.Name = "tabPageMR";
         this.tabPageMR.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageMR.Size = new System.Drawing.Size(1696, 864);
         this.tabPageMR.TabIndex = 1;
         this.tabPageMR.Text = "Merge Requests";
         this.tabPageMR.UseVisualStyleBackColor = true;
         // 
         // splitContainer1
         // 
         this.splitContainer1.BackColor = System.Drawing.Color.Transparent;
         this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer1.Location = new System.Drawing.Point(3, 3);
         this.splitContainer1.Name = "splitContainer1";
         // 
         // splitContainer1.Panel1
         // 
         this.splitContainer1.Panel1.Controls.Add(this.groupBoxSelectMergeRequest);
         this.splitContainer1.Panel1MinSize = 720;
         // 
         // splitContainer1.Panel2
         // 
         this.splitContainer1.Panel2.Controls.Add(this.panel2);
         this.splitContainer1.Panel2.Controls.Add(this.groupBoxTimeTracking);
         this.splitContainer1.Panel2.Controls.Add(this.panel1);
         this.splitContainer1.Panel2.Controls.Add(this.panel3);
         this.splitContainer1.Panel2.Controls.Add(this.groupBox3);
         this.splitContainer1.Panel2.Controls.Add(this.groupBox2);
         this.splitContainer1.Panel2MinSize = 710;
         this.splitContainer1.Size = new System.Drawing.Size(1690, 858);
         this.splitContainer1.SplitterDistance = 953;
         this.splitContainer1.SplitterWidth = 8;
         this.splitContainer1.TabIndex = 0;
         // 
         // groupBoxSelectMergeRequest
         // 
         this.groupBoxSelectMergeRequest.Controls.Add(this.buttonReloadList);
         this.groupBoxSelectMergeRequest.Controls.Add(this.textBoxLabels);
         this.groupBoxSelectMergeRequest.Controls.Add(this.listViewMergeRequests);
         this.groupBoxSelectMergeRequest.Controls.Add(this.checkBoxLabels);
         this.groupBoxSelectMergeRequest.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSelectMergeRequest.Location = new System.Drawing.Point(0, 0);
         this.groupBoxSelectMergeRequest.Name = "groupBoxSelectMergeRequest";
         this.groupBoxSelectMergeRequest.Size = new System.Drawing.Size(953, 858);
         this.groupBoxSelectMergeRequest.TabIndex = 0;
         this.groupBoxSelectMergeRequest.TabStop = false;
         this.groupBoxSelectMergeRequest.Text = "Select Merge Request";
         // 
         // buttonReloadList
         // 
         this.buttonReloadList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonReloadList.Enabled = false;
         this.buttonReloadList.Location = new System.Drawing.Point(851, 9);
         this.buttonReloadList.Name = "buttonReloadList";
         this.buttonReloadList.Size = new System.Drawing.Size(96, 32);
         this.buttonReloadList.TabIndex = 3;
         this.buttonReloadList.Text = "Reload List";
         this.buttonReloadList.UseVisualStyleBackColor = true;
         this.buttonReloadList.Click += new System.EventHandler(this.ButtonReloadList_Click);
         // 
         // listViewMergeRequests
         // 
         this.listViewMergeRequests.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listViewMergeRequests.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderIId,
            this.columnHeaderAuthor,
            this.columnHeaderTitle,
            this.columnHeaderLabels,
            this.columnHeaderJira});
         this.listViewMergeRequests.FullRowSelect = true;
         this.listViewMergeRequests.GridLines = true;
         this.listViewMergeRequests.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
         this.listViewMergeRequests.HideSelection = false;
         this.listViewMergeRequests.Location = new System.Drawing.Point(6, 43);
         this.listViewMergeRequests.MultiSelect = false;
         this.listViewMergeRequests.Name = "listViewMergeRequests";
         this.listViewMergeRequests.OwnerDraw = true;
         this.listViewMergeRequests.Size = new System.Drawing.Size(941, 809);
         this.listViewMergeRequests.TabIndex = 2;
         this.listViewMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewMergeRequests.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.ListViewMergeRequests_DrawColumnHeader);
         this.listViewMergeRequests.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.ListViewMergeRequests_DrawSubItem);
         this.listViewMergeRequests.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.ListViewMergeRequests_ItemSelectionChanged);
         this.listViewMergeRequests.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ListViewMergeRequests_MouseDown);
         this.listViewMergeRequests.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ListViewMergeRequests_MouseMove);
         // 
         // columnHeaderIId
         // 
         this.columnHeaderIId.Text = "IId";
         this.columnHeaderIId.Width = 40;
         // 
         // columnHeaderAuthor
         // 
         this.columnHeaderAuthor.Text = "Author";
         this.columnHeaderAuthor.Width = 140;
         // 
         // columnHeaderTitle
         // 
         this.columnHeaderTitle.Text = "Title";
         this.columnHeaderTitle.Width = 400;
         // 
         // columnHeaderLabels
         // 
         this.columnHeaderLabels.Text = "Labels";
         this.columnHeaderLabels.Width = 265;
         // 
         // columnHeaderJira
         // 
         this.columnHeaderJira.Text = "Jira";
         this.columnHeaderJira.Width = 80;
         // 
         // checkBoxLabels
         // 
         this.checkBoxLabels.AutoSize = true;
         this.checkBoxLabels.Enabled = false;
         this.checkBoxLabels.Location = new System.Drawing.Point(6, 19);
         this.checkBoxLabels.Name = "checkBoxLabels";
         this.checkBoxLabels.Size = new System.Drawing.Size(48, 17);
         this.checkBoxLabels.TabIndex = 0;
         this.checkBoxLabels.Text = "Filter";
         this.checkBoxLabels.UseVisualStyleBackColor = true;
         this.checkBoxLabels.CheckedChanged += new System.EventHandler(this.CheckBoxLabels_CheckedChanged);
         // 
         // panel2
         // 
         this.panel2.BackColor = System.Drawing.Color.WhiteSmoke;
         this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.panel2.Controls.Add(this.linkLabelAbortGit);
         this.panel2.Controls.Add(this.labelGitStatus);
         this.panel2.Controls.Add(this.labelWorkflowStatus);
         this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.panel2.Location = new System.Drawing.Point(0, 768);
         this.panel2.Name = "panel2";
         this.panel2.Size = new System.Drawing.Size(729, 56);
         this.panel2.TabIndex = 4;
         // 
         // linkLabelAbortGit
         // 
         this.linkLabelAbortGit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelAbortGit.AutoSize = true;
         this.linkLabelAbortGit.Location = new System.Drawing.Point(678, 32);
         this.linkLabelAbortGit.Name = "linkLabelAbortGit";
         this.linkLabelAbortGit.Size = new System.Drawing.Size(32, 13);
         this.linkLabelAbortGit.TabIndex = 2;
         this.linkLabelAbortGit.TabStop = true;
         this.linkLabelAbortGit.Text = "Abort";
         this.linkLabelAbortGit.Visible = false;
         this.linkLabelAbortGit.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelAbortGit_LinkClicked);
         // 
         // labelGitStatus
         // 
         this.labelGitStatus.Location = new System.Drawing.Point(6, 32);
         this.labelGitStatus.Name = "labelGitStatus";
         this.labelGitStatus.Size = new System.Drawing.Size(510, 13);
         this.labelGitStatus.TabIndex = 1;
         this.labelGitStatus.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor in" +
    "cididunt ut labore et dolore";
         this.labelGitStatus.Visible = false;
         // 
         // labelWorkflowStatus
         // 
         this.labelWorkflowStatus.AutoEllipsis = true;
         this.labelWorkflowStatus.Location = new System.Drawing.Point(6, 9);
         this.labelWorkflowStatus.Name = "labelWorkflowStatus";
         this.labelWorkflowStatus.Size = new System.Drawing.Size(570, 13);
         this.labelWorkflowStatus.TabIndex = 0;
         this.labelWorkflowStatus.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor in" +
    "cididunt ut labore et dolore magna aliqua";
         // 
         // groupBoxTimeTracking
         // 
         this.groupBoxTimeTracking.Controls.Add(this.labelTimeTrackingMergeRequestName);
         this.groupBoxTimeTracking.Controls.Add(this.buttonEditTime);
         this.groupBoxTimeTracking.Controls.Add(this.labelTimeTrackingTrackedTime);
         this.groupBoxTimeTracking.Controls.Add(this.labelTimeTrackingTrackedLabel);
         this.groupBoxTimeTracking.Controls.Add(this.buttonTimeTrackingCancel);
         this.groupBoxTimeTracking.Controls.Add(this.buttonTimeTrackingStart);
         this.groupBoxTimeTracking.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxTimeTracking.Location = new System.Drawing.Point(0, 321);
         this.groupBoxTimeTracking.Name = "groupBoxTimeTracking";
         this.groupBoxTimeTracking.Size = new System.Drawing.Size(729, 83);
         this.groupBoxTimeTracking.TabIndex = 3;
         this.groupBoxTimeTracking.TabStop = false;
         this.groupBoxTimeTracking.Text = "Time tracking";
         // 
         // labelTimeTrackingMergeRequestName
         // 
         this.labelTimeTrackingMergeRequestName.AutoSize = true;
         this.labelTimeTrackingMergeRequestName.Location = new System.Drawing.Point(6, 54);
         this.labelTimeTrackingMergeRequestName.Name = "labelTimeTrackingMergeRequestName";
         this.labelTimeTrackingMergeRequestName.Size = new System.Drawing.Size(259, 13);
         this.labelTimeTrackingMergeRequestName.TabIndex = 5;
         this.labelTimeTrackingMergeRequestName.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";
         this.labelTimeTrackingMergeRequestName.Visible = false;
         // 
         // labelTimeTrackingTrackedTime
         // 
         this.labelTimeTrackingTrackedTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.labelTimeTrackingTrackedTime.AutoSize = true;
         this.labelTimeTrackingTrackedTime.Location = new System.Drawing.Point(570, 29);
         this.labelTimeTrackingTrackedTime.Name = "labelTimeTrackingTrackedTime";
         this.labelTimeTrackingTrackedTime.Size = new System.Drawing.Size(49, 13);
         this.labelTimeTrackingTrackedTime.TabIndex = 3;
         this.labelTimeTrackingTrackedTime.Text = "00:00:00";
         // 
         // labelTimeTrackingTrackedLabel
         // 
         this.labelTimeTrackingTrackedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.labelTimeTrackingTrackedLabel.AutoSize = true;
         this.labelTimeTrackingTrackedLabel.Location = new System.Drawing.Point(491, 29);
         this.labelTimeTrackingTrackedLabel.Name = "labelTimeTrackingTrackedLabel";
         this.labelTimeTrackingTrackedLabel.Size = new System.Drawing.Size(57, 13);
         this.labelTimeTrackingTrackedLabel.TabIndex = 2;
         this.labelTimeTrackingTrackedLabel.Text = "Total Time";
         // 
         // buttonTimeTrackingCancel
         // 
         this.buttonTimeTrackingCancel.Enabled = false;
         this.buttonTimeTrackingCancel.Location = new System.Drawing.Point(108, 19);
         this.buttonTimeTrackingCancel.Name = "buttonTimeTrackingCancel";
         this.buttonTimeTrackingCancel.Size = new System.Drawing.Size(96, 32);
         this.buttonTimeTrackingCancel.TabIndex = 1;
         this.buttonTimeTrackingCancel.Text = "Cancel";
         this.buttonTimeTrackingCancel.UseVisualStyleBackColor = true;
         this.buttonTimeTrackingCancel.Click += new System.EventHandler(this.ButtonTimeTrackingCancel_Click);
         // 
         // buttonTimeTrackingStart
         // 
         this.buttonTimeTrackingStart.Enabled = false;
         this.buttonTimeTrackingStart.Location = new System.Drawing.Point(6, 19);
         this.buttonTimeTrackingStart.Name = "buttonTimeTrackingStart";
         this.buttonTimeTrackingStart.Size = new System.Drawing.Size(96, 32);
         this.buttonTimeTrackingStart.TabIndex = 0;
         this.buttonTimeTrackingStart.Text = "Start Timer";
         this.buttonTimeTrackingStart.UseVisualStyleBackColor = true;
         this.buttonTimeTrackingStart.Click += new System.EventHandler(this.ButtonTimeTrackingStart_Click);
         // 
         // panel1
         // 
         this.panel1.Controls.Add(this.groupBoxReview);
         this.panel1.Controls.Add(this.groupBoxActions);
         this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
         this.panel1.Location = new System.Drawing.Point(0, 242);
         this.panel1.Name = "panel1";
         this.panel1.Size = new System.Drawing.Size(729, 79);
         this.panel1.TabIndex = 2;
         // 
         // groupBoxReview
         // 
         this.groupBoxReview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxReview.Controls.Add(this.buttonAddComment);
         this.groupBoxReview.Controls.Add(this.buttonDiscussions);
         this.groupBoxReview.Controls.Add(this.buttonNewDiscussion);
         this.groupBoxReview.Location = new System.Drawing.Point(420, 6);
         this.groupBoxReview.Name = "groupBoxReview";
         this.groupBoxReview.Size = new System.Drawing.Size(313, 63);
         this.groupBoxReview.TabIndex = 1;
         this.groupBoxReview.TabStop = false;
         this.groupBoxReview.Text = "Review";
         // 
         // groupBoxActions
         // 
         this.groupBoxActions.Location = new System.Drawing.Point(0, 6);
         this.groupBoxActions.Name = "groupBoxActions";
         this.groupBoxActions.Size = new System.Drawing.Size(225, 63);
         this.groupBoxActions.TabIndex = 0;
         this.groupBoxActions.TabStop = false;
         this.groupBoxActions.Text = "Actions";
         // 
         // panel3
         // 
         this.panel3.BackColor = System.Drawing.Color.WhiteSmoke;
         this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.panel3.Controls.Add(this.linkLabelSendFeedback);
         this.panel3.Controls.Add(this.linkLabelNewVersion);
         this.panel3.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.panel3.Location = new System.Drawing.Point(0, 824);
         this.panel3.Name = "panel3";
         this.panel3.Size = new System.Drawing.Size(729, 34);
         this.panel3.TabIndex = 6;
         // 
         // groupBox3
         // 
         this.groupBox3.Controls.Add(this.buttonDiffTool);
         this.groupBox3.Controls.Add(this.comboBoxRightCommit);
         this.groupBox3.Controls.Add(this.comboBoxLeftCommit);
         this.groupBox3.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBox3.Location = new System.Drawing.Point(0, 162);
         this.groupBox3.Name = "groupBox3";
         this.groupBox3.Size = new System.Drawing.Size(729, 80);
         this.groupBox3.TabIndex = 1;
         this.groupBox3.TabStop = false;
         this.groupBox3.Text = "Select commits";
         // 
         // comboBoxRightCommit
         // 
         this.comboBoxRightCommit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.comboBoxRightCommit.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
         this.comboBoxRightCommit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxRightCommit.FormattingEnabled = true;
         this.comboBoxRightCommit.Location = new System.Drawing.Point(6, 46);
         this.comboBoxRightCommit.Name = "comboBoxRightCommit";
         this.comboBoxRightCommit.Size = new System.Drawing.Size(619, 21);
         this.comboBoxRightCommit.TabIndex = 1;
         this.comboBoxRightCommit.SelectedIndexChanged += new System.EventHandler(this.ComboBoxRightCommit_SelectedIndexChanged);
         this.comboBoxRightCommit.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBoxCommits_DrawItem);
         // 
         // comboBoxLeftCommit
         // 
         this.comboBoxLeftCommit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.comboBoxLeftCommit.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
         this.comboBoxLeftCommit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxLeftCommit.FormattingEnabled = true;
         this.comboBoxLeftCommit.Location = new System.Drawing.Point(6, 19);
         this.comboBoxLeftCommit.Name = "comboBoxLeftCommit";
         this.comboBoxLeftCommit.Size = new System.Drawing.Size(619, 21);
         this.comboBoxLeftCommit.TabIndex = 0;
         this.comboBoxLeftCommit.SelectedIndexChanged += new System.EventHandler(this.ComboBoxLeftCommit_SelectedIndexChanged);
         this.comboBoxLeftCommit.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBoxCommits_DrawItem);
         // 
         // groupBox2
         // 
         this.groupBox2.Controls.Add(this.linkLabelConnectedTo);
         this.groupBox2.Controls.Add(this.richTextBoxMergeRequestDescription);
         this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBox2.Location = new System.Drawing.Point(0, 0);
         this.groupBox2.Name = "groupBox2";
         this.groupBox2.Size = new System.Drawing.Size(729, 162);
         this.groupBox2.TabIndex = 0;
         this.groupBox2.TabStop = false;
         this.groupBox2.Text = "Merge Request";
         // 
         // linkLabelConnectedTo
         // 
         this.linkLabelConnectedTo.AutoSize = true;
         this.linkLabelConnectedTo.Location = new System.Drawing.Point(6, 137);
         this.linkLabelConnectedTo.Name = "linkLabelConnectedTo";
         this.linkLabelConnectedTo.Size = new System.Drawing.Size(259, 13);
         this.linkLabelConnectedTo.TabIndex = 1;
         this.linkLabelConnectedTo.TabStop = true;
         this.linkLabelConnectedTo.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";
         this.linkLabelConnectedTo.Visible = false;
         this.linkLabelConnectedTo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelConnectedTo_LinkClicked);
         // 
         // richTextBoxMergeRequestDescription
         // 
         this.richTextBoxMergeRequestDescription.Dock = System.Windows.Forms.DockStyle.Top;
         this.richTextBoxMergeRequestDescription.Location = new System.Drawing.Point(3, 16);
         this.richTextBoxMergeRequestDescription.Name = "richTextBoxMergeRequestDescription";
         this.richTextBoxMergeRequestDescription.ReadOnly = true;
         this.richTextBoxMergeRequestDescription.Size = new System.Drawing.Size(723, 118);
         this.richTextBoxMergeRequestDescription.TabIndex = 0;
         this.richTextBoxMergeRequestDescription.Text = "";
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(1704, 890);
         this.Controls.Add(this.tabControl);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.Name = "MainForm";
         this.Text = "Merge Request Helper";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MrHelperForm_FormClosing);
         this.Load += new System.EventHandler(this.MrHelperForm_Load);
         this.groupBoxKnownHosts.ResumeLayout(false);
         this.contextMenuStrip.ResumeLayout(false);
         this.tabControl.ResumeLayout(false);
         this.tabPageSettings.ResumeLayout(false);
         this.groupBoxOther.ResumeLayout(false);
         this.groupBoxOther.PerformLayout();
         this.groupBoxGit.ResumeLayout(false);
         this.groupBoxGit.PerformLayout();
         this.groupBoxHost.ResumeLayout(false);
         this.tabPageMR.ResumeLayout(false);
         this.splitContainer1.Panel1.ResumeLayout(false);
         this.splitContainer1.Panel2.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
         this.splitContainer1.ResumeLayout(false);
         this.groupBoxSelectMergeRequest.ResumeLayout(false);
         this.groupBoxSelectMergeRequest.PerformLayout();
         this.panel2.ResumeLayout(false);
         this.panel2.PerformLayout();
         this.groupBoxTimeTracking.ResumeLayout(false);
         this.groupBoxTimeTracking.PerformLayout();
         this.panel1.ResumeLayout(false);
         this.groupBoxReview.ResumeLayout(false);
         this.panel3.ResumeLayout(false);
         this.panel3.PerformLayout();
         this.groupBox3.ResumeLayout(false);
         this.groupBox2.ResumeLayout(false);
         this.groupBox2.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxKnownHosts;
      private System.Windows.Forms.ToolTip toolTip;
      private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
      private System.Windows.Forms.ToolStripMenuItem restoreToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
      private System.Windows.Forms.NotifyIcon notifyIcon;
      private System.Windows.Forms.Label labelLocalGitFolder;
      private System.Windows.Forms.FolderBrowserDialog localGitFolderBrowser;
      private System.Windows.Forms.Button buttonBrowseLocalGitFolder;
      private System.Windows.Forms.TextBox textBoxLocalGitFolder;
      private System.Windows.Forms.TabControl tabControl;
      private System.Windows.Forms.TabPage tabPageSettings;
      private System.Windows.Forms.GroupBox groupBoxGit;
      private System.Windows.Forms.TabPage tabPageMR;
      private System.Windows.Forms.GroupBox groupBoxOther;
      private System.Windows.Forms.Button buttonRemoveKnownHost;
      private System.Windows.Forms.Button buttonAddKnownHost;
      private System.Windows.Forms.ListView listViewKnownHosts;
      private System.Windows.Forms.ColumnHeader columnHeaderHost;
      private System.Windows.Forms.ColumnHeader columnHeaderAccessToken;
      private System.Windows.Forms.Label labelDepth;
      private System.Windows.Forms.ComboBox comboBoxDCDepth;
      private System.Windows.Forms.CheckBox checkBoxMinimizeOnClose;
      private System.Windows.Forms.ComboBox comboBoxColorSchemes;
      private System.Windows.Forms.Label labelColorScheme;
      private System.Windows.Forms.GroupBox groupBoxHost;
      private System.Windows.Forms.SplitContainer splitContainer1;
      private System.Windows.Forms.GroupBox groupBoxSelectMergeRequest;
      private mrHelper.CommonControls.ListViewEx listViewMergeRequests;
      private System.Windows.Forms.ColumnHeader columnHeaderIId;
      private System.Windows.Forms.ColumnHeader columnHeaderAuthor;
      private System.Windows.Forms.ColumnHeader columnHeaderTitle;
      private System.Windows.Forms.ColumnHeader columnHeaderLabels;
      private System.Windows.Forms.ColumnHeader columnHeaderJira;
      private System.Windows.Forms.TextBox textBoxLabels;
      private System.Windows.Forms.CheckBox checkBoxLabels;
      private System.Windows.Forms.Panel panel1;
      private System.Windows.Forms.GroupBox groupBoxReview;
      private System.Windows.Forms.Button buttonAddComment;
      private System.Windows.Forms.Button buttonDiscussions;
      private System.Windows.Forms.Button buttonNewDiscussion;
      private System.Windows.Forms.GroupBox groupBoxActions;
      private System.Windows.Forms.GroupBox groupBox3;
      private System.Windows.Forms.GroupBox groupBox2;
      private System.Windows.Forms.Button buttonDiffTool;
      private SelectionPreservingComboBox comboBoxRightCommit;
      private SelectionPreservingComboBox comboBoxLeftCommit;
      private System.Windows.Forms.GroupBox groupBoxTimeTracking;
      private System.Windows.Forms.Button buttonEditTime;
      private System.Windows.Forms.Label labelTimeTrackingTrackedTime;
      private System.Windows.Forms.Label labelTimeTrackingTrackedLabel;
      private System.Windows.Forms.Button buttonTimeTrackingCancel;
      private System.Windows.Forms.Button buttonTimeTrackingStart;
      private System.Windows.Forms.Label labelTimeTrackingMergeRequestName;
      private System.Windows.Forms.Panel panel2;
      private System.Windows.Forms.LinkLabel linkLabelAbortGit;
      private System.Windows.Forms.Label labelGitStatus;
      private System.Windows.Forms.Label labelWorkflowStatus;
      private System.Windows.Forms.LinkLabel linkLabelConnectedTo;
      private System.Windows.Forms.RichTextBox richTextBoxMergeRequestDescription;
      private SelectionPreservingComboBox comboBoxHost;
      private System.Windows.Forms.Button buttonReloadList;
      private System.Windows.Forms.LinkLabel linkLabelNewVersion;
      private System.Windows.Forms.Panel panel3;
      private System.Windows.Forms.LinkLabel linkLabelSendFeedback;
   }
}

