﻿using mrHelper.CommonControls.Controls;

namespace mrHelper.App.Controls
{
   internal partial class RevisionSplitContainerSite
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
         this.splitContainer = new System.Windows.Forms.SplitContainer();
         this.revisionBrowser = new mrHelper.App.Controls.RevisionBrowser();
         this.panelLoading = new System.Windows.Forms.Panel();
         this.labelLoading = new System.Windows.Forms.Label();
         this.listViewRevisionComparisonStructure = new mrHelper.App.Controls.RevisionComparisonListView();
         this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderAdded = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderDeleted = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
         this.splitContainer.Panel1.SuspendLayout();
         this.splitContainer.Panel2.SuspendLayout();
         this.splitContainer.SuspendLayout();
         this.panelLoading.SuspendLayout();
         this.SuspendLayout();
         // 
         // splitContainer
         // 
         this.splitContainer.BackColor = System.Drawing.Color.LightGray;
         this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
         this.splitContainer.Location = new System.Drawing.Point(0, 0);
         this.splitContainer.Name = "splitContainer";
         this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
         // 
         // splitContainer.Panel1
         // 
         this.splitContainer.Panel1.BackColor = System.Drawing.SystemColors.Window;
         this.splitContainer.Panel1.Controls.Add(this.revisionBrowser);
         // 
         // splitContainer.Panel2
         // 
         this.splitContainer.Panel2.BackColor = System.Drawing.SystemColors.Window;
         this.splitContainer.Panel2.Controls.Add(this.panelLoading);
         this.splitContainer.Panel2.Controls.Add(this.listViewRevisionComparisonStructure);
         this.splitContainer.Size = new System.Drawing.Size(396, 374);
         this.splitContainer.SplitterDistance = 124;
         this.splitContainer.TabIndex = 0;
         // 
         // revisionBrowser
         // 
         this.revisionBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
         this.revisionBrowser.Location = new System.Drawing.Point(0, 0);
         this.revisionBrowser.Name = "revisionBrowser";
         this.revisionBrowser.Size = new System.Drawing.Size(396, 124);
         this.revisionBrowser.TabIndex = 0;
         this.revisionBrowser.SelectionChanged += new System.EventHandler(this.revisionBrowser_SelectionChanged);
         // 
         // panelLoading
         // 
         this.panelLoading.Controls.Add(this.labelLoading);
         this.panelLoading.Dock = System.Windows.Forms.DockStyle.Fill;
         this.panelLoading.Location = new System.Drawing.Point(0, 0);
         this.panelLoading.Name = "panelLoading";
         this.panelLoading.Size = new System.Drawing.Size(396, 246);
         this.panelLoading.TabIndex = 2;
         this.panelLoading.Visible = false;
         this.panelLoading.SizeChanged += new System.EventHandler(this.panelLoading_SizeChanged);
         // 
         // labelLoading
         // 
         this.labelLoading.AutoSize = true;
         this.labelLoading.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.labelLoading.Location = new System.Drawing.Point(167, 109);
         this.labelLoading.Name = "labelLoading";
         this.labelLoading.Size = new System.Drawing.Size(54, 13);
         this.labelLoading.TabIndex = 3;
         this.labelLoading.Text = "Loading...";
         // 
         // listViewRevisionComparisonStructure
         // 
         this.listViewRevisionComparisonStructure.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName,
            this.columnHeaderAdded,
            this.columnHeaderDeleted});
         this.listViewRevisionComparisonStructure.Dock = System.Windows.Forms.DockStyle.Fill;
         this.listViewRevisionComparisonStructure.FullRowSelect = true;
         this.listViewRevisionComparisonStructure.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
         this.listViewRevisionComparisonStructure.HideSelection = false;
         this.listViewRevisionComparisonStructure.Location = new System.Drawing.Point(0, 0);
         this.listViewRevisionComparisonStructure.Name = "listViewRevisionComparisonStructure";
         this.listViewRevisionComparisonStructure.ShowGroups = false;
         this.listViewRevisionComparisonStructure.Size = new System.Drawing.Size(396, 246);
         this.listViewRevisionComparisonStructure.TabIndex = 1;
         this.listViewRevisionComparisonStructure.UseCompatibleStateImageBehavior = false;
         this.listViewRevisionComparisonStructure.View = System.Windows.Forms.View.Details;
         // 
         // columnHeaderName
         // 
         this.columnHeaderName.Tag = "Name";
         this.columnHeaderName.Text = "Name";
         this.columnHeaderName.Width = 240;
         // 
         // columnHeaderAdded
         // 
         this.columnHeaderAdded.Tag = "Added";
         this.columnHeaderAdded.Text = "Added";
         this.columnHeaderAdded.Width = 40;
         // 
         // columnHeaderDeleted
         // 
         this.columnHeaderDeleted.Tag = "Deleted";
         this.columnHeaderDeleted.Text = "Deleted";
         this.columnHeaderDeleted.Width = 40;
         // 
         // RevisionSplitContainerSite
         // 
         this.Controls.Add(this.splitContainer);
         this.Name = "RevisionSplitContainerSite";
         this.Size = new System.Drawing.Size(396, 374);
         this.splitContainer.Panel1.ResumeLayout(false);
         this.splitContainer.Panel2.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
         this.splitContainer.ResumeLayout(false);
         this.panelLoading.ResumeLayout(false);
         this.panelLoading.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.SplitContainer splitContainer;
      private App.Controls.RevisionBrowser revisionBrowser;
      private RevisionComparisonListView listViewRevisionComparisonStructure;
      private System.Windows.Forms.ColumnHeader columnHeaderName;
      private System.Windows.Forms.ColumnHeader columnHeaderAdded;
      private System.Windows.Forms.ColumnHeader columnHeaderDeleted;
      private System.Windows.Forms.Panel panelLoading;
      private System.Windows.Forms.Label labelLoading;
   }
}
