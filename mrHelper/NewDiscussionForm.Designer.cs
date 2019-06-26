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
         this.labelDiscussionBody = new System.Windows.Forms.Label();
         this.checkBoxIncludeContext = new System.Windows.Forms.CheckBox();
         this.groupBoxContext = new System.Windows.Forms.GroupBox();
         this.radionContext2 = new System.Windows.Forms.RadioButton();
         this.radioContext1 = new System.Windows.Forms.RadioButton();
         this.textBoxContext2 = new System.Windows.Forms.RichTextBox();
         this.textBoxContext1 = new System.Windows.Forms.RichTextBox();
         this.textBoxLineNumber1 = new System.Windows.Forms.TextBox();
         this.textBoxFileName1 = new System.Windows.Forms.TextBox();
         this.textBoxLineNumber2 = new System.Windows.Forms.TextBox();
         this.textBoxFileName2 = new System.Windows.Forms.TextBox();
         this.groupBoxContext.SuspendLayout();
         this.SuspendLayout();
         // 
         // textBoxDiscussionBody
         // 
         this.textBoxDiscussionBody.Location = new System.Drawing.Point(9, 323);
         this.textBoxDiscussionBody.Multiline = true;
         this.textBoxDiscussionBody.Name = "textBoxDiscussionBody";
         this.textBoxDiscussionBody.Size = new System.Drawing.Size(578, 120);
         this.textBoxDiscussionBody.TabIndex = 3;
         // 
         // buttonCancel
         // 
         this.buttonCancel.Location = new System.Drawing.Point(512, 449);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 6;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         this.buttonCancel.Click += new System.EventHandler(this.ButtonCancel_Click);
         // 
         // buttonOK
         // 
         this.buttonOK.Location = new System.Drawing.Point(431, 449);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 5;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         this.buttonOK.Click += new System.EventHandler(this.ButtonOK_Click);
         // 
         // labelDiscussionBody
         // 
         this.labelDiscussionBody.AutoSize = true;
         this.labelDiscussionBody.Location = new System.Drawing.Point(9, 307);
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
         this.checkBoxIncludeContext.Location = new System.Drawing.Point(14, 453);
         this.checkBoxIncludeContext.Name = "checkBoxIncludeContext";
         this.checkBoxIncludeContext.Size = new System.Drawing.Size(197, 17);
         this.checkBoxIncludeContext.TabIndex = 4;
         this.checkBoxIncludeContext.Text = "Include diff context in the discussion";
         this.checkBoxIncludeContext.UseVisualStyleBackColor = true;
         // 
         // groupBoxContext
         // 
         this.groupBoxContext.Controls.Add(this.textBoxLineNumber2);
         this.groupBoxContext.Controls.Add(this.textBoxFileName2);
         this.groupBoxContext.Controls.Add(this.textBoxLineNumber1);
         this.groupBoxContext.Controls.Add(this.textBoxFileName1);
         this.groupBoxContext.Controls.Add(this.radionContext2);
         this.groupBoxContext.Controls.Add(this.radioContext1);
         this.groupBoxContext.Controls.Add(this.textBoxContext2);
         this.groupBoxContext.Controls.Add(this.textBoxContext1);
         this.groupBoxContext.Location = new System.Drawing.Point(12, 12);
         this.groupBoxContext.Name = "groupBoxContext";
         this.groupBoxContext.Size = new System.Drawing.Size(578, 283);
         this.groupBoxContext.TabIndex = 13;
         this.groupBoxContext.TabStop = false;
         this.groupBoxContext.Text = "Discussion Context";
         // 
         // radionContext2
         // 
         this.radionContext2.AutoSize = true;
         this.radionContext2.Location = new System.Drawing.Point(4, 155);
         this.radionContext2.Name = "radionContext2";
         this.radionContext2.Size = new System.Drawing.Size(14, 13);
         this.radionContext2.TabIndex = 16;
         this.radionContext2.UseVisualStyleBackColor = true;
         // 
         // radioContext1
         // 
         this.radioContext1.AutoSize = true;
         this.radioContext1.Checked = true;
         this.radioContext1.Location = new System.Drawing.Point(5, 22);
         this.radioContext1.Name = "radioContext1";
         this.radioContext1.Size = new System.Drawing.Size(14, 13);
         this.radioContext1.TabIndex = 15;
         this.radioContext1.TabStop = true;
         this.radioContext1.UseVisualStyleBackColor = true;
         // 
         // textBoxContext2
         // 
         this.textBoxContext2.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
         this.textBoxContext2.Location = new System.Drawing.Point(21, 177);
         this.textBoxContext2.Name = "textBoxContext2";
         this.textBoxContext2.ReadOnly = true;
         this.textBoxContext2.Size = new System.Drawing.Size(546, 96);
         this.textBoxContext2.TabIndex = 14;
         this.textBoxContext2.Text = "";
         // 
         // textBoxContext1
         // 
         this.textBoxContext1.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
         this.textBoxContext1.Location = new System.Drawing.Point(21, 45);
         this.textBoxContext1.Name = "textBoxContext1";
         this.textBoxContext1.ReadOnly = true;
         this.textBoxContext1.Size = new System.Drawing.Size(546, 96);
         this.textBoxContext1.TabIndex = 13;
         this.textBoxContext1.Text = "";
         // 
         // textBoxLineNumber1
         // 
         this.textBoxLineNumber1.Location = new System.Drawing.Point(520, 19);
         this.textBoxLineNumber1.Name = "textBoxLineNumber1";
         this.textBoxLineNumber1.ReadOnly = true;
         this.textBoxLineNumber1.Size = new System.Drawing.Size(47, 20);
         this.textBoxLineNumber1.TabIndex = 18;
         // 
         // textBoxFileName1
         // 
         this.textBoxFileName1.Location = new System.Drawing.Point(21, 19);
         this.textBoxFileName1.Name = "textBoxFileName1";
         this.textBoxFileName1.ReadOnly = true;
         this.textBoxFileName1.Size = new System.Drawing.Size(482, 20);
         this.textBoxFileName1.TabIndex = 17;
         // 
         // textBoxLineNumber2
         // 
         this.textBoxLineNumber2.Location = new System.Drawing.Point(520, 151);
         this.textBoxLineNumber2.Name = "textBoxLineNumber2";
         this.textBoxLineNumber2.ReadOnly = true;
         this.textBoxLineNumber2.Size = new System.Drawing.Size(47, 20);
         this.textBoxLineNumber2.TabIndex = 20;
         // 
         // textBoxFileName2
         // 
         this.textBoxFileName2.Location = new System.Drawing.Point(21, 151);
         this.textBoxFileName2.Name = "textBoxFileName2";
         this.textBoxFileName2.ReadOnly = true;
         this.textBoxFileName2.Size = new System.Drawing.Size(482, 20);
         this.textBoxFileName2.TabIndex = 19;
         // 
         // NewDiscussionForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(602, 484);
         this.Controls.Add(this.checkBoxIncludeContext);
         this.Controls.Add(this.labelDiscussionBody);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.textBoxDiscussionBody);
         this.Controls.Add(this.groupBoxContext);
         this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "NewDiscussionForm";
         this.Text = "New Discussion";
         this.groupBoxContext.ResumeLayout(false);
         this.groupBoxContext.PerformLayout();
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.TextBox textBoxDiscussionBody;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.Button buttonOK;
      private System.Windows.Forms.Label labelDiscussionBody;
      private System.Windows.Forms.CheckBox checkBoxIncludeContext;
      private System.Windows.Forms.GroupBox groupBoxContext;
      private System.Windows.Forms.RadioButton radionContext2;
      private System.Windows.Forms.RadioButton radioContext1;
      private System.Windows.Forms.RichTextBox textBoxContext2;
      private System.Windows.Forms.RichTextBox textBoxContext1;
      private System.Windows.Forms.TextBox textBoxLineNumber2;
      private System.Windows.Forms.TextBox textBoxFileName2;
      private System.Windows.Forms.TextBox textBoxLineNumber1;
      private System.Windows.Forms.TextBox textBoxFileName1;
   }
}