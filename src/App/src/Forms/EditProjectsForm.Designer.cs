namespace mrHelper.App.Forms
{
   partial class EditProjectsForm
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
         this.listViewProjects = new System.Windows.Forms.ListView();
         this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.buttonAddProject = new System.Windows.Forms.Button();
         this.buttonRemoveProject = new System.Windows.Forms.Button();
         this.buttonToggleState = new System.Windows.Forms.Button();
         this.buttonOK = new System.Windows.Forms.Button();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.buttonUp = new System.Windows.Forms.Button();
         this.buttonDown = new System.Windows.Forms.Button();
         this.SuspendLayout();
         // 
         // listViewProjects
         // 
         this.listViewProjects.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.listViewProjects.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName});
         this.listViewProjects.FullRowSelect = true;
         this.listViewProjects.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
         this.listViewProjects.HideSelection = false;
         this.listViewProjects.Location = new System.Drawing.Point(12, 12);
         this.listViewProjects.MultiSelect = false;
         this.listViewProjects.Name = "listViewProjects";
         this.listViewProjects.OwnerDraw = true;
         this.listViewProjects.ShowGroups = false;
         this.listViewProjects.Size = new System.Drawing.Size(212, 236);
         this.listViewProjects.TabIndex = 0;
         this.listViewProjects.UseCompatibleStateImageBehavior = false;
         this.listViewProjects.View = System.Windows.Forms.View.Details;
         this.listViewProjects.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.listViewProjects_DrawSubItem);
         this.listViewProjects.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewProjects_ItemSelectionChanged);
         this.listViewProjects.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listViewProjects_KeyDown);
         // 
         // columnHeaderName
         // 
         this.columnHeaderName.Text = "Name";
         this.columnHeaderName.Width = 205;
         // 
         // buttonAddProject
         // 
         this.buttonAddProject.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonAddProject.Location = new System.Drawing.Point(230, 12);
         this.buttonAddProject.Name = "buttonAddProject";
         this.buttonAddProject.Size = new System.Drawing.Size(75, 23);
         this.buttonAddProject.TabIndex = 1;
         this.buttonAddProject.Text = "Add...";
         this.buttonAddProject.UseVisualStyleBackColor = true;
         this.buttonAddProject.Click += new System.EventHandler(this.buttonAddProject_Click);
         // 
         // buttonRemoveProject
         // 
         this.buttonRemoveProject.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonRemoveProject.Enabled = false;
         this.buttonRemoveProject.Location = new System.Drawing.Point(230, 41);
         this.buttonRemoveProject.Name = "buttonRemoveProject";
         this.buttonRemoveProject.Size = new System.Drawing.Size(75, 23);
         this.buttonRemoveProject.TabIndex = 2;
         this.buttonRemoveProject.Text = "Remove";
         this.buttonRemoveProject.UseVisualStyleBackColor = true;
         this.buttonRemoveProject.Click += new System.EventHandler(this.buttonRemoveProject_Click);
         // 
         // buttonToggleState
         // 
         this.buttonToggleState.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonToggleState.Enabled = false;
         this.buttonToggleState.Location = new System.Drawing.Point(230, 70);
         this.buttonToggleState.Name = "buttonToggleState";
         this.buttonToggleState.Size = new System.Drawing.Size(75, 23);
         this.buttonToggleState.TabIndex = 3;
         this.buttonToggleState.Text = "Enable";
         this.buttonToggleState.UseVisualStyleBackColor = true;
         this.buttonToggleState.Click += new System.EventHandler(this.buttonToggleState_Click);
         // 
         // buttonOK
         // 
         this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(230, 196);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 6;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(230, 225);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 7;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // buttonUp
         // 
         this.buttonUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonUp.Enabled = false;
         this.buttonUp.Location = new System.Drawing.Point(230, 119);
         this.buttonUp.Name = "buttonUp";
         this.buttonUp.Size = new System.Drawing.Size(75, 23);
         this.buttonUp.TabIndex = 4;
         this.buttonUp.Text = "Up";
         this.buttonUp.UseVisualStyleBackColor = true;
         this.buttonUp.Click += new System.EventHandler(this.buttonUp_Click);
         // 
         // buttonDown
         // 
         this.buttonDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonDown.Enabled = false;
         this.buttonDown.Location = new System.Drawing.Point(230, 148);
         this.buttonDown.Name = "buttonDown";
         this.buttonDown.Size = new System.Drawing.Size(75, 23);
         this.buttonDown.TabIndex = 5;
         this.buttonDown.Text = "Down";
         this.buttonDown.UseVisualStyleBackColor = true;
         this.buttonDown.Click += new System.EventHandler(this.buttonDown_Click);
         // 
         // EditProjectsForm
         // 
         this.AcceptButton = this.buttonOK;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(316, 260);
         this.Controls.Add(this.buttonDown);
         this.Controls.Add(this.buttonUp);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonToggleState);
         this.Controls.Add(this.buttonRemoveProject);
         this.Controls.Add(this.buttonAddProject);
         this.Controls.Add(this.listViewProjects);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(332, 299);
         this.Name = "EditProjectsForm";
         this.Text = "Edit Projects";
         this.ResumeLayout(false);

      }

        #endregion

        private System.Windows.Forms.ListView listViewProjects;
        private System.Windows.Forms.Button buttonAddProject;
        private System.Windows.Forms.Button buttonRemoveProject;
        private System.Windows.Forms.Button buttonToggleState;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.Button buttonUp;
      private System.Windows.Forms.Button buttonDown;
   }
}