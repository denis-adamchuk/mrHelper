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
         this.buttonCancel = new CommonControls.Controls.ConfirmCancelButton();
         this.checkBoxIncludeContext = new System.Windows.Forms.CheckBox();
         this.textBoxFileName = new System.Windows.Forms.TextBox();
         this.buttonOK = new System.Windows.Forms.Button();
         this.textBoxDiscussionBody = new System.Windows.Forms.TextBox();
         this.htmlContextCanvas = new System.Windows.Forms.Panel();
         this.SuspendLayout();
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
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
         this.checkBoxIncludeContext.TabIndex = 2;
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
         // 
         // buttonOK
         // 
         this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(667, 127);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 3;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         // 
         // textBoxDiscussionBody
         // 
         this.textBoxDiscussionBody.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxDiscussionBody.Location = new System.Drawing.Point(12, 127);
         this.textBoxDiscussionBody.Multiline = true;
         this.textBoxDiscussionBody.Name = "textBoxDiscussionBody";
         this.textBoxDiscussionBody.Size = new System.Drawing.Size(649, 82);
         this.textBoxDiscussionBody.TabIndex = 1;
         this.textBoxDiscussionBody.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxDiscussionBody_KeyDown);
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
         // NewDiscussionForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(754, 221);
         this.Controls.Add(this.htmlContextCanvas);
         this.Controls.Add(this.checkBoxIncludeContext);
         this.Controls.Add(this.textBoxFileName);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.textBoxDiscussionBody);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(770, 260);
         this.Name = "NewDiscussionForm";
         this.Text = "Start New Thread";
         this.TopMost = true;
         this.Shown += new System.EventHandler(this.NewDiscussionForm_Shown);
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.TextBox textBoxDiscussionBody;
      private CommonControls.Controls.ConfirmCancelButton buttonCancel;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.TextBox textBoxFileName;
      private System.Windows.Forms.CheckBox checkBoxIncludeContext;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanel = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
        private System.Windows.Forms.Panel htmlContextCanvas;
    }
}
