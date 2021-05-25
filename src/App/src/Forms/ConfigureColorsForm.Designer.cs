
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
         this.tabPageGeneral = new System.Windows.Forms.TabPage();
         this.labelColorSelector = new System.Windows.Forms.Label();
         this.linkLabelResetToFactoryValue = new System.Windows.Forms.LinkLabel();
         this.comboBoxColorSelector = new System.Windows.Forms.ComboBox();
         this.listBoxColorSchemeItemSelector = new System.Windows.Forms.ListBox();
         this.tabPageDiscussions = new System.Windows.Forms.TabPage();
         this.linkLabelChangeDiscussionColor = new System.Windows.Forms.LinkLabel();
         this.linkLabelResetDiscussionColorToFactoryValue = new System.Windows.Forms.LinkLabel();
         this.listBoxDiscussionColorSchemeItemSelector = new System.Windows.Forms.ListBox();
         this.linkLabelResetAllColors = new System.Windows.Forms.LinkLabel();
         this.comboBoxColorSchemes = new System.Windows.Forms.ComboBox();
         this.labelColorScheme = new System.Windows.Forms.Label();
         this.buttonClose = new System.Windows.Forms.Button();
         this.colorDialog = new System.Windows.Forms.ColorDialog();
         this.groupBoxColors.SuspendLayout();
         this.tabControl.SuspendLayout();
         this.tabPageGeneral.SuspendLayout();
         this.tabPageDiscussions.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxColors
         // 
         this.groupBoxColors.Controls.Add(this.tabControl);
         this.groupBoxColors.Controls.Add(this.linkLabelResetAllColors);
         this.groupBoxColors.Controls.Add(this.comboBoxColorSchemes);
         this.groupBoxColors.Controls.Add(this.labelColorScheme);
         this.groupBoxColors.Controls.Add(this.buttonClose);
         this.groupBoxColors.Dock = System.Windows.Forms.DockStyle.Fill;
         this.groupBoxColors.Location = new System.Drawing.Point(0, 0);
         this.groupBoxColors.Name = "groupBoxColors";
         this.groupBoxColors.Size = new System.Drawing.Size(503, 205);
         this.groupBoxColors.TabIndex = 0;
         this.groupBoxColors.TabStop = false;
         this.groupBoxColors.Text = "Colors";
         // 
         // tabControl
         // 
         this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.tabControl.Controls.Add(this.tabPageGeneral);
         this.tabControl.Controls.Add(this.tabPageDiscussions);
         this.tabControl.Location = new System.Drawing.Point(9, 49);
         this.tabControl.Name = "tabControl";
         this.tabControl.SelectedIndex = 0;
         this.tabControl.Size = new System.Drawing.Size(488, 144);
         this.tabControl.TabIndex = 9;
         // 
         // tabPageGeneral
         // 
         this.tabPageGeneral.Controls.Add(this.labelColorSelector);
         this.tabPageGeneral.Controls.Add(this.linkLabelResetToFactoryValue);
         this.tabPageGeneral.Controls.Add(this.comboBoxColorSelector);
         this.tabPageGeneral.Controls.Add(this.listBoxColorSchemeItemSelector);
         this.tabPageGeneral.Location = new System.Drawing.Point(4, 22);
         this.tabPageGeneral.Name = "tabPageGeneral";
         this.tabPageGeneral.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageGeneral.Size = new System.Drawing.Size(480, 118);
         this.tabPageGeneral.TabIndex = 0;
         this.tabPageGeneral.Text = "General";
         this.tabPageGeneral.UseVisualStyleBackColor = true;
         // 
         // labelColorSelector
         // 
         this.labelColorSelector.AutoSize = true;
         this.labelColorSelector.Location = new System.Drawing.Point(345, 6);
         this.labelColorSelector.Name = "labelColorSelector";
         this.labelColorSelector.Size = new System.Drawing.Size(91, 13);
         this.labelColorSelector.TabIndex = 4;
         this.labelColorSelector.Text = "Background color";
         // 
         // linkLabelResetToFactoryValue
         // 
         this.linkLabelResetToFactoryValue.AutoSize = true;
         this.linkLabelResetToFactoryValue.Location = new System.Drawing.Point(345, 97);
         this.linkLabelResetToFactoryValue.Name = "linkLabelResetToFactoryValue";
         this.linkLabelResetToFactoryValue.Size = new System.Drawing.Size(104, 13);
         this.linkLabelResetToFactoryValue.TabIndex = 6;
         this.linkLabelResetToFactoryValue.TabStop = true;
         this.linkLabelResetToFactoryValue.Text = "Reset selected color";
         this.linkLabelResetToFactoryValue.Visible = false;
         this.linkLabelResetToFactoryValue.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelResetToFactoryValue_LinkClicked);
         // 
         // comboBoxColorSelector
         // 
         this.comboBoxColorSelector.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
         this.comboBoxColorSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxColorSelector.FormattingEnabled = true;
         this.comboBoxColorSelector.Location = new System.Drawing.Point(348, 22);
         this.comboBoxColorSelector.Name = "comboBoxColorSelector";
         this.comboBoxColorSelector.Size = new System.Drawing.Size(126, 21);
         this.comboBoxColorSelector.TabIndex = 5;
         this.comboBoxColorSelector.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboBoxColorSelector_DrawItem);
         this.comboBoxColorSelector.SelectedIndexChanged += new System.EventHandler(this.comboBoxColorSelector_SelectedIndexChanged);
         // 
         // listBoxColorSchemeItemSelector
         // 
         this.listBoxColorSchemeItemSelector.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
         this.listBoxColorSchemeItemSelector.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
         this.listBoxColorSchemeItemSelector.FormattingEnabled = true;
         this.listBoxColorSchemeItemSelector.Location = new System.Drawing.Point(6, 6);
         this.listBoxColorSchemeItemSelector.Name = "listBoxColorSchemeItemSelector";
         this.listBoxColorSchemeItemSelector.Size = new System.Drawing.Size(330, 104);
         this.listBoxColorSchemeItemSelector.TabIndex = 3;
         this.listBoxColorSchemeItemSelector.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBoxColorSchemeItemSelector_DrawItem);
         this.listBoxColorSchemeItemSelector.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.listBoxColorSchemeItemSelector_MeasureItem);
         this.listBoxColorSchemeItemSelector.SelectedIndexChanged += new System.EventHandler(this.listBoxColorSchemeItemSelector_SelectedIndexChanged);
         this.listBoxColorSchemeItemSelector.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.listBoxColorSchemeItemSelector_Format);
         // 
         // tabPageDiscussions
         // 
         this.tabPageDiscussions.Controls.Add(this.linkLabelChangeDiscussionColor);
         this.tabPageDiscussions.Controls.Add(this.linkLabelResetDiscussionColorToFactoryValue);
         this.tabPageDiscussions.Controls.Add(this.listBoxDiscussionColorSchemeItemSelector);
         this.tabPageDiscussions.Location = new System.Drawing.Point(4, 22);
         this.tabPageDiscussions.Name = "tabPageDiscussions";
         this.tabPageDiscussions.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageDiscussions.Size = new System.Drawing.Size(480, 118);
         this.tabPageDiscussions.TabIndex = 1;
         this.tabPageDiscussions.Text = "Discussions";
         this.tabPageDiscussions.UseVisualStyleBackColor = true;
         // 
         // linkLabelChangeDiscussionColor
         // 
         this.linkLabelChangeDiscussionColor.AutoSize = true;
         this.linkLabelChangeDiscussionColor.Location = new System.Drawing.Point(345, 8);
         this.linkLabelChangeDiscussionColor.Name = "linkLabelChangeDiscussionColor";
         this.linkLabelChangeDiscussionColor.Size = new System.Drawing.Size(130, 13);
         this.linkLabelChangeDiscussionColor.TabIndex = 8;
         this.linkLabelChangeDiscussionColor.TabStop = true;
         this.linkLabelChangeDiscussionColor.Text = "Change background color";
         this.linkLabelChangeDiscussionColor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelChangeDiscussionColor_LinkClicked);
         // 
         // linkLabelResetDiscussionColorToFactoryValue
         // 
         this.linkLabelResetDiscussionColorToFactoryValue.AutoSize = true;
         this.linkLabelResetDiscussionColorToFactoryValue.Location = new System.Drawing.Point(345, 97);
         this.linkLabelResetDiscussionColorToFactoryValue.Name = "linkLabelResetDiscussionColorToFactoryValue";
         this.linkLabelResetDiscussionColorToFactoryValue.Size = new System.Drawing.Size(104, 13);
         this.linkLabelResetDiscussionColorToFactoryValue.TabIndex = 7;
         this.linkLabelResetDiscussionColorToFactoryValue.TabStop = true;
         this.linkLabelResetDiscussionColorToFactoryValue.Text = "Reset selected color";
         this.linkLabelResetDiscussionColorToFactoryValue.Visible = false;
         this.linkLabelResetDiscussionColorToFactoryValue.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelResetDiscussionColorToFactoryValue_LinkClicked);
         // 
         // listBoxDiscussionColorSchemeItemSelector
         // 
         this.listBoxDiscussionColorSchemeItemSelector.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
         this.listBoxDiscussionColorSchemeItemSelector.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
         this.listBoxDiscussionColorSchemeItemSelector.FormattingEnabled = true;
         this.listBoxDiscussionColorSchemeItemSelector.Location = new System.Drawing.Point(6, 6);
         this.listBoxDiscussionColorSchemeItemSelector.Name = "listBoxDiscussionColorSchemeItemSelector";
         this.listBoxDiscussionColorSchemeItemSelector.Size = new System.Drawing.Size(330, 104);
         this.listBoxDiscussionColorSchemeItemSelector.TabIndex = 4;
         this.listBoxDiscussionColorSchemeItemSelector.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBoxColorSchemeItemSelector_DrawItem);
         this.listBoxDiscussionColorSchemeItemSelector.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.listBoxColorSchemeItemSelector_MeasureItem);
         this.listBoxDiscussionColorSchemeItemSelector.SelectedIndexChanged += new System.EventHandler(this.listBoxDiscussionColorSchemeItemSelector_SelectedIndexChanged);
         this.listBoxDiscussionColorSchemeItemSelector.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.listBoxColorSchemeItemSelector_Format);
         // 
         // linkLabelResetAllColors
         // 
         this.linkLabelResetAllColors.AutoSize = true;
         this.linkLabelResetAllColors.Location = new System.Drawing.Point(294, 22);
         this.linkLabelResetAllColors.Name = "linkLabelResetAllColors";
         this.linkLabelResetAllColors.Size = new System.Drawing.Size(79, 13);
         this.linkLabelResetAllColors.TabIndex = 2;
         this.linkLabelResetAllColors.TabStop = true;
         this.linkLabelResetAllColors.Text = "Reset all colors";
         this.linkLabelResetAllColors.Visible = false;
         this.linkLabelResetAllColors.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelResetAllColors_LinkClicked);
         // 
         // comboBoxColorSchemes
         // 
         this.comboBoxColorSchemes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxColorSchemes.FormattingEnabled = true;
         this.comboBoxColorSchemes.Location = new System.Drawing.Point(106, 19);
         this.comboBoxColorSchemes.Name = "comboBoxColorSchemes";
         this.comboBoxColorSchemes.Size = new System.Drawing.Size(182, 21);
         this.comboBoxColorSchemes.TabIndex = 1;
         this.comboBoxColorSchemes.SelectedIndexChanged += new System.EventHandler(this.comboBoxColorSchemes_SelectedIndexChanged);
         // 
         // labelColorScheme
         // 
         this.labelColorScheme.AutoSize = true;
         this.labelColorScheme.Location = new System.Drawing.Point(6, 22);
         this.labelColorScheme.Name = "labelColorScheme";
         this.labelColorScheme.Size = new System.Drawing.Size(71, 13);
         this.labelColorScheme.TabIndex = 0;
         this.labelColorScheme.Text = "Color scheme";
         // 
         // buttonClose
         // 
         this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonClose.Location = new System.Drawing.Point(418, 17);
         this.buttonClose.Name = "buttonClose";
         this.buttonClose.Size = new System.Drawing.Size(75, 23);
         this.buttonClose.TabIndex = 8;
         this.buttonClose.Text = "Close";
         this.buttonClose.UseVisualStyleBackColor = true;
         // 
         // ConfigureColorsForm
         // 
         this.AcceptButton = this.buttonClose;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(503, 205);
         this.Controls.Add(this.groupBoxColors);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "ConfigureColorsForm";
         this.Text = "Configure Colors";
         this.groupBoxColors.ResumeLayout(false);
         this.groupBoxColors.PerformLayout();
         this.tabControl.ResumeLayout(false);
         this.tabPageGeneral.ResumeLayout(false);
         this.tabPageGeneral.PerformLayout();
         this.tabPageDiscussions.ResumeLayout(false);
         this.tabPageDiscussions.PerformLayout();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.GroupBox groupBoxColors;
      private System.Windows.Forms.LinkLabel linkLabelResetAllColors;
      private System.Windows.Forms.LinkLabel linkLabelResetToFactoryValue;
      private System.Windows.Forms.Label labelColorSelector;
      private System.Windows.Forms.ComboBox comboBoxColorSelector;
      private System.Windows.Forms.ListBox listBoxColorSchemeItemSelector;
      private System.Windows.Forms.Button buttonClose;
      private System.Windows.Forms.ComboBox comboBoxColorSchemes;
      private System.Windows.Forms.Label labelColorScheme;
      private System.Windows.Forms.TabControl tabControl;
      private System.Windows.Forms.TabPage tabPageGeneral;
      private System.Windows.Forms.TabPage tabPageDiscussions;
      private System.Windows.Forms.LinkLabel linkLabelChangeDiscussionColor;
      private System.Windows.Forms.LinkLabel linkLabelResetDiscussionColorToFactoryValue;
      private System.Windows.Forms.ListBox listBoxDiscussionColorSchemeItemSelector;
      private System.Windows.Forms.ColorDialog colorDialog;
   }
}