namespace mrHelper.App.Forms
{
   partial class AcceptMergeRequestForm
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

         unsubscribeFromTimer();
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.components = new System.ComponentModel.Container();
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AcceptMergeRequestForm));
         this.linkLabelOpenGitExtensions = new System.Windows.Forms.LinkLabel();
         this.linkLabelOpenSourceTree = new System.Windows.Forms.LinkLabel();
         this.linkLabelOpenExplorer = new System.Windows.Forms.LinkLabel();
         this.buttonClose = new System.Windows.Forms.Button();
         this.groupBoxMerge = new System.Windows.Forms.GroupBox();
         this.groupBoxMergeCommitMessage = new System.Windows.Forms.GroupBox();
         this.labelCommitMessageLabel = new System.Windows.Forms.Label();
         this.comboBoxCommit = new System.Windows.Forms.ComboBox();
         this.textBoxCommitMessage = new System.Windows.Forms.TextBox();
         this.checkBoxSquash = new System.Windows.Forms.CheckBox();
         this.checkBoxDeleteSourceBranch = new System.Windows.Forms.CheckBox();
         this.buttonMerge = new System.Windows.Forms.Button();
         this.labelMergeStatus = new System.Windows.Forms.Label();
         this.groupBoxMergeRequestInformation = new System.Windows.Forms.GroupBox();
         this.labelProject = new System.Windows.Forms.Label();
         this.labelProjectLabel = new System.Windows.Forms.Label();
         this.labelAuthor = new System.Windows.Forms.Label();
         this.labelAuthorLabel = new System.Windows.Forms.Label();
         this.labelTitleLabel = new System.Windows.Forms.Label();
         this.htmlPanelTitle = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.labelTargetBranch = new System.Windows.Forms.Label();
         this.labelSourceBranch = new System.Windows.Forms.Label();
         this.labelTargetBranchLabel = new System.Windows.Forms.Label();
         this.labelSourceBranchLabel = new System.Windows.Forms.Label();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.buttonToggleDraft = new System.Windows.Forms.Button();
         this.buttonDiscussions = new System.Windows.Forms.Button();
         this.buttonRebase = new System.Windows.Forms.Button();
         this.linkLabelOpenAtGitLab = new System.Windows.Forms.LinkLabel();
         this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
         this.groupBoxRebase = new System.Windows.Forms.GroupBox();
         this.labelRebaseStatus = new System.Windows.Forms.Label();
         this.groupBoxDiscussions = new System.Windows.Forms.GroupBox();
         this.labelDiscussionStatus = new System.Windows.Forms.Label();
         this.groupBoxWorkInProgress = new System.Windows.Forms.GroupBox();
         this.labelDraftStatus = new System.Windows.Forms.Label();
         this.groupBoxMerge.SuspendLayout();
         this.groupBoxMergeCommitMessage.SuspendLayout();
         this.groupBoxMergeRequestInformation.SuspendLayout();
         this.tableLayoutPanel1.SuspendLayout();
         this.groupBoxRebase.SuspendLayout();
         this.groupBoxDiscussions.SuspendLayout();
         this.groupBoxWorkInProgress.SuspendLayout();
         this.SuspendLayout();
         // 
         // linkLabelOpenGitExtensions
         // 
         this.linkLabelOpenGitExtensions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.linkLabelOpenGitExtensions.AutoSize = true;
         this.linkLabelOpenGitExtensions.Location = new System.Drawing.Point(105, 492);
         this.linkLabelOpenGitExtensions.Name = "linkLabelOpenGitExtensions";
         this.linkLabelOpenGitExtensions.Size = new System.Drawing.Size(103, 13);
         this.linkLabelOpenGitExtensions.TabIndex = 2;
         this.linkLabelOpenGitExtensions.TabStop = true;
         this.linkLabelOpenGitExtensions.Text = "Open Git Extensions";
         this.linkLabelOpenGitExtensions.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelOpenGitExtensions_LinkClicked);
         // 
         // linkLabelOpenSourceTree
         // 
         this.linkLabelOpenSourceTree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.linkLabelOpenSourceTree.AutoSize = true;
         this.linkLabelOpenSourceTree.Location = new System.Drawing.Point(105, 511);
         this.linkLabelOpenSourceTree.Name = "linkLabelOpenSourceTree";
         this.linkLabelOpenSourceTree.Size = new System.Drawing.Size(95, 13);
         this.linkLabelOpenSourceTree.TabIndex = 3;
         this.linkLabelOpenSourceTree.TabStop = true;
         this.linkLabelOpenSourceTree.Text = "Open Source Tree";
         this.linkLabelOpenSourceTree.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelOpenSourceTree_LinkClicked);
         // 
         // linkLabelOpenExplorer
         // 
         this.linkLabelOpenExplorer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.linkLabelOpenExplorer.AutoSize = true;
         this.linkLabelOpenExplorer.Location = new System.Drawing.Point(9, 492);
         this.linkLabelOpenExplorer.Name = "linkLabelOpenExplorer";
         this.linkLabelOpenExplorer.Size = new System.Drawing.Size(74, 13);
         this.linkLabelOpenExplorer.TabIndex = 0;
         this.linkLabelOpenExplorer.TabStop = true;
         this.linkLabelOpenExplorer.Text = "Open Explorer";
         this.linkLabelOpenExplorer.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelOpenExplorer_LinkClicked);
         // 
         // buttonClose
         // 
         this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonClose.Location = new System.Drawing.Point(608, 492);
         this.buttonClose.Name = "buttonClose";
         this.buttonClose.Size = new System.Drawing.Size(96, 32);
         this.buttonClose.TabIndex = 4;
         this.buttonClose.Text = "Close";
         this.buttonClose.UseVisualStyleBackColor = true;
         this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
         // 
         // groupBoxMerge
         // 
         this.groupBoxMerge.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxMerge.Controls.Add(this.groupBoxMergeCommitMessage);
         this.groupBoxMerge.Controls.Add(this.checkBoxSquash);
         this.groupBoxMerge.Controls.Add(this.checkBoxDeleteSourceBranch);
         this.groupBoxMerge.Controls.Add(this.buttonMerge);
         this.groupBoxMerge.Controls.Add(this.labelMergeStatus);
         this.groupBoxMerge.Location = new System.Drawing.Point(12, 218);
         this.groupBoxMerge.Name = "groupBoxMerge";
         this.groupBoxMerge.Size = new System.Drawing.Size(692, 268);
         this.groupBoxMerge.TabIndex = 6;
         this.groupBoxMerge.TabStop = false;
         this.groupBoxMerge.Text = "Merge";
         // 
         // groupBoxMergeCommitMessage
         // 
         this.groupBoxMergeCommitMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxMergeCommitMessage.Controls.Add(this.labelCommitMessageLabel);
         this.groupBoxMergeCommitMessage.Controls.Add(this.comboBoxCommit);
         this.groupBoxMergeCommitMessage.Controls.Add(this.textBoxCommitMessage);
         this.groupBoxMergeCommitMessage.Location = new System.Drawing.Point(9, 63);
         this.groupBoxMergeCommitMessage.Name = "groupBoxMergeCommitMessage";
         this.groupBoxMergeCommitMessage.Size = new System.Drawing.Size(670, 161);
         this.groupBoxMergeCommitMessage.TabIndex = 12;
         this.groupBoxMergeCommitMessage.TabStop = false;
         this.groupBoxMergeCommitMessage.Text = "Merge Commit Message";
         // 
         // labelCommitMessageLabel
         // 
         this.labelCommitMessageLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.labelCommitMessageLabel.AutoSize = true;
         this.labelCommitMessageLabel.Location = new System.Drawing.Point(116, 22);
         this.labelCommitMessageLabel.Name = "labelCommitMessageLabel";
         this.labelCommitMessageLabel.Size = new System.Drawing.Size(160, 13);
         this.labelCommitMessageLabel.TabIndex = 2;
         this.labelCommitMessageLabel.Text = "Use an existing commit message";
         // 
         // comboBoxCommit
         // 
         this.comboBoxCommit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.comboBoxCommit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxCommit.FormattingEnabled = true;
         this.comboBoxCommit.Location = new System.Drawing.Point(299, 19);
         this.comboBoxCommit.Name = "comboBoxCommit";
         this.comboBoxCommit.Size = new System.Drawing.Size(365, 21);
         this.comboBoxCommit.TabIndex = 0;
         this.comboBoxCommit.SelectedIndexChanged += new System.EventHandler(this.comboBoxCommit_SelectedIndexChanged);
         this.comboBoxCommit.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.comboBoxCommit_Format);
         // 
         // textBoxCommitMessage
         // 
         this.textBoxCommitMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxCommitMessage.Location = new System.Drawing.Point(6, 47);
         this.textBoxCommitMessage.Multiline = true;
         this.textBoxCommitMessage.Name = "textBoxCommitMessage";
         this.textBoxCommitMessage.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
         this.textBoxCommitMessage.Size = new System.Drawing.Size(658, 108);
         this.textBoxCommitMessage.TabIndex = 1;
         // 
         // checkBoxSquash
         // 
         this.checkBoxSquash.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.checkBoxSquash.AutoSize = true;
         this.checkBoxSquash.Location = new System.Drawing.Point(419, 40);
         this.checkBoxSquash.Name = "checkBoxSquash";
         this.checkBoxSquash.Size = new System.Drawing.Size(260, 17);
         this.checkBoxSquash.TabIndex = 1;
         this.checkBoxSquash.Text = "Squash commits when merge request is accepted";
         this.checkBoxSquash.UseVisualStyleBackColor = true;
         this.checkBoxSquash.CheckedChanged += new System.EventHandler(this.checkBoxSquash_CheckedChanged);
         // 
         // checkBoxDeleteSourceBranch
         // 
         this.checkBoxDeleteSourceBranch.AutoSize = true;
         this.checkBoxDeleteSourceBranch.Location = new System.Drawing.Point(9, 40);
         this.checkBoxDeleteSourceBranch.Name = "checkBoxDeleteSourceBranch";
         this.checkBoxDeleteSourceBranch.Size = new System.Drawing.Size(285, 17);
         this.checkBoxDeleteSourceBranch.TabIndex = 0;
         this.checkBoxDeleteSourceBranch.Text = "Delete source branch when merge request is accepted";
         this.checkBoxDeleteSourceBranch.UseVisualStyleBackColor = true;
         this.checkBoxDeleteSourceBranch.CheckedChanged += new System.EventHandler(this.checkBoxDeleteSourceBranch_CheckedChanged);
         // 
         // buttonMerge
         // 
         this.buttonMerge.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.buttonMerge.Location = new System.Drawing.Point(9, 230);
         this.buttonMerge.Name = "buttonMerge";
         this.buttonMerge.Size = new System.Drawing.Size(96, 32);
         this.buttonMerge.TabIndex = 2;
         this.buttonMerge.Text = "Merge";
         this.buttonMerge.UseVisualStyleBackColor = true;
         this.buttonMerge.Click += new System.EventHandler(this.buttonMerge_Click);
         // 
         // labelMergeStatus
         // 
         this.labelMergeStatus.AutoSize = true;
         this.labelMergeStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.labelMergeStatus.Location = new System.Drawing.Point(8, 20);
         this.labelMergeStatus.Name = "labelMergeStatus";
         this.labelMergeStatus.Size = new System.Drawing.Size(127, 13);
         this.labelMergeStatus.TabIndex = 0;
         this.labelMergeStatus.Text = "<Merge Status Here>";
         // 
         // groupBoxMergeRequestInformation
         // 
         this.groupBoxMergeRequestInformation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxMergeRequestInformation.Controls.Add(this.labelProject);
         this.groupBoxMergeRequestInformation.Controls.Add(this.labelProjectLabel);
         this.groupBoxMergeRequestInformation.Controls.Add(this.labelAuthor);
         this.groupBoxMergeRequestInformation.Controls.Add(this.labelAuthorLabel);
         this.groupBoxMergeRequestInformation.Controls.Add(this.labelTitleLabel);
         this.groupBoxMergeRequestInformation.Controls.Add(this.htmlPanelTitle);
         this.groupBoxMergeRequestInformation.Controls.Add(this.labelTargetBranch);
         this.groupBoxMergeRequestInformation.Controls.Add(this.labelSourceBranch);
         this.groupBoxMergeRequestInformation.Controls.Add(this.labelTargetBranchLabel);
         this.groupBoxMergeRequestInformation.Controls.Add(this.labelSourceBranchLabel);
         this.groupBoxMergeRequestInformation.Location = new System.Drawing.Point(12, 12);
         this.groupBoxMergeRequestInformation.Name = "groupBoxMergeRequestInformation";
         this.groupBoxMergeRequestInformation.Size = new System.Drawing.Size(692, 106);
         this.groupBoxMergeRequestInformation.TabIndex = 10;
         this.groupBoxMergeRequestInformation.TabStop = false;
         this.groupBoxMergeRequestInformation.Text = "Merge Request Information";
         // 
         // labelProject
         // 
         this.labelProject.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.labelProject.AutoSize = true;
         this.labelProject.Location = new System.Drawing.Point(538, 57);
         this.labelProject.Name = "labelProject";
         this.labelProject.Size = new System.Drawing.Size(141, 13);
         this.labelProject.TabIndex = 11;
         this.labelProject.Text = "<Project branch name here>";
         // 
         // labelProjectLabel
         // 
         this.labelProjectLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.labelProjectLabel.AutoSize = true;
         this.labelProjectLabel.Location = new System.Drawing.Point(451, 57);
         this.labelProjectLabel.Name = "labelProjectLabel";
         this.labelProjectLabel.Size = new System.Drawing.Size(43, 13);
         this.labelProjectLabel.TabIndex = 10;
         this.labelProjectLabel.Text = "Project:";
         // 
         // labelAuthor
         // 
         this.labelAuthor.AutoSize = true;
         this.labelAuthor.Location = new System.Drawing.Point(93, 57);
         this.labelAuthor.Name = "labelAuthor";
         this.labelAuthor.Size = new System.Drawing.Size(103, 13);
         this.labelAuthor.TabIndex = 9;
         this.labelAuthor.Text = "<Author name here>";
         // 
         // labelAuthorLabel
         // 
         this.labelAuthorLabel.AutoSize = true;
         this.labelAuthorLabel.Location = new System.Drawing.Point(8, 57);
         this.labelAuthorLabel.Name = "labelAuthorLabel";
         this.labelAuthorLabel.Size = new System.Drawing.Size(41, 13);
         this.labelAuthorLabel.TabIndex = 8;
         this.labelAuthorLabel.Text = "Author:";
         // 
         // labelTitleLabel
         // 
         this.labelTitleLabel.AutoSize = true;
         this.labelTitleLabel.Location = new System.Drawing.Point(8, 29);
         this.labelTitleLabel.Name = "labelTitleLabel";
         this.labelTitleLabel.Size = new System.Drawing.Size(30, 13);
         this.labelTitleLabel.TabIndex = 7;
         this.labelTitleLabel.Text = "Title:";
         // 
         // htmlPanelTitle
         // 
         this.htmlPanelTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.htmlPanelTitle.AutoScroll = true;
         this.htmlPanelTitle.BackColor = System.Drawing.SystemColors.Window;
         this.htmlPanelTitle.BaseStylesheet = resources.GetString("htmlPanelTitle.BaseStylesheet");
         this.htmlPanelTitle.Cursor = System.Windows.Forms.Cursors.IBeam;
         this.htmlPanelTitle.Location = new System.Drawing.Point(96, 19);
         this.htmlPanelTitle.Name = "htmlPanelTitle";
         this.htmlPanelTitle.Size = new System.Drawing.Size(583, 23);
         this.htmlPanelTitle.TabIndex = 0;
         this.htmlPanelTitle.Text = null;
         // 
         // labelTargetBranch
         // 
         this.labelTargetBranch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.labelTargetBranch.AutoSize = true;
         this.labelTargetBranch.Location = new System.Drawing.Point(538, 81);
         this.labelTargetBranch.Name = "labelTargetBranch";
         this.labelTargetBranch.Size = new System.Drawing.Size(139, 13);
         this.labelTargetBranch.TabIndex = 5;
         this.labelTargetBranch.Text = "<Target branch name here>";
         // 
         // labelSourceBranch
         // 
         this.labelSourceBranch.AutoSize = true;
         this.labelSourceBranch.Location = new System.Drawing.Point(93, 81);
         this.labelSourceBranch.Name = "labelSourceBranch";
         this.labelSourceBranch.Size = new System.Drawing.Size(142, 13);
         this.labelSourceBranch.TabIndex = 4;
         this.labelSourceBranch.Text = "<Source branch name here>";
         // 
         // labelTargetBranchLabel
         // 
         this.labelTargetBranchLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.labelTargetBranchLabel.AutoSize = true;
         this.labelTargetBranchLabel.Location = new System.Drawing.Point(451, 81);
         this.labelTargetBranchLabel.Name = "labelTargetBranchLabel";
         this.labelTargetBranchLabel.Size = new System.Drawing.Size(78, 13);
         this.labelTargetBranchLabel.TabIndex = 3;
         this.labelTargetBranchLabel.Text = "Target Branch:";
         // 
         // labelSourceBranchLabel
         // 
         this.labelSourceBranchLabel.AutoSize = true;
         this.labelSourceBranchLabel.Location = new System.Drawing.Point(8, 81);
         this.labelSourceBranchLabel.Name = "labelSourceBranchLabel";
         this.labelSourceBranchLabel.Size = new System.Drawing.Size(81, 13);
         this.labelSourceBranchLabel.TabIndex = 2;
         this.labelSourceBranchLabel.Text = "Source Branch:";
         // 
         // buttonToggleDraft
         // 
         this.buttonToggleDraft.Location = new System.Drawing.Point(6, 44);
         this.buttonToggleDraft.Name = "buttonToggleDraft";
         this.buttonToggleDraft.Size = new System.Drawing.Size(129, 32);
         this.buttonToggleDraft.TabIndex = 0;
         this.buttonToggleDraft.Text = "Toggle WIP/Draft Status";
         this.toolTip.SetToolTip(this.buttonToggleDraft, "Set or reset Draft state");
         this.buttonToggleDraft.UseVisualStyleBackColor = true;
         this.buttonToggleDraft.Click += new System.EventHandler(this.buttonToggleDraft_Click);
         // 
         // buttonDiscussions
         // 
         this.buttonDiscussions.Location = new System.Drawing.Point(6, 44);
         this.buttonDiscussions.Name = "buttonDiscussions";
         this.buttonDiscussions.Size = new System.Drawing.Size(96, 32);
         this.buttonDiscussions.TabIndex = 0;
         this.buttonDiscussions.Text = "Discussions";
         this.toolTip.SetToolTip(this.buttonDiscussions, "Open Discussions view");
         this.buttonDiscussions.UseVisualStyleBackColor = true;
         this.buttonDiscussions.Click += new System.EventHandler(this.buttonDiscussions_Click);
         // 
         // buttonRebase
         // 
         this.buttonRebase.Location = new System.Drawing.Point(6, 44);
         this.buttonRebase.Name = "buttonRebase";
         this.buttonRebase.Size = new System.Drawing.Size(96, 32);
         this.buttonRebase.TabIndex = 0;
         this.buttonRebase.Text = "Rebase";
         this.toolTip.SetToolTip(this.buttonRebase, "Rebase the source branch onto the target branch to allow this merge request to be" +
        " merged");
         this.buttonRebase.UseVisualStyleBackColor = true;
         this.buttonRebase.Click += new System.EventHandler(this.buttonRebase_Click);
         // 
         // linkLabelOpenAtGitLab
         // 
         this.linkLabelOpenAtGitLab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.linkLabelOpenAtGitLab.AutoSize = true;
         this.linkLabelOpenAtGitLab.Location = new System.Drawing.Point(9, 511);
         this.linkLabelOpenAtGitLab.Name = "linkLabelOpenAtGitLab";
         this.linkLabelOpenAtGitLab.Size = new System.Drawing.Size(79, 13);
         this.linkLabelOpenAtGitLab.TabIndex = 1;
         this.linkLabelOpenAtGitLab.TabStop = true;
         this.linkLabelOpenAtGitLab.Text = "Open at GitLab";
         this.linkLabelOpenAtGitLab.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelOpenAtGitLab_LinkClicked);
         // 
         // tableLayoutPanel1
         // 
         this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.tableLayoutPanel1.ColumnCount = 3;
         this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
         this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
         this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
         this.tableLayoutPanel1.Controls.Add(this.groupBoxRebase, 2, 0);
         this.tableLayoutPanel1.Controls.Add(this.groupBoxDiscussions, 1, 0);
         this.tableLayoutPanel1.Controls.Add(this.groupBoxWorkInProgress, 0, 0);
         this.tableLayoutPanel1.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
         this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 124);
         this.tableLayoutPanel1.Name = "tableLayoutPanel1";
         this.tableLayoutPanel1.RowCount = 1;
         this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
         this.tableLayoutPanel1.Size = new System.Drawing.Size(692, 88);
         this.tableLayoutPanel1.TabIndex = 22;
         // 
         // groupBoxRebase
         // 
         this.groupBoxRebase.Controls.Add(this.buttonRebase);
         this.groupBoxRebase.Controls.Add(this.labelRebaseStatus);
         this.groupBoxRebase.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxRebase.Location = new System.Drawing.Point(463, 3);
         this.groupBoxRebase.Name = "groupBoxRebase";
         this.groupBoxRebase.Size = new System.Drawing.Size(226, 82);
         this.groupBoxRebase.TabIndex = 24;
         this.groupBoxRebase.TabStop = false;
         this.groupBoxRebase.Text = "Rebase";
         // 
         // labelRebaseStatus
         // 
         this.labelRebaseStatus.AutoSize = true;
         this.labelRebaseStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.labelRebaseStatus.ForeColor = System.Drawing.SystemColors.ControlText;
         this.labelRebaseStatus.Location = new System.Drawing.Point(6, 20);
         this.labelRebaseStatus.Name = "labelRebaseStatus";
         this.labelRebaseStatus.Size = new System.Drawing.Size(131, 13);
         this.labelRebaseStatus.TabIndex = 0;
         this.labelRebaseStatus.Text = "<Rebase status here>";
         // 
         // groupBoxDiscussions
         // 
         this.groupBoxDiscussions.Controls.Add(this.buttonDiscussions);
         this.groupBoxDiscussions.Controls.Add(this.labelDiscussionStatus);
         this.groupBoxDiscussions.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxDiscussions.Location = new System.Drawing.Point(233, 3);
         this.groupBoxDiscussions.Name = "groupBoxDiscussions";
         this.groupBoxDiscussions.Size = new System.Drawing.Size(224, 82);
         this.groupBoxDiscussions.TabIndex = 23;
         this.groupBoxDiscussions.TabStop = false;
         this.groupBoxDiscussions.Text = "Discussions";
         // 
         // labelDiscussionStatus
         // 
         this.labelDiscussionStatus.AutoSize = true;
         this.labelDiscussionStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.labelDiscussionStatus.Location = new System.Drawing.Point(8, 20);
         this.labelDiscussionStatus.Name = "labelDiscussionStatus";
         this.labelDiscussionStatus.Size = new System.Drawing.Size(155, 13);
         this.labelDiscussionStatus.TabIndex = 0;
         this.labelDiscussionStatus.Text = "<Discussions status here>";
         // 
         // groupBoxWorkInProgress
         // 
         this.groupBoxWorkInProgress.Controls.Add(this.buttonToggleDraft);
         this.groupBoxWorkInProgress.Controls.Add(this.labelDraftStatus);
         this.groupBoxWorkInProgress.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxWorkInProgress.Location = new System.Drawing.Point(3, 3);
         this.groupBoxWorkInProgress.Name = "groupBoxWorkInProgress";
         this.groupBoxWorkInProgress.Size = new System.Drawing.Size(224, 82);
         this.groupBoxWorkInProgress.TabIndex = 22;
         this.groupBoxWorkInProgress.TabStop = false;
         this.groupBoxWorkInProgress.Text = "WIP/Draft";
         // 
         // labelDraftStatus
         // 
         this.labelDraftStatus.AutoSize = true;
         this.labelDraftStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
         this.labelDraftStatus.Location = new System.Drawing.Point(8, 20);
         this.labelDraftStatus.Name = "labelDraftStatus";
         this.labelDraftStatus.Size = new System.Drawing.Size(116, 13);
         this.labelDraftStatus.TabIndex = 0;
         this.labelDraftStatus.Text = "<Draft status here>";
         // 
         // AcceptMergeRequestForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.AutoScroll = true;
         this.CancelButton = this.buttonClose;
         this.ClientSize = new System.Drawing.Size(716, 537);
         this.Controls.Add(this.tableLayoutPanel1);
         this.Controls.Add(this.linkLabelOpenAtGitLab);
         this.Controls.Add(this.groupBoxMergeRequestInformation);
         this.Controls.Add(this.groupBoxMerge);
         this.Controls.Add(this.buttonClose);
         this.Controls.Add(this.linkLabelOpenSourceTree);
         this.Controls.Add(this.linkLabelOpenGitExtensions);
         this.Controls.Add(this.linkLabelOpenExplorer);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimumSize = new System.Drawing.Size(732, 576);
         this.Name = "AcceptMergeRequestForm";
         this.Text = "Request to merge";
         this.Load += new System.EventHandler(this.AcceptMergeRequestForm_Load);
         this.groupBoxMerge.ResumeLayout(false);
         this.groupBoxMerge.PerformLayout();
         this.groupBoxMergeCommitMessage.ResumeLayout(false);
         this.groupBoxMergeCommitMessage.PerformLayout();
         this.groupBoxMergeRequestInformation.ResumeLayout(false);
         this.groupBoxMergeRequestInformation.PerformLayout();
         this.tableLayoutPanel1.ResumeLayout(false);
         this.groupBoxRebase.ResumeLayout(false);
         this.groupBoxRebase.PerformLayout();
         this.groupBoxDiscussions.ResumeLayout(false);
         this.groupBoxDiscussions.PerformLayout();
         this.groupBoxWorkInProgress.ResumeLayout(false);
         this.groupBoxWorkInProgress.PerformLayout();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion
      private System.Windows.Forms.LinkLabel linkLabelOpenGitExtensions;
      private System.Windows.Forms.LinkLabel linkLabelOpenSourceTree;
      private System.Windows.Forms.LinkLabel linkLabelOpenExplorer;
      private System.Windows.Forms.Button buttonClose;
      private System.Windows.Forms.GroupBox groupBoxMerge;
      private System.Windows.Forms.Button buttonMerge;
      private System.Windows.Forms.Label labelMergeStatus;
      private System.Windows.Forms.GroupBox groupBoxMergeRequestInformation;
      private System.Windows.Forms.Label labelTargetBranch;
      private System.Windows.Forms.Label labelSourceBranch;
      private System.Windows.Forms.Label labelTargetBranchLabel;
      private System.Windows.Forms.Label labelSourceBranchLabel;
      private System.Windows.Forms.Label labelProject;
      private System.Windows.Forms.Label labelProjectLabel;
      private System.Windows.Forms.Label labelAuthor;
      private System.Windows.Forms.Label labelAuthorLabel;
      private System.Windows.Forms.Label labelTitleLabel;
      protected TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanelTitle;
      protected System.Windows.Forms.CheckBox checkBoxSquash;
      protected System.Windows.Forms.CheckBox checkBoxDeleteSourceBranch;
      private System.Windows.Forms.GroupBox groupBoxMergeCommitMessage;
      private System.Windows.Forms.Label labelCommitMessageLabel;
      private System.Windows.Forms.ComboBox comboBoxCommit;
      private System.Windows.Forms.TextBox textBoxCommitMessage;
      private System.Windows.Forms.ToolTip toolTip;
      private System.Windows.Forms.LinkLabel linkLabelOpenAtGitLab;
      private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
      private System.Windows.Forms.GroupBox groupBoxRebase;
      private System.Windows.Forms.Button buttonRebase;
      private System.Windows.Forms.Label labelRebaseStatus;
      private System.Windows.Forms.GroupBox groupBoxDiscussions;
      private System.Windows.Forms.Button buttonDiscussions;
      private System.Windows.Forms.Label labelDiscussionStatus;
      private System.Windows.Forms.GroupBox groupBoxWorkInProgress;
      private System.Windows.Forms.Button buttonToggleDraft;
      private System.Windows.Forms.Label labelDraftStatus;
   }
}