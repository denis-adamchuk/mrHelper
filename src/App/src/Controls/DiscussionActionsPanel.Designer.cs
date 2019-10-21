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
         this.buttonDiscussionsRefresh = new System.Windows.Forms.Button();
         this.toolTipActionsPanel = new System.Windows.Forms.ToolTip(this.components);
         this.groupBox1.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.buttonDiscussionsRefresh);
         this.groupBox1.Location = new System.Drawing.Point(0, 0);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(89, 53);
         this.groupBox1.TabIndex = 0;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "Actions";
         // 
         // buttonDiscussionsRefresh
         // 
         this.buttonDiscussionsRefresh.Location = new System.Drawing.Point(6, 19);
         this.buttonDiscussionsRefresh.Name = "buttonDiscussionsRefresh";
         this.buttonDiscussionsRefresh.Size = new System.Drawing.Size(75, 23);
         this.buttonDiscussionsRefresh.TabIndex = 0;
         this.buttonDiscussionsRefresh.Text = "Refresh";
         this.toolTipActionsPanel.SetToolTip(this.buttonDiscussionsRefresh, "Reload discussions from Server");
         this.buttonDiscussionsRefresh.UseVisualStyleBackColor = true;
         this.buttonDiscussionsRefresh.Click += new System.EventHandler(this.ButtonDiscussionsRefresh_Click);
         // 
         // DiscussionActionsPanel
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.groupBox1);
         this.Name = "DiscussionActionsPanel";
         this.Size = new System.Drawing.Size(90, 54);
         this.groupBox1.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.Button buttonDiscussionsRefresh;
      private System.Windows.Forms.ToolTip toolTipActionsPanel;
   }
}
