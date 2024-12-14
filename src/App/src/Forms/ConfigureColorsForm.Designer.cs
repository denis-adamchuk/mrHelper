using mrHelper.App.Controls;

namespace mrHelper.App.Forms
{
   partial class ConfigureColorsForm
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
         this.groupBoxColors = new System.Windows.Forms.GroupBox();
         this.tabControl = new System.Windows.Forms.TabControl();
         this.labelColorScheme = new System.Windows.Forms.Label();
         this.linkLabelResetAllColors = new System.Windows.Forms.LinkLabel();
         this.buttonClose = new System.Windows.Forms.Button();
         this.groupBoxColors.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxColors
         // 
         this.groupBoxColors.Controls.Add(this.tabControl);
         this.groupBoxColors.Controls.Add(this.linkLabelResetAllColors);
         this.groupBoxColors.Controls.Add(this.labelColorScheme);
         this.groupBoxColors.Controls.Add(this.buttonClose);
         this.groupBoxColors.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxColors.Location = new System.Drawing.Point(0, 0);
         this.groupBoxColors.Name = "groupBoxColors";
         this.groupBoxColors.Size = new System.Drawing.Size(545, 236);
         this.groupBoxColors.TabIndex = 0;
         this.groupBoxColors.TabStop = false;
         this.groupBoxColors.Text = "Colors";
         // 
         // tabControl
         // 
         this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.tabControl.Location = new System.Drawing.Point(9, 49);
         this.tabControl.Name = "tabControl";
         this.tabControl.SelectedIndex = 0;
         this.tabControl.Size = new System.Drawing.Size(530, 181);
         this.tabControl.TabIndex = 4;
         // 
         // linkLabelResetAllColors
         // 
         this.linkLabelResetAllColors.AutoSize = true;
         this.linkLabelResetAllColors.LinkColor = System.Drawing.Color.Black;
         this.linkLabelResetAllColors.Location = new System.Drawing.Point(294, 22);
         this.linkLabelResetAllColors.Name = "linkLabelResetAllColors";
         this.linkLabelResetAllColors.Size = new System.Drawing.Size(79, 13);
         this.linkLabelResetAllColors.TabIndex = 2;
         this.linkLabelResetAllColors.TabStop = true;
         this.linkLabelResetAllColors.Text = "Reset all colors";
         this.linkLabelResetAllColors.Visible = false;
         this.linkLabelResetAllColors.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelResetAllColors_LinkClicked);
         // 
         // labelColorScheme
         // 
         this.labelColorScheme.AutoSize = true;
         this.labelColorScheme.Location = new System.Drawing.Point(6, 22);
         this.labelColorScheme.Name = "labelColorScheme";
         this.labelColorScheme.Size = new System.Drawing.Size(71, 13);
         this.labelColorScheme.TabIndex = 0;
         this.labelColorScheme.Text = "Configure colors";
         // 
         // buttonClose
         // 
         this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonClose.Location = new System.Drawing.Point(460, 17);
         this.buttonClose.Name = "buttonClose";
         this.buttonClose.Size = new System.Drawing.Size(75, 23);
         this.buttonClose.TabIndex = 4;
         this.buttonClose.Text = "Close";
         this.buttonClose.UseVisualStyleBackColor = true;
         // 
         // ConfigureColorsForm
         // 
         this.AcceptButton = this.buttonClose;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(545, 236);
         this.Controls.Add(this.groupBoxColors);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "ConfigureColorsForm";
         this.Text = "Configure Colors";
         this.groupBoxColors.ResumeLayout(false);
         this.groupBoxColors.PerformLayout();
         this.ResumeLayout(false);
      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxColors;
      private System.Windows.Forms.LinkLabel linkLabelResetAllColors;
      private System.Windows.Forms.Button buttonClose;
      private System.Windows.Forms.Label labelColorScheme;
      private System.Windows.Forms.TabControl tabControl;
   }
}