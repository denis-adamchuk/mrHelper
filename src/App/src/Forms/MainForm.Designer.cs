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
         this.components = new System.ComponentModel.Container();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
         this.restoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
         this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
         this.localGitFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
         this.panel4 = new System.Windows.Forms.Panel();
         this.panel1 = new System.Windows.Forms.Panel();
         this.splitContainer1 = new System.Windows.Forms.SplitContainer();
         this.groupBoxSelectedMR = new System.Windows.Forms.GroupBox();
         this.textBox1 = new System.Windows.Forms.TextBox();
         this.comboBoxFonts = new System.Windows.Forms.ComboBox();
         this.contextMenuStrip.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
         this.splitContainer1.Panel1.SuspendLayout();
         this.splitContainer1.Panel2.SuspendLayout();
         this.splitContainer1.SuspendLayout();
         this.groupBoxSelectedMR.SuspendLayout();
         this.SuspendLayout();
         // 
         // toolTip
         // 
         this.toolTip.AutoPopDelay = 5000;
         this.toolTip.InitialDelay = 500;
         this.toolTip.ReshowDelay = 100;
         // 
         // contextMenuStrip
         // 
         this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.restoreToolStripMenuItem,
            this.exitToolStripMenuItem});
         this.contextMenuStrip.Name = "contextMenuStrip1";
         this.contextMenuStrip.Size = new System.Drawing.Size(114, 48);
         // 
         // restoreToolStripMenuItem
         // 
         this.restoreToolStripMenuItem.Name = "restoreToolStripMenuItem";
         this.restoreToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
         this.restoreToolStripMenuItem.Text = "Restore";
         // 
         // exitToolStripMenuItem
         // 
         this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
         this.exitToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
         this.exitToolStripMenuItem.Text = "Exit";
         // 
         // notifyIcon
         // 
         this.notifyIcon.BalloonTipText = "I will now live in your tray";
         this.notifyIcon.ContextMenuStrip = this.contextMenuStrip;
         this.notifyIcon.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.notifyIcon.Text = "Merge Request Helper";
         this.notifyIcon.Visible = true;
         // 
         // localGitFolderBrowser
         // 
         this.localGitFolderBrowser.Description = "Select a folder where git repository will be stored locally";
         this.localGitFolderBrowser.RootFolder = System.Environment.SpecialFolder.MyComputer;
         // 
         // panel4
         // 
         this.panel4.Dock = System.Windows.Forms.DockStyle.Top;
         this.panel4.Location = new System.Drawing.Point(0, 159);
         this.panel4.Name = "panel4";
         this.panel4.Size = new System.Drawing.Size(910, 79);
         this.panel4.TabIndex = 14;
         // 
         // panel1
         // 
         this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
         this.panel1.Location = new System.Drawing.Point(0, 80);
         this.panel1.Name = "panel1";
         this.panel1.Size = new System.Drawing.Size(910, 79);
         this.panel1.TabIndex = 5;
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
         this.splitContainer1.Panel1.Controls.Add(this.groupBoxSelectedMR);
         // 
         // splitContainer1.Panel2
         // 
         this.splitContainer1.Panel2.Controls.Add(this.comboBoxFonts);
         this.splitContainer1.Size = new System.Drawing.Size(797, 417);
         this.splitContainer1.SplitterDistance = 620;
         this.splitContainer1.SplitterWidth = 8;
         this.splitContainer1.TabIndex = 5;
         this.splitContainer1.TabStop = false;
         // 
         // groupBoxSelectedMR
         // 
         this.groupBoxSelectedMR.Anchor = System.Windows.Forms.AnchorStyles.Right;
         this.groupBoxSelectedMR.Controls.Add(this.textBox1);
         this.groupBoxSelectedMR.Location = new System.Drawing.Point(0, 0);
         this.groupBoxSelectedMR.Name = "groupBoxSelectedMR";
         this.groupBoxSelectedMR.Size = new System.Drawing.Size(620, 417);
         this.groupBoxSelectedMR.TabIndex = 13;
         this.groupBoxSelectedMR.TabStop = false;
         this.groupBoxSelectedMR.Text = "Merge Request";
         // 
         // textBox1
         // 
         this.textBox1.Anchor = System.Windows.Forms.AnchorStyles.Right;
         this.textBox1.BackColor = System.Drawing.SystemColors.Window;
         this.textBox1.Location = new System.Drawing.Point(3, 16);
         this.textBox1.MinimumSize = new System.Drawing.Size(4, 4);
         this.textBox1.Multiline = true;
         this.textBox1.Name = "textBox1";
         this.textBox1.Size = new System.Drawing.Size(614, 395);
         this.textBox1.TabIndex = 0;
         // 
         // comboBoxFonts
         // 
         this.comboBoxFonts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxFonts.FormattingEnabled = true;
         this.comboBoxFonts.Location = new System.Drawing.Point(41, 36);
         this.comboBoxFonts.Name = "comboBoxFonts";
         this.comboBoxFonts.Size = new System.Drawing.Size(96, 21);
         this.comboBoxFonts.TabIndex = 13;
         this.comboBoxFonts.SelectedIndexChanged += new System.EventHandler(this.comboBoxFonts_SelectedIndexChanged);
         // 
         // MainForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(797, 417);
         this.Controls.Add(this.splitContainer1);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.Name = "MainForm";
         this.Text = "Merge Request Helper";
         this.contextMenuStrip.ResumeLayout(false);
         this.splitContainer1.Panel1.ResumeLayout(false);
         this.splitContainer1.Panel2.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
         this.splitContainer1.ResumeLayout(false);
         this.groupBoxSelectedMR.ResumeLayout(false);
         this.groupBoxSelectedMR.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion
      private System.Windows.Forms.ToolTip toolTip;
      private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
      private System.Windows.Forms.ToolStripMenuItem restoreToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
      private System.Windows.Forms.NotifyIcon notifyIcon;
      private System.Windows.Forms.FolderBrowserDialog localGitFolderBrowser;
      private System.Windows.Forms.Panel panel1;
      private System.Windows.Forms.Panel panel4;
      private SplitContainer splitContainer1;
      private GroupBox groupBoxSelectedMR;
      private TextBox textBox1;
      private ComboBox comboBoxFonts;
   }
}

