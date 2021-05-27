using System.Drawing;

namespace mrHelper.App.Forms
{
   partial class AddKnownHostForm
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
         this.label1 = new System.Windows.Forms.Label();
         this.label2 = new System.Windows.Forms.Label();
         this.textBoxHost = new System.Windows.Forms.TextBox();
         this.textBoxAccessToken = new System.Windows.Forms.TextBox();
         this.buttonCancel = new System.Windows.Forms.Button();
         this.buttonOK = new System.Windows.Forms.Button();
         this.linkLabelCreateAccessToken = new mrHelper.CommonControls.Controls.LinkLabelEx();
         this.SuspendLayout();
         // 
         // label1
         // 
         this.label1.AutoSize = true;
         this.label1.Location = new System.Drawing.Point(12, 9);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(29, 13);
         this.label1.TabIndex = 0;
         this.label1.Text = "Host";
         // 
         // label2
         // 
         this.label2.AutoSize = true;
         this.label2.Location = new System.Drawing.Point(222, 9);
         this.label2.Name = "label2";
         this.label2.Size = new System.Drawing.Size(76, 13);
         this.label2.TabIndex = 1;
         this.label2.Text = "Access Token";
         // 
         // textBoxHost
         // 
         this.textBoxHost.Location = new System.Drawing.Point(12, 27);
         this.textBoxHost.Name = "textBoxHost";
         this.textBoxHost.Size = new System.Drawing.Size(193, 20);
         this.textBoxHost.TabIndex = 0;
         this.textBoxHost.TextChanged += new System.EventHandler(this.textBoxHost_TextChanged);
         this.textBoxHost.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_KeyDown);
         // 
         // textBoxAccessToken
         // 
         this.textBoxAccessToken.Location = new System.Drawing.Point(225, 27);
         this.textBoxAccessToken.Name = "textBoxAccessToken";
         this.textBoxAccessToken.Size = new System.Drawing.Size(193, 20);
         this.textBoxAccessToken.TabIndex = 1;
         this.textBoxAccessToken.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_KeyDown);
         // 
         // buttonCancel
         // 
         this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(343, 67);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 3;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // buttonOK
         // 
         this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(251, 67);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 2;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         // 
         // linkLabelCreateAccessToken
         // 
         this.linkLabelCreateAccessToken.AutoSize = true;
         this.linkLabelCreateAccessToken.Location = new System.Drawing.Point(12, 50);
         this.linkLabelCreateAccessToken.Name = "linkLabelCreateAccessToken";
         this.linkLabelCreateAccessToken.Size = new System.Drawing.Size(105, 13);
         this.linkLabelCreateAccessToken.TabIndex = 4;
         this.linkLabelCreateAccessToken.TabStop = true;
         this.linkLabelCreateAccessToken.Text = "Create access token";
         // 
         // AddKnownHostForm
         // 
         this.AcceptButton = this.buttonOK;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(440, 102);
         this.Controls.Add(this.linkLabelCreateAccessToken);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.textBoxAccessToken);
         this.Controls.Add(this.textBoxHost);
         this.Controls.Add(this.label2);
         this.Controls.Add(this.label1);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "AddKnownHostForm";
         this.Text = "Add Known Host";
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.Label label2;
      private System.Windows.Forms.TextBox textBoxHost;
      private System.Windows.Forms.TextBox textBoxAccessToken;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.Button buttonOK;
      private CommonControls.Controls.LinkLabelEx linkLabelCreateAccessToken;
   }
}
