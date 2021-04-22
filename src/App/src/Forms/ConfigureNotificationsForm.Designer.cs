
namespace mrHelper.App.Forms
{
   partial class ConfigureNotificationsForm
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
         this.groupBoxNotifications = new System.Windows.Forms.GroupBox();
         this.checkBoxShowMergedMergeRequests = new System.Windows.Forms.CheckBox();
         this.checkBoxShowServiceNotifications = new System.Windows.Forms.CheckBox();
         this.checkBoxShowNewMergeRequests = new System.Windows.Forms.CheckBox();
         this.checkBoxShowMyActivity = new System.Windows.Forms.CheckBox();
         this.checkBoxShowUpdatedMergeRequests = new System.Windows.Forms.CheckBox();
         this.checkBoxShowKeywords = new System.Windows.Forms.CheckBox();
         this.checkBoxShowResolvedAll = new System.Windows.Forms.CheckBox();
         this.checkBoxShowOnMention = new System.Windows.Forms.CheckBox();
         this.buttonOK = new System.Windows.Forms.Button();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.groupBoxNotifications.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxNotifications
         // 
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowMergedMergeRequests);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowServiceNotifications);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowNewMergeRequests);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowMyActivity);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowUpdatedMergeRequests);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowKeywords);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowResolvedAll);
         this.groupBoxNotifications.Controls.Add(this.checkBoxShowOnMention);
         this.groupBoxNotifications.Location = new System.Drawing.Point(12, 12);
         this.groupBoxNotifications.Name = "groupBoxNotifications";
         this.groupBoxNotifications.Size = new System.Drawing.Size(577, 132);
         this.groupBoxNotifications.TabIndex = 0;
         this.groupBoxNotifications.TabStop = false;
         this.groupBoxNotifications.Text = "Notifications";
         // 
         // checkBoxShowMergedMergeRequests
         // 
         this.checkBoxShowMergedMergeRequests.AutoSize = true;
         this.checkBoxShowMergedMergeRequests.Location = new System.Drawing.Point(6, 41);
         this.checkBoxShowMergedMergeRequests.Name = "checkBoxShowMergedMergeRequests";
         this.checkBoxShowMergedMergeRequests.Size = new System.Drawing.Size(307, 17);
         this.checkBoxShowMergedMergeRequests.TabIndex = 2;
         this.checkBoxShowMergedMergeRequests.Text = "Merged or closed Merge Requests (project-based workflow)";
         this.checkBoxShowMergedMergeRequests.UseVisualStyleBackColor = true;
         // 
         // checkBoxShowServiceNotifications
         // 
         this.checkBoxShowServiceNotifications.AutoSize = true;
         this.checkBoxShowServiceNotifications.Location = new System.Drawing.Point(319, 105);
         this.checkBoxShowServiceNotifications.Name = "checkBoxShowServiceNotifications";
         this.checkBoxShowServiceNotifications.Size = new System.Drawing.Size(149, 17);
         this.checkBoxShowServiceNotifications.TabIndex = 7;
         this.checkBoxShowServiceNotifications.Text = "Show service notifications";
         this.checkBoxShowServiceNotifications.UseVisualStyleBackColor = true;
         // 
         // checkBoxShowNewMergeRequests
         // 
         this.checkBoxShowNewMergeRequests.AutoSize = true;
         this.checkBoxShowNewMergeRequests.Location = new System.Drawing.Point(6, 18);
         this.checkBoxShowNewMergeRequests.Name = "checkBoxShowNewMergeRequests";
         this.checkBoxShowNewMergeRequests.Size = new System.Drawing.Size(129, 17);
         this.checkBoxShowNewMergeRequests.TabIndex = 0;
         this.checkBoxShowNewMergeRequests.Text = "New Merge Requests";
         this.checkBoxShowNewMergeRequests.UseVisualStyleBackColor = true;
         // 
         // checkBoxShowMyActivity
         // 
         this.checkBoxShowMyActivity.AutoSize = true;
         this.checkBoxShowMyActivity.Location = new System.Drawing.Point(6, 105);
         this.checkBoxShowMyActivity.Name = "checkBoxShowMyActivity";
         this.checkBoxShowMyActivity.Size = new System.Drawing.Size(113, 17);
         this.checkBoxShowMyActivity.TabIndex = 6;
         this.checkBoxShowMyActivity.Text = "Include my activity";
         this.checkBoxShowMyActivity.UseVisualStyleBackColor = true;
         // 
         // checkBoxShowUpdatedMergeRequests
         // 
         this.checkBoxShowUpdatedMergeRequests.AutoSize = true;
         this.checkBoxShowUpdatedMergeRequests.Location = new System.Drawing.Point(6, 64);
         this.checkBoxShowUpdatedMergeRequests.Name = "checkBoxShowUpdatedMergeRequests";
         this.checkBoxShowUpdatedMergeRequests.Size = new System.Drawing.Size(181, 17);
         this.checkBoxShowUpdatedMergeRequests.TabIndex = 4;
         this.checkBoxShowUpdatedMergeRequests.Text = "New commits in Merge Requests";
         this.checkBoxShowUpdatedMergeRequests.UseVisualStyleBackColor = true;
         // 
         // checkBoxShowKeywords
         // 
         this.checkBoxShowKeywords.AutoSize = true;
         this.checkBoxShowKeywords.Location = new System.Drawing.Point(319, 64);
         this.checkBoxShowKeywords.Name = "checkBoxShowKeywords";
         this.checkBoxShowKeywords.Size = new System.Drawing.Size(75, 17);
         this.checkBoxShowKeywords.TabIndex = 5;
         this.checkBoxShowKeywords.Text = "Keywords:";
         this.checkBoxShowKeywords.UseVisualStyleBackColor = true;
         // 
         // checkBoxShowResolvedAll
         // 
         this.checkBoxShowResolvedAll.AutoSize = true;
         this.checkBoxShowResolvedAll.Location = new System.Drawing.Point(319, 18);
         this.checkBoxShowResolvedAll.Name = "checkBoxShowResolvedAll";
         this.checkBoxShowResolvedAll.Size = new System.Drawing.Size(127, 17);
         this.checkBoxShowResolvedAll.TabIndex = 1;
         this.checkBoxShowResolvedAll.Text = "Resolved All Threads";
         this.checkBoxShowResolvedAll.UseVisualStyleBackColor = true;
         // 
         // checkBoxShowOnMention
         // 
         this.checkBoxShowOnMention.AutoSize = true;
         this.checkBoxShowOnMention.Location = new System.Drawing.Point(319, 41);
         this.checkBoxShowOnMention.Name = "checkBoxShowOnMention";
         this.checkBoxShowOnMention.Size = new System.Drawing.Size(170, 17);
         this.checkBoxShowOnMention.TabIndex = 3;
         this.checkBoxShowOnMention.Text = "When someone mentioned me";
         this.checkBoxShowOnMention.UseVisualStyleBackColor = true;
         // 
         // buttonOK
         // 
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(12, 150);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 1;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
         // 
         // buttonCancel
         // 
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(93, 150);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 2;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // ConfigureNotificationsForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(596, 182);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.groupBoxNotifications);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "ConfigureNotificationsForm";
         this.Text = "Configure Notifications";
         this.groupBoxNotifications.ResumeLayout(false);
         this.groupBoxNotifications.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxNotifications;
      private System.Windows.Forms.CheckBox checkBoxShowMergedMergeRequests;
      private System.Windows.Forms.CheckBox checkBoxShowServiceNotifications;
      private System.Windows.Forms.CheckBox checkBoxShowNewMergeRequests;
      private System.Windows.Forms.CheckBox checkBoxShowMyActivity;
      private System.Windows.Forms.CheckBox checkBoxShowUpdatedMergeRequests;
      private System.Windows.Forms.CheckBox checkBoxShowKeywords;
      private System.Windows.Forms.CheckBox checkBoxShowResolvedAll;
      private System.Windows.Forms.CheckBox checkBoxShowOnMention;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.Button buttonCancel;
   }
}