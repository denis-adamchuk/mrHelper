namespace mrHelper.App.Forms
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
         this.groupBoxKnownHosts = new System.Windows.Forms.GroupBox();
         this.buttonRemoveKnownHost = new System.Windows.Forms.Button();
         this.buttonAddKnownHost = new System.Windows.Forms.Button();
         this.listViewKnownHosts = new System.Windows.Forms.ListView();
         this.columnHeaderHost = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderAccessToken = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.groupBoxSelectMergeRequest = new System.Windows.Forms.GroupBox();
         this.buttonApplyLabels = new System.Windows.Forms.Button();
         this.textBoxLabels = new System.Windows.Forms.TextBox();
         this.checkBoxLabels = new System.Windows.Forms.CheckBox();
         this.linkLabelConnectedTo = new System.Windows.Forms.LinkLabel();
         this.buttonBrowseLocalGitFolder = new System.Windows.Forms.Button();
         this.textBoxLocalGitFolder = new System.Windows.Forms.TextBox();
         this.labelLocalGitFolder = new System.Windows.Forms.Label();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.buttonDiffTool = new System.Windows.Forms.Button();
         this.buttonDiscussions = new System.Windows.Forms.Button();
         this.comboBoxDCDepth = new System.Windows.Forms.ComboBox();
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
         this.checkBoxShowPublicOnly = new System.Windows.Forms.CheckBox();
         this.checkBoxRequireTimer = new System.Windows.Forms.CheckBox();
         this.groupBoxGit = new System.Windows.Forms.GroupBox();
         this.tabPageMR = new System.Windows.Forms.TabPage();
         this.linkLabelAbortGit = new System.Windows.Forms.LinkLabel();
         this.labelGitLabStatus = new System.Windows.Forms.Label();
         this.labelGitStatus = new System.Windows.Forms.Label();
         this.groupBoxSelectProject = new System.Windows.Forms.GroupBox();
         this.groupBoxActions = new System.Windows.Forms.GroupBox();
         this.groupBoxReview = new System.Windows.Forms.GroupBox();
         this.groupBoxTimeTracking = new System.Windows.Forms.GroupBox();
         this.labelSpentTime = new System.Windows.Forms.Label();
         this.buttonToggleTimer = new System.Windows.Forms.Button();
         this.labelSpentTimeLabel = new System.Windows.Forms.Label();
         this.groupBoxDescription = new System.Windows.Forms.GroupBox();
         this.textBoxMergeRequestName = new System.Windows.Forms.TextBox();
         this.richTextBoxMergeRequestDescription = new System.Windows.Forms.RichTextBox();
         this.groupBoxHost = new System.Windows.Forms.GroupBox();
         this.groupBoxDiff = new System.Windows.Forms.GroupBox();
         this.label3 = new System.Windows.Forms.Label();
         this.label4 = new System.Windows.Forms.Label();
         this.labelAutoUpdate = new System.Windows.Forms.Label();
         this.comboBoxProjects = new mrHelper.App.SelectionPreservingComboBox();
         this.comboBoxFilteredMergeRequests = new mrHelper.App.SelectionPreservingComboBox();
         this.comboBoxHost = new mrHelper.App.SelectionPreservingComboBox();
         this.comboBoxRightVersion = new mrHelper.App.SelectionPreservingComboBox();
         this.comboBoxLeftVersion = new mrHelper.App.SelectionPreservingComboBox();
         this.groupBoxKnownHosts.SuspendLayout();
         this.groupBoxSelectMergeRequest.SuspendLayout();
         this.contextMenuStrip.SuspendLayout();
         this.tabControl.SuspendLayout();
         this.tabPageSettings.SuspendLayout();
         this.groupBoxOther.SuspendLayout();
         this.groupBoxGit.SuspendLayout();
         this.tabPageMR.SuspendLayout();
         this.groupBoxSelectProject.SuspendLayout();
         this.groupBoxReview.SuspendLayout();
         this.groupBoxTimeTracking.SuspendLayout();
         this.groupBoxDescription.SuspendLayout();
         this.groupBoxHost.SuspendLayout();
         this.groupBoxDiff.SuspendLayout();
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
         this.buttonAddKnownHost.UseVisualStyleBackColor = true;
         this.buttonAddKnownHost.Click += new System.EventHandler(this.ButtonAddKnownHost_Click);
         // 
         // listViewKnownHosts
         // 
         this.listViewKnownHosts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderHost,
            this.columnHeaderAccessToken});
         this.listViewKnownHosts.FullRowSelect = true;
         this.listViewKnownHosts.GridLines = true;
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
         // groupBoxSelectMergeRequest
         // 
         this.groupBoxSelectMergeRequest.Controls.Add(this.buttonApplyLabels);
         this.groupBoxSelectMergeRequest.Controls.Add(this.textBoxLabels);
         this.groupBoxSelectMergeRequest.Controls.Add(this.checkBoxLabels);
         this.groupBoxSelectMergeRequest.Controls.Add(this.linkLabelConnectedTo);
         this.groupBoxSelectMergeRequest.Controls.Add(this.comboBoxFilteredMergeRequests);
         this.groupBoxSelectMergeRequest.Location = new System.Drawing.Point(6, 57);
         this.groupBoxSelectMergeRequest.Name = "groupBoxSelectMergeRequest";
         this.groupBoxSelectMergeRequest.Size = new System.Drawing.Size(511, 94);
         this.groupBoxSelectMergeRequest.TabIndex = 3;
         this.groupBoxSelectMergeRequest.TabStop = false;
         this.groupBoxSelectMergeRequest.Text = "Select Merge Request";
         // 
         // buttonApplyLabels
         // 
         this.buttonApplyLabels.Location = new System.Drawing.Point(307, 14);
         this.buttonApplyLabels.Name = "buttonApplyLabels";
         this.buttonApplyLabels.Size = new System.Drawing.Size(83, 27);
         this.buttonApplyLabels.TabIndex = 5;
         this.buttonApplyLabels.Text = "Apply";
         this.toolTip.SetToolTip(this.buttonApplyLabels, "Press Alt-K to create a new discussion");
         this.buttonApplyLabels.UseVisualStyleBackColor = true;
         this.buttonApplyLabels.Click += new System.EventHandler(this.ButtonApplyLabels_Click);
         // 
         // textBoxLabels
         // 
         this.textBoxLabels.Location = new System.Drawing.Point(69, 18);
         this.textBoxLabels.Name = "textBoxLabels";
         this.textBoxLabels.Size = new System.Drawing.Size(232, 20);
         this.textBoxLabels.TabIndex = 4;
         this.toolTip.SetToolTip(this.textBoxLabels, "Return merge requests that contain any of these labels");
         this.textBoxLabels.Leave += new System.EventHandler(this.TextBoxLabels_Leave);
         // 
         // checkBoxLabels
         // 
         this.checkBoxLabels.AutoSize = true;
         this.checkBoxLabels.Location = new System.Drawing.Point(6, 20);
         this.checkBoxLabels.Name = "checkBoxLabels";
         this.checkBoxLabels.Size = new System.Drawing.Size(57, 17);
         this.checkBoxLabels.TabIndex = 3;
         this.checkBoxLabels.Text = "Labels";
         this.checkBoxLabels.UseVisualStyleBackColor = true;
         this.checkBoxLabels.CheckedChanged += new System.EventHandler(this.CheckBoxLabels_CheckedChanged);
         // 
         // linkLabelConnectedTo
         // 
         this.linkLabelConnectedTo.AutoSize = true;
         this.linkLabelConnectedTo.Location = new System.Drawing.Point(6, 70);
         this.linkLabelConnectedTo.Name = "linkLabelConnectedTo";
         this.linkLabelConnectedTo.Size = new System.Drawing.Size(42, 13);
         this.linkLabelConnectedTo.TabIndex = 7;
         this.linkLabelConnectedTo.TabStop = true;
         this.linkLabelConnectedTo.Text = "url-here";
         this.linkLabelConnectedTo.Visible = false;
         this.linkLabelConnectedTo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelConnectedTo_LinkClicked);
         // 
         // buttonBrowseLocalGitFolder
         // 
         this.buttonBrowseLocalGitFolder.Location = new System.Drawing.Point(409, 31);
         this.buttonBrowseLocalGitFolder.Name = "buttonBrowseLocalGitFolder";
         this.buttonBrowseLocalGitFolder.Size = new System.Drawing.Size(83, 27);
         this.buttonBrowseLocalGitFolder.TabIndex = 4;
         this.buttonBrowseLocalGitFolder.Text = "Browse...";
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
         // toolTip
         // 
         this.toolTip.AutoPopDelay = 5000;
         this.toolTip.InitialDelay = 500;
         this.toolTip.ReshowDelay = 100;
         // 
         // buttonDiffTool
         // 
         this.buttonDiffTool.Enabled = false;
         this.buttonDiffTool.Location = new System.Drawing.Point(24, 19);
         this.buttonDiffTool.Name = "buttonDiffTool";
         this.buttonDiffTool.Size = new System.Drawing.Size(83, 27);
         this.buttonDiffTool.TabIndex = 11;
         this.buttonDiffTool.Text = "Diff Tool";
         this.toolTip.SetToolTip(this.buttonDiffTool, "Press Alt-K to create a new discussion");
         this.buttonDiffTool.UseVisualStyleBackColor = true;
         this.buttonDiffTool.Click += new System.EventHandler(this.ButtonDifftool_Click);
         // 
         // buttonDiscussions
         // 
         this.buttonDiscussions.Enabled = false;
         this.buttonDiscussions.Location = new System.Drawing.Point(135, 19);
         this.buttonDiscussions.Name = "buttonDiscussions";
         this.buttonDiscussions.Size = new System.Drawing.Size(83, 27);
         this.buttonDiscussions.TabIndex = 12;
         this.buttonDiscussions.Text = "Discussions";
         this.toolTip.SetToolTip(this.buttonDiscussions, "List of all discussions");
         this.buttonDiscussions.UseVisualStyleBackColor = true;
         this.buttonDiscussions.Click += new System.EventHandler(this.ButtonDiscussions_Click);
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
         this.comboBoxDCDepth.Location = new System.Drawing.Point(106, 88);
         this.comboBoxDCDepth.Name = "comboBoxDCDepth";
         this.comboBoxDCDepth.Size = new System.Drawing.Size(58, 21);
         this.comboBoxDCDepth.TabIndex = 8;
         this.toolTip.SetToolTip(this.comboBoxDCDepth, "Number of lines under the line the discussion was created for.");
         this.comboBoxDCDepth.SelectedIndexChanged += new System.EventHandler(this.comboBoxDCDepth_SelectedIndexChanged);
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
         this.tabControl.Location = new System.Drawing.Point(9, 12);
         this.tabControl.Name = "tabControl";
         this.tabControl.SelectedIndex = 0;
         this.tabControl.Size = new System.Drawing.Size(533, 584);
         this.tabControl.TabIndex = 0;
         // 
         // tabPageSettings
         // 
         this.tabPageSettings.Controls.Add(this.groupBoxOther);
         this.tabPageSettings.Controls.Add(this.groupBoxGit);
         this.tabPageSettings.Controls.Add(this.groupBoxKnownHosts);
         this.tabPageSettings.Location = new System.Drawing.Point(4, 22);
         this.tabPageSettings.Name = "tabPageSettings";
         this.tabPageSettings.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSettings.Size = new System.Drawing.Size(525, 558);
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
         this.groupBoxOther.Controls.Add(this.checkBoxShowPublicOnly);
         this.groupBoxOther.Controls.Add(this.checkBoxRequireTimer);
         this.groupBoxOther.Location = new System.Drawing.Point(6, 222);
         this.groupBoxOther.Name = "groupBoxOther";
         this.groupBoxOther.Size = new System.Drawing.Size(513, 144);
         this.groupBoxOther.TabIndex = 2;
         this.groupBoxOther.TabStop = false;
         this.groupBoxOther.Text = "Other";
         // 
         // comboBoxColorSchemes
         // 
         this.comboBoxColorSchemes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxColorSchemes.FormattingEnabled = true;
         this.comboBoxColorSchemes.Location = new System.Drawing.Point(106, 115);
         this.comboBoxColorSchemes.Name = "comboBoxColorSchemes";
         this.comboBoxColorSchemes.Size = new System.Drawing.Size(159, 21);
         this.comboBoxColorSchemes.TabIndex = 9;
         this.comboBoxColorSchemes.SelectedIndexChanged += new System.EventHandler(this.ComboBoxColorSchemes_SelectedIndexChanged);
         // 
         // labelColorScheme
         // 
         this.labelColorScheme.AutoSize = true;
         this.labelColorScheme.Location = new System.Drawing.Point(6, 118);
         this.labelColorScheme.Name = "labelColorScheme";
         this.labelColorScheme.Size = new System.Drawing.Size(71, 13);
         this.labelColorScheme.TabIndex = 8;
         this.labelColorScheme.Text = "Color scheme";
         // 
         // labelDepth
         // 
         this.labelDepth.AutoSize = true;
         this.labelDepth.Location = new System.Drawing.Point(6, 91);
         this.labelDepth.Name = "labelDepth";
         this.labelDepth.Size = new System.Drawing.Size(94, 13);
         this.labelDepth.TabIndex = 5;
         this.labelDepth.Text = "Diff Context Depth";
         // 
         // checkBoxMinimizeOnClose
         // 
         this.checkBoxMinimizeOnClose.AutoSize = true;
         this.checkBoxMinimizeOnClose.Location = new System.Drawing.Point(6, 65);
         this.checkBoxMinimizeOnClose.Name = "checkBoxMinimizeOnClose";
         this.checkBoxMinimizeOnClose.Size = new System.Drawing.Size(109, 17);
         this.checkBoxMinimizeOnClose.TabIndex = 7;
         this.checkBoxMinimizeOnClose.Text = "Minimize on close";
         this.checkBoxMinimizeOnClose.UseVisualStyleBackColor = true;
         this.checkBoxMinimizeOnClose.CheckedChanged += new System.EventHandler(this.CheckBoxMinimizeOnClose_CheckedChanged);
         // 
         // checkBoxShowPublicOnly
         // 
         this.checkBoxShowPublicOnly.AutoSize = true;
         this.checkBoxShowPublicOnly.Location = new System.Drawing.Point(6, 42);
         this.checkBoxShowPublicOnly.Name = "checkBoxShowPublicOnly";
         this.checkBoxShowPublicOnly.Size = new System.Drawing.Size(206, 17);
         this.checkBoxShowPublicOnly.TabIndex = 6;
         this.checkBoxShowPublicOnly.Text = "Show projects with public visibility only";
         this.checkBoxShowPublicOnly.UseVisualStyleBackColor = true;
         this.checkBoxShowPublicOnly.CheckedChanged += new System.EventHandler(this.CheckBoxShowPublicOnly_CheckedChanged);
         // 
         // checkBoxRequireTimer
         // 
         this.checkBoxRequireTimer.AutoSize = true;
         this.checkBoxRequireTimer.Location = new System.Drawing.Point(6, 19);
         this.checkBoxRequireTimer.Name = "checkBoxRequireTimer";
         this.checkBoxRequireTimer.Size = new System.Drawing.Size(259, 17);
         this.checkBoxRequireTimer.TabIndex = 5;
         this.checkBoxRequireTimer.Text = "Require started timer for creating new discussions";
         this.checkBoxRequireTimer.UseVisualStyleBackColor = true;
         this.checkBoxRequireTimer.CheckedChanged += new System.EventHandler(this.CheckBoxRequireTimer_CheckedChanged);
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
         // tabPageMR
         // 
         this.tabPageMR.Controls.Add(this.labelAutoUpdate);
         this.tabPageMR.Controls.Add(this.linkLabelAbortGit);
         this.tabPageMR.Controls.Add(this.labelGitLabStatus);
         this.tabPageMR.Controls.Add(this.labelGitStatus);
         this.tabPageMR.Controls.Add(this.groupBoxSelectProject);
         this.tabPageMR.Controls.Add(this.groupBoxActions);
         this.tabPageMR.Controls.Add(this.groupBoxReview);
         this.tabPageMR.Controls.Add(this.groupBoxTimeTracking);
         this.tabPageMR.Controls.Add(this.groupBoxDescription);
         this.tabPageMR.Controls.Add(this.groupBoxSelectMergeRequest);
         this.tabPageMR.Controls.Add(this.groupBoxHost);
         this.tabPageMR.Controls.Add(this.groupBoxDiff);
         this.tabPageMR.Location = new System.Drawing.Point(4, 22);
         this.tabPageMR.Name = "tabPageMR";
         this.tabPageMR.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageMR.Size = new System.Drawing.Size(525, 558);
         this.tabPageMR.TabIndex = 1;
         this.tabPageMR.Text = "Merge Requests";
         this.tabPageMR.UseVisualStyleBackColor = true;
         // 
         // linkLabelAbortGit
         // 
         this.linkLabelAbortGit.AutoSize = true;
         this.linkLabelAbortGit.Location = new System.Drawing.Point(484, 518);
         this.linkLabelAbortGit.Name = "linkLabelAbortGit";
         this.linkLabelAbortGit.Size = new System.Drawing.Size(32, 13);
         this.linkLabelAbortGit.TabIndex = 25;
         this.linkLabelAbortGit.TabStop = true;
         this.linkLabelAbortGit.Text = "Abort";
         this.linkLabelAbortGit.Visible = false;
         this.linkLabelAbortGit.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelAbortGit_LinkClicked);
         // 
         // labelGitLabStatus
         // 
         this.labelGitLabStatus.AutoEllipsis = true;
         this.labelGitLabStatus.AutoSize = true;
         this.labelGitLabStatus.Location = new System.Drawing.Point(3, 542);
         this.labelGitLabStatus.Name = "labelGitLabStatus";
         this.labelGitLabStatus.Size = new System.Drawing.Size(0, 13);
         this.labelGitLabStatus.TabIndex = 24;
         // 
         // labelGitStatus
         // 
         this.labelGitStatus.AutoEllipsis = true;
         this.labelGitStatus.Location = new System.Drawing.Point(6, 518);
         this.labelGitStatus.Name = "labelGitStatus";
         this.labelGitStatus.Size = new System.Drawing.Size(472, 13);
         this.labelGitStatus.TabIndex = 23;
         // 
         // groupBoxSelectProject
         // 
         this.groupBoxSelectProject.Controls.Add(this.comboBoxProjects);
         this.groupBoxSelectProject.Location = new System.Drawing.Point(232, 6);
         this.groupBoxSelectProject.Name = "groupBoxSelectProject";
         this.groupBoxSelectProject.Size = new System.Drawing.Size(284, 45);
         this.groupBoxSelectProject.TabIndex = 2;
         this.groupBoxSelectProject.TabStop = false;
         this.groupBoxSelectProject.Text = "Select Project";
         // 
         // groupBoxActions
         // 
         this.groupBoxActions.Location = new System.Drawing.Point(6, 460);
         this.groupBoxActions.Name = "groupBoxActions";
         this.groupBoxActions.Size = new System.Drawing.Size(513, 55);
         this.groupBoxActions.TabIndex = 22;
         this.groupBoxActions.TabStop = false;
         this.groupBoxActions.Text = "Actions";
         // 
         // groupBoxReview
         // 
         this.groupBoxReview.Controls.Add(this.buttonDiscussions);
         this.groupBoxReview.Controls.Add(this.buttonDiffTool);
         this.groupBoxReview.Location = new System.Drawing.Point(276, 399);
         this.groupBoxReview.Name = "groupBoxReview";
         this.groupBoxReview.Size = new System.Drawing.Size(243, 55);
         this.groupBoxReview.TabIndex = 11;
         this.groupBoxReview.TabStop = false;
         this.groupBoxReview.Text = "Review";
         // 
         // groupBoxTimeTracking
         // 
         this.groupBoxTimeTracking.Controls.Add(this.labelSpentTime);
         this.groupBoxTimeTracking.Controls.Add(this.buttonToggleTimer);
         this.groupBoxTimeTracking.Controls.Add(this.labelSpentTimeLabel);
         this.groupBoxTimeTracking.Location = new System.Drawing.Point(6, 399);
         this.groupBoxTimeTracking.Name = "groupBoxTimeTracking";
         this.groupBoxTimeTracking.Size = new System.Drawing.Size(253, 55);
         this.groupBoxTimeTracking.TabIndex = 10;
         this.groupBoxTimeTracking.TabStop = false;
         this.groupBoxTimeTracking.Text = "Time Tracking";
         // 
         // labelSpentTime
         // 
         this.labelSpentTime.AutoSize = true;
         this.labelSpentTime.Location = new System.Drawing.Point(171, 26);
         this.labelSpentTime.Name = "labelSpentTime";
         this.labelSpentTime.Size = new System.Drawing.Size(49, 13);
         this.labelSpentTime.TabIndex = 24;
         this.labelSpentTime.Text = "00:00:00";
         // 
         // buttonToggleTimer
         // 
         this.buttonToggleTimer.Enabled = false;
         this.buttonToggleTimer.Location = new System.Drawing.Point(4, 19);
         this.buttonToggleTimer.Name = "buttonToggleTimer";
         this.buttonToggleTimer.Size = new System.Drawing.Size(83, 27);
         this.buttonToggleTimer.TabIndex = 10;
         this.buttonToggleTimer.UseVisualStyleBackColor = true;
         this.buttonToggleTimer.Click += new System.EventHandler(this.ButtonToggleTimer_Click);
         // 
         // labelSpentTimeLabel
         // 
         this.labelSpentTimeLabel.AutoSize = true;
         this.labelSpentTimeLabel.Location = new System.Drawing.Point(103, 26);
         this.labelSpentTimeLabel.Name = "labelSpentTimeLabel";
         this.labelSpentTimeLabel.Size = new System.Drawing.Size(64, 13);
         this.labelSpentTimeLabel.TabIndex = 19;
         this.labelSpentTimeLabel.Text = "Spent Time:";
         // 
         // groupBoxDescription
         // 
         this.groupBoxDescription.Controls.Add(this.textBoxMergeRequestName);
         this.groupBoxDescription.Controls.Add(this.richTextBoxMergeRequestDescription);
         this.groupBoxDescription.Location = new System.Drawing.Point(4, 157);
         this.groupBoxDescription.Name = "groupBoxDescription";
         this.groupBoxDescription.Size = new System.Drawing.Size(512, 163);
         this.groupBoxDescription.TabIndex = 15;
         this.groupBoxDescription.TabStop = false;
         this.groupBoxDescription.Text = "Merge Request";
         // 
         // textBoxMergeRequestName
         // 
         this.textBoxMergeRequestName.Location = new System.Drawing.Point(6, 19);
         this.textBoxMergeRequestName.Name = "textBoxMergeRequestName";
         this.textBoxMergeRequestName.ReadOnly = true;
         this.textBoxMergeRequestName.Size = new System.Drawing.Size(500, 20);
         this.textBoxMergeRequestName.TabIndex = 0;
         this.textBoxMergeRequestName.TabStop = false;
         // 
         // richTextBoxMergeRequestDescription
         // 
         this.richTextBoxMergeRequestDescription.Location = new System.Drawing.Point(6, 45);
         this.richTextBoxMergeRequestDescription.Name = "richTextBoxMergeRequestDescription";
         this.richTextBoxMergeRequestDescription.ReadOnly = true;
         this.richTextBoxMergeRequestDescription.Size = new System.Drawing.Size(500, 110);
         this.richTextBoxMergeRequestDescription.TabIndex = 1;
         this.richTextBoxMergeRequestDescription.TabStop = false;
         this.richTextBoxMergeRequestDescription.Text = "";
         // 
         // groupBoxHost
         // 
         this.groupBoxHost.Controls.Add(this.comboBoxHost);
         this.groupBoxHost.Location = new System.Drawing.Point(6, 6);
         this.groupBoxHost.Name = "groupBoxHost";
         this.groupBoxHost.Size = new System.Drawing.Size(220, 45);
         this.groupBoxHost.TabIndex = 1;
         this.groupBoxHost.TabStop = false;
         this.groupBoxHost.Text = "Select Host";
         // 
         // groupBoxDiff
         // 
         this.groupBoxDiff.Controls.Add(this.comboBoxRightVersion);
         this.groupBoxDiff.Controls.Add(this.comboBoxLeftVersion);
         this.groupBoxDiff.Controls.Add(this.label3);
         this.groupBoxDiff.Controls.Add(this.label4);
         this.groupBoxDiff.Location = new System.Drawing.Point(6, 326);
         this.groupBoxDiff.Name = "groupBoxDiff";
         this.groupBoxDiff.Size = new System.Drawing.Size(511, 67);
         this.groupBoxDiff.TabIndex = 8;
         this.groupBoxDiff.TabStop = false;
         this.groupBoxDiff.Text = "Select Versions";
         // 
         // label3
         // 
         this.label3.AutoSize = true;
         this.label3.Location = new System.Drawing.Point(269, 16);
         this.label3.Name = "label3";
         this.label3.Size = new System.Drawing.Size(25, 13);
         this.label3.TabIndex = 18;
         this.label3.Text = "and";
         // 
         // label4
         // 
         this.label4.AutoSize = true;
         this.label4.Location = new System.Drawing.Point(3, 16);
         this.label4.Name = "label4";
         this.label4.Size = new System.Drawing.Size(93, 13);
         this.label4.TabIndex = 15;
         this.label4.Text = "Changes between";
         // 
         // labelAutoUpdate
         // 
         this.labelAutoUpdate.AutoSize = true;
         this.labelAutoUpdate.Location = new System.Drawing.Point(273, 542);
         this.labelAutoUpdate.Name = "labelAutoUpdate";
         this.labelAutoUpdate.Size = new System.Drawing.Size(152, 13);
         this.labelAutoUpdate.TabIndex = 26;
         this.labelAutoUpdate.Text = "Checking for updates...";
         this.labelAutoUpdate.Visible = false;
         // 
         // comboBoxProjects
         // 
         this.comboBoxProjects.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxProjects.FormattingEnabled = true;
         this.comboBoxProjects.Location = new System.Drawing.Point(6, 15);
         this.comboBoxProjects.Name = "comboBoxProjects";
         this.comboBoxProjects.Size = new System.Drawing.Size(272, 21);
         this.comboBoxProjects.Sorted = true;
         this.comboBoxProjects.TabIndex = 2;
         this.comboBoxProjects.SelectedIndexChanged += new System.EventHandler(this.ComboBoxProjects_SelectedIndexChanged);
         this.comboBoxProjects.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxProjects_Format);
         // 
         // comboBoxFilteredMergeRequests
         // 
         this.comboBoxFilteredMergeRequests.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxFilteredMergeRequests.FormattingEnabled = true;
         this.comboBoxFilteredMergeRequests.Location = new System.Drawing.Point(6, 46);
         this.comboBoxFilteredMergeRequests.Name = "comboBoxFilteredMergeRequests";
         this.comboBoxFilteredMergeRequests.Size = new System.Drawing.Size(498, 21);
         this.comboBoxFilteredMergeRequests.TabIndex = 6;
         this.comboBoxFilteredMergeRequests.SelectedIndexChanged += new System.EventHandler(this.ComboBoxFilteredMergeRequests_SelectedIndexChanged);
         this.comboBoxFilteredMergeRequests.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxFilteredMergeRequests_Format);
         // 
         // comboBoxHost
         // 
         this.comboBoxHost.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxHost.FormattingEnabled = true;
         this.comboBoxHost.Location = new System.Drawing.Point(9, 15);
         this.comboBoxHost.Name = "comboBoxHost";
         this.comboBoxHost.Size = new System.Drawing.Size(205, 21);
         this.comboBoxHost.TabIndex = 1;
         this.comboBoxHost.SelectedIndexChanged += new System.EventHandler(this.ComboBoxHost_SelectedIndexChanged);
         this.comboBoxHost.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxHost_Format);
         // 
         // comboBoxRightVersion
         // 
         this.comboBoxRightVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxRightVersion.FormattingEnabled = true;
         this.comboBoxRightVersion.Location = new System.Drawing.Point(269, 32);
         this.comboBoxRightVersion.Name = "comboBoxRightVersion";
         this.comboBoxRightVersion.Size = new System.Drawing.Size(236, 21);
         this.comboBoxRightVersion.TabIndex = 9;
         this.comboBoxRightVersion.SelectedIndexChanged += new System.EventHandler(this.ComboBoxRightVersion_SelectedIndexChanged);
         this.comboBoxRightVersion.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxVersion_Format);
         // 
         // comboBoxLeftVersion
         // 
         this.comboBoxLeftVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxLeftVersion.FormattingEnabled = true;
         this.comboBoxLeftVersion.Location = new System.Drawing.Point(4, 32);
         this.comboBoxLeftVersion.Name = "comboBoxLeftVersion";
         this.comboBoxLeftVersion.Size = new System.Drawing.Size(249, 21);
         this.comboBoxLeftVersion.TabIndex = 8;
         this.comboBoxLeftVersion.SelectedIndexChanged += new System.EventHandler(this.ComboBoxLeftVersion_SelectedIndexChanged);
         this.comboBoxLeftVersion.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxVersion_Format);
         // 
         // mrHelperForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(548, 608);
         this.Controls.Add(this.tabControl);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "mrHelperForm";
         this.Text = "Merge Request Helper";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MrHelperForm_FormClosing);
         this.Load += new System.EventHandler(this.MrHelperForm_Load);
         this.groupBoxKnownHosts.ResumeLayout(false);
         this.groupBoxSelectMergeRequest.ResumeLayout(false);
         this.groupBoxSelectMergeRequest.PerformLayout();
         this.contextMenuStrip.ResumeLayout(false);
         this.tabControl.ResumeLayout(false);
         this.tabPageSettings.ResumeLayout(false);
         this.groupBoxOther.ResumeLayout(false);
         this.groupBoxOther.PerformLayout();
         this.groupBoxGit.ResumeLayout(false);
         this.groupBoxGit.PerformLayout();
         this.tabPageMR.ResumeLayout(false);
         this.tabPageMR.PerformLayout();
         this.groupBoxSelectProject.ResumeLayout(false);
         this.groupBoxReview.ResumeLayout(false);
         this.groupBoxTimeTracking.ResumeLayout(false);
         this.groupBoxTimeTracking.PerformLayout();
         this.groupBoxDescription.ResumeLayout(false);
         this.groupBoxDescription.PerformLayout();
         this.groupBoxHost.ResumeLayout(false);
         this.groupBoxDiff.ResumeLayout(false);
         this.groupBoxDiff.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxKnownHosts;
      private System.Windows.Forms.GroupBox groupBoxSelectMergeRequest;
      private System.Windows.Forms.ToolTip toolTip;
      private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
      private System.Windows.Forms.ToolStripMenuItem restoreToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
      private System.Windows.Forms.NotifyIcon notifyIcon;
      private System.Windows.Forms.Label labelLocalGitFolder;
      private System.Windows.Forms.FolderBrowserDialog localGitFolderBrowser;
      private System.Windows.Forms.Button buttonBrowseLocalGitFolder;
      private System.Windows.Forms.TextBox textBoxLocalGitFolder;
      private SelectionPreservingComboBox comboBoxFilteredMergeRequests;
      private System.Windows.Forms.TabControl tabControl;
      private System.Windows.Forms.TabPage tabPageSettings;
      private System.Windows.Forms.GroupBox groupBoxGit;
      private System.Windows.Forms.TabPage tabPageMR;
      private System.Windows.Forms.LinkLabel linkLabelConnectedTo;
      private System.Windows.Forms.GroupBox groupBoxDiff;
      private System.Windows.Forms.GroupBox groupBoxDescription;
      private System.Windows.Forms.RichTextBox richTextBoxMergeRequestDescription;
      private System.Windows.Forms.TextBox textBoxMergeRequestName;
      private System.Windows.Forms.CheckBox checkBoxLabels;
      private System.Windows.Forms.TextBox textBoxLabels;
      private System.Windows.Forms.Label labelSpentTime;
      private System.Windows.Forms.Label labelSpentTimeLabel;
      private System.Windows.Forms.Button buttonToggleTimer;
      private SelectionPreservingComboBox comboBoxLeftVersion;
      private SelectionPreservingComboBox comboBoxRightVersion;
      private System.Windows.Forms.Label label3;
      private System.Windows.Forms.Button buttonDiffTool;
      private System.Windows.Forms.Label label4;
      private SelectionPreservingComboBox comboBoxHost;
      private System.Windows.Forms.GroupBox groupBoxHost;
      private System.Windows.Forms.GroupBox groupBoxReview;
      private System.Windows.Forms.GroupBox groupBoxTimeTracking;
      private System.Windows.Forms.GroupBox groupBoxActions;
      private System.Windows.Forms.GroupBox groupBoxOther;
      private System.Windows.Forms.CheckBox checkBoxRequireTimer;
      private System.Windows.Forms.Button buttonRemoveKnownHost;
      private System.Windows.Forms.Button buttonAddKnownHost;
      private System.Windows.Forms.ListView listViewKnownHosts;
      private System.Windows.Forms.ColumnHeader columnHeaderHost;
      private System.Windows.Forms.ColumnHeader columnHeaderAccessToken;
      private System.Windows.Forms.Button buttonApplyLabels;
      private System.Windows.Forms.GroupBox groupBoxSelectProject;
      private SelectionPreservingComboBox comboBoxProjects;
      private System.Windows.Forms.CheckBox checkBoxShowPublicOnly;
      private System.Windows.Forms.Button buttonDiscussions;
      private System.Windows.Forms.Label labelDepth;
      private System.Windows.Forms.ComboBox comboBoxDCDepth;
      private System.Windows.Forms.CheckBox checkBoxMinimizeOnClose;
      private System.Windows.Forms.ComboBox comboBoxColorSchemes;
      private System.Windows.Forms.Label labelColorScheme;
      private System.Windows.Forms.Label labelGitStatus;
      private System.Windows.Forms.Label labelGitLabStatus;
      private System.Windows.Forms.LinkLabel linkLabelAbortGit;
      private System.Windows.Forms.Label labelAutoUpdate;
   }
}

