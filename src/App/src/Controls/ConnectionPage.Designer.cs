
namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
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

         Program.Settings.MainWindowLayoutChanged -= onMainWindowLayoutChanged;
         Program.Settings.WordWrapLongRowsChanged -= onWrapLongRowsChanged;

         finalizeWork();

         disposeGitHelpers();
         disposeLocalGitRepositoryFactory();
         disposeLiveDataCacheDependencies();

         resetLostConnectionInfo();

         _liveDataCache.Dispose();
         _liveDataCache = null;
         _searchDataCache.Dispose();
         _searchDataCache = null;
         _recentDataCache.Dispose();
         _recentDataCache = null;

         disposeGitLabInstance();

         stopRedrawTimer();
         _redrawTimer.Dispose();

         base.Dispose(disposing);
      }

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.components = new System.ComponentModel.Container();
         this.splitContainerPrimary = new System.Windows.Forms.SplitContainer();
         this.tabControlMode = new PlainTabControl();
         this.tabPageLive = new System.Windows.Forms.TabPage();
         this.groupBoxSelectMergeRequest = new System.Windows.Forms.GroupBox();
         this.textBoxDisplayFilter = new mrHelper.CommonControls.Controls.DelayedTextBox();
         this.listViewLiveMergeRequests = new mrHelper.App.Controls.MergeRequestListView();
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
         this.columnHeaderRefreshTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderActivities = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.checkBoxDisplayFilter = new System.Windows.Forms.CheckBox();
         this.tabPageSearch = new System.Windows.Forms.TabPage();
         this.groupBoxSearchMergeRequest = new System.Windows.Forms.GroupBox();
         this.linkLabelNewSearch = new System.Windows.Forms.LinkLabel();
         this.listViewFoundMergeRequests = new mrHelper.App.Controls.MergeRequestListView();
         this.columnHeaderFoundIId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundAuthor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundLabels = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundJira = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundSourceBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundTargetBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundActivities = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.tabPageRecent = new System.Windows.Forms.TabPage();
         this.groupBoxRecentMergeRequest = new System.Windows.Forms.GroupBox();
         this.textBoxRecentMergeRequestsHint = new System.Windows.Forms.TextBox();
         this.listViewRecentMergeRequests = new mrHelper.App.Controls.MergeRequestListView();
         this.columnHeaderRecentIId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentAuthor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentLabels = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentJira = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentSourceBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentTargetBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentActivities = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.splitContainerSecondary = new System.Windows.Forms.SplitContainer();
         this.groupBoxSelectedMR = new System.Windows.Forms.GroupBox();
         this.richTextBoxMergeRequestDescription = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.linkLabelConnectedTo = new mrHelper.CommonControls.Controls.LinkLabelEx();
         this.groupBoxSelectRevisions = new System.Windows.Forms.GroupBox();
         this.revisionBrowser = new mrHelper.App.Controls.RevisionBrowser();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainerPrimary)).BeginInit();
         this.splitContainerPrimary.Panel1.SuspendLayout();
         this.splitContainerPrimary.Panel2.SuspendLayout();
         this.splitContainerPrimary.SuspendLayout();
         this.tabControlMode.SuspendLayout();
         this.tabPageLive.SuspendLayout();
         this.groupBoxSelectMergeRequest.SuspendLayout();
         this.tabPageSearch.SuspendLayout();
         this.groupBoxSearchMergeRequest.SuspendLayout();
         this.tabPageRecent.SuspendLayout();
         this.groupBoxRecentMergeRequest.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainerSecondary)).BeginInit();
         this.splitContainerSecondary.Panel1.SuspendLayout();
         this.splitContainerSecondary.Panel2.SuspendLayout();
         this.splitContainerSecondary.SuspendLayout();
         this.groupBoxSelectedMR.SuspendLayout();
         this.groupBoxSelectRevisions.SuspendLayout();
         this.SuspendLayout();
         // 
         // splitContainer1
         // 
         this.splitContainerPrimary.BackColor = System.Drawing.Color.Transparent;
         this.splitContainerPrimary.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainerPrimary.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
         this.splitContainerPrimary.Location = new System.Drawing.Point(0, 0);
         this.splitContainerPrimary.Name = "splitContainerPrimary";
         // 
         // splitContainer1.Panel1
         // 
         this.splitContainerPrimary.Panel1.AutoScroll = true;
         this.splitContainerPrimary.Panel1.Controls.Add(this.tabControlMode);
         // 
         // splitContainer1.Panel2
         // 
         this.splitContainerPrimary.Panel2.AutoScroll = true;
         this.splitContainerPrimary.Panel2.Controls.Add(this.splitContainerSecondary);
         this.splitContainerPrimary.Size = new System.Drawing.Size(866, 422);
         this.splitContainerPrimary.SplitterDistance = 538;
         this.splitContainerPrimary.SplitterWidth = 8;
         this.splitContainerPrimary.TabIndex = 5;
         this.splitContainerPrimary.TabStop = false;
         this.splitContainerPrimary.SplitterMoving += new System.Windows.Forms.SplitterCancelEventHandler(this.splitContainer_SplitterMoving);
         this.splitContainerPrimary.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer_SplitterMoved);
         // 
         // tabControlMode
         // 
         this.tabControlMode.Controls.Add(this.tabPageLive);
         this.tabControlMode.Controls.Add(this.tabPageSearch);
         this.tabControlMode.Controls.Add(this.tabPageRecent);
         this.tabControlMode.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tabControlMode.ItemSize = new System.Drawing.Size(0, 1);
         this.tabControlMode.Location = new System.Drawing.Point(0, 0);
         this.tabControlMode.Name = "tabControlMode";
         this.tabControlMode.SelectedIndex = 0;
         this.tabControlMode.Size = new System.Drawing.Size(538, 422);
         this.tabControlMode.TabIndex = 0;
         this.tabControlMode.SelectedIndexChanged += new System.EventHandler(this.tabControlMode_SelectedIndexChanged);
         // 
         // tabPageLive
         // 
         this.tabPageLive.AutoScroll = true;
         this.tabPageLive.Controls.Add(this.groupBoxSelectMergeRequest);
         this.tabPageLive.Location = new System.Drawing.Point(4, 5);
         this.tabPageLive.Name = "tabPageLive";
         this.tabPageLive.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageLive.Size = new System.Drawing.Size(530, 413);
         this.tabPageLive.TabIndex = 0;
         this.tabPageLive.Text = "Live";
         this.tabPageLive.UseVisualStyleBackColor = true;
         // 
         // groupBoxSelectMergeRequest
         // 
         this.groupBoxSelectMergeRequest.Controls.Add(this.textBoxDisplayFilter);
         this.groupBoxSelectMergeRequest.Controls.Add(this.listViewLiveMergeRequests);
         this.groupBoxSelectMergeRequest.Controls.Add(this.checkBoxDisplayFilter);
         this.groupBoxSelectMergeRequest.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSelectMergeRequest.Location = new System.Drawing.Point(3, 3);
         this.groupBoxSelectMergeRequest.Name = "groupBoxSelectMergeRequest";
         this.groupBoxSelectMergeRequest.Size = new System.Drawing.Size(524, 407);
         this.groupBoxSelectMergeRequest.TabIndex = 1;
         this.groupBoxSelectMergeRequest.TabStop = false;
         this.groupBoxSelectMergeRequest.Text = "Select Merge Request";
         // 
         // textBoxDisplayFilter
         // 
         this.textBoxDisplayFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxDisplayFilter.Location = new System.Drawing.Point(60, 17);
         this.textBoxDisplayFilter.Name = "textBoxDisplayFilter";
         this.textBoxDisplayFilter.Size = new System.Drawing.Size(461, 20);
         this.textBoxDisplayFilter.TabIndex = 1;
         this.textBoxDisplayFilter.TextChanged += new System.EventHandler(this.textBoxDisplayFilter_TextChanged);
         this.textBoxDisplayFilter.Leave += new System.EventHandler(this.textBoxDisplayFilter_Leave);
         // 
         // listViewLiveMergeRequests
         // 
         this.listViewLiveMergeRequests.AllowColumnReorder = true;
         this.listViewLiveMergeRequests.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listViewLiveMergeRequests.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderIId,
            this.columnHeaderAuthor,
            this.columnHeaderTitle,
            this.columnHeaderLabels,
            this.columnHeaderSize,
            this.columnHeaderJira,
            this.columnHeaderTotalTime,
            this.columnHeaderResolved,
            this.columnHeaderSourceBranch,
            this.columnHeaderTargetBranch,
            this.columnHeaderRefreshTime,
            this.columnHeaderActivities});
         this.listViewLiveMergeRequests.FullRowSelect = true;
         this.listViewLiveMergeRequests.GridLines = true;
         this.listViewLiveMergeRequests.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
         this.listViewLiveMergeRequests.HideSelection = false;
         this.listViewLiveMergeRequests.Location = new System.Drawing.Point(3, 46);
         this.listViewLiveMergeRequests.MultiSelect = false;
         this.listViewLiveMergeRequests.Name = "listViewLiveMergeRequests";
         this.listViewLiveMergeRequests.OwnerDraw = true;
         this.listViewLiveMergeRequests.Size = new System.Drawing.Size(518, 358);
         this.listViewLiveMergeRequests.TabIndex = 3;
         this.listViewLiveMergeRequests.Tag = "DesignTimeName";
         this.listViewLiveMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewLiveMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewLiveMergeRequests.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewMergeRequests_ItemSelectionChanged);
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
         // columnHeaderRefreshTime
         // 
         this.columnHeaderRefreshTime.Tag = "RefreshTime";
         this.columnHeaderRefreshTime.Text = "Refreshed";
         this.columnHeaderRefreshTime.Width = 90;
         // 
         // columnHeaderActivities
         // 
         this.columnHeaderActivities.Tag = "Activities";
         this.columnHeaderActivities.Text = "Activities";
         this.columnHeaderActivities.Width = 90;
         // 
         // checkBoxDisplayFilter
         // 
         this.checkBoxDisplayFilter.AutoSize = true;
         this.checkBoxDisplayFilter.Location = new System.Drawing.Point(6, 19);
         this.checkBoxDisplayFilter.MinimumSize = new System.Drawing.Size(48, 0);
         this.checkBoxDisplayFilter.Name = "checkBoxDisplayFilter";
         this.checkBoxDisplayFilter.Size = new System.Drawing.Size(48, 17);
         this.checkBoxDisplayFilter.TabIndex = 0;
         this.checkBoxDisplayFilter.Text = "Filter";
         this.checkBoxDisplayFilter.UseVisualStyleBackColor = true;
         this.checkBoxDisplayFilter.CheckedChanged += new System.EventHandler(this.checkBoxDisplayFilter_CheckedChanged);
         // 
         // tabPageSearch
         // 
         this.tabPageSearch.AutoScroll = true;
         this.tabPageSearch.Controls.Add(this.groupBoxSearchMergeRequest);
         this.tabPageSearch.Location = new System.Drawing.Point(4, 5);
         this.tabPageSearch.Name = "tabPageSearch";
         this.tabPageSearch.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSearch.Size = new System.Drawing.Size(530, 413);
         this.tabPageSearch.TabIndex = 1;
         this.tabPageSearch.Text = "Search";
         this.tabPageSearch.UseVisualStyleBackColor = true;
         // 
         // groupBoxSearchMergeRequest
         // 
         this.groupBoxSearchMergeRequest.Controls.Add(this.linkLabelNewSearch);
         this.groupBoxSearchMergeRequest.Controls.Add(this.listViewFoundMergeRequests);
         this.groupBoxSearchMergeRequest.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSearchMergeRequest.Location = new System.Drawing.Point(3, 3);
         this.groupBoxSearchMergeRequest.Name = "groupBoxSearchMergeRequest";
         this.groupBoxSearchMergeRequest.Size = new System.Drawing.Size(524, 407);
         this.groupBoxSearchMergeRequest.TabIndex = 2;
         this.groupBoxSearchMergeRequest.TabStop = false;
         this.groupBoxSearchMergeRequest.Text = "Search Merge Request";
         // 
         // linkLabelNewSearch
         // 
         this.linkLabelNewSearch.AutoSize = true;
         this.linkLabelNewSearch.Location = new System.Drawing.Point(3, 16);
         this.linkLabelNewSearch.Name = "linkLabelNewSearch";
         this.linkLabelNewSearch.Size = new System.Drawing.Size(66, 13);
         this.linkLabelNewSearch.TabIndex = 4;
         this.linkLabelNewSearch.TabStop = true;
         this.linkLabelNewSearch.Text = "New Search";
         this.linkLabelNewSearch.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelNewSearch_LinkClicked);
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
            this.columnHeaderFoundTargetBranch,
            this.columnHeaderFoundActivities});
         this.listViewFoundMergeRequests.FullRowSelect = true;
         this.listViewFoundMergeRequests.GridLines = true;
         this.listViewFoundMergeRequests.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
         this.listViewFoundMergeRequests.HideSelection = false;
         this.listViewFoundMergeRequests.Location = new System.Drawing.Point(3, 32);
         this.listViewFoundMergeRequests.MultiSelect = false;
         this.listViewFoundMergeRequests.Name = "listViewFoundMergeRequests";
         this.listViewFoundMergeRequests.OwnerDraw = true;
         this.listViewFoundMergeRequests.Size = new System.Drawing.Size(518, 372);
         this.listViewFoundMergeRequests.TabIndex = 3;
         this.listViewFoundMergeRequests.Tag = "DesignTimeName";
         this.listViewFoundMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewFoundMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewFoundMergeRequests.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewMergeRequests_ItemSelectionChanged);
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
         // columnHeaderFoundActivities
         // 
         this.columnHeaderFoundActivities.Tag = "Activities";
         this.columnHeaderFoundActivities.Text = "Activities";
         this.columnHeaderFoundActivities.Width = 90;
         // 
         // tabPageRecent
         // 
         this.tabPageRecent.AutoScroll = true;
         this.tabPageRecent.Controls.Add(this.groupBoxRecentMergeRequest);
         this.tabPageRecent.Location = new System.Drawing.Point(4, 5);
         this.tabPageRecent.Name = "tabPageRecent";
         this.tabPageRecent.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageRecent.Size = new System.Drawing.Size(530, 413);
         this.tabPageRecent.TabIndex = 2;
         this.tabPageRecent.Text = "Recent";
         this.tabPageRecent.UseVisualStyleBackColor = true;
         // 
         // groupBoxRecentMergeRequest
         // 
         this.groupBoxRecentMergeRequest.Controls.Add(this.textBoxRecentMergeRequestsHint);
         this.groupBoxRecentMergeRequest.Controls.Add(this.listViewRecentMergeRequests);
         this.groupBoxRecentMergeRequest.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxRecentMergeRequest.Location = new System.Drawing.Point(3, 3);
         this.groupBoxRecentMergeRequest.Name = "groupBoxRecentMergeRequest";
         this.groupBoxRecentMergeRequest.Size = new System.Drawing.Size(524, 407);
         this.groupBoxRecentMergeRequest.TabIndex = 5;
         this.groupBoxRecentMergeRequest.TabStop = false;
         this.groupBoxRecentMergeRequest.Text = "Recent Merge Requests";
         // 
         // textBoxRecentMergeRequestsHint
         // 
         this.textBoxRecentMergeRequestsHint.BorderStyle = System.Windows.Forms.BorderStyle.None;
         this.textBoxRecentMergeRequestsHint.Location = new System.Drawing.Point(3, 19);
         this.textBoxRecentMergeRequestsHint.Multiline = true;
         this.textBoxRecentMergeRequestsHint.Name = "textBoxRecentMergeRequestsHint";
         this.textBoxRecentMergeRequestsHint.ReadOnly = true;
         this.textBoxRecentMergeRequestsHint.Size = new System.Drawing.Size(463, 28);
         this.textBoxRecentMergeRequestsHint.TabIndex = 5;
         this.textBoxRecentMergeRequestsHint.Text = "This list contains a few merge requests which have been recently reviewed by you " +
    "in mrHelper.\r\nThe maximum number of recent merge requests can be configured in S" +
    "ettings.";
         // 
         // listViewRecentMergeRequests
         // 
         this.listViewRecentMergeRequests.AllowColumnReorder = true;
         this.listViewRecentMergeRequests.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listViewRecentMergeRequests.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderRecentIId,
            this.columnHeaderRecentState,
            this.columnHeaderRecentAuthor,
            this.columnHeaderRecentTitle,
            this.columnHeaderRecentLabels,
            this.columnHeaderRecentJira,
            this.columnHeaderRecentSourceBranch,
            this.columnHeaderRecentTargetBranch,
            this.columnHeaderRecentActivities});
         this.listViewRecentMergeRequests.FullRowSelect = true;
         this.listViewRecentMergeRequests.GridLines = true;
         this.listViewRecentMergeRequests.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
         this.listViewRecentMergeRequests.HideSelection = false;
         this.listViewRecentMergeRequests.Location = new System.Drawing.Point(3, 53);
         this.listViewRecentMergeRequests.MultiSelect = false;
         this.listViewRecentMergeRequests.Name = "listViewRecentMergeRequests";
         this.listViewRecentMergeRequests.OwnerDraw = true;
         this.listViewRecentMergeRequests.Size = new System.Drawing.Size(518, 351);
         this.listViewRecentMergeRequests.TabIndex = 4;
         this.listViewRecentMergeRequests.Tag = "DesignTimeName";
         this.listViewRecentMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewRecentMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewRecentMergeRequests.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewMergeRequests_ItemSelectionChanged);
         // 
         // columnHeaderRecentIId
         // 
         this.columnHeaderRecentIId.Tag = "IId";
         this.columnHeaderRecentIId.Text = "IId";
         this.columnHeaderRecentIId.Width = 40;
         // 
         // columnHeaderRecentState
         // 
         this.columnHeaderRecentState.Tag = "State";
         this.columnHeaderRecentState.Text = "State";
         this.columnHeaderRecentState.Width = 80;
         // 
         // columnHeaderRecentAuthor
         // 
         this.columnHeaderRecentAuthor.Tag = "Author";
         this.columnHeaderRecentAuthor.Text = "Author";
         this.columnHeaderRecentAuthor.Width = 110;
         // 
         // columnHeaderRecentTitle
         // 
         this.columnHeaderRecentTitle.Tag = "Title";
         this.columnHeaderRecentTitle.Text = "Title";
         this.columnHeaderRecentTitle.Width = 400;
         // 
         // columnHeaderRecentLabels
         // 
         this.columnHeaderRecentLabels.Tag = "Labels";
         this.columnHeaderRecentLabels.Text = "Labels";
         this.columnHeaderRecentLabels.Width = 180;
         // 
         // columnHeaderRecentJira
         // 
         this.columnHeaderRecentJira.Tag = "Jira";
         this.columnHeaderRecentJira.Text = "Jira";
         this.columnHeaderRecentJira.Width = 80;
         // 
         // columnHeaderRecentSourceBranch
         // 
         this.columnHeaderRecentSourceBranch.Tag = "SourceBranch";
         this.columnHeaderRecentSourceBranch.Text = "Source Branch";
         this.columnHeaderRecentSourceBranch.Width = 100;
         // 
         // columnHeaderRecentTargetBranch
         // 
         this.columnHeaderRecentTargetBranch.Tag = "TargetBranch";
         this.columnHeaderRecentTargetBranch.Text = "Target Branch";
         this.columnHeaderRecentTargetBranch.Width = 100;
         // 
         // columnHeaderRecentActivities
         // 
         this.columnHeaderRecentActivities.Tag = "Activities";
         this.columnHeaderRecentActivities.Text = "Activities";
         this.columnHeaderRecentActivities.Width = 90;
         // 
         // splitContainer2
         // 
         this.splitContainerSecondary.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainerSecondary.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
         this.splitContainerSecondary.Location = new System.Drawing.Point(0, 0);
         this.splitContainerSecondary.Name = "splitContainerSecondary";
         this.splitContainerSecondary.Orientation = System.Windows.Forms.Orientation.Horizontal;
         this.splitContainerSecondary.SplitterMoving += new System.Windows.Forms.SplitterCancelEventHandler(this.splitContainer_SplitterMoving);
         this.splitContainerSecondary.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer_SplitterMoved);
         // 
         // splitContainer2.Panel1
         // 
         this.splitContainerSecondary.Panel1.AutoScroll = true;
         this.splitContainerSecondary.Panel1.Controls.Add(this.groupBoxSelectedMR);
         // 
         // splitContainer2.Panel2
         // 
         this.splitContainerSecondary.Panel2.AutoScroll = true;
         this.splitContainerSecondary.Panel2.Controls.Add(this.groupBoxSelectRevisions);
         this.splitContainerSecondary.Size = new System.Drawing.Size(320, 422);
         this.splitContainerSecondary.SplitterDistance = 130;
         this.splitContainerSecondary.SplitterWidth = 8;
         this.splitContainerSecondary.TabIndex = 7;
         this.splitContainerSecondary.TabStop = false;
         // 
         // groupBoxSelectedMR
         // 
         this.groupBoxSelectedMR.Controls.Add(this.richTextBoxMergeRequestDescription);
         this.groupBoxSelectedMR.Controls.Add(this.linkLabelConnectedTo);
         this.groupBoxSelectedMR.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSelectedMR.Location = new System.Drawing.Point(0, 0);
         this.groupBoxSelectedMR.Name = "groupBoxSelectedMR";
         this.groupBoxSelectedMR.Size = new System.Drawing.Size(320, 130);
         this.groupBoxSelectedMR.TabIndex = 1;
         this.groupBoxSelectedMR.TabStop = false;
         this.groupBoxSelectedMR.Text = "Merge Request";
         // 
         // richTextBoxMergeRequestDescription
         // 
         this.richTextBoxMergeRequestDescription.AutoScroll = true;
         this.richTextBoxMergeRequestDescription.BackColor = System.Drawing.SystemColors.Window;
         this.richTextBoxMergeRequestDescription.BaseStylesheet = null;
         this.richTextBoxMergeRequestDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.richTextBoxMergeRequestDescription.Dock = System.Windows.Forms.DockStyle.Fill;
         this.richTextBoxMergeRequestDescription.Location = new System.Drawing.Point(3, 16);
         this.richTextBoxMergeRequestDescription.Name = "richTextBoxMergeRequestDescription";
         this.richTextBoxMergeRequestDescription.Size = new System.Drawing.Size(314, 93);
         this.richTextBoxMergeRequestDescription.TabIndex = 2;
         this.richTextBoxMergeRequestDescription.TabStop = false;
         this.richTextBoxMergeRequestDescription.Text = null;
         // 
         // linkLabelConnectedTo
         // 
         this.linkLabelConnectedTo.AutoEllipsis = true;
         this.linkLabelConnectedTo.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.linkLabelConnectedTo.Location = new System.Drawing.Point(3, 109);
         this.linkLabelConnectedTo.Name = "linkLabelConnectedTo";
         this.linkLabelConnectedTo.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
         this.linkLabelConnectedTo.Size = new System.Drawing.Size(314, 18);
         this.linkLabelConnectedTo.TabIndex = 5;
         this.linkLabelConnectedTo.TabStop = true;
         this.linkLabelConnectedTo.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";
         // 
         // groupBoxSelectRevisions
         // 
         this.groupBoxSelectRevisions.Controls.Add(this.revisionBrowser);
         this.groupBoxSelectRevisions.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSelectRevisions.Location = new System.Drawing.Point(0, 0);
         this.groupBoxSelectRevisions.Name = "groupBoxSelectRevisions";
         this.groupBoxSelectRevisions.Size = new System.Drawing.Size(320, 284);
         this.groupBoxSelectRevisions.TabIndex = 4;
         this.groupBoxSelectRevisions.TabStop = false;
         this.groupBoxSelectRevisions.Text = "Select revisions for comparison";
         // 
         // revisionBrowser
         // 
         this.revisionBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
         this.revisionBrowser.Location = new System.Drawing.Point(3, 16);
         this.revisionBrowser.Name = "revisionBrowser";
         this.revisionBrowser.Size = new System.Drawing.Size(314, 265);
         this.revisionBrowser.TabIndex = 0;
         this.revisionBrowser.SelectionChanged += new System.EventHandler(this.revisionBrowser_SelectionChanged);
         // 
         // ConnectionPage
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.splitContainerPrimary);
         this.Dock = System.Windows.Forms.DockStyle.Fill;
         this.Name = "ConnectionPage";
         this.Size = new System.Drawing.Size(866, 422);
         this.splitContainerPrimary.Panel1.ResumeLayout(false);
         this.splitContainerPrimary.Panel2.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainerPrimary)).EndInit();
         this.splitContainerPrimary.ResumeLayout(false);
         this.tabControlMode.ResumeLayout(false);
         this.tabPageLive.ResumeLayout(false);
         this.groupBoxSelectMergeRequest.ResumeLayout(false);
         this.groupBoxSelectMergeRequest.PerformLayout();
         this.tabPageSearch.ResumeLayout(false);
         this.groupBoxSearchMergeRequest.ResumeLayout(false);
         this.groupBoxSearchMergeRequest.PerformLayout();
         this.tabPageRecent.ResumeLayout(false);
         this.groupBoxRecentMergeRequest.ResumeLayout(false);
         this.groupBoxRecentMergeRequest.PerformLayout();
         this.splitContainerSecondary.Panel1.ResumeLayout(false);
         this.splitContainerSecondary.Panel2.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainerSecondary)).EndInit();
         this.splitContainerSecondary.ResumeLayout(false);
         this.groupBoxSelectedMR.ResumeLayout(false);
         this.groupBoxSelectRevisions.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.SplitContainer splitContainerPrimary;
      private PlainTabControl tabControlMode;
      private System.Windows.Forms.TabPage tabPageLive;
      private System.Windows.Forms.GroupBox groupBoxSelectMergeRequest;
      private CommonControls.Controls.DelayedTextBox textBoxDisplayFilter;
      private App.Controls.MergeRequestListView listViewLiveMergeRequests;
      private System.Windows.Forms.ColumnHeader columnHeaderIId;
      private System.Windows.Forms.ColumnHeader columnHeaderAuthor;
      private System.Windows.Forms.ColumnHeader columnHeaderTitle;
      private System.Windows.Forms.ColumnHeader columnHeaderLabels;
      private System.Windows.Forms.ColumnHeader columnHeaderSize;
      private System.Windows.Forms.ColumnHeader columnHeaderJira;
      private System.Windows.Forms.ColumnHeader columnHeaderTotalTime;
      private System.Windows.Forms.ColumnHeader columnHeaderResolved;
      private System.Windows.Forms.ColumnHeader columnHeaderSourceBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderTargetBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderRefreshTime;
      private System.Windows.Forms.ColumnHeader columnHeaderActivities;
      private System.Windows.Forms.CheckBox checkBoxDisplayFilter;
      private System.Windows.Forms.TabPage tabPageSearch;
      private System.Windows.Forms.GroupBox groupBoxSearchMergeRequest;
      private App.Controls.MergeRequestListView listViewFoundMergeRequests;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundIId;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundState;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundAuthor;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundTitle;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundLabels;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundJira;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundSourceBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundTargetBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundActivities;
      private System.Windows.Forms.TabPage tabPageRecent;
      private System.Windows.Forms.GroupBox groupBoxRecentMergeRequest;
      private System.Windows.Forms.TextBox textBoxRecentMergeRequestsHint;
      private App.Controls.MergeRequestListView listViewRecentMergeRequests;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentIId;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentState;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentAuthor;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentTitle;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentLabels;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentJira;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentSourceBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentTargetBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentActivities;
      private System.Windows.Forms.SplitContainer splitContainerSecondary;
      private System.Windows.Forms.GroupBox groupBoxSelectedMR;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel richTextBoxMergeRequestDescription;
      private System.Windows.Forms.GroupBox groupBoxSelectRevisions;
      private App.Controls.RevisionBrowser revisionBrowser;
      private CommonControls.Controls.LinkLabelEx linkLabelConnectedTo;
      private System.Windows.Forms.LinkLabel linkLabelNewSearch;
   }
}
