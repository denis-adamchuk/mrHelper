namespace mrHelper.App.Controls
{
   partial class DiscussionFilterPanel
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

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.groupBoxFilter = new System.Windows.Forms.GroupBox();
         this.buttonApplyFilter = new System.Windows.Forms.Button();
         this.groupBox1 = new System.Windows.Forms.GroupBox();
         this.radioButtonShowUnansweredOnly = new System.Windows.Forms.RadioButton();
         this.radioButtonShowAnsweredOnly = new System.Windows.Forms.RadioButton();
         this.radioButtonNoFilterByAnswers = new System.Windows.Forms.RadioButton();
         this.checkBoxCreatedByMe = new System.Windows.Forms.CheckBox();
         this.groupBox2 = new System.Windows.Forms.GroupBox();
         this.radioButtonShowNotResolvedOnly = new System.Windows.Forms.RadioButton();
         this.radioButtonShowResolvedOnly = new System.Windows.Forms.RadioButton();
         this.radioButtonNoFilterByResolution = new System.Windows.Forms.RadioButton();
         this.groupBoxFilter.SuspendLayout();
         this.groupBox1.SuspendLayout();
         this.groupBox2.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxFilter
         // 
         this.groupBoxFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.groupBoxFilter.Controls.Add(this.groupBox2);
         this.groupBoxFilter.Controls.Add(this.buttonApplyFilter);
         this.groupBoxFilter.Controls.Add(this.groupBox1);
         this.groupBoxFilter.Controls.Add(this.checkBoxCreatedByMe);
         this.groupBoxFilter.Location = new System.Drawing.Point(3, 3);
         this.groupBoxFilter.Name = "groupBoxFilter";
         this.groupBoxFilter.Size = new System.Drawing.Size(457, 145);
         this.groupBoxFilter.TabIndex = 0;
         this.groupBoxFilter.TabStop = false;
         this.groupBoxFilter.Text = "Discussion Filter";
         // 
         // buttonApplyFilter
         // 
         this.buttonApplyFilter.Location = new System.Drawing.Point(377, 13);
         this.buttonApplyFilter.Name = "buttonApplyFilter";
         this.buttonApplyFilter.Size = new System.Drawing.Size(75, 23);
         this.buttonApplyFilter.TabIndex = 2;
         this.buttonApplyFilter.Text = "Apply";
         this.buttonApplyFilter.UseVisualStyleBackColor = true;
         this.buttonApplyFilter.Click += new System.EventHandler(this.ButtonApplyFilter_Click);
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.radioButtonShowUnansweredOnly);
         this.groupBox1.Controls.Add(this.radioButtonShowAnsweredOnly);
         this.groupBox1.Controls.Add(this.radioButtonNoFilterByAnswers);
         this.groupBox1.Location = new System.Drawing.Point(6, 42);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(220, 94);
         this.groupBox1.TabIndex = 1;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "Filter by answers";
         // 
         // radioButtonShowUnansweredOnly
         // 
         this.radioButtonShowUnansweredOnly.AutoSize = true;
         this.radioButtonShowUnansweredOnly.Location = new System.Drawing.Point(6, 65);
         this.radioButtonShowUnansweredOnly.Name = "radioButtonShowUnansweredOnly";
         this.radioButtonShowUnansweredOnly.Size = new System.Drawing.Size(210, 17);
         this.radioButtonShowUnansweredOnly.TabIndex = 2;
         this.radioButtonShowUnansweredOnly.TabStop = true;
         this.radioButtonShowUnansweredOnly.Text = "Show discussions without answers only";
         this.radioButtonShowUnansweredOnly.UseVisualStyleBackColor = true;
         // 
         // radioButtonShowAnsweredOnly
         // 
         this.radioButtonShowAnsweredOnly.AutoSize = true;
         this.radioButtonShowAnsweredOnly.Location = new System.Drawing.Point(6, 42);
         this.radioButtonShowAnsweredOnly.Name = "radioButtonShowAnsweredOnly";
         this.radioButtonShowAnsweredOnly.Size = new System.Drawing.Size(195, 17);
         this.radioButtonShowAnsweredOnly.TabIndex = 1;
         this.radioButtonShowAnsweredOnly.TabStop = true;
         this.radioButtonShowAnsweredOnly.Text = "Show discussions with answers only";
         this.radioButtonShowAnsweredOnly.UseVisualStyleBackColor = true;
         // 
         // radioButtonNoFilterByAnswers
         // 
         this.radioButtonNoFilterByAnswers.AutoSize = true;
         this.radioButtonNoFilterByAnswers.Location = new System.Drawing.Point(6, 19);
         this.radioButtonNoFilterByAnswers.Name = "radioButtonNoFilterByAnswers";
         this.radioButtonNoFilterByAnswers.Size = new System.Drawing.Size(66, 17);
         this.radioButtonNoFilterByAnswers.TabIndex = 0;
         this.radioButtonNoFilterByAnswers.TabStop = true;
         this.radioButtonNoFilterByAnswers.Text = "Show All";
         this.radioButtonNoFilterByAnswers.UseVisualStyleBackColor = true;
         // 
         // checkBoxCreatedByMe
         // 
         this.checkBoxCreatedByMe.AutoSize = true;
         this.checkBoxCreatedByMe.Location = new System.Drawing.Point(6, 19);
         this.checkBoxCreatedByMe.Name = "checkBoxCreatedByMe";
         this.checkBoxCreatedByMe.Size = new System.Drawing.Size(198, 17);
         this.checkBoxCreatedByMe.TabIndex = 0;
         this.checkBoxCreatedByMe.Text = "Show discussions started by me only";
         this.checkBoxCreatedByMe.UseVisualStyleBackColor = true;
         // 
         // groupBox2
         // 
         this.groupBox2.Controls.Add(this.radioButtonShowNotResolvedOnly);
         this.groupBox2.Controls.Add(this.radioButtonShowResolvedOnly);
         this.groupBox2.Controls.Add(this.radioButtonNoFilterByResolution);
         this.groupBox2.Location = new System.Drawing.Point(232, 42);
         this.groupBox2.Name = "groupBox2";
         this.groupBox2.Size = new System.Drawing.Size(220, 94);
         this.groupBox2.TabIndex = 3;
         this.groupBox2.TabStop = false;
         this.groupBox2.Text = "Filter by resolution";
         // 
         // radioButtonShowNotResolvedOnly
         // 
         this.radioButtonShowNotResolvedOnly.AutoSize = true;
         this.radioButtonShowNotResolvedOnly.Location = new System.Drawing.Point(6, 65);
         this.radioButtonShowNotResolvedOnly.Name = "radioButtonShowNotResolvedOnly";
         this.radioButtonShowNotResolvedOnly.Size = new System.Drawing.Size(192, 17);
         this.radioButtonShowNotResolvedOnly.TabIndex = 2;
         this.radioButtonShowNotResolvedOnly.TabStop = true;
         this.radioButtonShowNotResolvedOnly.Text = "Show not resolved discussions only";
         this.radioButtonShowNotResolvedOnly.UseVisualStyleBackColor = true;
         // 
         // radioButtonShowResolvedOnly
         // 
         this.radioButtonShowResolvedOnly.AutoSize = true;
         this.radioButtonShowResolvedOnly.Location = new System.Drawing.Point(6, 42);
         this.radioButtonShowResolvedOnly.Name = "radioButtonShowResolvedOnly";
         this.radioButtonShowResolvedOnly.Size = new System.Drawing.Size(174, 17);
         this.radioButtonShowResolvedOnly.TabIndex = 1;
         this.radioButtonShowResolvedOnly.TabStop = true;
         this.radioButtonShowResolvedOnly.Text = "Show resolved discussions only";
         this.radioButtonShowResolvedOnly.UseVisualStyleBackColor = true;
         // 
         // radioButtonNoFilterByResolution
         // 
         this.radioButtonNoFilterByResolution.AutoSize = true;
         this.radioButtonNoFilterByResolution.Location = new System.Drawing.Point(6, 19);
         this.radioButtonNoFilterByResolution.Name = "radioButtonNoFilterByResolution";
         this.radioButtonNoFilterByResolution.Size = new System.Drawing.Size(66, 17);
         this.radioButtonNoFilterByResolution.TabIndex = 0;
         this.radioButtonNoFilterByResolution.TabStop = true;
         this.radioButtonNoFilterByResolution.Text = "Show All";
         this.radioButtonNoFilterByResolution.UseVisualStyleBackColor = true;
         // 
         // DiscussionFilterPanel
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.groupBoxFilter);
         this.Name = "DiscussionFilterPanel";
         this.Size = new System.Drawing.Size(463, 151);
         this.groupBoxFilter.ResumeLayout(false);
         this.groupBoxFilter.PerformLayout();
         this.groupBox1.ResumeLayout(false);
         this.groupBox1.PerformLayout();
         this.groupBox2.ResumeLayout(false);
         this.groupBox2.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxFilter;
      private System.Windows.Forms.Button buttonApplyFilter;
      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.RadioButton radioButtonShowUnansweredOnly;
      private System.Windows.Forms.RadioButton radioButtonShowAnsweredOnly;
      private System.Windows.Forms.RadioButton radioButtonNoFilterByAnswers;
      private System.Windows.Forms.CheckBox checkBoxCreatedByMe;
      private System.Windows.Forms.GroupBox groupBox2;
      private System.Windows.Forms.RadioButton radioButtonShowNotResolvedOnly;
      private System.Windows.Forms.RadioButton radioButtonShowResolvedOnly;
      private System.Windows.Forms.RadioButton radioButtonNoFilterByResolution;
   }
}
