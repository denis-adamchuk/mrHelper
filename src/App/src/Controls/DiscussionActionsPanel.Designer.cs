namespace mrHelper.App.Controls
{
   partial class DiscussionActionsPanel
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
         this.components = new System.ComponentModel.Container();
         this.groupBox1 = new System.Windows.Forms.GroupBox();
         this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
         this.buttonNewThread = new System.Windows.Forms.Button();
         this.buttonDiscussionsRefresh = new System.Windows.Forms.Button();
         this.buttonAddComment = new System.Windows.Forms.Button();
         this.toolTipActionsPanel = new System.Windows.Forms.ToolTip(this.components);
         this.groupBox1.SuspendLayout();
         this.tableLayoutPanel1.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.tableLayoutPanel1);
         this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBox1.Location = new System.Drawing.Point(0, 0);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(320, 139);
         this.groupBox1.TabIndex = 0;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "Actions";
         // 
         // tableLayoutPanel1
         // 
         this.tableLayoutPanel1.ColumnCount = 1;
         this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
         this.tableLayoutPanel1.Controls.Add(this.buttonNewThread, 0, 2);
         this.tableLayoutPanel1.Controls.Add(this.buttonDiscussionsRefresh, 0, 0);
         this.tableLayoutPanel1.Controls.Add(this.buttonAddComment, 0, 1);
         this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 16);
         this.tableLayoutPanel1.Name = "tableLayoutPanel1";
         this.tableLayoutPanel1.RowCount = 3;
         this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33395F));
         this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33395F));
         this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.3321F));
         this.tableLayoutPanel1.Size = new System.Drawing.Size(314, 120);
         this.tableLayoutPanel1.TabIndex = 4;
         // 
         // buttonNewThread
         // 
         this.buttonNewThread.Dock = System.Windows.Forms.DockStyle.Fill;
         this.buttonNewThread.Location = new System.Drawing.Point(3, 83);
         this.buttonNewThread.Name = "buttonNewThread";
         this.buttonNewThread.Size = new System.Drawing.Size(308, 34);
         this.buttonNewThread.TabIndex = 3;
         this.buttonNewThread.Text = "Start a thread";
         this.toolTipActionsPanel.SetToolTip(this.buttonNewThread, "Create a new resolvable thread");
         this.buttonNewThread.UseVisualStyleBackColor = true;
         this.buttonNewThread.Click += new System.EventHandler(this.buttonNewThread_Click);
         // 
         // buttonDiscussionsRefresh
         // 
         this.buttonDiscussionsRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
         this.buttonDiscussionsRefresh.Location = new System.Drawing.Point(3, 3);
         this.buttonDiscussionsRefresh.Name = "buttonDiscussionsRefresh";
         this.buttonDiscussionsRefresh.Size = new System.Drawing.Size(308, 34);
         this.buttonDiscussionsRefresh.TabIndex = 1;
         this.buttonDiscussionsRefresh.Text = "Refresh";
         this.toolTipActionsPanel.SetToolTip(this.buttonDiscussionsRefresh, "Reload discussions from Server");
         this.buttonDiscussionsRefresh.UseVisualStyleBackColor = true;
         this.buttonDiscussionsRefresh.Click += new System.EventHandler(this.ButtonDiscussionsRefresh_Click);
         // 
         // buttonAddComment
         // 
         this.buttonAddComment.Dock = System.Windows.Forms.DockStyle.Fill;
         this.buttonAddComment.Location = new System.Drawing.Point(3, 43);
         this.buttonAddComment.Name = "buttonAddComment";
         this.buttonAddComment.Size = new System.Drawing.Size(308, 34);
         this.buttonAddComment.TabIndex = 2;
         this.buttonAddComment.Text = "Add a comment";
         this.toolTipActionsPanel.SetToolTip(this.buttonAddComment, "Add a comment (cannot be resolved and replied)");
         this.buttonAddComment.UseVisualStyleBackColor = true;
         this.buttonAddComment.Click += new System.EventHandler(this.buttonAddComment_Click);
         // 
         // DiscussionActionsPanel
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
         this.Controls.Add(this.groupBox1);
         this.Name = "DiscussionActionsPanel";
         this.Size = new System.Drawing.Size(120, 139);
         this.groupBox1.ResumeLayout(false);
         this.tableLayoutPanel1.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.ToolTip toolTipActionsPanel;
      private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
      private System.Windows.Forms.Button buttonNewThread;
      private System.Windows.Forms.Button buttonDiscussionsRefresh;
      private System.Windows.Forms.Button buttonAddComment;
   }
}
