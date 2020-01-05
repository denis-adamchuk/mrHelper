namespace mrHelper.App.Controls
{
   partial class DiscussionSearchPanel
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
         this.checkBoxCaseSensitive = new System.Windows.Forms.CheckBox();
         this.labelFoundCount = new System.Windows.Forms.Label();
         this.buttonFindPrev = new System.Windows.Forms.Button();
         this.buttonFindNext = new System.Windows.Forms.Button();
         this.textBoxSearch = new System.Windows.Forms.TextBox();
         this.toolTipSearchPanel = new System.Windows.Forms.ToolTip(this.components);
         this.groupBox1.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.checkBoxCaseSensitive);
         this.groupBox1.Controls.Add(this.labelFoundCount);
         this.groupBox1.Controls.Add(this.buttonFindPrev);
         this.groupBox1.Controls.Add(this.buttonFindNext);
         this.groupBox1.Controls.Add(this.textBoxSearch);
         this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBox1.Location = new System.Drawing.Point(0, 0);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(418, 85);
         this.groupBox1.TabIndex = 0;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "Search";
         // 
         // checkBoxCaseSensitive
         // 
         this.checkBoxCaseSensitive.AutoSize = true;
         this.checkBoxCaseSensitive.Location = new System.Drawing.Point(6, 45);
         this.checkBoxCaseSensitive.Name = "checkBoxCaseSensitive";
         this.checkBoxCaseSensitive.Size = new System.Drawing.Size(94, 17);
         this.checkBoxCaseSensitive.TabIndex = 4;
         this.checkBoxCaseSensitive.Text = "Case-sensitive";
         this.checkBoxCaseSensitive.UseVisualStyleBackColor = true;
         this.checkBoxCaseSensitive.CheckedChanged += new System.EventHandler(this.checkBoxCaseSensitive_CheckedChanged);
         // 
         // labelFoundCount
         // 
         this.labelFoundCount.AutoSize = true;
         this.labelFoundCount.Location = new System.Drawing.Point(3, 68);
         this.labelFoundCount.Name = "labelFoundCount";
         this.labelFoundCount.Size = new System.Drawing.Size(79, 13);
         this.labelFoundCount.TabIndex = 3;
         this.labelFoundCount.Text = "Found 0 results";
         this.labelFoundCount.Visible = false;
         // 
         // buttonFindPrev
         // 
         this.buttonFindPrev.Location = new System.Drawing.Point(335, 16);
         this.buttonFindPrev.Name = "buttonFindPrev";
         this.buttonFindPrev.Size = new System.Drawing.Size(75, 23);
         this.buttonFindPrev.TabIndex = 2;
         this.buttonFindPrev.Text = "Find prev";
         this.toolTipSearchPanel.SetToolTip(this.buttonFindPrev, "Find previous occurrence (Shift-F3)");
         this.buttonFindPrev.UseVisualStyleBackColor = true;
         this.buttonFindPrev.Click += new System.EventHandler(this.buttonFindPrev_Click);
         // 
         // buttonFindNext
         // 
         this.buttonFindNext.Location = new System.Drawing.Point(254, 17);
         this.buttonFindNext.Name = "buttonFindNext";
         this.buttonFindNext.Size = new System.Drawing.Size(75, 23);
         this.buttonFindNext.TabIndex = 1;
         this.buttonFindNext.Text = "Find next";
         this.toolTipSearchPanel.SetToolTip(this.buttonFindNext, "Find next occurrence (F3)");
         this.buttonFindNext.UseVisualStyleBackColor = true;
         this.buttonFindNext.Click += new System.EventHandler(this.ButtonFind_Click);
         // 
         // textBoxSearch
         // 
         this.textBoxSearch.Location = new System.Drawing.Point(6, 19);
         this.textBoxSearch.Name = "textBoxSearch";
         this.textBoxSearch.Size = new System.Drawing.Size(242, 20);
         this.textBoxSearch.TabIndex = 0;
         this.toolTipSearchPanel.SetToolTip(this.textBoxSearch, "Search text in discussions and comments.");
         this.textBoxSearch.TextChanged += new System.EventHandler(this.textBoxSearch_TextChanged);
         this.textBoxSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxSearch_KeyDown);
         // 
         // DiscussionSearchPanel
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
         this.Controls.Add(this.groupBox1);
         this.Name = "DiscussionSearchPanel";
         this.Size = new System.Drawing.Size(418, 85);
         this.groupBox1.ResumeLayout(false);
         this.groupBox1.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.Button buttonFindNext;
      private System.Windows.Forms.TextBox textBoxSearch;
      private System.Windows.Forms.Button buttonFindPrev;
      private System.Windows.Forms.ToolTip toolTipSearchPanel;
      private System.Windows.Forms.Label labelFoundCount;
      private System.Windows.Forms.CheckBox checkBoxCaseSensitive;
   }
}
