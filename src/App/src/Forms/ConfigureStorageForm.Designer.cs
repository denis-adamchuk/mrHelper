
namespace mrHelper.App.Forms
{
   partial class ConfigureStorageForm
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
         this.groupBoxFileStorageType = new System.Windows.Forms.GroupBox();
         this.linkLabelCommitStorageDescription = new System.Windows.Forms.LinkLabel();
         this.radioButtonUseGitShallowClone = new System.Windows.Forms.RadioButton();
         this.radioButtonDontUseGit = new System.Windows.Forms.RadioButton();
         this.radioButtonUseGitFullClone = new System.Windows.Forms.RadioButton();
         this.buttonBrowseStorageFolder = new System.Windows.Forms.Button();
         this.labelLocalStorageFolder = new System.Windows.Forms.Label();
         this.textBoxStorageFolder = new System.Windows.Forms.TextBox();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.buttonOK = new System.Windows.Forms.Button();
         this.storageFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.groupBoxFileStorageType.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxFileStorageType
         // 
         this.groupBoxFileStorageType.Controls.Add(this.linkLabelCommitStorageDescription);
         this.groupBoxFileStorageType.Controls.Add(this.radioButtonUseGitShallowClone);
         this.groupBoxFileStorageType.Controls.Add(this.radioButtonDontUseGit);
         this.groupBoxFileStorageType.Controls.Add(this.radioButtonUseGitFullClone);
         this.groupBoxFileStorageType.Location = new System.Drawing.Point(12, 51);
         this.groupBoxFileStorageType.Name = "groupBoxFileStorageType";
         this.groupBoxFileStorageType.Size = new System.Drawing.Size(412, 78);
         this.groupBoxFileStorageType.TabIndex = 31;
         this.groupBoxFileStorageType.TabStop = false;
         this.groupBoxFileStorageType.Text = "File Storage Type";
         // 
         // linkLabelCommitStorageDescription
         // 
         this.linkLabelCommitStorageDescription.AutoSize = true;
         this.linkLabelCommitStorageDescription.Location = new System.Drawing.Point(251, 46);
         this.linkLabelCommitStorageDescription.Name = "linkLabelCommitStorageDescription";
         this.linkLabelCommitStorageDescription.Size = new System.Drawing.Size(128, 13);
         this.linkLabelCommitStorageDescription.TabIndex = 30;
         this.linkLabelCommitStorageDescription.TabStop = true;
         this.linkLabelCommitStorageDescription.Text = "Show detailed description";
         this.linkLabelCommitStorageDescription.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelCommitStorageDescription_LinkClicked);
         // 
         // radioButtonUseGitShallowClone
         // 
         this.radioButtonUseGitShallowClone.AutoSize = true;
         this.radioButtonUseGitShallowClone.Location = new System.Drawing.Point(6, 44);
         this.radioButtonUseGitShallowClone.Name = "radioButtonUseGitShallowClone";
         this.radioButtonUseGitShallowClone.Size = new System.Drawing.Size(165, 17);
         this.radioButtonUseGitShallowClone.TabIndex = 29;
         this.radioButtonUseGitShallowClone.TabStop = true;
         this.radioButtonUseGitShallowClone.Text = "Use git in shallow clone mode";
         this.radioButtonUseGitShallowClone.UseVisualStyleBackColor = true;
         // 
         // radioButtonDontUseGit
         // 
         this.radioButtonDontUseGit.AutoSize = true;
         this.radioButtonDontUseGit.Location = new System.Drawing.Point(254, 19);
         this.radioButtonDontUseGit.Name = "radioButtonDontUseGit";
         this.radioButtonDontUseGit.Size = new System.Drawing.Size(152, 17);
         this.radioButtonDontUseGit.TabIndex = 28;
         this.radioButtonDontUseGit.TabStop = true;
         this.radioButtonDontUseGit.Text = "Don\'t use git as file storage";
         this.radioButtonDontUseGit.UseVisualStyleBackColor = true;
         // 
         // radioButtonUseGitFullClone
         // 
         this.radioButtonUseGitFullClone.AutoSize = true;
         this.radioButtonUseGitFullClone.Location = new System.Drawing.Point(6, 19);
         this.radioButtonUseGitFullClone.Name = "radioButtonUseGitFullClone";
         this.radioButtonUseGitFullClone.Size = new System.Drawing.Size(180, 17);
         this.radioButtonUseGitFullClone.TabIndex = 27;
         this.radioButtonUseGitFullClone.TabStop = true;
         this.radioButtonUseGitFullClone.Text = "Use git and clone full repositories";
         this.radioButtonUseGitFullClone.UseVisualStyleBackColor = true;
         // 
         // buttonBrowseStorageFolder
         // 
         this.buttonBrowseStorageFolder.Location = new System.Drawing.Point(430, 21);
         this.buttonBrowseStorageFolder.Name = "buttonBrowseStorageFolder";
         this.buttonBrowseStorageFolder.Size = new System.Drawing.Size(83, 27);
         this.buttonBrowseStorageFolder.TabIndex = 29;
         this.buttonBrowseStorageFolder.Text = "Browse...";
         this.buttonBrowseStorageFolder.UseVisualStyleBackColor = true;
         this.buttonBrowseStorageFolder.Click += new System.EventHandler(this.buttonBrowseStorageFolder_Click);
         // 
         // labelLocalStorageFolder
         // 
         this.labelLocalStorageFolder.AutoSize = true;
         this.labelLocalStorageFolder.Location = new System.Drawing.Point(12, 9);
         this.labelLocalStorageFolder.Name = "labelLocalStorageFolder";
         this.labelLocalStorageFolder.Size = new System.Drawing.Size(121, 13);
         this.labelLocalStorageFolder.TabIndex = 30;
         this.labelLocalStorageFolder.Text = "Folder for temporary files";
         // 
         // textBoxStorageFolder
         // 
         this.textBoxStorageFolder.Location = new System.Drawing.Point(12, 25);
         this.textBoxStorageFolder.Name = "textBoxStorageFolder";
         this.textBoxStorageFolder.ReadOnly = true;
         this.textBoxStorageFolder.Size = new System.Drawing.Size(412, 20);
         this.textBoxStorageFolder.TabIndex = 28;
         this.textBoxStorageFolder.TabStop = false;
         // 
         // buttonCancel
         // 
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(430, 102);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(83, 27);
         this.buttonCancel.TabIndex = 32;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // buttonOK
         // 
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(430, 70);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(83, 27);
         this.buttonOK.TabIndex = 33;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
         // 
         // ConfigureStorageForm
         // 
         this.AcceptButton = this.buttonOK;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(522, 138);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.groupBoxFileStorageType);
         this.Controls.Add(this.buttonBrowseStorageFolder);
         this.Controls.Add(this.labelLocalStorageFolder);
         this.Controls.Add(this.textBoxStorageFolder);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "ConfigureStorageForm";
         this.Text = "Configure Storage";
         this.groupBoxFileStorageType.ResumeLayout(false);
         this.groupBoxFileStorageType.PerformLayout();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxFileStorageType;
      private System.Windows.Forms.LinkLabel linkLabelCommitStorageDescription;
      private System.Windows.Forms.RadioButton radioButtonUseGitShallowClone;
      private System.Windows.Forms.RadioButton radioButtonDontUseGit;
      private System.Windows.Forms.RadioButton radioButtonUseGitFullClone;
      private System.Windows.Forms.Button buttonBrowseStorageFolder;
      private System.Windows.Forms.Label labelLocalStorageFolder;
      private System.Windows.Forms.TextBox textBoxStorageFolder;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.FolderBrowserDialog storageFolderBrowser;
      private System.Windows.Forms.ToolTip toolTip;
   }
}