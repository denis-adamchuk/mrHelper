namespace mrHelper.App.Controls
{
   partial class ColorGroupConfigPage
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
         colorDialog?.Dispose();
         base.Dispose(disposing);
      }

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.linkLabelResetToFactoryValue = new System.Windows.Forms.LinkLabel();
         this.linkLabelResetTextToFactoryValue = new System.Windows.Forms.LinkLabel();
         this.listBoxColorSchemeItemSelector = new System.Windows.Forms.ListBox();
         this.linkLabelChangeColor = new System.Windows.Forms.LinkLabel();
         this.linkLabelChangeTextColor = new System.Windows.Forms.LinkLabel();
         this.colorDialog = new System.Windows.Forms.ColorDialog();
         this.SuspendLayout();
         // 
         // linkLabelResetToFactoryValue
         // 
         this.linkLabelResetToFactoryValue.AutoSize = true;
         this.linkLabelResetToFactoryValue.LinkColor = System.Drawing.Color.Black;
         this.linkLabelResetToFactoryValue.Location = new System.Drawing.Point(370, 97);
         this.linkLabelResetToFactoryValue.Name = "linkLabelResetToFactoryValue";
         this.linkLabelResetToFactoryValue.Size = new System.Drawing.Size(121, 13);
         this.linkLabelResetToFactoryValue.TabIndex = 3;
         this.linkLabelResetToFactoryValue.TabStop = true;
         this.linkLabelResetToFactoryValue.Text = "Reset background color";
         this.linkLabelResetToFactoryValue.Visible = false;
         this.linkLabelResetToFactoryValue.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelResetToFactoryValue_LinkClicked);
         // 
         // linkLabelResetTextToFactoryValue
         // 
         this.linkLabelResetTextToFactoryValue.AutoSize = true;
         this.linkLabelResetTextToFactoryValue.LinkColor = System.Drawing.Color.Black;
         this.linkLabelResetTextToFactoryValue.Location = new System.Drawing.Point(370, 121);
         this.linkLabelResetTextToFactoryValue.Name = "linkLabelResetTextToFactoryValue";
         this.linkLabelResetTextToFactoryValue.Size = new System.Drawing.Size(81, 13);
         this.linkLabelResetTextToFactoryValue.TabIndex = 3;
         this.linkLabelResetTextToFactoryValue.TabStop = true;
         this.linkLabelResetTextToFactoryValue.Text = "Reset text color";
         this.linkLabelResetTextToFactoryValue.Visible = false;
         this.linkLabelResetTextToFactoryValue.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelResetTextToFactoryValue_LinkClicked);
         // 
         // listBoxColorSchemeItemSelector
         // 
         this.listBoxColorSchemeItemSelector.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
         this.listBoxColorSchemeItemSelector.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
         this.listBoxColorSchemeItemSelector.HorizontalScrollbar = true;
         this.listBoxColorSchemeItemSelector.Location = new System.Drawing.Point(6, 6);
         this.listBoxColorSchemeItemSelector.Name = "listBoxColorSchemeItemSelector";
         this.listBoxColorSchemeItemSelector.Size = new System.Drawing.Size(350, 220);
         this.listBoxColorSchemeItemSelector.TabIndex = 0;
         this.listBoxColorSchemeItemSelector.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBoxColorSchemeItemSelector_DrawItem);
         this.listBoxColorSchemeItemSelector.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.listBoxColorSchemeItemSelector_MeasureItem);
         this.listBoxColorSchemeItemSelector.SelectedIndexChanged += new System.EventHandler(this.listBoxColorSchemeItemSelector_SelectedIndexChanged);
         // 
         // linkLabelChangeColor
         // 
         this.linkLabelChangeColor.AutoSize = true;
         this.linkLabelChangeColor.LinkColor = System.Drawing.Color.Black;
         this.linkLabelChangeColor.Location = new System.Drawing.Point(370, 6);
         this.linkLabelChangeColor.Name = "linkLabelChangeColor";
         this.linkLabelChangeColor.Size = new System.Drawing.Size(130, 13);
         this.linkLabelChangeColor.TabIndex = 1;
         this.linkLabelChangeColor.TabStop = true;
         this.linkLabelChangeColor.Text = "Change background color";
         this.linkLabelChangeColor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelChangeColor_LinkClicked);
         // 
         // linkLabelChangeTextColor
         // 
         this.linkLabelChangeTextColor.AutoSize = true;
         this.linkLabelChangeTextColor.LinkColor = System.Drawing.Color.Black;
         this.linkLabelChangeTextColor.Location = new System.Drawing.Point(370, 30);
         this.linkLabelChangeTextColor.Name = "linkLabelChangeTextColor";
         this.linkLabelChangeTextColor.Size = new System.Drawing.Size(90, 13);
         this.linkLabelChangeTextColor.TabIndex = 2;
         this.linkLabelChangeTextColor.TabStop = true;
         this.linkLabelChangeTextColor.Text = "Change text color";
         this.linkLabelChangeTextColor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelChangeTextColor_LinkClicked);
         this.linkLabelChangeTextColor.Visible = false;
         // 
         // ColorGroupConfigPage
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.Controls.Add(this.listBoxColorSchemeItemSelector);
         this.Controls.Add(this.linkLabelChangeColor);
         this.Controls.Add(this.linkLabelChangeTextColor);
         this.Controls.Add(this.linkLabelResetToFactoryValue);
         this.Controls.Add(this.linkLabelResetTextToFactoryValue);
         this.Name = "ColorGroupConfigPage";
         this.Size = new System.Drawing.Size(511, 233);
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.LinkLabel linkLabelChangeColor;
      private System.Windows.Forms.LinkLabel linkLabelChangeTextColor;
      private System.Windows.Forms.LinkLabel linkLabelResetToFactoryValue;
      private System.Windows.Forms.LinkLabel linkLabelResetTextToFactoryValue;
      private System.Windows.Forms.ListBox listBoxColorSchemeItemSelector;
      private System.Windows.Forms.ColorDialog colorDialog;
   }
}
