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
         this.textBoxHost = new System.Windows.Forms.Integration.ElementHost();
         this.toolTip = new System.Windows.Forms.ToolTip(this.components);
         this.panelExtraActions = new System.Windows.Forms.Panel();
         this.SuspendLayout();
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.ConfirmationText = "All changes will be lost, are you sure?";
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(537, 109);
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
         this.buttonOK.Location = new System.Drawing.Point(421, 109);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 5;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         // 
         // textBoxHost
         // 
         this.textBoxHost.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
         this.textBoxHost.Location = new System.Drawing.Point(12, 11);
         this.textBoxHost.Name = "textBoxHost";
         this.textBoxHost.Size = new System.Drawing.Size(600, 85);
         this.textBoxHost.TabIndex = 3;
         this.textBoxHost.Text = "textBoxHost";
         this.textBoxHost.Child = null;
         // 
         // panelExtraActions
         // 
         this.panelExtraActions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
         this.panelExtraActions.Location = new System.Drawing.Point(12, 109);
         this.panelExtraActions.Name = "panelExtraActions";
         this.panelExtraActions.Size = new System.Drawing.Size(403, 23);
         this.panelExtraActions.TabIndex = 7;
         // 
         // TextEditForm
         // 
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(624, 144);
         this.Controls.Add(this.panelExtraActions);
         this.Controls.Add(this.textBoxHost);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.buttonOK);
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.MinimumSize = new System.Drawing.Size(640, 183);
         this.Name = "TextEditForm";
         this.Text = "Dialog caption";
         this.ResumeLayout(false);

      }

      #endregion
      private System.Windows.Forms.Button buttonOK;
      private CommonControls.Controls.ConfirmCancelButton buttonCancel;
      private System.Windows.Forms.Integration.ElementHost textBoxHost;
      private System.Windows.Controls.TextBox textBox;
      private System.Windows.Forms.ToolTip toolTip;
      private System.Windows.Forms.Panel panelExtraActions;
   }
}
