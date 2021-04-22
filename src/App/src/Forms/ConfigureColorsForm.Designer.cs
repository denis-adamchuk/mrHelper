
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
         this.buttonClose = new System.Windows.Forms.Button();
         this.labelDiscussionsViewColorPaletteHint = new System.Windows.Forms.Label();
         this.linkLabelResetAllColors = new System.Windows.Forms.LinkLabel();
         this.linkLabelResetToFactoryValue = new System.Windows.Forms.LinkLabel();
         this.labelColorSelector = new System.Windows.Forms.Label();
         this.comboBoxColorSelector = new System.Windows.Forms.ComboBox();
         this.listBoxColorSchemeItemSelector = new System.Windows.Forms.ListBox();
         this.labelColorScheme = new System.Windows.Forms.Label();
         this.comboBoxColorSchemes = new System.Windows.Forms.ComboBox();
         this.groupBoxColors.SuspendLayout();
         this.SuspendLayout();
         // 
         // groupBoxColors
         // 
         this.groupBoxColors.Controls.Add(this.buttonClose);
         this.groupBoxColors.Controls.Add(this.labelDiscussionsViewColorPaletteHint);
         this.groupBoxColors.Controls.Add(this.linkLabelResetAllColors);
         this.groupBoxColors.Controls.Add(this.linkLabelResetToFactoryValue);
         this.groupBoxColors.Controls.Add(this.labelColorSelector);
         this.groupBoxColors.Controls.Add(this.comboBoxColorSelector);
         this.groupBoxColors.Controls.Add(this.listBoxColorSchemeItemSelector);
         this.groupBoxColors.Controls.Add(this.comboBoxColorSchemes);
         this.groupBoxColors.Controls.Add(this.labelColorScheme);
         this.groupBoxColors.Location = new System.Drawing.Point(11, 8);
         this.groupBoxColors.Name = "groupBoxColors";
         this.groupBoxColors.Size = new System.Drawing.Size(577, 229);
         this.groupBoxColors.TabIndex = 0;
         this.groupBoxColors.TabStop = false;
         this.groupBoxColors.Text = "Colors";
         // 
         // buttonClose
         // 
         this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonClose.Location = new System.Drawing.Point(496, 196);
         this.buttonClose.Name = "buttonClose";
         this.buttonClose.Size = new System.Drawing.Size(75, 23);
         this.buttonClose.TabIndex = 8;
         this.buttonClose.Text = "Close";
         this.buttonClose.UseVisualStyleBackColor = true;
         // 
         // labelDiscussionsViewColorPaletteHint
         // 
         this.labelDiscussionsViewColorPaletteHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
         this.labelDiscussionsViewColorPaletteHint.AutoSize = true;
         this.labelDiscussionsViewColorPaletteHint.ForeColor = System.Drawing.Color.Olive;
         this.labelDiscussionsViewColorPaletteHint.Location = new System.Drawing.Point(6, 206);
         this.labelDiscussionsViewColorPaletteHint.Name = "labelDiscussionsViewColorPaletteHint";
         this.labelDiscussionsViewColorPaletteHint.Size = new System.Drawing.Size(322, 13);
         this.labelDiscussionsViewColorPaletteHint.TabIndex = 7;
         this.labelDiscussionsViewColorPaletteHint.Text = "Discussions view color palette will be configurable in a next version";
         // 
         // linkLabelResetAllColors
         // 
         this.linkLabelResetAllColors.AutoSize = true;
         this.linkLabelResetAllColors.Location = new System.Drawing.Point(346, 22);
         this.linkLabelResetAllColors.Name = "linkLabelResetAllColors";
         this.linkLabelResetAllColors.Size = new System.Drawing.Size(79, 13);
         this.linkLabelResetAllColors.TabIndex = 2;
         this.linkLabelResetAllColors.TabStop = true;
         this.linkLabelResetAllColors.Text = "Reset all colors";
         this.linkLabelResetAllColors.Visible = false;
         this.linkLabelResetAllColors.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelResetAllColors_LinkClicked);
         // 
         // linkLabelResetToFactoryValue
         // 
         this.linkLabelResetToFactoryValue.AutoSize = true;
         this.linkLabelResetToFactoryValue.Location = new System.Drawing.Point(442, 73);
         this.linkLabelResetToFactoryValue.Name = "linkLabelResetToFactoryValue";
         this.linkLabelResetToFactoryValue.Size = new System.Drawing.Size(104, 13);
         this.linkLabelResetToFactoryValue.TabIndex = 6;
         this.linkLabelResetToFactoryValue.TabStop = true;
         this.linkLabelResetToFactoryValue.Text = "Reset selected color";
         this.linkLabelResetToFactoryValue.Visible = false;
         this.linkLabelResetToFactoryValue.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelResetToFactoryValue_LinkClicked);
         // 
         // labelColorSelector
         // 
         this.labelColorSelector.AutoSize = true;
         this.labelColorSelector.Location = new System.Drawing.Point(345, 52);
         this.labelColorSelector.Name = "labelColorSelector";
         this.labelColorSelector.Size = new System.Drawing.Size(91, 13);
         this.labelColorSelector.TabIndex = 4;
         this.labelColorSelector.Text = "Background color";
         // 
         // comboBoxColorSelector
         // 
         this.comboBoxColorSelector.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
         this.comboBoxColorSelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
         this.comboBoxColorSelector.FormattingEnabled = true;
         this.comboBoxColorSelector.Location = new System.Drawing.Point(445, 49);
         this.comboBoxColorSelector.Name = "comboBoxColorSelector";
         this.comboBoxColorSelector.Size = new System.Drawing.Size(126, 21);
         this.comboBoxColorSelector.TabIndex = 5;
         this.comboBoxColorSelector.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.comboBoxColorSelector_DrawItem);
         this.comboBoxColorSelector.SelectedIndexChanged += new System.EventHandler(this.comboBoxColorSelector_SelectedIndexChanged);
         // 
         // listBoxColorSchemeItemSelector
         // 
         this.listBoxColorSchemeItemSelector.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
         this.listBoxColorSchemeItemSelector.FormattingEnabled = true;
         this.listBoxColorSchemeItemSelector.Location = new System.Drawing.Point(9, 49);
         this.listBoxColorSchemeItemSelector.Name = "listBoxColorSchemeItemSelector";
         this.listBoxColorSchemeItemSelector.Size = new System.Drawing.Size(330, 147);
         this.listBoxColorSchemeItemSelector.TabIndex = 3;
         this.listBoxColorSchemeItemSelector.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBoxColorSchemeItemSelector_DrawItem);
         this.listBoxColorSchemeItemSelector.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.listBoxColorSchemeItemSelector_MeasureItem);
         this.listBoxColorSchemeItemSelector.SelectedIndexChanged += new System.EventHandler(this.listBoxColorSchemeItemSelector_SelectedIndexChanged);
         this.listBoxColorSchemeItemSelector.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.listBoxColorSchemeItemSelector_Format);
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
         // ConfigureColorsForm
         // 
         this.AcceptButton = this.buttonClose;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.ClientSize = new System.Drawing.Size(600, 249);
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
      private System.Windows.Forms.Label labelDiscussionsViewColorPaletteHint;
      private System.Windows.Forms.LinkLabel linkLabelResetAllColors;
      private System.Windows.Forms.LinkLabel linkLabelResetToFactoryValue;
      private System.Windows.Forms.Label labelColorSelector;
      private System.Windows.Forms.ComboBox comboBoxColorSelector;
      private System.Windows.Forms.ListBox listBoxColorSchemeItemSelector;
      private System.Windows.Forms.Button buttonClose;
      private System.Windows.Forms.ComboBox comboBoxColorSchemes;
      private System.Windows.Forms.Label labelColorScheme;
   }
}