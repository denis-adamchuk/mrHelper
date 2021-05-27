
namespace mrHelper.App.Forms
{
   partial class ConfigureHostsForm
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
         this.groupBoxSelectWorkflow = new System.Windows.Forms.GroupBox();
         this.linkLabelWorkflowDescription = new System.Windows.Forms.LinkLabel();
         this.radioButtonSelectByProjects = new System.Windows.Forms.RadioButton();
         this.radioButtonSelectByUsernames = new System.Windows.Forms.RadioButton();
         this.groupBoxConfigureProjectBasedWorkflow = new System.Windows.Forms.GroupBox();
         this.buttonEditProjects = new System.Windows.Forms.Button();
         this.listViewProjects = new System.Windows.Forms.ListView();
         this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.groupBoxConfigureUserBasedWorkflow = new System.Windows.Forms.GroupBox();
         this.listViewUsers = new System.Windows.Forms.ListView();
         this.columnHeaderUserName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.buttonEditUsers = new System.Windows.Forms.Button();
         this.buttonRemoveKnownHost = new System.Windows.Forms.Button();
         this.buttonAddKnownHost = new System.Windows.Forms.Button();
         this.listViewKnownHosts = new System.Windows.Forms.ListView();
         this.columnHeaderHost = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderAccessToken = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.buttonOK = new System.Windows.Forms.Button();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.groupBoxKnownHosts = new System.Windows.Forms.GroupBox();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.groupBoxSelectWorkflow.SuspendLayout();
         this.groupBoxConfigureProjectBasedWorkflow.SuspendLayout();
         this.groupBoxConfigureUserBasedWorkflow.SuspendLayout();
         this.groupBoxKnownHosts.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxSelectWorkflow
         // 
         this.groupBoxSelectWorkflow.Controls.Add(this.linkLabelWorkflowDescription);
         this.groupBoxSelectWorkflow.Controls.Add(this.radioButtonSelectByProjects);
         this.groupBoxSelectWorkflow.Controls.Add(this.radioButtonSelectByUsernames);
         this.groupBoxSelectWorkflow.Location = new System.Drawing.Point(12, 156);
         this.groupBoxSelectWorkflow.Name = "groupBoxSelectWorkflow";
         this.groupBoxSelectWorkflow.Size = new System.Drawing.Size(577, 52);
         this.groupBoxSelectWorkflow.TabIndex = 28;
         this.groupBoxSelectWorkflow.TabStop = false;
         this.groupBoxSelectWorkflow.Text = "Select Workflow";
         // 
         // linkLabelWorkflowDescription
         // 
         this.linkLabelWorkflowDescription.AutoSize = true;
         this.linkLabelWorkflowDescription.Location = new System.Drawing.Point(441, 21);
         this.linkLabelWorkflowDescription.Name = "linkLabelWorkflowDescription";
         this.linkLabelWorkflowDescription.Size = new System.Drawing.Size(128, 13);
         this.linkLabelWorkflowDescription.TabIndex = 31;
         this.linkLabelWorkflowDescription.TabStop = true;
         this.linkLabelWorkflowDescription.Text = "Show detailed description";
         this.linkLabelWorkflowDescription.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelWorkflowDescription_LinkClicked_1);
         // 
         // radioButtonSelectByProjects
         // 
         this.radioButtonSelectByProjects.AutoSize = true;
         this.radioButtonSelectByProjects.Location = new System.Drawing.Point(182, 19);
         this.radioButtonSelectByProjects.Name = "radioButtonSelectByProjects";
         this.radioButtonSelectByProjects.Size = new System.Drawing.Size(135, 17);
         this.radioButtonSelectByProjects.TabIndex = 22;
         this.radioButtonSelectByProjects.TabStop = true;
         this.radioButtonSelectByProjects.Text = "Project-based workflow";
         this.radioButtonSelectByProjects.UseVisualStyleBackColor = true;
         this.radioButtonSelectByProjects.CheckedChanged += new System.EventHandler(this.radioButtonWorkflowType_CheckedChanged);
         // 
         // radioButtonSelectByUsernames
         // 
         this.radioButtonSelectByUsernames.AutoSize = true;
         this.radioButtonSelectByUsernames.Location = new System.Drawing.Point(6, 19);
         this.radioButtonSelectByUsernames.Name = "radioButtonSelectByUsernames";
         this.radioButtonSelectByUsernames.Size = new System.Drawing.Size(124, 17);
         this.radioButtonSelectByUsernames.TabIndex = 19;
         this.radioButtonSelectByUsernames.TabStop = true;
         this.radioButtonSelectByUsernames.Text = "User-based workflow";
         this.radioButtonSelectByUsernames.UseVisualStyleBackColor = true;
         this.radioButtonSelectByUsernames.CheckedChanged += new System.EventHandler(this.radioButtonWorkflowType_CheckedChanged);
         // 
         // groupBoxConfigureProjectBasedWorkflow
         // 
         this.groupBoxConfigureProjectBasedWorkflow.Controls.Add(this.buttonEditProjects);
         this.groupBoxConfigureProjectBasedWorkflow.Controls.Add(this.listViewProjects);
         this.groupBoxConfigureProjectBasedWorkflow.Location = new System.Drawing.Point(316, 212);
         this.groupBoxConfigureProjectBasedWorkflow.Name = "groupBoxConfigureProjectBasedWorkflow";
         this.groupBoxConfigureProjectBasedWorkflow.Size = new System.Drawing.Size(273, 219);
         this.groupBoxConfigureProjectBasedWorkflow.TabIndex = 27;
         this.groupBoxConfigureProjectBasedWorkflow.TabStop = false;
         this.groupBoxConfigureProjectBasedWorkflow.Text = "Configure Project-based workflow";
         // 
         // buttonEditProjects
         // 
         this.buttonEditProjects.Enabled = false;
         this.buttonEditProjects.Location = new System.Drawing.Point(182, 182);
         this.buttonEditProjects.Name = "buttonEditProjects";
         this.buttonEditProjects.Size = new System.Drawing.Size(83, 27);
         this.buttonEditProjects.TabIndex = 18;
         this.buttonEditProjects.Text = "Edit...";
         this.buttonEditProjects.UseVisualStyleBackColor = true;
         this.buttonEditProjects.Click += new System.EventHandler(this.buttonEditProjects_Click);
         // 
         // listViewProjects
         // 
         this.listViewProjects.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName});
         this.listViewProjects.FullRowSelect = true;
         this.listViewProjects.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
         this.listViewProjects.HideSelection = false;
         this.listViewProjects.Location = new System.Drawing.Point(6, 19);
         this.listViewProjects.MultiSelect = false;
         this.listViewProjects.Name = "listViewProjects";
         this.listViewProjects.ShowGroups = false;
         this.listViewProjects.Size = new System.Drawing.Size(259, 157);
         this.listViewProjects.TabIndex = 17;
         this.listViewProjects.UseCompatibleStateImageBehavior = false;
         this.listViewProjects.View = System.Windows.Forms.View.Details;
         // 
         // columnHeaderName
         // 
         this.columnHeaderName.Text = "Name";
         this.columnHeaderName.Width = 160;
         // 
         // groupBoxConfigureUserBasedWorkflow
         // 
         this.groupBoxConfigureUserBasedWorkflow.Controls.Add(this.listViewUsers);
         this.groupBoxConfigureUserBasedWorkflow.Controls.Add(this.buttonEditUsers);
         this.groupBoxConfigureUserBasedWorkflow.Location = new System.Drawing.Point(12, 212);
         this.groupBoxConfigureUserBasedWorkflow.Name = "groupBoxConfigureUserBasedWorkflow";
         this.groupBoxConfigureUserBasedWorkflow.Size = new System.Drawing.Size(273, 219);
         this.groupBoxConfigureUserBasedWorkflow.TabIndex = 26;
         this.groupBoxConfigureUserBasedWorkflow.TabStop = false;
         this.groupBoxConfigureUserBasedWorkflow.Text = "Configure User-based workflow";
         // 
         // listViewUsers
         // 
         this.listViewUsers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderUserName});
         this.listViewUsers.FullRowSelect = true;
         this.listViewUsers.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
         this.listViewUsers.HideSelection = false;
         this.listViewUsers.Location = new System.Drawing.Point(6, 19);
         this.listViewUsers.MultiSelect = false;
         this.listViewUsers.Name = "listViewUsers";
         this.listViewUsers.ShowGroups = false;
         this.listViewUsers.Size = new System.Drawing.Size(259, 157);
         this.listViewUsers.TabIndex = 20;
         this.listViewUsers.UseCompatibleStateImageBehavior = false;
         this.listViewUsers.View = System.Windows.Forms.View.Details;
         // 
         // columnHeaderUserName
         // 
         this.columnHeaderUserName.Text = "Name";
         this.columnHeaderUserName.Width = 160;
         // 
         // buttonEditUsers
         // 
         this.buttonEditUsers.Enabled = false;
         this.buttonEditUsers.Location = new System.Drawing.Point(182, 182);
         this.buttonEditUsers.Name = "buttonEditUsers";
         this.buttonEditUsers.Size = new System.Drawing.Size(83, 27);
         this.buttonEditUsers.TabIndex = 21;
         this.buttonEditUsers.Text = "Edit...";
         this.buttonEditUsers.UseVisualStyleBackColor = true;
         this.buttonEditUsers.Click += new System.EventHandler(this.buttonEditUsers_Click);
         // 
         // buttonRemoveKnownHost
         // 
         this.buttonRemoveKnownHost.Enabled = false;
         this.buttonRemoveKnownHost.Location = new System.Drawing.Point(486, 102);
         this.buttonRemoveKnownHost.Name = "buttonRemoveKnownHost";
         this.buttonRemoveKnownHost.Size = new System.Drawing.Size(83, 27);
         this.buttonRemoveKnownHost.TabIndex = 31;
         this.buttonRemoveKnownHost.Text = "Remove";
         this.buttonRemoveKnownHost.UseVisualStyleBackColor = true;
         this.buttonRemoveKnownHost.Click += new System.EventHandler(this.buttonRemoveKnownHost_Click);
         // 
         // buttonAddKnownHost
         // 
         this.buttonAddKnownHost.Location = new System.Drawing.Point(488, 19);
         this.buttonAddKnownHost.Name = "buttonAddKnownHost";
         this.buttonAddKnownHost.Size = new System.Drawing.Size(83, 27);
         this.buttonAddKnownHost.TabIndex = 30;
         this.buttonAddKnownHost.Text = "Add...";
         this.buttonAddKnownHost.UseVisualStyleBackColor = true;
         this.buttonAddKnownHost.Click += new System.EventHandler(this.buttonAddKnownHost_Click);
         // 
         // listViewKnownHosts
         // 
         this.listViewKnownHosts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderHost,
            this.columnHeaderAccessToken});
         this.listViewKnownHosts.FullRowSelect = true;
         this.listViewKnownHosts.HideSelection = false;
         this.listViewKnownHosts.Location = new System.Drawing.Point(6, 19);
         this.listViewKnownHosts.MultiSelect = false;
         this.listViewKnownHosts.Name = "listViewKnownHosts";
         this.listViewKnownHosts.Size = new System.Drawing.Size(474, 110);
         this.listViewKnownHosts.TabIndex = 29;
         this.listViewKnownHosts.UseCompatibleStateImageBehavior = false;
         this.listViewKnownHosts.View = System.Windows.Forms.View.Details;
         this.listViewKnownHosts.SelectedIndexChanged += new System.EventHandler(this.listViewKnownHosts_SelectedIndexChanged);
         // 
         // columnHeaderHost
         // 
         this.columnHeaderHost.Text = "Host";
         this.columnHeaderHost.Width = 180;
         // 
         // columnHeaderAccessToken
         // 
         this.columnHeaderAccessToken.Text = "AccessToken";
         this.columnHeaderAccessToken.Width = 180;
         // 
         // buttonOK
         // 
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(595, 31);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(83, 27);
         this.buttonOK.TabIndex = 32;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
         // 
         // buttonCancel
         // 
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(595, 114);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(83, 27);
         this.buttonCancel.TabIndex = 33;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // groupBoxKnownHosts
         // 
         this.groupBoxKnownHosts.Controls.Add(this.buttonRemoveKnownHost);
         this.groupBoxKnownHosts.Controls.Add(this.buttonAddKnownHost);
         this.groupBoxKnownHosts.Controls.Add(this.listViewKnownHosts);
         this.groupBoxKnownHosts.Location = new System.Drawing.Point(12, 12);
         this.groupBoxKnownHosts.Name = "groupBoxKnownHosts";
         this.groupBoxKnownHosts.Size = new System.Drawing.Size(577, 138);
         this.groupBoxKnownHosts.TabIndex = 34;
         this.groupBoxKnownHosts.TabStop = false;
         this.groupBoxKnownHosts.Text = "Known Hosts";
         // 
         // ConfigureHostsForm
         // 
         this.AcceptButton = this.buttonOK;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(687, 438);
         this.Controls.Add(this.groupBoxKnownHosts);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.groupBoxSelectWorkflow);
         this.Controls.Add(this.groupBoxConfigureProjectBasedWorkflow);
         this.Controls.Add(this.groupBoxConfigureUserBasedWorkflow);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "ConfigureHostsForm";
         this.Text = "Configure Hosts";
         this.Load += new System.EventHandler(this.configureHostsForm_Load);
         this.groupBoxSelectWorkflow.ResumeLayout(false);
         this.groupBoxSelectWorkflow.PerformLayout();
         this.groupBoxConfigureProjectBasedWorkflow.ResumeLayout(false);
         this.groupBoxConfigureUserBasedWorkflow.ResumeLayout(false);
         this.groupBoxKnownHosts.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion
      private System.Windows.Forms.GroupBox groupBoxSelectWorkflow;
      private System.Windows.Forms.LinkLabel linkLabelWorkflowDescription;
      private System.Windows.Forms.RadioButton radioButtonSelectByProjects;
      private System.Windows.Forms.RadioButton radioButtonSelectByUsernames;
      private System.Windows.Forms.GroupBox groupBoxConfigureProjectBasedWorkflow;
      private System.Windows.Forms.Button buttonEditProjects;
      private System.Windows.Forms.ListView listViewProjects;
      private System.Windows.Forms.ColumnHeader columnHeaderName;
      private System.Windows.Forms.GroupBox groupBoxConfigureUserBasedWorkflow;
      private System.Windows.Forms.ListView listViewUsers;
      private System.Windows.Forms.ColumnHeader columnHeaderUserName;
      private System.Windows.Forms.Button buttonEditUsers;
      private System.Windows.Forms.Button buttonRemoveKnownHost;
      private System.Windows.Forms.Button buttonAddKnownHost;
      private System.Windows.Forms.ListView listViewKnownHosts;
      private System.Windows.Forms.ColumnHeader columnHeaderHost;
      private System.Windows.Forms.ColumnHeader columnHeaderAccessToken;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.GroupBox groupBoxKnownHosts;
      private System.Windows.Forms.ToolTip toolTip;
   }
}