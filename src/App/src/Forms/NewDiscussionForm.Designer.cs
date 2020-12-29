namespace mrHelper.App.Forms
{
   partial class NewDiscussionForm
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
         this.buttonCancel = new mrHelper.CommonControls.Controls.ConfirmCancelButton();
         this.checkBoxIncludeContext = new System.Windows.Forms.CheckBox();
         this.textBoxFileName = new System.Windows.Forms.TextBox();
         this.buttonOK = new System.Windows.Forms.Button();
         this.htmlContextCanvas = new System.Windows.Forms.Panel();
         this.htmlPanelContext = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.textBoxDiscussionBodyHost = new System.Windows.Forms.Integration.ElementHost();
         this.buttonInsertCode = new System.Windows.Forms.Button();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.buttonPrev = new System.Windows.Forms.Button();
         this.buttonNext = new System.Windows.Forms.Button();
         this.buttonDelete = new System.Windows.Forms.Button();
         this.buttonPrevRelatedDiscussion = new System.Windows.Forms.Button();
         this.buttonNextRelatedDiscussion = new System.Windows.Forms.Button();
         this.tabControlMode = new System.Windows.Forms.TabControl();
         this.tabPageEdit = new System.Windows.Forms.TabPage();
         this.tabPagePreview = new System.Windows.Forms.TabPage();
         this.htmlPanelPreview = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.labelInvisibleCharactersHint = new System.Windows.Forms.Label();
         this.panelNavigation = new System.Windows.Forms.Panel();
         this.labelModificationsHint = new System.Windows.Forms.Label();
         this.labelCounter = new System.Windows.Forms.Label();
         this.checkBoxShowRelated = new System.Windows.Forms.CheckBox();
         this.groupBoxRelated = new System.Windows.Forms.GroupBox();
         this.labelRelatedDiscussionCounter = new System.Windows.Forms.Label();
         this.labelRelatedDiscussionLineNumber = new System.Windows.Forms.Label();
         this.labelRelatedDiscussionAuthor = new System.Windows.Forms.Label();
         this.htmlPanelPreviewRelatedDiscussion = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.htmlContextCanvas.SuspendLayout();
         this.tabControlMode.SuspendLayout();
         this.tabPageEdit.SuspendLayout();
         this.tabPagePreview.SuspendLayout();
         this.panelNavigation.SuspendLayout();
         this.groupBoxRelated.SuspendLayout();
         this.SuspendLayout();
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.ConfirmationText = "All changes will be lost, are you sure?";
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(742, 227);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 4;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
         // 
         // checkBoxIncludeContext
         // 
         this.checkBoxIncludeContext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.checkBoxIncludeContext.AutoSize = true;
         this.checkBoxIncludeContext.Checked = true;
         this.checkBoxIncludeContext.CheckState = System.Windows.Forms.CheckState.Checked;
         this.checkBoxIncludeContext.Location = new System.Drawing.Point(620, 14);
         this.checkBoxIncludeContext.Name = "checkBoxIncludeContext";
         this.checkBoxIncludeContext.Size = new System.Drawing.Size(197, 17);
         this.checkBoxIncludeContext.TabIndex = 5;
         this.checkBoxIncludeContext.Text = "Include diff context in the discussion";
         this.checkBoxIncludeContext.UseVisualStyleBackColor = true;
         // 
         // textBoxFileName
         // 
         this.textBoxFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxFileName.Location = new System.Drawing.Point(12, 11);
         this.textBoxFileName.Name = "textBoxFileName";
         this.textBoxFileName.ReadOnly = true;
         this.textBoxFileName.Size = new System.Drawing.Size(602, 20);
         this.textBoxFileName.TabIndex = 0;
         this.textBoxFileName.TabStop = false;
         // 
         // buttonOK
         // 
         this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(742, 198);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 3;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
         // 
         // htmlContextCanvas
         // 
         this.htmlContextCanvas.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.htmlContextCanvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.htmlContextCanvas.Controls.Add(this.htmlPanelContext);
         this.htmlContextCanvas.Location = new System.Drawing.Point(12, 38);
         this.htmlContextCanvas.Name = "htmlContextCanvas";
         this.htmlContextCanvas.Size = new System.Drawing.Size(805, 88);
         this.htmlContextCanvas.TabIndex = 10;
         // 
         // htmlPanelContext
         // 
         this.htmlPanelContext.AutoScroll = true;
         this.htmlPanelContext.BackColor = System.Drawing.SystemColors.Window;
         this.htmlPanelContext.BaseStylesheet = null;
         this.htmlPanelContext.Cursor = System.Windows.Forms.Cursors.IBeam;
         this.htmlPanelContext.Dock = System.Windows.Forms.DockStyle.Fill;
         this.htmlPanelContext.Location = new System.Drawing.Point(0, 0);
         this.htmlPanelContext.Name = "htmlPanelContext";
         this.htmlPanelContext.Size = new System.Drawing.Size(803, 86);
         this.htmlPanelContext.TabIndex = 0;
         this.htmlPanelContext.Text = null;
         // 
         // textBoxDiscussionBodyHost
         // 
         this.textBoxDiscussionBodyHost.Dock = System.Windows.Forms.DockStyle.Fill;
         this.textBoxDiscussionBodyHost.Location = new System.Drawing.Point(3, 3);
         this.textBoxDiscussionBodyHost.Name = "textBoxDiscussionBodyHost";
         this.textBoxDiscussionBodyHost.Size = new System.Drawing.Size(710, 86);
         this.textBoxDiscussionBodyHost.TabIndex = 11;
         this.textBoxDiscussionBodyHost.Text = "textBoxDiscussionBodyHost";
         this.textBoxDiscussionBodyHost.Child = null;
         // 
         // buttonInsertCode
         // 
         this.buttonInsertCode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonInsertCode.Location = new System.Drawing.Point(742, 154);
         this.buttonInsertCode.Name = "buttonInsertCode";
         this.buttonInsertCode.Size = new System.Drawing.Size(75, 23);
         this.buttonInsertCode.TabIndex = 12;
         this.buttonInsertCode.Text = "Insert code";
         this.toolTip.SetToolTip(this.buttonInsertCode, "Insert a placeholder for a code snippet");
         this.buttonInsertCode.UseVisualStyleBackColor = true;
         this.buttonInsertCode.Click += new System.EventHandler(this.buttonInsertCode_Click);
         // 
         // buttonPrev
         // 
         this.buttonPrev.Location = new System.Drawing.Point(268, 0);
         this.buttonPrev.Margin = new System.Windows.Forms.Padding(0);
         this.buttonPrev.Name = "buttonPrev";
         this.buttonPrev.Size = new System.Drawing.Size(22, 22);
         this.buttonPrev.TabIndex = 15;
         this.buttonPrev.Text = "<";
         this.toolTip.SetToolTip(this.buttonPrev, "Go to my previous discussion");
         this.buttonPrev.UseVisualStyleBackColor = true;
         this.buttonPrev.Click += new System.EventHandler(this.buttonPrev_Click);
         // 
         // buttonNext
         // 
         this.buttonNext.Location = new System.Drawing.Point(355, 0);
         this.buttonNext.Margin = new System.Windows.Forms.Padding(0);
         this.buttonNext.Name = "buttonNext";
         this.buttonNext.Size = new System.Drawing.Size(22, 22);
         this.buttonNext.TabIndex = 16;
         this.buttonNext.Text = ">";
         this.toolTip.SetToolTip(this.buttonNext, "Go to my next discussion");
         this.buttonNext.UseVisualStyleBackColor = true;
         this.buttonNext.Click += new System.EventHandler(this.buttonNext_Click);
         // 
         // buttonDelete
         // 
         this.buttonDelete.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonDelete.Location = new System.Drawing.Point(399, 0);
         this.buttonDelete.Margin = new System.Windows.Forms.Padding(0);
         this.buttonDelete.Name = "buttonDelete";
         this.buttonDelete.Size = new System.Drawing.Size(22, 22);
         this.buttonDelete.TabIndex = 21;
         this.buttonDelete.Text = "X";
         this.toolTip.SetToolTip(this.buttonDelete, "Delete current note");
         this.buttonDelete.UseVisualStyleBackColor = true;
         this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
         // 
         // buttonPrevRelatedDiscussion
         // 
         this.buttonPrevRelatedDiscussion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonPrevRelatedDiscussion.Location = new System.Drawing.Point(610, 7);
         this.buttonPrevRelatedDiscussion.Margin = new System.Windows.Forms.Padding(0);
         this.buttonPrevRelatedDiscussion.Name = "buttonPrevRelatedDiscussion";
         this.buttonPrevRelatedDiscussion.Size = new System.Drawing.Size(22, 22);
         this.buttonPrevRelatedDiscussion.TabIndex = 32;
         this.buttonPrevRelatedDiscussion.Text = "<";
         this.toolTip.SetToolTip(this.buttonPrevRelatedDiscussion, "Go to my previous discussion");
         this.buttonPrevRelatedDiscussion.UseVisualStyleBackColor = true;
         this.buttonPrevRelatedDiscussion.Click += new System.EventHandler(this.buttonRelatedPrev_Click);
         // 
         // buttonNextRelatedDiscussion
         // 
         this.buttonNextRelatedDiscussion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonNextRelatedDiscussion.Location = new System.Drawing.Point(697, 7);
         this.buttonNextRelatedDiscussion.Margin = new System.Windows.Forms.Padding(0);
         this.buttonNextRelatedDiscussion.Name = "buttonNextRelatedDiscussion";
         this.buttonNextRelatedDiscussion.Size = new System.Drawing.Size(22, 22);
         this.buttonNextRelatedDiscussion.TabIndex = 33;
         this.buttonNextRelatedDiscussion.Text = ">";
         this.toolTip.SetToolTip(this.buttonNextRelatedDiscussion, "Go to my next discussion");
         this.buttonNextRelatedDiscussion.UseVisualStyleBackColor = true;
         this.buttonNextRelatedDiscussion.Click += new System.EventHandler(this.buttonRelatedNext_Click);
         // 
         // tabControlMode
         // 
         this.tabControlMode.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.tabControlMode.Controls.Add(this.tabPageEdit);
         this.tabControlMode.Controls.Add(this.tabPagePreview);
         this.tabControlMode.Location = new System.Drawing.Point(12, 132);
         this.tabControlMode.Name = "tabControlMode";
         this.tabControlMode.SelectedIndex = 0;
         this.tabControlMode.Size = new System.Drawing.Size(724, 118);
         this.tabControlMode.TabIndex = 13;
         this.tabControlMode.SelectedIndexChanged += new System.EventHandler(this.tabControlMode_SelectedIndexChanged);
         // 
         // tabPageEdit
         // 
         this.tabPageEdit.Controls.Add(this.textBoxDiscussionBodyHost);
         this.tabPageEdit.Location = new System.Drawing.Point(4, 22);
         this.tabPageEdit.Name = "tabPageEdit";
         this.tabPageEdit.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageEdit.Size = new System.Drawing.Size(716, 92);
         this.tabPageEdit.TabIndex = 0;
         this.tabPageEdit.Text = "Edit";
         this.tabPageEdit.UseVisualStyleBackColor = true;
         // 
         // tabPagePreview
         // 
         this.tabPagePreview.Controls.Add(this.htmlPanelPreview);
         this.tabPagePreview.Location = new System.Drawing.Point(4, 22);
         this.tabPagePreview.Name = "tabPagePreview";
         this.tabPagePreview.Padding = new System.Windows.Forms.Padding(3);
         this.tabPagePreview.Size = new System.Drawing.Size(716, 92);
         this.tabPagePreview.TabIndex = 1;
         this.tabPagePreview.Text = "Preview";
         this.tabPagePreview.UseVisualStyleBackColor = true;
         // 
         // htmlPanelPreview
         // 
         this.htmlPanelPreview.AutoScroll = true;
         this.htmlPanelPreview.BackColor = System.Drawing.SystemColors.Window;
         this.htmlPanelPreview.BaseStylesheet = null;
         this.htmlPanelPreview.Cursor = System.Windows.Forms.Cursors.IBeam;
         this.htmlPanelPreview.Dock = System.Windows.Forms.DockStyle.Fill;
         this.htmlPanelPreview.Location = new System.Drawing.Point(3, 3);
         this.htmlPanelPreview.Name = "htmlPanelPreview";
         this.htmlPanelPreview.Size = new System.Drawing.Size(710, 86);
         this.htmlPanelPreview.TabIndex = 0;
         this.htmlPanelPreview.Text = null;
         // 
         // labelInvisibleCharactersHint
         // 
         this.labelInvisibleCharactersHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.labelInvisibleCharactersHint.AutoSize = true;
         this.labelInvisibleCharactersHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
         this.labelInvisibleCharactersHint.Location = new System.Drawing.Point(12, 253);
         this.labelInvisibleCharactersHint.Name = "labelInvisibleCharactersHint";
         this.labelInvisibleCharactersHint.Size = new System.Drawing.Size(149, 13);
         this.labelInvisibleCharactersHint.TabIndex = 14;
         this.labelInvisibleCharactersHint.Text = "<labelInvisibleCharactersHint>";
         this.labelInvisibleCharactersHint.Visible = false;
         // 
         // panelNavigation
         // 
         this.panelNavigation.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.panelNavigation.Controls.Add(this.labelModificationsHint);
         this.panelNavigation.Controls.Add(this.buttonPrev);
         this.panelNavigation.Controls.Add(this.labelCounter);
         this.panelNavigation.Controls.Add(this.buttonNext);
         this.panelNavigation.Controls.Add(this.buttonDelete);
         this.panelNavigation.Location = new System.Drawing.Point(311, 129);
         this.panelNavigation.Margin = new System.Windows.Forms.Padding(0);
         this.panelNavigation.Name = "panelNavigation";
         this.panelNavigation.Size = new System.Drawing.Size(421, 22);
         this.panelNavigation.TabIndex = 24;
         this.panelNavigation.SizeChanged += new System.EventHandler(this.panelNavigation_SizeChanged);
         this.panelNavigation.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.panelNavigation_MouseWheel);
         // 
         // labelModificationsHint
         // 
         this.labelModificationsHint.AutoSize = true;
         this.labelModificationsHint.Location = new System.Drawing.Point(3, 4);
         this.labelModificationsHint.Name = "labelModificationsHint";
         this.labelModificationsHint.Size = new System.Drawing.Size(235, 13);
         this.labelModificationsHint.TabIndex = 22;
         this.labelModificationsHint.Text = "Modifications are applied when the dialog closes";
         this.labelModificationsHint.Visible = false;
         // 
         // labelCounter
         // 
         this.labelCounter.AutoSize = true;
         this.labelCounter.Location = new System.Drawing.Point(293, 4);
         this.labelCounter.Name = "labelCounter";
         this.labelCounter.Size = new System.Drawing.Size(60, 13);
         this.labelCounter.TabIndex = 19;
         this.labelCounter.Text = "<100/100>";
         this.labelCounter.SizeChanged += new System.EventHandler(this.labelCounter_SizeChanged);
         // 
         // checkBoxShowRelated
         // 
         this.checkBoxShowRelated.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.checkBoxShowRelated.AutoSize = true;
         this.checkBoxShowRelated.Checked = true;
         this.checkBoxShowRelated.CheckState = System.Windows.Forms.CheckState.Checked;
         this.checkBoxShowRelated.Location = new System.Drawing.Point(648, 252);
         this.checkBoxShowRelated.Name = "checkBoxShowRelated";
         this.checkBoxShowRelated.Size = new System.Drawing.Size(88, 17);
         this.checkBoxShowRelated.TabIndex = 27;
         this.checkBoxShowRelated.Text = "Show related";
         this.checkBoxShowRelated.UseVisualStyleBackColor = true;
         this.checkBoxShowRelated.CheckedChanged += new System.EventHandler(this.checkBoxShowRelated_CheckedChanged);
         // 
         // groupBoxRelated
         // 
         this.groupBoxRelated.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxRelated.Controls.Add(this.buttonPrevRelatedDiscussion);
         this.groupBoxRelated.Controls.Add(this.labelRelatedDiscussionCounter);
         this.groupBoxRelated.Controls.Add(this.buttonNextRelatedDiscussion);
         this.groupBoxRelated.Controls.Add(this.labelRelatedDiscussionLineNumber);
         this.groupBoxRelated.Controls.Add(this.labelRelatedDiscussionAuthor);
         this.groupBoxRelated.Controls.Add(this.htmlPanelPreviewRelatedDiscussion);
         this.groupBoxRelated.Location = new System.Drawing.Point(13, 270);
         this.groupBoxRelated.Name = "groupBoxRelated";
         this.groupBoxRelated.Size = new System.Drawing.Size(723, 131);
         this.groupBoxRelated.TabIndex = 28;
         this.groupBoxRelated.TabStop = false;
         this.groupBoxRelated.Text = "Related threads";
         this.groupBoxRelated.SizeChanged += new System.EventHandler(this.groupBoxRelated_SizeChanged);
         this.groupBoxRelated.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.groupBoxRelated_MouseWheel);
         // 
         // labelRelatedDiscussionCounter
         // 
         this.labelRelatedDiscussionCounter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.labelRelatedDiscussionCounter.AutoSize = true;
         this.labelRelatedDiscussionCounter.Location = new System.Drawing.Point(635, 11);
         this.labelRelatedDiscussionCounter.Name = "labelRelatedDiscussionCounter";
         this.labelRelatedDiscussionCounter.Size = new System.Drawing.Size(60, 13);
         this.labelRelatedDiscussionCounter.TabIndex = 34;
         this.labelRelatedDiscussionCounter.Text = "<100/100>";
         this.labelRelatedDiscussionCounter.SizeChanged += new System.EventHandler(this.labelRelatedDiscussionCounter_SizeChanged);
         // 
         // labelRelatedDiscussionLineNumber
         // 
         this.labelRelatedDiscussionLineNumber.AutoSize = true;
         this.labelRelatedDiscussionLineNumber.Location = new System.Drawing.Point(6, 12);
         this.labelRelatedDiscussionLineNumber.Name = "labelRelatedDiscussionLineNumber";
         this.labelRelatedDiscussionLineNumber.Size = new System.Drawing.Size(69, 13);
         this.labelRelatedDiscussionLineNumber.TabIndex = 31;
         this.labelRelatedDiscussionLineNumber.Text = "Line: <0000>";
         // 
         // labelRelatedDiscussionAuthor
         // 
         this.labelRelatedDiscussionAuthor.AutoSize = true;
         this.labelRelatedDiscussionAuthor.Location = new System.Drawing.Point(158, 12);
         this.labelRelatedDiscussionAuthor.Name = "labelRelatedDiscussionAuthor";
         this.labelRelatedDiscussionAuthor.Size = new System.Drawing.Size(68, 13);
         this.labelRelatedDiscussionAuthor.TabIndex = 30;
         this.labelRelatedDiscussionAuthor.Text = "Author: <....>";
         // 
         // htmlPanelPreviewRelatedDiscussion
         // 
         this.htmlPanelPreviewRelatedDiscussion.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.htmlPanelPreviewRelatedDiscussion.AutoScroll = true;
         this.htmlPanelPreviewRelatedDiscussion.BackColor = System.Drawing.SystemColors.Window;
         this.htmlPanelPreviewRelatedDiscussion.BaseStylesheet = null;
         this.htmlPanelPreviewRelatedDiscussion.Cursor = System.Windows.Forms.Cursors.IBeam;
         this.htmlPanelPreviewRelatedDiscussion.Location = new System.Drawing.Point(6, 32);
         this.htmlPanelPreviewRelatedDiscussion.Name = "htmlPanelPreviewRelatedDiscussion";
         this.htmlPanelPreviewRelatedDiscussion.Size = new System.Drawing.Size(710, 86);
         this.htmlPanelPreviewRelatedDiscussion.TabIndex = 29;
         this.htmlPanelPreviewRelatedDiscussion.Text = null;
         // 
         // NewDiscussionForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(829, 406);
         this.Controls.Add(this.groupBoxRelated);
         this.Controls.Add(this.checkBoxShowRelated);
         this.Controls.Add(this.panelNavigation);
         this.Controls.Add(this.labelInvisibleCharactersHint);
         this.Controls.Add(this.tabControlMode);
         this.Controls.Add(this.buttonInsertCode);
         this.Controls.Add(this.htmlContextCanvas);
         this.Controls.Add(this.checkBoxIncludeContext);
         this.Controls.Add(this.textBoxFileName);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonCancel);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimumSize = new System.Drawing.Size(845, 445);
         this.Name = "NewDiscussionForm";
         this.Text = "Start a thread";
         this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.NewDiscussionForm_FormClosed);
         this.Shown += new System.EventHandler(this.newDiscussionForm_Shown);
         this.htmlContextCanvas.ResumeLayout(false);
         this.tabControlMode.ResumeLayout(false);
         this.tabPageEdit.ResumeLayout(false);
         this.tabPagePreview.ResumeLayout(false);
         this.panelNavigation.ResumeLayout(false);
         this.panelNavigation.PerformLayout();
         this.groupBoxRelated.ResumeLayout(false);
         this.groupBoxRelated.PerformLayout();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion
      private CommonControls.Controls.ConfirmCancelButton buttonCancel;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.TextBox textBoxFileName;
      private System.Windows.Forms.CheckBox checkBoxIncludeContext;
        private System.Windows.Forms.Panel htmlContextCanvas;
      private System.Windows.Controls.TextBox textBoxDiscussionBody;
      private System.Windows.Forms.Integration.ElementHost textBoxDiscussionBodyHost;
      private System.Windows.Forms.Button buttonInsertCode;
      private System.Windows.Forms.ToolTip toolTip;
      private System.Windows.Forms.TabControl tabControlMode;
      private System.Windows.Forms.TabPage tabPageEdit;
      private System.Windows.Forms.TabPage tabPagePreview;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanelPreview;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanelContext;
      private System.Windows.Forms.Label labelInvisibleCharactersHint;
      private System.Windows.Forms.Panel panelNavigation;
      private System.Windows.Forms.Button buttonPrev;
      private System.Windows.Forms.Label labelCounter;
      private System.Windows.Forms.Button buttonNext;
      private System.Windows.Forms.Button buttonDelete;
      private System.Windows.Forms.Label labelModificationsHint;
      private System.Windows.Forms.CheckBox checkBoxShowRelated;
      private System.Windows.Forms.GroupBox groupBoxRelated;
      private System.Windows.Forms.Button buttonPrevRelatedDiscussion;
      private System.Windows.Forms.Label labelRelatedDiscussionCounter;
      private System.Windows.Forms.Button buttonNextRelatedDiscussion;
      private System.Windows.Forms.Label labelRelatedDiscussionLineNumber;
      private System.Windows.Forms.Label labelRelatedDiscussionAuthor;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanelPreviewRelatedDiscussion;
   }
}
