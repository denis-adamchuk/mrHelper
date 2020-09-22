namespace mrHelper.App.Forms
{
   partial class DiscussionsForm
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
         this.FilterPanel.Dispose();
         this.ActionsPanel.Dispose();
         this.SearchPanel.Dispose();
         base.Dispose(disposing);
      }

      #region Windows Form Designer generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.pictureBox1 = new System.Windows.Forms.PictureBox();
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
         this.SuspendLayout();
         // 
         // pictureBox1
         // 
         this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.pictureBox1.Location = new System.Drawing.Point(1096, 12);
         this.pictureBox1.Name = "pictureBox1";
         this.pictureBox1.Size = new System.Drawing.Size(245, 160);
         this.pictureBox1.TabIndex = 0;
         this.pictureBox1.TabStop = false;
         this.pictureBox1.Visible = false;
         // 
         // DiscussionsForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.AutoScroll = true;
         this.AutoScrollMargin = new System.Drawing.Size(0, 50);
         this.ClientSize = new System.Drawing.Size(1353, 456);
         this.Controls.Add(this.pictureBox1);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.KeyPreview = true;
         this.Name = "DiscussionsForm";
         this.Text = "Discussions";
         this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
         this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.DiscussionsForm_FormClosing);
         this.Shown += new System.EventHandler(this.DiscussionsForm_Shown);
         this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DiscussionsForm_KeyDown);
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.PictureBox pictureBox1;
   }
}
