namespace mrHelper.App.Controls
{
   partial class DiscussionSortPanel
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
         this.groupBox1 = new System.Windows.Forms.GroupBox();
         this.radioButtonSortByReviewer = new System.Windows.Forms.RadioButton();
         this.radioButtonSortDefault = new System.Windows.Forms.RadioButton();
         this.groupBox1.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.radioButtonSortByReviewer);
         this.groupBox1.Controls.Add(this.radioButtonSortDefault);
         this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBox1.Location = new System.Drawing.Point(0, 0);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(169, 50);
         this.groupBox1.TabIndex = 0;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "Sort";
         // 
         // radioButtonSortByReviewer
         // 
         this.radioButtonSortByReviewer.AutoSize = true;
         this.radioButtonSortByReviewer.Location = new System.Drawing.Point(84, 19);
         this.radioButtonSortByReviewer.Name = "radioButtonSortByReviewer";
         this.radioButtonSortByReviewer.Size = new System.Drawing.Size(80, 17);
         this.radioButtonSortByReviewer.TabIndex = 1;
         this.radioButtonSortByReviewer.TabStop = true;
         this.radioButtonSortByReviewer.Text = "By reviewer";
         this.radioButtonSortByReviewer.UseVisualStyleBackColor = true;
         // 
         // radioButtonSortDefault
         // 
         this.radioButtonSortDefault.AutoSize = true;
         this.radioButtonSortDefault.Location = new System.Drawing.Point(6, 19);
         this.radioButtonSortDefault.Name = "radioButtonSortDefault";
         this.radioButtonSortDefault.Size = new System.Drawing.Size(59, 17);
         this.radioButtonSortDefault.TabIndex = 0;
         this.radioButtonSortDefault.TabStop = true;
         this.radioButtonSortDefault.Text = "Default";
         this.radioButtonSortDefault.UseVisualStyleBackColor = true;
         // 
         // DiscussionSortPanel
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
         this.Controls.Add(this.groupBox1);
         this.Name = "DiscussionSortPanel";
         this.Size = new System.Drawing.Size(169, 50);
         this.groupBox1.ResumeLayout(false);
         this.groupBox1.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.RadioButton radioButtonSortByReviewer;
      private System.Windows.Forms.RadioButton radioButtonSortDefault;
   }
}
