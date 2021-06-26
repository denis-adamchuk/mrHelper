
using System;

namespace mrHelper.App.Controls
{
   internal partial class RevisionSplitContainerSite
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
         this.splitContainer = new System.Windows.Forms.SplitContainer();
         this.revisionBrowser = new mrHelper.App.Controls.RevisionBrowser();
         this.textBox1 = new System.Windows.Forms.TextBox();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
         this.splitContainer.Panel1.SuspendLayout();
         this.splitContainer.Panel2.SuspendLayout();
         this.splitContainer.SuspendLayout();
         this.SuspendLayout();
         // 
         // splitContainer
         // 
         this.splitContainer.BackColor = System.Drawing.Color.LightGray;
         this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
         this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
         this.splitContainer.Location = new System.Drawing.Point(0, 0);
         this.splitContainer.Name = "splitContainer";
         this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
         // 
         // splitContainer.Panel1
         // 
         this.splitContainer.Panel1.BackColor = System.Drawing.SystemColors.Window;
         this.splitContainer.Panel1.Controls.Add(this.revisionBrowser);
         // 
         // splitContainer.Panel2
         // 
         this.splitContainer.Panel2.BackColor = System.Drawing.SystemColors.Window;
         this.splitContainer.Panel2.Controls.Add(this.textBox1);
         this.splitContainer.Size = new System.Drawing.Size(396, 374);
         this.splitContainer.SplitterDistance = 124;
         this.splitContainer.TabIndex = 0;
         // 
         // revisionBrowser
         // 
         this.revisionBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
         this.revisionBrowser.Location = new System.Drawing.Point(0, 0);
         this.revisionBrowser.Name = "revisionBrowser";
         this.revisionBrowser.Size = new System.Drawing.Size(396, 124);
         this.revisionBrowser.TabIndex = 0;
         this.revisionBrowser.SelectionChanged += new System.EventHandler(this.revisionBrowser_SelectionChanged);
         // 
         // textBox1
         // 
         this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
         this.textBox1.Location = new System.Drawing.Point(0, 0);
         this.textBox1.Multiline = true;
         this.textBox1.Name = "textBox1";
         this.textBox1.Size = new System.Drawing.Size(396, 246);
         this.textBox1.TabIndex = 0;
         // 
         // RevisionSplitContainerSite
         // 
         this.Controls.Add(this.splitContainer);
         this.Name = "RevisionSplitContainerSite";
         this.Size = new System.Drawing.Size(396, 374);
         this.splitContainer.Panel1.ResumeLayout(false);
         this.splitContainer.Panel2.ResumeLayout(false);
         this.splitContainer.Panel2.PerformLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
         this.splitContainer.ResumeLayout(false);
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.SplitContainer splitContainer;
      private App.Controls.RevisionBrowser revisionBrowser;
      private System.Windows.Forms.TextBox textBox1;
   }
}
