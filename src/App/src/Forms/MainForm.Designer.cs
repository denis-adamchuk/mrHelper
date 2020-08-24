using mrHelper.CommonControls.Controls;
using System;
using System.Windows.Forms;

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

         _liveDataCache?.Dispose();
         _searchDataCache?.Dispose();

         disposeGitHelpers();
         disposeLocalGitRepositoryFactory();
         disposeLiveDataCacheDependencies();

         _checkForUpdatesTimer?.Stop();
         _checkForUpdatesTimer?.Dispose();

         _timeTrackingTimer?.Stop();
         _timeTrackingTimer?.Dispose();

         _clipboardCheckingTimer?.Stop();
         _clipboardCheckingTimer?.Dispose();

         // This allows to handle all pending invocations that other threads are
         // already ready to make before we dispose ourselves
         Application.DoEvents();

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
         this.revisionBrowser = new mrHelper.App.Controls.RevisionBrowser();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.buttonEditTime = new System.Windows.Forms.Button();
         this.buttonDiffTool = new System.Windows.Forms.Button();
         this.buttonAddComment = new System.Windows.Forms.Button();
         this.buttonDiscussions = new System.Windows.Forms.Button();
         this.buttonNewDiscussion = new System.Windows.Forms.Button();
         this.linkLabelHelp = new System.Windows.Forms.LinkLabel();
         this.linkLabelSendFeedback = new System.Windows.Forms.LinkLabel();
         this.linkLabelNewVersion = new System.Windows.Forms.LinkLabel();
         this.textBoxDisplayFilter = new mrHelper.CommonControls.Controls.DelayedTextBox();
         this.textBoxSearch = new System.Windows.Forms.TextBox();
         this.buttonReloadList = new System.Windows.Forms.Button();
         this.tabPageLive = new System.Windows.Forms.TabPage();
         this.groupBoxSelectMergeRequest = new System.Windows.Forms.GroupBox();
         this.buttonCreateNew = new System.Windows.Forms.Button();
         this.listViewMergeRequests = new mrHelper.App.Controls.MergeRequestListView();
         this.columnHeaderIId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderAuthor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderLabels = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderJira = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderTotalTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderResolved = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderSourceBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderTargetBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.checkBoxDisplayFilter = new System.Windows.Forms.CheckBox();
         this.tabPageSearch = new System.Windows.Forms.TabPage();
         this.groupBoxSearch = new System.Windows.Forms.GroupBox();
         this.radioButtonSearchByTargetBranch = new System.Windows.Forms.RadioButton();
         this.radioButtonSearchByTitleAndDescription = new System.Windows.Forms.RadioButton();
         this.listViewFoundMergeRequests = new mrHelper.App.Controls.MergeRequestListView();
         this.columnHeaderFoundIId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundAuthor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundLabels = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundJira = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundSourceBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundTargetBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.linkLabelAbortGitClone = new System.Windows.Forms.LinkLabel();
         this.buttonTimeTrackingCancel = new mrHelper.CommonControls.Controls.ConfirmCancelButton();
         this.buttonTimeTrackingStart = new System.Windows.Forms.Button();
         this.radioButtonLastVsNext = new System.Windows.Forms.RadioButton();
         this.radioButtonLastVsLatest = new System.Windows.Forms.RadioButton();
         this.radioButtonBaseVsLatest = new System.Windows.Forms.RadioButton();
         this.radioButtonCommits = new System.Windows.Forms.RadioButton();
         this.radioButtonVersions = new System.Windows.Forms.RadioButton();
         this.checkBoxMinimizeOnClose = new System.Windows.Forms.CheckBox();
         this.checkBoxRunWhenWindowsStarts = new System.Windows.Forms.CheckBox();
         this.radioButtonShowWarningsAlways = new System.Windows.Forms.RadioButton();
         this.radioButtonShowWarningsOnce = new System.Windows.Forms.RadioButton();
         this.radioButtonShowWarningsNever = new System.Windows.Forms.RadioButton();
         this.checkBoxDisableSplitterRestrictions = new System.Windows.Forms.CheckBox();
         this.checkBoxNewDiscussionIsTopMostForm = new System.Windows.Forms.CheckBox();
         this.comboBoxHost = new System.Windows.Forms.ComboBox();
         this.comboBoxDCDepth = new System.Windows.Forms.ComboBox();
         this.radioButtonSelectByProjects = new System.Windows.Forms.RadioButton();
         this.buttonEditUsers = new System.Windows.Forms.Button();
         this.listViewUsers = new System.Windows.Forms.ListView();
         this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.radioButtonSelectByUsernames = new System.Windows.Forms.RadioButton();
         this.buttonEditProjects = new System.Windows.Forms.Button();
         this.listViewProjects = new System.Windows.Forms.ListView();
         this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.buttonBrowseStorageFolder = new System.Windows.Forms.Button();
         this.textBoxStorageFolder = new System.Windows.Forms.TextBox();
         this.buttonRemoveKnownHost = new System.Windows.Forms.Button();
         this.buttonAddKnownHost = new System.Windows.Forms.Button();
         this.listViewKnownHosts = new System.Windows.Forms.ListView();
         this.columnHeaderHost = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderAccessToken = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.restoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
         this.storageFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
         this.tabControl = new System.Windows.Forms.TabControl();
         this.tabPageSettings = new System.Windows.Forms.TabPage();
         this.tabControlSettings = new System.Windows.Forms.TabControl();
         this.tabPageSettingsAccessTokens = new System.Windows.Forms.TabPage();
         this.groupBoxKnownHosts = new System.Windows.Forms.GroupBox();
         this.tabPageSettingsStorage = new System.Windows.Forms.TabPage();
         this.groupBoxFileStorage = new System.Windows.Forms.GroupBox();
         this.groupBoxFileStorageType = new System.Windows.Forms.GroupBox();
         this.linkLabelCommitStorageDescription = new System.Windows.Forms.LinkLabel();
         this.radioButtonUseGitShallowClone = new System.Windows.Forms.RadioButton();
         this.radioButtonDontUseGit = new System.Windows.Forms.RadioButton();
         this.radioButtonUseGitFullClone = new System.Windows.Forms.RadioButton();
         this.labelLocalStorageFolder = new System.Windows.Forms.Label();
         this.tabPageSettingsUserInterface = new System.Windows.Forms.TabPage();
         this.groupBoxOtherUI = new System.Windows.Forms.GroupBox();
         this.groupBoxDiscussionsView = new System.Windows.Forms.GroupBox();
         this.labelDepth = new System.Windows.Forms.Label();
         this.groupBoxNewDiscussionViewUI = new System.Windows.Forms.GroupBox();
         this.groupBoxGeneral = new System.Windows.Forms.GroupBox();
         this.comboBoxColorSchemes = new System.Windows.Forms.ComboBox();
         this.labelColorScheme = new System.Windows.Forms.Label();
         this.labelVisualTheme = new System.Windows.Forms.Label();
         this.comboBoxThemes = new System.Windows.Forms.ComboBox();
         this.comboBoxFonts = new System.Windows.Forms.ComboBox();
         this.labelFontSize = new System.Windows.Forms.Label();
         this.tabPageSettingsBehavior = new System.Windows.Forms.TabPage();
         this.groupBoxNewDiscussionViewBehavior = new System.Windows.Forms.GroupBox();
         this.groupBoxShowWarningsOnMismatch = new System.Windows.Forms.GroupBox();
         this.groupBoxRevisionTreeSettings = new System.Windows.Forms.GroupBox();
         this.groupBoxAutoSelection = new System.Windows.Forms.GroupBox();
         this.groupBoxRevisionType = new System.Windows.Forms.GroupBox();
         this.groupBoxGeneralBehavior = new System.Windows.Forms.GroupBox();
         this.tabPageSettingsNotifications = new System.Windows.Forms.TabPage();
         this.groupBoxNotifications = new System.Windows.Forms.GroupBox();
         this.checkBoxShowMergedMergeRequests = new System.Windows.Forms.CheckBox();
         this.checkBoxShowServiceNotifications = new System.Windows.Forms.CheckBox();
         this.checkBoxShowNewMergeRequests = new System.Windows.Forms.CheckBox();
         this.checkBoxShowMyActivity = new System.Windows.Forms.CheckBox();
         this.checkBoxShowUpdatedMergeRequests = new System.Windows.Forms.CheckBox();
         this.checkBoxShowKeywords = new System.Windows.Forms.CheckBox();
         this.checkBoxShowResolvedAll = new System.Windows.Forms.CheckBox();
         this.checkBoxShowOnMention = new System.Windows.Forms.CheckBox();
         this.tabPageWorkflow = new System.Windows.Forms.TabPage();
         this.groupBoxSelectWorkflow = new System.Windows.Forms.GroupBox();
         this.groupBoxConfigureProjectBasedWorkflow = new System.Windows.Forms.GroupBox();
         this.groupBoxConfigureUserBasedWorkflow = new System.Windows.Forms.GroupBox();
         this.groupBoxSelectHost = new System.Windows.Forms.GroupBox();
         this.tabPageMR = new System.Windows.Forms.TabPage();
         this.splitContainer1 = new System.Windows.Forms.SplitContainer();
         this.linkLabelFromClipboard = new System.Windows.Forms.LinkLabel();
         this.tabControlMode = new System.Windows.Forms.TabControl();
         this.splitContainer2 = new System.Windows.Forms.SplitContainer();
         this.groupBoxSelectedMR = new System.Windows.Forms.GroupBox();
         this.linkLabelConnectedTo = new System.Windows.Forms.LinkLabel();
         this.richTextBoxMergeRequestDescription = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.panelFreeSpace = new System.Windows.Forms.Panel();
         this.pictureBox2 = new System.Windows.Forms.PictureBox();
         this.pictureBox1 = new System.Windows.Forms.PictureBox();
         this.panelStatusBar = new System.Windows.Forms.Panel();
         this.labelStorageStatus = new System.Windows.Forms.Label();
         this.labelWorkflowStatus = new System.Windows.Forms.Label();
         this.panelBottomMenu = new System.Windows.Forms.Panel();
         this.groupBoxActions = new System.Windows.Forms.GroupBox();
         this.groupBoxTimeTracking = new System.Windows.Forms.GroupBox();
         this.labelTimeTrackingTrackedLabel = new System.Windows.Forms.Label();
         this.linkLabelTimeTrackingMergeRequest = new System.Windows.Forms.LinkLabel();
         this.groupBoxReview = new System.Windows.Forms.GroupBox();
         this.groupBoxSelectRevisions = new System.Windows.Forms.GroupBox();
         this.panel4 = new System.Windows.Forms.Panel();
         this.panel1 = new System.Windows.Forms.Panel();
         this.linkLabelWorkflowDescription = new System.Windows.Forms.LinkLabel();
         this.tabPageLive.SuspendLayout();
         this.groupBoxSelectMergeRequest.SuspendLayout();
         this.tabPageSearch.SuspendLayout();
         this.groupBoxSearch.SuspendLayout();
         this.contextMenuStrip.SuspendLayout();
         this.tabControl.SuspendLayout();
         this.tabPageSettings.SuspendLayout();
         this.tabControlSettings.SuspendLayout();
         this.tabPageSettingsAccessTokens.SuspendLayout();
         this.groupBoxKnownHosts.SuspendLayout();
         this.tabPageSettingsStorage.SuspendLayout();
         this.groupBoxFileStorage.SuspendLayout();
         this.groupBoxFileStorageType.SuspendLayout();
         this.tabPageSettingsUserInterface.SuspendLayout();
         this.groupBoxOtherUI.SuspendLayout();
         this.groupBoxDiscussionsView.SuspendLayout();
         this.groupBoxNewDiscussionViewUI.SuspendLayout();
         this.groupBoxGeneral.SuspendLayout();
         this.tabPageSettingsBehavior.SuspendLayout();
         this.groupBoxNewDiscussionViewBehavior.SuspendLayout();
         this.groupBoxShowWarningsOnMismatch.SuspendLayout();
         this.groupBoxRevisionTreeSettings.SuspendLayout();
         this.groupBoxAutoSelection.SuspendLayout();
         this.groupBoxRevisionType.SuspendLayout();
         this.groupBoxGeneralBehavior.SuspendLayout();
         this.tabPageSettingsNotifications.SuspendLayout();
         this.groupBoxNotifications.SuspendLayout();
         this.tabPageWorkflow.SuspendLayout();
         this.groupBoxSelectWorkflow.SuspendLayout();
         this.groupBoxConfigureProjectBasedWorkflow.SuspendLayout();
         this.groupBoxConfigureUserBasedWorkflow.SuspendLayout();
         this.groupBoxSelectHost.SuspendLayout();
         this.tabPageMR.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
         this.splitContainer1.Panel1.SuspendLayout();
         this.splitContainer1.Panel2.SuspendLayout();
         this.splitContainer1.SuspendLayout();
         this.tabControlMode.SuspendLayout();
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
         this.groupBoxSelectRevisions.SuspendLayout();
         this.SuspendLayout();
         // 
         // revisionBrowser
         // 
         this.revisionBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
         this.revisionBrowser.Location = new System.Drawing.Point(3, 16);
         this.revisionBrowser.Name = "revisionBrowser";
         this.revisionBrowser.Size = new System.Drawing.Size(740, 181);
         this.revisionBrowser.TabIndex = 0;
         this.revisionBrowser.SelectionChanged += new System.EventHandler(this.RevisionBrowser_SelectionChanged);
         // 
         // toolTip
         // 
         this.toolTip.AutoPopDelay = 5000;
         this.toolTip.InitialDelay = 500;
         this.toolTip.ReshowDelay = 100;
         // 
         // buttonEditTime
         // 
         this.buttonEditTime.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonEditTime.Enabled = false;
         this.buttonEditTime.Location = new System.Drawing.Point(644, 19);
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
         this.buttonDiffTool.Location = new System.Drawing.Point(644, 19);
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
         this.buttonDiscussions.Location = new System.Drawing.Point(542, 19);
         this.buttonDiscussions.Name = "buttonDiscussions";
         this.buttonDiscussions.Size = new System.Drawing.Size(96, 32);
         this.buttonDiscussions.TabIndex = 10;
         this.buttonDiscussions.Text = "Discussions";
         this.toolTip.SetToolTip(this.buttonDiscussions, "Show full list of comments and threads");
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
         this.buttonNewDiscussion.Text = "New thread";
         this.toolTip.SetToolTip(this.buttonNewDiscussion, "Create a new resolvable thread");
         this.buttonNewDiscussion.UseVisualStyleBackColor = true;
         this.buttonNewDiscussion.Click += new System.EventHandler(this.ButtonNewDiscussion_Click);
         // 
         // linkLabelHelp
         // 
         this.linkLabelHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelHelp.AutoSize = true;
         this.linkLabelHelp.Location = new System.Drawing.Point(624, 10);
         this.linkLabelHelp.Name = "linkLabelHelp";
         this.linkLabelHelp.Size = new System.Drawing.Size(29, 13);
         this.linkLabelHelp.TabIndex = 14;
         this.linkLabelHelp.TabStop = true;
         this.linkLabelHelp.Text = "Help";
         this.toolTip.SetToolTip(this.linkLabelHelp, "Open a web page with documentation");
         this.linkLabelHelp.Visible = false;
         this.linkLabelHelp.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelHelp_LinkClicked);
         // 
         // linkLabelSendFeedback
         // 
         this.linkLabelSendFeedback.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelSendFeedback.AutoSize = true;
         this.linkLabelSendFeedback.Location = new System.Drawing.Point(684, 10);
         this.linkLabelSendFeedback.Name = "linkLabelSendFeedback";
         this.linkLabelSendFeedback.Size = new System.Drawing.Size(55, 13);
         this.linkLabelSendFeedback.TabIndex = 15;
         this.linkLabelSendFeedback.TabStop = true;
         this.linkLabelSendFeedback.Text = "Feedback";
         this.toolTip.SetToolTip(this.linkLabelSendFeedback, "Report a bug or suggestion to developer. Logs are attached automatically.");
         this.linkLabelSendFeedback.Visible = false;
         this.linkLabelSendFeedback.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelSendFeedback_LinkClicked);
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
         this.linkLabelNewVersion.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelNewVersion_LinkClicked);
         // 
         // textBoxDisplayFilter
         // 
         this.textBoxDisplayFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxDisplayFilter.Enabled = false;
         this.textBoxDisplayFilter.Location = new System.Drawing.Point(60, 17);
         this.textBoxDisplayFilter.Name = "textBoxDisplayFilter";
         this.textBoxDisplayFilter.Size = new System.Drawing.Size(187, 20);
         this.textBoxDisplayFilter.TabIndex = 1;
         this.toolTip.SetToolTip(this.textBoxDisplayFilter, "To select merge requests use comma-separated list of the following:\n#{username} o" +
        "r label or MR IId or any substring from MR title/author name/label/branch");
         this.textBoxDisplayFilter.TextChanged += new System.EventHandler(this.textBoxDisplayFilter_TextChanged);
         this.textBoxDisplayFilter.Leave += new System.EventHandler(this.textBoxDisplayFilter_Leave);
         // 
         // textBoxSearch
         // 
         this.textBoxSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxSearch.Location = new System.Drawing.Point(3, 42);
         this.textBoxSearch.Name = "textBoxSearch";
         this.textBoxSearch.Size = new System.Drawing.Size(496, 20);
         this.textBoxSearch.TabIndex = 1;
         this.toolTip.SetToolTip(this.textBoxSearch, "Press Enter to search");
         this.textBoxSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxSearch_KeyDown);
         // 
         // buttonReloadList
         // 
         this.buttonReloadList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonReloadList.Enabled = false;
         this.buttonReloadList.Location = new System.Drawing.Point(253, 10);
         this.buttonReloadList.Name = "buttonReloadList";
         this.buttonReloadList.Size = new System.Drawing.Size(96, 32);
         this.buttonReloadList.TabIndex = 2;
         this.buttonReloadList.Text = "Refresh List";
         this.toolTip.SetToolTip(this.buttonReloadList, "Refresh merge request list in the background");
         this.buttonReloadList.UseVisualStyleBackColor = true;
         this.buttonReloadList.Click += new System.EventHandler(this.ButtonReloadList_Click);
         // 
         // tabPageLive
         // 
         this.tabPageLive.Controls.Add(this.groupBoxSelectMergeRequest);
         this.tabPageLive.Location = new System.Drawing.Point(4, 22);
         this.tabPageLive.Name = "tabPageLive";
         this.tabPageLive.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageLive.Size = new System.Drawing.Size(508, 832);
         this.tabPageLive.TabIndex = 0;
         this.tabPageLive.Text = "Live";
         this.toolTip.SetToolTip(this.tabPageLive, "List of open merge requests (updates automatically)");
         this.tabPageLive.UseVisualStyleBackColor = true;
         // 
         // groupBoxSelectMergeRequest
         // 
         this.groupBoxSelectMergeRequest.Controls.Add(this.buttonCreateNew);
         this.groupBoxSelectMergeRequest.Controls.Add(this.buttonReloadList);
         this.groupBoxSelectMergeRequest.Controls.Add(this.textBoxDisplayFilter);
         this.groupBoxSelectMergeRequest.Controls.Add(this.listViewMergeRequests);
         this.groupBoxSelectMergeRequest.Controls.Add(this.checkBoxDisplayFilter);
         this.groupBoxSelectMergeRequest.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSelectMergeRequest.Location = new System.Drawing.Point(3, 3);
         this.groupBoxSelectMergeRequest.Name = "groupBoxSelectMergeRequest";
         this.groupBoxSelectMergeRequest.Size = new System.Drawing.Size(502, 826);
         this.groupBoxSelectMergeRequest.TabIndex = 1;
         this.groupBoxSelectMergeRequest.TabStop = false;
         this.groupBoxSelectMergeRequest.Text = "Select Merge Request";
         // 
         // buttonCreateNew
         // 
         this.buttonCreateNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCreateNew.Location = new System.Drawing.Point(400, 10);
         this.buttonCreateNew.Name = "buttonCreateNew";
         this.buttonCreateNew.Size = new System.Drawing.Size(96, 32);
         this.buttonCreateNew.TabIndex = 4;
         this.buttonCreateNew.Text = "Create New...";
         this.toolTip.SetToolTip(this.buttonCreateNew, "Create a new merge request");
         this.buttonCreateNew.UseVisualStyleBackColor = true;
         this.buttonCreateNew.Click += new System.EventHandler(this.buttonCreateNew_Click);
         // 
         // listViewMergeRequests
         // 
         this.listViewMergeRequests.AllowColumnReorder = true;
         this.listViewMergeRequests.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listViewMergeRequests.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderIId,
            this.columnHeaderAuthor,
            this.columnHeaderTitle,
            this.columnHeaderLabels,
            this.columnHeaderSize,
            this.columnHeaderJira,
            this.columnHeaderTotalTime,
            this.columnHeaderResolved,
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
         this.listViewMergeRequests.Size = new System.Drawing.Size(496, 777);
         this.listViewMergeRequests.TabIndex = 3;
         this.listViewMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewMergeRequests.ColumnReordered += new System.Windows.Forms.ColumnReorderedEventHandler(this.listViewMergeRequests_ColumnReordered);
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
         // columnHeaderSize
         // 
         this.columnHeaderSize.Tag = "Size";
         this.columnHeaderSize.Text = "Size";
         this.columnHeaderSize.Width = 100;
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
         // columnHeaderResolved
         // 
         this.columnHeaderResolved.Tag = "Resolved";
         this.columnHeaderResolved.Text = "Resolved";
         this.columnHeaderResolved.Width = 65;
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
         // checkBoxDisplayFilter
         // 
         this.checkBoxDisplayFilter.AutoSize = true;
         this.checkBoxDisplayFilter.Enabled = false;
         this.checkBoxDisplayFilter.Location = new System.Drawing.Point(6, 19);
         this.checkBoxDisplayFilter.MinimumSize = new System.Drawing.Size(48, 0);
         this.checkBoxDisplayFilter.Name = "checkBoxDisplayFilter";
         this.checkBoxDisplayFilter.Size = new System.Drawing.Size(48, 17);
         this.checkBoxDisplayFilter.TabIndex = 0;
         this.checkBoxDisplayFilter.Text = "Filter";
         this.checkBoxDisplayFilter.UseVisualStyleBackColor = true;
         this.checkBoxDisplayFilter.CheckedChanged += new System.EventHandler(this.CheckBoxDisplayFilter_CheckedChanged);
         // 
         // tabPageSearch
         // 
         this.tabPageSearch.Controls.Add(this.groupBoxSearch);
         this.tabPageSearch.Location = new System.Drawing.Point(4, 22);
         this.tabPageSearch.Name = "tabPageSearch";
         this.tabPageSearch.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSearch.Size = new System.Drawing.Size(508, 832);
         this.tabPageSearch.TabIndex = 1;
         this.tabPageSearch.Text = "Search";
         this.toolTip.SetToolTip(this.tabPageSearch, "Use this mode to search closed merge requests");
         this.tabPageSearch.UseVisualStyleBackColor = true;
         // 
         // groupBoxSearch
         // 
         this.groupBoxSearch.Controls.Add(this.radioButtonSearchByTargetBranch);
         this.groupBoxSearch.Controls.Add(this.radioButtonSearchByTitleAndDescription);
         this.groupBoxSearch.Controls.Add(this.textBoxSearch);
         this.groupBoxSearch.Controls.Add(this.listViewFoundMergeRequests);
         this.groupBoxSearch.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSearch.Location = new System.Drawing.Point(3, 3);
         this.groupBoxSearch.Name = "groupBoxSearch";
         this.groupBoxSearch.Size = new System.Drawing.Size(502, 826);
         this.groupBoxSearch.TabIndex = 2;
         this.groupBoxSearch.TabStop = false;
         this.groupBoxSearch.Text = "Search Merge";
         // 
         // radioButtonSearchByTargetBranch
         // 
         this.radioButtonSearchByTargetBranch.AutoSize = true;
         this.radioButtonSearchByTargetBranch.Location = new System.Drawing.Point(212, 19);
         this.radioButtonSearchByTargetBranch.Name = "radioButtonSearchByTargetBranch";
         this.radioButtonSearchByTargetBranch.Size = new System.Drawing.Size(107, 17);
         this.radioButtonSearchByTargetBranch.TabIndex = 5;
         this.radioButtonSearchByTargetBranch.Text = "by Target Branch";
         this.toolTip.SetToolTip(this.radioButtonSearchByTargetBranch, "Search merge requests by target branch name");
         this.radioButtonSearchByTargetBranch.UseVisualStyleBackColor = true;
         // 
         // radioButtonSearchByTitleAndDescription
         // 
         this.radioButtonSearchByTitleAndDescription.AutoSize = true;
         this.radioButtonSearchByTitleAndDescription.Checked = true;
         this.radioButtonSearchByTitleAndDescription.Location = new System.Drawing.Point(3, 19);
         this.radioButtonSearchByTitleAndDescription.Name = "radioButtonSearchByTitleAndDescription";
         this.radioButtonSearchByTitleAndDescription.Size = new System.Drawing.Size(136, 17);
         this.radioButtonSearchByTitleAndDescription.TabIndex = 4;
         this.radioButtonSearchByTitleAndDescription.TabStop = true;
         this.radioButtonSearchByTitleAndDescription.Text = "by Title and Description";
         this.toolTip.SetToolTip(this.radioButtonSearchByTitleAndDescription, "Search merge requests by words from title and description");
         this.radioButtonSearchByTitleAndDescription.UseVisualStyleBackColor = true;
         // 
         // listViewFoundMergeRequests
         // 
         this.listViewFoundMergeRequests.AllowColumnReorder = true;
         this.listViewFoundMergeRequests.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listViewFoundMergeRequests.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderFoundIId,
            this.columnHeaderFoundState,
            this.columnHeaderFoundAuthor,
            this.columnHeaderFoundTitle,
            this.columnHeaderFoundLabels,
            this.columnHeaderFoundJira,
            this.columnHeaderFoundSourceBranch,
            this.columnHeaderFoundTargetBranch});
         this.listViewFoundMergeRequests.FullRowSelect = true;
         this.listViewFoundMergeRequests.GridLines = true;
         this.listViewFoundMergeRequests.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
         this.listViewFoundMergeRequests.HideSelection = false;
         this.listViewFoundMergeRequests.Location = new System.Drawing.Point(3, 68);
         this.listViewFoundMergeRequests.MultiSelect = false;
         this.listViewFoundMergeRequests.Name = "listViewFoundMergeRequests";
         this.listViewFoundMergeRequests.OwnerDraw = true;
         this.listViewFoundMergeRequests.Size = new System.Drawing.Size(496, 755);
         this.listViewFoundMergeRequests.TabIndex = 3;
         this.listViewFoundMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewFoundMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewFoundMergeRequests.ColumnReordered += new System.Windows.Forms.ColumnReorderedEventHandler(this.listViewMergeRequests_ColumnReordered);
         this.listViewFoundMergeRequests.ColumnWidthChanged += new System.Windows.Forms.ColumnWidthChangedEventHandler(this.listViewMergeRequests_ColumnWidthChanged);
         this.listViewFoundMergeRequests.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.ListViewMergeRequests_DrawColumnHeader);
         this.listViewFoundMergeRequests.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.ListViewMergeRequests_DrawSubItem);
         this.listViewFoundMergeRequests.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.ListViewMergeRequests_ItemSelectionChanged);
         this.listViewFoundMergeRequests.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ListViewMergeRequests_MouseDown);
         this.listViewFoundMergeRequests.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ListViewMergeRequests_MouseMove);
         // 
         // columnHeaderFoundIId
         // 
         this.columnHeaderFoundIId.Tag = "IId";
         this.columnHeaderFoundIId.Text = "IId";
         this.columnHeaderFoundIId.Width = 40;
         // 
         // columnHeaderFoundState
         // 
         this.columnHeaderFoundState.Tag = "State";
         this.columnHeaderFoundState.Text = "State";
         this.columnHeaderFoundState.Width = 80;
         // 
         // columnHeaderFoundAuthor
         // 
         this.columnHeaderFoundAuthor.Tag = "Author";
         this.columnHeaderFoundAuthor.Text = "Author";
         this.columnHeaderFoundAuthor.Width = 110;
         // 
         // columnHeaderFoundTitle
         // 
         this.columnHeaderFoundTitle.Tag = "Title";
         this.columnHeaderFoundTitle.Text = "Title";
         this.columnHeaderFoundTitle.Width = 400;
         // 
         // columnHeaderFoundLabels
         // 
         this.columnHeaderFoundLabels.Tag = "Labels";
         this.columnHeaderFoundLabels.Text = "Labels";
         this.columnHeaderFoundLabels.Width = 180;
         // 
         // columnHeaderFoundJira
         // 
         this.columnHeaderFoundJira.Tag = "Jira";
         this.columnHeaderFoundJira.Text = "Jira";
         this.columnHeaderFoundJira.Width = 80;
         // 
         // columnHeaderFoundSourceBranch
         // 
         this.columnHeaderFoundSourceBranch.Tag = "SourceBranch";
         this.columnHeaderFoundSourceBranch.Text = "Source Branch";
         this.columnHeaderFoundSourceBranch.Width = 100;
         // 
         // columnHeaderFoundTargetBranch
         // 
         this.columnHeaderFoundTargetBranch.Tag = "TargetBranch";
         this.columnHeaderFoundTargetBranch.Text = "Target Branch";
         this.columnHeaderFoundTargetBranch.Width = 100;
         // 
         // linkLabelAbortGitClone
         // 
         this.linkLabelAbortGitClone.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelAbortGitClone.Location = new System.Drawing.Point(707, 31);
         this.linkLabelAbortGitClone.Name = "linkLabelAbortGitClone";
         this.linkLabelAbortGitClone.Size = new System.Drawing.Size(32, 15);
         this.linkLabelAbortGitClone.TabIndex = 15;
         this.linkLabelAbortGitClone.TabStop = true;
         this.linkLabelAbortGitClone.Text = "Abort";
         this.toolTip.SetToolTip(this.linkLabelAbortGitClone, "Abort git clone operation");
         this.linkLabelAbortGitClone.Visible = false;
         this.linkLabelAbortGitClone.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelAbortGitClone_LinkClicked);
         // 
         // buttonTimeTrackingCancel
         // 
         this.buttonTimeTrackingCancel.ConfirmationText = "All changes will be lost, are you sure?";
         this.buttonTimeTrackingCancel.Enabled = false;
         this.buttonTimeTrackingCancel.Location = new System.Drawing.Point(108, 19);
         this.buttonTimeTrackingCancel.MinimumSize = new System.Drawing.Size(96, 0);
         this.buttonTimeTrackingCancel.Name = "buttonTimeTrackingCancel";
         this.buttonTimeTrackingCancel.Size = new System.Drawing.Size(96, 32);
         this.buttonTimeTrackingCancel.TabIndex = 12;
         this.buttonTimeTrackingCancel.Text = "Cancel";
         this.toolTip.SetToolTip(this.buttonTimeTrackingCancel, "Discard tracked time");
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
         this.toolTip.SetToolTip(this.buttonTimeTrackingStart, "Start/stop tracking time for the selected merge request");
         this.buttonTimeTrackingStart.UseVisualStyleBackColor = true;
         this.buttonTimeTrackingStart.Click += new System.EventHandler(this.ButtonTimeTrackingStart_Click);
         // 
         // radioButtonLastVsNext
         // 
         this.radioButtonLastVsNext.AutoSize = true;
         this.radioButtonLastVsNext.Location = new System.Drawing.Point(6, 19);
         this.radioButtonLastVsNext.Name = "radioButtonLastVsNext";
         this.radioButtonLastVsNext.Size = new System.Drawing.Size(135, 17);
         this.radioButtonLastVsNext.TabIndex = 0;
         this.radioButtonLastVsNext.TabStop = true;
         this.radioButtonLastVsNext.Text = "Last Reviewed vs Next";
         this.toolTip.SetToolTip(this.radioButtonLastVsNext, "Select the most recent reviewed revision for comparison with the next one");
         this.radioButtonLastVsNext.UseVisualStyleBackColor = true;
         this.radioButtonLastVsNext.CheckedChanged += new System.EventHandler(this.radioButtonAutoSelectionMode_CheckedChanged);
         // 
         // radioButtonLastVsLatest
         // 
         this.radioButtonLastVsLatest.AutoSize = true;
         this.radioButtonLastVsLatest.Checked = true;
         this.radioButtonLastVsLatest.Location = new System.Drawing.Point(6, 42);
         this.radioButtonLastVsLatest.Name = "radioButtonLastVsLatest";
         this.radioButtonLastVsLatest.Size = new System.Drawing.Size(142, 17);
         this.radioButtonLastVsLatest.TabIndex = 1;
         this.radioButtonLastVsLatest.TabStop = true;
         this.radioButtonLastVsLatest.Text = "Last Reviewed vs Latest";
         this.toolTip.SetToolTip(this.radioButtonLastVsLatest, "Select the most recent reviewed revision for comparison with the latest one");
         this.radioButtonLastVsLatest.UseVisualStyleBackColor = true;
         this.radioButtonLastVsLatest.CheckedChanged += new System.EventHandler(this.radioButtonAutoSelectionMode_CheckedChanged);
         // 
         // radioButtonBaseVsLatest
         // 
         this.radioButtonBaseVsLatest.AutoSize = true;
         this.radioButtonBaseVsLatest.Location = new System.Drawing.Point(6, 65);
         this.radioButtonBaseVsLatest.Name = "radioButtonBaseVsLatest";
         this.radioButtonBaseVsLatest.Size = new System.Drawing.Size(95, 17);
         this.radioButtonBaseVsLatest.TabIndex = 2;
         this.radioButtonBaseVsLatest.Text = "Base vs Latest";
         this.toolTip.SetToolTip(this.radioButtonBaseVsLatest, "Select the latest revision for comparison with the base revision");
         this.radioButtonBaseVsLatest.UseVisualStyleBackColor = true;
         this.radioButtonBaseVsLatest.CheckedChanged += new System.EventHandler(this.radioButtonAutoSelectionMode_CheckedChanged);
         // 
         // radioButtonCommits
         // 
         this.radioButtonCommits.AutoSize = true;
         this.radioButtonCommits.Location = new System.Drawing.Point(6, 19);
         this.radioButtonCommits.Name = "radioButtonCommits";
         this.radioButtonCommits.Size = new System.Drawing.Size(64, 17);
         this.radioButtonCommits.TabIndex = 0;
         this.radioButtonCommits.TabStop = true;
         this.radioButtonCommits.Text = "Commits";
         this.toolTip.SetToolTip(this.radioButtonCommits, "Expand a list of commits in the revision tree when merge request is selected");
         this.radioButtonCommits.UseVisualStyleBackColor = true;
         this.radioButtonCommits.CheckedChanged += new System.EventHandler(this.radioButtonRevisionType_CheckedChanged);
         // 
         // radioButtonVersions
         // 
         this.radioButtonVersions.AutoSize = true;
         this.radioButtonVersions.Checked = true;
         this.radioButtonVersions.Location = new System.Drawing.Point(5, 65);
         this.radioButtonVersions.Name = "radioButtonVersions";
         this.radioButtonVersions.Size = new System.Drawing.Size(65, 17);
         this.radioButtonVersions.TabIndex = 1;
         this.radioButtonVersions.TabStop = true;
         this.radioButtonVersions.Text = "Versions";
         this.toolTip.SetToolTip(this.radioButtonVersions, "Expand a list of versions in the revision tree when merge request is selected (si" +
        "milar to Changes tab of GitLab Web UI)");
         this.radioButtonVersions.UseVisualStyleBackColor = true;
         this.radioButtonVersions.CheckedChanged += new System.EventHandler(this.radioButtonRevisionType_CheckedChanged);
         // 
         // checkBoxMinimizeOnClose
         // 
         this.checkBoxMinimizeOnClose.AutoSize = true;
         this.checkBoxMinimizeOnClose.Location = new System.Drawing.Point(6, 18);
         this.checkBoxMinimizeOnClose.Name = "checkBoxMinimizeOnClose";
         this.checkBoxMinimizeOnClose.Size = new System.Drawing.Size(109, 17);
         this.checkBoxMinimizeOnClose.TabIndex = 21;
         this.checkBoxMinimizeOnClose.Text = "Minimize on close";
         this.toolTip.SetToolTip(this.checkBoxMinimizeOnClose, "Don\'t exit on closing the main window but minimize the application to the tray");
         this.checkBoxMinimizeOnClose.UseVisualStyleBackColor = true;
         this.checkBoxMinimizeOnClose.CheckedChanged += new System.EventHandler(this.CheckBoxMinimizeOnClose_CheckedChanged);
         // 
         // checkBoxRunWhenWindowsStarts
         // 
         this.checkBoxRunWhenWindowsStarts.AutoSize = true;
         this.checkBoxRunWhenWindowsStarts.Location = new System.Drawing.Point(6, 41);
         this.checkBoxRunWhenWindowsStarts.Name = "checkBoxRunWhenWindowsStarts";
         this.checkBoxRunWhenWindowsStarts.Size = new System.Drawing.Size(195, 17);
         this.checkBoxRunWhenWindowsStarts.TabIndex = 22;
         this.checkBoxRunWhenWindowsStarts.Text = "Run mrHelper when Windows starts";
         this.toolTip.SetToolTip(this.checkBoxRunWhenWindowsStarts, "Add mrHelper to the list of Startup Apps");
         this.checkBoxRunWhenWindowsStarts.UseVisualStyleBackColor = true;
         this.checkBoxRunWhenWindowsStarts.CheckedChanged += new System.EventHandler(this.checkBoxRunWhenWindowsStarts_CheckedChanged);
         // 
         // radioButtonShowWarningsAlways
         // 
         this.radioButtonShowWarningsAlways.AutoSize = true;
         this.radioButtonShowWarningsAlways.Location = new System.Drawing.Point(6, 19);
         this.radioButtonShowWarningsAlways.Name = "radioButtonShowWarningsAlways";
         this.radioButtonShowWarningsAlways.Size = new System.Drawing.Size(58, 17);
         this.radioButtonShowWarningsAlways.TabIndex = 1;
         this.radioButtonShowWarningsAlways.TabStop = true;
         this.radioButtonShowWarningsAlways.Text = "Always";
         this.toolTip.SetToolTip(this.radioButtonShowWarningsAlways, "Notify about mismatch between left and right files on each occurrence");
         this.radioButtonShowWarningsAlways.UseVisualStyleBackColor = true;
         this.radioButtonShowWarningsAlways.CheckedChanged += new System.EventHandler(this.radioButtonShowWarningsOnFileMismatchMode_CheckedChanged);
         // 
         // radioButtonShowWarningsOnce
         // 
         this.radioButtonShowWarningsOnce.AutoSize = true;
         this.radioButtonShowWarningsOnce.Checked = true;
         this.radioButtonShowWarningsOnce.Location = new System.Drawing.Point(6, 42);
         this.radioButtonShowWarningsOnce.Name = "radioButtonShowWarningsOnce";
         this.radioButtonShowWarningsOnce.Size = new System.Drawing.Size(121, 17);
         this.radioButtonShowWarningsOnce.TabIndex = 2;
         this.radioButtonShowWarningsOnce.TabStop = true;
         this.radioButtonShowWarningsOnce.Text = "Until ignored by user";
         this.toolTip.SetToolTip(this.radioButtonShowWarningsOnce, "Notify about mismatch between left and right files until user decides to ignore w" +
        "arnings for a specific file");
         this.radioButtonShowWarningsOnce.UseVisualStyleBackColor = true;
         this.radioButtonShowWarningsOnce.CheckedChanged += new System.EventHandler(this.radioButtonShowWarningsOnFileMismatchMode_CheckedChanged);
         // 
         // radioButtonShowWarningsNever
         // 
         this.radioButtonShowWarningsNever.AutoSize = true;
         this.radioButtonShowWarningsNever.Location = new System.Drawing.Point(6, 65);
         this.radioButtonShowWarningsNever.Name = "radioButtonShowWarningsNever";
         this.radioButtonShowWarningsNever.Size = new System.Drawing.Size(54, 17);
         this.radioButtonShowWarningsNever.TabIndex = 8;
         this.radioButtonShowWarningsNever.TabStop = true;
         this.radioButtonShowWarningsNever.Text = "Never";
         this.toolTip.SetToolTip(this.radioButtonShowWarningsNever, "Never notify about mismatch between left and right files");
         this.radioButtonShowWarningsNever.UseVisualStyleBackColor = true;
         this.radioButtonShowWarningsNever.CheckedChanged += new System.EventHandler(this.radioButtonShowWarningsOnFileMismatchMode_CheckedChanged);
         // 
         // checkBoxDisableSplitterRestrictions
         // 
         this.checkBoxDisableSplitterRestrictions.AutoSize = true;
         this.checkBoxDisableSplitterRestrictions.Location = new System.Drawing.Point(9, 19);
         this.checkBoxDisableSplitterRestrictions.Name = "checkBoxDisableSplitterRestrictions";
         this.checkBoxDisableSplitterRestrictions.Size = new System.Drawing.Size(147, 17);
         this.checkBoxDisableSplitterRestrictions.TabIndex = 30;
         this.checkBoxDisableSplitterRestrictions.Text = "Disable splitter restrictions";
         this.toolTip.SetToolTip(this.checkBoxDisableSplitterRestrictions, "Allow any position for horizontal and vertical splitters at the main window");
         this.checkBoxDisableSplitterRestrictions.UseVisualStyleBackColor = true;
         this.checkBoxDisableSplitterRestrictions.CheckedChanged += new System.EventHandler(this.CheckBoxDisableSplitterRestrictions_CheckedChanged);
         // 
         // checkBoxNewDiscussionIsTopMostForm
         // 
         this.checkBoxNewDiscussionIsTopMostForm.AutoSize = true;
         this.checkBoxNewDiscussionIsTopMostForm.Location = new System.Drawing.Point(9, 19);
         this.checkBoxNewDiscussionIsTopMostForm.Name = "checkBoxNewDiscussionIsTopMostForm";
         this.checkBoxNewDiscussionIsTopMostForm.Size = new System.Drawing.Size(280, 17);
         this.checkBoxNewDiscussionIsTopMostForm.TabIndex = 19;
         this.checkBoxNewDiscussionIsTopMostForm.Text = "Show New Discussion dialog on top of all applications";
         this.toolTip.SetToolTip(this.checkBoxNewDiscussionIsTopMostForm, "Forces Create New Discussion dialog to be a topmost one");
         this.checkBoxNewDiscussionIsTopMostForm.UseVisualStyleBackColor = true;
         this.checkBoxNewDiscussionIsTopMostForm.CheckedChanged += new System.EventHandler(this.CheckBoxNewDiscussionIsTopMostForm_CheckedChanged);
         // 
         // comboBoxHost
         // 
         this.comboBoxHost.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxHost.FormattingEnabled = true;
         this.comboBoxHost.Location = new System.Drawing.Point(6, 19);
         this.comboBoxHost.Name = "comboBoxHost";
         this.comboBoxHost.Size = new System.Drawing.Size(259, 21);
         this.comboBoxHost.TabIndex = 12;
         this.toolTip.SetToolTip(this.comboBoxHost, "Select a GitLab host");
         this.comboBoxHost.SelectionChangeCommitted += new System.EventHandler(this.ComboBoxHost_SelectionChangeCommited);
         this.comboBoxHost.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.ComboBoxHost_Format);
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
         this.comboBoxDCDepth.Location = new System.Drawing.Point(106, 19);
         this.comboBoxDCDepth.Name = "comboBoxDCDepth";
         this.comboBoxDCDepth.Size = new System.Drawing.Size(58, 21);
         this.comboBoxDCDepth.TabIndex = 27;
         this.toolTip.SetToolTip(this.comboBoxDCDepth, "Number of lines under the line the discussion was created for.");
         this.comboBoxDCDepth.SelectedIndexChanged += new System.EventHandler(this.comboBoxDCDepth_SelectedIndexChanged);
         // 
         // radioButtonSelectByProjects
         // 
         this.radioButtonSelectByProjects.AutoSize = true;
         this.radioButtonSelectByProjects.Location = new System.Drawing.Point(182, 19);
         this.radioButtonSelectByProjects.Name = "radioButtonSelectByProjects";
         this.radioButtonSelectByProjects.Size = new System.Drawing.Size(135, 17);
         this.radioButtonSelectByProjects.TabIndex = 22;
         this.radioButtonSelectByProjects.TabStop = true;
         this.radioButtonSelectByProjects.Text = "Project-based workflow";
         this.toolTip.SetToolTip(this.radioButtonSelectByProjects, "All merge requests from selected projects will be loaded from GitLab");
         this.radioButtonSelectByProjects.UseVisualStyleBackColor = true;
         this.radioButtonSelectByProjects.CheckedChanged += new System.EventHandler(this.radioButtonWorkflowType_CheckedChanged);
         // 
         // buttonEditUsers
         // 
         this.buttonEditUsers.Location = new System.Drawing.Point(182, 182);
         this.buttonEditUsers.Name = "buttonEditUsers";
         this.buttonEditUsers.Size = new System.Drawing.Size(83, 27);
         this.buttonEditUsers.TabIndex = 21;
         this.buttonEditUsers.Text = "Edit...";
         this.toolTip.SetToolTip(this.buttonEditUsers, "Edit list of usernames");
         this.buttonEditUsers.UseVisualStyleBackColor = true;
         this.buttonEditUsers.Click += new System.EventHandler(this.buttonEditUsers_Click);
         // 
         // listViewUsers
         // 
         this.listViewUsers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
         this.listViewUsers.FullRowSelect = true;
         this.listViewUsers.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
         this.listViewUsers.HideSelection = false;
         this.listViewUsers.Location = new System.Drawing.Point(6, 19);
         this.listViewUsers.MultiSelect = false;
         this.listViewUsers.Name = "listViewUsers";
         this.listViewUsers.ShowGroups = false;
         this.listViewUsers.Size = new System.Drawing.Size(259, 157);
         this.listViewUsers.TabIndex = 20;
         this.toolTip.SetToolTip(this.listViewUsers, "Selected usernames");
         this.listViewUsers.UseCompatibleStateImageBehavior = false;
         this.listViewUsers.View = System.Windows.Forms.View.Details;
         // 
         // columnHeader1
         // 
         this.columnHeader1.Text = "Name";
         this.columnHeader1.Width = 160;
         // 
         // radioButtonSelectByUsernames
         // 
         this.radioButtonSelectByUsernames.AutoSize = true;
         this.radioButtonSelectByUsernames.Location = new System.Drawing.Point(6, 19);
         this.radioButtonSelectByUsernames.Name = "radioButtonSelectByUsernames";
         this.radioButtonSelectByUsernames.Size = new System.Drawing.Size(124, 17);
         this.radioButtonSelectByUsernames.TabIndex = 19;
         this.radioButtonSelectByUsernames.TabStop = true;
         this.radioButtonSelectByUsernames.Text = "User-based workflow";
         this.toolTip.SetToolTip(this.radioButtonSelectByUsernames, "Select user names to track only their merge requests");
         this.radioButtonSelectByUsernames.UseVisualStyleBackColor = true;
         this.radioButtonSelectByUsernames.CheckedChanged += new System.EventHandler(this.radioButtonWorkflowType_CheckedChanged);
         // 
         // buttonEditProjects
         // 
         this.buttonEditProjects.Location = new System.Drawing.Point(182, 182);
         this.buttonEditProjects.Name = "buttonEditProjects";
         this.buttonEditProjects.Size = new System.Drawing.Size(83, 27);
         this.buttonEditProjects.TabIndex = 18;
         this.buttonEditProjects.Text = "Edit...";
         this.toolTip.SetToolTip(this.buttonEditProjects, "Edit list of projects");
         this.buttonEditProjects.UseVisualStyleBackColor = true;
         this.buttonEditProjects.Click += new System.EventHandler(this.buttonEditProjects_Click);
         // 
         // listViewProjects
         // 
         this.listViewProjects.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName});
         this.listViewProjects.FullRowSelect = true;
         this.listViewProjects.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
         this.listViewProjects.HideSelection = false;
         this.listViewProjects.Location = new System.Drawing.Point(6, 19);
         this.listViewProjects.MultiSelect = false;
         this.listViewProjects.Name = "listViewProjects";
         this.listViewProjects.ShowGroups = false;
         this.listViewProjects.Size = new System.Drawing.Size(259, 157);
         this.listViewProjects.TabIndex = 17;
         this.toolTip.SetToolTip(this.listViewProjects, "Selected projects");
         this.listViewProjects.UseCompatibleStateImageBehavior = false;
         this.listViewProjects.View = System.Windows.Forms.View.Details;
         // 
         // columnHeaderName
         // 
         this.columnHeaderName.Text = "Name";
         this.columnHeaderName.Width = 160;
         // 
         // buttonBrowseStorageFolder
         // 
         this.buttonBrowseStorageFolder.Location = new System.Drawing.Point(424, 28);
         this.buttonBrowseStorageFolder.Name = "buttonBrowseStorageFolder";
         this.buttonBrowseStorageFolder.Size = new System.Drawing.Size(83, 27);
         this.buttonBrowseStorageFolder.TabIndex = 21;
         this.buttonBrowseStorageFolder.Text = "Browse...";
         this.toolTip.SetToolTip(this.buttonBrowseStorageFolder, "Select a folder where repositories will be stored");
         this.buttonBrowseStorageFolder.UseVisualStyleBackColor = true;
         this.buttonBrowseStorageFolder.Click += new System.EventHandler(this.ButtonBrowseStorageFolder_Click);
         // 
         // textBoxStorageFolder
         // 
         this.textBoxStorageFolder.Location = new System.Drawing.Point(6, 32);
         this.textBoxStorageFolder.Name = "textBoxStorageFolder";
         this.textBoxStorageFolder.ReadOnly = true;
         this.textBoxStorageFolder.Size = new System.Drawing.Size(412, 20);
         this.textBoxStorageFolder.TabIndex = 20;
         this.textBoxStorageFolder.TabStop = false;
         this.toolTip.SetToolTip(this.textBoxStorageFolder, "A folder where repositories are stored");
         // 
         // buttonRemoveKnownHost
         // 
         this.buttonRemoveKnownHost.Enabled = false;
         this.buttonRemoveKnownHost.Location = new System.Drawing.Point(421, 102);
         this.buttonRemoveKnownHost.Name = "buttonRemoveKnownHost";
         this.buttonRemoveKnownHost.Size = new System.Drawing.Size(83, 27);
         this.buttonRemoveKnownHost.TabIndex = 9;
         this.buttonRemoveKnownHost.Text = "Remove";
         this.toolTip.SetToolTip(this.buttonRemoveKnownHost, "Remove a selected host from the list of known hosts");
         this.buttonRemoveKnownHost.UseVisualStyleBackColor = true;
         this.buttonRemoveKnownHost.Click += new System.EventHandler(this.ButtonRemoveKnownHost_Click);
         // 
         // buttonAddKnownHost
         // 
         this.buttonAddKnownHost.Location = new System.Drawing.Point(421, 19);
         this.buttonAddKnownHost.Name = "buttonAddKnownHost";
         this.buttonAddKnownHost.Size = new System.Drawing.Size(83, 27);
         this.buttonAddKnownHost.TabIndex = 8;
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
         this.listViewKnownHosts.Location = new System.Drawing.Point(6, 19);
         this.listViewKnownHosts.MultiSelect = false;
         this.listViewKnownHosts.Name = "listViewKnownHosts";
         this.listViewKnownHosts.Size = new System.Drawing.Size(409, 110);
         this.listViewKnownHosts.TabIndex = 7;
         this.toolTip.SetToolTip(this.listViewKnownHosts, "Available GitLab hosts");
         this.listViewKnownHosts.UseCompatibleStateImageBehavior = false;
         this.listViewKnownHosts.View = System.Windows.Forms.View.Details;
         this.listViewKnownHosts.SelectedIndexChanged += new System.EventHandler(this.listViewKnownHosts_SelectedIndexChanged);
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
         // storageFolderBrowser
         // 
         this.storageFolderBrowser.Description = "Select a folder where temp files will be stored";
         this.storageFolderBrowser.RootFolder = System.Environment.SpecialFolder.MyComputer;
         // 
         // tabControl
         // 
         this.tabControl.Controls.Add(this.tabPageSettings);
         this.tabControl.Controls.Add(this.tabPageWorkflow);
         this.tabControl.Controls.Add(this.tabPageMR);
         this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tabControl.Location = new System.Drawing.Point(0, 0);
         this.tabControl.Name = "tabControl";
         this.tabControl.SelectedIndex = 0;
         this.tabControl.Size = new System.Drawing.Size(1284, 890);
         this.tabControl.TabIndex = 0;
         this.tabControl.SelectedIndexChanged += new System.EventHandler(this.tabControl_SelectedIndexChanged);
         this.tabControl.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl_Selecting);
         // 
         // tabPageSettings
         // 
         this.tabPageSettings.AutoScroll = true;
         this.tabPageSettings.Controls.Add(this.tabControlSettings);
         this.tabPageSettings.Location = new System.Drawing.Point(4, 22);
         this.tabPageSettings.Name = "tabPageSettings";
         this.tabPageSettings.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSettings.Size = new System.Drawing.Size(1276, 864);
         this.tabPageSettings.TabIndex = 0;
         this.tabPageSettings.Text = "Settings";
         this.tabPageSettings.UseVisualStyleBackColor = true;
         // 
         // tabControlSettings
         // 
         this.tabControlSettings.Controls.Add(this.tabPageSettingsAccessTokens);
         this.tabControlSettings.Controls.Add(this.tabPageSettingsStorage);
         this.tabControlSettings.Controls.Add(this.tabPageSettingsUserInterface);
         this.tabControlSettings.Controls.Add(this.tabPageSettingsBehavior);
         this.tabControlSettings.Controls.Add(this.tabPageSettingsNotifications);
         this.tabControlSettings.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tabControlSettings.Location = new System.Drawing.Point(3, 3);
         this.tabControlSettings.Name = "tabControlSettings";
         this.tabControlSettings.SelectedIndex = 0;
         this.tabControlSettings.Size = new System.Drawing.Size(1270, 858);
         this.tabControlSettings.TabIndex = 11;
         // 
         // tabPageSettingsAccessTokens
         // 
         this.tabPageSettingsAccessTokens.Controls.Add(this.groupBoxKnownHosts);
         this.tabPageSettingsAccessTokens.Location = new System.Drawing.Point(4, 22);
         this.tabPageSettingsAccessTokens.Name = "tabPageSettingsAccessTokens";
         this.tabPageSettingsAccessTokens.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSettingsAccessTokens.Size = new System.Drawing.Size(1262, 832);
         this.tabPageSettingsAccessTokens.TabIndex = 5;
         this.tabPageSettingsAccessTokens.Text = "Access Tokens";
         this.tabPageSettingsAccessTokens.UseVisualStyleBackColor = true;
         // 
         // groupBoxKnownHosts
         // 
         this.groupBoxKnownHosts.Controls.Add(this.buttonRemoveKnownHost);
         this.groupBoxKnownHosts.Controls.Add(this.buttonAddKnownHost);
         this.groupBoxKnownHosts.Controls.Add(this.listViewKnownHosts);
         this.groupBoxKnownHosts.Location = new System.Drawing.Point(6, 6);
         this.groupBoxKnownHosts.Name = "groupBoxKnownHosts";
         this.groupBoxKnownHosts.Size = new System.Drawing.Size(577, 145);
         this.groupBoxKnownHosts.TabIndex = 24;
         this.groupBoxKnownHosts.TabStop = false;
         this.groupBoxKnownHosts.Text = "Known Hosts";
         // 
         // tabPageSettingsStorage
         // 
         this.tabPageSettingsStorage.Controls.Add(this.groupBoxFileStorage);
         this.tabPageSettingsStorage.Location = new System.Drawing.Point(4, 22);
         this.tabPageSettingsStorage.Name = "tabPageSettingsStorage";
         this.tabPageSettingsStorage.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSettingsStorage.Size = new System.Drawing.Size(1262, 832);
         this.tabPageSettingsStorage.TabIndex = 6;
         this.tabPageSettingsStorage.Text = "Storage";
         this.tabPageSettingsStorage.UseVisualStyleBackColor = true;
         // 
         // groupBoxFileStorage
         // 
         this.groupBoxFileStorage.Controls.Add(this.groupBoxFileStorageType);
         this.groupBoxFileStorage.Controls.Add(this.buttonBrowseStorageFolder);
         this.groupBoxFileStorage.Controls.Add(this.labelLocalStorageFolder);
         this.groupBoxFileStorage.Controls.Add(this.textBoxStorageFolder);
         this.groupBoxFileStorage.Location = new System.Drawing.Point(6, 6);
         this.groupBoxFileStorage.Name = "groupBoxFileStorage";
         this.groupBoxFileStorage.Size = new System.Drawing.Size(577, 146);
         this.groupBoxFileStorage.TabIndex = 20;
         this.groupBoxFileStorage.TabStop = false;
         this.groupBoxFileStorage.Text = "File Storage";
         // 
         // groupBoxFileStorageType
         // 
         this.groupBoxFileStorageType.Controls.Add(this.linkLabelCommitStorageDescription);
         this.groupBoxFileStorageType.Controls.Add(this.radioButtonUseGitShallowClone);
         this.groupBoxFileStorageType.Controls.Add(this.radioButtonDontUseGit);
         this.groupBoxFileStorageType.Controls.Add(this.radioButtonUseGitFullClone);
         this.groupBoxFileStorageType.Location = new System.Drawing.Point(6, 58);
         this.groupBoxFileStorageType.Name = "groupBoxFileStorageType";
         this.groupBoxFileStorageType.Size = new System.Drawing.Size(501, 78);
         this.groupBoxFileStorageType.TabIndex = 27;
         this.groupBoxFileStorageType.TabStop = false;
         this.groupBoxFileStorageType.Text = "File Storage Type";
         // 
         // linkLabelCommitStorageDescription
         // 
         this.linkLabelCommitStorageDescription.AutoSize = true;
         this.linkLabelCommitStorageDescription.Location = new System.Drawing.Point(306, 46);
         this.linkLabelCommitStorageDescription.Name = "linkLabelCommitStorageDescription";
         this.linkLabelCommitStorageDescription.Size = new System.Drawing.Size(128, 13);
         this.linkLabelCommitStorageDescription.TabIndex = 30;
         this.linkLabelCommitStorageDescription.TabStop = true;
         this.linkLabelCommitStorageDescription.Text = "Show detailed description";
         this.linkLabelCommitStorageDescription.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelCommitStorageDescription_LinkClicked);
         // 
         // radioButtonUseGitShallowClone
         // 
         this.radioButtonUseGitShallowClone.AutoSize = true;
         this.radioButtonUseGitShallowClone.Location = new System.Drawing.Point(6, 44);
         this.radioButtonUseGitShallowClone.Name = "radioButtonUseGitShallowClone";
         this.radioButtonUseGitShallowClone.Size = new System.Drawing.Size(165, 17);
         this.radioButtonUseGitShallowClone.TabIndex = 29;
         this.radioButtonUseGitShallowClone.TabStop = true;
         this.radioButtonUseGitShallowClone.Text = "Use git in shallow clone mode";
         this.radioButtonUseGitShallowClone.UseVisualStyleBackColor = true;
         this.radioButtonUseGitShallowClone.CheckedChanged += new System.EventHandler(this.radioButtonUseGit_CheckedChanged);
         // 
         // radioButtonDontUseGit
         // 
         this.radioButtonDontUseGit.AutoSize = true;
         this.radioButtonDontUseGit.Location = new System.Drawing.Point(309, 19);
         this.radioButtonDontUseGit.Name = "radioButtonDontUseGit";
         this.radioButtonDontUseGit.Size = new System.Drawing.Size(152, 17);
         this.radioButtonDontUseGit.TabIndex = 28;
         this.radioButtonDontUseGit.TabStop = true;
         this.radioButtonDontUseGit.Text = "Don\'t use git as file storage";
         this.radioButtonDontUseGit.UseVisualStyleBackColor = true;
         this.radioButtonDontUseGit.CheckedChanged += new System.EventHandler(this.radioButtonUseGit_CheckedChanged);
         // 
         // radioButtonUseGitFullClone
         // 
         this.radioButtonUseGitFullClone.AutoSize = true;
         this.radioButtonUseGitFullClone.Location = new System.Drawing.Point(6, 19);
         this.radioButtonUseGitFullClone.Name = "radioButtonUseGitFullClone";
         this.radioButtonUseGitFullClone.Size = new System.Drawing.Size(180, 17);
         this.radioButtonUseGitFullClone.TabIndex = 27;
         this.radioButtonUseGitFullClone.TabStop = true;
         this.radioButtonUseGitFullClone.Text = "Use git and clone full repositories";
         this.radioButtonUseGitFullClone.UseVisualStyleBackColor = true;
         this.radioButtonUseGitFullClone.CheckedChanged += new System.EventHandler(this.radioButtonUseGit_CheckedChanged);
         // 
         // labelLocalStorageFolder
         // 
         this.labelLocalStorageFolder.AutoSize = true;
         this.labelLocalStorageFolder.Location = new System.Drawing.Point(6, 16);
         this.labelLocalStorageFolder.Name = "labelLocalStorageFolder";
         this.labelLocalStorageFolder.Size = new System.Drawing.Size(121, 13);
         this.labelLocalStorageFolder.TabIndex = 22;
         this.labelLocalStorageFolder.Text = "Folder for temporary files";
         // 
         // tabPageSettingsUserInterface
         // 
         this.tabPageSettingsUserInterface.Controls.Add(this.groupBoxOtherUI);
         this.tabPageSettingsUserInterface.Controls.Add(this.groupBoxDiscussionsView);
         this.tabPageSettingsUserInterface.Controls.Add(this.groupBoxNewDiscussionViewUI);
         this.tabPageSettingsUserInterface.Controls.Add(this.groupBoxGeneral);
         this.tabPageSettingsUserInterface.Location = new System.Drawing.Point(4, 22);
         this.tabPageSettingsUserInterface.Name = "tabPageSettingsUserInterface";
         this.tabPageSettingsUserInterface.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSettingsUserInterface.Size = new System.Drawing.Size(1262, 832);
         this.tabPageSettingsUserInterface.TabIndex = 0;
         this.tabPageSettingsUserInterface.Text = "User Interface";
         this.tabPageSettingsUserInterface.UseVisualStyleBackColor = true;
         // 
         // groupBoxOtherUI
         // 
         this.groupBoxOtherUI.Controls.Add(this.checkBoxDisableSplitterRestrictions);
         this.groupBoxOtherUI.Location = new System.Drawing.Point(6, 230);
         this.groupBoxOtherUI.Name = "groupBoxOtherUI";
         this.groupBoxOtherUI.Size = new System.Drawing.Size(577, 46);
         this.groupBoxOtherUI.TabIndex = 29;
         this.groupBoxOtherUI.TabStop = false;
         this.groupBoxOtherUI.Text = "Other";
         // 
         // groupBoxDiscussionsView
         // 
         this.groupBoxDiscussionsView.Controls.Add(this.labelDepth);
         this.groupBoxDiscussionsView.Controls.Add(this.comboBoxDCDepth);
         this.groupBoxDiscussionsView.Location = new System.Drawing.Point(6, 171);
         this.groupBoxDiscussionsView.Name = "groupBoxDiscussionsView";
         this.groupBoxDiscussionsView.Size = new System.Drawing.Size(577, 53);
         this.groupBoxDiscussionsView.TabIndex = 28;
         this.groupBoxDiscussionsView.TabStop = false;
         this.groupBoxDiscussionsView.Text = "Discussions View";
         // 
         // labelDepth
         // 
         this.labelDepth.AutoSize = true;
         this.labelDepth.Location = new System.Drawing.Point(6, 22);
         this.labelDepth.Name = "labelDepth";
         this.labelDepth.Size = new System.Drawing.Size(94, 13);
         this.labelDepth.TabIndex = 26;
         this.labelDepth.Text = "Diff Context Depth";
         // 
         // groupBoxNewDiscussionViewUI
         // 
         this.groupBoxNewDiscussionViewUI.Controls.Add(this.checkBoxNewDiscussionIsTopMostForm);
         this.groupBoxNewDiscussionViewUI.Location = new System.Drawing.Point(6, 112);
         this.groupBoxNewDiscussionViewUI.Name = "groupBoxNewDiscussionViewUI";
         this.groupBoxNewDiscussionViewUI.Size = new System.Drawing.Size(577, 53);
         this.groupBoxNewDiscussionViewUI.TabIndex = 27;
         this.groupBoxNewDiscussionViewUI.TabStop = false;
         this.groupBoxNewDiscussionViewUI.Text = "New Discussion Dialog";
         // 
         // groupBoxGeneral
         // 
         this.groupBoxGeneral.Controls.Add(this.comboBoxColorSchemes);
         this.groupBoxGeneral.Controls.Add(this.labelColorScheme);
         this.groupBoxGeneral.Controls.Add(this.labelVisualTheme);
         this.groupBoxGeneral.Controls.Add(this.comboBoxThemes);
         this.groupBoxGeneral.Controls.Add(this.comboBoxFonts);
         this.groupBoxGeneral.Controls.Add(this.labelFontSize);
         this.groupBoxGeneral.Location = new System.Drawing.Point(6, 6);
         this.groupBoxGeneral.Name = "groupBoxGeneral";
         this.groupBoxGeneral.Size = new System.Drawing.Size(577, 100);
         this.groupBoxGeneral.TabIndex = 26;
         this.groupBoxGeneral.TabStop = false;
         this.groupBoxGeneral.Text = "General";
         // 
         // comboBoxColorSchemes
         // 
         this.comboBoxColorSchemes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxColorSchemes.FormattingEnabled = true;
         this.comboBoxColorSchemes.Location = new System.Drawing.Point(106, 68);
         this.comboBoxColorSchemes.Name = "comboBoxColorSchemes";
         this.comboBoxColorSchemes.Size = new System.Drawing.Size(182, 21);
         this.comboBoxColorSchemes.TabIndex = 18;
         this.comboBoxColorSchemes.SelectionChangeCommitted += new System.EventHandler(this.ComboBoxColorSchemes_SelectionChangeCommited);
         // 
         // labelColorScheme
         // 
         this.labelColorScheme.AutoSize = true;
         this.labelColorScheme.Location = new System.Drawing.Point(6, 71);
         this.labelColorScheme.Name = "labelColorScheme";
         this.labelColorScheme.Size = new System.Drawing.Size(71, 13);
         this.labelColorScheme.TabIndex = 14;
         this.labelColorScheme.Text = "Color scheme";
         // 
         // labelVisualTheme
         // 
         this.labelVisualTheme.AutoSize = true;
         this.labelVisualTheme.Location = new System.Drawing.Point(6, 44);
         this.labelVisualTheme.Name = "labelVisualTheme";
         this.labelVisualTheme.Size = new System.Drawing.Size(40, 13);
         this.labelVisualTheme.TabIndex = 15;
         this.labelVisualTheme.Text = "Theme";
         // 
         // comboBoxThemes
         // 
         this.comboBoxThemes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxThemes.FormattingEnabled = true;
         this.comboBoxThemes.Location = new System.Drawing.Point(106, 41);
         this.comboBoxThemes.Name = "comboBoxThemes";
         this.comboBoxThemes.Size = new System.Drawing.Size(182, 21);
         this.comboBoxThemes.TabIndex = 17;
         this.comboBoxThemes.SelectionChangeCommitted += new System.EventHandler(this.comboBoxThemes_SelectionChangeCommitted);
         // 
         // comboBoxFonts
         // 
         this.comboBoxFonts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxFonts.FormattingEnabled = true;
         this.comboBoxFonts.Location = new System.Drawing.Point(106, 13);
         this.comboBoxFonts.Name = "comboBoxFonts";
         this.comboBoxFonts.Size = new System.Drawing.Size(182, 21);
         this.comboBoxFonts.TabIndex = 16;
         this.comboBoxFonts.SelectionChangeCommitted += new System.EventHandler(this.comboBoxFonts_SelectionChangeCommitted);
         // 
         // labelFontSize
         // 
         this.labelFontSize.AutoSize = true;
         this.labelFontSize.Location = new System.Drawing.Point(6, 16);
         this.labelFontSize.Name = "labelFontSize";
         this.labelFontSize.Size = new System.Drawing.Size(49, 13);
         this.labelFontSize.TabIndex = 18;
         this.labelFontSize.Text = "Font size";
         // 
         // tabPageSettingsBehavior
         // 
         this.tabPageSettingsBehavior.Controls.Add(this.groupBoxNewDiscussionViewBehavior);
         this.tabPageSettingsBehavior.Controls.Add(this.groupBoxRevisionTreeSettings);
         this.tabPageSettingsBehavior.Controls.Add(this.groupBoxGeneralBehavior);
         this.tabPageSettingsBehavior.Location = new System.Drawing.Point(4, 22);
         this.tabPageSettingsBehavior.Name = "tabPageSettingsBehavior";
         this.tabPageSettingsBehavior.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSettingsBehavior.Size = new System.Drawing.Size(1262, 832);
         this.tabPageSettingsBehavior.TabIndex = 8;
         this.tabPageSettingsBehavior.Text = "Behavior";
         this.tabPageSettingsBehavior.UseVisualStyleBackColor = true;
         // 
         // groupBoxNewDiscussionViewBehavior
         // 
         this.groupBoxNewDiscussionViewBehavior.Controls.Add(this.groupBoxShowWarningsOnMismatch);
         this.groupBoxNewDiscussionViewBehavior.Location = new System.Drawing.Point(6, 210);
         this.groupBoxNewDiscussionViewBehavior.Name = "groupBoxNewDiscussionViewBehavior";
         this.groupBoxNewDiscussionViewBehavior.Size = new System.Drawing.Size(577, 126);
         this.groupBoxNewDiscussionViewBehavior.TabIndex = 26;
         this.groupBoxNewDiscussionViewBehavior.TabStop = false;
         this.groupBoxNewDiscussionViewBehavior.Text = "New Discussion Dialog";
         // 
         // groupBoxShowWarningsOnMismatch
         // 
         this.groupBoxShowWarningsOnMismatch.Controls.Add(this.radioButtonShowWarningsNever);
         this.groupBoxShowWarningsOnMismatch.Controls.Add(this.radioButtonShowWarningsOnce);
         this.groupBoxShowWarningsOnMismatch.Controls.Add(this.radioButtonShowWarningsAlways);
         this.groupBoxShowWarningsOnMismatch.Location = new System.Drawing.Point(6, 19);
         this.groupBoxShowWarningsOnMismatch.Name = "groupBoxShowWarningsOnMismatch";
         this.groupBoxShowWarningsOnMismatch.Size = new System.Drawing.Size(202, 95);
         this.groupBoxShowWarningsOnMismatch.TabIndex = 24;
         this.groupBoxShowWarningsOnMismatch.TabStop = false;
         this.groupBoxShowWarningsOnMismatch.Text = "Show warnings on file mismatch";
         // 
         // groupBoxRevisionTreeSettings
         // 
         this.groupBoxRevisionTreeSettings.Controls.Add(this.groupBoxAutoSelection);
         this.groupBoxRevisionTreeSettings.Controls.Add(this.groupBoxRevisionType);
         this.groupBoxRevisionTreeSettings.Location = new System.Drawing.Point(6, 84);
         this.groupBoxRevisionTreeSettings.Name = "groupBoxRevisionTreeSettings";
         this.groupBoxRevisionTreeSettings.Size = new System.Drawing.Size(577, 120);
         this.groupBoxRevisionTreeSettings.TabIndex = 25;
         this.groupBoxRevisionTreeSettings.TabStop = false;
         this.groupBoxRevisionTreeSettings.Text = "Revision Tree";
         // 
         // groupBoxAutoSelection
         // 
         this.groupBoxAutoSelection.Controls.Add(this.radioButtonBaseVsLatest);
         this.groupBoxAutoSelection.Controls.Add(this.radioButtonLastVsLatest);
         this.groupBoxAutoSelection.Controls.Add(this.radioButtonLastVsNext);
         this.groupBoxAutoSelection.Location = new System.Drawing.Point(6, 19);
         this.groupBoxAutoSelection.Name = "groupBoxAutoSelection";
         this.groupBoxAutoSelection.Size = new System.Drawing.Size(206, 89);
         this.groupBoxAutoSelection.TabIndex = 21;
         this.groupBoxAutoSelection.TabStop = false;
         this.groupBoxAutoSelection.Text = "Revision auto-selection mode";
         // 
         // groupBoxRevisionType
         // 
         this.groupBoxRevisionType.Controls.Add(this.radioButtonVersions);
         this.groupBoxRevisionType.Controls.Add(this.radioButtonCommits);
         this.groupBoxRevisionType.Location = new System.Drawing.Point(218, 19);
         this.groupBoxRevisionType.Name = "groupBoxRevisionType";
         this.groupBoxRevisionType.Size = new System.Drawing.Size(205, 89);
         this.groupBoxRevisionType.TabIndex = 22;
         this.groupBoxRevisionType.TabStop = false;
         this.groupBoxRevisionType.Text = "Default revision type";
         // 
         // groupBoxGeneralBehavior
         // 
         this.groupBoxGeneralBehavior.Controls.Add(this.checkBoxRunWhenWindowsStarts);
         this.groupBoxGeneralBehavior.Controls.Add(this.checkBoxMinimizeOnClose);
         this.groupBoxGeneralBehavior.Location = new System.Drawing.Point(6, 6);
         this.groupBoxGeneralBehavior.Name = "groupBoxGeneralBehavior";
         this.groupBoxGeneralBehavior.Size = new System.Drawing.Size(577, 71);
         this.groupBoxGeneralBehavior.TabIndex = 23;
         this.groupBoxGeneralBehavior.TabStop = false;
         this.groupBoxGeneralBehavior.Text = "General";
         // 
         // tabPageSettingsNotifications
         // 
         this.tabPageSettingsNotifications.Controls.Add(this.groupBoxNotifications);
         this.tabPageSettingsNotifications.Location = new System.Drawing.Point(4, 22);
         this.tabPageSettingsNotifications.Name = "tabPageSettingsNotifications";
         this.tabPageSettingsNotifications.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSettingsNotifications.Size = new System.Drawing.Size(1262, 832);
         this.tabPageSettingsNotifications.TabIndex = 4;
         this.tabPageSettingsNotifications.Text = "Notifications";
         this.tabPageSettingsNotifications.UseVisualStyleBackColor = true;
         // 
         // groupBoxNotifications
         // 
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowMergedMergeRequests);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowServiceNotifications);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowNewMergeRequests);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowMyActivity);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowUpdatedMergeRequests);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowKeywords);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowResolvedAll);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowOnMention);
         this.groupBoxNotifications.Location = new System.Drawing.Point(6, 6);
         this.groupBoxNotifications.Name = "groupBoxNotifications";
         this.groupBoxNotifications.Size = new System.Drawing.Size(577, 132);
         this.groupBoxNotifications.TabIndex = 26;
         this.groupBoxNotifications.TabStop = false;
         this.groupBoxNotifications.Text = "Notifications";
         // 
         // checkBoxShowMergedMergeRequests
         // 
         this.checkBoxShowMergedMergeRequests.AutoSize = true;
         this.checkBoxShowMergedMergeRequests.Location = new System.Drawing.Point(6, 41);
         this.checkBoxShowMergedMergeRequests.Name = "checkBoxShowMergedMergeRequests";
         this.checkBoxShowMergedMergeRequests.Size = new System.Drawing.Size(189, 17);
         this.checkBoxShowMergedMergeRequests.TabIndex = 19;
         this.checkBoxShowMergedMergeRequests.Text = "Merged or closed Merge Requests";
         this.checkBoxShowMergedMergeRequests.UseVisualStyleBackColor = true;
         this.checkBoxShowMergedMergeRequests.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowServiceNotifications
         // 
         this.checkBoxShowServiceNotifications.AutoSize = true;
         this.checkBoxShowServiceNotifications.Location = new System.Drawing.Point(231, 105);
         this.checkBoxShowServiceNotifications.Name = "checkBoxShowServiceNotifications";
         this.checkBoxShowServiceNotifications.Size = new System.Drawing.Size(149, 17);
         this.checkBoxShowServiceNotifications.TabIndex = 25;
         this.checkBoxShowServiceNotifications.Text = "Show service notifications";
         this.checkBoxShowServiceNotifications.UseVisualStyleBackColor = true;
         this.checkBoxShowServiceNotifications.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowNewMergeRequests
         // 
         this.checkBoxShowNewMergeRequests.AutoSize = true;
         this.checkBoxShowNewMergeRequests.Location = new System.Drawing.Point(6, 18);
         this.checkBoxShowNewMergeRequests.Name = "checkBoxShowNewMergeRequests";
         this.checkBoxShowNewMergeRequests.Size = new System.Drawing.Size(129, 17);
         this.checkBoxShowNewMergeRequests.TabIndex = 18;
         this.checkBoxShowNewMergeRequests.Text = "New Merge Requests";
         this.checkBoxShowNewMergeRequests.UseVisualStyleBackColor = true;
         this.checkBoxShowNewMergeRequests.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowMyActivity
         // 
         this.checkBoxShowMyActivity.AutoSize = true;
         this.checkBoxShowMyActivity.Location = new System.Drawing.Point(6, 105);
         this.checkBoxShowMyActivity.Name = "checkBoxShowMyActivity";
         this.checkBoxShowMyActivity.Size = new System.Drawing.Size(113, 17);
         this.checkBoxShowMyActivity.TabIndex = 21;
         this.checkBoxShowMyActivity.Text = "Include my activity";
         this.checkBoxShowMyActivity.UseVisualStyleBackColor = true;
         this.checkBoxShowMyActivity.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowUpdatedMergeRequests
         // 
         this.checkBoxShowUpdatedMergeRequests.AutoSize = true;
         this.checkBoxShowUpdatedMergeRequests.Location = new System.Drawing.Point(6, 64);
         this.checkBoxShowUpdatedMergeRequests.Name = "checkBoxShowUpdatedMergeRequests";
         this.checkBoxShowUpdatedMergeRequests.Size = new System.Drawing.Size(181, 17);
         this.checkBoxShowUpdatedMergeRequests.TabIndex = 20;
         this.checkBoxShowUpdatedMergeRequests.Text = "New commits in Merge Requests";
         this.checkBoxShowUpdatedMergeRequests.UseVisualStyleBackColor = true;
         this.checkBoxShowUpdatedMergeRequests.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowKeywords
         // 
         this.checkBoxShowKeywords.AutoSize = true;
         this.checkBoxShowKeywords.Location = new System.Drawing.Point(231, 64);
         this.checkBoxShowKeywords.Name = "checkBoxShowKeywords";
         this.checkBoxShowKeywords.Size = new System.Drawing.Size(75, 17);
         this.checkBoxShowKeywords.TabIndex = 24;
         this.checkBoxShowKeywords.Text = "Keywords:";
         this.checkBoxShowKeywords.UseVisualStyleBackColor = true;
         this.checkBoxShowKeywords.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowResolvedAll
         // 
         this.checkBoxShowResolvedAll.AutoSize = true;
         this.checkBoxShowResolvedAll.Location = new System.Drawing.Point(231, 18);
         this.checkBoxShowResolvedAll.Name = "checkBoxShowResolvedAll";
         this.checkBoxShowResolvedAll.Size = new System.Drawing.Size(127, 17);
         this.checkBoxShowResolvedAll.TabIndex = 22;
         this.checkBoxShowResolvedAll.Text = "Resolved All Threads";
         this.checkBoxShowResolvedAll.UseVisualStyleBackColor = true;
         this.checkBoxShowResolvedAll.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // checkBoxShowOnMention
         // 
         this.checkBoxShowOnMention.AutoSize = true;
         this.checkBoxShowOnMention.Location = new System.Drawing.Point(231, 41);
         this.checkBoxShowOnMention.Name = "checkBoxShowOnMention";
         this.checkBoxShowOnMention.Size = new System.Drawing.Size(170, 17);
         this.checkBoxShowOnMention.TabIndex = 23;
         this.checkBoxShowOnMention.Text = "When someone mentioned me";
         this.checkBoxShowOnMention.UseVisualStyleBackColor = true;
         this.checkBoxShowOnMention.CheckedChanged += new System.EventHandler(this.checkBoxNotifications_CheckedChanged);
         // 
         // tabPageWorkflow
         // 
         this.tabPageWorkflow.Controls.Add(this.groupBoxSelectWorkflow);
         this.tabPageWorkflow.Controls.Add(this.groupBoxConfigureProjectBasedWorkflow);
         this.tabPageWorkflow.Controls.Add(this.groupBoxConfigureUserBasedWorkflow);
         this.tabPageWorkflow.Controls.Add(this.groupBoxSelectHost);
         this.tabPageWorkflow.Location = new System.Drawing.Point(4, 22);
         this.tabPageWorkflow.Name = "tabPageWorkflow";
         this.tabPageWorkflow.Size = new System.Drawing.Size(1276, 864);
         this.tabPageWorkflow.TabIndex = 2;
         this.tabPageWorkflow.Text = "Workflow";
         this.tabPageWorkflow.UseVisualStyleBackColor = true;
         // 
         // groupBoxSelectWorkflow
         // 
         this.groupBoxSelectWorkflow.Controls.Add(this.linkLabelWorkflowDescription);
         this.groupBoxSelectWorkflow.Controls.Add(this.radioButtonSelectByProjects);
         this.groupBoxSelectWorkflow.Controls.Add(this.radioButtonSelectByUsernames);
         this.groupBoxSelectWorkflow.Location = new System.Drawing.Point(8, 61);
         this.groupBoxSelectWorkflow.Name = "groupBoxSelectWorkflow";
         this.groupBoxSelectWorkflow.Size = new System.Drawing.Size(577, 52);
         this.groupBoxSelectWorkflow.TabIndex = 20;
         this.groupBoxSelectWorkflow.TabStop = false;
         this.groupBoxSelectWorkflow.Text = "Select Workflow";
         // 
         // groupBoxConfigureProjectBasedWorkflow
         // 
         this.groupBoxConfigureProjectBasedWorkflow.Controls.Add(this.buttonEditProjects);
         this.groupBoxConfigureProjectBasedWorkflow.Controls.Add(this.listViewProjects);
         this.groupBoxConfigureProjectBasedWorkflow.Location = new System.Drawing.Point(312, 117);
         this.groupBoxConfigureProjectBasedWorkflow.Name = "groupBoxConfigureProjectBasedWorkflow";
         this.groupBoxConfigureProjectBasedWorkflow.Size = new System.Drawing.Size(273, 219);
         this.groupBoxConfigureProjectBasedWorkflow.TabIndex = 19;
         this.groupBoxConfigureProjectBasedWorkflow.TabStop = false;
         this.groupBoxConfigureProjectBasedWorkflow.Text = "Configure Project-based workflow";
         // 
         // groupBoxConfigureUserBasedWorkflow
         // 
         this.groupBoxConfigureUserBasedWorkflow.Controls.Add(this.listViewUsers);
         this.groupBoxConfigureUserBasedWorkflow.Controls.Add(this.buttonEditUsers);
         this.groupBoxConfigureUserBasedWorkflow.Location = new System.Drawing.Point(8, 117);
         this.groupBoxConfigureUserBasedWorkflow.Name = "groupBoxConfigureUserBasedWorkflow";
         this.groupBoxConfigureUserBasedWorkflow.Size = new System.Drawing.Size(273, 219);
         this.groupBoxConfigureUserBasedWorkflow.TabIndex = 18;
         this.groupBoxConfigureUserBasedWorkflow.TabStop = false;
         this.groupBoxConfigureUserBasedWorkflow.Text = "Configure User-based workflow";
         // 
         // groupBoxSelectHost
         // 
         this.groupBoxSelectHost.Controls.Add(this.comboBoxHost);
         this.groupBoxSelectHost.Location = new System.Drawing.Point(8, 3);
         this.groupBoxSelectHost.Name = "groupBoxSelectHost";
         this.groupBoxSelectHost.Size = new System.Drawing.Size(577, 52);
         this.groupBoxSelectHost.TabIndex = 17;
         this.groupBoxSelectHost.TabStop = false;
         this.groupBoxSelectHost.Text = "Select Host";
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
         this.splitContainer1.Panel1.AutoScroll = true;
         this.splitContainer1.Panel1.Controls.Add(this.linkLabelFromClipboard);
         this.splitContainer1.Panel1.Controls.Add(this.tabControlMode);
         // 
         // splitContainer1.Panel2
         // 
         this.splitContainer1.Panel2.AutoScroll = true;
         this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
         this.splitContainer1.Size = new System.Drawing.Size(1270, 858);
         this.splitContainer1.SplitterDistance = 516;
         this.splitContainer1.SplitterWidth = 8;
         this.splitContainer1.TabIndex = 4;
         this.splitContainer1.TabStop = false;
         this.splitContainer1.SplitterMoving += new System.Windows.Forms.SplitterCancelEventHandler(this.splitContainer_SplitterMoving);
         this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer_SplitterMoved);
         // 
         // linkLabelFromClipboard
         // 
         this.linkLabelFromClipboard.AutoSize = true;
         this.linkLabelFromClipboard.Enabled = false;
         this.linkLabelFromClipboard.Location = new System.Drawing.Point(200, 2);
         this.linkLabelFromClipboard.Name = "linkLabelFromClipboard";
         this.linkLabelFromClipboard.Size = new System.Drawing.Size(103, 13);
         this.linkLabelFromClipboard.TabIndex = 3;
         this.linkLabelFromClipboard.TabStop = true;
         this.linkLabelFromClipboard.Text = "Open from Clipboard";
         this.linkLabelFromClipboard.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabelFromClipboard_LinkClicked);
         // 
         // tabControlMode
         // 
         this.tabControlMode.Controls.Add(this.tabPageLive);
         this.tabControlMode.Controls.Add(this.tabPageSearch);
         this.tabControlMode.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tabControlMode.Location = new System.Drawing.Point(0, 0);
         this.tabControlMode.Name = "tabControlMode";
         this.tabControlMode.SelectedIndex = 0;
         this.tabControlMode.Size = new System.Drawing.Size(516, 858);
         this.tabControlMode.TabIndex = 0;
         this.tabControlMode.SelectedIndexChanged += new System.EventHandler(this.tabControlMode_SelectedIndexChanged);
         this.tabControlMode.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControl_Selecting);
         this.tabControlMode.SizeChanged += new System.EventHandler(this.tabControlMode_SizeChanged);
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
         this.splitContainer2.Panel1.AutoScroll = true;
         this.splitContainer2.Panel1.Controls.Add(this.groupBoxSelectedMR);
         // 
         // splitContainer2.Panel2
         // 
         this.splitContainer2.Panel2.AutoScroll = true;
         this.splitContainer2.Panel2.Controls.Add(this.panelFreeSpace);
         this.splitContainer2.Panel2.Controls.Add(this.panelStatusBar);
         this.splitContainer2.Panel2.Controls.Add(this.panelBottomMenu);
         this.splitContainer2.Panel2.Controls.Add(this.groupBoxActions);
         this.splitContainer2.Panel2.Controls.Add(this.groupBoxTimeTracking);
         this.splitContainer2.Panel2.Controls.Add(this.groupBoxReview);
         this.splitContainer2.Panel2.Controls.Add(this.groupBoxSelectRevisions);
         this.splitContainer2.Size = new System.Drawing.Size(746, 858);
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
         this.groupBoxSelectedMR.Size = new System.Drawing.Size(746, 267);
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
         this.linkLabelConnectedTo.Size = new System.Drawing.Size(740, 18);
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
         this.richTextBoxMergeRequestDescription.Name = "richTextBoxMergeRequestDescription";
         this.richTextBoxMergeRequestDescription.Size = new System.Drawing.Size(740, 227);
         this.richTextBoxMergeRequestDescription.TabIndex = 2;
         this.richTextBoxMergeRequestDescription.TabStop = false;
         this.richTextBoxMergeRequestDescription.Text = null;
         // 
         // panelFreeSpace
         // 
         this.panelFreeSpace.Controls.Add(this.pictureBox2);
         this.panelFreeSpace.Controls.Add(this.pictureBox1);
         this.panelFreeSpace.Dock = System.Windows.Forms.DockStyle.Fill;
         this.panelFreeSpace.Location = new System.Drawing.Point(0, 427);
         this.panelFreeSpace.MinimumSize = new System.Drawing.Size(0, 10);
         this.panelFreeSpace.Name = "panelFreeSpace";
         this.panelFreeSpace.Size = new System.Drawing.Size(746, 70);
         this.panelFreeSpace.TabIndex = 9;
         // 
         // pictureBox2
         // 
         this.pictureBox2.Dock = System.Windows.Forms.DockStyle.Right;
         this.pictureBox2.Location = new System.Drawing.Point(496, 0);
         this.pictureBox2.MinimumSize = new System.Drawing.Size(250, 100);
         this.pictureBox2.Name = "pictureBox2";
         this.pictureBox2.Size = new System.Drawing.Size(250, 100);
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
         this.pictureBox1.Size = new System.Drawing.Size(250, 100);
         this.pictureBox1.TabIndex = 9;
         this.pictureBox1.TabStop = false;
         this.pictureBox1.Visible = false;
         // 
         // panelStatusBar
         // 
         this.panelStatusBar.BackColor = System.Drawing.Color.WhiteSmoke;
         this.panelStatusBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.panelStatusBar.Controls.Add(this.linkLabelAbortGitClone);
         this.panelStatusBar.Controls.Add(this.labelStorageStatus);
         this.panelStatusBar.Controls.Add(this.labelWorkflowStatus);
         this.panelStatusBar.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.panelStatusBar.Location = new System.Drawing.Point(0, 497);
         this.panelStatusBar.Name = "panelStatusBar";
         this.panelStatusBar.Size = new System.Drawing.Size(746, 52);
         this.panelStatusBar.TabIndex = 10;
         // 
         // labelStorageStatus
         // 
         this.labelStorageStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.labelStorageStatus.AutoEllipsis = true;
         this.labelStorageStatus.Location = new System.Drawing.Point(0, 31);
         this.labelStorageStatus.Name = "labelStorageStatus";
         this.labelStorageStatus.Size = new System.Drawing.Size(701, 16);
         this.labelStorageStatus.TabIndex = 1;
         this.labelStorageStatus.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor in" +
    "cididunt ut labore et dolore";
         // 
         // labelWorkflowStatus
         // 
         this.labelWorkflowStatus.AutoEllipsis = true;
         this.labelWorkflowStatus.Dock = System.Windows.Forms.DockStyle.Top;
         this.labelWorkflowStatus.Location = new System.Drawing.Point(0, 0);
         this.labelWorkflowStatus.Name = "labelWorkflowStatus";
         this.labelWorkflowStatus.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
         this.labelWorkflowStatus.Size = new System.Drawing.Size(744, 24);
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
         this.panelBottomMenu.Size = new System.Drawing.Size(746, 34);
         this.panelBottomMenu.TabIndex = 11;
         // 
         // groupBoxActions
         // 
         this.groupBoxActions.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxActions.Location = new System.Drawing.Point(0, 364);
         this.groupBoxActions.Name = "groupBoxActions";
         this.groupBoxActions.Size = new System.Drawing.Size(746, 63);
         this.groupBoxActions.TabIndex = 0;
         this.groupBoxActions.TabStop = false;
         this.groupBoxActions.Text = "Actions";
         this.groupBoxActions.SizeChanged += new System.EventHandler(this.groupBoxActions_SizeChanged);
         // 
         // groupBoxTimeTracking
         // 
         this.groupBoxTimeTracking.Controls.Add(this.labelTimeTrackingTrackedLabel);
         this.groupBoxTimeTracking.Controls.Add(this.linkLabelTimeTrackingMergeRequest);
         this.groupBoxTimeTracking.Controls.Add(this.buttonEditTime);
         this.groupBoxTimeTracking.Controls.Add(this.buttonTimeTrackingCancel);
         this.groupBoxTimeTracking.Controls.Add(this.buttonTimeTrackingStart);
         this.groupBoxTimeTracking.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxTimeTracking.Location = new System.Drawing.Point(0, 263);
         this.groupBoxTimeTracking.Name = "groupBoxTimeTracking";
         this.groupBoxTimeTracking.Size = new System.Drawing.Size(746, 101);
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
         this.labelTimeTrackingTrackedLabel.Size = new System.Drawing.Size(740, 22);
         this.labelTimeTrackingTrackedLabel.TabIndex = 2;
         this.labelTimeTrackingTrackedLabel.Text = "Total Time";
         // 
         // linkLabelTimeTrackingMergeRequest
         // 
         this.linkLabelTimeTrackingMergeRequest.AutoEllipsis = true;
         this.linkLabelTimeTrackingMergeRequest.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.linkLabelTimeTrackingMergeRequest.Location = new System.Drawing.Point(3, 76);
         this.linkLabelTimeTrackingMergeRequest.Name = "linkLabelTimeTrackingMergeRequest";
         this.linkLabelTimeTrackingMergeRequest.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
         this.linkLabelTimeTrackingMergeRequest.Size = new System.Drawing.Size(740, 22);
         this.linkLabelTimeTrackingMergeRequest.TabIndex = 5;
         this.linkLabelTimeTrackingMergeRequest.TabStop = true;
         this.linkLabelTimeTrackingMergeRequest.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";
         this.linkLabelTimeTrackingMergeRequest.Visible = false;
         this.linkLabelTimeTrackingMergeRequest.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelTimeTrackingMergeRequest_LinkClicked);
         // 
         // groupBoxReview
         // 
         this.groupBoxReview.Controls.Add(this.buttonDiffTool);
         this.groupBoxReview.Controls.Add(this.buttonAddComment);
         this.groupBoxReview.Controls.Add(this.buttonDiscussions);
         this.groupBoxReview.Controls.Add(this.buttonNewDiscussion);
         this.groupBoxReview.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxReview.Location = new System.Drawing.Point(0, 200);
         this.groupBoxReview.Name = "groupBoxReview";
         this.groupBoxReview.Size = new System.Drawing.Size(746, 63);
         this.groupBoxReview.TabIndex = 2;
         this.groupBoxReview.TabStop = false;
         this.groupBoxReview.Text = "Review";
         // 
         // groupBoxSelectRevisions
         // 
         this.groupBoxSelectRevisions.Controls.Add(this.revisionBrowser);
         this.groupBoxSelectRevisions.Dock = System.Windows.Forms.DockStyle.Top;
         this.groupBoxSelectRevisions.Location = new System.Drawing.Point(0, 0);
         this.groupBoxSelectRevisions.Name = "groupBoxSelectRevisions";
         this.groupBoxSelectRevisions.Size = new System.Drawing.Size(746, 200);
         this.groupBoxSelectRevisions.TabIndex = 4;
         this.groupBoxSelectRevisions.TabStop = false;
         this.groupBoxSelectRevisions.Text = "Select revisions for comparison";
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
         // linkLabelWorkflowDescription
         // 
         this.linkLabelWorkflowDescription.AutoSize = true;
         this.linkLabelWorkflowDescription.Location = new System.Drawing.Point(441, 21);
         this.linkLabelWorkflowDescription.Name = "linkLabelWorkflowDescription";
         this.linkLabelWorkflowDescription.Size = new System.Drawing.Size(128, 13);
         this.linkLabelWorkflowDescription.TabIndex = 31;
         this.linkLabelWorkflowDescription.TabStop = true;
         this.linkLabelWorkflowDescription.Text = "Show detailed description";
         this.linkLabelWorkflowDescription.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelWorkflowDescription_LinkClicked);
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
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
         this.Load += new System.EventHandler(this.MainForm_Load);
         this.Resize += new System.EventHandler(this.MainForm_Resize);
         this.tabPageLive.ResumeLayout(false);
         this.groupBoxSelectMergeRequest.ResumeLayout(false);
         this.groupBoxSelectMergeRequest.PerformLayout();
         this.tabPageSearch.ResumeLayout(false);
         this.groupBoxSearch.ResumeLayout(false);
         this.groupBoxSearch.PerformLayout();
         this.contextMenuStrip.ResumeLayout(false);
         this.tabControl.ResumeLayout(false);
         this.tabPageSettings.ResumeLayout(false);
         this.tabControlSettings.ResumeLayout(false);
         this.tabPageSettingsAccessTokens.ResumeLayout(false);
         this.groupBoxKnownHosts.ResumeLayout(false);
         this.tabPageSettingsStorage.ResumeLayout(false);
         this.groupBoxFileStorage.ResumeLayout(false);
         this.groupBoxFileStorage.PerformLayout();
         this.groupBoxFileStorageType.ResumeLayout(false);
         this.groupBoxFileStorageType.PerformLayout();
         this.tabPageSettingsUserInterface.ResumeLayout(false);
         this.groupBoxOtherUI.ResumeLayout(false);
         this.groupBoxOtherUI.PerformLayout();
         this.groupBoxDiscussionsView.ResumeLayout(false);
         this.groupBoxDiscussionsView.PerformLayout();
         this.groupBoxNewDiscussionViewUI.ResumeLayout(false);
         this.groupBoxNewDiscussionViewUI.PerformLayout();
         this.groupBoxGeneral.ResumeLayout(false);
         this.groupBoxGeneral.PerformLayout();
         this.tabPageSettingsBehavior.ResumeLayout(false);
         this.groupBoxNewDiscussionViewBehavior.ResumeLayout(false);
         this.groupBoxShowWarningsOnMismatch.ResumeLayout(false);
         this.groupBoxShowWarningsOnMismatch.PerformLayout();
         this.groupBoxRevisionTreeSettings.ResumeLayout(false);
         this.groupBoxAutoSelection.ResumeLayout(false);
         this.groupBoxAutoSelection.PerformLayout();
         this.groupBoxRevisionType.ResumeLayout(false);
         this.groupBoxRevisionType.PerformLayout();
         this.groupBoxGeneralBehavior.ResumeLayout(false);
         this.groupBoxGeneralBehavior.PerformLayout();
         this.tabPageSettingsNotifications.ResumeLayout(false);
         this.groupBoxNotifications.ResumeLayout(false);
         this.groupBoxNotifications.PerformLayout();
         this.tabPageWorkflow.ResumeLayout(false);
         this.groupBoxSelectWorkflow.ResumeLayout(false);
         this.groupBoxSelectWorkflow.PerformLayout();
         this.groupBoxConfigureProjectBasedWorkflow.ResumeLayout(false);
         this.groupBoxConfigureUserBasedWorkflow.ResumeLayout(false);
         this.groupBoxSelectHost.ResumeLayout(false);
         this.tabPageMR.ResumeLayout(false);
         this.splitContainer1.Panel1.ResumeLayout(false);
         this.splitContainer1.Panel1.PerformLayout();
         this.splitContainer1.Panel2.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
         this.splitContainer1.ResumeLayout(false);
         this.tabControlMode.ResumeLayout(false);
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
         this.groupBoxSelectRevisions.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion
      private System.Windows.Forms.ToolTip toolTip;
      private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
      private System.Windows.Forms.ToolStripMenuItem restoreToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
      private System.Windows.Forms.NotifyIcon notifyIcon;
      private System.Windows.Forms.FolderBrowserDialog storageFolderBrowser;
      private System.Windows.Forms.TabControl tabControl;
      private System.Windows.Forms.TabPage tabPageSettings;
      private System.Windows.Forms.TabPage tabPageMR;
      private System.Windows.Forms.SplitContainer splitContainer1;
      private System.Windows.Forms.SplitContainer splitContainer2;
      private System.Windows.Forms.GroupBox groupBoxSelectedMR;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel richTextBoxMergeRequestDescription;
      private System.Windows.Forms.LinkLabel linkLabelConnectedTo;
      private System.Windows.Forms.GroupBox groupBoxTimeTracking;
      private System.Windows.Forms.LinkLabel linkLabelTimeTrackingMergeRequest;
      private System.Windows.Forms.Button buttonEditTime;
      private System.Windows.Forms.Label labelTimeTrackingTrackedLabel;
      private CommonControls.Controls.ConfirmCancelButton buttonTimeTrackingCancel;
      private System.Windows.Forms.Button buttonTimeTrackingStart;
      private System.Windows.Forms.Panel panel1;
      private System.Windows.Forms.GroupBox groupBoxActions;
      private System.Windows.Forms.GroupBox groupBoxSelectRevisions;
      private System.Windows.Forms.Button buttonDiffTool;
      private System.Windows.Forms.Panel panel4;
      private System.Windows.Forms.GroupBox groupBoxReview;
      private System.Windows.Forms.Button buttonAddComment;
      private System.Windows.Forms.Button buttonDiscussions;
      private System.Windows.Forms.Button buttonNewDiscussion;
      private System.Windows.Forms.Panel panelStatusBar;
      private System.Windows.Forms.LinkLabel linkLabelAbortGitClone;
      private System.Windows.Forms.Label labelStorageStatus;
      private System.Windows.Forms.Label labelWorkflowStatus;
      private System.Windows.Forms.Panel panelBottomMenu;
      private System.Windows.Forms.LinkLabel linkLabelHelp;
      private System.Windows.Forms.LinkLabel linkLabelSendFeedback;
      private System.Windows.Forms.LinkLabel linkLabelNewVersion;
      private System.Windows.Forms.Panel panelFreeSpace;
      private System.Windows.Forms.PictureBox pictureBox2;
      private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TabControl tabControlMode;
        private System.Windows.Forms.TabPage tabPageLive;
        private System.Windows.Forms.GroupBox groupBoxSelectMergeRequest;
        private System.Windows.Forms.Button buttonReloadList;
        private mrHelper.CommonControls.Controls.DelayedTextBox textBoxDisplayFilter;
        private Controls.MergeRequestListView listViewMergeRequests;
        private System.Windows.Forms.ColumnHeader columnHeaderIId;
        private System.Windows.Forms.ColumnHeader columnHeaderAuthor;
        private System.Windows.Forms.ColumnHeader columnHeaderTitle;
        private System.Windows.Forms.ColumnHeader columnHeaderLabels;
        private System.Windows.Forms.ColumnHeader columnHeaderSize;
        private System.Windows.Forms.ColumnHeader columnHeaderJira;
        private System.Windows.Forms.ColumnHeader columnHeaderTotalTime;
        private System.Windows.Forms.ColumnHeader columnHeaderSourceBranch;
        private System.Windows.Forms.ColumnHeader columnHeaderTargetBranch;
        private System.Windows.Forms.ColumnHeader columnHeaderResolved;
        private System.Windows.Forms.CheckBox checkBoxDisplayFilter;
        private System.Windows.Forms.TabPage tabPageSearch;
        private System.Windows.Forms.GroupBox groupBoxSearch;
        private System.Windows.Forms.TextBox textBoxSearch;
        private Controls.MergeRequestListView listViewFoundMergeRequests;
        private System.Windows.Forms.ColumnHeader columnHeaderFoundIId;
        private System.Windows.Forms.ColumnHeader columnHeaderFoundAuthor;
        private System.Windows.Forms.ColumnHeader columnHeaderFoundTitle;
        private System.Windows.Forms.ColumnHeader columnHeaderFoundLabels;
        private System.Windows.Forms.ColumnHeader columnHeaderFoundJira;
        private System.Windows.Forms.ColumnHeader columnHeaderFoundSourceBranch;
        private System.Windows.Forms.ColumnHeader columnHeaderFoundTargetBranch;
        private System.Windows.Forms.ColumnHeader columnHeaderFoundState;
        private System.Windows.Forms.RadioButton radioButtonSearchByTargetBranch;
        private System.Windows.Forms.RadioButton radioButtonSearchByTitleAndDescription;
      private Controls.RevisionBrowser revisionBrowser;
      private Button buttonCreateNew;
      private LinkLabel linkLabelFromClipboard;
      private TabControl tabControlSettings;
      private TabPage tabPageSettingsAccessTokens;
      private TabPage tabPageSettingsStorage;
      private ComboBox comboBoxHost;
      private TabPage tabPageSettingsUserInterface;
      private CheckBox checkBoxNewDiscussionIsTopMostForm;
      private CheckBox checkBoxDisableSplitterRestrictions;
      private Label labelFontSize;
      private ComboBox comboBoxFonts;
      private ComboBox comboBoxThemes;
      private Label labelVisualTheme;
      private ComboBox comboBoxColorSchemes;
      private Label labelColorScheme;
      private TabPage tabPageSettingsBehavior;
      private GroupBox groupBoxNewDiscussionViewBehavior;
      private GroupBox groupBoxShowWarningsOnMismatch;
      private RadioButton radioButtonShowWarningsNever;
      private RadioButton radioButtonShowWarningsOnce;
      private RadioButton radioButtonShowWarningsAlways;
      private GroupBox groupBoxRevisionTreeSettings;
      private GroupBox groupBoxAutoSelection;
      private RadioButton radioButtonBaseVsLatest;
      private RadioButton radioButtonLastVsLatest;
      private RadioButton radioButtonLastVsNext;
      private GroupBox groupBoxRevisionType;
      private RadioButton radioButtonVersions;
      private RadioButton radioButtonCommits;
      private GroupBox groupBoxGeneralBehavior;
      private CheckBox checkBoxRunWhenWindowsStarts;
      private CheckBox checkBoxMinimizeOnClose;
      private TabPage tabPageSettingsNotifications;
      private CheckBox checkBoxShowServiceNotifications;
      private CheckBox checkBoxShowMyActivity;
      private CheckBox checkBoxShowKeywords;
      private CheckBox checkBoxShowOnMention;
      private CheckBox checkBoxShowResolvedAll;
      private CheckBox checkBoxShowUpdatedMergeRequests;
      private CheckBox checkBoxShowMergedMergeRequests;
      private CheckBox checkBoxShowNewMergeRequests;
      private GroupBox groupBoxNotifications;
      private GroupBox groupBoxKnownHosts;
      private Button buttonRemoveKnownHost;
      private Button buttonAddKnownHost;
      private ListView listViewKnownHosts;
      private ColumnHeader columnHeaderHost;
      private ColumnHeader columnHeaderAccessToken;
      private GroupBox groupBoxFileStorage;
      private Button buttonBrowseStorageFolder;
      private Label labelLocalStorageFolder;
      private TextBox textBoxStorageFolder;
      private GroupBox groupBoxSelectHost;
      private RadioButton radioButtonSelectByProjects;
      private Button buttonEditUsers;
      private ListView listViewUsers;
      private ColumnHeader columnHeader1;
      private RadioButton radioButtonSelectByUsernames;
      private Button buttonEditProjects;
      private ListView listViewProjects;
      private ColumnHeader columnHeaderName;
      private GroupBox groupBoxOtherUI;
      private GroupBox groupBoxDiscussionsView;
      private Label labelDepth;
      private ComboBox comboBoxDCDepth;
      private GroupBox groupBoxNewDiscussionViewUI;
      private GroupBox groupBoxGeneral;
      private GroupBox groupBoxFileStorageType;
      private LinkLabel linkLabelCommitStorageDescription;
      private RadioButton radioButtonUseGitShallowClone;
      private RadioButton radioButtonDontUseGit;
      private RadioButton radioButtonUseGitFullClone;
      private TabPage tabPageWorkflow;
      private GroupBox groupBoxConfigureProjectBasedWorkflow;
      private GroupBox groupBoxConfigureUserBasedWorkflow;
      private GroupBox groupBoxSelectWorkflow;
      private LinkLabel linkLabelWorkflowDescription;
   }
}

