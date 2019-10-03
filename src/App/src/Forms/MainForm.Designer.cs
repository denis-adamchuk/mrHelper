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
         this.groupBoxSelectMergeRequest = new System.Windows.Forms.GroupBox();
         this.listViewMergeRequests = new System.Windows.Forms.ListView();
         this.columnHeaderId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderAuthor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderProject = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.textBoxLabels = new System.Windows.Forms.TextBox();
         this.checkBoxLabels = new System.Windows.Forms.CheckBox();
         this.buttonBrowseLocalGitFolder = new System.Windows.Forms.Button();
         this.textBoxLocalGitFolder = new System.Windows.Forms.TextBox();
         this.labelLocalGitFolder = new System.Windows.Forms.Label();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.comboBoxDCDepth = new System.Windows.Forms.ComboBox();
         this.buttonDiffTool = new System.Windows.Forms.Button();
         this.buttonNewDiscussion = new System.Windows.Forms.Button();
         this.buttonAddComment = new System.Windows.Forms.Button();
         this.buttonDiscussions = new System.Windows.Forms.Button();
         this.buttonEditTime = new System.Windows.Forms.Button();
         this.comboBoxHost = new mrHelper.CommonControls.SelectionPreservingComboBox();
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
         this.groupBoxGit = new System.Windows.Forms.GroupBox();
         this.groupBoxHost = new System.Windows.Forms.GroupBox();
         this.tabPageMR = new System.Windows.Forms.TabPage();
         this.panel1 = new System.Windows.Forms.Panel();
         this.panel3 = new System.Windows.Forms.Panel();
         this.linkLabelAbortGit = new System.Windows.Forms.LinkLabel();
         this.labelWorkflowStatus = new System.Windows.Forms.Label();
         this.labelGitStatus = new System.Windows.Forms.Label();
         this.groupBoxTimeTracking = new System.Windows.Forms.GroupBox();
         this.labelTimeTrackingMergeRequestName = new System.Windows.Forms.Label();
         this.buttonTimeTrackingCancel = new System.Windows.Forms.Button();
         this.labelTimeTrackingTrackedTime = new System.Windows.Forms.Label();
         this.buttonTimeTrackingStart = new System.Windows.Forms.Button();
         this.labelTimeTrackingTrackedLabel = new System.Windows.Forms.Label();
         this.panel2 = new System.Windows.Forms.Panel();
         this.groupBoxReview = new System.Windows.Forms.GroupBox();
         this.groupBoxActions = new System.Windows.Forms.GroupBox();
         this.groupBoxDiff = new System.Windows.Forms.GroupBox();
         this.comboBoxRightCommit = new mrHelper.CommonControls.SelectionPreservingComboBox();
         this.comboBoxLeftCommit = new mrHelper.CommonControls.SelectionPreservingComboBox();
         this.groupBoxDescription = new System.Windows.Forms.GroupBox();
         this.richTextBoxMergeRequestDescription = new System.Windows.Forms.RichTextBox();
         this.linkLabelConnectedTo = new System.Windows.Forms.LinkLabel();
         this.groupBoxKnownHosts.SuspendLayout();
         this.groupBoxSelectMergeRequest.SuspendLayout();
         this.contextMenuStrip.SuspendLayout();
         this.tabControl.SuspendLayout();
         this.tabPageSettings.SuspendLayout();
         this.groupBoxOther.SuspendLayout();
         this.groupBoxGit.SuspendLayout();
         this.groupBoxHost.SuspendLayout();
         this.tabPageMR.SuspendLayout();
         this.panel1.SuspendLayout();
         this.panel3.SuspendLayout();
         this.groupBoxTimeTracking.SuspendLayout();
         this.panel2.SuspendLayout();
         this.groupBoxReview.SuspendLayout();
         this.groupBoxDiff.SuspendLayout();
         this.groupBoxDescription.SuspendLayout();
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
         this.groupBoxSelectMergeRequest.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxSelectMergeRequest.Controls.Add(this.listViewMergeRequests);
         this.groupBoxSelectMergeRequest.Controls.Add(this.textBoxLabels);
         this.groupBoxSelectMergeRequest.Controls.Add(this.checkBoxLabels);
         this.groupBoxSelectMergeRequest.Location = new System.Drawing.Point(3, 3);
         this.groupBoxSelectMergeRequest.Name = "groupBoxSelectMergeRequest";
         this.groupBoxSelectMergeRequest.Size = new System.Drawing.Size(567, 572);
         this.groupBoxSelectMergeRequest.TabIndex = 3;
         this.groupBoxSelectMergeRequest.TabStop = false;
         this.groupBoxSelectMergeRequest.Text = "Select Merge Request";
         // 
         // listViewMergeRequests
         // 
         this.listViewMergeRequests.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listViewMergeRequests.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderId,
            this.columnHeaderAuthor,
            this.columnHeaderProject,
            this.columnHeaderTitle});
         this.listViewMergeRequests.FullRowSelect = true;
         this.listViewMergeRequests.GridLines = true;
         this.listViewMergeRequests.HideSelection = false;
         this.listViewMergeRequests.Location = new System.Drawing.Point(0, 44);
         this.listViewMergeRequests.MultiSelect = false;
         this.listViewMergeRequests.Name = "listViewMergeRequests";
         this.listViewMergeRequests.OwnerDraw = true;
         this.listViewMergeRequests.Size = new System.Drawing.Size(561, 522);
         this.listViewMergeRequests.TabIndex = 5;
         this.listViewMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewMergeRequests.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.listViewMergeRequests_DrawColumnHeader);
         this.listViewMergeRequests.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.listViewMergeRequests_DrawItem);
         this.listViewMergeRequests.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.listViewMergeRequests_DrawSubItem);
         this.listViewMergeRequests.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewMergeRequests_ItemSelectionChanged);
         this.listViewMergeRequests.SelectedIndexChanged += new System.EventHandler(this.ListViewMergeRequests_SelectedIndexChanged);
         this.listViewMergeRequests.MouseClick += new System.Windows.Forms.MouseEventHandler(this.listViewMergeRequests_MouseClick);
         // 
         // columnHeaderId
         // 
         this.columnHeaderId.Text = "Id";
         this.columnHeaderId.Width = 34;
         // 
         // columnHeaderAuthor
         // 
         this.columnHeaderAuthor.Text = "Author";
         this.columnHeaderAuthor.Width = 106;
         // 
         // columnHeaderProject
         // 
         this.columnHeaderProject.Text = "Project";
         this.columnHeaderProject.Width = 80;
         // 
         // columnHeaderTitle
         // 
         this.columnHeaderTitle.Text = "TItle";
         this.columnHeaderTitle.Width = 400;
         // 
         // textBoxLabels
         // 
         this.textBoxLabels.Location = new System.Drawing.Point(69, 18);
         this.textBoxLabels.Name = "textBoxLabels";
         this.textBoxLabels.Size = new System.Drawing.Size(568, 20);
         this.textBoxLabels.TabIndex = 4;
         this.toolTip.SetToolTip(this.textBoxLabels, "Show merge requests that contain any of these labels (comma-separated list is exp" +
        "ected)");
         this.textBoxLabels.LostFocus += new System.EventHandler(this.TextBoxLabels_LostFocus);
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
         this.comboBoxDCDepth.Location = new System.Drawing.Point(106, 65);
         this.comboBoxDCDepth.Name = "comboBoxDCDepth";
         this.comboBoxDCDepth.Size = new System.Drawing.Size(58, 21);
         this.comboBoxDCDepth.TabIndex = 8;
         this.toolTip.SetToolTip(this.comboBoxDCDepth, "Number of lines under the line the discussion was created for.");
         this.comboBoxDCDepth.SelectedIndexChanged += new System.EventHandler(this.comboBoxDCDepth_SelectedIndexChanged);
         // 
         // buttonDiffTool
         // 
         this.buttonDiffTool.Location = new System.Drawing.Point(565, 17);
         this.buttonDiffTool.Name = "buttonDiffTool";
         this.buttonDiffTool.Size = new System.Drawing.Size(75, 23);
         this.buttonDiffTool.TabIndex = 2;
         this.buttonDiffTool.Text = "Diff Tool";
         this.toolTip.SetToolTip(this.buttonDiffTool, "Launch diff tool to review diff between selected commits");
         this.buttonDiffTool.UseVisualStyleBackColor = true;
         this.buttonDiffTool.Click += new System.EventHandler(this.ButtonDifftool_Click);
         // 
         // buttonNewDiscussion
         // 
         this.buttonNewDiscussion.Enabled = false;
         this.buttonNewDiscussion.Location = new System.Drawing.Point(9, 19);
         this.buttonNewDiscussion.Name = "buttonNewDiscussion";
         this.buttonNewDiscussion.Size = new System.Drawing.Size(103, 27);
         this.buttonNewDiscussion.TabIndex = 13;
         this.buttonNewDiscussion.Text = "New Discussion";
         this.toolTip.SetToolTip(this.buttonNewDiscussion, "Create a new resolvable discussion");
         this.buttonNewDiscussion.UseVisualStyleBackColor = true;
         // 
         // buttonAddComment
         // 
         this.buttonAddComment.Enabled = false;
         this.buttonAddComment.Location = new System.Drawing.Point(238, 19);
         this.buttonAddComment.Name = "buttonAddComment";
         this.buttonAddComment.Size = new System.Drawing.Size(103, 27);
         this.buttonAddComment.TabIndex = 14;
         this.buttonAddComment.Text = "Add comment";
         this.toolTip.SetToolTip(this.buttonAddComment, "Leave a comment (cannot be resolved and replied)");
         this.buttonAddComment.UseVisualStyleBackColor = true;
         // 
         // buttonDiscussions
         // 
         this.buttonDiscussions.Enabled = false;
         this.buttonDiscussions.Location = new System.Drawing.Point(137, 19);
         this.buttonDiscussions.Name = "buttonDiscussions";
         this.buttonDiscussions.Size = new System.Drawing.Size(83, 27);
         this.buttonDiscussions.TabIndex = 12;
         this.buttonDiscussions.Text = "Discussions";
         this.toolTip.SetToolTip(this.buttonDiscussions, "Show full list of Discussions");
         this.buttonDiscussions.UseVisualStyleBackColor = true;
         // 
         // buttonEditTime
         // 
         this.buttonEditTime.Enabled = false;
         this.buttonEditTime.Location = new System.Drawing.Point(538, 19);
         this.buttonEditTime.Name = "buttonEditTime";
         this.buttonEditTime.Size = new System.Drawing.Size(83, 27);
         this.buttonEditTime.TabIndex = 28;
         this.buttonEditTime.Text = "Edit";
         this.toolTip.SetToolTip(this.buttonEditTime, "Edit total time tracked on this merge request");
         this.buttonEditTime.UseVisualStyleBackColor = true;
         // 
         // comboBoxHost
         // 
         this.comboBoxHost.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxHost.FormattingEnabled = true;
         this.comboBoxHost.Location = new System.Drawing.Point(6, 31);
         this.comboBoxHost.Name = "comboBoxHost";
         this.comboBoxHost.Size = new System.Drawing.Size(250, 21);
         this.comboBoxHost.TabIndex = 0;
         this.toolTip.SetToolTip(this.comboBoxHost, "Select a host from a list of known hosts");
         this.comboBoxHost.SelectionChangeCommitted += new System.EventHandler(this.ComboBoxHost_SelectionChangeCommited);
         this.comboBoxHost.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxHost_Format);
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
         this.tabControl.Size = new System.Drawing.Size(1310, 604);
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
         this.tabPageSettings.Size = new System.Drawing.Size(1302, 578);
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
         this.comboBoxColorSchemes.Location = new System.Drawing.Point(106, 92);
         this.comboBoxColorSchemes.Name = "comboBoxColorSchemes";
         this.comboBoxColorSchemes.Size = new System.Drawing.Size(159, 21);
         this.comboBoxColorSchemes.TabIndex = 9;
         this.comboBoxColorSchemes.SelectionChangeCommitted += new System.EventHandler(this.ComboBoxColorSchemes_SelectionChangeCommited);
         // 
         // labelColorScheme
         // 
         this.labelColorScheme.AutoSize = true;
         this.labelColorScheme.Location = new System.Drawing.Point(6, 95);
         this.labelColorScheme.Name = "labelColorScheme";
         this.labelColorScheme.Size = new System.Drawing.Size(71, 13);
         this.labelColorScheme.TabIndex = 8;
         this.labelColorScheme.Text = "Color scheme";
         // 
         // labelDepth
         // 
         this.labelDepth.AutoSize = true;
         this.labelDepth.Location = new System.Drawing.Point(6, 68);
         this.labelDepth.Name = "labelDepth";
         this.labelDepth.Size = new System.Drawing.Size(94, 13);
         this.labelDepth.TabIndex = 5;
         this.labelDepth.Text = "Diff Context Depth";
         // 
         // checkBoxMinimizeOnClose
         // 
         this.checkBoxMinimizeOnClose.AutoSize = true;
         this.checkBoxMinimizeOnClose.Location = new System.Drawing.Point(6, 42);
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
         this.checkBoxShowPublicOnly.Location = new System.Drawing.Point(6, 19);
         this.checkBoxShowPublicOnly.Name = "checkBoxShowPublicOnly";
         this.checkBoxShowPublicOnly.Size = new System.Drawing.Size(206, 17);
         this.checkBoxShowPublicOnly.TabIndex = 6;
         this.checkBoxShowPublicOnly.Text = "Show projects with public visibility only";
         this.checkBoxShowPublicOnly.UseVisualStyleBackColor = true;
         this.checkBoxShowPublicOnly.CheckedChanged += new System.EventHandler(this.CheckBoxShowPublicOnly_CheckedChanged);
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
         // tabPageMR
         // 
         this.tabPageMR.Controls.Add(this.groupBoxSelectMergeRequest);
         this.tabPageMR.Controls.Add(this.panel1);
         this.tabPageMR.Location = new System.Drawing.Point(4, 22);
         this.tabPageMR.Name = "tabPageMR";
         this.tabPageMR.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageMR.Size = new System.Drawing.Size(1302, 578);
         this.tabPageMR.TabIndex = 1;
         this.tabPageMR.Text = "Merge Requests";
         this.tabPageMR.UseVisualStyleBackColor = true;
         // 
         // panel1
         // 
         this.panel1.Controls.Add(this.panel3);
         this.panel1.Controls.Add(this.groupBoxTimeTracking);
         this.panel1.Controls.Add(this.panel2);
         this.panel1.Controls.Add(this.groupBoxDiff);
         this.panel1.Controls.Add(this.groupBoxDescription);
         this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
         this.panel1.Location = new System.Drawing.Point(649, 3);
         this.panel1.Name = "panel1";
         this.panel1.Size = new System.Drawing.Size(650, 572);
         this.panel1.TabIndex = 26;
         // 
         // panel3
         // 
         this.panel3.Controls.Add(this.linkLabelAbortGit);
         this.panel3.Controls.Add(this.labelWorkflowStatus);
         this.panel3.Controls.Add(this.labelGitStatus);
         this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
         this.panel3.Location = new System.Drawing.Point(0, 478);
         this.panel3.Name = "panel3";
         this.panel3.Size = new System.Drawing.Size(650, 71);
         this.panel3.TabIndex = 37;
         // 
         // linkLabelAbortGit
         // 
         this.linkLabelAbortGit.AutoSize = true;
         this.linkLabelAbortGit.Location = new System.Drawing.Point(601, 41);
         this.linkLabelAbortGit.Name = "linkLabelAbortGit";
         this.linkLabelAbortGit.Size = new System.Drawing.Size(32, 13);
         this.linkLabelAbortGit.TabIndex = 36;
         this.linkLabelAbortGit.TabStop = true;
         this.linkLabelAbortGit.Text = "Abort";
         this.linkLabelAbortGit.Visible = false;
         // 
         // labelWorkflowStatus
         // 
         this.labelWorkflowStatus.AutoEllipsis = true;
         this.labelWorkflowStatus.Location = new System.Drawing.Point(11, 16);
         this.labelWorkflowStatus.Name = "labelWorkflowStatus";
         this.labelWorkflowStatus.Size = new System.Drawing.Size(622, 13);
         this.labelWorkflowStatus.TabIndex = 35;
         this.labelWorkflowStatus.Text = "<Workflow Status Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do " +
    "eiusmod tempor incididunt sed do eiusmod sdae>";
         // 
         // labelGitStatus
         // 
         this.labelGitStatus.AutoEllipsis = true;
         this.labelGitStatus.Location = new System.Drawing.Point(11, 41);
         this.labelGitStatus.Name = "labelGitStatus";
         this.labelGitStatus.Size = new System.Drawing.Size(569, 13);
         this.labelGitStatus.TabIndex = 34;
         this.labelGitStatus.Text = "<Git Status Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusm" +
    "od tempor incididunt incididu sde abnt>";
         // 
         // groupBoxTimeTracking
         // 
         this.groupBoxTimeTracking.Controls.Add(this.buttonEditTime);
         this.groupBoxTimeTracking.Controls.Add(this.labelTimeTrackingMergeRequestName);
         this.groupBoxTimeTracking.Controls.Add(this.buttonTimeTrackingCancel);
         this.groupBoxTimeTracking.Controls.Add(this.labelTimeTrackingTrackedTime);
         this.groupBoxTimeTracking.Controls.Add(this.buttonTimeTrackingStart);
         this.groupBoxTimeTracking.Controls.Add(this.labelTimeTrackingTrackedLabel);
         this.groupBoxTimeTracking.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxTimeTracking.Location = new System.Drawing.Point(0, 406);
         this.groupBoxTimeTracking.Name = "groupBoxTimeTracking";
         this.groupBoxTimeTracking.Size = new System.Drawing.Size(650, 72);
         this.groupBoxTimeTracking.TabIndex = 36;
         this.groupBoxTimeTracking.TabStop = false;
         this.groupBoxTimeTracking.Text = "Time Tracking";
         // 
         // labelTimeTrackingMergeRequestName
         // 
         this.labelTimeTrackingMergeRequestName.AutoEllipsis = true;
         this.labelTimeTrackingMergeRequestName.Location = new System.Drawing.Point(9, 49);
         this.labelTimeTrackingMergeRequestName.Name = "labelTimeTrackingMergeRequestName";
         this.labelTimeTrackingMergeRequestName.Size = new System.Drawing.Size(612, 13);
         this.labelTimeTrackingMergeRequestName.TabIndex = 27;
         this.labelTimeTrackingMergeRequestName.Text = "<Merge Request Name Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed " +
    "do eiusmod tempor, sed do eiusmod tempor>";
         // 
         // buttonTimeTrackingCancel
         // 
         this.buttonTimeTrackingCancel.Enabled = false;
         this.buttonTimeTrackingCancel.Location = new System.Drawing.Point(110, 19);
         this.buttonTimeTrackingCancel.Name = "buttonTimeTrackingCancel";
         this.buttonTimeTrackingCancel.Size = new System.Drawing.Size(83, 27);
         this.buttonTimeTrackingCancel.TabIndex = 25;
         this.buttonTimeTrackingCancel.Text = "Cancel";
         this.buttonTimeTrackingCancel.UseVisualStyleBackColor = true;
         // 
         // labelTimeTrackingTrackedTime
         // 
         this.labelTimeTrackingTrackedTime.AutoSize = true;
         this.labelTimeTrackingTrackedTime.Location = new System.Drawing.Point(459, 26);
         this.labelTimeTrackingTrackedTime.Name = "labelTimeTrackingTrackedTime";
         this.labelTimeTrackingTrackedTime.Size = new System.Drawing.Size(61, 13);
         this.labelTimeTrackingTrackedTime.TabIndex = 24;
         this.labelTimeTrackingTrackedTime.Text = "<00:00:00>";
         // 
         // buttonTimeTrackingStart
         // 
         this.buttonTimeTrackingStart.Enabled = false;
         this.buttonTimeTrackingStart.Location = new System.Drawing.Point(8, 19);
         this.buttonTimeTrackingStart.Name = "buttonTimeTrackingStart";
         this.buttonTimeTrackingStart.Size = new System.Drawing.Size(83, 27);
         this.buttonTimeTrackingStart.TabIndex = 10;
         this.buttonTimeTrackingStart.Text = "Start";
         this.buttonTimeTrackingStart.UseVisualStyleBackColor = true;
         // 
         // labelTimeTrackingTrackedLabel
         // 
         this.labelTimeTrackingTrackedLabel.AutoSize = true;
         this.labelTimeTrackingTrackedLabel.Location = new System.Drawing.Point(377, 26);
         this.labelTimeTrackingTrackedLabel.Name = "labelTimeTrackingTrackedLabel";
         this.labelTimeTrackingTrackedLabel.Size = new System.Drawing.Size(76, 13);
         this.labelTimeTrackingTrackedLabel.TabIndex = 19;
         this.labelTimeTrackingTrackedLabel.Text = "Tracked Time:";
         // 
         // panel2
         // 
         this.panel2.Controls.Add(this.groupBoxReview);
         this.panel2.Controls.Add(this.groupBoxActions);
         this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
         this.panel2.Location = new System.Drawing.Point(0, 329);
         this.panel2.Name = "panel2";
         this.panel2.Size = new System.Drawing.Size(650, 77);
         this.panel2.TabIndex = 35;
         // 
         // groupBoxReview
         // 
         this.groupBoxReview.Controls.Add(this.buttonNewDiscussion);
         this.groupBoxReview.Controls.Add(this.buttonAddComment);
         this.groupBoxReview.Controls.Add(this.buttonDiscussions);
         this.groupBoxReview.Location = new System.Drawing.Point(290, 3);
         this.groupBoxReview.Name = "groupBoxReview";
         this.groupBoxReview.Size = new System.Drawing.Size(349, 55);
         this.groupBoxReview.TabIndex = 29;
         this.groupBoxReview.TabStop = false;
         this.groupBoxReview.Text = "Review";
         // 
         // groupBoxActions
         // 
         this.groupBoxActions.Location = new System.Drawing.Point(3, 3);
         this.groupBoxActions.Name = "groupBoxActions";
         this.groupBoxActions.Size = new System.Drawing.Size(274, 55);
         this.groupBoxActions.TabIndex = 31;
         this.groupBoxActions.TabStop = false;
         this.groupBoxActions.Text = "Actions";
         // 
         // groupBoxDiff
         // 
         this.groupBoxDiff.Controls.Add(this.buttonDiffTool);
         this.groupBoxDiff.Controls.Add(this.comboBoxRightCommit);
         this.groupBoxDiff.Controls.Add(this.comboBoxLeftCommit);
         this.groupBoxDiff.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxDiff.Location = new System.Drawing.Point(0, 242);
         this.groupBoxDiff.Name = "groupBoxDiff";
         this.groupBoxDiff.Size = new System.Drawing.Size(650, 87);
         this.groupBoxDiff.TabIndex = 34;
         this.groupBoxDiff.TabStop = false;
         this.groupBoxDiff.Text = "Select commits";
         // 
         // comboBoxRightCommit
         // 
         this.comboBoxRightCommit.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
         this.comboBoxRightCommit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxRightCommit.FormattingEnabled = true;
         this.comboBoxRightCommit.Location = new System.Drawing.Point(6, 46);
         this.comboBoxRightCommit.Name = "comboBoxRightCommit";
         this.comboBoxRightCommit.Size = new System.Drawing.Size(553, 21);
         this.comboBoxRightCommit.TabIndex = 1;
         this.comboBoxRightCommit.SelectedIndexChanged += new System.EventHandler(this.ComboBoxRightCommit_SelectedIndexChanged);
         this.comboBoxRightCommit.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBoxCommits_DrawItem);
         // 
         // comboBoxLeftCommit
         // 
         this.comboBoxLeftCommit.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
         this.comboBoxLeftCommit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxLeftCommit.FormattingEnabled = true;
         this.comboBoxLeftCommit.Location = new System.Drawing.Point(6, 19);
         this.comboBoxLeftCommit.Name = "comboBoxLeftCommit";
         this.comboBoxLeftCommit.Size = new System.Drawing.Size(553, 21);
         this.comboBoxLeftCommit.TabIndex = 0;
         this.comboBoxLeftCommit.SelectedIndexChanged += new System.EventHandler(this.ComboBoxLeftCommit_SelectedIndexChanged);
         this.comboBoxLeftCommit.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.ComboBoxCommits_DrawItem);
         // 
         // groupBoxDescription
         // 
         this.groupBoxDescription.Controls.Add(this.richTextBoxMergeRequestDescription);
         this.groupBoxDescription.Controls.Add(this.linkLabelConnectedTo);
         this.groupBoxDescription.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxDescription.Location = new System.Drawing.Point(0, 0);
         this.groupBoxDescription.Name = "groupBoxDescription";
         this.groupBoxDescription.Size = new System.Drawing.Size(650, 242);
         this.groupBoxDescription.TabIndex = 29;
         this.groupBoxDescription.TabStop = false;
         this.groupBoxDescription.Text = "Merge Request";
         // 
         // richTextBoxMergeRequestDescription
         // 
         this.richTextBoxMergeRequestDescription.Location = new System.Drawing.Point(3, 16);
         this.richTextBoxMergeRequestDescription.Name = "richTextBoxMergeRequestDescription";
         this.richTextBoxMergeRequestDescription.ReadOnly = true;
         this.richTextBoxMergeRequestDescription.Size = new System.Drawing.Size(644, 193);
         this.richTextBoxMergeRequestDescription.TabIndex = 1;
         this.richTextBoxMergeRequestDescription.TabStop = false;
         this.richTextBoxMergeRequestDescription.Text = "";
         // 
         // linkLabelConnectedTo
         // 
         this.linkLabelConnectedTo.AutoSize = true;
         this.linkLabelConnectedTo.Location = new System.Drawing.Point(3, 212);
         this.linkLabelConnectedTo.Name = "linkLabelConnectedTo";
         this.linkLabelConnectedTo.Size = new System.Drawing.Size(54, 13);
         this.linkLabelConnectedTo.TabIndex = 7;
         this.linkLabelConnectedTo.TabStop = true;
         this.linkLabelConnectedTo.Text = "<url-here>";
         this.linkLabelConnectedTo.Visible = false;
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(1310, 604);
         this.Controls.Add(this.tabControl);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.Name = "MainForm";
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
         this.groupBoxHost.ResumeLayout(false);
         this.tabPageMR.ResumeLayout(false);
         this.panel1.ResumeLayout(false);
         this.panel3.ResumeLayout(false);
         this.panel3.PerformLayout();
         this.groupBoxTimeTracking.ResumeLayout(false);
         this.groupBoxTimeTracking.PerformLayout();
         this.panel2.ResumeLayout(false);
         this.groupBoxReview.ResumeLayout(false);
         this.groupBoxDiff.ResumeLayout(false);
         this.groupBoxDescription.ResumeLayout(false);
         this.groupBoxDescription.PerformLayout();
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
      private System.Windows.Forms.TabControl tabControl;
      private System.Windows.Forms.TabPage tabPageSettings;
      private System.Windows.Forms.GroupBox groupBoxGit;
      private System.Windows.Forms.TabPage tabPageMR;
      private System.Windows.Forms.CheckBox checkBoxLabels;
      private System.Windows.Forms.TextBox textBoxLabels;
      private SelectionPreservingComboBox comboBoxHost;
      private SelectionPreservingComboBox comboBoxLeftCommit;
      private SelectionPreservingComboBox comboBoxRightCommit;
      private System.Windows.Forms.GroupBox groupBoxOther;
      private System.Windows.Forms.Button buttonRemoveKnownHost;
      private System.Windows.Forms.Button buttonAddKnownHost;
      private System.Windows.Forms.ListView listViewKnownHosts;
      private System.Windows.Forms.ColumnHeader columnHeaderHost;
      private System.Windows.Forms.ColumnHeader columnHeaderAccessToken;
      private System.Windows.Forms.CheckBox checkBoxShowPublicOnly;
      private System.Windows.Forms.Label labelDepth;
      private System.Windows.Forms.ComboBox comboBoxDCDepth;
      private System.Windows.Forms.CheckBox checkBoxMinimizeOnClose;
      private System.Windows.Forms.ComboBox comboBoxColorSchemes;
      private System.Windows.Forms.Label labelColorScheme;
      private System.Windows.Forms.GroupBox groupBoxHost;
      private System.Windows.Forms.Panel panel1;
      private System.Windows.Forms.GroupBox groupBoxDescription;
      private System.Windows.Forms.RichTextBox richTextBoxMergeRequestDescription;
      private System.Windows.Forms.LinkLabel linkLabelConnectedTo;
      private System.Windows.Forms.GroupBox groupBoxDiff;
      private System.Windows.Forms.Button buttonDiffTool;
      private System.Windows.Forms.Panel panel3;
      private System.Windows.Forms.LinkLabel linkLabelAbortGit;
      private System.Windows.Forms.Label labelWorkflowStatus;
      private System.Windows.Forms.Label labelGitStatus;
      private System.Windows.Forms.GroupBox groupBoxTimeTracking;
      private System.Windows.Forms.Button buttonEditTime;
      private System.Windows.Forms.Label labelTimeTrackingMergeRequestName;
      private System.Windows.Forms.Button buttonTimeTrackingCancel;
      private System.Windows.Forms.Label labelTimeTrackingTrackedTime;
      private System.Windows.Forms.Button buttonTimeTrackingStart;
      private System.Windows.Forms.Label labelTimeTrackingTrackedLabel;
      private System.Windows.Forms.Panel panel2;
      private System.Windows.Forms.GroupBox groupBoxReview;
      private System.Windows.Forms.Button buttonNewDiscussion;
      private System.Windows.Forms.Button buttonAddComment;
      private System.Windows.Forms.Button buttonDiscussions;
      private System.Windows.Forms.GroupBox groupBoxActions;
      private System.Windows.Forms.ListView listViewMergeRequests;
      private System.Windows.Forms.ColumnHeader columnHeaderAuthor;
      private System.Windows.Forms.ColumnHeader columnHeaderProject;
      private System.Windows.Forms.ColumnHeader columnHeaderTitle;
      private System.Windows.Forms.ColumnHeader columnHeaderId;
   }
}

