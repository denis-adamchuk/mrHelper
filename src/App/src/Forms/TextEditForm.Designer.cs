namespace mrHelper.App.Forms
{
   partial class TextEditForm
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
         this.buttonCancel = new mrHelper.CommonControls.Controls.ConfirmCancelButton();
         this.buttonOK = new System.Windows.Forms.Button();
         this.textBox = new mrHelper.CommonControls.Controls.TextBoxWithUserAutoComplete();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.panelExtraActions = new System.Windows.Forms.Panel();
         this.tabControlMode = new System.Windows.Forms.TabControl();
         this.tabPageEdit = new System.Windows.Forms.TabPage();
         this.tabPagePreview = new System.Windows.Forms.TabPage();
         this.htmlPanelPreview = new TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel();
         this.labelNoteAboutInvisibleCharacters = new System.Windows.Forms.Label();
         this.tabControlMode.SuspendLayout();
         this.tabPageEdit.SuspendLayout();
         this.tabPagePreview.SuspendLayout();
         this.SuspendLayout();
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.ConfirmationText = "All changes will be lost, are you sure?";
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(537, 149);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 6;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // buttonOK
         // 
         this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(421, 149);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 5;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         // 
         // textBox
         // 
         textBox.Dock = System.Windows.Forms.DockStyle.Fill;;
         textBox.Location = new System.Drawing.Point(3, 3);
         textBox.Name = "textBox";
         textBox.Size = new System.Drawing.Size(586, 82);
         textBox.TabIndex = 3;
         // 
         // panelExtraActions
         // 
         this.panelExtraActions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.panelExtraActions.Location = new System.Drawing.Point(12, 149);
         this.panelExtraActions.Name = "panelExtraActions";
         this.panelExtraActions.Size = new System.Drawing.Size(403, 23);
         this.panelExtraActions.TabIndex = 7;
         // 
         // tabControlMode
         // 
         this.tabControlMode.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.tabControlMode.Controls.Add(this.tabPageEdit);
         this.tabControlMode.Controls.Add(this.tabPagePreview);
         this.tabControlMode.Location = new System.Drawing.Point(12, 12);
         this.tabControlMode.Name = "tabControlMode";
         this.tabControlMode.SelectedIndex = 0;
         this.tabControlMode.Size = new System.Drawing.Size(600, 114);
         this.tabControlMode.TabIndex = 8;
         this.tabControlMode.SelectedIndexChanged += new System.EventHandler(this.tabControlMode_SelectedIndexChanged);
         // 
         // tabPageEdit
         // 
         this.tabPageEdit.Controls.Add(this.textBox);
         this.tabPageEdit.Location = new System.Drawing.Point(4, 22);
         this.tabPageEdit.Name = "tabPageEdit";
         this.tabPageEdit.Padding = new System.Windows.Forms.Padding(3);
         this.tabPageEdit.Size = new System.Drawing.Size(592, 88);
         this.tabPageEdit.TabIndex = 0;
         this.tabPageEdit.Text = "Edit";
         this.tabPageEdit.UseVisualStyleBackColor = true;
         // 
         // tabPagePreview
         // 
         this.tabPagePreview.Controls.Add(this.htmlPanelPreview);
         this.tabPagePreview.Location = new System.Drawing.Point(4, 22);
         this.tabPagePreview.Name = "tabPagePreview";
         this.tabPagePreview.Padding = new System.Windows.Forms.Padding(3);
         this.tabPagePreview.Size = new System.Drawing.Size(592, 88);
         this.tabPagePreview.TabIndex = 1;
         this.tabPagePreview.Text = "Preview";
         this.tabPagePreview.UseVisualStyleBackColor = true;
         // 
         // htmlPanelPreview
         // 
         this.htmlPanelPreview.AutoScroll = true;
         this.htmlPanelPreview.BackColor = System.Drawing.SystemColors.Window;
         this.htmlPanelPreview.BaseStylesheet = null;
         this.htmlPanelPreview.Cursor = System.Windows.Forms.Cursors.IBeam;
         this.htmlPanelPreview.Dock = System.Windows.Forms.DockStyle.Fill;
         this.htmlPanelPreview.Location = new System.Drawing.Point(3, 3);
         this.htmlPanelPreview.Name = "htmlPanelPreview";
         this.htmlPanelPreview.Size = new System.Drawing.Size(586, 82);
         this.htmlPanelPreview.TabIndex = 0;
         this.htmlPanelPreview.Text = null;
         // 
         // labelNoteAboutInvisibleCharacters
         // 
         this.labelNoteAboutInvisibleCharacters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.labelNoteAboutInvisibleCharacters.AutoSize = true;
         this.labelNoteAboutInvisibleCharacters.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
         this.labelNoteAboutInvisibleCharacters.Location = new System.Drawing.Point(12, 130);
         this.labelNoteAboutInvisibleCharacters.Name = "labelNoteAboutInvisibleCharacters";
         this.labelNoteAboutInvisibleCharacters.Size = new System.Drawing.Size(573, 13);
         this.labelNoteAboutInvisibleCharacters.TabIndex = 15;
         this.labelNoteAboutInvisibleCharacters.Visible = false;
         // 
         // TextEditForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(624, 184);
         this.Controls.Add(this.labelNoteAboutInvisibleCharacters);
         this.Controls.Add(this.tabControlMode);
         this.Controls.Add(this.panelExtraActions);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonOK);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(640, 223);
         this.Name = "TextEditForm";
         this.Text = "Dialog caption";
         this.Shown += new System.EventHandler(this.textEditForm_Shown);
         this.tabControlMode.ResumeLayout(false);
         this.tabPageEdit.ResumeLayout(false);
         this.tabPagePreview.ResumeLayout(false);
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion
      private System.Windows.Forms.Button buttonOK;
      private CommonControls.Controls.ConfirmCancelButton buttonCancel;
      private CommonControls.Controls.TextBoxWithUserAutoComplete textBox;
      private System.Windows.Forms.ToolTip toolTip;
      private System.Windows.Forms.Panel panelExtraActions;
      private System.Windows.Forms.TabControl tabControlMode;
      private System.Windows.Forms.TabPage tabPageEdit;
      private System.Windows.Forms.TabPage tabPagePreview;
      private TheArtOfDev.HtmlRenderer.WinForms.HtmlPanel htmlPanelPreview;
      private System.Windows.Forms.Label labelNoteAboutInvisibleCharacters;
   }
}
