namespace mrHelper.App.Forms
{
   partial class MergeRequestPropertiesForm
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MergeRequestPropertiesForm));
         this.groupBoxSource = new System.Windows.Forms.GroupBox();
         this.comboBoxSourceBranch = new System.Windows.Forms.ComboBox();
         this.groupBoxTarget = new System.Windows.Forms.GroupBox();
         this.comboBoxTargetBranch = new System.Windows.Forms.ComboBox();
         this.groupBox3 = new System.Windows.Forms.GroupBox();
         this.buttonToggleWIP = new System.Windows.Forms.Button();
         this.buttonEditTitle = new System.Windows.Forms.Button();
         this.htmlPanelTitle = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.groupBoxDescription = new System.Windows.Forms.GroupBox();
         this.buttonEditDescription = new System.Windows.Forms.Button();
         this.htmlPanelDescription = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.groupBoxOptions = new System.Windows.Forms.GroupBox();
         this.labelSpecialNotePrefix = new System.Windows.Forms.Label();
         this.textBoxSpecialNote = new System.Windows.Forms.TextBox();
         this.labelAssignee = new System.Windows.Forms.Label();
         this.textBoxAssigneeUsername = new System.Windows.Forms.TextBox();
         this.checkBoxSquash = new System.Windows.Forms.CheckBox();
         this.checkBoxDeleteSourceBranch = new System.Windows.Forms.CheckBox();
         this.buttonSubmit = new System.Windows.Forms.Button();
         this.buttonCancel = new mrHelper.CommonControls.Controls.ConfirmCancelButton();
         this.groupBoxProject = new System.Windows.Forms.GroupBox();
         this.comboBoxProject = new System.Windows.Forms.ComboBox();
         this.groupBoxSource.SuspendLayout();
         this.groupBoxTarget.SuspendLayout();
         this.groupBox3.SuspendLayout();
         this.groupBoxDescription.SuspendLayout();
         this.groupBoxOptions.SuspendLayout();
         this.groupBoxProject.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxSource
         // 
         this.groupBoxSource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxSource.Controls.Add(this.comboBoxSourceBranch);
         this.groupBoxSource.Location = new System.Drawing.Point(277, 12);
         this.groupBoxSource.Name = "groupBoxSource";
         this.groupBoxSource.Size = new System.Drawing.Size(251, 56);
         this.groupBoxSource.TabIndex = 0;
         this.groupBoxSource.TabStop = false;
         this.groupBoxSource.Text = "Source Branch";
         // 
         // comboBoxSourceBranch
         // 
         this.comboBoxSourceBranch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxSourceBranch.FormattingEnabled = true;
         this.comboBoxSourceBranch.Location = new System.Drawing.Point(6, 19);
         this.comboBoxSourceBranch.Name = "comboBoxSourceBranch";
         this.comboBoxSourceBranch.Size = new System.Drawing.Size(239, 21);
         this.comboBoxSourceBranch.TabIndex = 1;
         this.comboBoxSourceBranch.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.comboBoxSourceBranch_Format);
         // 
         // groupBoxTarget
         // 
         this.groupBoxTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxTarget.Controls.Add(this.comboBoxTargetBranch);
         this.groupBoxTarget.Location = new System.Drawing.Point(534, 12);
         this.groupBoxTarget.Name = "groupBoxTarget";
         this.groupBoxTarget.Size = new System.Drawing.Size(251, 56);
         this.groupBoxTarget.TabIndex = 1;
         this.groupBoxTarget.TabStop = false;
         this.groupBoxTarget.Text = "Target Branch";
         // 
         // comboBoxTargetBranch
         // 
         this.comboBoxTargetBranch.FormattingEnabled = true;
         this.comboBoxTargetBranch.Location = new System.Drawing.Point(6, 19);
         this.comboBoxTargetBranch.Name = "comboBoxTargetBranch";
         this.comboBoxTargetBranch.Size = new System.Drawing.Size(239, 21);
         this.comboBoxTargetBranch.TabIndex = 2;
         this.comboBoxTargetBranch.TextChanged += new System.EventHandler(this.comboBoxTargetBranch_TextChanged);
         // 
         // groupBox3
         // 
         this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBox3.Controls.Add(this.buttonToggleWIP);
         this.groupBox3.Controls.Add(this.buttonEditTitle);
         this.groupBox3.Controls.Add(this.htmlPanelTitle);
         this.groupBox3.Location = new System.Drawing.Point(12, 74);
         this.groupBox3.Name = "groupBox3";
         this.groupBox3.Size = new System.Drawing.Size(773, 78);
         this.groupBox3.TabIndex = 3;
         this.groupBox3.TabStop = false;
         this.groupBox3.Text = "Title";
         // 
         // buttonToggleWIP
         // 
         this.buttonToggleWIP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.buttonToggleWIP.Location = new System.Drawing.Point(6, 45);
         this.buttonToggleWIP.Name = "buttonToggleWIP";
         this.buttonToggleWIP.Size = new System.Drawing.Size(110, 23);
         this.buttonToggleWIP.TabIndex = 4;
         this.buttonToggleWIP.Text = "Toggle WIP status";
         this.buttonToggleWIP.UseVisualStyleBackColor = true;
         this.buttonToggleWIP.Click += new System.EventHandler(this.buttonToggleWIP_Click);
         // 
         // buttonEditTitle
         // 
         this.buttonEditTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonEditTitle.Location = new System.Drawing.Point(692, 45);
         this.buttonEditTitle.Name = "buttonEditTitle";
         this.buttonEditTitle.Size = new System.Drawing.Size(75, 23);
         this.buttonEditTitle.TabIndex = 5;
         this.buttonEditTitle.Text = "Edit...";
         this.buttonEditTitle.UseVisualStyleBackColor = true;
         this.buttonEditTitle.Click += new System.EventHandler(this.buttonEditTitle_Click);
         // 
         // htmlPanelTitle
         // 
         this.htmlPanelTitle.AutoScroll = true;
         this.htmlPanelTitle.BackColor = System.Drawing.SystemColors.Window;
         this.htmlPanelTitle.BaseStylesheet = null;
         this.htmlPanelTitle.Cursor = System.Windows.Forms.Cursors.IBeam;
         this.htmlPanelTitle.Dock = System.Windows.Forms.DockStyle.Top;
         this.htmlPanelTitle.Location = new System.Drawing.Point(3, 16);
         this.htmlPanelTitle.Name = "htmlPanelTitle";
         this.htmlPanelTitle.Size = new System.Drawing.Size(767, 23);
         this.htmlPanelTitle.TabIndex = 3;
         this.htmlPanelTitle.Text = null;
         // 
         // groupBoxDescription
         // 
         this.groupBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxDescription.Controls.Add(this.buttonEditDescription);
         this.groupBoxDescription.Controls.Add(this.htmlPanelDescription);
         this.groupBoxDescription.Location = new System.Drawing.Point(12, 158);
         this.groupBoxDescription.Name = "groupBoxDescription";
         this.groupBoxDescription.Size = new System.Drawing.Size(773, 174);
         this.groupBoxDescription.TabIndex = 4;
         this.groupBoxDescription.TabStop = false;
         this.groupBoxDescription.Text = "Description";
         // 
         // buttonEditDescription
         // 
         this.buttonEditDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonEditDescription.Location = new System.Drawing.Point(692, 140);
         this.buttonEditDescription.Name = "buttonEditDescription";
         this.buttonEditDescription.Size = new System.Drawing.Size(75, 23);
         this.buttonEditDescription.TabIndex = 7;
         this.buttonEditDescription.Text = "Edit...";
         this.buttonEditDescription.UseVisualStyleBackColor = true;
         this.buttonEditDescription.Click += new System.EventHandler(this.buttonEditDescription_Click);
         // 
         // htmlPanelDescription
         // 
         this.htmlPanelDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.htmlPanelDescription.AutoScroll = true;
         this.htmlPanelDescription.BackColor = System.Drawing.SystemColors.Window;
         this.htmlPanelDescription.BaseStylesheet = null;
         this.htmlPanelDescription.Cursor = System.Windows.Forms.Cursors.IBeam;
         this.htmlPanelDescription.Location = new System.Drawing.Point(6, 19);
         this.htmlPanelDescription.Name = "htmlPanelDescription";
         this.htmlPanelDescription.Size = new System.Drawing.Size(761, 115);
         this.htmlPanelDescription.TabIndex = 6;
         this.htmlPanelDescription.Text = null;
         // 
         // groupBoxOptions
         // 
         this.groupBoxOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxOptions.Controls.Add(this.labelSpecialNotePrefix);
         this.groupBoxOptions.Controls.Add(this.textBoxSpecialNote);
         this.groupBoxOptions.Controls.Add(this.labelAssignee);
         this.groupBoxOptions.Controls.Add(this.textBoxAssigneeUsername);
         this.groupBoxOptions.Controls.Add(this.checkBoxSquash);
         this.groupBoxOptions.Controls.Add(this.checkBoxDeleteSourceBranch);
         this.groupBoxOptions.Location = new System.Drawing.Point(12, 338);
         this.groupBoxOptions.Name = "groupBoxOptions";
         this.groupBoxOptions.Size = new System.Drawing.Size(773, 76);
         this.groupBoxOptions.TabIndex = 5;
         this.groupBoxOptions.TabStop = false;
         this.groupBoxOptions.Text = "Options";
         // 
         // labelSpecialNotePrefix
         // 
         this.labelSpecialNotePrefix.AutoSize = true;
         this.labelSpecialNotePrefix.Location = new System.Drawing.Point(434, 47);
         this.labelSpecialNotePrefix.Name = "labelSpecialNotePrefix";
         this.labelSpecialNotePrefix.Size = new System.Drawing.Size(34, 13);
         this.labelSpecialNotePrefix.TabIndex = 5;
         this.labelSpecialNotePrefix.Text = "/insp ";
         // 
         // textBoxSpecialNote
         // 
         this.textBoxSpecialNote.Location = new System.Drawing.Point(471, 44);
         this.textBoxSpecialNote.Name = "textBoxSpecialNote";
         this.textBoxSpecialNote.Size = new System.Drawing.Size(296, 20);
         this.textBoxSpecialNote.TabIndex = 11;
         this.textBoxSpecialNote.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxSpecialNote_KeyDown);
         // 
         // labelAssignee
         // 
         this.labelAssignee.AutoSize = true;
         this.labelAssignee.Location = new System.Drawing.Point(6, 47);
         this.labelAssignee.Name = "labelAssignee";
         this.labelAssignee.Size = new System.Drawing.Size(102, 13);
         this.labelAssignee.TabIndex = 3;
         this.labelAssignee.Text = "Assignee user name";
         // 
         // textBoxAssigneeUsername
         // 
         this.textBoxAssigneeUsername.Location = new System.Drawing.Point(114, 44);
         this.textBoxAssigneeUsername.Name = "textBoxAssigneeUsername";
         this.textBoxAssigneeUsername.Size = new System.Drawing.Size(131, 20);
         this.textBoxAssigneeUsername.TabIndex = 10;
         this.textBoxAssigneeUsername.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxAssigneeUsername_KeyDown);
         // 
         // checkBoxSquash
         // 
         this.checkBoxSquash.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.checkBoxSquash.AutoSize = true;
         this.checkBoxSquash.Location = new System.Drawing.Point(507, 19);
         this.checkBoxSquash.Name = "checkBoxSquash";
         this.checkBoxSquash.Size = new System.Drawing.Size(260, 17);
         this.checkBoxSquash.TabIndex = 9;
         this.checkBoxSquash.Text = "Squash commits when merge request is accepted";
         this.checkBoxSquash.UseVisualStyleBackColor = true;
         // 
         // checkBoxDeleteSourceBranch
         // 
         this.checkBoxDeleteSourceBranch.AutoSize = true;
         this.checkBoxDeleteSourceBranch.Location = new System.Drawing.Point(6, 19);
         this.checkBoxDeleteSourceBranch.Name = "checkBoxDeleteSourceBranch";
         this.checkBoxDeleteSourceBranch.Size = new System.Drawing.Size(285, 17);
         this.checkBoxDeleteSourceBranch.TabIndex = 8;
         this.checkBoxDeleteSourceBranch.Text = "Delete source branch when merge request is accepted";
         this.checkBoxDeleteSourceBranch.UseVisualStyleBackColor = true;
         // 
         // buttonSubmit
         // 
         this.buttonSubmit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.buttonSubmit.Location = new System.Drawing.Point(12, 420);
         this.buttonSubmit.Name = "buttonSubmit";
         this.buttonSubmit.Size = new System.Drawing.Size(75, 23);
         this.buttonSubmit.TabIndex = 12;
         this.buttonSubmit.Text = "Submit";
         this.buttonSubmit.UseVisualStyleBackColor = true;
         this.buttonSubmit.Click += new System.EventHandler(this.buttonSubmit_Click);
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.ConfirmationText = "All changes will be lost, are you sure?";
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(710, 420);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 13;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // groupBoxProject
         // 
         this.groupBoxProject.Controls.Add(this.comboBoxProject);
         this.groupBoxProject.Location = new System.Drawing.Point(12, 12);
         this.groupBoxProject.Name = "groupBoxProject";
         this.groupBoxProject.Size = new System.Drawing.Size(251, 56);
         this.groupBoxProject.TabIndex = 1;
         this.groupBoxProject.TabStop = false;
         this.groupBoxProject.Text = "Project";
         // 
         // comboBoxProject
         // 
         this.comboBoxProject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxProject.FormattingEnabled = true;
         this.comboBoxProject.Location = new System.Drawing.Point(6, 19);
         this.comboBoxProject.Name = "comboBoxProject";
         this.comboBoxProject.Size = new System.Drawing.Size(239, 21);
         this.comboBoxProject.TabIndex = 0;
         // 
         // MergeRequestPropertiesForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(797, 455);
         this.Controls.Add(this.groupBoxProject);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonSubmit);
         this.Controls.Add(this.groupBoxOptions);
         this.Controls.Add(this.groupBoxDescription);
         this.Controls.Add(this.groupBox3);
         this.Controls.Add(this.groupBoxTarget);
         this.Controls.Add(this.groupBoxSource);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MinimumSize = new System.Drawing.Size(813, 494);
         this.Name = "MergeRequestPropertiesForm";
         this.Text = "Create New Merge Request";
         this.groupBoxSource.ResumeLayout(false);
         this.groupBoxTarget.ResumeLayout(false);
         this.groupBox3.ResumeLayout(false);
         this.groupBoxDescription.ResumeLayout(false);
         this.groupBoxOptions.ResumeLayout(false);
         this.groupBoxOptions.PerformLayout();
         this.groupBoxProject.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      protected System.Windows.Forms.GroupBox groupBoxSource;
      protected System.Windows.Forms.GroupBox groupBoxTarget;
      protected System.Windows.Forms.GroupBox groupBox3;
      protected System.Windows.Forms.Button buttonToggleWIP;
      protected System.Windows.Forms.Button buttonEditTitle;
      protected TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanelTitle;
      protected System.Windows.Forms.GroupBox groupBoxDescription;
      protected System.Windows.Forms.Button buttonEditDescription;
      protected TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanelDescription;
      protected System.Windows.Forms.GroupBox groupBoxOptions;
      protected System.Windows.Forms.CheckBox checkBoxSquash;
      protected System.Windows.Forms.CheckBox checkBoxDeleteSourceBranch;
      protected System.Windows.Forms.Button buttonSubmit;
      protected CommonControls.Controls.ConfirmCancelButton buttonCancel;
      protected System.Windows.Forms.ComboBox comboBoxSourceBranch;
      protected System.Windows.Forms.ComboBox comboBoxTargetBranch;
      protected System.Windows.Forms.GroupBox groupBoxProject;
      protected System.Windows.Forms.ComboBox comboBoxProject;
      protected System.Windows.Forms.Label labelAssignee;
      protected System.Windows.Forms.TextBox textBoxAssigneeUsername;
      protected System.Windows.Forms.TextBox textBoxSpecialNote;
      protected System.Windows.Forms.Label labelSpecialNotePrefix;
   }
}