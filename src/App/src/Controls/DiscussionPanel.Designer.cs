namespace mrHelper.App.Controls
{
   partial class DiscussionPanel
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

         _discussionSort.SortStateChanged -= onSortStateChanged;

         _displayFilter.FilterStateChanged -= onFilterChanged;

         _discussionLoader.Loaded -= onDiscussionsLoaded;

         _discussionLayout.DiffContextPositionChanged -= onDiffContextPositionChanged;
         _discussionLayout.DiscussionColumnWidthChanged -= onDiscussionColumnWidthChanged;
         _discussionLayout.NeedShiftRepliesChanged -= onNeedShiftRepliesChanged;

         _htmlTooltip.Dispose();

         base.Dispose(disposing);
      }

      #region Component Designer generated code

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
         this.pictureBox1.Location = new System.Drawing.Point(157, 3);
         this.pictureBox1.Name = "pictureBox1";
         this.pictureBox1.Size = new System.Drawing.Size(208, 135);
         this.pictureBox1.TabIndex = 0;
         this.pictureBox1.TabStop = false;
         this.pictureBox1.Visible = false;
         // 
         // DiscussionPanel
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
         this.AutoScroll = true;
         this.AutoScrollMargin = new System.Drawing.Size(0, 200);
         this.Controls.Add(this.pictureBox1);
         this.Name = "DiscussionPanel";
         this.Size = new System.Drawing.Size(368, 355);
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.PictureBox pictureBox1;
   }
}
