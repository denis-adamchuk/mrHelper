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
         this.textBoxDiscussionBodyHost = new System.Windows.Forms.Integration.ElementHost();
         this.buttonInsertCode = new System.Windows.Forms.Button();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.SuspendLayout();
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.ConfirmationText = "All changes will be lost, are you sure?";
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(667, 186);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 4;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
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
         this.buttonOK.Location = new System.Drawing.Point(667, 157);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 3;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         // 
         // htmlContextCanvas
         // 
         this.htmlContextCanvas.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.htmlContextCanvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
         this.htmlContextCanvas.Location = new System.Drawing.Point(12, 38);
         this.htmlContextCanvas.Name = "htmlContextCanvas";
         this.htmlContextCanvas.Size = new System.Drawing.Size(730, 83);
         this.htmlContextCanvas.TabIndex = 10;
         // 
         // textBoxDiscussionBodyHost
         // 
         this.textBoxDiscussionBodyHost.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxDiscussionBodyHost.Location = new System.Drawing.Point(12, 127);
         this.textBoxDiscussionBodyHost.Name = "textBoxDiscussionBodyHost";
         this.textBoxDiscussionBodyHost.Size = new System.Drawing.Size(649, 82);
         this.textBoxDiscussionBodyHost.TabIndex = 11;
         this.textBoxDiscussionBodyHost.Text = "textBoxDiscussionBodyHost";
         this.textBoxDiscussionBodyHost.Child = null;
         // 
         // buttonInsertCode
         // 
         this.buttonInsertCode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonInsertCode.Location = new System.Drawing.Point(667, 127);
         this.buttonInsertCode.Name = "buttonInsertCode";
         this.buttonInsertCode.Size = new System.Drawing.Size(75, 23);
         this.buttonInsertCode.TabIndex = 12;
         this.buttonInsertCode.Text = "Insert code";
         this.toolTip.SetToolTip(this.buttonInsertCode, "Insert a placeholder for a code snippet");
         this.buttonInsertCode.UseVisualStyleBackColor = true;
         this.buttonInsertCode.Click += new System.EventHandler(this.buttonInsertCode_Click);
         // 
         // NewDiscussionForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(754, 221);
         this.Controls.Add(this.buttonInsertCode);
         this.Controls.Add(this.textBoxDiscussionBodyHost);
         this.Controls.Add(this.htmlContextCanvas);
         this.Controls.Add(this.checkBoxIncludeContext);
         this.Controls.Add(this.textBoxFileName);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonCancel);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(770, 260);
         this.Name = "NewDiscussionForm";
         this.Text = "Start New Thread";
         this.Shown += new System.EventHandler(this.NewDiscussionForm_Shown);
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion
      private CommonControls.Controls.ConfirmCancelButton buttonCancel;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.TextBox textBoxFileName;
      private System.Windows.Forms.CheckBox checkBoxIncludeContext;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanel = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
        private System.Windows.Forms.Panel htmlContextCanvas;
      private System.Windows.Controls.TextBox textBoxDiscussionBody;
      private System.Windows.Forms.Integration.ElementHost textBoxDiscussionBodyHost;
      private System.Windows.Forms.Button buttonInsertCode;
      private System.Windows.Forms.ToolTip toolTip;
   }
}
