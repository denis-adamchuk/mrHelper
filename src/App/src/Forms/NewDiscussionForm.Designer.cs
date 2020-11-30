﻿namespace mrHelper.App.Forms
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
         this.tabControlMode = new System.Windows.Forms.TabControl();
         this.tabPageEdit = new System.Windows.Forms.TabPage();
         this.tabPagePreview = new System.Windows.Forms.TabPage();
         this.htmlPanelPreview = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.labelInvisibleCharactersHint = new System.Windows.Forms.Label();
         this.panelNavigation = new System.Windows.Forms.Panel();
         this.labelCounter = new System.Windows.Forms.Label();
         this.labelModificationsHint = new System.Windows.Forms.Label();
         this.htmlContextCanvas.SuspendLayout();
         this.tabControlMode.SuspendLayout();
         this.tabPageEdit.SuspendLayout();
         this.tabPagePreview.SuspendLayout();
         this.panelNavigation.SuspendLayout();
         this.SuspendLayout();
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.ConfirmationText = "All changes will be lost, are you sure?";
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(666, 222);
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
         this.checkBoxIncludeContext.Location = new System.Drawing.Point(545, 14);
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
         this.textBoxFileName.Size = new System.Drawing.Size(527, 20);
         this.textBoxFileName.TabIndex = 0;
         this.textBoxFileName.TabStop = false;
         // 
         // buttonOK
         // 
         this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(667, 193);
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
         this.htmlContextCanvas.Size = new System.Drawing.Size(730, 83);
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
         this.htmlPanelContext.Size = new System.Drawing.Size(728, 81);
         this.htmlPanelContext.TabIndex = 0;
         this.htmlPanelContext.Text = null;
         // 
         // textBoxDiscussionBodyHost
         // 
         this.textBoxDiscussionBodyHost.Dock = System.Windows.Forms.DockStyle.Fill;
         this.textBoxDiscussionBodyHost.Location = new System.Drawing.Point(3, 3);
         this.textBoxDiscussionBodyHost.Name = "textBoxDiscussionBodyHost";
         this.textBoxDiscussionBodyHost.Size = new System.Drawing.Size(635, 86);
         this.textBoxDiscussionBodyHost.TabIndex = 11;
         this.textBoxDiscussionBodyHost.Text = "textBoxDiscussionBodyHost";
         this.textBoxDiscussionBodyHost.Child = null;
         // 
         // buttonInsertCode
         // 
         this.buttonInsertCode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonInsertCode.Location = new System.Drawing.Point(667, 149);
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
         // tabControlMode
         // 
         this.tabControlMode.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.tabControlMode.Controls.Add(this.tabPageEdit);
         this.tabControlMode.Controls.Add(this.tabPagePreview);
         this.tabControlMode.Location = new System.Drawing.Point(12, 127);
         this.tabControlMode.Name = "tabControlMode";
         this.tabControlMode.SelectedIndex = 0;
         this.tabControlMode.Size = new System.Drawing.Size(649, 118);
         this.tabControlMode.TabIndex = 13;
         this.tabControlMode.SelectedIndexChanged += new System.EventHandler(this.tabControlMode_SelectedIndexChanged);
         // 
         // tabPageEdit
         // 
         this.tabPageEdit.Controls.Add(this.textBoxDiscussionBodyHost);
         this.tabPageEdit.Location = new System.Drawing.Point(4, 22);
         this.tabPageEdit.Name = "tabPageEdit";
         this.tabPageEdit.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageEdit.Size = new System.Drawing.Size(641, 92);
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
         this.tabPagePreview.Size = new System.Drawing.Size(641, 92);
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
         this.htmlPanelPreview.Size = new System.Drawing.Size(635, 86);
         this.htmlPanelPreview.TabIndex = 0;
         this.htmlPanelPreview.Text = null;
         // 
         // labelInvisibleCharactersHint
         // 
         this.labelInvisibleCharactersHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.labelInvisibleCharactersHint.AutoSize = true;
         this.labelInvisibleCharactersHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
         this.labelInvisibleCharactersHint.Location = new System.Drawing.Point(12, 244);
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
         this.panelNavigation.Location = new System.Drawing.Point(236, 124);
         this.panelNavigation.Margin = new System.Windows.Forms.Padding(0);
         this.panelNavigation.Name = "panelNavigation";
         this.panelNavigation.Size = new System.Drawing.Size(421, 22);
         this.panelNavigation.TabIndex = 24;
         this.panelNavigation.SizeChanged += new System.EventHandler(this.panelNavigation_SizeChanged);
         this.panelNavigation.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.panelNavigation_MouseWheel);
         // 
         // labelCounter
         // 
         this.labelCounter.AutoSize = true;
         this.labelCounter.Location = new System.Drawing.Point(293, 4);
         this.labelCounter.Name = "labelCounter";
         this.labelCounter.Size = new System.Drawing.Size(60, 13);
         this.labelCounter.TabIndex = 19;
         this.labelCounter.Text = "<100/100>";
         this.labelCounter.TextChanged += new System.EventHandler(this.labelCounter_TextChanged);
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
         // NewDiscussionForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(754, 262);
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
         this.MinimumSize = new System.Drawing.Size(770, 301);
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
   }
}
