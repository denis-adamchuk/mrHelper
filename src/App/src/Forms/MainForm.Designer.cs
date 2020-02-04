using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
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
         this.splitContainer1 = new System.Windows.Forms.SplitContainer();
         this.groupBox1 = new System.Windows.Forms.GroupBox();
         this.textBox1 = new System.Windows.Forms.TextBox();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
         this.splitContainer1.Panel1.SuspendLayout();
         this.splitContainer1.SuspendLayout();
         this.groupBox1.SuspendLayout();
         this.SuspendLayout();
         // 
         // splitContainer1
         // 
         this.splitContainer1.BackColor = System.Drawing.Color.Transparent;
         this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer1.Location = new System.Drawing.Point(0, 0);
         this.splitContainer1.Name = "splitContainer1";
         // 
         // splitContainer1.Panel1
         // 
         this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
         this.splitContainer1.Size = new System.Drawing.Size(567, 69);
         this.splitContainer1.SplitterDistance = 440;
         this.splitContainer1.SplitterWidth = 8;
         this.splitContainer1.TabIndex = 5;
         this.splitContainer1.TabStop = false;
         // 
         // groupBox1
         // 
         this.groupBox1.Controls.Add(this.textBox1);
         this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBox1.Location = new System.Drawing.Point(0, 0);
         this.groupBox1.Name = "groupBox1";
         this.groupBox1.Size = new System.Drawing.Size(440, 69);
         this.groupBox1.TabIndex = 13;
         this.groupBox1.TabStop = false;
         this.groupBox1.Text = "Dock = Fill";
         // 
         // textBox1
         // 
         this.textBox1.Anchor = System.Windows.Forms.AnchorStyles.Right;
         this.textBox1.BackColor = System.Drawing.SystemColors.Window;
         this.textBox1.Location = new System.Drawing.Point(2, 20);
         this.textBox1.Name = "textBox1";
         this.textBox1.Size = new System.Drawing.Size(436, 20);
         this.textBox1.TabIndex = 0;
         this.textBox1.Text = "Original Properties: Location.X = 3, Size.Width = 436, Anchor = Right";
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(567, 69);
         this.Controls.Add(this.splitContainer1);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.Name = "MainForm";
         this.Text = "Test Application";
         this.splitContainer1.Panel1.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
         this.splitContainer1.ResumeLayout(false);
         this.groupBox1.ResumeLayout(false);
         this.groupBox1.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion
      private SplitContainer splitContainer1;
      private GroupBox groupBox1;
      private TextBox textBox1;
   }
}

