
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
         System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Users", System.Windows.Forms.HorizontalAlignment.Left);
         System.Windows.Forms.ListViewGroup listViewGroup2 = new System.Windows.Forms.ListViewGroup("Projects", System.Windows.Forms.HorizontalAlignment.Left);
         this.buttonEditProjects = new System.Windows.Forms.Button();
         this.buttonEditUsers = new System.Windows.Forms.Button();
         this.buttonRemoveKnownHost = new System.Windows.Forms.Button();
         this.buttonAddKnownHost = new System.Windows.Forms.Button();
         this.listViewKnownHosts = new CommonControls.Controls.ListViewEx();
         this.columnHeaderHost = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderAccessToken = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.columnHeaderExpiresAt = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.buttonOK = new System.Windows.Forms.Button();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.groupBoxKnownHosts = new System.Windows.Forms.GroupBox();
         this.labelExpirationHint = new System.Windows.Forms.Label();
         this.labelChecking = new System.Windows.Forms.Label();
         this.linkLabelCreateAccessToken = new CommonControls.Controls.LinkLabelEx();
         this.toolTip = new Controls.ThemedToolTip(this.components);
         this.listViewWorkflow = new mrHelper.App.Controls.StringToBooleanListView();
         this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
         this.groupBoxSelectWorkflow = new System.Windows.Forms.GroupBox();
         this.textBox1 = new CommonControls.Controls.MultilineLabel();
         this.groupBoxKnownHosts.SuspendLayout();
         this.groupBoxSelectWorkflow.SuspendLayout();
         this.SuspendLayout();
         // 
         // buttonEditProjects
         // 
         this.buttonEditProjects.Enabled = false;
         this.buttonEditProjects.Location = new System.Drawing.Point(318, 100);
         this.buttonEditProjects.Name = "buttonEditProjects";
         this.buttonEditProjects.Size = new System.Drawing.Size(156, 27);
         this.buttonEditProjects.TabIndex = 1;
         this.buttonEditProjects.Text = "Add/remove projects...";
         this.buttonEditProjects.UseVisualStyleBackColor = true;
         this.buttonEditProjects.Click += new System.EventHandler(this.buttonEditProjects_Click);
         // 
         // buttonEditUsers
         // 
         this.buttonEditUsers.Enabled = false;
         this.buttonEditUsers.Location = new System.Drawing.Point(318, 67);
         this.buttonEditUsers.Name = "buttonEditUsers";
         this.buttonEditUsers.Size = new System.Drawing.Size(156, 27);
         this.buttonEditUsers.TabIndex = 1;
         this.buttonEditUsers.Text = "Add/remove user names...";
         this.buttonEditUsers.UseVisualStyleBackColor = true;
         this.buttonEditUsers.Click += new System.EventHandler(this.buttonEditUsers_Click);
         // 
         // buttonRemoveKnownHost
         // 
         this.buttonRemoveKnownHost.Enabled = false;
         this.buttonRemoveKnownHost.Location = new System.Drawing.Point(488, 52);
         this.buttonRemoveKnownHost.Name = "buttonRemoveKnownHost";
         this.buttonRemoveKnownHost.Size = new System.Drawing.Size(83, 27);
         this.buttonRemoveKnownHost.TabIndex = 2;
         this.buttonRemoveKnownHost.Text = "Remove";
         this.buttonRemoveKnownHost.UseVisualStyleBackColor = true;
         this.buttonRemoveKnownHost.Click += new System.EventHandler(this.buttonRemoveKnownHost_Click);
         // 
         // buttonAddKnownHost
         // 
         this.buttonAddKnownHost.Location = new System.Drawing.Point(488, 19);
         this.buttonAddKnownHost.Name = "buttonAddKnownHost";
         this.buttonAddKnownHost.Size = new System.Drawing.Size(83, 27);
         this.buttonAddKnownHost.TabIndex = 1;
         this.buttonAddKnownHost.Text = "Add...";
         this.buttonAddKnownHost.UseVisualStyleBackColor = true;
         this.buttonAddKnownHost.Click += new System.EventHandler(this.buttonAddKnownHost_Click);
         // 
         // listViewKnownHosts
         // 
         this.listViewKnownHosts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderHost,
            this.columnHeaderAccessToken,
            this.columnHeaderExpiresAt});
         this.listViewKnownHosts.FullRowSelect = true;
         this.listViewKnownHosts.HideSelection = false;
         this.listViewKnownHosts.Location = new System.Drawing.Point(6, 19);
         this.listViewKnownHosts.MultiSelect = false;
         this.listViewKnownHosts.Name = "listViewKnownHosts";
         this.listViewKnownHosts.Size = new System.Drawing.Size(474, 94);
         this.listViewKnownHosts.TabIndex = 0;
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
         this.columnHeaderAccessToken.Width = 220;
         // 
         // columnHeaderExpiresAt
         // 
         this.columnHeaderExpiresAt.Text = "Expires At";
         this.columnHeaderExpiresAt.Width = 180;
         // 
         // buttonOK
         // 
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(500, 168);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(83, 27);
         this.buttonOK.TabIndex = 5;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
         // 
         // buttonCancel
         // 
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(500, 201);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(83, 27);
         this.buttonCancel.TabIndex = 0;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // groupBoxKnownHosts
         // 
         this.groupBoxKnownHosts.Controls.Add(this.labelExpirationHint);
         this.groupBoxKnownHosts.Controls.Add(this.labelChecking);
         this.groupBoxKnownHosts.Controls.Add(this.linkLabelCreateAccessToken);
         this.groupBoxKnownHosts.Controls.Add(this.buttonRemoveKnownHost);
         this.groupBoxKnownHosts.Controls.Add(this.buttonAddKnownHost);
         this.groupBoxKnownHosts.Controls.Add(this.listViewKnownHosts);
         this.groupBoxKnownHosts.Location = new System.Drawing.Point(12, 12);
         this.groupBoxKnownHosts.Name = "groupBoxKnownHosts";
         this.groupBoxKnownHosts.Size = new System.Drawing.Size(577, 138);
         this.groupBoxKnownHosts.TabIndex = 1;
         this.groupBoxKnownHosts.TabStop = false;
         this.groupBoxKnownHosts.Text = "Known Hosts";
         // 
         // labelExpirationHint
         // 
         this.labelExpirationHint.AutoSize = true;
         this.labelExpirationHint.Location = new System.Drawing.Point(311, 116);
         this.labelExpirationHint.Name = "labelExpirationHint";
         this.labelExpirationHint.Size = new System.Drawing.Size(266, 13);
         this.labelExpirationHint.TabIndex = 34;
         this.labelExpirationHint.Text = "Tokens prolong automatically 30 days before expiration";
         // 
         // labelChecking
         // 
         this.labelChecking.AutoSize = true;
         this.labelChecking.Location = new System.Drawing.Point(486, 91);
         this.labelChecking.Name = "labelChecking";
         this.labelChecking.Size = new System.Drawing.Size(61, 13);
         this.labelChecking.TabIndex = 33;
         this.labelChecking.Text = "Checking...";
         this.labelChecking.Visible = false;
         // 
         // linkLabelCreateAccessToken
         // 
         this.linkLabelCreateAccessToken.AutoSize = true;
         this.linkLabelCreateAccessToken.Location = new System.Drawing.Point(6, 116);
         this.linkLabelCreateAccessToken.Name = "linkLabelCreateAccessToken";
         this.linkLabelCreateAccessToken.Size = new System.Drawing.Size(105, 13);
         this.linkLabelCreateAccessToken.TabIndex = 32;
         this.linkLabelCreateAccessToken.TabStop = true;
         this.linkLabelCreateAccessToken.Text = "Create access token";
         // 
         // listViewWorkflow
         // 
         this.listViewWorkflow.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
         this.listViewWorkflow.FullRowSelect = true;
         listViewGroup1.Header = "Users";
         listViewGroup1.Name = "listViewGroupUsers";
         listViewGroup2.Header = "Projects";
         listViewGroup2.Name = "listViewGroupProjects";
         this.listViewWorkflow.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup1,
            listViewGroup2});
         this.listViewWorkflow.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
         this.listViewWorkflow.HideSelection = false;
         this.listViewWorkflow.Location = new System.Drawing.Point(6, 67);
         this.listViewWorkflow.MultiSelect = false;
         this.listViewWorkflow.Name = "listViewWorkflow";
         this.listViewWorkflow.OwnerDraw = true;
         this.listViewWorkflow.Size = new System.Drawing.Size(306, 202);
         this.listViewWorkflow.TabIndex = 6;
         this.listViewWorkflow.UseCompatibleStateImageBehavior = false;
         this.listViewWorkflow.View = System.Windows.Forms.View.Details;
         // 
         // columnHeader1
         // 
         this.columnHeader1.Text = "Name";
         this.columnHeader1.Width = 240;
         // 
         // groupBoxSelectWorkflow
         // 
         this.groupBoxSelectWorkflow.Controls.Add(this.textBox1);
         this.groupBoxSelectWorkflow.Controls.Add(this.buttonEditProjects);
         this.groupBoxSelectWorkflow.Controls.Add(this.listViewWorkflow);
         this.groupBoxSelectWorkflow.Controls.Add(this.buttonEditUsers);
         this.groupBoxSelectWorkflow.Location = new System.Drawing.Point(12, 156);
         this.groupBoxSelectWorkflow.Name = "groupBoxSelectWorkflow";
         this.groupBoxSelectWorkflow.Size = new System.Drawing.Size(480, 275);
         this.groupBoxSelectWorkflow.TabIndex = 2;
         this.groupBoxSelectWorkflow.TabStop = false;
         this.groupBoxSelectWorkflow.Text = "What Merge Requests to watch";
         // 
         // textBox1
         // 
         this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
         this.textBox1.Location = new System.Drawing.Point(6, 19);
         this.textBox1.Multiline = true;
         this.textBox1.Name = "textBox1";
         this.textBox1.ReadOnly = true;
         this.textBox1.Size = new System.Drawing.Size(468, 42);
         this.textBox1.TabIndex = 7;
         this.textBox1.Text = "Select user names whose merge requests you would like to watch.\r\nIf you also want" +
    " to watch ALL merge requests in some projects, specify their names.\r\nThe list is" +
    " configured per-host.";
         // 
         // ConfigureHostsForm
         // 
         this.AcceptButton = this.buttonOK;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(601, 438);
         this.Controls.Add(this.groupBoxKnownHosts);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.groupBoxSelectWorkflow);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "ConfigureHostsForm";
         this.Text = "Configure Hosts";
         this.Load += new System.EventHandler(this.configureHostsForm_Load);
         this.groupBoxKnownHosts.ResumeLayout(false);
         this.groupBoxKnownHosts.PerformLayout();
         this.groupBoxSelectWorkflow.ResumeLayout(false);
         this.groupBoxSelectWorkflow.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion
      private System.Windows.Forms.Button buttonEditProjects;
      private System.Windows.Forms.Button buttonEditUsers;
      private System.Windows.Forms.Button buttonRemoveKnownHost;
      private System.Windows.Forms.Button buttonAddKnownHost;
      private CommonControls.Controls.ListViewEx listViewKnownHosts;
      private System.Windows.Forms.ColumnHeader columnHeaderHost;
      private System.Windows.Forms.ColumnHeader columnHeaderAccessToken;
      private System.Windows.Forms.ColumnHeader columnHeaderExpiresAt;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.GroupBox groupBoxKnownHosts;
      private Controls.ThemedToolTip toolTip;
      private CommonControls.Controls.LinkLabelEx linkLabelCreateAccessToken;
      private Controls.StringToBooleanListView listViewWorkflow;
      private System.Windows.Forms.ColumnHeader columnHeader1;
      private System.Windows.Forms.GroupBox groupBoxSelectWorkflow;
      private CommonControls.Controls.MultilineLabel textBox1;
      private System.Windows.Forms.Label labelChecking;
      private System.Windows.Forms.Label labelExpirationHint;
   }
}