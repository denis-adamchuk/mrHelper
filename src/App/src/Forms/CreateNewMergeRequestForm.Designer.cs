namespace mrHelper.App.src.Forms
{
   partial class CreateNewMergeRequestForm
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

         _repositoryAccessor.Dispose();
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateNewMergeRequestForm));
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
         this.checkBoxSquash = new System.Windows.Forms.CheckBox();
         this.checkBoxDeleteSourceBranch = new System.Windows.Forms.CheckBox();
         this.buttonSubmit = new System.Windows.Forms.Button();
         this.buttonCancel = new System.Windows.Forms.Button();
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
         this.groupBoxSource.Location = new System.Drawing.Point(208, 12);
         this.groupBoxSource.Name = "groupBoxSource";
         this.groupBoxSource.Size = new System.Drawing.Size(183, 56);
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
         this.comboBoxSourceBranch.Size = new System.Drawing.Size(171, 21);
         this.comboBoxSourceBranch.TabIndex = 0;
         this.comboBoxSourceBranch.SelectedIndexChanged += new System.EventHandler(this.comboBoxSourceBranch_SelectedIndexChanged);
         // 
         // groupBoxTarget
         // 
         this.groupBoxTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxTarget.Controls.Add(this.comboBoxTargetBranch);
         this.groupBoxTarget.Location = new System.Drawing.Point(397, 12);
         this.groupBoxTarget.Name = "groupBoxTarget";
         this.groupBoxTarget.Size = new System.Drawing.Size(183, 56);
         this.groupBoxTarget.TabIndex = 1;
         this.groupBoxTarget.TabStop = false;
         this.groupBoxTarget.Text = "Target Branch";
         // 
         // comboBoxTargetBranch
         // 
         this.comboBoxTargetBranch.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxTargetBranch.FormattingEnabled = true;
         this.comboBoxTargetBranch.Location = new System.Drawing.Point(6, 19);
         this.comboBoxTargetBranch.Name = "comboBoxTargetBranch";
         this.comboBoxTargetBranch.Size = new System.Drawing.Size(171, 21);
         this.comboBoxTargetBranch.TabIndex = 1;
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
         this.groupBox3.Size = new System.Drawing.Size(568, 78);
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
         this.buttonToggleWIP.TabIndex = 5;
         this.buttonToggleWIP.Text = "Toggle WIP status";
         this.buttonToggleWIP.UseVisualStyleBackColor = true;
         // 
         // buttonEditTitle
         // 
         this.buttonEditTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonEditTitle.Location = new System.Drawing.Point(487, 45);
         this.buttonEditTitle.Name = "buttonEditTitle";
         this.buttonEditTitle.Size = new System.Drawing.Size(75, 23);
         this.buttonEditTitle.TabIndex = 4;
         this.buttonEditTitle.Text = "Edit...";
         this.buttonEditTitle.UseVisualStyleBackColor = true;
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
         this.htmlPanelTitle.Size = new System.Drawing.Size(562, 23);
         this.htmlPanelTitle.TabIndex = 3;
         this.htmlPanelTitle.Text = null;
         // 
         // groupBoxDescription
         // 
         this.groupBoxDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxDescription.Controls.Add(this.buttonEditDescription);
         this.groupBoxDescription.Controls.Add(this.htmlPanelDescription);
         this.groupBoxDescription.Location = new System.Drawing.Point(12, 158);
         this.groupBoxDescription.Name = "groupBoxDescription";
         this.groupBoxDescription.Size = new System.Drawing.Size(568, 134);
         this.groupBoxDescription.TabIndex = 4;
         this.groupBoxDescription.TabStop = false;
         this.groupBoxDescription.Text = "Description";
         // 
         // buttonEditDescription
         // 
         this.buttonEditDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonEditDescription.Location = new System.Drawing.Point(487, 100);
         this.buttonEditDescription.Name = "buttonEditDescription";
         this.buttonEditDescription.Size = new System.Drawing.Size(75, 23);
         this.buttonEditDescription.TabIndex = 5;
         this.buttonEditDescription.Text = "Edit...";
         this.buttonEditDescription.UseVisualStyleBackColor = true;
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
         this.htmlPanelDescription.Size = new System.Drawing.Size(556, 75);
         this.htmlPanelDescription.TabIndex = 0;
         this.htmlPanelDescription.Text = null;
         // 
         // groupBoxOptions
         // 
         this.groupBoxOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxOptions.Controls.Add(this.checkBoxSquash);
         this.groupBoxOptions.Controls.Add(this.checkBoxDeleteSourceBranch);
         this.groupBoxOptions.Location = new System.Drawing.Point(12, 298);
         this.groupBoxOptions.Name = "groupBoxOptions";
         this.groupBoxOptions.Size = new System.Drawing.Size(568, 46);
         this.groupBoxOptions.TabIndex = 5;
         this.groupBoxOptions.TabStop = false;
         this.groupBoxOptions.Text = "Options";
         // 
         // checkBoxSquash
         // 
         this.checkBoxSquash.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.checkBoxSquash.AutoSize = true;
         this.checkBoxSquash.Location = new System.Drawing.Point(302, 19);
         this.checkBoxSquash.Name = "checkBoxSquash";
         this.checkBoxSquash.Size = new System.Drawing.Size(260, 17);
         this.checkBoxSquash.TabIndex = 1;
         this.checkBoxSquash.Text = "Squash commits when merge request is accepted";
         this.checkBoxSquash.UseVisualStyleBackColor = true;
         // 
         // checkBoxDeleteSourceBranch
         // 
         this.checkBoxDeleteSourceBranch.AutoSize = true;
         this.checkBoxDeleteSourceBranch.Location = new System.Drawing.Point(6, 19);
         this.checkBoxDeleteSourceBranch.Name = "checkBoxDeleteSourceBranch";
         this.checkBoxDeleteSourceBranch.Size = new System.Drawing.Size(285, 17);
         this.checkBoxDeleteSourceBranch.TabIndex = 0;
         this.checkBoxDeleteSourceBranch.Text = "Delete source branch when merge request is accepted";
         this.checkBoxDeleteSourceBranch.UseVisualStyleBackColor = true;
         // 
         // buttonSubmit
         // 
         this.buttonSubmit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.buttonSubmit.Location = new System.Drawing.Point(12, 352);
         this.buttonSubmit.Name = "buttonSubmit";
         this.buttonSubmit.Size = new System.Drawing.Size(75, 23);
         this.buttonSubmit.TabIndex = 6;
         this.buttonSubmit.Text = "Submit";
         this.buttonSubmit.UseVisualStyleBackColor = true;
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.Location = new System.Drawing.Point(505, 352);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 7;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // groupBoxProject
         // 
         this.groupBoxProject.Controls.Add(this.comboBoxProject);
         this.groupBoxProject.Location = new System.Drawing.Point(12, 12);
         this.groupBoxProject.Name = "groupBoxProject";
         this.groupBoxProject.Size = new System.Drawing.Size(183, 56);
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
         this.comboBoxProject.Size = new System.Drawing.Size(171, 21);
         this.comboBoxProject.TabIndex = 0;
         this.comboBoxProject.SelectedIndexChanged += new System.EventHandler(this.comboBoxProject_SelectedIndexChanged);
         // 
         // CreateNewMergeRequestForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(592, 387);
         this.Controls.Add(this.groupBoxProject);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonSubmit);
         this.Controls.Add(this.groupBoxOptions);
         this.Controls.Add(this.groupBoxDescription);
         this.Controls.Add(this.groupBox3);
         this.Controls.Add(this.groupBoxTarget);
         this.Controls.Add(this.groupBoxSource);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MinimumSize = new System.Drawing.Size(608, 426);
         this.Name = "CreateNewMergeRequestForm";
         this.Text = "Create New Merge Request";
         this.Load += new System.EventHandler(this.CreateNewMergeRequestForm_Load);
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

      private System.Windows.Forms.GroupBox groupBoxSource;
      private System.Windows.Forms.GroupBox groupBoxTarget;
      private System.Windows.Forms.GroupBox groupBox3;
      private System.Windows.Forms.Button buttonToggleWIP;
      private System.Windows.Forms.Button buttonEditTitle;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanelTitle;
      private System.Windows.Forms.GroupBox groupBoxDescription;
      private System.Windows.Forms.Button buttonEditDescription;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanelDescription;
      private System.Windows.Forms.GroupBox groupBoxOptions;
      private System.Windows.Forms.CheckBox checkBoxSquash;
      private System.Windows.Forms.CheckBox checkBoxDeleteSourceBranch;
      private System.Windows.Forms.Button buttonSubmit;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.ComboBox comboBoxSourceBranch;
      private System.Windows.Forms.ComboBox comboBoxTargetBranch;
      private System.Windows.Forms.GroupBox groupBoxProject;
      private System.Windows.Forms.ComboBox comboBoxProject;
   }
}