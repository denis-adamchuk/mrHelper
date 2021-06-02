using mrHelper.CommonControls.Controls;

namespace mrHelper.App.Controls
{
   partial class DiscussionsFormMenu
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

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.menuStrip = new MenuStripEx();
         this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.sortToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.defaultToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.reverseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.byReviewerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.filterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showThreadsStartedByMeOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showServiceMessagesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
         this.showResolvedAndNotResolvedThreadsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showResolvedThreadsOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showNotResolvedThreadsOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
         this.showAnsweredAndUnansweredThreadsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showAnsweredThreadsOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.showUnansweredThreadsOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.actionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.startAThreadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.addACommentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripMenuItemSeparator = new System.Windows.Forms.ToolStripSeparator();
         this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.diffContextPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.topToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.flatListOfRepliesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
         this.increaseColumnWidthToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.decreaseColumnWidthToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
         this.diffContextDepthToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripMenuItemDiffContextDepth0 = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripMenuItemDiffContextDepth1 = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripMenuItemDiffContextDepth2 = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripMenuItemDiffContextDepth3 = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripMenuItemDiffContextDepth4 = new System.Windows.Forms.ToolStripMenuItem();
         this.showTooltipsForCodeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.emulateNativeLineBreaksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
         this.fontSizeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.configureColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.menuStrip.SuspendLayout();
         this.SuspendLayout();
         // 
         // menuStrip
         // 
         this.menuStrip.BackColor = System.Drawing.SystemColors.Menu;
         this.menuStrip.ClickThrough = true;
         this.menuStrip.Dock = System.Windows.Forms.DockStyle.Fill;
         this.menuStrip.GripMargin = new System.Windows.Forms.Padding(0);
         this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.sortToolStripMenuItem,
            this.filterToolStripMenuItem,
            this.actionsToolStripMenuItem,
            this.viewToolStripMenuItem});
         this.menuStrip.Location = new System.Drawing.Point(0, 0);
         this.menuStrip.Name = "menuStrip";
         this.menuStrip.Padding = new System.Windows.Forms.Padding(0);
         this.menuStrip.Size = new System.Drawing.Size(534, 24);
         this.menuStrip.TabIndex = 4;
         this.menuStrip.Text = "menuStrip1";
         // 
         // refreshToolStripMenuItem
         // 
         this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
         this.refreshToolStripMenuItem.ShortcutKeyDisplayString = "F5";
         this.refreshToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
         this.refreshToolStripMenuItem.Size = new System.Drawing.Size(58, 24);
         this.refreshToolStripMenuItem.Text = "&Refresh";
         this.refreshToolStripMenuItem.Click += new System.EventHandler(this.onRefreshMenuItemClicked);
         // 
         // sortToolStripMenuItem
         // 
         this.sortToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.defaultToolStripMenuItem,
            this.reverseToolStripMenuItem,
            this.byReviewerToolStripMenuItem});
         this.sortToolStripMenuItem.Name = "sortToolStripMenuItem";
         this.sortToolStripMenuItem.Size = new System.Drawing.Size(40, 24);
         this.sortToolStripMenuItem.Text = "&Sort";
         // 
         // defaultToolStripMenuItem
         // 
         this.defaultToolStripMenuItem.CheckOnClick = true;
         this.defaultToolStripMenuItem.Name = "defaultToolStripMenuItem";
         this.defaultToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
         this.defaultToolStripMenuItem.Text = "Default";
         this.defaultToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onSortMenuItemCheckedChanged);
         // 
         // reverseToolStripMenuItem
         // 
         this.reverseToolStripMenuItem.CheckOnClick = true;
         this.reverseToolStripMenuItem.Name = "reverseToolStripMenuItem";
         this.reverseToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
         this.reverseToolStripMenuItem.Text = "Reverse";
         this.reverseToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onSortMenuItemCheckedChanged);
         // 
         // byReviewerToolStripMenuItem
         // 
         this.byReviewerToolStripMenuItem.CheckOnClick = true;
         this.byReviewerToolStripMenuItem.Name = "byReviewerToolStripMenuItem";
         this.byReviewerToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
         this.byReviewerToolStripMenuItem.Text = "By reviewer";
         this.byReviewerToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onSortMenuItemCheckedChanged);
         // 
         // filterToolStripMenuItem
         // 
         this.filterToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showThreadsStartedByMeOnlyToolStripMenuItem,
            this.showServiceMessagesToolStripMenuItem,
            this.toolStripSeparator1,
            this.showResolvedAndNotResolvedThreadsToolStripMenuItem,
            this.showResolvedThreadsOnlyToolStripMenuItem,
            this.showNotResolvedThreadsOnlyToolStripMenuItem,
            this.toolStripSeparator2,
            this.showAnsweredAndUnansweredThreadsToolStripMenuItem,
            this.showAnsweredThreadsOnlyToolStripMenuItem,
            this.showUnansweredThreadsOnlyToolStripMenuItem});
         this.filterToolStripMenuItem.Name = "filterToolStripMenuItem";
         this.filterToolStripMenuItem.Size = new System.Drawing.Size(45, 24);
         this.filterToolStripMenuItem.Text = "&Filter";
         // 
         // showThreadsStartedByMeOnlyToolStripMenuItem
         // 
         this.showThreadsStartedByMeOnlyToolStripMenuItem.CheckOnClick = true;
         this.showThreadsStartedByMeOnlyToolStripMenuItem.Name = "showThreadsStartedByMeOnlyToolStripMenuItem";
         this.showThreadsStartedByMeOnlyToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
         this.showThreadsStartedByMeOnlyToolStripMenuItem.Text = "Show threads started by me only";
         this.showThreadsStartedByMeOnlyToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onFilterMenuItemCheckedChanged);
         // 
         // showServiceMessagesToolStripMenuItem
         // 
         this.showServiceMessagesToolStripMenuItem.CheckOnClick = true;
         this.showServiceMessagesToolStripMenuItem.Name = "showServiceMessagesToolStripMenuItem";
         this.showServiceMessagesToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
         this.showServiceMessagesToolStripMenuItem.Text = "Show service messages";
         this.showServiceMessagesToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onFilterMenuItemCheckedChanged);
         // 
         // toolStripSeparator1
         // 
         this.toolStripSeparator1.Name = "toolStripSeparator1";
         this.toolStripSeparator1.Size = new System.Drawing.Size(285, 6);
         // 
         // showResolvedAndNotResolvedThreadsToolStripMenuItem
         // 
         this.showResolvedAndNotResolvedThreadsToolStripMenuItem.CheckOnClick = true;
         this.showResolvedAndNotResolvedThreadsToolStripMenuItem.Name = "showResolvedAndNotResolvedThreadsToolStripMenuItem";
         this.showResolvedAndNotResolvedThreadsToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
         this.showResolvedAndNotResolvedThreadsToolStripMenuItem.Text = "Show resolved and not resolved threads";
         this.showResolvedAndNotResolvedThreadsToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onFilterByResolutionCheckedChanged);
         // 
         // showResolvedThreadsOnlyToolStripMenuItem
         // 
         this.showResolvedThreadsOnlyToolStripMenuItem.CheckOnClick = true;
         this.showResolvedThreadsOnlyToolStripMenuItem.Name = "showResolvedThreadsOnlyToolStripMenuItem";
         this.showResolvedThreadsOnlyToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
         this.showResolvedThreadsOnlyToolStripMenuItem.Text = "Show resolved threads only";
         this.showResolvedThreadsOnlyToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onFilterByResolutionCheckedChanged);
         // 
         // showNotResolvedThreadsOnlyToolStripMenuItem
         // 
         this.showNotResolvedThreadsOnlyToolStripMenuItem.CheckOnClick = true;
         this.showNotResolvedThreadsOnlyToolStripMenuItem.Name = "showNotResolvedThreadsOnlyToolStripMenuItem";
         this.showNotResolvedThreadsOnlyToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
         this.showNotResolvedThreadsOnlyToolStripMenuItem.Text = "Show not resolved threads only";
         this.showNotResolvedThreadsOnlyToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onFilterByResolutionCheckedChanged);
         // 
         // toolStripSeparator2
         // 
         this.toolStripSeparator2.Name = "toolStripSeparator2";
         this.toolStripSeparator2.Size = new System.Drawing.Size(285, 6);
         // 
         // showAnsweredAndUnansweredThreadsToolStripMenuItem
         // 
         this.showAnsweredAndUnansweredThreadsToolStripMenuItem.CheckOnClick = true;
         this.showAnsweredAndUnansweredThreadsToolStripMenuItem.Name = "showAnsweredAndUnansweredThreadsToolStripMenuItem";
         this.showAnsweredAndUnansweredThreadsToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
         this.showAnsweredAndUnansweredThreadsToolStripMenuItem.Text = "Show answered and unanswered threads";
         this.showAnsweredAndUnansweredThreadsToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onFilterByAnswersCheckedChanged);
         // 
         // showAnsweredThreadsOnlyToolStripMenuItem
         // 
         this.showAnsweredThreadsOnlyToolStripMenuItem.CheckOnClick = true;
         this.showAnsweredThreadsOnlyToolStripMenuItem.Name = "showAnsweredThreadsOnlyToolStripMenuItem";
         this.showAnsweredThreadsOnlyToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
         this.showAnsweredThreadsOnlyToolStripMenuItem.Text = "Show answered threads only";
         this.showAnsweredThreadsOnlyToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onFilterByAnswersCheckedChanged);
         // 
         // showUnansweredThreadsOnlyToolStripMenuItem
         // 
         this.showUnansweredThreadsOnlyToolStripMenuItem.CheckOnClick = true;
         this.showUnansweredThreadsOnlyToolStripMenuItem.Name = "showUnansweredThreadsOnlyToolStripMenuItem";
         this.showUnansweredThreadsOnlyToolStripMenuItem.Size = new System.Drawing.Size(288, 22);
         this.showUnansweredThreadsOnlyToolStripMenuItem.Text = "Show unanswered threads only";
         this.showUnansweredThreadsOnlyToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onFilterByAnswersCheckedChanged);
         // 
         // actionsToolStripMenuItem
         // 
         this.actionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startAThreadToolStripMenuItem,
            this.addACommentToolStripMenuItem,
            this.toolStripMenuItemSeparator});
         this.actionsToolStripMenuItem.Name = "actionsToolStripMenuItem";
         this.actionsToolStripMenuItem.Size = new System.Drawing.Size(59, 24);
         this.actionsToolStripMenuItem.Text = "&Actions";
         // 
         // startAThreadToolStripMenuItem
         // 
         this.startAThreadToolStripMenuItem.Name = "startAThreadToolStripMenuItem";
         this.startAThreadToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
         this.startAThreadToolStripMenuItem.Text = "Start a thread";
         this.startAThreadToolStripMenuItem.Click += new System.EventHandler(this.onStartAThreadMenuItemClicked);
         // 
         // addACommentToolStripMenuItem
         // 
         this.addACommentToolStripMenuItem.Name = "addACommentToolStripMenuItem";
         this.addACommentToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
         this.addACommentToolStripMenuItem.Text = "Add a comment";
         this.addACommentToolStripMenuItem.Click += new System.EventHandler(this.onAddACommentMenuItemClicked);
         // 
         // toolStripMenuItemSeparator
         // 
         this.toolStripMenuItemSeparator.Name = "toolStripMenuItemSeparator";
         this.toolStripMenuItemSeparator.Size = new System.Drawing.Size(157, 6);
         // 
         // viewToolStripMenuItem
         // 
         this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.diffContextPositionToolStripMenuItem,
            this.flatListOfRepliesToolStripMenuItem,
            this.toolStripMenuItem1,
            this.increaseColumnWidthToolStripMenuItem,
            this.decreaseColumnWidthToolStripMenuItem,
            this.toolStripSeparator3,
            this.diffContextDepthToolStripMenuItem,
            this.showTooltipsForCodeToolStripMenuItem,
            this.emulateNativeLineBreaksToolStripMenuItem,
            this.toolStripSeparator4,
            this.fontSizeToolStripMenuItem,
            this.configureColorsToolStripMenuItem});
         this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
         this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
         this.viewToolStripMenuItem.Text = "&View";
         // 
         // diffContextPositionToolStripMenuItem
         // 
         this.diffContextPositionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.topToolStripMenuItem,
            this.leftToolStripMenuItem,
            this.rightToolStripMenuItem});
         this.diffContextPositionToolStripMenuItem.Name = "diffContextPositionToolStripMenuItem";
         this.diffContextPositionToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
         this.diffContextPositionToolStripMenuItem.Text = "Diff context position";
         // 
         // topToolStripMenuItem
         // 
         this.topToolStripMenuItem.Name = "topToolStripMenuItem";
         this.topToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Up)));
         this.topToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
         this.topToolStripMenuItem.Text = "Top";
         this.topToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onDiffContextPositionCheckedChanged);
         // 
         // leftToolStripMenuItem
         // 
         this.leftToolStripMenuItem.Name = "leftToolStripMenuItem";
         this.leftToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Left)));
         this.leftToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
         this.leftToolStripMenuItem.Text = "Left";
         this.leftToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onDiffContextPositionCheckedChanged);
         // 
         // rightToolStripMenuItem
         // 
         this.rightToolStripMenuItem.Name = "rightToolStripMenuItem";
         this.rightToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Right)));
         this.rightToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
         this.rightToolStripMenuItem.Text = "Right";
         this.rightToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onDiffContextPositionCheckedChanged);
         // 
         // flatListOfRepliesToolStripMenuItem
         // 
         this.flatListOfRepliesToolStripMenuItem.CheckOnClick = true;
         this.flatListOfRepliesToolStripMenuItem.Name = "flatListOfRepliesToolStripMenuItem";
         this.flatListOfRepliesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Down)));
         this.flatListOfRepliesToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
         this.flatListOfRepliesToolStripMenuItem.Text = "Flat list of replies";
         this.flatListOfRepliesToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onFlatListOfRepliesCheckedChanged);
         // 
         // toolStripMenuItem1
         // 
         this.toolStripMenuItem1.Name = "toolStripMenuItem1";
         this.toolStripMenuItem1.Size = new System.Drawing.Size(273, 6);
         // 
         // increaseColumnWidthToolStripMenuItem
         // 
         this.increaseColumnWidthToolStripMenuItem.Name = "increaseColumnWidthToolStripMenuItem";
         this.increaseColumnWidthToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Add)));
         this.increaseColumnWidthToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
         this.increaseColumnWidthToolStripMenuItem.Text = "Increase column width";
         this.increaseColumnWidthToolStripMenuItem.Click += new System.EventHandler(this.onIncreaseColumnWidthClicked);
         // 
         // decreaseColumnWidthToolStripMenuItem
         // 
         this.decreaseColumnWidthToolStripMenuItem.Name = "decreaseColumnWidthToolStripMenuItem";
         this.decreaseColumnWidthToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Subtract)));
         this.decreaseColumnWidthToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
         this.decreaseColumnWidthToolStripMenuItem.Text = "Decrease column width";
         this.decreaseColumnWidthToolStripMenuItem.Click += new System.EventHandler(this.onDecreaseColumnWidthClicked);
         // 
         // toolStripSeparator3
         // 
         this.toolStripSeparator3.Name = "toolStripSeparator3";
         this.toolStripSeparator3.Size = new System.Drawing.Size(273, 6);
         // 
         // diffContextDepthToolStripMenuItem
         // 
         this.diffContextDepthToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItemDiffContextDepth0,
            this.toolStripMenuItemDiffContextDepth1,
            this.toolStripMenuItemDiffContextDepth2,
            this.toolStripMenuItemDiffContextDepth3,
            this.toolStripMenuItemDiffContextDepth4});
         this.diffContextDepthToolStripMenuItem.Name = "diffContextDepthToolStripMenuItem";
         this.diffContextDepthToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
         this.diffContextDepthToolStripMenuItem.Text = "Diff context depth";
         // 
         // toolStripMenuItemDiffContextDepth0
         // 
         this.toolStripMenuItemDiffContextDepth0.CheckOnClick = true;
         this.toolStripMenuItemDiffContextDepth0.Name = "toolStripMenuItemDiffContextDepth0";
         this.toolStripMenuItemDiffContextDepth0.Size = new System.Drawing.Size(80, 22);
         this.toolStripMenuItemDiffContextDepth0.Text = "0";
         this.toolStripMenuItemDiffContextDepth0.CheckedChanged += new System.EventHandler(this.onDiffContextDepthCheckedChanged);
         // 
         // toolStripMenuItemDiffContextDepth1
         // 
         this.toolStripMenuItemDiffContextDepth1.CheckOnClick = true;
         this.toolStripMenuItemDiffContextDepth1.Name = "toolStripMenuItemDiffContextDepth1";
         this.toolStripMenuItemDiffContextDepth1.Size = new System.Drawing.Size(80, 22);
         this.toolStripMenuItemDiffContextDepth1.Text = "1";
         this.toolStripMenuItemDiffContextDepth1.CheckedChanged += new System.EventHandler(this.onDiffContextDepthCheckedChanged);
         // 
         // toolStripMenuItemDiffContextDepth2
         // 
         this.toolStripMenuItemDiffContextDepth2.CheckOnClick = true;
         this.toolStripMenuItemDiffContextDepth2.Name = "toolStripMenuItemDiffContextDepth2";
         this.toolStripMenuItemDiffContextDepth2.Size = new System.Drawing.Size(80, 22);
         this.toolStripMenuItemDiffContextDepth2.Text = "2";
         this.toolStripMenuItemDiffContextDepth2.CheckedChanged += new System.EventHandler(this.onDiffContextDepthCheckedChanged);
         // 
         // toolStripMenuItemDiffContextDepth3
         // 
         this.toolStripMenuItemDiffContextDepth3.CheckOnClick = true;
         this.toolStripMenuItemDiffContextDepth3.Name = "toolStripMenuItemDiffContextDepth3";
         this.toolStripMenuItemDiffContextDepth3.Size = new System.Drawing.Size(80, 22);
         this.toolStripMenuItemDiffContextDepth3.Text = "3";
         this.toolStripMenuItemDiffContextDepth3.CheckedChanged += new System.EventHandler(this.onDiffContextDepthCheckedChanged);
         // 
         // toolStripMenuItemDiffContextDepth4
         // 
         this.toolStripMenuItemDiffContextDepth4.CheckOnClick = true;
         this.toolStripMenuItemDiffContextDepth4.Name = "toolStripMenuItemDiffContextDepth4";
         this.toolStripMenuItemDiffContextDepth4.Size = new System.Drawing.Size(80, 22);
         this.toolStripMenuItemDiffContextDepth4.Text = "4";
         this.toolStripMenuItemDiffContextDepth4.CheckedChanged += new System.EventHandler(this.onDiffContextDepthCheckedChanged);
         // 
         // showTooltipsForCodeToolStripMenuItem
         // 
         this.showTooltipsForCodeToolStripMenuItem.CheckOnClick = true;
         this.showTooltipsForCodeToolStripMenuItem.Name = "showTooltipsForCodeToolStripMenuItem";
         this.showTooltipsForCodeToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
         this.showTooltipsForCodeToolStripMenuItem.Text = "Show tooltips for code";
         this.showTooltipsForCodeToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onShowTooltipsForCodeCheckedChanged);
         // 
         // emulateNativeLineBreaksToolStripMenuItem
         // 
         this.emulateNativeLineBreaksToolStripMenuItem.CheckOnClick = true;
         this.emulateNativeLineBreaksToolStripMenuItem.Name = "emulateNativeLineBreaksToolStripMenuItem";
         this.emulateNativeLineBreaksToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
         this.emulateNativeLineBreaksToolStripMenuItem.Text = "Emulate native line breaks";
         this.emulateNativeLineBreaksToolStripMenuItem.CheckedChanged += new System.EventHandler(this.onEmulateNativeLineBreaksCheckedChanged);
         // 
         // toolStripSeparator4
         // 
         this.toolStripSeparator4.Name = "toolStripSeparator4";
         this.toolStripSeparator4.Size = new System.Drawing.Size(273, 6);
         // 
         // fontSizeToolStripMenuItem
         // 
         this.fontSizeToolStripMenuItem.Name = "fontSizeToolStripMenuItem";
         this.fontSizeToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
         this.fontSizeToolStripMenuItem.Text = "Font Size";
         // 
         // configureColorsToolStripMenuItem
         // 
         this.configureColorsToolStripMenuItem.Name = "configureColorsToolStripMenuItem";
         this.configureColorsToolStripMenuItem.Size = new System.Drawing.Size(276, 22);
         this.configureColorsToolStripMenuItem.Text = "Configure colors...";
         this.configureColorsToolStripMenuItem.Click += new System.EventHandler(this.onConfigureColorsClicked);
         // 
         // DiscussionsFormMenu
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
         this.BackColor = System.Drawing.SystemColors.Menu;
         this.Controls.Add(this.menuStrip);
         this.Margin = new System.Windows.Forms.Padding(0);
         this.Name = "DiscussionsFormMenu";
         this.Size = new System.Drawing.Size(534, 24);
         this.menuStrip.ResumeLayout(false);
         this.menuStrip.PerformLayout();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private MenuStripEx menuStrip;
      private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem sortToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem defaultToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem reverseToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem byReviewerToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem filterToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem showThreadsStartedByMeOnlyToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem showServiceMessagesToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem showResolvedAndNotResolvedThreadsToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem showResolvedThreadsOnlyToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem showNotResolvedThreadsOnlyToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem showAnsweredAndUnansweredThreadsToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem showAnsweredThreadsOnlyToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem showUnansweredThreadsOnlyToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem actionsToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem startAThreadToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem addACommentToolStripMenuItem;
      private System.Windows.Forms.ToolStripSeparator toolStripMenuItemSeparator;
      private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
      private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
      private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem diffContextPositionToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem topToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem leftToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem rightToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem flatListOfRepliesToolStripMenuItem;
      private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
      private System.Windows.Forms.ToolStripMenuItem increaseColumnWidthToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem decreaseColumnWidthToolStripMenuItem;
      private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
      private System.Windows.Forms.ToolStripMenuItem emulateNativeLineBreaksToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem diffContextDepthToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemDiffContextDepth0;
      private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemDiffContextDepth1;
      private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemDiffContextDepth2;
      private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemDiffContextDepth3;
      private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemDiffContextDepth4;
      private System.Windows.Forms.ToolStripMenuItem showTooltipsForCodeToolStripMenuItem;
      private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
      private System.Windows.Forms.ToolStripMenuItem fontSizeToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem configureColorsToolStripMenuItem;
   }
}
