namespace mrHelper
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewDiscussionForm));
         this.textBoxDiscussionBody = new System.Windows.Forms.TextBox();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.buttonOK = new System.Windows.Forms.Button();
         this.textBoxFileName = new System.Windows.Forms.TextBox();
         this.textBoxLineNumber = new System.Windows.Forms.TextBox();
         this.labelFileName = new System.Windows.Forms.Label();
         this.labelLineNumber = new System.Windows.Forms.Label();
         this.labelContext = new System.Windows.Forms.Label();
         this.labelDiscussionBody = new System.Windows.Forms.Label();
         this.checkBoxIncludeContext = new System.Windows.Forms.CheckBox();
         this.textBoxContext = new System.Windows.Forms.RichTextBox();
         this.SuspendLayout();
         // 
         // textBoxDiscussionBody
         // 
         this.textBoxDiscussionBody.Location = new System.Drawing.Point(12, 193);
         this.textBoxDiscussionBody.Multiline = true;
         this.textBoxDiscussionBody.Name = "textBoxDiscussionBody";
         this.textBoxDiscussionBody.Size = new System.Drawing.Size(578, 120);
         this.textBoxDiscussionBody.TabIndex = 3;
         // 
         // buttonCancel
         // 
         this.buttonCancel.Location = new System.Drawing.Point(515, 319);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 5;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
         // 
         // buttonOK
         // 
         this.buttonOK.Location = new System.Drawing.Point(434, 319);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 4;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         this.buttonOK.Click += new System.EventHandler(this.ButtonOK_Click);
         // 
         // textBoxFileName
         // 
         this.textBoxFileName.Location = new System.Drawing.Point(12, 25);
         this.textBoxFileName.Name = "textBoxFileName";
         this.textBoxFileName.ReadOnly = true;
         this.textBoxFileName.Size = new System.Drawing.Size(497, 20);
         this.textBoxFileName.TabIndex = 0;
         // 
         // textBoxLineNumber
         // 
         this.textBoxLineNumber.Location = new System.Drawing.Point(515, 25);
         this.textBoxLineNumber.Name = "textBoxLineNumber";
         this.textBoxLineNumber.ReadOnly = true;
         this.textBoxLineNumber.Size = new System.Drawing.Size(75, 20);
         this.textBoxLineNumber.TabIndex = 1;
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
         // labelLineNumber
         // 
         this.labelLineNumber.AutoSize = true;
         this.labelLineNumber.Location = new System.Drawing.Point(512, 9);
         this.labelLineNumber.Name = "labelLineNumber";
         this.labelLineNumber.Size = new System.Drawing.Size(67, 13);
         this.labelLineNumber.TabIndex = 6;
         this.labelLineNumber.Text = "Line Number";
         // 
         // labelContext
         // 
         this.labelContext.AutoSize = true;
         this.labelContext.Location = new System.Drawing.Point(12, 57);
         this.labelContext.Name = "labelContext";
         this.labelContext.Size = new System.Drawing.Size(43, 13);
         this.labelContext.TabIndex = 8;
         this.labelContext.Text = "Context";
         // 
         // labelDiscussionBody
         // 
         this.labelDiscussionBody.AutoSize = true;
         this.labelDiscussionBody.Location = new System.Drawing.Point(12, 177);
         this.labelDiscussionBody.Name = "labelDiscussionBody";
         this.labelDiscussionBody.Size = new System.Drawing.Size(85, 13);
         this.labelDiscussionBody.TabIndex = 9;
         this.labelDiscussionBody.Text = "Discussion Body";
         // 
         // checkBoxIncludeContext
         // 
         this.checkBoxIncludeContext.AutoSize = true;
         this.checkBoxIncludeContext.Checked = true;
         this.checkBoxIncludeContext.CheckState = System.Windows.Forms.CheckState.Checked;
         this.checkBoxIncludeContext.Location = new System.Drawing.Point(17, 323);
         this.checkBoxIncludeContext.Name = "checkBoxIncludeContext";
         this.checkBoxIncludeContext.Size = new System.Drawing.Size(197, 17);
         this.checkBoxIncludeContext.TabIndex = 10;
         this.checkBoxIncludeContext.Text = "Include diff context in the discussion";
         this.checkBoxIncludeContext.UseVisualStyleBackColor = true;
         // 
         // textBoxContext
         // 
         this.textBoxContext.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
         this.textBoxContext.Location = new System.Drawing.Point(12, 73);
         this.textBoxContext.Name = "textBoxContext";
         this.textBoxContext.ReadOnly = true;
         this.textBoxContext.Size = new System.Drawing.Size(578, 96);
         this.textBoxContext.TabIndex = 11;
         this.textBoxContext.Text = "";
         // 
         // NewDiscussionForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(602, 353);
         this.Controls.Add(this.textBoxContext);
         this.Controls.Add(this.checkBoxIncludeContext);
         this.Controls.Add(this.labelDiscussionBody);
         this.Controls.Add(this.labelContext);
         this.Controls.Add(this.labelLineNumber);
         this.Controls.Add(this.labelFileName);
         this.Controls.Add(this.textBoxLineNumber);
         this.Controls.Add(this.textBoxFileName);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.textBoxDiscussionBody);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "NewDiscussionForm";
         this.Text = "New Discussion";
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.TextBox textBoxDiscussionBody;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.TextBox textBoxFileName;
      private System.Windows.Forms.TextBox textBoxLineNumber;
      private System.Windows.Forms.Label labelFileName;
      private System.Windows.Forms.Label labelLineNumber;
      private System.Windows.Forms.Label labelContext;
      private System.Windows.Forms.Label labelDiscussionBody;
      private System.Windows.Forms.CheckBox checkBoxIncludeContext;
      private System.Windows.Forms.RichTextBox textBoxContext;
   }
}