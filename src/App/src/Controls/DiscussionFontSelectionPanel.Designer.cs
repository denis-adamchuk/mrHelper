namespace mrHelper.App.Controls
{
   partial class DiscussionFontSelectionPanel
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
         this.comboBoxFonts = new System.Windows.Forms.ComboBox();
         this.groupBox1.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.comboBoxFonts);
         this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBox1.Location = new System.Drawing.Point(0, 0);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(150, 50);
         this.groupBox1.TabIndex = 0;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "Font Size";
         // 
         // comboBoxFonts
         // 
         this.comboBoxFonts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxFonts.FormattingEnabled = true;
         this.comboBoxFonts.Location = new System.Drawing.Point(6, 19);
         this.comboBoxFonts.Name = "comboBoxFonts";
         this.comboBoxFonts.Size = new System.Drawing.Size(138, 21);
         this.comboBoxFonts.TabIndex = 0;
         this.comboBoxFonts.SelectionChangeCommitted += new System.EventHandler(this.comboBoxFonts_SelectionChangeCommitted);
         // 
         // DiscussionFontSelectionPanel
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
         this.Controls.Add(this.groupBox1);
         this.Name = "DiscussionFontSelectionPanel";
         this.Size = new System.Drawing.Size(150, 50);
         this.groupBox1.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBox1;
      private System.Windows.Forms.ComboBox comboBoxFonts;
   }
}
