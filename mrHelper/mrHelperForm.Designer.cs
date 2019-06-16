namespace mrHelper
{
   partial class mrHelperForm
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(mrHelperForm));
         this.groupBoxAuthorization = new System.Windows.Forms.GroupBox();
         this.textBoxAccessToken = new System.Windows.Forms.TextBox();
         this.labelAccessToken = new System.Windows.Forms.Label();
         this.labelHost = new System.Windows.Forms.Label();
         this.textBoxHost = new System.Windows.Forms.TextBox();
         this.groupBoxMergeRequest = new System.Windows.Forms.GroupBox();
         this.labelSpentTime = new System.Windows.Forms.Label();
         this.labelSpentTimeLabel = new System.Windows.Forms.Label();
         this.buttonStartTimer = new System.Windows.Forms.Button();
         this.buttonConnect = new System.Windows.Forms.Button();
         this.textBoxMrURL = new System.Windows.Forms.TextBox();
         this.labelURL = new System.Windows.Forms.Label();
         this.groupBoxDiff = new System.Windows.Forms.GroupBox();
         this.buttonBrowseLocalGitFolder = new System.Windows.Forms.Button();
         this.textBoxLocalGitFolder = new System.Windows.Forms.TextBox();
         this.labelLocalGitFolder = new System.Windows.Forms.Label();
         this.labelCreateDiscussion = new System.Windows.Forms.Label();
         this.buttonDifftool = new System.Windows.Forms.Button();
         this.labelRight = new System.Windows.Forms.Label();
         this.comboBoxRightCommit = new System.Windows.Forms.ComboBox();
         this.labeLeft = new System.Windows.Forms.Label();
         this.comboBoxLeftCommit = new System.Windows.Forms.ComboBox();
         this.toolTipOnURL = new System.Windows.Forms.ToolTip(this.components);
         this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.restoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
         this.localGitFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
         this.groupBoxAuthorization.SuspendLayout();
         this.groupBoxMergeRequest.SuspendLayout();
         this.groupBoxDiff.SuspendLayout();
         this.contextMenuStrip.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxAuthorization
         // 
         this.groupBoxAuthorization.Controls.Add(this.textBoxAccessToken);
         this.groupBoxAuthorization.Controls.Add(this.labelAccessToken);
         this.groupBoxAuthorization.Controls.Add(this.labelHost);
         this.groupBoxAuthorization.Controls.Add(this.textBoxHost);
         this.groupBoxAuthorization.Location = new System.Drawing.Point(12, 12);
         this.groupBoxAuthorization.Name = "groupBoxAuthorization";
         this.groupBoxAuthorization.Size = new System.Drawing.Size(428, 68);
         this.groupBoxAuthorization.TabIndex = 0;
         this.groupBoxAuthorization.TabStop = false;
         this.groupBoxAuthorization.Text = "Authorization";
         // 
         // textBoxAccessToken
         // 
         this.textBoxAccessToken.Location = new System.Drawing.Point(219, 32);
         this.textBoxAccessToken.Name = "textBoxAccessToken";
         this.textBoxAccessToken.Size = new System.Drawing.Size(197, 20);
         this.textBoxAccessToken.TabIndex = 1;
         // 
         // labelAccessToken
         // 
         this.labelAccessToken.AutoSize = true;
         this.labelAccessToken.Location = new System.Drawing.Point(216, 16);
         this.labelAccessToken.Name = "labelAccessToken";
         this.labelAccessToken.Size = new System.Drawing.Size(73, 13);
         this.labelAccessToken.TabIndex = 2;
         this.labelAccessToken.Text = "AccessToken";
         // 
         // labelHost
         // 
         this.labelHost.AutoSize = true;
         this.labelHost.Location = new System.Drawing.Point(6, 16);
         this.labelHost.Name = "labelHost";
         this.labelHost.Size = new System.Drawing.Size(29, 13);
         this.labelHost.TabIndex = 1;
         this.labelHost.Text = "Host";
         // 
         // textBoxHost
         // 
         this.textBoxHost.Location = new System.Drawing.Point(6, 32);
         this.textBoxHost.Name = "textBoxHost";
         this.textBoxHost.Size = new System.Drawing.Size(197, 20);
         this.textBoxHost.TabIndex = 0;
         // 
         // groupBoxMergeRequest
         // 
         this.groupBoxMergeRequest.Controls.Add(this.labelSpentTime);
         this.groupBoxMergeRequest.Controls.Add(this.labelSpentTimeLabel);
         this.groupBoxMergeRequest.Controls.Add(this.buttonStartTimer);
         this.groupBoxMergeRequest.Controls.Add(this.buttonConnect);
         this.groupBoxMergeRequest.Controls.Add(this.textBoxMrURL);
         this.groupBoxMergeRequest.Controls.Add(this.labelURL);
         this.groupBoxMergeRequest.Location = new System.Drawing.Point(12, 86);
         this.groupBoxMergeRequest.Name = "groupBoxMergeRequest";
         this.groupBoxMergeRequest.Size = new System.Drawing.Size(428, 100);
         this.groupBoxMergeRequest.TabIndex = 1;
         this.groupBoxMergeRequest.TabStop = false;
         this.groupBoxMergeRequest.Text = "Merge Request";
         // 
         // labelSpentTime
         // 
         this.labelSpentTime.AutoSize = true;
         this.labelSpentTime.Location = new System.Drawing.Point(367, 65);
         this.labelSpentTime.Name = "labelSpentTime";
         this.labelSpentTime.Size = new System.Drawing.Size(0, 13);
         this.labelSpentTime.TabIndex = 5;
         // 
         // labelSpentTimeLabel
         // 
         this.labelSpentTimeLabel.AutoSize = true;
         this.labelSpentTimeLabel.Location = new System.Drawing.Point(308, 65);
         this.labelSpentTimeLabel.Name = "labelSpentTimeLabel";
         this.labelSpentTimeLabel.Size = new System.Drawing.Size(64, 13);
         this.labelSpentTimeLabel.TabIndex = 4;
         this.labelSpentTimeLabel.Text = "Spent Time:";
         // 
         // buttonStartTimer
         // 
         this.buttonStartTimer.Enabled = false;
         this.buttonStartTimer.Location = new System.Drawing.Point(219, 58);
         this.buttonStartTimer.Name = "buttonStartTimer";
         this.buttonStartTimer.Size = new System.Drawing.Size(83, 27);
         this.buttonStartTimer.TabIndex = 4;
         this.buttonStartTimer.UseVisualStyleBackColor = true;
         this.buttonStartTimer.Click += new System.EventHandler(this.ButtonStartTimer_Click);
         // 
         // buttonConnect
         // 
         this.buttonConnect.Location = new System.Drawing.Point(9, 58);
         this.buttonConnect.Name = "buttonConnect";
         this.buttonConnect.Size = new System.Drawing.Size(83, 27);
         this.buttonConnect.TabIndex = 3;
         this.buttonConnect.Text = "Connect";
         this.buttonConnect.UseVisualStyleBackColor = true;
         this.buttonConnect.Click += new System.EventHandler(this.ButtonConnect_Click);
         // 
         // textBoxMrURL
         // 
         this.textBoxMrURL.Location = new System.Drawing.Point(6, 32);
         this.textBoxMrURL.Name = "textBoxMrURL";
         this.textBoxMrURL.Size = new System.Drawing.Size(410, 20);
         this.textBoxMrURL.TabIndex = 2;
         // 
         // labelURL
         // 
         this.labelURL.AutoSize = true;
         this.labelURL.Location = new System.Drawing.Point(6, 16);
         this.labelURL.Name = "labelURL";
         this.labelURL.Size = new System.Drawing.Size(29, 13);
         this.labelURL.TabIndex = 0;
         this.labelURL.Text = "URL";
         // 
         // groupBoxDiff
         // 
         this.groupBoxDiff.Controls.Add(this.buttonBrowseLocalGitFolder);
         this.groupBoxDiff.Controls.Add(this.textBoxLocalGitFolder);
         this.groupBoxDiff.Controls.Add(this.labelLocalGitFolder);
         this.groupBoxDiff.Controls.Add(this.labelCreateDiscussion);
         this.groupBoxDiff.Controls.Add(this.buttonDifftool);
         this.groupBoxDiff.Controls.Add(this.labelRight);
         this.groupBoxDiff.Controls.Add(this.comboBoxRightCommit);
         this.groupBoxDiff.Controls.Add(this.labeLeft);
         this.groupBoxDiff.Controls.Add(this.comboBoxLeftCommit);
         this.groupBoxDiff.Location = new System.Drawing.Point(12, 192);
         this.groupBoxDiff.Name = "groupBoxDiff";
         this.groupBoxDiff.Size = new System.Drawing.Size(428, 155);
         this.groupBoxDiff.TabIndex = 2;
         this.groupBoxDiff.TabStop = false;
         this.groupBoxDiff.Text = "Diff";
         // 
         // buttonBrowseLocalGitFolder
         // 
         this.buttonBrowseLocalGitFolder.Location = new System.Drawing.Point(333, 28);
         this.buttonBrowseLocalGitFolder.Name = "buttonBrowseLocalGitFolder";
         this.buttonBrowseLocalGitFolder.Size = new System.Drawing.Size(83, 27);
         this.buttonBrowseLocalGitFolder.TabIndex = 5;
         this.buttonBrowseLocalGitFolder.Text = "Browse...";
         this.buttonBrowseLocalGitFolder.UseVisualStyleBackColor = true;
         this.buttonBrowseLocalGitFolder.Click += new System.EventHandler(this.ButtonBrowseLocalGitFolder_Click);
         // 
         // textBoxLocalGitFolder
         // 
         this.textBoxLocalGitFolder.Location = new System.Drawing.Point(6, 32);
         this.textBoxLocalGitFolder.Name = "textBoxLocalGitFolder";
         this.textBoxLocalGitFolder.ReadOnly = true;
         this.textBoxLocalGitFolder.Size = new System.Drawing.Size(321, 20);
         this.textBoxLocalGitFolder.TabIndex = 9;
         this.textBoxLocalGitFolder.TabStop = false;
         // 
         // labelLocalGitFolder
         // 
         this.labelLocalGitFolder.AutoSize = true;
         this.labelLocalGitFolder.Location = new System.Drawing.Point(6, 16);
         this.labelLocalGitFolder.Name = "labelLocalGitFolder";
         this.labelLocalGitFolder.Size = new System.Drawing.Size(139, 13);
         this.labelLocalGitFolder.TabIndex = 8;
         this.labelLocalGitFolder.Text = "Local folder for git repository";
         // 
         // labelCreateDiscussion
         // 
         this.labelCreateDiscussion.AutoSize = true;
         this.labelCreateDiscussion.Location = new System.Drawing.Point(173, 119);
         this.labelCreateDiscussion.Name = "labelCreateDiscussion";
         this.labelCreateDiscussion.Size = new System.Drawing.Size(243, 13);
         this.labelCreateDiscussion.TabIndex = 5;
         this.labelCreateDiscussion.Text = "To create a merge request discussion, press Ctrl-K";
         // 
         // buttonDifftool
         // 
         this.buttonDifftool.Enabled = false;
         this.buttonDifftool.Location = new System.Drawing.Point(9, 112);
         this.buttonDifftool.Name = "buttonDifftool";
         this.buttonDifftool.Size = new System.Drawing.Size(83, 27);
         this.buttonDifftool.TabIndex = 8;
         this.buttonDifftool.Text = "Diff Tool";
         this.buttonDifftool.UseVisualStyleBackColor = true;
         this.buttonDifftool.Click += new System.EventHandler(this.ButtonDifftool_Click);
         // 
         // labelRight
         // 
         this.labelRight.AutoSize = true;
         this.labelRight.Location = new System.Drawing.Point(216, 69);
         this.labelRight.Name = "labelRight";
         this.labelRight.Size = new System.Drawing.Size(32, 13);
         this.labelRight.TabIndex = 3;
         this.labelRight.Text = "Right";
         // 
         // comboBoxRightCommit
         // 
         this.comboBoxRightCommit.FormattingEnabled = true;
         this.comboBoxRightCommit.Location = new System.Drawing.Point(219, 85);
         this.comboBoxRightCommit.Name = "comboBoxRightCommit";
         this.comboBoxRightCommit.Size = new System.Drawing.Size(197, 21);
         this.comboBoxRightCommit.TabIndex = 7;
         // 
         // labeLeft
         // 
         this.labeLeft.AutoSize = true;
         this.labeLeft.Location = new System.Drawing.Point(6, 69);
         this.labeLeft.Name = "labeLeft";
         this.labeLeft.Size = new System.Drawing.Size(25, 13);
         this.labeLeft.TabIndex = 1;
         this.labeLeft.Text = "Left";
         // 
         // comboBoxLeftCommit
         // 
         this.comboBoxLeftCommit.FormattingEnabled = true;
         this.comboBoxLeftCommit.Location = new System.Drawing.Point(6, 85);
         this.comboBoxLeftCommit.Name = "comboBoxLeftCommit";
         this.comboBoxLeftCommit.Size = new System.Drawing.Size(197, 21);
         this.comboBoxLeftCommit.TabIndex = 6;
         // 
         // toolTipOnURL
         // 
         this.toolTipOnURL.AutoPopDelay = 10000;
         this.toolTipOnURL.InitialDelay = 10;
         this.toolTipOnURL.ReshowDelay = 10;
         // 
         // contextMenuStrip
         // 
         this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.restoreToolStripMenuItem,
            this.exitToolStripMenuItem});
         this.contextMenuStrip.Name = "contextMenuStrip1";
         this.contextMenuStrip.Size = new System.Drawing.Size(114, 48);
         // 
         // restoreToolStripMenuItem
         // 
         this.restoreToolStripMenuItem.Name = "restoreToolStripMenuItem";
         this.restoreToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
         this.restoreToolStripMenuItem.Text = "Restore";
         this.restoreToolStripMenuItem.Click += new System.EventHandler(this.RestoreToolStripMenuItem_Click);
         // 
         // exitToolStripMenuItem
         // 
         this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
         this.exitToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
         this.exitToolStripMenuItem.Text = "Exit";
         this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
         // 
         // notifyIcon
         // 
         this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
         this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
         this.notifyIcon.Text = "Merge Request Helper";
         this.notifyIcon.Visible = true;
         this.notifyIcon.DoubleClick += new System.EventHandler(this.NotifyIcon_DoubleClick);
         // 
         // localGitFolderBrowser
         // 
         this.localGitFolderBrowser.Description = "Select a folder where git repository will be stored locally";
         this.localGitFolderBrowser.RootFolder = System.Environment.SpecialFolder.MyComputer;
         // 
         // mrHelperForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(452, 359);
         this.Controls.Add(this.groupBoxDiff);
         this.Controls.Add(this.groupBoxMergeRequest);
         this.Controls.Add(this.groupBoxAuthorization);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "mrHelperForm";
         this.Text = "Merge Requests Helper";
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MrHelperForm_FormClosing);
         this.Load += new System.EventHandler(this.MrHelperForm_Load);
         this.groupBoxAuthorization.ResumeLayout(false);
         this.groupBoxAuthorization.PerformLayout();
         this.groupBoxMergeRequest.ResumeLayout(false);
         this.groupBoxMergeRequest.PerformLayout();
         this.groupBoxDiff.ResumeLayout(false);
         this.groupBoxDiff.PerformLayout();
         this.contextMenuStrip.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxAuthorization;
      private System.Windows.Forms.TextBox textBoxAccessToken;
      private System.Windows.Forms.Label labelAccessToken;
      private System.Windows.Forms.Label labelHost;
      private System.Windows.Forms.TextBox textBoxHost;
      private System.Windows.Forms.GroupBox groupBoxMergeRequest;
      private System.Windows.Forms.Button buttonConnect;
      private System.Windows.Forms.TextBox textBoxMrURL;
      private System.Windows.Forms.Label labelURL;
      private System.Windows.Forms.GroupBox groupBoxDiff;
      private System.Windows.Forms.Button buttonDifftool;
      private System.Windows.Forms.Label labelRight;
      private System.Windows.Forms.ComboBox comboBoxRightCommit;
      private System.Windows.Forms.Label labeLeft;
      private System.Windows.Forms.ComboBox comboBoxLeftCommit;
      private System.Windows.Forms.Button buttonStartTimer;
      private System.Windows.Forms.Label labelSpentTime;
      private System.Windows.Forms.Label labelSpentTimeLabel;
      private System.Windows.Forms.Label labelCreateDiscussion;
      private System.Windows.Forms.ToolTip toolTipOnURL;
      private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
      private System.Windows.Forms.ToolStripMenuItem restoreToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
      private System.Windows.Forms.NotifyIcon notifyIcon;
      private System.Windows.Forms.Label labelLocalGitFolder;
      private System.Windows.Forms.FolderBrowserDialog localGitFolderBrowser;
      private System.Windows.Forms.Button buttonBrowseLocalGitFolder;
      private System.Windows.Forms.TextBox textBoxLocalGitFolder;
   }
}

