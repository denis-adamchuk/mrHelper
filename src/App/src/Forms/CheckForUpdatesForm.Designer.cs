namespace mrHelper.App.Forms
{
   partial class CheckForUpdatesForm
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

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.components = new System.ComponentModel.Container();
         this.labelStatus = new System.Windows.Forms.Label();
         this.buttonUpgradeNow = new System.Windows.Forms.Button();
         this.buttonRemindLater = new System.Windows.Forms.Button();
         this.toolTip = new Controls.ThemedToolTip(this.components);
         this.SuspendLayout();
         // 
         // labelStatus
         // 
         this.labelStatus.AutoSize = true;
         this.labelStatus.Location = new System.Drawing.Point(12, 9);
         this.labelStatus.Name = "labelStatus";
         this.labelStatus.Size = new System.Drawing.Size(47, 13);
         this.labelStatus.TabIndex = 0;
         this.labelStatus.Text = "<status>";
         // 
         // buttonUpgradeNow
         // 
         this.buttonUpgradeNow.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonUpgradeNow.Location = new System.Drawing.Point(277, 40);
         this.buttonUpgradeNow.Name = "buttonUpgradeNow";
         this.buttonUpgradeNow.Size = new System.Drawing.Size(91, 23);
         this.buttonUpgradeNow.TabIndex = 1;
         this.buttonUpgradeNow.Text = "Upgrade now";
         this.toolTip.SetToolTip(this.buttonUpgradeNow, "Close application and update it to a new version");
         this.buttonUpgradeNow.UseVisualStyleBackColor = true;
         // 
         // buttonRemindLater
         // 
         this.buttonRemindLater.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonRemindLater.Location = new System.Drawing.Point(407, 40);
         this.buttonRemindLater.Name = "buttonRemindLater";
         this.buttonRemindLater.Size = new System.Drawing.Size(90, 23);
         this.buttonRemindLater.TabIndex = 2;
         this.buttonRemindLater.Text = "Remind later";
         this.toolTip.SetToolTip(this.buttonRemindLater, "Remind about available new version in 24 hours");
         this.buttonRemindLater.UseVisualStyleBackColor = true;
         // 
         // CheckForUpdatesForm
         // 
         this.AcceptButton = this.buttonUpgradeNow;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonRemindLater;
         this.ClientSize = new System.Drawing.Size(509, 74);
         this.Controls.Add(this.buttonUpgradeNow);
         this.Controls.Add(this.buttonRemindLater);
         this.Controls.Add(this.labelStatus);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "CheckForUpdatesForm";
         this.Load += new System.EventHandler(this.CheckForUpdatesForm_Load);
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Label labelStatus;
      private System.Windows.Forms.Button buttonUpgradeNow;
      private System.Windows.Forms.Button buttonRemindLater;
      private Controls.ThemedToolTip toolTip;
   }
}