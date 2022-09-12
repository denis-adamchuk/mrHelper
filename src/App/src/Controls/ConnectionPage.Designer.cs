
using System.Windows.Forms;

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

         foreach (EDataCacheType mode in new EDataCacheType[]
            {
               EDataCacheType.Live, EDataCacheType.Recent, EDataCacheType.Search
            })
         {
            var listView = getListView(mode);

            // Let ListView drop ContextMenuStrip
            listView.AssignContextMenu(null);

            // Let ListView unsubscribe from other objects
            listView.SetDataCache(null);
            listView.SetDiffStatisticProvider(null);
            listView.SetColorScheme(null);
            listView.SetFilter(null);
            listView.SetExpressionResolver(null);
            listView.SetAvatarImageCache(null);
         }

         // Clear RevisionBrowser and let it drop ContextMenuStrip
         revisionSplitContainerSite.ClearData();
         getRevisionBrowser().AssignContextMenu(null);

         _colorScheme.Changed -= onColorSchemeChanged;

         // To avoid dangling LinkLabelEx in a tooltip that we received from creator
         _toolTip.SetToolTip(linkLabelConnectedTo, null);

         Program.Settings.MainWindowLayoutChanged -= onMainWindowLayoutChanged;
         Program.Settings.WordWrapLongRowsChanged -= onWrapLongRowsChanged;
         Program.Settings.ShowHiddenMergeRequestIdsChanged -= onShowHiddenMergeRequestIdsChanged;

         finalizeWork();

         disposeGitHelpers();
         disposeLocalGitRepositoryFactory();
         disposeLiveDataCacheDependencies();
         disposeSearchDataCacheDependencies();
         disposeRecentDataCacheDependencies();

         resetLostConnectionInfo();

         _liveDataCache.Dispose();
         _searchDataCache.Dispose();
         _recentDataCache.Dispose();

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
         this.tabControlMode = new mrHelper.App.Controls.PlainTabControl();
         this.tabPageLive = new System.Windows.Forms.TabPage();
         this.groupBoxSelectMergeRequest = new System.Windows.Forms.GroupBox();
         this.textBoxDisplayFilter = new mrHelper.App.Controls.FilterTextBox();
         this.listViewLiveMergeRequests = new mrHelper.App.Controls.MergeRequestListView();
         this.columnHeaderIId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderColor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
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
         this.columnHeaderProject = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.tabPageSearch = new System.Windows.Forms.TabPage();
         this.groupBoxSearchMergeRequest = new System.Windows.Forms.GroupBox();
         this.linkLabelNewSearch = new System.Windows.Forms.LinkLabel();
         this.listViewFoundMergeRequests = new mrHelper.App.Controls.MergeRequestListView();
         this.columnHeaderFoundIId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundColor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundAuthor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundLabels = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundJira = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundSourceBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundTargetBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundActivities = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderFoundProject = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.tabPageRecent = new System.Windows.Forms.TabPage();
         this.groupBoxRecentMergeRequest = new System.Windows.Forms.GroupBox();
         this.textBoxDisplayFilterRecent = new mrHelper.App.Controls.FilterTextBox();
         this.textBoxRecentMergeRequestsHint = new System.Windows.Forms.TextBox();
         this.listViewRecentMergeRequests = new mrHelper.App.Controls.MergeRequestListView();
         this.columnHeaderRecentIId = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentColor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentState = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentAuthor = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentLabels = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentJira = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentSourceBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentTargetBranch = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentActivities = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderRecentProject = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.splitContainerSecondary = new System.Windows.Forms.SplitContainer();
         this.groupBoxSelectedMR = new System.Windows.Forms.GroupBox();
         this.descriptionSplitContainerSite = new mrHelper.App.Controls.DescriptionSplitContainerSite();
         this.linkLabelConnectedTo = new mrHelper.CommonControls.Controls.LinkLabelEx();
         this.groupBoxSelectRevisions = new System.Windows.Forms.GroupBox();
         this.revisionSplitContainerSite = new mrHelper.App.Controls.RevisionSplitContainerSite();
         this.comboBoxFilter = new FilterStateComboBox();
         this.comboBoxFilterRecent = new FilterStateComboBox();
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
         // splitContainerPrimary
         // 
         this.splitContainerPrimary.BackColor = System.Drawing.Color.LightGray;
         this.splitContainerPrimary.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainerPrimary.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
         this.splitContainerPrimary.Location = new System.Drawing.Point(0, 0);
         this.splitContainerPrimary.Name = "splitContainerPrimary";
         // 
         // splitContainerPrimary.Panel1
         // 
         this.splitContainerPrimary.Panel1.AutoScroll = true;
         this.splitContainerPrimary.Panel1.BackColor = System.Drawing.SystemColors.Window;
         this.splitContainerPrimary.Panel1.Controls.Add(this.tabControlMode);
         // 
         // splitContainerPrimary.Panel2
         // 
         this.splitContainerPrimary.Panel2.AutoScroll = true;
         this.splitContainerPrimary.Panel2.BackColor = System.Drawing.SystemColors.Window;
         this.splitContainerPrimary.Panel2.Controls.Add(this.splitContainerSecondary);
         this.splitContainerPrimary.Size = new System.Drawing.Size(1185, 631);
         this.splitContainerPrimary.SplitterDistance = 853;
         this.splitContainerPrimary.TabIndex = 5;
         this.splitContainerPrimary.TabStop = false;
         this.splitContainerPrimary.SplitterMoving += new System.Windows.Forms.SplitterCancelEventHandler(this.splitContainer_SplitterMoving);
         this.splitContainerPrimary.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer_SplitterMoved);
         // 
         // tabControlMode
         // 
         this.tabControlMode.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
         this.tabControlMode.Controls.Add(this.tabPageLive);
         this.tabControlMode.Controls.Add(this.tabPageSearch);
         this.tabControlMode.Controls.Add(this.tabPageRecent);
         this.tabControlMode.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tabControlMode.ItemSize = new System.Drawing.Size(0, 1);
         this.tabControlMode.Location = new System.Drawing.Point(0, 0);
         this.tabControlMode.Name = "tabControlMode";
         this.tabControlMode.SelectedIndex = 0;
         this.tabControlMode.Size = new System.Drawing.Size(853, 631);
         this.tabControlMode.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
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
         this.tabPageLive.Size = new System.Drawing.Size(845, 622);
         this.tabPageLive.TabIndex = 0;
         this.tabPageLive.Text = "Live";
         this.tabPageLive.UseVisualStyleBackColor = true;
         // 
         // groupBoxSelectMergeRequest
         // 
         this.groupBoxSelectMergeRequest.Controls.Add(this.comboBoxFilter);
         this.groupBoxSelectMergeRequest.Controls.Add(this.textBoxDisplayFilter);
         this.groupBoxSelectMergeRequest.Controls.Add(this.listViewLiveMergeRequests);
         this.groupBoxSelectMergeRequest.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSelectMergeRequest.Location = new System.Drawing.Point(3, 3);
         this.groupBoxSelectMergeRequest.Name = "groupBoxSelectMergeRequest";
         this.groupBoxSelectMergeRequest.Size = new System.Drawing.Size(839, 616);
         this.groupBoxSelectMergeRequest.TabIndex = 1;
         this.groupBoxSelectMergeRequest.TabStop = false;
         this.groupBoxSelectMergeRequest.Text = "Select Merge Request";
         // 
         // textBoxDisplayFilter
         // 
         this.textBoxDisplayFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxDisplayFilter.Location = new System.Drawing.Point(160, 17);
         this.textBoxDisplayFilter.Multiline = false;
         this.textBoxDisplayFilter.Name = "textBoxDisplayFilter";
         this.textBoxDisplayFilter.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
         this.textBoxDisplayFilter.Size = new System.Drawing.Size(676, 20);
         this.textBoxDisplayFilter.TabIndex = 1;
         this.textBoxDisplayFilter.Text = "";
         this.textBoxDisplayFilter.TextChanged += new System.EventHandler(this.textBoxDisplayFilter_TextChanged);
         // 
         // listViewLiveMergeRequests
         // 
         this.listViewLiveMergeRequests.AllowColumnReorder = true;
         this.listViewLiveMergeRequests.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listViewLiveMergeRequests.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderIId,
            this.columnHeaderColor,
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
            this.columnHeaderActivities,
            this.columnHeaderProject});
         this.listViewLiveMergeRequests.FullRowSelect = true;
         this.listViewLiveMergeRequests.GridLines = false;
         this.listViewLiveMergeRequests.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Clickable;
         this.listViewLiveMergeRequests.HideSelection = false;
         this.listViewLiveMergeRequests.Location = new System.Drawing.Point(3, 46);
         this.listViewLiveMergeRequests.MultiSelect = false;
         this.listViewLiveMergeRequests.Name = "listViewLiveMergeRequests";
         this.listViewLiveMergeRequests.OwnerDraw = true;
         this.listViewLiveMergeRequests.Size = new System.Drawing.Size(833, 567);
         this.listViewLiveMergeRequests.TabIndex = 3;
         this.listViewLiveMergeRequests.Tag = "DesignTimeName";
         this.listViewLiveMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewLiveMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewLiveMergeRequests.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewMergeRequests_ItemSelectionChanged);
         // 
         // columnHeaderIId
         // 
         this.columnHeaderIId.Tag = mrHelper.App.Controls.ColumnType.IId;
         this.columnHeaderIId.Text = "IId";
         this.columnHeaderIId.Width = 40;
         // 
         // columnHeaderColor
         // 
         this.columnHeaderColor.Tag = mrHelper.App.Controls.ColumnType.Color;
         this.columnHeaderColor.Text = "Color";
         this.columnHeaderColor.Width = MergeRequestListView.DefaultColorColumnWidth;
         // 
         // columnHeaderAuthor
         // 
         this.columnHeaderAuthor.Tag = mrHelper.App.Controls.ColumnType.Author;
         this.columnHeaderAuthor.Text = "Author";
         this.columnHeaderAuthor.Width = 110;
         // 
         // columnHeaderTitle
         // 
         this.columnHeaderTitle.Tag = mrHelper.App.Controls.ColumnType.Title;
         this.columnHeaderTitle.Text = "Title";
         this.columnHeaderTitle.Width = 400;
         // 
         // columnHeaderLabels
         // 
         this.columnHeaderLabels.Tag = mrHelper.App.Controls.ColumnType.Labels;
         this.columnHeaderLabels.Text = "Labels";
         this.columnHeaderLabels.Width = 180;
         // 
         // columnHeaderSize
         // 
         this.columnHeaderSize.Tag = mrHelper.App.Controls.ColumnType.Size;
         this.columnHeaderSize.Text = "Size";
         this.columnHeaderSize.Width = 100;
         // 
         // columnHeaderJira
         // 
         this.columnHeaderJira.Tag = mrHelper.App.Controls.ColumnType.Jira;
         this.columnHeaderJira.Text = "Jira";
         this.columnHeaderJira.Width = 80;
         // 
         // columnHeaderTotalTime
         // 
         this.columnHeaderTotalTime.Tag = mrHelper.App.Controls.ColumnType.TotalTime;
         this.columnHeaderTotalTime.Text = "Total Time";
         this.columnHeaderTotalTime.Width = 70;
         // 
         // columnHeaderResolved
         // 
         this.columnHeaderResolved.Tag = mrHelper.App.Controls.ColumnType.Resolved;
         this.columnHeaderResolved.Text = "Resolved";
         this.columnHeaderResolved.Width = 65;
         // 
         // columnHeaderSourceBranch
         // 
         this.columnHeaderSourceBranch.Tag = mrHelper.App.Controls.ColumnType.SourceBranch;
         this.columnHeaderSourceBranch.Text = "Source Branch";
         this.columnHeaderSourceBranch.Width = 100;
         // 
         // columnHeaderTargetBranch
         // 
         this.columnHeaderTargetBranch.Tag = mrHelper.App.Controls.ColumnType.TargetBranch;
         this.columnHeaderTargetBranch.Text = "Target Branch";
         this.columnHeaderTargetBranch.Width = 100;
         // 
         // columnHeaderRefreshTime
         // 
         this.columnHeaderRefreshTime.Tag = mrHelper.App.Controls.ColumnType.RefreshTime;
         this.columnHeaderRefreshTime.Text = "Refreshed";
         this.columnHeaderRefreshTime.Width = 90;
         // 
         // columnHeaderActivities
         // 
         this.columnHeaderActivities.Tag = mrHelper.App.Controls.ColumnType.Activities;
         this.columnHeaderActivities.Text = "Activities";
         this.columnHeaderActivities.Width = 90;
         // 
         // columnHeaderProject
         // 
         this.columnHeaderProject.Tag = mrHelper.App.Controls.ColumnType.Project;
         this.columnHeaderProject.Text = "Project";
         this.columnHeaderProject.Width = 130;
         // 
         // tabPageSearch
         // 
         this.tabPageSearch.AutoScroll = true;
         this.tabPageSearch.Controls.Add(this.groupBoxSearchMergeRequest);
         this.tabPageSearch.Location = new System.Drawing.Point(4, 5);
         this.tabPageSearch.Name = "tabPageSearch";
         this.tabPageSearch.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageSearch.Size = new System.Drawing.Size(845, 622);
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
         this.groupBoxSearchMergeRequest.Size = new System.Drawing.Size(839, 616);
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
            this.columnHeaderFoundColor,
            this.columnHeaderFoundState,
            this.columnHeaderFoundAuthor,
            this.columnHeaderFoundTitle,
            this.columnHeaderFoundLabels,
            this.columnHeaderFoundJira,
            this.columnHeaderFoundSourceBranch,
            this.columnHeaderFoundTargetBranch,
            this.columnHeaderFoundActivities,
            this.columnHeaderFoundProject});
         this.listViewFoundMergeRequests.FullRowSelect = true;
         this.listViewFoundMergeRequests.GridLines = false;
         this.listViewFoundMergeRequests.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Clickable;
         this.listViewFoundMergeRequests.HideSelection = false;
         this.listViewFoundMergeRequests.Location = new System.Drawing.Point(3, 32);
         this.listViewFoundMergeRequests.MultiSelect = false;
         this.listViewFoundMergeRequests.Name = "listViewFoundMergeRequests";
         this.listViewFoundMergeRequests.OwnerDraw = true;
         this.listViewFoundMergeRequests.Size = new System.Drawing.Size(833, 581);
         this.listViewFoundMergeRequests.TabIndex = 3;
         this.listViewFoundMergeRequests.Tag = "DesignTimeName";
         this.listViewFoundMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewFoundMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewFoundMergeRequests.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewMergeRequests_ItemSelectionChanged);
         // 
         // columnHeaderFoundIId
         // 
         this.columnHeaderFoundIId.Tag = mrHelper.App.Controls.ColumnType.IId;
         this.columnHeaderFoundIId.Text = "IId";
         this.columnHeaderFoundIId.Width = 40;
         // 
         // columnHeaderFoundColor
         // 
         this.columnHeaderFoundColor.Tag = mrHelper.App.Controls.ColumnType.Color;
         this.columnHeaderFoundColor.Text = "Color";
         this.columnHeaderFoundColor.Width = MergeRequestListView.DefaultColorColumnWidth;
         // 
         // columnHeaderFoundState
         // 
         this.columnHeaderFoundState.Tag = mrHelper.App.Controls.ColumnType.State;
         this.columnHeaderFoundState.Text = "State";
         this.columnHeaderFoundState.Width = 80;
         // 
         // columnHeaderFoundAuthor
         // 
         this.columnHeaderFoundAuthor.Tag = mrHelper.App.Controls.ColumnType.Author;
         this.columnHeaderFoundAuthor.Text = "Author";
         this.columnHeaderFoundAuthor.Width = 110;
         // 
         // columnHeaderFoundTitle
         // 
         this.columnHeaderFoundTitle.Tag = mrHelper.App.Controls.ColumnType.Title;
         this.columnHeaderFoundTitle.Text = "Title";
         this.columnHeaderFoundTitle.Width = 400;
         // 
         // columnHeaderFoundLabels
         // 
         this.columnHeaderFoundLabels.Tag = mrHelper.App.Controls.ColumnType.Labels;
         this.columnHeaderFoundLabels.Text = "Labels";
         this.columnHeaderFoundLabels.Width = 180;
         // 
         // columnHeaderFoundJira
         // 
         this.columnHeaderFoundJira.Tag = mrHelper.App.Controls.ColumnType.Jira;
         this.columnHeaderFoundJira.Text = "Jira";
         this.columnHeaderFoundJira.Width = 80;
         // 
         // columnHeaderFoundSourceBranch
         // 
         this.columnHeaderFoundSourceBranch.Tag = mrHelper.App.Controls.ColumnType.SourceBranch;
         this.columnHeaderFoundSourceBranch.Text = "Source Branch";
         this.columnHeaderFoundSourceBranch.Width = 100;
         // 
         // columnHeaderFoundTargetBranch
         // 
         this.columnHeaderFoundTargetBranch.Tag = mrHelper.App.Controls.ColumnType.TargetBranch;
         this.columnHeaderFoundTargetBranch.Text = "Target Branch";
         this.columnHeaderFoundTargetBranch.Width = 100;
         // 
         // columnHeaderFoundActivities
         // 
         this.columnHeaderFoundActivities.Tag = mrHelper.App.Controls.ColumnType.Activities;
         this.columnHeaderFoundActivities.Text = "Activities";
         this.columnHeaderFoundActivities.Width = 90;
         // 
         // columnHeaderFoundProject
         // 
         this.columnHeaderFoundProject.Tag = mrHelper.App.Controls.ColumnType.Project;
         this.columnHeaderFoundProject.Text = "Project";
         this.columnHeaderFoundProject.Width = 130;
         // 
         // tabPageRecent
         // 
         this.tabPageRecent.AutoScroll = true;
         this.tabPageRecent.Controls.Add(this.groupBoxRecentMergeRequest);
         this.tabPageRecent.Location = new System.Drawing.Point(4, 5);
         this.tabPageRecent.Name = "tabPageRecent";
         this.tabPageRecent.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageRecent.Size = new System.Drawing.Size(845, 622);
         this.tabPageRecent.TabIndex = 2;
         this.tabPageRecent.Text = "Recent";
         this.tabPageRecent.UseVisualStyleBackColor = true;
         // 
         // groupBoxRecentMergeRequest
         // 
         this.groupBoxRecentMergeRequest.Controls.Add(this.comboBoxFilterRecent);
         this.groupBoxRecentMergeRequest.Controls.Add(this.textBoxDisplayFilterRecent);
         this.groupBoxRecentMergeRequest.Controls.Add(this.textBoxRecentMergeRequestsHint);
         this.groupBoxRecentMergeRequest.Controls.Add(this.listViewRecentMergeRequests);
         this.groupBoxRecentMergeRequest.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxRecentMergeRequest.Location = new System.Drawing.Point(3, 3);
         this.groupBoxRecentMergeRequest.Name = "groupBoxRecentMergeRequest";
         this.groupBoxRecentMergeRequest.Size = new System.Drawing.Size(839, 616);
         this.groupBoxRecentMergeRequest.TabIndex = 5;
         this.groupBoxRecentMergeRequest.TabStop = false;
         this.groupBoxRecentMergeRequest.Text = "Recent Merge Requests";
         // 
         // textBoxDisplayFilterRecent
         // 
         this.textBoxDisplayFilterRecent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxDisplayFilterRecent.Location = new System.Drawing.Point(160, 17);
         this.textBoxDisplayFilterRecent.Multiline = false;
         this.textBoxDisplayFilterRecent.Name = "textBoxDisplayFilterRecent";
         this.textBoxDisplayFilterRecent.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
         this.textBoxDisplayFilterRecent.Size = new System.Drawing.Size(676, 20);
         this.textBoxDisplayFilterRecent.TabIndex = 1;
         this.textBoxDisplayFilterRecent.Text = "";
         this.textBoxDisplayFilterRecent.TextChanged += new System.EventHandler(this.textBoxDisplayFilterRecent_TextChanged);
         // 
         // comboBoxFilterRecent
         // 
         this.comboBoxFilterRecent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxFilterRecent.FormattingEnabled = true;
         this.comboBoxFilterRecent.Location = new System.Drawing.Point(3, 17);
         this.comboBoxFilterRecent.Name = "comboBoxFilterRecent";
         this.comboBoxFilterRecent.Size = new System.Drawing.Size(150, 21);
         this.comboBoxFilterRecent.TabIndex = 4;
         this.comboBoxFilterRecent.SelectionChangeCommitted += new System.EventHandler(this.comboBoxFilterRecent_SelectionChangeCommitted);
         // 
         // textBoxRecentMergeRequestsHint
         // 
         this.textBoxRecentMergeRequestsHint.BorderStyle = System.Windows.Forms.BorderStyle.None;
         this.textBoxRecentMergeRequestsHint.Location = new System.Drawing.Point(3, 39);
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
            this.columnHeaderRecentColor,
            this.columnHeaderRecentState,
            this.columnHeaderRecentAuthor,
            this.columnHeaderRecentTitle,
            this.columnHeaderRecentLabels,
            this.columnHeaderRecentJira,
            this.columnHeaderRecentSourceBranch,
            this.columnHeaderRecentTargetBranch,
            this.columnHeaderRecentActivities,
            this.columnHeaderRecentProject});
         this.listViewRecentMergeRequests.FullRowSelect = true;
         this.listViewRecentMergeRequests.GridLines = false;
         this.listViewRecentMergeRequests.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Clickable;
         this.listViewRecentMergeRequests.HideSelection = false;
         this.listViewRecentMergeRequests.Location = new System.Drawing.Point(3, 73);
         this.listViewRecentMergeRequests.MultiSelect = false;
         this.listViewRecentMergeRequests.Name = "listViewRecentMergeRequests";
         this.listViewRecentMergeRequests.OwnerDraw = true;
         this.listViewRecentMergeRequests.Size = new System.Drawing.Size(833, 540);
         this.listViewRecentMergeRequests.TabIndex = 4;
         this.listViewRecentMergeRequests.Tag = "DesignTimeName";
         this.listViewRecentMergeRequests.UseCompatibleStateImageBehavior = false;
         this.listViewRecentMergeRequests.View = System.Windows.Forms.View.Details;
         this.listViewRecentMergeRequests.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewMergeRequests_ItemSelectionChanged);
         // 
         // columnHeaderRecentIId
         // 
         this.columnHeaderRecentIId.Tag = mrHelper.App.Controls.ColumnType.IId;
         this.columnHeaderRecentIId.Text = "IId";
         this.columnHeaderRecentIId.Width = 40;
         // 
         // columnHeaderRecentColor
         // 
         this.columnHeaderRecentColor.Tag = mrHelper.App.Controls.ColumnType.Color;
         this.columnHeaderRecentColor.Text = "Color";
         this.columnHeaderRecentColor.Width = MergeRequestListView.DefaultColorColumnWidth;
         // 
         // columnHeaderRecentState
         // 
         this.columnHeaderRecentState.Tag = mrHelper.App.Controls.ColumnType.State;
         this.columnHeaderRecentState.Text = "State";
         this.columnHeaderRecentState.Width = 80;
         // 
         // columnHeaderRecentAuthor
         // 
         this.columnHeaderRecentAuthor.Tag = mrHelper.App.Controls.ColumnType.Author;
         this.columnHeaderRecentAuthor.Text = "Author";
         this.columnHeaderRecentAuthor.Width = 110;
         // 
         // columnHeaderRecentTitle
         // 
         this.columnHeaderRecentTitle.Tag = mrHelper.App.Controls.ColumnType.Title;
         this.columnHeaderRecentTitle.Text = "Title";
         this.columnHeaderRecentTitle.Width = 400;
         // 
         // columnHeaderRecentLabels
         // 
         this.columnHeaderRecentLabels.Tag = mrHelper.App.Controls.ColumnType.Labels;
         this.columnHeaderRecentLabels.Text = "Labels";
         this.columnHeaderRecentLabels.Width = 180;
         // 
         // columnHeaderRecentJira
         // 
         this.columnHeaderRecentJira.Tag = mrHelper.App.Controls.ColumnType.Jira;
         this.columnHeaderRecentJira.Text = "Jira";
         this.columnHeaderRecentJira.Width = 80;
         // 
         // columnHeaderRecentSourceBranch
         // 
         this.columnHeaderRecentSourceBranch.Tag = mrHelper.App.Controls.ColumnType.SourceBranch;
         this.columnHeaderRecentSourceBranch.Text = "Source Branch";
         this.columnHeaderRecentSourceBranch.Width = 100;
         // 
         // columnHeaderRecentTargetBranch
         // 
         this.columnHeaderRecentTargetBranch.Tag = mrHelper.App.Controls.ColumnType.TargetBranch;
         this.columnHeaderRecentTargetBranch.Text = "Target Branch";
         this.columnHeaderRecentTargetBranch.Width = 100;
         // 
         // columnHeaderRecentActivities
         // 
         this.columnHeaderRecentActivities.Tag = mrHelper.App.Controls.ColumnType.Activities;
         this.columnHeaderRecentActivities.Text = "Activities";
         this.columnHeaderRecentActivities.Width = 90;
         // 
         // columnHeaderRecentProject
         // 
         this.columnHeaderRecentProject.Tag = mrHelper.App.Controls.ColumnType.Project;
         this.columnHeaderRecentProject.Text = "Project";
         this.columnHeaderRecentProject.Width = 130;
         // 
         // splitContainerSecondary
         // 
         this.splitContainerSecondary.BackColor = System.Drawing.Color.LightGray;
         this.splitContainerSecondary.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainerSecondary.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
         this.splitContainerSecondary.Location = new System.Drawing.Point(0, 0);
         this.splitContainerSecondary.Name = "splitContainerSecondary";
         this.splitContainerSecondary.Orientation = System.Windows.Forms.Orientation.Horizontal;
         // 
         // splitContainerSecondary.Panel1
         // 
         this.splitContainerSecondary.Panel1.AutoScroll = true;
         this.splitContainerSecondary.Panel1.BackColor = System.Drawing.SystemColors.Window;
         this.splitContainerSecondary.Panel1.Controls.Add(this.groupBoxSelectedMR);
         // 
         // splitContainerSecondary.Panel2
         // 
         this.splitContainerSecondary.Panel2.AutoScroll = true;
         this.splitContainerSecondary.Panel2.BackColor = System.Drawing.SystemColors.Window;
         this.splitContainerSecondary.Panel2.Controls.Add(this.groupBoxSelectRevisions);
         this.splitContainerSecondary.Size = new System.Drawing.Size(328, 631);
         this.splitContainerSecondary.SplitterDistance = 335;
         this.splitContainerSecondary.TabIndex = 7;
         this.splitContainerSecondary.TabStop = false;
         this.splitContainerSecondary.SplitterMoving += new System.Windows.Forms.SplitterCancelEventHandler(this.splitContainer_SplitterMoving);
         this.splitContainerSecondary.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer_SplitterMoved);
         // 
         // groupBoxSelectedMR
         // 
         this.groupBoxSelectedMR.Controls.Add(this.descriptionSplitContainerSite);
         this.groupBoxSelectedMR.Controls.Add(this.linkLabelConnectedTo);
         this.groupBoxSelectedMR.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSelectedMR.Location = new System.Drawing.Point(0, 0);
         this.groupBoxSelectedMR.Name = "groupBoxSelectedMR";
         this.groupBoxSelectedMR.Size = new System.Drawing.Size(328, 335);
         this.groupBoxSelectedMR.TabIndex = 1;
         this.groupBoxSelectedMR.TabStop = false;
         this.groupBoxSelectedMR.Text = "Merge Request";
         // 
         // descriptionSplitContainerSite
         // 
         this.descriptionSplitContainerSite.Dock = System.Windows.Forms.DockStyle.Fill;
         this.descriptionSplitContainerSite.Location = new System.Drawing.Point(3, 16);
         this.descriptionSplitContainerSite.Name = "descriptionSplitContainerSite";
         this.descriptionSplitContainerSite.Size = new System.Drawing.Size(322, 293);
         this.descriptionSplitContainerSite.TabIndex = 6;
         // 
         // linkLabelConnectedTo
         // 
         this.linkLabelConnectedTo.AutoEllipsis = true;
         this.linkLabelConnectedTo.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.linkLabelConnectedTo.Location = new System.Drawing.Point(3, 309);
         this.linkLabelConnectedTo.Name = "linkLabelConnectedTo";
         this.linkLabelConnectedTo.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
         this.linkLabelConnectedTo.Size = new System.Drawing.Size(322, 23);
         this.linkLabelConnectedTo.TabIndex = 5;
         this.linkLabelConnectedTo.TabStop = true;
         this.linkLabelConnectedTo.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";
         this.linkLabelConnectedTo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         // 
         // groupBoxSelectRevisions
         // 
         this.groupBoxSelectRevisions.Controls.Add(this.revisionSplitContainerSite);
         this.groupBoxSelectRevisions.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxSelectRevisions.Location = new System.Drawing.Point(0, 0);
         this.groupBoxSelectRevisions.Name = "groupBoxSelectRevisions";
         this.groupBoxSelectRevisions.Size = new System.Drawing.Size(328, 292);
         this.groupBoxSelectRevisions.TabIndex = 4;
         this.groupBoxSelectRevisions.TabStop = false;
         this.groupBoxSelectRevisions.Text = "Select revisions for comparison";
         // 
         // revisionSplitContainerSite
         // 
         this.revisionSplitContainerSite.Dock = System.Windows.Forms.DockStyle.Fill;
         this.revisionSplitContainerSite.Location = new System.Drawing.Point(3, 16);
         this.revisionSplitContainerSite.Name = "revisionSplitContainerSite";
         this.revisionSplitContainerSite.Size = new System.Drawing.Size(322, 273);
         this.revisionSplitContainerSite.TabIndex = 0;
         // 
         // comboBoxFilter
         // 
         this.comboBoxFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxFilter.FormattingEnabled = true;
         this.comboBoxFilter.Location = new System.Drawing.Point(3, 17);
         this.comboBoxFilter.Name = "comboBoxFilter";
         this.comboBoxFilter.Size = new System.Drawing.Size(150, 21);
         this.comboBoxFilter.TabIndex = 4;
         this.comboBoxFilter.SelectionChangeCommitted += new System.EventHandler(this.comboBoxFilter_SelectionChangeCommitted);
         // 
         // ConnectionPage
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.splitContainerPrimary);
         this.Name = "ConnectionPage";
         this.Size = new System.Drawing.Size(1185, 631);
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
      private mrHelper.App.Controls.FilterTextBox textBoxDisplayFilterRecent;
      private App.Controls.MergeRequestListView listViewLiveMergeRequests;
      private System.Windows.Forms.ColumnHeader columnHeaderIId;
      private System.Windows.Forms.ColumnHeader columnHeaderColor;
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
      private System.Windows.Forms.ColumnHeader columnHeaderProject;
      private System.Windows.Forms.TabPage tabPageSearch;
      private System.Windows.Forms.GroupBox groupBoxSearchMergeRequest;
      private App.Controls.MergeRequestListView listViewFoundMergeRequests;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundIId;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundColor;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundState;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundAuthor;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundTitle;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundLabels;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundJira;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundSourceBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundTargetBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundActivities;
      private System.Windows.Forms.ColumnHeader columnHeaderFoundProject;
      private System.Windows.Forms.TabPage tabPageRecent;
      private System.Windows.Forms.GroupBox groupBoxRecentMergeRequest;
      private System.Windows.Forms.TextBox textBoxRecentMergeRequestsHint;
      private App.Controls.MergeRequestListView listViewRecentMergeRequests;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentIId;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentColor;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentState;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentAuthor;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentTitle;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentLabels;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentJira;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentSourceBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentTargetBranch;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentActivities;
      private System.Windows.Forms.ColumnHeader columnHeaderRecentProject;
      private System.Windows.Forms.SplitContainer splitContainerSecondary;
      private System.Windows.Forms.GroupBox groupBoxSelectedMR;
      private System.Windows.Forms.GroupBox groupBoxSelectRevisions;
      private CommonControls.Controls.LinkLabelEx linkLabelConnectedTo;
      private System.Windows.Forms.LinkLabel linkLabelNewSearch;
      private DescriptionSplitContainerSite descriptionSplitContainerSite;
      private RevisionSplitContainerSite revisionSplitContainerSite;
      private mrHelper.App.Controls.FilterTextBox textBoxDisplayFilter;
      private FilterStateComboBox comboBoxFilter;
      private FilterStateComboBox comboBoxFilterRecent;
   }
}
