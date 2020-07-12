namespace mrHelper.App.Controls
{
   partial class RevisionBrowser
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
         this._treeView = new Aga.Controls.Tree.TreeViewAdv();
         this.treeColumn1 = new Aga.Controls.Tree.TreeColumn();
         this.treeColumn2 = new Aga.Controls.Tree.TreeColumn();
         this._name = new Aga.Controls.Tree.NodeControls.NodeTextBox();
         this._timestamp = new Aga.Controls.Tree.NodeControls.NodeTextBox();
         this.SuspendLayout();
         // 
         // _treeView
         // 
         this._treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this._treeView.AutoRowHeight = true;
         this._treeView.BackColor = System.Drawing.SystemColors.Window;
         this._treeView.Columns.Add(this.treeColumn1);
         this._treeView.Columns.Add(this.treeColumn2);
         this._treeView.Cursor = System.Windows.Forms.Cursors.Default;
         this._treeView.DefaultToolTipProvider = null;
         this._treeView.DragDropMarkColor = System.Drawing.Color.Black;
         this._treeView.FullRowSelect = true;
         this._treeView.GridLineStyle = ((Aga.Controls.Tree.GridLineStyle)((Aga.Controls.Tree.GridLineStyle.Horizontal | Aga.Controls.Tree.GridLineStyle.Vertical)));
         this._treeView.HideSelection = true;
         this._treeView.LineColor = System.Drawing.SystemColors.ControlDark;
         this._treeView.Location = new System.Drawing.Point(0, 0);
         this._treeView.Model = null;
         this._treeView.Name = "_treeView";
         this._treeView.NodeControls.Add(this._name);
         this._treeView.NodeControls.Add(this._timestamp);
         this._treeView.SelectedNode = null;
         this._treeView.SelectionMode = Aga.Controls.Tree.TreeSelectionMode.MultiSameParent;
         this._treeView.ShowNodeToolTips = true;
         this._treeView.Size = new System.Drawing.Size(100, 100);
         this._treeView.TabIndex = 0;
         this._treeView.UseColumns = true;
         this._treeView.ColumnWidthChanged += new System.EventHandler<Aga.Controls.Tree.TreeColumnEventArgs>(this.treeView_ColumnWidthChanged);
         // 
         // treeColumn1
         // 
         this.treeColumn1.Header = "Name";
         this.treeColumn1.SortOrder = System.Windows.Forms.SortOrder.None;
         this.treeColumn1.TooltipText = "Commit message title or version number";
         this.treeColumn1.Width = 300;
         // 
         // treeColumn2
         // 
         this.treeColumn2.Header = "Timestamp";
         this.treeColumn2.SortOrder = System.Windows.Forms.SortOrder.None;
         this.treeColumn2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
         this.treeColumn2.TooltipText = "Created at";
         this.treeColumn2.Width = 140;
         // 
         // _name
         // 
         this._name.DataPropertyName = "Name";
         this._name.IncrementalSearchEnabled = true;
         this._name.LeftMargin = 3;
         this._name.ParentColumn = this.treeColumn1;
         this._name.Trimming = System.Drawing.StringTrimming.EllipsisCharacter;
         this._name.UseCompatibleTextRendering = true;
         // 
         // _timestamp
         // 
         this._timestamp.DataPropertyName = "Timestamp";
         this._timestamp.IncrementalSearchEnabled = true;
         this._timestamp.LeftMargin = 3;
         this._timestamp.ParentColumn = this.treeColumn2;
         this._timestamp.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
         // 
         // RevisionBrowser
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
         this.Controls.Add(this._treeView);
         this.Name = "RevisionBrowser";
         this.Size = new System.Drawing.Size(100, 100);
         this.Load += new System.EventHandler(this.RevisionBrowser_Load);
         this.ResumeLayout(false);

      }

      #endregion

      private Aga.Controls.Tree.TreeViewAdv _treeView;
      private Aga.Controls.Tree.NodeControls.NodeTextBox _name;
      private Aga.Controls.Tree.NodeControls.NodeTextBox _timestamp;
      private Aga.Controls.Tree.TreeColumn treeColumn1;
      private Aga.Controls.Tree.TreeColumn treeColumn2;
   }
}
