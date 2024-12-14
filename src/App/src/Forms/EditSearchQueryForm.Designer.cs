
namespace mrHelper.App.Forms
{
   partial class EditSearchQueryForm
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
         this.labelSearchByState = new System.Windows.Forms.Label();
         this.comboBoxSearchByState = new System.Windows.Forms.ComboBox();
         this.textBoxSearchTargetBranch = new System.Windows.Forms.TextBox();
         this.linkLabelFindMe = new System.Windows.Forms.LinkLabel();
         this.buttonOK = new System.Windows.Forms.Button();
         this.comboBoxUser = new System.Windows.Forms.ComboBox();
         this.comboBoxProjectName = new System.Windows.Forms.ComboBox();
         this.checkBoxSearchByAuthor = new System.Windows.Forms.CheckBox();
         this.checkBoxSearchByProject = new System.Windows.Forms.CheckBox();
         this.checkBoxSearchByTargetBranch = new System.Windows.Forms.CheckBox();
         this.checkBoxSearchByTitleAndDescription = new System.Windows.Forms.CheckBox();
         this.textBoxSearchTitleAndDescription = new System.Windows.Forms.TextBox();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.toolTip = new Controls.ThemedToolTip(this.components);
         this.comboBoxMaxSearchResults = new System.Windows.Forms.ComboBox();
         this.labelMaxResultCount = new System.Windows.Forms.Label();
         this.SuspendLayout();
         // 
         // labelSearchByState
         // 
         this.labelSearchByState.AutoSize = true;
         this.labelSearchByState.Location = new System.Drawing.Point(12, 12);
         this.labelSearchByState.Name = "labelSearchByState";
         this.labelSearchByState.Size = new System.Drawing.Size(32, 13);
         this.labelSearchByState.TabIndex = 0;
         this.labelSearchByState.Text = "State";
         // 
         // comboBoxSearchByState
         // 
         this.comboBoxSearchByState.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxSearchByState.Items.AddRange(new object[] {
            "any",
            "opened",
            "closed",
            "merged"});
         this.comboBoxSearchByState.Location = new System.Drawing.Point(122, 9);
         this.comboBoxSearchByState.Name = "comboBoxSearchByState";
         this.comboBoxSearchByState.Size = new System.Drawing.Size(82, 21);
         this.comboBoxSearchByState.TabIndex = 1;
         // 
         // textBoxSearchTargetBranch
         // 
         this.textBoxSearchTargetBranch.Location = new System.Drawing.Point(122, 55);
         this.textBoxSearchTargetBranch.Name = "textBoxSearchTargetBranch";
         this.textBoxSearchTargetBranch.Size = new System.Drawing.Size(175, 20);
         this.textBoxSearchTargetBranch.TabIndex = 5;
         this.textBoxSearchTargetBranch.TextChanged += new System.EventHandler(this.textBoxSearchTargetBranch_TextChanged);
         this.textBoxSearchTargetBranch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxSearchTargetBranch_KeyDown);
         // 
         // linkLabelFindMe
         // 
         this.linkLabelFindMe.AutoSize = true;
         this.linkLabelFindMe.Location = new System.Drawing.Point(119, 125);
         this.linkLabelFindMe.Name = "linkLabelFindMe";
         this.linkLabelFindMe.Size = new System.Drawing.Size(44, 13);
         this.linkLabelFindMe.TabIndex = 10;
         this.linkLabelFindMe.TabStop = true;
         this.linkLabelFindMe.Text = "Find me";
         this.linkLabelFindMe.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelFindMe_LinkClicked);
         // 
         // buttonOK
         // 
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(12, 190);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(71, 23);
         this.buttonOK.TabIndex = 11;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         // 
         // comboBoxUser
         // 
         this.comboBoxUser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxUser.FormattingEnabled = true;
         this.comboBoxUser.Location = new System.Drawing.Point(122, 101);
         this.comboBoxUser.Name = "comboBoxUser";
         this.comboBoxUser.Size = new System.Drawing.Size(175, 21);
         this.comboBoxUser.TabIndex = 9;
         this.comboBoxUser.SelectionChangeCommitted += new System.EventHandler(this.comboBoxUser_SelectionChangeCommitted);
         this.comboBoxUser.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.comboBoxUser_Format);
         // 
         // comboBoxProjectName
         // 
         this.comboBoxProjectName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxProjectName.FormattingEnabled = true;
         this.comboBoxProjectName.Location = new System.Drawing.Point(122, 78);
         this.comboBoxProjectName.Name = "comboBoxProjectName";
         this.comboBoxProjectName.Size = new System.Drawing.Size(175, 21);
         this.comboBoxProjectName.TabIndex = 7;
         this.comboBoxProjectName.SelectionChangeCommitted += new System.EventHandler(this.comboBoxProjectName_SelectionChangeCommitted);
         // 
         // checkBoxSearchByAuthor
         // 
         this.checkBoxSearchByAuthor.AutoSize = true;
         this.checkBoxSearchByAuthor.Location = new System.Drawing.Point(12, 103);
         this.checkBoxSearchByAuthor.Name = "checkBoxSearchByAuthor";
         this.checkBoxSearchByAuthor.Size = new System.Drawing.Size(57, 17);
         this.checkBoxSearchByAuthor.TabIndex = 8;
         this.checkBoxSearchByAuthor.Text = "Author";
         this.checkBoxSearchByAuthor.UseVisualStyleBackColor = true;
         this.checkBoxSearchByAuthor.CheckedChanged += new System.EventHandler(this.checkBoxSearch_CheckedChanged);
         // 
         // checkBoxSearchByProject
         // 
         this.checkBoxSearchByProject.AutoSize = true;
         this.checkBoxSearchByProject.Location = new System.Drawing.Point(12, 80);
         this.checkBoxSearchByProject.Name = "checkBoxSearchByProject";
         this.checkBoxSearchByProject.Size = new System.Drawing.Size(59, 17);
         this.checkBoxSearchByProject.TabIndex = 6;
         this.checkBoxSearchByProject.Text = "Project";
         this.checkBoxSearchByProject.UseVisualStyleBackColor = true;
         this.checkBoxSearchByProject.CheckedChanged += new System.EventHandler(this.checkBoxSearch_CheckedChanged);
         // 
         // checkBoxSearchByTargetBranch
         // 
         this.checkBoxSearchByTargetBranch.AutoSize = true;
         this.checkBoxSearchByTargetBranch.Location = new System.Drawing.Point(12, 57);
         this.checkBoxSearchByTargetBranch.Name = "checkBoxSearchByTargetBranch";
         this.checkBoxSearchByTargetBranch.Size = new System.Drawing.Size(94, 17);
         this.checkBoxSearchByTargetBranch.TabIndex = 4;
         this.checkBoxSearchByTargetBranch.Text = "Target Branch";
         this.checkBoxSearchByTargetBranch.UseVisualStyleBackColor = true;
         this.checkBoxSearchByTargetBranch.CheckedChanged += new System.EventHandler(this.checkBoxSearch_CheckedChanged);
         // 
         // checkBoxSearchByTitleAndDescription
         // 
         this.checkBoxSearchByTitleAndDescription.AutoSize = true;
         this.checkBoxSearchByTitleAndDescription.Location = new System.Drawing.Point(12, 34);
         this.checkBoxSearchByTitleAndDescription.Name = "checkBoxSearchByTitleAndDescription";
         this.checkBoxSearchByTitleAndDescription.Size = new System.Drawing.Size(104, 17);
         this.checkBoxSearchByTitleAndDescription.TabIndex = 2;
         this.checkBoxSearchByTitleAndDescription.Text = "Title/Description";
         this.checkBoxSearchByTitleAndDescription.UseVisualStyleBackColor = true;
         this.checkBoxSearchByTitleAndDescription.CheckedChanged += new System.EventHandler(this.checkBoxSearch_CheckedChanged);
         // 
         // textBoxSearchTitleAndDescription
         // 
         this.textBoxSearchTitleAndDescription.Location = new System.Drawing.Point(122, 32);
         this.textBoxSearchTitleAndDescription.Name = "textBoxSearchTitleAndDescription";
         this.textBoxSearchTitleAndDescription.Size = new System.Drawing.Size(175, 20);
         this.textBoxSearchTitleAndDescription.TabIndex = 3;
         this.textBoxSearchTitleAndDescription.TextChanged += new System.EventHandler(this.textBoxSearchText_TextChanged);
         this.textBoxSearchTitleAndDescription.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxSearchText_KeyDown);
         // 
         // buttonCancel
         // 
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(89, 190);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(71, 23);
         this.buttonCancel.TabIndex = 12;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // comboBoxMaxSearchResults
         // 
         this.comboBoxMaxSearchResults.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxMaxSearchResults.Items.AddRange(new object[] {
            "20",
            "50",
            "75",
            "100"});
         this.comboBoxMaxSearchResults.Location = new System.Drawing.Point(122, 150);
         this.comboBoxMaxSearchResults.Name = "comboBoxMaxSearchResults";
         this.comboBoxMaxSearchResults.Size = new System.Drawing.Size(82, 21);
         this.comboBoxMaxSearchResults.TabIndex = 13;
         // 
         // labelMaxResultCount
         // 
         this.labelMaxResultCount.AutoSize = true;
         this.labelMaxResultCount.Location = new System.Drawing.Point(12, 153);
         this.labelMaxResultCount.Name = "labelMaxResultCount";
         this.labelMaxResultCount.Size = new System.Drawing.Size(85, 13);
         this.labelMaxResultCount.TabIndex = 14;
         this.labelMaxResultCount.Text = "Max result count";
         // 
         // EditSearchQueryForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(308, 225);
         this.Controls.Add(this.labelMaxResultCount);
         this.Controls.Add(this.comboBoxMaxSearchResults);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.labelSearchByState);
         this.Controls.Add(this.comboBoxSearchByState);
         this.Controls.Add(this.textBoxSearchTargetBranch);
         this.Controls.Add(this.linkLabelFindMe);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.comboBoxUser);
         this.Controls.Add(this.comboBoxProjectName);
         this.Controls.Add(this.checkBoxSearchByAuthor);
         this.Controls.Add(this.checkBoxSearchByProject);
         this.Controls.Add(this.checkBoxSearchByTargetBranch);
         this.Controls.Add(this.checkBoxSearchByTitleAndDescription);
         this.Controls.Add(this.textBoxSearchTitleAndDescription);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "EditSearchQueryForm";
         this.Text = "Edit Search Query";
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Label labelSearchByState;
      private System.Windows.Forms.ComboBox comboBoxSearchByState;
      private System.Windows.Forms.TextBox textBoxSearchTargetBranch;
      private System.Windows.Forms.LinkLabel linkLabelFindMe;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.ComboBox comboBoxUser;
      private System.Windows.Forms.ComboBox comboBoxProjectName;
      private System.Windows.Forms.CheckBox checkBoxSearchByAuthor;
      private System.Windows.Forms.CheckBox checkBoxSearchByProject;
      private System.Windows.Forms.CheckBox checkBoxSearchByTargetBranch;
      private System.Windows.Forms.CheckBox checkBoxSearchByTitleAndDescription;
      private System.Windows.Forms.TextBox textBoxSearchTitleAndDescription;
      private System.Windows.Forms.Button buttonCancel;
      private Controls.ThemedToolTip toolTip;
      private System.Windows.Forms.ComboBox comboBoxMaxSearchResults;
      private System.Windows.Forms.Label labelMaxResultCount;
   }
}