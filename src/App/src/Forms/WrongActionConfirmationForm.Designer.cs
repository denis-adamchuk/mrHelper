namespace mrHelper.App.Forms
{
   partial class WrongActionConfirmationForm
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
         System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WrongActionConfirmationForm));
         this.labelConfirmationText = new System.Windows.Forms.Label();
         this.buttonYes = new System.Windows.Forms.Button();
         this.buttonNo = new System.Windows.Forms.Button();
         this.buttonGoTo = new System.Windows.Forms.Button();
         this.panel1 = new System.Windows.Forms.Panel();
         this.pictureBox1 = new System.Windows.Forms.PictureBox();
         this.panel1.SuspendLayout();
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
         this.SuspendLayout();
         // 
         // labelConfirmationText
         // 
         this.labelConfirmationText.Dock = System.Windows.Forms.DockStyle.Fill;
         this.labelConfirmationText.Location = new System.Drawing.Point(0, 0);
         this.labelConfirmationText.Name = "labelConfirmationText";
         this.labelConfirmationText.Size = new System.Drawing.Size(278, 40);
         this.labelConfirmationText.TabIndex = 0;
         this.labelConfirmationText.Text = "You are going to create a discussion to the wrong merge request that you are curr" +
    "ently tracking time on. Are you sure?";
         // 
         // buttonYes
         // 
         this.buttonYes.DialogResult = System.Windows.Forms.DialogResult.Yes;
         this.buttonYes.Location = new System.Drawing.Point(12, 58);
         this.buttonYes.Name = "buttonYes";
         this.buttonYes.Size = new System.Drawing.Size(75, 23);
         this.buttonYes.TabIndex = 1;
         this.buttonYes.Text = "Yes";
         this.buttonYes.UseVisualStyleBackColor = true;
         // 
         // buttonNo
         // 
         this.buttonNo.DialogResult = System.Windows.Forms.DialogResult.No;
         this.buttonNo.Location = new System.Drawing.Point(93, 58);
         this.buttonNo.Name = "buttonNo";
         this.buttonNo.Size = new System.Drawing.Size(75, 23);
         this.buttonNo.TabIndex = 2;
         this.buttonNo.Text = "No";
         this.buttonNo.UseVisualStyleBackColor = true;
         // 
         // buttonGoTo
         // 
         this.buttonGoTo.DialogResult = System.Windows.Forms.DialogResult.Ignore;
         this.buttonGoTo.Location = new System.Drawing.Point(198, 58);
         this.buttonGoTo.Name = "buttonGoTo";
         this.buttonGoTo.Size = new System.Drawing.Size(139, 23);
         this.buttonGoTo.TabIndex = 3;
         this.buttonGoTo.Text = "Go to the current MR";
         this.buttonGoTo.UseVisualStyleBackColor = true;
         // 
         // panel1
         // 
         this.panel1.BackColor = System.Drawing.SystemColors.Control;
         this.panel1.Controls.Add(this.labelConfirmationText);
         this.panel1.Location = new System.Drawing.Point(59, 12);
         this.panel1.Name = "panel1";
         this.panel1.Size = new System.Drawing.Size(278, 40);
         this.panel1.TabIndex = 4;
         // 
         // pictureBox1
         // 
         this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
         this.pictureBox1.Location = new System.Drawing.Point(12, 16);
         this.pictureBox1.Name = "pictureBox1";
         this.pictureBox1.Size = new System.Drawing.Size(32, 32);
         this.pictureBox1.TabIndex = 5;
         this.pictureBox1.TabStop = false;
         // 
         // WrongActionConfirmationForm
         // 
         this.AcceptButton = this.buttonYes;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonNo;
         this.ClientSize = new System.Drawing.Size(349, 93);
         this.Controls.Add(this.pictureBox1);
         this.Controls.Add(this.panel1);
         this.Controls.Add(this.buttonGoTo);
         this.Controls.Add(this.buttonNo);
         this.Controls.Add(this.buttonYes);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.Icon = global::mrHelper.App.Properties.Resources.DefaultAppIcon;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "WrongActionConfirmationForm";
         this.ShowIcon = false;
         this.ShowInTaskbar = false;
         this.Text = "Confirmation";
         this.panel1.ResumeLayout(false);
         ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
         this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.Label labelConfirmationText;
      private System.Windows.Forms.Button buttonYes;
      private System.Windows.Forms.Button buttonNo;
      private System.Windows.Forms.Button buttonGoTo;
      private System.Windows.Forms.Panel panel1;
      private System.Windows.Forms.PictureBox pictureBox1;
   }
}