using mrHelper.CommonControls.Controls;

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
         _discussionManager?.Dispose();
         _checkForUpdatesTimer?.Dispose();
         _mergeRequestManager?.Dispose();
         _timeTrackingTimer?.Dispose();
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
         this.buttonDiffTool = new System.Windows.Forms.Button();
         this.buttonAddComment = new System.Windows.Forms.Button();
         this.buttonDiscussions = new System.Windows.Forms.Button();
         this.buttonNewDiscussion = new System.Windows.Forms.Button();
         this.linkLabelHelp = new System.Windows.Forms.LinkLabel();
         this.linkLabelSendFeedback = new System.Windows.Forms.LinkLabel();
         this.linkLabelNewVersion = new System.Windows.Forms.LinkLabel();
         this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.restoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
         this.localGitFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
         this.tabControl = new System.Windows.Forms.TabControl();
         this.tabPageSettings = new System.Windows.Forms.TabPage();
         this.groupBoxNotifications = new System.Windows.Forms.GroupBox();
         this.checkBoxShowServiceNotifications = new System.Windows.Forms.CheckBox();
         this.checkBoxShowMyActivity = new System.Windows.Forms.CheckBox();
         this.checkBoxShowKeywords = new System.Windows.Forms.CheckBox();
         this.checkBoxShowOnMention = new System.Windows.Forms.CheckBox();
         this.checkBoxShowResolvedAll = new System.Windows.Forms.CheckBox();
         this.checkBoxShowUpdatedMergeRequests = new System.Windows.Forms.CheckBox();
         this.checkBoxShowMergedMergeRequests = new System.Windows.Forms.CheckBox();
         this.checkBoxShowNewMergeRequests = new System.Windows.Forms.CheckBox();
         this.groupBoxOther = new System.Windows.Forms.GroupBox();
         this.labelFontSize = new System.Windows.Forms.Label();
         this.comboBoxFonts = new System.Windows.Forms.ComboBox();
         this.comboBoxThemes = new System.Windows.Forms.ComboBox();
         this.labelVisualTheme = new System.Windows.Forms.Label();
         this.comboBoxColorSchemes = new System.Windows.Forms.ComboBox();
         this.labelColorScheme = new System.Windows.Forms.Label();
         this.labelDepth = new System.Windows.Forms.Label();
         this.checkBoxMinimizeOnClose = new System.Windows.Forms.CheckBox();
         this.groupBoxGit = new System.Windows.Forms.GroupBox();
         this.groupBoxHost = new System.Windows.Forms.GroupBox();
         this.buttonEditProjects = new System.Windows.Forms.Button();
         this.comboBoxHost = new mrHelper.CommonControls.Controls.SelectionPreservingComboBox();
         this.listViewProjects = new System.Windows.Forms.ListView();
         this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.tabPageMR = new System.Windows.Forms.TabPage();
         this.splitContainer1 = new System.Windows.Forms.SplitContainer();
         this.groupBoxSelectMergeRequest = new System.Windows.Forms.GroupBox();
         this.buttonReloadList = new System.Windows.Forms.Button();
         this.listViewMergeRequests = new mrHelper.CommonControls.Controls.ListViewEx();
         this.columnHeaderIId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderAuthor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderLabels = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderJira = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderTotalTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderSourceBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderTargetBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.checkBoxLabels = new System.Windows.Forms.CheckBox();
         this.splitContainer2 = new System.Windows.Forms.SplitContainer();
         this.groupBoxSelectedMR = new System.Windows.Forms.GroupBox();
         this.linkLabelConnectedTo = new System.Windows.Forms.LinkLabel();
         this.richTextBoxMergeRequestDescription = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.panelFreeSpace = new System.Windows.Forms.Panel();
         this.pictureBox2 = new System.Windows.Forms.PictureBox();
         this.pictureBox1 = new System.Windows.Forms.PictureBox();
         this.panelStatusBar = new System.Windows.Forms.Panel();
         this.linkLabelAbortGit = new System.Windows.Forms.LinkLabel();
         this.labelGitStatus = new System.Windows.Forms.Label();
         this.labelWorkflowStatus = new System.Windows.Forms.Label();
         this.panelBottomMenu = new System.Windows.Forms.Panel();
         this.groupBoxActions = new System.Windows.Forms.GroupBox();
         this.groupBoxTimeTracking = new System.Windows.Forms.GroupBox();
         this.labelTimeTrackingTrackedLabel = new System.Windows.Forms.Label();
         this.labelTimeTrackingMergeRequestName = new System.Windows.Forms.Label();
         this.buttonTimeTrackingCancel = new System.Windows.Forms.Button();
         this.buttonTimeTrackingStart = new System.Windows.Forms.Button();
         this.groupBoxReview = new System.Windows.Forms.GroupBox();
         this.groupBoxSelectCommits = new System.Windows.Forms.GroupBox();
         this.labelRightCommitTimestampLabel = new System.Windows.Forms.Label();
         this.comboBoxRightCommit = new mrHelper.CommonControls.Controls.SelectionPreservingComboBox();
         this.labelLeftCommitTimestampLabel = new System.Windows.Forms.Label();
         this.comboBoxLeftCommit = new mrHelper.CommonControls.Controls.SelectionPreservingComboBox();
         this.panel4 = new System.Windows.Forms.Panel();
         this.panel1 = new System.Windows.Forms.Panel();
         this.groupBoxKnownHosts.SuspendLayout();
         this.contextMenuStrip.SuspendLayout();
         this.tabControl.SuspendLayout();
         this.tabPageSettings.SuspendLayout();
         this.groupBoxNotifications.SuspendLayout();
         this.groupBoxOther.SuspendLayout();
         this.groupBoxGit.SuspendLayout();
         this.groupBoxHost.SuspendLayout();
         this.tabPageMR.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
         this.splitContainer1.Panel1.SuspendLayout();
         this.splitContainer1.Panel2.SuspendLayout();
         this.splitContainer1.SuspendLayout();
         this.groupBoxSelectMergeRequest.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
         this.splitContainer2.Panel1.SuspendLayout();
         this.splitContainer2.Panel2.SuspendLayout();
         this.splitContainer2.SuspendLayout();
         this.groupBoxSelectedMR.SuspendLayout();
         this.panelFreeSpace.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
         this.panelStatusBar.SuspendLayout();
         this.panelBottomMenu.SuspendLayout();
         this.groupBoxTimeTracking.SuspendLayout();
         this.groupBoxReview.SuspendLayout();
         this.groupBoxSelectCommits.SuspendLayout();
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
         this.buttonRemoveKnownHost.Location = new System.Drawing.Point(424, 72);
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
         this.buttonAddKnownHost.Location = new System.Drawing.Point(424, 39);
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
         this.listViewKnownHosts.Size = new System.Drawing.Size(409, 110);
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
         this.buttonBrowseLocalGitFolder.Location = new System.Drawing.Point(424, 31);
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
         this.textBoxLocalGitFolder.Size = new System.Drawing.Size(412, 20);
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
         this.comboBoxDCDepth.Location = new System.Drawing.Point(107, 95);
         this.comboBoxDCDepth.Name = "comboBoxDCDepth";
         this.comboBoxDCDepth.Size = new System.Drawing.Size(58, 21);
         this.comboBoxDCDepth.TabIndex = 7;
         this.toolTip.SetToolTip(this.comboBoxDCDepth, "Number of lines under the line the discussion was created for.");
         this.comboBoxDCDepth.SelectedIndexChanged += new System.EventHandler(this.comboBoxDCDepth_SelectedIndexChanged);
         // 
         // textBoxLabels
         // 
         this.textBoxLabels.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxLabels.Enabled = false;
         this.textBoxLabels.Location = new System.Drawing.Point(60, 17);
         this.textBoxLabels.MinimumSize = new System.Drawing.Size(100, 4);
         this.textBoxLabels.Name = "textBoxLabels";
         this.textBoxLabels.Size = new System.Drawing.Size(168, 20);
         this.textBoxLabels.TabIndex = 1;
         this.toolTip.SetToolTip(this.textBoxLabels, "To select merge requests use comma-separated list of the following:\n#{username} o" +
        "r label or MR IId or any substring from MR title/author name/label/branch");
         this.textBoxLabels.TextChanged += new System.EventHandler(this.textBoxLabels_TextChanged);
         this.textBoxLabels.Leave += new System.EventHandler(this.TextBoxLabels_LostFocus);
         // 
         // buttonEditTime
         // 
         this.buttonEditTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonEditTime.Enabled = false;
         this.buttonEditTime.Location = new System.Drawing.Point(824, 19);
         this.buttonEditTime.MinimumSize = new System.Drawing.Size(96, 0);
         this.buttonEditTime.Name = "buttonEditTime";
         this.buttonEditTime.Size = new System.Drawing.Size(96, 32);
         this.buttonEditTime.TabIndex = 13;
         this.buttonEditTime.Text = "Edit";
         this.toolTip.SetToolTip(this.buttonEditTime, "Edit total time tracked on this merge request");
         this.buttonEditTime.UseVisualStyleBackColor = true;
         this.buttonEditTime.Click += new System.EventHandler(this.ButtonTimeEdit_Click);
         // 
         // buttonDiffTool
         // 
         this.buttonDiffTool.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonDiffTool.Enabled = false;
         this.buttonDiffTool.Location = new System.Drawing.Point(824, 19);
         this.buttonDiffTool.MinimumSize = new System.Drawing.Size(96, 0);
         this.buttonDiffTool.Name = "buttonDiffTool";
         this.buttonDiffTool.Size = new System.Drawing.Size(96, 32);
         this.buttonDiffTool.TabIndex = 7;
         this.buttonDiffTool.Text = "Diff tool";
         this.toolTip.SetToolTip(this.buttonDiffTool, "Launch diff tool to review diff between selected commits");
         this.buttonDiffTool.UseVisualStyleBackColor = true;
         this.buttonDiffTool.Click += new System.EventHandler(this.ButtonDifftool_Click);
         // 
         // buttonAddComment
         // 
         this.buttonAddComment.Enabled = false;
         this.buttonAddComment.Location = new System.Drawing.Point(6, 19);
         this.buttonAddComment.MinimumSize = new System.Drawing.Size(96, 0);
         this.buttonAddComment.Name = "buttonAddComment";
         this.buttonAddComment.Size = new System.Drawing.Size(96, 32);
         this.buttonAddComment.TabIndex = 8;
         this.buttonAddComment.Text = "Add comment";
         this.toolTip.SetToolTip(this.buttonAddComment, "Leave a comment (cannot be resolved and replied)");
         this.buttonAddComment.UseVisualStyleBackColor = true;
         this.buttonAddComment.Click += new System.EventHandler(this.ButtonAddComment_Click);
         // 
         // buttonDiscussions
         // 
         this.buttonDiscussions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonDiscussions.Enabled = false;
         this.buttonDiscussions.Location = new System.Drawing.Point(722, 19);
         this.buttonDiscussions.MinimumSize = new System.Drawing.Size(96, 0);
         this.buttonDiscussions.Name = "buttonDiscussions";
         this.buttonDiscussions.Size = new System.Drawing.Size(96, 32);
         this.buttonDiscussions.TabIndex = 10;
         this.buttonDiscussions.Text = "Discussions";
         this.toolTip.SetToolTip(this.buttonDiscussions, "Show full list of Discussions");
         this.buttonDiscussions.UseVisualStyleBackColor = true;
         this.buttonDiscussions.Click += new System.EventHandler(this.ButtonDiscussions_Click);
         // 
         // buttonNewDiscussion
         // 
         this.buttonNewDiscussion.Enabled = false;
         this.buttonNewDiscussion.Location = new System.Drawing.Point(108, 19);
         this.buttonNewDiscussion.MinimumSize = new System.Drawing.Size(96, 0);
         this.buttonNewDiscussion.Name = "buttonNewDiscussion";
         this.buttonNewDiscussion.Size = new System.Drawing.Size(96, 32);
         this.buttonNewDiscussion.TabIndex = 9;
         this.buttonNewDiscussion.Text = "New discussion";
         this.toolTip.SetToolTip(this.buttonNewDiscussion, "Create a new resolvable discussion");
         this.buttonNewDiscussion.UseVisualStyleBackColor = true;
         this.buttonNewDiscussion.Click += new System.EventHandler(this.ButtonNewDiscussion_Click);
         // 
         // linkLabelHelp
         // 
         this.linkLabelHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelHelp.AutoSize = true;
         this.linkLabelHelp.Location = new System.Drawing.Point(804, 10);
         this.linkLabelHelp.Name = "linkLabelHelp";
         this.linkLabelHelp.Size = new System.Drawing.Size(29, 13);
         this.linkLabelHelp.TabIndex = 14;
         this.linkLabelHelp.TabStop = true;
         this.linkLabelHelp.Text = "Help";
         this.toolTip.SetToolTip(this.linkLabelHelp, "Open a web page with documentation");
         this.linkLabelHelp.Visible = false;
         // 
         // linkLabelSendFeedback
         // 
         this.linkLabelSendFeedback.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelSendFeedback.AutoSize = true;
         this.linkLabelSendFeedback.Location = new System.Drawing.Point(864, 10);
         this.linkLabelSendFeedback.Name = "linkLabelSendFeedback";
         this.linkLabelSendFeedback.Size = new System.Drawing.Size(55, 13);
         this.linkLabelSendFeedback.TabIndex = 15;
         this.linkLabelSendFeedback.TabStop = true;
         this.linkLabelSendFeedback.Text = "Feedback";
         this.toolTip.SetToolTip(this.linkLabelSendFeedback, "Report a bug or suggestion to developer. Logs are attached automatically.");
         this.linkLabelSendFeedback.Visible = false;
         // 
         // linkLabelNewVersion
         // 
         this.linkLabelNewVersion.AutoSize = true;
         this.linkLabelNewVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
         this.linkLabelNewVersion.Location = new System.Drawing.Point(2, 7);
         this.linkLabelNewVersion.Name = "linkLabelNewVersion";
         this.linkLabelNewVersion.Size = new System.Drawing.Size(299, 17);
         this.linkLabelNewVersion.TabIndex = 16;
         this.linkLabelNewVersion.TabStop = true;
         this.linkLabelNewVersion.Text = "New version is available! Click here to install it.";
         this.toolTip.SetToolTip(this.linkLabelNewVersion, "New version is already downloaded. Click to install it.");
         this.linkLabelNewVersion.Visible = false;
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
         this.notifyIcon.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
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
         this.tabControl.Size = new System.Drawing.Size(1284, 890);
         this.tabControl.TabIndex = 0;
         this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
         // 
         // tabPageSettings
         // 
         this.tabPageSettings.Controls.Add(this.groupBoxNotifications);
         this.tabPageSettings.Controls.Add(this.groupBoxOther);
         this.tabPageSettings.Controls.Add(this.groupBoxGit);
         this.tabPageSettings.Controls.Add(this.groupBoxKnownHosts);
         this.tabPageSettings.Controls.Add(this.groupBoxHost);
         this.tabPageSettings.Location = new System.Drawing.Point(4, 22);
         this.tabPageSettings.Name = "tabPageSettings";
         this.tabPageSettings.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSettings.Size = new System.Drawing.Size(1276, 864);
         this.tabPageSettings.TabIndex = 0;
         this.tabPageSettings.Text = "Settings";
         this.tabPageSettings.UseVisualStyleBackColor = true;
         // 
         // groupBoxNotifications
         // 
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowServiceNotifications);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowMyActivity);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowKeywords);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowOnMention);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowResolvedAll);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowUpdatedMergeRequests);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowMergedMergeRequests);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowNewMergeRequests);
         this.groupBoxNotifications.Location = new System.Drawing.Point(6, 222);
         this.groupBoxNotifications.Name = "groupBoxNotifications";
         this.groupBoxNotifications.Size = new System.Drawing.Size(513, 135);
         this.groupBoxNotifications.TabIndex = 4;
         this.groupBoxNotifications.TabStop = false;
         this.groupBoxNotifications.Text = "Notifications";
         // 
         // checkBoxShowServiceNotifications
         // 
         this.checkBoxShowServiceNotifications.AutoSize = true;
         this.checkBoxShowServiceNotifications.Location = new System.Drawing.Point(234, 106);
         this.checkBoxShowServiceNotifications.Name = "checkBoxShowServiceNotifications";
         this.checkBoxShowServiceNotifications.Size = new System.Drawing.Size(149, 17);
         this.checkBoxShowServiceNotifications.TabIndex = 17;
         this.checkBoxShowServiceNotifications.Text = "Show service notifications";
         this.checkBoxShowServiceNotifications.UseVisualStyleBackColor = true;
         this.checkBoxShowServiceNotifications.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowMyActivity
         // 
         this.checkBoxShowMyActivity.AutoSize = true;
         this.checkBoxShowMyActivity.Location = new System.Drawing.Point(9, 106);
         this.checkBoxShowMyActivity.Name = "checkBoxShowMyActivity";
         this.checkBoxShowMyActivity.Size = new System.Drawing.Size(113, 17);
         this.checkBoxShowMyActivity.TabIndex = 13;
         this.checkBoxShowMyActivity.Text = "Include my activity";
         this.checkBoxShowMyActivity.UseVisualStyleBackColor = true;
         this.checkBoxShowMyActivity.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowKeywords
         // 
         this.checkBoxShowKeywords.AutoSize = true;
         this.checkBoxShowKeywords.Location = new System.Drawing.Point(234, 65);
         this.checkBoxShowKeywords.Name = "checkBoxShowKeywords";
         this.checkBoxShowKeywords.Size = new System.Drawing.Size(75, 17);
         this.checkBoxShowKeywords.TabIndex = 16;
         this.checkBoxShowKeywords.Text = "Keywords:";
         this.checkBoxShowKeywords.UseVisualStyleBackColor = true;
         this.checkBoxShowKeywords.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowOnMention
         // 
         this.checkBoxShowOnMention.AutoSize = true;
         this.checkBoxShowOnMention.Location = new System.Drawing.Point(234, 42);
         this.checkBoxShowOnMention.Name = "checkBoxShowOnMention";
         this.checkBoxShowOnMention.Size = new System.Drawing.Size(242, 17);
         this.checkBoxShowOnMention.TabIndex = 15;
         this.checkBoxShowOnMention.Text = "When someone mentioned me in a discussion";
         this.checkBoxShowOnMention.UseVisualStyleBackColor = true;
         this.checkBoxShowOnMention.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowResolvedAll
         // 
         this.checkBoxShowResolvedAll.AutoSize = true;
         this.checkBoxShowResolvedAll.Location = new System.Drawing.Point(234, 19);
         this.checkBoxShowResolvedAll.Name = "checkBoxShowResolvedAll";
         this.checkBoxShowResolvedAll.Size = new System.Drawing.Size(144, 17);
         this.checkBoxShowResolvedAll.TabIndex = 14;
         this.checkBoxShowResolvedAll.Text = "Resolved All Discussions";
         this.checkBoxShowResolvedAll.UseVisualStyleBackColor = true;
         this.checkBoxShowResolvedAll.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowUpdatedMergeRequests
         // 
         this.checkBoxShowUpdatedMergeRequests.AutoSize = true;
         this.checkBoxShowUpdatedMergeRequests.Location = new System.Drawing.Point(9, 65);
         this.checkBoxShowUpdatedMergeRequests.Name = "checkBoxShowUpdatedMergeRequests";
         this.checkBoxShowUpdatedMergeRequests.Size = new System.Drawing.Size(181, 17);
         this.checkBoxShowUpdatedMergeRequests.TabIndex = 12;
         this.checkBoxShowUpdatedMergeRequests.Text = "New commits in Merge Requests";
         this.checkBoxShowUpdatedMergeRequests.UseVisualStyleBackColor = true;
         this.checkBoxShowUpdatedMergeRequests.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowMergedMergeRequests
         // 
         this.checkBoxShowMergedMergeRequests.AutoSize = true;
         this.checkBoxShowMergedMergeRequests.Location = new System.Drawing.Point(9, 42);
         this.checkBoxShowMergedMergeRequests.Name = "checkBoxShowMergedMergeRequests";
         this.checkBoxShowMergedMergeRequests.Size = new System.Drawing.Size(189, 17);
         this.checkBoxShowMergedMergeRequests.TabIndex = 11;
         this.checkBoxShowMergedMergeRequests.Text = "Merged or closed Merge Requests";
         this.checkBoxShowMergedMergeRequests.UseVisualStyleBackColor = true;
         this.checkBoxShowMergedMergeRequests.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowNewMergeRequests
         // 
         this.checkBoxShowNewMergeRequests.AutoSize = true;
         this.checkBoxShowNewMergeRequests.Location = new System.Drawing.Point(9, 19);
         this.checkBoxShowNewMergeRequests.Name = "checkBoxShowNewMergeRequests";
         this.checkBoxShowNewMergeRequests.Size = new System.Drawing.Size(129, 17);
         this.checkBoxShowNewMergeRequests.TabIndex = 10;
         this.checkBoxShowNewMergeRequests.Text = "New Merge Requests";
         this.checkBoxShowNewMergeRequests.UseVisualStyleBackColor = true;
         this.checkBoxShowNewMergeRequests.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // groupBoxOther
         // 
         this.groupBoxOther.Controls.Add(this.labelFontSize);
         this.groupBoxOther.Controls.Add(this.comboBoxFonts);
         this.groupBoxOther.Controls.Add(this.comboBoxThemes);
         this.groupBoxOther.Controls.Add(this.labelVisualTheme);
         this.groupBoxOther.Controls.Add(this.comboBoxColorSchemes);
         this.groupBoxOther.Controls.Add(this.labelColorScheme);
         this.groupBoxOther.Controls.Add(this.labelDepth);
         this.groupBoxOther.Controls.Add(this.comboBoxDCDepth);
         this.groupBoxOther.Controls.Add(this.checkBoxMinimizeOnClose);
         this.groupBoxOther.Location = new System.Drawing.Point(6, 363);
         this.groupBoxOther.Name = "groupBoxOther";
         this.groupBoxOther.Size = new System.Drawing.Size(301, 151);
         this.groupBoxOther.TabIndex = 2;
         this.groupBoxOther.TabStop = false;
         this.groupBoxOther.Text = "Other";
         // 
         // labelFontSize
         // 
         this.labelFontSize.AutoSize = true;
         this.labelFontSize.Location = new System.Drawing.Point(7, 16);
         this.labelFontSize.Name = "labelFontSize";
         this.labelFontSize.Size = new System.Drawing.Size(49, 13);
         this.labelFontSize.TabIndex = 11;
         this.labelFontSize.Text = "Font size";
         // 
         // comboBoxFonts
         // 
         this.comboBoxFonts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxFonts.FormattingEnabled = true;
         this.comboBoxFonts.Location = new System.Drawing.Point(107, 13);
         this.comboBoxFonts.Name = "comboBoxFonts";
         this.comboBoxFonts.Size = new System.Drawing.Size(182, 21);
         this.comboBoxFonts.TabIndex = 10;
         this.comboBoxFonts.SelectionChangeCommitted += new System.EventHandler(this.comboBoxFonts_SelectionChangeCommitted);
         // 
         // comboBoxThemes
         // 
         this.comboBoxThemes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxThemes.FormattingEnabled = true;
         this.comboBoxThemes.Location = new System.Drawing.Point(107, 41);
         this.comboBoxThemes.Name = "comboBoxThemes";
         this.comboBoxThemes.Size = new System.Drawing.Size(182, 21);
         this.comboBoxThemes.TabIndex = 10;
         this.comboBoxThemes.SelectionChangeCommitted += new System.EventHandler(this.comboBoxThemes_SelectionChangeCommitted);
         // 
         // labelVisualTheme
         // 
         this.labelVisualTheme.AutoSize = true;
         this.labelVisualTheme.Location = new System.Drawing.Point(7, 44);
         this.labelVisualTheme.Name = "labelVisualTheme";
         this.labelVisualTheme.Size = new System.Drawing.Size(40, 13);
         this.labelVisualTheme.TabIndex = 9;
         this.labelVisualTheme.Text = "Theme";
         // 
         // comboBoxColorSchemes
         // 
         this.comboBoxColorSchemes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxColorSchemes.FormattingEnabled = true;
         this.comboBoxColorSchemes.Location = new System.Drawing.Point(107, 68);
         this.comboBoxColorSchemes.Name = "comboBoxColorSchemes";
         this.comboBoxColorSchemes.Size = new System.Drawing.Size(182, 21);
         this.comboBoxColorSchemes.TabIndex = 6;
         this.comboBoxColorSchemes.SelectionChangeCommitted += new System.EventHandler(this.ComboBoxColorSchemes_SelectionChangeCommited);
         // 
         // labelColorScheme
         // 
         this.labelColorScheme.AutoSize = true;
         this.labelColorScheme.Location = new System.Drawing.Point(7, 71);
         this.labelColorScheme.Name = "labelColorScheme";
         this.labelColorScheme.Size = new System.Drawing.Size(71, 13);
         this.labelColorScheme.TabIndex = 8;
         this.labelColorScheme.Text = "Color scheme";
         // 
         // labelDepth
         // 
         this.labelDepth.AutoSize = true;
         this.labelDepth.Location = new System.Drawing.Point(7, 98);
         this.labelDepth.Name = "labelDepth";
         this.labelDepth.Size = new System.Drawing.Size(94, 13);
         this.labelDepth.TabIndex = 5;
         this.labelDepth.Text = "Diff Context Depth";
         // 
         // checkBoxMinimizeOnClose
         // 
         this.checkBoxMinimizeOnClose.AutoSize = true;
         this.checkBoxMinimizeOnClose.Location = new System.Drawing.Point(6, 128);
         this.checkBoxMinimizeOnClose.Name = "checkBoxMinimizeOnClose";
         this.checkBoxMinimizeOnClose.Size = new System.Drawing.Size(109, 17);
         this.checkBoxMinimizeOnClose.TabIndex = 8;
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
         this.groupBoxHost.Controls.Add(this.buttonEditProjects);
         this.groupBoxHost.Controls.Add(this.comboBoxHost);
         this.groupBoxHost.Controls.Add(this.listViewProjects);
         this.groupBoxHost.Location = new System.Drawing.Point(525, 6);
         this.groupBoxHost.Name = "groupBoxHost";
         this.groupBoxHost.Size = new System.Drawing.Size(277, 351);
         this.groupBoxHost.TabIndex = 3;
         this.groupBoxHost.TabStop = false;
         this.groupBoxHost.Text = "Select Host and Projects";
         // 
         // buttonEditProjects
         // 
         this.buttonEditProjects.Location = new System.Drawing.Point(6, 312);
         this.buttonEditProjects.Name = "buttonEditProjects";
         this.buttonEditProjects.Size = new System.Drawing.Size(83, 27);
         this.buttonEditProjects.TabIndex = 1;
         this.buttonEditProjects.Text = "Edit...";
         this.buttonEditProjects.UseVisualStyleBackColor = true;
         this.buttonEditProjects.Click += new System.EventHandler(this.buttonEditProjects_Click);
         // 
         // comboBoxHost
         // 
         this.comboBoxHost.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxHost.FormattingEnabled = true;
         this.comboBoxHost.Location = new System.Drawing.Point(6, 19);
         this.comboBoxHost.Name = "comboBoxHost";
         this.comboBoxHost.Size = new System.Drawing.Size(259, 21);
         this.comboBoxHost.TabIndex = 5;
         this.comboBoxHost.SelectionChangeCommitted += new System.EventHandler(this.ComboBoxHost_SelectionChangeCommited);
         this.comboBoxHost.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxHost_Format);
         // 
         // listViewProjects
         // 
         this.listViewProjects.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName});
         this.listViewProjects.FullRowSelect = true;
         this.listViewProjects.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
         this.listViewProjects.HideSelection = false;
         this.listViewProjects.Location = new System.Drawing.Point(6, 46);
         this.listViewProjects.MultiSelect = false;
         this.listViewProjects.Name = "listViewProjects";
         this.listViewProjects.ShowGroups = false;
         this.listViewProjects.Size = new System.Drawing.Size(259, 260);
         this.listViewProjects.TabIndex = 0;
         this.listViewProjects.UseCompatibleStateImageBehavior = false;
         this.listViewProjects.View = System.Windows.Forms.View.Details;
         // 
         // columnHeaderName
         // 
         this.columnHeaderName.Text = "Name";
         this.columnHeaderName.Width = 160;
         // 
         // tabPageMR
         // 
         this.tabPageMR.Controls.Add(this.splitContainer1);
         this.tabPageMR.Location = new System.Drawing.Point(4, 22);
         this.tabPageMR.Name = "tabPageMR";
         this.tabPageMR.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageMR.Size = new System.Drawing.Size(1276, 864);
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
         // 
         // splitContainer1.Panel2
         // 
         this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
         this.splitContainer1.Size = new System.Drawing.Size(1270, 858);
         this.splitContainer1.SplitterDistance = 336;
         this.splitContainer1.SplitterWidth = 8;
         this.splitContainer1.TabIndex = 4;
         this.splitContainer1.TabStop = false;
         this.splitContainer1.SplitterMoving += new System.Windows.Forms.SplitterCancelEventHandler(this.splitContainer_SplitterMoving);
         this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer_SplitterMoved);
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
         this.groupBoxSelectMergeRequest.Size = new System.Drawing.Size(336, 858);
         this.groupBoxSelectMergeRequest.TabIndex = 0;
         this.groupBoxSelectMergeRequest.TabStop = false;
         this.groupBoxSelectMergeRequest.Text = "Select Merge Request";
         // 
         // buttonReloadList
         // 
         this.buttonReloadList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonReloadList.Enabled = false;
         this.buttonReloadList.Location = new System.Drawing.Point(234, 9);
         this.buttonReloadList.MinimumSize = new System.Drawing.Size(96, 0);
         this.buttonReloadList.Name = "buttonReloadList";
         this.buttonReloadList.Size = new System.Drawing.Size(96, 32);
         this.buttonReloadList.TabIndex = 2;
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
            this.columnHeaderJira,
            this.columnHeaderTotalTime,
            this.columnHeaderSourceBranch,
            this.columnHeaderTargetBranch});
         this.listViewMergeRequests.FullRowSelect = true;
         this.listViewMergeRequests.GridLines = true;
         this.listViewMergeRequests.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
         this.listViewMergeRequests.HideSelection = false;
         this.listViewMergeRequests.Location = new System.Drawing.Point(3, 46);
         this.listViewMergeRequests.MultiSelect = false;
         this.listViewMergeRequests.Name = "listViewMergeRequests";
         this.listViewMergeRequests.OwnerDraw = true;
         this.listViewMergeRequests.Size = new System.Drawing.Size(330, 809);
         this.listViewMergeRequests.TabIndex = 3;
         this.listViewMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewMergeRequests.ColumnWidthChanged += new System.Windows.Forms.ColumnWidthChangedEventHandler(this.listViewMergeRequests_ColumnWidthChanged);
         this.listViewMergeRequests.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.ListViewMergeRequests_DrawColumnHeader);
         this.listViewMergeRequests.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.ListViewMergeRequests_DrawSubItem);
         this.listViewMergeRequests.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.ListViewMergeRequests_ItemSelectionChanged);
         this.listViewMergeRequests.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ListViewMergeRequests_MouseDown);
         this.listViewMergeRequests.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ListViewMergeRequests_MouseMove);
         // 
         // columnHeaderIId
         // 
         this.columnHeaderIId.Tag = "IId";
         this.columnHeaderIId.Text = "IId";
         this.columnHeaderIId.Width = 40;
         // 
         // columnHeaderAuthor
         // 
         this.columnHeaderAuthor.Tag = "Author";
         this.columnHeaderAuthor.Text = "Author";
         this.columnHeaderAuthor.Width = 110;
         // 
         // columnHeaderTitle
         // 
         this.columnHeaderTitle.Tag = "Title";
         this.columnHeaderTitle.Text = "Title";
         this.columnHeaderTitle.Width = 400;
         // 
         // columnHeaderLabels
         // 
         this.columnHeaderLabels.Tag = "Labels";
         this.columnHeaderLabels.Text = "Labels";
         this.columnHeaderLabels.Width = 180;
         // 
         // columnHeaderJira
         // 
         this.columnHeaderJira.Tag = "Jira";
         this.columnHeaderJira.Text = "Jira";
         this.columnHeaderJira.Width = 80;
         // 
         // columnHeaderTotalTime
         // 
         this.columnHeaderTotalTime.Tag = "TotalTime";
         this.columnHeaderTotalTime.Text = "Total Time";
         this.columnHeaderTotalTime.Width = 70;
         // 
         // columnHeaderSourceBranch
         // 
         this.columnHeaderSourceBranch.Tag = "SourceBranch";
         this.columnHeaderSourceBranch.Text = "Source Branch";
         this.columnHeaderSourceBranch.Width = 100;
         // 
         // columnHeaderTargetBranch
         // 
         this.columnHeaderTargetBranch.Tag = "TargetBranch";
         this.columnHeaderTargetBranch.Text = "Target Branch";
         this.columnHeaderTargetBranch.Width = 100;
         // 
         // checkBoxLabels
         // 
         this.checkBoxLabels.AutoSize = true;
         this.checkBoxLabels.Enabled = false;
         this.checkBoxLabels.Location = new System.Drawing.Point(6, 19);
         this.checkBoxLabels.MinimumSize = new System.Drawing.Size(48, 0);
         this.checkBoxLabels.Name = "checkBoxLabels";
         this.checkBoxLabels.Size = new System.Drawing.Size(48, 17);
         this.checkBoxLabels.TabIndex = 0;
         this.checkBoxLabels.Text = "Filter";
         this.checkBoxLabels.UseVisualStyleBackColor = true;
         this.checkBoxLabels.CheckedChanged += new System.EventHandler(this.CheckBoxLabels_CheckedChanged);
         // 
         // splitContainer2
         // 
         this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer2.Location = new System.Drawing.Point(0, 0);
         this.splitContainer2.Name = "splitContainer2";
         this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
         // 
         // splitContainer2.Panel1
         // 
         this.splitContainer2.Panel1.Controls.Add(this.groupBoxSelectedMR);
         // 
         // splitContainer2.Panel2
         // 
         this.splitContainer2.Panel2.Controls.Add(this.panelFreeSpace);
         this.splitContainer2.Panel2.Controls.Add(this.panelStatusBar);
         this.splitContainer2.Panel2.Controls.Add(this.panelBottomMenu);
         this.splitContainer2.Panel2.Controls.Add(this.groupBoxActions);
         this.splitContainer2.Panel2.Controls.Add(this.groupBoxTimeTracking);
         this.splitContainer2.Panel2.Controls.Add(this.groupBoxReview);
         this.splitContainer2.Panel2.Controls.Add(this.groupBoxSelectCommits);
         this.splitContainer2.Size = new System.Drawing.Size(926, 858);
         this.splitContainer2.SplitterDistance = 267;
         this.splitContainer2.SplitterWidth = 8;
         this.splitContainer2.TabIndex = 7;
         this.splitContainer2.TabStop = false;
         this.splitContainer2.SplitterMoving += new System.Windows.Forms.SplitterCancelEventHandler(this.splitContainer_SplitterMoving);
         this.splitContainer2.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer_SplitterMoved);
         // 
         // groupBoxSelectedMR
         // 
         this.groupBoxSelectedMR.Controls.Add(this.linkLabelConnectedTo);
         this.groupBoxSelectedMR.Controls.Add(this.richTextBoxMergeRequestDescription);
         this.groupBoxSelectedMR.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSelectedMR.Location = new System.Drawing.Point(0, 0);
         this.groupBoxSelectedMR.Name = "groupBoxSelectedMR";
         this.groupBoxSelectedMR.Size = new System.Drawing.Size(926, 267);
         this.groupBoxSelectedMR.TabIndex = 1;
         this.groupBoxSelectedMR.TabStop = false;
         this.groupBoxSelectedMR.Text = "Merge Request";
         // 
         // linkLabelConnectedTo
         // 
         this.linkLabelConnectedTo.AutoEllipsis = true;
         this.linkLabelConnectedTo.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.linkLabelConnectedTo.Location = new System.Drawing.Point(3, 246);
         this.linkLabelConnectedTo.Name = "linkLabelConnectedTo";
         this.linkLabelConnectedTo.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
         this.linkLabelConnectedTo.Size = new System.Drawing.Size(920, 18);
         this.linkLabelConnectedTo.TabIndex = 4;
         this.linkLabelConnectedTo.TabStop = true;
         this.linkLabelConnectedTo.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";
         this.linkLabelConnectedTo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelConnectedTo_LinkClicked);
         // 
         // richTextBoxMergeRequestDescription
         // 
         this.richTextBoxMergeRequestDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.richTextBoxMergeRequestDescription.AutoScroll = true;
         this.richTextBoxMergeRequestDescription.BackColor = System.Drawing.SystemColors.Window;
         this.richTextBoxMergeRequestDescription.BaseStylesheet = null;
         this.richTextBoxMergeRequestDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.richTextBoxMergeRequestDescription.Location = new System.Drawing.Point(3, 16);
         this.richTextBoxMergeRequestDescription.MinimumSize = new System.Drawing.Size(2, 100);
         this.richTextBoxMergeRequestDescription.Name = "richTextBoxMergeRequestDescription";
         this.richTextBoxMergeRequestDescription.Size = new System.Drawing.Size(920, 227);
         this.richTextBoxMergeRequestDescription.TabIndex = 2;
         this.richTextBoxMergeRequestDescription.TabStop = false;
         this.richTextBoxMergeRequestDescription.Text = null;
         // 
         // panelFreeSpace
         // 
         this.panelFreeSpace.Controls.Add(this.pictureBox2);
         this.panelFreeSpace.Controls.Add(this.pictureBox1);
         this.panelFreeSpace.Dock = System.Windows.Forms.DockStyle.Fill;
         this.panelFreeSpace.Location = new System.Drawing.Point(0, 334);
         this.panelFreeSpace.MinimumSize = new System.Drawing.Size(0, 10);
         this.panelFreeSpace.Name = "panelFreeSpace";
         this.panelFreeSpace.Size = new System.Drawing.Size(926, 163);
         this.panelFreeSpace.TabIndex = 9;
         // 
         // pictureBox2
         // 
         this.pictureBox2.Dock = System.Windows.Forms.DockStyle.Right;
         this.pictureBox2.Location = new System.Drawing.Point(676, 0);
         this.pictureBox2.MinimumSize = new System.Drawing.Size(250, 100);
         this.pictureBox2.Name = "pictureBox2";
         this.pictureBox2.Size = new System.Drawing.Size(250, 163);
         this.pictureBox2.TabIndex = 10;
         this.pictureBox2.TabStop = false;
         this.pictureBox2.Visible = false;
         // 
         // pictureBox1
         // 
         this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Left;
         this.pictureBox1.Location = new System.Drawing.Point(0, 0);
         this.pictureBox1.MinimumSize = new System.Drawing.Size(250, 100);
         this.pictureBox1.Name = "pictureBox1";
         this.pictureBox1.Size = new System.Drawing.Size(250, 163);
         this.pictureBox1.TabIndex = 9;
         this.pictureBox1.TabStop = false;
         this.pictureBox1.Visible = false;
         // 
         // panelStatusBar
         // 
         this.panelStatusBar.BackColor = System.Drawing.Color.WhiteSmoke;
         this.panelStatusBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.panelStatusBar.Controls.Add(this.linkLabelAbortGit);
         this.panelStatusBar.Controls.Add(this.labelGitStatus);
         this.panelStatusBar.Controls.Add(this.labelWorkflowStatus);
         this.panelStatusBar.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.panelStatusBar.Location = new System.Drawing.Point(0, 497);
         this.panelStatusBar.Name = "panelStatusBar";
         this.panelStatusBar.Size = new System.Drawing.Size(926, 52);
         this.panelStatusBar.TabIndex = 10;
         // 
         // linkLabelAbortGit
         // 
         this.linkLabelAbortGit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelAbortGit.Location = new System.Drawing.Point(887, 31);
         this.linkLabelAbortGit.Name = "linkLabelAbortGit";
         this.linkLabelAbortGit.Size = new System.Drawing.Size(32, 15);
         this.linkLabelAbortGit.TabIndex = 15;
         this.linkLabelAbortGit.TabStop = true;
         this.linkLabelAbortGit.Text = "Abort";
         this.linkLabelAbortGit.Visible = false;
         // 
         // labelGitStatus
         // 
         this.labelGitStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.labelGitStatus.AutoEllipsis = true;
         this.labelGitStatus.Location = new System.Drawing.Point(0, 31);
         this.labelGitStatus.Name = "labelGitStatus";
         this.labelGitStatus.Size = new System.Drawing.Size(881, 16);
         this.labelGitStatus.TabIndex = 1;
         this.labelGitStatus.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor in" +
    "cididunt ut labore et dolore";
         this.labelGitStatus.Visible = false;
         // 
         // labelWorkflowStatus
         // 
         this.labelWorkflowStatus.AutoEllipsis = true;
         this.labelWorkflowStatus.Dock = System.Windows.Forms.DockStyle.Top;
         this.labelWorkflowStatus.Location = new System.Drawing.Point(0, 0);
         this.labelWorkflowStatus.Name = "labelWorkflowStatus";
         this.labelWorkflowStatus.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
         this.labelWorkflowStatus.Size = new System.Drawing.Size(924, 24);
         this.labelWorkflowStatus.TabIndex = 0;
         this.labelWorkflowStatus.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor in" +
    "cididunt ut labore et dolore magna aliqua";
         // 
         // panelBottomMenu
         // 
         this.panelBottomMenu.BackColor = System.Drawing.Color.WhiteSmoke;
         this.panelBottomMenu.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.panelBottomMenu.Controls.Add(this.linkLabelHelp);
         this.panelBottomMenu.Controls.Add(this.linkLabelSendFeedback);
         this.panelBottomMenu.Controls.Add(this.linkLabelNewVersion);
         this.panelBottomMenu.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.panelBottomMenu.Location = new System.Drawing.Point(0, 549);
         this.panelBottomMenu.Name = "panelBottomMenu";
         this.panelBottomMenu.Size = new System.Drawing.Size(926, 34);
         this.panelBottomMenu.TabIndex = 11;
         // 
         // groupBoxActions
         // 
         this.groupBoxActions.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxActions.Location = new System.Drawing.Point(0, 271);
         this.groupBoxActions.Name = "groupBoxActions";
         this.groupBoxActions.Size = new System.Drawing.Size(926, 63);
         this.groupBoxActions.TabIndex = 0;
         this.groupBoxActions.TabStop = false;
         this.groupBoxActions.Text = "Actions";
         this.groupBoxActions.SizeChanged += new System.EventHandler(this.groupBoxActions_SizeChanged);
         // 
         // groupBoxTimeTracking
         // 
         this.groupBoxTimeTracking.Controls.Add(this.labelTimeTrackingTrackedLabel);
         this.groupBoxTimeTracking.Controls.Add(this.labelTimeTrackingMergeRequestName);
         this.groupBoxTimeTracking.Controls.Add(this.buttonEditTime);
         this.groupBoxTimeTracking.Controls.Add(this.buttonTimeTrackingCancel);
         this.groupBoxTimeTracking.Controls.Add(this.buttonTimeTrackingStart);
         this.groupBoxTimeTracking.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxTimeTracking.Location = new System.Drawing.Point(0, 170);
         this.groupBoxTimeTracking.Name = "groupBoxTimeTracking";
         this.groupBoxTimeTracking.Size = new System.Drawing.Size(926, 101);
         this.groupBoxTimeTracking.TabIndex = 6;
         this.groupBoxTimeTracking.TabStop = false;
         this.groupBoxTimeTracking.Text = "Time tracking";
         // 
         // labelTimeTrackingTrackedLabel
         // 
         this.labelTimeTrackingTrackedLabel.AutoEllipsis = true;
         this.labelTimeTrackingTrackedLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.labelTimeTrackingTrackedLabel.Location = new System.Drawing.Point(3, 54);
         this.labelTimeTrackingTrackedLabel.Name = "labelTimeTrackingTrackedLabel";
         this.labelTimeTrackingTrackedLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 4);
         this.labelTimeTrackingTrackedLabel.Size = new System.Drawing.Size(920, 22);
         this.labelTimeTrackingTrackedLabel.TabIndex = 2;
         this.labelTimeTrackingTrackedLabel.Text = "Total Time";
         // 
         // labelTimeTrackingMergeRequestName
         // 
         this.labelTimeTrackingMergeRequestName.AutoEllipsis = true;
         this.labelTimeTrackingMergeRequestName.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.labelTimeTrackingMergeRequestName.Location = new System.Drawing.Point(3, 76);
         this.labelTimeTrackingMergeRequestName.Name = "labelTimeTrackingMergeRequestName";
         this.labelTimeTrackingMergeRequestName.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
         this.labelTimeTrackingMergeRequestName.Size = new System.Drawing.Size(920, 22);
         this.labelTimeTrackingMergeRequestName.TabIndex = 5;
         this.labelTimeTrackingMergeRequestName.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";
         this.labelTimeTrackingMergeRequestName.Visible = false;
         // 
         // buttonTimeTrackingCancel
         // 
         this.buttonTimeTrackingCancel.Enabled = false;
         this.buttonTimeTrackingCancel.Location = new System.Drawing.Point(108, 19);
         this.buttonTimeTrackingCancel.MinimumSize = new System.Drawing.Size(96, 0);
         this.buttonTimeTrackingCancel.Name = "buttonTimeTrackingCancel";
         this.buttonTimeTrackingCancel.Size = new System.Drawing.Size(96, 32);
         this.buttonTimeTrackingCancel.TabIndex = 12;
         this.buttonTimeTrackingCancel.Text = "Cancel";
         this.buttonTimeTrackingCancel.UseVisualStyleBackColor = true;
         this.buttonTimeTrackingCancel.Click += new System.EventHandler(this.ButtonTimeTrackingCancel_Click);
         // 
         // buttonTimeTrackingStart
         // 
         this.buttonTimeTrackingStart.Enabled = false;
         this.buttonTimeTrackingStart.Location = new System.Drawing.Point(6, 19);
         this.buttonTimeTrackingStart.MinimumSize = new System.Drawing.Size(96, 0);
         this.buttonTimeTrackingStart.Name = "buttonTimeTrackingStart";
         this.buttonTimeTrackingStart.Size = new System.Drawing.Size(96, 32);
         this.buttonTimeTrackingStart.TabIndex = 11;
         this.buttonTimeTrackingStart.Text = "Start Timer";
         this.buttonTimeTrackingStart.UseVisualStyleBackColor = true;
         this.buttonTimeTrackingStart.Click += new System.EventHandler(this.ButtonTimeTrackingStart_Click);
         // 
         // groupBoxReview
         // 
         this.groupBoxReview.Controls.Add(this.buttonDiffTool);
         this.groupBoxReview.Controls.Add(this.buttonAddComment);
         this.groupBoxReview.Controls.Add(this.buttonDiscussions);
         this.groupBoxReview.Controls.Add(this.buttonNewDiscussion);
         this.groupBoxReview.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxReview.Location = new System.Drawing.Point(0, 107);
         this.groupBoxReview.Name = "groupBoxReview";
         this.groupBoxReview.Size = new System.Drawing.Size(926, 63);
         this.groupBoxReview.TabIndex = 2;
         this.groupBoxReview.TabStop = false;
         this.groupBoxReview.Text = "Review";
         // 
         // groupBoxSelectCommits
         // 
         this.groupBoxSelectCommits.Controls.Add(this.labelRightCommitTimestampLabel);
         this.groupBoxSelectCommits.Controls.Add(this.comboBoxRightCommit);
         this.groupBoxSelectCommits.Controls.Add(this.labelLeftCommitTimestampLabel);
         this.groupBoxSelectCommits.Controls.Add(this.comboBoxLeftCommit);
         this.groupBoxSelectCommits.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxSelectCommits.Location = new System.Drawing.Point(0, 0);
         this.groupBoxSelectCommits.Name = "groupBoxSelectCommits";
         this.groupBoxSelectCommits.Size = new System.Drawing.Size(926, 107);
         this.groupBoxSelectCommits.TabIndex = 4;
         this.groupBoxSelectCommits.TabStop = false;
         this.groupBoxSelectCommits.Text = "Select commits";
         // 
         // labelRightCommitTimestampLabel
         // 
         this.labelRightCommitTimestampLabel.AutoEllipsis = true;
         this.labelRightCommitTimestampLabel.Dock = System.Windows.Forms.DockStyle.Top;
         this.labelRightCommitTimestampLabel.Location = new System.Drawing.Point(3, 82);
         this.labelRightCommitTimestampLabel.Name = "labelRightCommitTimestampLabel";
         this.labelRightCommitTimestampLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
         this.labelRightCommitTimestampLabel.Size = new System.Drawing.Size(920, 22);
         this.labelRightCommitTimestampLabel.TabIndex = 8;
         this.labelRightCommitTimestampLabel.Text = "Created at:";
         // 
         // comboBoxRightCommit
         // 
         this.comboBoxRightCommit.Dock = System.Windows.Forms.DockStyle.Top;
         this.comboBoxRightCommit.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
         this.comboBoxRightCommit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxRightCommit.FormattingEnabled = true;
         this.comboBoxRightCommit.Location = new System.Drawing.Point(3, 61);
         this.comboBoxRightCommit.Name = "comboBoxRightCommit";
         this.comboBoxRightCommit.Size = new System.Drawing.Size(920, 21);
         this.comboBoxRightCommit.TabIndex = 6;
         this.comboBoxRightCommit.SelectedIndexChanged += new System.EventHandler(this.ComboBoxRightCommit_SelectedIndexChanged);
         this.comboBoxRightCommit.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBoxCommits_DrawItem);
         // 
         // labelLeftCommitTimestampLabel
         // 
         this.labelLeftCommitTimestampLabel.AutoEllipsis = true;
         this.labelLeftCommitTimestampLabel.Dock = System.Windows.Forms.DockStyle.Top;
         this.labelLeftCommitTimestampLabel.Location = new System.Drawing.Point(3, 37);
         this.labelLeftCommitTimestampLabel.Name = "labelLeftCommitTimestampLabel";
         this.labelLeftCommitTimestampLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 6);
         this.labelLeftCommitTimestampLabel.Size = new System.Drawing.Size(920, 24);
         this.labelLeftCommitTimestampLabel.TabIndex = 7;
         this.labelLeftCommitTimestampLabel.Text = "Created at:";
         // 
         // comboBoxLeftCommit
         // 
         this.comboBoxLeftCommit.Dock = System.Windows.Forms.DockStyle.Top;
         this.comboBoxLeftCommit.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
         this.comboBoxLeftCommit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxLeftCommit.FormattingEnabled = true;
         this.comboBoxLeftCommit.Location = new System.Drawing.Point(3, 16);
         this.comboBoxLeftCommit.Name = "comboBoxLeftCommit";
         this.comboBoxLeftCommit.Size = new System.Drawing.Size(920, 21);
         this.comboBoxLeftCommit.TabIndex = 5;
         this.comboBoxLeftCommit.SelectedIndexChanged += new System.EventHandler(this.ComboBoxLeftCommit_SelectedIndexChanged);
         this.comboBoxLeftCommit.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBoxCommits_DrawItem);
         // 
         // panel4
         // 
         this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
         this.panel4.Location = new System.Drawing.Point(0, 159);
         this.panel4.Name = "panel4";
         this.panel4.Size = new System.Drawing.Size(910, 79);
         this.panel4.TabIndex = 14;
         // 
         // panel1
         // 
         this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
         this.panel1.Location = new System.Drawing.Point(0, 80);
         this.panel1.Name = "panel1";
         this.panel1.Size = new System.Drawing.Size(910, 79);
         this.panel1.TabIndex = 5;
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(1284, 890);
         this.Controls.Add(this.tabControl);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.Name = "MainForm";
         this.Text = "Merge Request Helper";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MrHelperForm_FormClosing);
         this.Load += new System.EventHandler(this.MrHelperForm_Load);
         this.groupBoxKnownHosts.ResumeLayout(false);
         this.contextMenuStrip.ResumeLayout(false);
         this.tabControl.ResumeLayout(false);
         this.tabPageSettings.ResumeLayout(false);
         this.groupBoxNotifications.ResumeLayout(false);
         this.groupBoxNotifications.PerformLayout();
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
         this.splitContainer2.Panel1.ResumeLayout(false);
         this.splitContainer2.Panel2.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
         this.splitContainer2.ResumeLayout(false);
         this.groupBoxSelectedMR.ResumeLayout(false);
         this.panelFreeSpace.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
         this.panelStatusBar.ResumeLayout(false);
         this.panelBottomMenu.ResumeLayout(false);
         this.panelBottomMenu.PerformLayout();
         this.groupBoxTimeTracking.ResumeLayout(false);
         this.groupBoxReview.ResumeLayout(false);
         this.groupBoxSelectCommits.ResumeLayout(false);
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
      private mrHelper.CommonControls.Controls.ListViewEx listViewMergeRequests;
      private System.Windows.Forms.ColumnHeader columnHeaderIId;
      private System.Windows.Forms.ColumnHeader columnHeaderAuthor;
      private System.Windows.Forms.ColumnHeader columnHeaderTitle;
      private System.Windows.Forms.ColumnHeader columnHeaderLabels;
      private System.Windows.Forms.ColumnHeader columnHeaderJira;
      private System.Windows.Forms.TextBox textBoxLabels;
      private System.Windows.Forms.CheckBox checkBoxLabels;
      private SelectionPreservingComboBox comboBoxHost;
      private System.Windows.Forms.Button buttonReloadList;
      private System.Windows.Forms.ColumnHeader columnHeaderTotalTime;
      private System.Windows.Forms.GroupBox groupBoxNotifications;
      private System.Windows.Forms.CheckBox checkBoxShowNewMergeRequests;
      private System.Windows.Forms.CheckBox checkBoxShowKeywords;
      private System.Windows.Forms.CheckBox checkBoxShowOnMention;
      private System.Windows.Forms.CheckBox checkBoxShowResolvedAll;
      private System.Windows.Forms.CheckBox checkBoxShowUpdatedMergeRequests;
      private System.Windows.Forms.CheckBox checkBoxShowMergedMergeRequests;
      private System.Windows.Forms.CheckBox checkBoxShowMyActivity;
      private System.Windows.Forms.ColumnHeader columnHeaderSourceBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderTargetBranch;
      private System.Windows.Forms.SplitContainer splitContainer2;
      private System.Windows.Forms.GroupBox groupBoxSelectedMR;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel richTextBoxMergeRequestDescription;
      private System.Windows.Forms.LinkLabel linkLabelConnectedTo;
      private System.Windows.Forms.GroupBox groupBoxTimeTracking;
      private System.Windows.Forms.Label labelTimeTrackingMergeRequestName;
      private System.Windows.Forms.Button buttonEditTime;
      private System.Windows.Forms.Label labelTimeTrackingTrackedLabel;
      private System.Windows.Forms.Button buttonTimeTrackingCancel;
      private System.Windows.Forms.Button buttonTimeTrackingStart;
      private System.Windows.Forms.Panel panel1;
      private System.Windows.Forms.GroupBox groupBoxActions;
      private System.Windows.Forms.GroupBox groupBoxSelectCommits;
      private System.Windows.Forms.Button buttonDiffTool;
      private SelectionPreservingComboBox comboBoxRightCommit;
      private SelectionPreservingComboBox comboBoxLeftCommit;
      private System.Windows.Forms.CheckBox checkBoxShowServiceNotifications;
      private System.Windows.Forms.ComboBox comboBoxThemes;
      private System.Windows.Forms.ComboBox comboBoxFonts;
      private System.Windows.Forms.Label labelVisualTheme;
      private System.Windows.Forms.Button buttonEditProjects;
      private System.Windows.Forms.ListView listViewProjects;
      private System.Windows.Forms.ColumnHeader columnHeaderName;
      private System.Windows.Forms.Label labelFontSize;
      private System.Windows.Forms.Panel panel4;
      private System.Windows.Forms.GroupBox groupBoxReview;
      private System.Windows.Forms.Button buttonAddComment;
      private System.Windows.Forms.Button buttonDiscussions;
      private System.Windows.Forms.Button buttonNewDiscussion;
        private System.Windows.Forms.Label labelRightCommitTimestampLabel;
        private System.Windows.Forms.Label labelLeftCommitTimestampLabel;
      private System.Windows.Forms.Panel panelStatusBar;
      private System.Windows.Forms.LinkLabel linkLabelAbortGit;
      private System.Windows.Forms.Label labelGitStatus;
      private System.Windows.Forms.Label labelWorkflowStatus;
      private System.Windows.Forms.Panel panelBottomMenu;
      private System.Windows.Forms.LinkLabel linkLabelHelp;
      private System.Windows.Forms.LinkLabel linkLabelSendFeedback;
      private System.Windows.Forms.LinkLabel linkLabelNewVersion;
      private System.Windows.Forms.Panel panelFreeSpace;
      private System.Windows.Forms.PictureBox pictureBox2;
      private System.Windows.Forms.PictureBox pictureBox1;
   }
}

