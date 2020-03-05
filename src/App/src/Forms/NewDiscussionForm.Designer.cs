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
         this.buttonCancel = new System.Windows.Forms.Button();
         this.checkBoxIncludeContext = new System.Windows.Forms.CheckBox();
         this.labelDiscussionBody = new System.Windows.Forms.Label();
         this.labelContext = new System.Windows.Forms.Label();
         this.labelFileName = new System.Windows.Forms.Label();
         this.textBoxFileName = new System.Windows.Forms.TextBox();
         this.buttonOK = new System.Windows.Forms.Button();
         this.textBoxDiscussionBody = new System.Windows.Forms.TextBox();
         this.pictureBox1 = new System.Windows.Forms.PictureBox();
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
         this.SuspendLayout();
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(775, 229);
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
         this.checkBoxIncludeContext.Location = new System.Drawing.Point(675, 27);
         this.checkBoxIncludeContext.Name = "checkBoxIncludeContext";
         this.checkBoxIncludeContext.Size = new System.Drawing.Size(197, 17);
         this.checkBoxIncludeContext.TabIndex = 2;
         this.checkBoxIncludeContext.Text = "Include diff context in the discussion";
         this.checkBoxIncludeContext.UseVisualStyleBackColor = true;
         // 
         // labelDiscussionBody
         // 
         this.labelDiscussionBody.AutoSize = true;
         this.labelDiscussionBody.Location = new System.Drawing.Point(9, 159);
         this.labelDiscussionBody.Name = "labelDiscussionBody";
         this.labelDiscussionBody.Size = new System.Drawing.Size(85, 13);
         this.labelDiscussionBody.TabIndex = 9;
         this.labelDiscussionBody.Text = "Discussion Body";
         // 
         // labelContext
         // 
         this.labelContext.AutoSize = true;
         this.labelContext.Location = new System.Drawing.Point(9, 57);
         this.labelContext.Name = "labelContext";
         this.labelContext.Size = new System.Drawing.Size(43, 13);
         this.labelContext.TabIndex = 8;
         this.labelContext.Text = "Context";
         // 
         // labelFileName
         // 
         this.labelFileName.AutoSize = true;
         this.labelFileName.Location = new System.Drawing.Point(9, 9);
         this.labelFileName.Name = "labelFileName";
         this.labelFileName.Size = new System.Drawing.Size(54, 13);
         this.labelFileName.TabIndex = 5;
         this.labelFileName.Text = "File Name";
         // 
         // textBoxFileName
         // 
         this.textBoxFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxFileName.Location = new System.Drawing.Point(12, 25);
         this.textBoxFileName.Name = "textBoxFileName";
         this.textBoxFileName.ReadOnly = true;
         this.textBoxFileName.Size = new System.Drawing.Size(643, 20);
         this.textBoxFileName.TabIndex = 0;
         // 
         // buttonOK
         // 
         this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(775, 175);
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
         this.textBoxDiscussionBody.Location = new System.Drawing.Point(12, 175);
         this.textBoxDiscussionBody.Multiline = true;
         this.textBoxDiscussionBody.Name = "textBoxDiscussionBody";
         this.textBoxDiscussionBody.Size = new System.Drawing.Size(730, 77);
         this.textBoxDiscussionBody.TabIndex = 1;
         this.textBoxDiscussionBody.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxDiscussionBody_KeyDown);
         // 
         // pictureBox1
         // 
         this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.pictureBox1.Location = new System.Drawing.Point(748, 47);
         this.pictureBox1.Name = "pictureBox1";
         this.pictureBox1.Size = new System.Drawing.Size(124, 125);
         this.pictureBox1.TabIndex = 10;
         this.pictureBox1.TabStop = false;
         this.pictureBox1.Visible = false;
         // 
         // NewDiscussionForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(884, 264);
         this.Controls.Add(this.checkBoxIncludeContext);
         this.Controls.Add(this.labelDiscussionBody);
         this.Controls.Add(this.labelContext);
         this.Controls.Add(this.labelFileName);
         this.Controls.Add(this.textBoxFileName);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.textBoxDiscussionBody);
         this.Controls.Add(this.pictureBox1);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(900, 303);
         this.Name = "NewDiscussionForm";
         this.Text = "Create New Discussion";
         this.TopMost = true;
         this.Shown += new System.EventHandler(this.NewDiscussionForm_Shown);
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.TextBox textBoxDiscussionBody;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.TextBox textBoxFileName;
      private System.Windows.Forms.Label labelFileName;
      private System.Windows.Forms.Label labelContext;
      private System.Windows.Forms.Label labelDiscussionBody;
      private System.Windows.Forms.CheckBox checkBoxIncludeContext;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanel = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}
