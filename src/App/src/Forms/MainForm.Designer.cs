using System.Windows.Forms;
using mrHelper.App.Controls;
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

         _colorScheme.Changed -= onColorSchemeChanged;

         foreach (ToolStrip toolStrip in new ToolStrip[] { toolStripHosts, toolStripActions, toolStripCustomActions })
         {
            removeToolbarButtons(toolStrip); // actually unneeded
            toolStrip.Dispose();
         }
         toolTip.Dispose();

         disposeAllConnectionPages();

         Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
         Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

         unsubscribeFromApplicationUpdates();
         _applicationUpdateChecker.Dispose();

         stopNewVersionReminderTimer();
         _newVersionReminderTimer?.Dispose();

         stopConnectionLossBlinkingTimer();
         _connectionLossBlinkingTimer?.Dispose();

         _timeTrackingTimer?.Stop();
         _timeTrackingTimer?.Dispose();

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
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.openFromClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.restoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
         this.tabControlHost = new mrHelper.App.Controls.PlainTabControl();
         this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
         this.statusStrip1 = new StatusStripEx();
         this.labelConnectionStatus = new System.Windows.Forms.ToolStripStatusLabel();
         this.labelOperationStatus = new System.Windows.Forms.ToolStripStatusLabel();
         this.labelStorageStatus = new System.Windows.Forms.ToolStripStatusLabel();
         this.linkLabelAbortGitClone = new System.Windows.Forms.ToolStripStatusLabel();
         this.menuStrip1 = new MenuStripEx();
         this.gitLabToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.configureHostsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.configureStorageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.behaviorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.minimizeOnCloseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.runMrHelperWhenWindowsStartsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.remindAboutNewVersionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
         this.revisionAutoselectionModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.lastReviewedVsNextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.lastReviewedVsLatestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.baseVsLatestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.defaultRevisionTypeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.commitsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.versionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
         this.showWarningsOnFileMismatchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showWarningsAlwaysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showWarningsUntilIgnoredByUserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showWarningsNeverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator10 = new System.Windows.Forms.ToolStripSeparator();
         this.configureNotificationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.fontSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.configureColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
         this.disableSplitterRestrictionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.wrapLongRowsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
         this.layoutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.horizontalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.verticalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.sendFeedbackToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showHelpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
         this.updateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.hiddenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.discussionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.diffToolToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.diffToBaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.refreshListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.refreshSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripHosts = new ToolStripEx();
         this.toolStripActions = new ToolStripEx();
         this.toolStripHostsSeparator = new System.Windows.Forms.ToolStripSeparator();
         this.toolStripButtonLive = new System.Windows.Forms.ToolStripButton();
         this.toolStripButtonRecent = new System.Windows.Forms.ToolStripButton();
         this.toolStripButtonSearch = new System.Windows.Forms.ToolStripButton();
         this.toolStripModesSeparator = new System.Windows.Forms.ToolStripSeparator();
         this.toolStripButtonCreateNew = new System.Windows.Forms.ToolStripButton();
         this.toolStripButtonRefreshList = new System.Windows.Forms.ToolStripButton();
         this.toolStripButtonOpenFromClipboard = new System.Windows.Forms.ToolStripButton();
         this.toolStripGlobalActionsSeparator = new System.Windows.Forms.ToolStripSeparator();
         this.toolStripButtonDiffTool = new System.Windows.Forms.ToolStripButton();
         this.toolStripButtonDiscussions = new System.Windows.Forms.ToolStripButton();
         this.toolStripButtonAddComment = new System.Windows.Forms.ToolStripButton();
         this.toolStripButtonNewThread = new System.Windows.Forms.ToolStripButton();
         this.toolStripActionsSeparator = new System.Windows.Forms.ToolStripSeparator();
         this.toolStripButtonStartStopTimer = new System.Windows.Forms.ToolStripButton();
         this.toolStripButtonCancelTimer = new System.Windows.Forms.ToolStripButton();
         this.toolStripButtonGoToTimeTracking = new System.Windows.Forms.ToolStripButton();
         this.toolStripTextBoxTrackedTime = new System.Windows.Forms.ToolStripTextBox();
         this.toolStripButtonEditTrackedTime = new System.Windows.Forms.ToolStripButton();
         this.toolStripTimeTrackingSeparator = new System.Windows.Forms.ToolStripSeparator();
         this.toolStripCustomActions = new ToolStripEx();
         this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
         this.exitToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
         this.contextMenuStrip.SuspendLayout();
         this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
         this.toolStripContainer1.ContentPanel.SuspendLayout();
         this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
         this.toolStripContainer1.SuspendLayout();
         this.statusStrip1.SuspendLayout();
         this.menuStrip1.SuspendLayout();
         this.toolStripActions.SuspendLayout();
         this.SuspendLayout();
         // 
         // toolTip
         // 
         this.toolTip.AutoPopDelay = 5000;
         this.toolTip.InitialDelay = 500;
         this.toolTip.ReshowDelay = 100;
         // 
         // contextMenuStrip
         // 
         this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFromClipboardToolStripMenuItem,
            this.restoreToolStripMenuItem,
            this.exitToolStripMenuItem});
         this.contextMenuStrip.Name = "contextMenuStrip1";
         this.contextMenuStrip.Size = new System.Drawing.Size(209, 70);
         // 
         // openFromClipboardToolStripMenuItem
         // 
         this.openFromClipboardToolStripMenuItem.Name = "openFromClipboardToolStripMenuItem";
         this.openFromClipboardToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
         this.openFromClipboardToolStripMenuItem.Text = "Open MR from Clipboard";
         this.openFromClipboardToolStripMenuItem.Click += new System.EventHandler(this.openFromClipboardMenuItem_Click);
         // 
         // restoreToolStripMenuItem
         // 
         this.restoreToolStripMenuItem.Name = "restoreToolStripMenuItem";
         this.restoreToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
         this.restoreToolStripMenuItem.Text = "Restore";
         this.restoreToolStripMenuItem.Click += new System.EventHandler(this.notifyIcon_DoubleClick);
         // 
         // exitToolStripMenuItem
         // 
         this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
         this.exitToolStripMenuItem.Size = new System.Drawing.Size(208, 22);
         this.exitToolStripMenuItem.Text = "Exit";
         this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
         // 
         // notifyIcon
         // 
         this.notifyIcon.BalloonTipText = "I will now live in your tray";
         this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
         this.notifyIcon.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.notifyIcon.Text = "Merge Request Helper";
         this.notifyIcon.Visible = true;
         this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);
         // 
         // tabControlHost
         // 
         this.tabControlHost.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
         this.tabControlHost.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tabControlHost.ItemSize = new System.Drawing.Size(0, 1);
         this.tabControlHost.Location = new System.Drawing.Point(0, 0);
         this.tabControlHost.Name = "tabControlHost";
         this.tabControlHost.SelectedIndex = 0;
         this.tabControlHost.Size = new System.Drawing.Size(1050, 402);
         this.tabControlHost.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
         this.tabControlHost.TabIndex = 0;
         this.tabControlHost.SelectedIndexChanged += new System.EventHandler(this.tabControlHost_SelectedIndexChanged);
         // 
         // toolStripContainer1
         // 
         // 
         // toolStripContainer1.BottomToolStripPanel
         // 
         this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.statusStrip1);
         // 
         // toolStripContainer1.ContentPanel
         // 
         this.toolStripContainer1.ContentPanel.AutoScroll = true;
         this.toolStripContainer1.ContentPanel.Controls.Add(this.tabControlHost);
         this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(1050, 402);
         this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
         this.toolStripContainer1.Name = "toolStripContainer1";
         this.toolStripContainer1.Size = new System.Drawing.Size(1050, 481);
         this.toolStripContainer1.TabIndex = 2;
         this.toolStripContainer1.Text = "toolStripContainer1";
         // 
         // toolStripContainer1.TopToolStripPanel
         // 
         this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.menuStrip1);
         this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStripHosts);
         this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStripActions);
         this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStripCustomActions);
         // 
         // statusStrip1
         // 
         this.statusStrip1.ClickThrough = true;
         this.statusStrip1.Dock = System.Windows.Forms.DockStyle.None;
         this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.labelConnectionStatus,
            this.labelOperationStatus,
            this.labelStorageStatus,
            this.linkLabelAbortGitClone});
         this.statusStrip1.Location = new System.Drawing.Point(0, 0);
         this.statusStrip1.Name = "statusStrip1";
         this.statusStrip1.ShowItemToolTips = true;
         this.statusStrip1.Size = new System.Drawing.Size(1050, 24);
         this.statusStrip1.TabIndex = 0;
         // 
         // labelConnectionStatus
         // 
         this.labelConnectionStatus.AutoSize = false;
         this.labelConnectionStatus.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.labelConnectionStatus.Name = "labelConnectionStatus";
         this.labelConnectionStatus.Size = new System.Drawing.Size(230, 19);
         this.labelConnectionStatus.Text = "labelConnectionStatus";
         this.labelConnectionStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         // 
         // labelOperationStatus
         // 
         this.labelOperationStatus.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
         this.labelOperationStatus.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.labelOperationStatus.Name = "labelOperationStatus";
         this.labelOperationStatus.Size = new System.Drawing.Size(382, 19);
         this.labelOperationStatus.Spring = true;
         this.labelOperationStatus.Text = "labelOperationStatus";
         this.labelOperationStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         // 
         // labelStorageStatus
         // 
         this.labelStorageStatus.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
         this.labelStorageStatus.BorderStyle = System.Windows.Forms.Border3DStyle.Etched;
         this.labelStorageStatus.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.labelStorageStatus.Name = "labelStorageStatus";
         this.labelStorageStatus.Size = new System.Drawing.Size(382, 19);
         this.labelStorageStatus.Spring = true;
         this.labelStorageStatus.Text = "labelStorageStatus";
         this.labelStorageStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
         // 
         // linkLabelAbortGitClone
         // 
         this.linkLabelAbortGitClone.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
         this.linkLabelAbortGitClone.IsLink = true;
         this.linkLabelAbortGitClone.Name = "linkLabelAbortGitClone";
         this.linkLabelAbortGitClone.Size = new System.Drawing.Size(41, 19);
         this.linkLabelAbortGitClone.Text = "Abort";
         this.linkLabelAbortGitClone.Click += new System.EventHandler(this.linkLabelAbortGitClone_Click);
         // 
         // menuStrip1
         // 
         this.menuStrip1.ClickThrough = true;
         this.menuStrip1.Dock = System.Windows.Forms.DockStyle.None;
         this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gitLabToolStripMenuItem,
            this.behaviorToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem,
            this.hiddenToolStripMenuItem});
         this.menuStrip1.Location = new System.Drawing.Point(0, 0);
         this.menuStrip1.Name = "menuStrip1";
         this.menuStrip1.Size = new System.Drawing.Size(1050, 24);
         this.menuStrip1.TabIndex = 2;
         this.menuStrip1.Text = "menuStrip1";
         // 
         // gitLabToolStripMenuItem
         // 
         this.gitLabToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.configureHostsToolStripMenuItem,
            this.configureStorageToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem1});
         this.gitLabToolStripMenuItem.Name = "gitLabToolStripMenuItem";
         this.gitLabToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
         this.gitLabToolStripMenuItem.Text = "System";
         // 
         // configureHostsToolStripMenuItem
         // 
         this.configureHostsToolStripMenuItem.Name = "configureHostsToolStripMenuItem";
         this.configureHostsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
         this.configureHostsToolStripMenuItem.Text = "Configure hosts...";
         this.configureHostsToolStripMenuItem.Click += new System.EventHandler(this.configureHostsToolStripMenuItem_Click);
         // 
         // configureStorageToolStripMenuItem
         // 
         this.configureStorageToolStripMenuItem.Name = "configureStorageToolStripMenuItem";
         this.configureStorageToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
         this.configureStorageToolStripMenuItem.Text = "Configure storage...";
         this.configureStorageToolStripMenuItem.Click += new System.EventHandler(this.configureStorageToolStripMenuItem_Click);
         // 
         // behaviorToolStripMenuItem
         // 
         this.behaviorToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.minimizeOnCloseToolStripMenuItem,
            this.runMrHelperWhenWindowsStartsToolStripMenuItem,
            this.remindAboutNewVersionsToolStripMenuItem,
            this.toolStripSeparator4,
            this.revisionAutoselectionModeToolStripMenuItem,
            this.defaultRevisionTypeToolStripMenuItem,
            this.toolStripSeparator5,
            this.showWarningsOnFileMismatchToolStripMenuItem,
            this.showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem,
            this.toolStripSeparator10,
            this.configureNotificationsToolStripMenuItem});
         this.behaviorToolStripMenuItem.Name = "behaviorToolStripMenuItem";
         this.behaviorToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
         this.behaviorToolStripMenuItem.Text = "Options";
         // 
         // minimizeOnCloseToolStripMenuItem
         // 
         this.minimizeOnCloseToolStripMenuItem.CheckOnClick = true;
         this.minimizeOnCloseToolStripMenuItem.Name = "minimizeOnCloseToolStripMenuItem";
         this.minimizeOnCloseToolStripMenuItem.Size = new System.Drawing.Size(323, 22);
         this.minimizeOnCloseToolStripMenuItem.Text = "Minimize on close";
         this.minimizeOnCloseToolStripMenuItem.CheckedChanged += new System.EventHandler(this.checkBoxMinimizeOnClose_CheckedChanged);
         // 
         // runMrHelperWhenWindowsStartsToolStripMenuItem
         // 
         this.runMrHelperWhenWindowsStartsToolStripMenuItem.CheckOnClick = true;
         this.runMrHelperWhenWindowsStartsToolStripMenuItem.Name = "runMrHelperWhenWindowsStartsToolStripMenuItem";
         this.runMrHelperWhenWindowsStartsToolStripMenuItem.Size = new System.Drawing.Size(323, 22);
         this.runMrHelperWhenWindowsStartsToolStripMenuItem.Text = "Run mrHelper when Windows starts";
         this.runMrHelperWhenWindowsStartsToolStripMenuItem.CheckedChanged += new System.EventHandler(this.checkBoxRunWhenWindowsStarts_CheckedChanged);
         // 
         // remindAboutNewVersionsToolStripMenuItem
         // 
         this.remindAboutNewVersionsToolStripMenuItem.CheckOnClick = true;
         this.remindAboutNewVersionsToolStripMenuItem.Name = "remindAboutNewVersionsToolStripMenuItem";
         this.remindAboutNewVersionsToolStripMenuItem.Size = new System.Drawing.Size(323, 22);
         this.remindAboutNewVersionsToolStripMenuItem.Text = "Remind about new versions";
         this.remindAboutNewVersionsToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.checkBoxRemindAboutAvailableNewVersion_CheckedChanged);
         // 
         // toolStripSeparator4
         // 
         this.toolStripSeparator4.Name = "toolStripSeparator4";
         this.toolStripSeparator4.Size = new System.Drawing.Size(320, 6);
         // 
         // revisionAutoselectionModeToolStripMenuItem
         // 
         this.revisionAutoselectionModeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lastReviewedVsNextToolStripMenuItem,
            this.lastReviewedVsLatestToolStripMenuItem,
            this.baseVsLatestToolStripMenuItem});
         this.revisionAutoselectionModeToolStripMenuItem.Name = "revisionAutoselectionModeToolStripMenuItem";
         this.revisionAutoselectionModeToolStripMenuItem.Size = new System.Drawing.Size(323, 22);
         this.revisionAutoselectionModeToolStripMenuItem.Text = "Revision auto-selection mode";
         // 
         // lastReviewedVsNextToolStripMenuItem
         // 
         this.lastReviewedVsNextToolStripMenuItem.CheckOnClick = true;
         this.lastReviewedVsNextToolStripMenuItem.Name = "lastReviewedVsNextToolStripMenuItem";
         this.lastReviewedVsNextToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
         this.lastReviewedVsNextToolStripMenuItem.Text = "Last Reviewed vs Next";
         this.lastReviewedVsNextToolStripMenuItem.CheckedChanged += new System.EventHandler(this.radioButtonAutoSelectionMode_CheckedChanged);
         // 
         // lastReviewedVsLatestToolStripMenuItem
         // 
         this.lastReviewedVsLatestToolStripMenuItem.CheckOnClick = true;
         this.lastReviewedVsLatestToolStripMenuItem.Name = "lastReviewedVsLatestToolStripMenuItem";
         this.lastReviewedVsLatestToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
         this.lastReviewedVsLatestToolStripMenuItem.Text = "Last Reviewed vs Latest";
         this.lastReviewedVsLatestToolStripMenuItem.CheckedChanged += new System.EventHandler(this.radioButtonAutoSelectionMode_CheckedChanged);
         // 
         // baseVsLatestToolStripMenuItem
         // 
         this.baseVsLatestToolStripMenuItem.CheckOnClick = true;
         this.baseVsLatestToolStripMenuItem.Name = "baseVsLatestToolStripMenuItem";
         this.baseVsLatestToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
         this.baseVsLatestToolStripMenuItem.Text = "Base vs Latest";
         this.baseVsLatestToolStripMenuItem.CheckedChanged += new System.EventHandler(this.radioButtonAutoSelectionMode_CheckedChanged);
         // 
         // defaultRevisionTypeToolStripMenuItem
         // 
         this.defaultRevisionTypeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.commitsToolStripMenuItem,
            this.versionsToolStripMenuItem});
         this.defaultRevisionTypeToolStripMenuItem.Name = "defaultRevisionTypeToolStripMenuItem";
         this.defaultRevisionTypeToolStripMenuItem.Size = new System.Drawing.Size(323, 22);
         this.defaultRevisionTypeToolStripMenuItem.Text = "Default revision type";
         // 
         // commitsToolStripMenuItem
         // 
         this.commitsToolStripMenuItem.CheckOnClick = true;
         this.commitsToolStripMenuItem.Name = "commitsToolStripMenuItem";
         this.commitsToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
         this.commitsToolStripMenuItem.Text = "Commits";
         this.commitsToolStripMenuItem.CheckedChanged += new System.EventHandler(this.radioButtonRevisionType_CheckedChanged);
         // 
         // versionsToolStripMenuItem
         // 
         this.versionsToolStripMenuItem.CheckOnClick = true;
         this.versionsToolStripMenuItem.Name = "versionsToolStripMenuItem";
         this.versionsToolStripMenuItem.Size = new System.Drawing.Size(123, 22);
         this.versionsToolStripMenuItem.Text = "Versions";
         this.versionsToolStripMenuItem.CheckedChanged += new System.EventHandler(this.radioButtonRevisionType_CheckedChanged);
         // 
         // toolStripSeparator5
         // 
         this.toolStripSeparator5.Name = "toolStripSeparator5";
         this.toolStripSeparator5.Size = new System.Drawing.Size(320, 6);
         // 
         // showWarningsOnFileMismatchToolStripMenuItem
         // 
         this.showWarningsOnFileMismatchToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showWarningsAlwaysToolStripMenuItem,
            this.showWarningsUntilIgnoredByUserToolStripMenuItem,
            this.showWarningsNeverToolStripMenuItem});
         this.showWarningsOnFileMismatchToolStripMenuItem.Name = "showWarningsOnFileMismatchToolStripMenuItem";
         this.showWarningsOnFileMismatchToolStripMenuItem.Size = new System.Drawing.Size(323, 22);
         this.showWarningsOnFileMismatchToolStripMenuItem.Text = "Show warnings on file mismatch";
         // 
         // showWarningsAlwaysToolStripMenuItem
         // 
         this.showWarningsAlwaysToolStripMenuItem.CheckOnClick = true;
         this.showWarningsAlwaysToolStripMenuItem.Name = "showWarningsAlwaysToolStripMenuItem";
         this.showWarningsAlwaysToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
         this.showWarningsAlwaysToolStripMenuItem.Text = "Always";
         this.showWarningsAlwaysToolStripMenuItem.CheckedChanged += new System.EventHandler(this.radioButtonShowWarningsOnFileMismatchMode_CheckedChanged);
         // 
         // showWarningsUntilIgnoredByUserToolStripMenuItem
         // 
         this.showWarningsUntilIgnoredByUserToolStripMenuItem.CheckOnClick = true;
         this.showWarningsUntilIgnoredByUserToolStripMenuItem.Name = "showWarningsUntilIgnoredByUserToolStripMenuItem";
         this.showWarningsUntilIgnoredByUserToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
         this.showWarningsUntilIgnoredByUserToolStripMenuItem.Text = "Until ignored by user";
         this.showWarningsUntilIgnoredByUserToolStripMenuItem.CheckedChanged += new System.EventHandler(this.radioButtonShowWarningsOnFileMismatchMode_CheckedChanged);
         // 
         // showWarningsNeverToolStripMenuItem
         // 
         this.showWarningsNeverToolStripMenuItem.CheckOnClick = true;
         this.showWarningsNeverToolStripMenuItem.Name = "showWarningsNeverToolStripMenuItem";
         this.showWarningsNeverToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
         this.showWarningsNeverToolStripMenuItem.Text = "Never";
         this.showWarningsNeverToolStripMenuItem.CheckedChanged += new System.EventHandler(this.radioButtonShowWarningsOnFileMismatchMode_CheckedChanged);
         // 
         // showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem
         // 
         this.showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem.CheckOnClick = true;
         this.showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem.Name = "showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem";
         this.showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem.Size = new System.Drawing.Size(323, 22);
         this.showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem.Text = "Show New Discussion on top of all applications";
         this.showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem.CheckedChanged += new System.EventHandler(this.checkBoxNewDiscussionIsTopMostForm_CheckedChanged);
         // 
         // toolStripSeparator10
         // 
         this.toolStripSeparator10.Name = "toolStripSeparator10";
         this.toolStripSeparator10.Size = new System.Drawing.Size(320, 6);
         // 
         // configureNotificationsToolStripMenuItem
         // 
         this.configureNotificationsToolStripMenuItem.Name = "configureNotificationsToolStripMenuItem";
         this.configureNotificationsToolStripMenuItem.Size = new System.Drawing.Size(323, 22);
         this.configureNotificationsToolStripMenuItem.Text = "Configure notifications...";
         this.configureNotificationsToolStripMenuItem.Click += new System.EventHandler(this.configureNotificationsToolStripMenuItem_Click);
         // 
         // viewToolStripMenuItem
         // 
         this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fontSizeToolStripMenuItem,
            this.configureColorsToolStripMenuItem,
            this.toolStripSeparator6,
            this.disableSplitterRestrictionsToolStripMenuItem,
            this.wrapLongRowsToolStripMenuItem,
            this.toolStripSeparator7,
            this.layoutToolStripMenuItem});
         this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
         this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
         this.viewToolStripMenuItem.Text = "View";
         // 
         // fontSizeToolStripMenuItem
         // 
         this.fontSizeToolStripMenuItem.Name = "fontSizeToolStripMenuItem";
         this.fontSizeToolStripMenuItem.Size = new System.Drawing.Size(212, 22);
         this.fontSizeToolStripMenuItem.Text = "Font size";
         // 
         // configureColorsToolStripMenuItem
         // 
         this.configureColorsToolStripMenuItem.Name = "configureColorsToolStripMenuItem";
         this.configureColorsToolStripMenuItem.Size = new System.Drawing.Size(212, 22);
         this.configureColorsToolStripMenuItem.Text = "Configure colors...";
         this.configureColorsToolStripMenuItem.Click += new System.EventHandler(this.configureColorsToolStripMenuItem_Click);
         // 
         // toolStripSeparator6
         // 
         this.toolStripSeparator6.Name = "toolStripSeparator6";
         this.toolStripSeparator6.Size = new System.Drawing.Size(209, 6);
         // 
         // disableSplitterRestrictionsToolStripMenuItem
         // 
         this.disableSplitterRestrictionsToolStripMenuItem.CheckOnClick = true;
         this.disableSplitterRestrictionsToolStripMenuItem.Name = "disableSplitterRestrictionsToolStripMenuItem";
         this.disableSplitterRestrictionsToolStripMenuItem.Size = new System.Drawing.Size(212, 22);
         this.disableSplitterRestrictionsToolStripMenuItem.Text = "Disable splitter restrictions";
         this.disableSplitterRestrictionsToolStripMenuItem.Visible = false;
         this.disableSplitterRestrictionsToolStripMenuItem.CheckedChanged += new System.EventHandler(this.checkBoxDisableSplitterRestrictions_CheckedChanged);
         // 
         // wrapLongRowsToolStripMenuItem
         // 
         this.wrapLongRowsToolStripMenuItem.CheckOnClick = true;
         this.wrapLongRowsToolStripMenuItem.Name = "wrapLongRowsToolStripMenuItem";
         this.wrapLongRowsToolStripMenuItem.Size = new System.Drawing.Size(212, 22);
         this.wrapLongRowsToolStripMenuItem.Text = "Wrap long rows";
         this.wrapLongRowsToolStripMenuItem.CheckedChanged += new System.EventHandler(this.checkBoxWordWrapLongRows_CheckedChanged);
         // 
         // toolStripSeparator7
         // 
         this.toolStripSeparator7.Name = "toolStripSeparator7";
         this.toolStripSeparator7.Size = new System.Drawing.Size(209, 6);
         // 
         // layoutToolStripMenuItem
         // 
         this.layoutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.horizontalToolStripMenuItem,
            this.verticalToolStripMenuItem});
         this.layoutToolStripMenuItem.Name = "layoutToolStripMenuItem";
         this.layoutToolStripMenuItem.Size = new System.Drawing.Size(212, 22);
         this.layoutToolStripMenuItem.Text = "Layout";
         // 
         // horizontalToolStripMenuItem
         // 
         this.horizontalToolStripMenuItem.Name = "horizontalToolStripMenuItem";
         this.horizontalToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
         this.horizontalToolStripMenuItem.Text = "Horizontal";
         this.horizontalToolStripMenuItem.CheckedChanged += new System.EventHandler(this.radioButtonMainWindowLayout_CheckedChanged);
         // 
         // verticalToolStripMenuItem
         // 
         this.verticalToolStripMenuItem.Name = "verticalToolStripMenuItem";
         this.verticalToolStripMenuItem.Size = new System.Drawing.Size(129, 22);
         this.verticalToolStripMenuItem.Text = "Vertical";
         this.verticalToolStripMenuItem.CheckedChanged += new System.EventHandler(this.radioButtonMainWindowLayout_CheckedChanged);
         // 
         // helpToolStripMenuItem
         // 
         this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.sendFeedbackToolStripMenuItem,
            this.showHelpToolStripMenuItem,
            this.toolStripSeparator8,
            this.updateToolStripMenuItem});
         this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
         this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
         this.helpToolStripMenuItem.Text = "Help";
         // 
         // sendFeedbackToolStripMenuItem
         // 
         this.sendFeedbackToolStripMenuItem.Name = "sendFeedbackToolStripMenuItem";
         this.sendFeedbackToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
         this.sendFeedbackToolStripMenuItem.Text = "Send logs/feedback";
         this.sendFeedbackToolStripMenuItem.Click += new System.EventHandler(this.sendFeedbackToolStripMenuItem_Click);
         // 
         // showHelpToolStripMenuItem
         // 
         this.showHelpToolStripMenuItem.Name = "showHelpToolStripMenuItem";
         this.showHelpToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F1;
         this.showHelpToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
         this.showHelpToolStripMenuItem.Text = "Help";
         this.showHelpToolStripMenuItem.Click += new System.EventHandler(this.showHelpToolStripMenuItem_Click);
         // 
         // toolStripSeparator8
         // 
         this.toolStripSeparator8.Name = "toolStripSeparator8";
         this.toolStripSeparator8.Size = new System.Drawing.Size(175, 6);
         // 
         // updateToolStripMenuItem
         // 
         this.updateToolStripMenuItem.Enabled = false;
         this.updateToolStripMenuItem.Name = "updateToolStripMenuItem";
         this.updateToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
         this.updateToolStripMenuItem.Text = "Update";
         this.updateToolStripMenuItem.Click += new System.EventHandler(this.updateToolStripMenuItem_Click);
         // 
         // hiddenToolStripMenuItem
         // 
         this.hiddenToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.discussionsToolStripMenuItem,
            this.diffToolToolStripMenuItem,
            this.diffToBaseToolStripMenuItem,
            this.refreshListToolStripMenuItem,
            this.refreshSelectedToolStripMenuItem});
         this.hiddenToolStripMenuItem.Name = "hiddenToolStripMenuItem";
         this.hiddenToolStripMenuItem.Size = new System.Drawing.Size(58, 20);
         this.hiddenToolStripMenuItem.Text = "Hidden";
         this.hiddenToolStripMenuItem.Visible = false;
         // 
         // discussionsToolStripMenuItem
         // 
         this.discussionsToolStripMenuItem.Name = "discussionsToolStripMenuItem";
         this.discussionsToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F2;
         this.discussionsToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
         this.discussionsToolStripMenuItem.Text = "Discussions";
         this.discussionsToolStripMenuItem.Click += new System.EventHandler(this.toolStripButtonDiscussions_Click);
         // 
         // diffToolToolStripMenuItem
         // 
         this.diffToolToolStripMenuItem.Name = "diffToolToolStripMenuItem";
         this.diffToolToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F3;
         this.diffToolToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
         this.diffToolToolStripMenuItem.Text = "Diff Tool";
         this.diffToolToolStripMenuItem.Click += new System.EventHandler(this.toolStripButtonDiffTool_Click);
         // 
         // diffToBaseToolStripMenuItem
         // 
         this.diffToBaseToolStripMenuItem.Name = "diffToBaseToolStripMenuItem";
         this.diffToBaseToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F3)));
         this.diffToBaseToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
         this.diffToBaseToolStripMenuItem.Text = "Diff to Base";
         this.diffToBaseToolStripMenuItem.Click += new System.EventHandler(this.diffToBaseToolStripMenuItem_Click);
         // 
         // refreshListToolStripMenuItem
         // 
         this.refreshListToolStripMenuItem.Name = "refreshListToolStripMenuItem";
         this.refreshListToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
         this.refreshListToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
         this.refreshListToolStripMenuItem.Text = "Refresh list";
         this.refreshListToolStripMenuItem.Click += new System.EventHandler(this.toolStripButtonRefreshList_Click);
         // 
         // refreshSelectedToolStripMenuItem
         // 
         this.refreshSelectedToolStripMenuItem.Name = "refreshSelectedToolStripMenuItem";
         this.refreshSelectedToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F5)));
         this.refreshSelectedToolStripMenuItem.Size = new System.Drawing.Size(210, 22);
         this.refreshSelectedToolStripMenuItem.Text = "Refresh selected";
         this.refreshSelectedToolStripMenuItem.Click += new System.EventHandler(this.refreshSelectedToolStripMenuItem_Click);
         // 
         // toolStripHosts
         // 
         this.toolStripHosts.ClickThrough = true;
         this.toolStripHosts.Dock = System.Windows.Forms.DockStyle.None;
         this.toolStripHosts.ImageScalingSize = new System.Drawing.Size(24, 24);
         this.toolStripHosts.Location = new System.Drawing.Point(3, 24);
         this.toolStripHosts.Name = "toolStripHosts";
         this.toolStripHosts.Size = new System.Drawing.Size(111, 25);
         this.toolStripHosts.TabIndex = 5;
         // 
         // toolStripActions
         // 
         this.toolStripActions.ClickThrough = true;
         this.toolStripActions.Dock = System.Windows.Forms.DockStyle.None;
         this.toolStripActions.ImageScalingSize = new System.Drawing.Size(24, 24);
         this.toolStripActions.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripHostsSeparator,
            this.toolStripButtonLive,
            this.toolStripButtonRecent,
            this.toolStripButtonSearch,
            this.toolStripModesSeparator,
            this.toolStripButtonCreateNew,
            this.toolStripButtonRefreshList,
            this.toolStripButtonOpenFromClipboard,
            this.toolStripGlobalActionsSeparator,
            this.toolStripButtonDiffTool,
            this.toolStripButtonDiscussions,
            this.toolStripButtonAddComment,
            this.toolStripButtonNewThread,
            this.toolStripActionsSeparator,
            this.toolStripButtonStartStopTimer,
            this.toolStripButtonCancelTimer,
            this.toolStripButtonGoToTimeTracking,
            this.toolStripTextBoxTrackedTime,
            this.toolStripButtonEditTrackedTime,
            this.toolStripTimeTrackingSeparator});
         this.toolStripActions.Location = new System.Drawing.Point(114, 24);
         this.toolStripActions.Name = "toolStripActions";
         this.toolStripActions.Size = new System.Drawing.Size(577, 31);
         this.toolStripActions.TabIndex = 3;
         // 
         // toolStripHostsSeparator
         // 
         this.toolStripHostsSeparator.Name = "toolStripHostsSeparator";
         this.toolStripHostsSeparator.Size = new System.Drawing.Size(6, 31);
         // 
         // toolStripButtonLive
         // 
         this.toolStripButtonLive.Checked = true;
         this.toolStripButtonLive.CheckState = System.Windows.Forms.CheckState.Checked;
         this.toolStripButtonLive.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.toolStripButtonLive.Name = "toolStripButtonLive";
         this.toolStripButtonLive.Size = new System.Drawing.Size(32, 28);
         this.toolStripButtonLive.Text = "Live";
         this.toolStripButtonLive.ToolTipText = "Show list of active merge requests";
         this.toolStripButtonLive.CheckedChanged += new System.EventHandler(this.toolStripButton_CheckedChanged);
         // 
         // toolStripButtonRecent
         // 
         this.toolStripButtonRecent.CheckOnClick = true;
         this.toolStripButtonRecent.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.toolStripButtonRecent.Name = "toolStripButtonRecent";
         this.toolStripButtonRecent.Size = new System.Drawing.Size(47, 28);
         this.toolStripButtonRecent.Text = "Recent";
         this.toolStripButtonRecent.ToolTipText = "Show list of recently reviewed merge requests";
         this.toolStripButtonRecent.CheckedChanged += new System.EventHandler(this.toolStripButton_CheckedChanged);
         // 
         // toolStripButtonSearch
         // 
         this.toolStripButtonSearch.CheckOnClick = true;
         this.toolStripButtonSearch.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
         this.toolStripButtonSearch.Name = "toolStripButtonSearch";
         this.toolStripButtonSearch.Size = new System.Drawing.Size(46, 28);
         this.toolStripButtonSearch.Text = "Search";
         this.toolStripButtonSearch.ToolTipText = "Show Search panel";
         this.toolStripButtonSearch.CheckedChanged += new System.EventHandler(this.toolStripButton_CheckedChanged);
         // 
         // toolStripModesSeparator
         // 
         this.toolStripModesSeparator.Name = "toolStripModesSeparator";
         this.toolStripModesSeparator.Size = new System.Drawing.Size(6, 31);
         // 
         // toolStripButtonCreateNew
         // 
         this.toolStripButtonCreateNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonCreateNew.Image = global::mrHelper.App.Properties.Resources.create_new_24x24;
         this.toolStripButtonCreateNew.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonCreateNew.Name = "toolStripButtonCreateNew";
         this.toolStripButtonCreateNew.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonCreateNew.Text = "toolStripButtonCreateNew";
         this.toolStripButtonCreateNew.ToolTipText = "Create new merge request";
         this.toolStripButtonCreateNew.Click += new System.EventHandler(this.toolStripButtonCreateNew_Click);
         // 
         // toolStripButtonRefreshList
         // 
         this.toolStripButtonRefreshList.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonRefreshList.Image = global::mrHelper.App.Properties.Resources.refresh_24x24;
         this.toolStripButtonRefreshList.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonRefreshList.Name = "toolStripButtonRefreshList";
         this.toolStripButtonRefreshList.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonRefreshList.Text = "toolStripButtonRefreshList";
         this.toolStripButtonRefreshList.ToolTipText = "Refresh list of active merge requests (F5)";
         this.toolStripButtonRefreshList.Click += new System.EventHandler(this.toolStripButtonRefreshList_Click);
         // 
         // toolStripButtonOpenFromClipboard
         // 
         this.toolStripButtonOpenFromClipboard.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonOpenFromClipboard.Image = global::mrHelper.App.Properties.Resources.clipboard_24x24;
         this.toolStripButtonOpenFromClipboard.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonOpenFromClipboard.Name = "toolStripButtonOpenFromClipboard";
         this.toolStripButtonOpenFromClipboard.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonOpenFromClipboard.Text = "toolStripButtonOpenFromClipboard";
         this.toolStripButtonOpenFromClipboard.ToolTipText = "Open merge request by URL from Clipboard";
         this.toolStripButtonOpenFromClipboard.Click += new System.EventHandler(this.toolStripButtonOpenFromClipboard_Click);
         // 
         // toolStripGlobalActionsSeparator
         // 
         this.toolStripGlobalActionsSeparator.Name = "toolStripGlobalActionsSeparator";
         this.toolStripGlobalActionsSeparator.Size = new System.Drawing.Size(6, 31);
         // 
         // toolStripButtonDiffTool
         // 
         this.toolStripButtonDiffTool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonDiffTool.Image = global::mrHelper.App.Properties.Resources.diff_24x24;
         this.toolStripButtonDiffTool.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonDiffTool.Name = "toolStripButtonDiffTool";
         this.toolStripButtonDiffTool.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonDiffTool.Text = "toolStripButtonDiffTool";
         this.toolStripButtonDiffTool.ToolTipText = "Launch Diff Tool (F3)";
         this.toolStripButtonDiffTool.Click += new System.EventHandler(this.toolStripButtonDiffTool_Click);
         // 
         // toolStripButtonDiscussions
         // 
         this.toolStripButtonDiscussions.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonDiscussions.Image = global::mrHelper.App.Properties.Resources.discussions_24x24;
         this.toolStripButtonDiscussions.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonDiscussions.Name = "toolStripButtonDiscussions";
         this.toolStripButtonDiscussions.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonDiscussions.Text = "toolStripButtonDiscussions";
         this.toolStripButtonDiscussions.ToolTipText = "Show Discussions (F2)";
         this.toolStripButtonDiscussions.Click += new System.EventHandler(this.toolStripButtonDiscussions_Click);
         // 
         // toolStripButtonAddComment
         // 
         this.toolStripButtonAddComment.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonAddComment.Image = global::mrHelper.App.Properties.Resources.add_comment_24x24;
         this.toolStripButtonAddComment.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonAddComment.Name = "toolStripButtonAddComment";
         this.toolStripButtonAddComment.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonAddComment.Text = "toolStripButtonAddComment";
         this.toolStripButtonAddComment.ToolTipText = "Add a new comment";
         this.toolStripButtonAddComment.Click += new System.EventHandler(this.toolStripButtonAddComment_Click);
         // 
         // toolStripButtonNewThread
         // 
         this.toolStripButtonNewThread.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonNewThread.Image = global::mrHelper.App.Properties.Resources.thread_24x24;
         this.toolStripButtonNewThread.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonNewThread.Name = "toolStripButtonNewThread";
         this.toolStripButtonNewThread.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonNewThread.Text = "toolStripButtonNewThread";
         this.toolStripButtonNewThread.ToolTipText = "Start a new discussion thread";
         this.toolStripButtonNewThread.Click += new System.EventHandler(this.toolStripButtonNewThread_Click);
         // 
         // toolStripActionsSeparator
         // 
         this.toolStripActionsSeparator.Name = "toolStripActionsSeparator";
         this.toolStripActionsSeparator.Size = new System.Drawing.Size(6, 31);
         // 
         // toolStripButtonStartStopTimer
         // 
         this.toolStripButtonStartStopTimer.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonStartStopTimer.Image = global::mrHelper.App.Properties.Resources.play_24x24;
         this.toolStripButtonStartStopTimer.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonStartStopTimer.Name = "toolStripButtonStartStopTimer";
         this.toolStripButtonStartStopTimer.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonStartStopTimer.Text = "toolStripButtonStartStopTimer";
         this.toolStripButtonStartStopTimer.ToolTipText = "Start/Stop timer";
         this.toolStripButtonStartStopTimer.Click += new System.EventHandler(this.toolStripButtonStartStopTimer_Click);
         // 
         // toolStripButtonCancelTimer
         // 
         this.toolStripButtonCancelTimer.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonCancelTimer.Enabled = false;
         this.toolStripButtonCancelTimer.Image = global::mrHelper.App.Properties.Resources.cancel_24x24;
         this.toolStripButtonCancelTimer.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonCancelTimer.Name = "toolStripButtonCancelTimer";
         this.toolStripButtonCancelTimer.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonCancelTimer.Text = "toolStripButtonCancelTimer";
         this.toolStripButtonCancelTimer.ToolTipText = "Cancel timer";
         this.toolStripButtonCancelTimer.Click += new System.EventHandler(this.toolStripButtonCancelTimer_Click);
         // 
         // toolStripButtonGoToTimeTracking
         // 
         this.toolStripButtonGoToTimeTracking.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonGoToTimeTracking.Enabled = false;
         this.toolStripButtonGoToTimeTracking.Image = global::mrHelper.App.Properties.Resources.link_24x24;
         this.toolStripButtonGoToTimeTracking.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonGoToTimeTracking.Name = "toolStripButtonGoToTimeTracking";
         this.toolStripButtonGoToTimeTracking.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonGoToTimeTracking.Text = "toolStripButtonGoToTimeTracking";
         this.toolStripButtonGoToTimeTracking.ToolTipText = "Select time tracking merge request";
         this.toolStripButtonGoToTimeTracking.Click += new System.EventHandler(this.toolStripButtonGoToTimeTracking_Click);
         // 
         // toolStripTextBoxTrackedTime
         // 
         this.toolStripTextBoxTrackedTime.Enabled = false;
         this.toolStripTextBoxTrackedTime.Font = new System.Drawing.Font("Segoe UI", 9F);
         this.toolStripTextBoxTrackedTime.Name = "toolStripTextBoxTrackedTime";
         this.toolStripTextBoxTrackedTime.ReadOnly = true;
         this.toolStripTextBoxTrackedTime.Size = new System.Drawing.Size(100, 31);
         // 
         // toolStripButtonEditTrackedTime
         // 
         this.toolStripButtonEditTrackedTime.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
         this.toolStripButtonEditTrackedTime.Image = global::mrHelper.App.Properties.Resources.edit_24x24;
         this.toolStripButtonEditTrackedTime.ImageTransparentColor = System.Drawing.Color.Magenta;
         this.toolStripButtonEditTrackedTime.Name = "toolStripButtonEditTrackedTime";
         this.toolStripButtonEditTrackedTime.Size = new System.Drawing.Size(28, 28);
         this.toolStripButtonEditTrackedTime.Text = "toolStripButtonEditTrackedTime";
         this.toolStripButtonEditTrackedTime.ToolTipText = "Edit tracked time";
         this.toolStripButtonEditTrackedTime.Click += new System.EventHandler(this.toolStripButtonEditTrackedTime_Click);
         // 
         // toolStripTimeTrackingSeparator
         // 
         this.toolStripTimeTrackingSeparator.Name = "toolStripTimeTrackingSeparator";
         this.toolStripTimeTrackingSeparator.Size = new System.Drawing.Size(6, 31);
         // 
         // toolStripCustomActions
         // 
         this.toolStripCustomActions.ClickThrough = true;
         this.toolStripCustomActions.Dock = System.Windows.Forms.DockStyle.None;
         this.toolStripCustomActions.ImageScalingSize = new System.Drawing.Size(24, 24);
         this.toolStripCustomActions.Location = new System.Drawing.Point(693, 24);
         this.toolStripCustomActions.Name = "toolStripCustomActions";
         this.toolStripCustomActions.Size = new System.Drawing.Size(111, 25);
         this.toolStripCustomActions.TabIndex = 4;
         // 
         // toolStripSeparator1
         // 
         this.toolStripSeparator1.Name = "toolStripSeparator1";
         this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
         // 
         // exitToolStripMenuItem1
         // 
         this.exitToolStripMenuItem1.Name = "exitToolStripMenuItem1";
         this.exitToolStripMenuItem1.Size = new System.Drawing.Size(180, 22);
         this.exitToolStripMenuItem1.Text = "Exit";
         this.exitToolStripMenuItem1.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(1050, 481);
         this.Controls.Add(this.toolStripContainer1);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.KeyPreview = true;
         this.MainMenuStrip = this.menuStrip1;
         this.Name = "MainForm";
         this.Text = "Merge Request Helper";
         this.contextMenuStrip.ResumeLayout(false);
         this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
         this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
         this.toolStripContainer1.ContentPanel.ResumeLayout(false);
         this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
         this.toolStripContainer1.TopToolStripPanel.PerformLayout();
         this.toolStripContainer1.ResumeLayout(false);
         this.toolStripContainer1.PerformLayout();
         this.statusStrip1.ResumeLayout(false);
         this.statusStrip1.PerformLayout();
         this.menuStrip1.ResumeLayout(false);
         this.menuStrip1.PerformLayout();
         this.toolStripActions.ResumeLayout(false);
         this.toolStripActions.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion
      private System.Windows.Forms.ToolTip toolTip;
      private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
      private System.Windows.Forms.ToolStripMenuItem restoreToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
      private System.Windows.Forms.NotifyIcon notifyIcon;
      private ToolStripMenuItem openFromClipboardToolStripMenuItem;
      private PlainTabControl tabControlHost;
      private ToolStripContainer toolStripContainer1;
      private MenuStripEx menuStrip1;
      private ToolStripEx toolStripActions;
      private StatusStripEx statusStrip1;
      private ToolStripButton toolStripButtonCancelTimer;
      private ToolStripSeparator toolStripHostsSeparator;
      private ToolStripButton toolStripButtonLive;
      private ToolStripSeparator toolStripActionsSeparator;
      private ToolStripSeparator toolStripGlobalActionsSeparator;
      private ToolStripSeparator toolStripModesSeparator;
      private ToolStripButton toolStripButtonDiffTool;
      private ToolStripButton toolStripButtonDiscussions;
      private ToolStripButton toolStripButtonAddComment;
      private ToolStripButton toolStripButtonNewThread;
      private ToolStripButton toolStripButtonRefreshList;
      private ToolStripButton toolStripButtonOpenFromClipboard;
      private ToolStripButton toolStripButtonCreateNew;
      private ToolStripButton toolStripButtonSearch;
      private ToolStripButton toolStripButtonRecent;
      private ToolStripTextBox toolStripTextBoxTrackedTime;
      private ToolStripSeparator toolStripTimeTrackingSeparator;
      private ToolStripMenuItem gitLabToolStripMenuItem;
      private ToolStripMenuItem behaviorToolStripMenuItem;
      private ToolStripMenuItem minimizeOnCloseToolStripMenuItem;
      private ToolStripMenuItem runMrHelperWhenWindowsStartsToolStripMenuItem;
      private ToolStripMenuItem remindAboutNewVersionsToolStripMenuItem;
      private ToolStripSeparator toolStripSeparator4;
      private ToolStripMenuItem revisionAutoselectionModeToolStripMenuItem;
      private ToolStripMenuItem lastReviewedVsNextToolStripMenuItem;
      private ToolStripMenuItem lastReviewedVsLatestToolStripMenuItem;
      private ToolStripMenuItem baseVsLatestToolStripMenuItem;
      private ToolStripMenuItem defaultRevisionTypeToolStripMenuItem;
      private ToolStripMenuItem commitsToolStripMenuItem;
      private ToolStripMenuItem versionsToolStripMenuItem;
      private ToolStripSeparator toolStripSeparator5;
      private ToolStripMenuItem showWarningsOnFileMismatchToolStripMenuItem;
      private ToolStripMenuItem showWarningsAlwaysToolStripMenuItem;
      private ToolStripMenuItem showWarningsUntilIgnoredByUserToolStripMenuItem;
      private ToolStripMenuItem showWarningsNeverToolStripMenuItem;
      private ToolStripMenuItem showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem;
      private ToolStripMenuItem viewToolStripMenuItem;
      private ToolStripMenuItem fontSizeToolStripMenuItem;
      private ToolStripMenuItem configureColorsToolStripMenuItem;
      private ToolStripSeparator toolStripSeparator6;
      private ToolStripSeparator toolStripSeparator7;
      private ToolStripMenuItem disableSplitterRestrictionsToolStripMenuItem;
      private ToolStripMenuItem wrapLongRowsToolStripMenuItem;
      private ToolStripMenuItem helpToolStripMenuItem;
      private ToolStripMenuItem sendFeedbackToolStripMenuItem;
      private ToolStripMenuItem showHelpToolStripMenuItem;
      private ToolStripSeparator toolStripSeparator8;
      private ToolStripMenuItem updateToolStripMenuItem;
      private ToolStripMenuItem configureHostsToolStripMenuItem;
      private ToolStripMenuItem configureStorageToolStripMenuItem;
      private ToolStripStatusLabel labelConnectionStatus;
      private ToolStripStatusLabel labelOperationStatus;
      private ToolStripStatusLabel labelStorageStatus;
      private ToolStripStatusLabel linkLabelAbortGitClone;
      private ToolStripSeparator toolStripSeparator10;
      private ToolStripMenuItem configureNotificationsToolStripMenuItem;
      private ToolStripButton toolStripButtonStartStopTimer;
      private ToolStripButton toolStripButtonGoToTimeTracking;
      private ToolStripButton toolStripButtonEditTrackedTime;
      private ToolStripMenuItem hiddenToolStripMenuItem;
      private ToolStripMenuItem discussionsToolStripMenuItem;
      private ToolStripMenuItem diffToolToolStripMenuItem;
      private ToolStripMenuItem diffToBaseToolStripMenuItem;
      private ToolStripMenuItem refreshListToolStripMenuItem;
      private ToolStripMenuItem refreshSelectedToolStripMenuItem;
      private ToolStripMenuItem layoutToolStripMenuItem;
      private ToolStripMenuItem horizontalToolStripMenuItem;
      private ToolStripMenuItem verticalToolStripMenuItem;
      private ToolStripEx toolStripCustomActions;
      private ToolStripEx toolStripHosts;
      private ToolStripSeparator toolStripSeparator1;
      private ToolStripMenuItem exitToolStripMenuItem1;
   }
}

