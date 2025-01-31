using mrHelper.App.Controls;

namespace mrHelper.App.Forms
{
   partial class DiscussionsForm
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

         _discussionLayout.DiffContextPositionChanged += updateSaveDefaultLayoutState;
         _discussionLayout.DiscussionColumnWidthChanged += updateSaveDefaultLayoutState;
         _discussionLayout.NeedShiftRepliesChanged += updateSaveDefaultLayoutState;

         _discussionLoader.StatusChanged -= onDiscussionLoaderStatusChanged;

         Program.Settings.DiffContextPositionChanged -= updateSaveDefaultLayoutState;
         Program.Settings.DiscussionColumnWidthChanged -= updateSaveDefaultLayoutState;
         Program.Settings.NeedShiftRepliesChanged -= updateSaveDefaultLayoutState;

         toolTip.Dispose();

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
         this.toolTip = new Controls.ThemedToolTip(this.components);
         this.discussionPanel = new mrHelper.App.Controls.DiscussionPanel();
         this.discussionMenu = new mrHelper.App.Controls.DiscussionsFormMenu();
         this.searchPanel = new mrHelper.App.Controls.DiscussionSearchPanel();
         this.panelHeader = new System.Windows.Forms.Panel();
         this.linkLabelPrevPage = new System.Windows.Forms.LinkLabel();
         this.linkLabelNextPage = new System.Windows.Forms.LinkLabel();
         this.linkLabelReapplyFilter = new System.Windows.Forms.LinkLabel();
         this.linkLabelSaveAsDefaultLayout = new System.Windows.Forms.LinkLabel();
         this.linkLabelGitLabURL = new CommonControls.Controls.LinkLabelEx();
         this.panelHeader.SuspendLayout();
         this.SuspendLayout();
         // 
         // discussionPanel
         // 
         this.discussionPanel.AutoScroll = true;
         this.discussionPanel.AutoScrollMargin = new System.Drawing.Size(0, 200);
         this.discussionPanel.Dock = System.Windows.Forms.DockStyle.Fill;
         this.discussionPanel.Location = new System.Drawing.Point(0, 43);
         this.discussionPanel.Name = "discussionPanel";
         this.discussionPanel.Size = new System.Drawing.Size(519, 187);
         this.discussionPanel.TabIndex = 7;
         // 
         // discussionMenu
         // 
         this.discussionMenu.Dock = System.Windows.Forms.DockStyle.Top;
         this.discussionMenu.Font = new System.Drawing.Font("Segoe UI", 9F);
         this.discussionMenu.Location = new System.Drawing.Point(0, 0);
         this.discussionMenu.Margin = new System.Windows.Forms.Padding(0);
         this.discussionMenu.Name = "discussionMenu";
         this.discussionMenu.Size = new System.Drawing.Size(519, 24);
         this.discussionMenu.TabIndex = 6;
         // 
         // searchPanel
         // 
         this.searchPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
         this.searchPanel.Location = new System.Drawing.Point(0, 230);
         this.searchPanel.Name = "searchPanel";
         this.searchPanel.Size = new System.Drawing.Size(519, 38);
         this.searchPanel.TabIndex = 6;
         // 
         // panelHeader
         // 
         this.panelHeader.Controls.Add(this.linkLabelPrevPage);
         this.panelHeader.Controls.Add(this.linkLabelNextPage);
         this.panelHeader.Controls.Add(this.linkLabelReapplyFilter);
         this.panelHeader.Controls.Add(this.linkLabelSaveAsDefaultLayout);
         this.panelHeader.Controls.Add(this.linkLabelGitLabURL);
         this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
         this.panelHeader.Location = new System.Drawing.Point(0, 24);
         this.panelHeader.Margin = new System.Windows.Forms.Padding(0);
         this.panelHeader.Name = "panelHeader";
         this.panelHeader.Padding = new System.Windows.Forms.Padding(2);
         this.panelHeader.Size = new System.Drawing.Size(519, 19);
         this.panelHeader.TabIndex = 8;
         // 
         // linkLabelPrevPage
         // 
         this.linkLabelPrevPage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelPrevPage.AutoSize = true;
         this.linkLabelPrevPage.Location = new System.Drawing.Point(172, 3);
         this.linkLabelPrevPage.Name = "linkLabelPrevPage";
         this.linkLabelPrevPage.Size = new System.Drawing.Size(29, 13);
         this.linkLabelPrevPage.TabIndex = 7;
         this.linkLabelPrevPage.TabStop = true;
         this.linkLabelPrevPage.Text = "Prev";
         this.linkLabelPrevPage.Visible = false;
         this.linkLabelPrevPage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.onPrevPageClicked);
         // 
         // linkLabelNextPage
         // 
         this.linkLabelNextPage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelNextPage.AutoSize = true;
         this.linkLabelNextPage.Location = new System.Drawing.Point(207, 3);
         this.linkLabelNextPage.Name = "linkLabelNextPage";
         this.linkLabelNextPage.Size = new System.Drawing.Size(29, 13);
         this.linkLabelNextPage.TabIndex = 6;
         this.linkLabelNextPage.TabStop = true;
         this.linkLabelNextPage.Text = "Next";
         this.linkLabelNextPage.Visible = false;
         this.linkLabelNextPage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.onNextPageClicked);
         // 
         // linkLabelReapplyFilter
         // 
         this.linkLabelReapplyFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelReapplyFilter.AutoSize = true;
         this.linkLabelReapplyFilter.Location = new System.Drawing.Point(284, 3);
         this.linkLabelReapplyFilter.Name = "linkLabelReapplyFilter";
         this.linkLabelReapplyFilter.Size = new System.Drawing.Size(71, 13);
         this.linkLabelReapplyFilter.TabIndex = 5;
         this.linkLabelReapplyFilter.TabStop = true;
         this.linkLabelReapplyFilter.Text = "Re-apply filter";
         this.linkLabelReapplyFilter.Visible = false;
         this.linkLabelReapplyFilter.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.onReaplyFilterClicked);
         // 
         // linkLabelSaveAsDefaultLayout
         // 
         this.linkLabelSaveAsDefaultLayout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.linkLabelSaveAsDefaultLayout.AutoSize = true;
         this.linkLabelSaveAsDefaultLayout.Location = new System.Drawing.Point(402, 2);
         this.linkLabelSaveAsDefaultLayout.Name = "linkLabelSaveAsDefaultLayout";
         this.linkLabelSaveAsDefaultLayout.Size = new System.Drawing.Size(112, 13);
         this.linkLabelSaveAsDefaultLayout.TabIndex = 4;
         this.linkLabelSaveAsDefaultLayout.TabStop = true;
         this.linkLabelSaveAsDefaultLayout.Text = "Save as default layout";
         this.linkLabelSaveAsDefaultLayout.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.onSaveAsDefaultClicked);
         // 
         // linkLabelGitLabURL
         // 
         this.linkLabelGitLabURL.AutoSize = true;
         this.linkLabelGitLabURL.Location = new System.Drawing.Point(9, 2);
         this.linkLabelGitLabURL.Margin = new System.Windows.Forms.Padding(0);
         this.linkLabelGitLabURL.Name = "linkLabelGitLabURL";
         this.linkLabelGitLabURL.Size = new System.Drawing.Size(100, 13);
         this.linkLabelGitLabURL.TabIndex = 2;
         this.linkLabelGitLabURL.TabStop = true;
         this.linkLabelGitLabURL.Text = "<merge-request-url>";
         // 
         // DiscussionsForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.AutoScroll = true;
         this.ClientSize = new System.Drawing.Size(519, 268);
         this.Controls.Add(this.discussionPanel);
         this.Controls.Add(this.panelHeader);
         this.Controls.Add(this.discussionMenu);
         this.Controls.Add(this.searchPanel);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.KeyPreview = true;
         this.MinimumSize = new System.Drawing.Size(535, 307);
         this.Name = "DiscussionsForm";
         this.Text = "Discussions";
         this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
         this.panelHeader.ResumeLayout(false);
         this.panelHeader.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private Controls.ThemedToolTip toolTip;
      private mrHelper.App.Controls.DiscussionPanel discussionPanel;
      private mrHelper.App.Controls.DiscussionsFormMenu discussionMenu;
      private Controls.DiscussionSearchPanel searchPanel;
      private System.Windows.Forms.Panel panelHeader;
      private CommonControls.Controls.LinkLabelEx linkLabelGitLabURL;
      private System.Windows.Forms.LinkLabel linkLabelSaveAsDefaultLayout;
      private System.Windows.Forms.LinkLabel linkLabelReapplyFilter;
      private System.Windows.Forms.LinkLabel linkLabelPrevPage;
      private System.Windows.Forms.LinkLabel linkLabelNextPage;
   }
}
