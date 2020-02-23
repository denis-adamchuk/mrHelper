using System.Drawing;

namespace mrHelper.App.Forms
{
   partial class AddProjectForm
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
         this.buttonCancel = new System.Windows.Forms.Button();
         this.buttonOK = new System.Windows.Forms.Button();
         this.textBoxProjectName = new System.Windows.Forms.TextBox();
         this.label1 = new System.Windows.Forms.Label();
         this.SuspendLayout();
         // 
         // buttonCancel
         // 
         this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
         this.buttonCancel.Location = new System.Drawing.Point(164, 59);
         this.buttonCancel.Name = "buttonCancel";
         this.buttonCancel.Size = new System.Drawing.Size(75, 23);
         this.buttonCancel.TabIndex = 3;
         this.buttonCancel.Text = "Cancel";
         this.buttonCancel.UseVisualStyleBackColor = true;
         // 
         // buttonOK
         // 
         this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
         this.buttonOK.Location = new System.Drawing.Point(12, 59);
         this.buttonOK.Name = "buttonOK";
         this.buttonOK.Size = new System.Drawing.Size(75, 23);
         this.buttonOK.TabIndex = 2;
         this.buttonOK.Text = "OK";
         this.buttonOK.UseVisualStyleBackColor = true;
         // 
         // textBoxProjectName
         // 
         this.textBoxProjectName.Location = new System.Drawing.Point(12, 27);
         this.textBoxProjectName.Name = "textBoxProjectName";
         this.textBoxProjectName.Size = new System.Drawing.Size(227, 20);
         this.textBoxProjectName.TabIndex = 0;
         this.textBoxProjectName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox_KeyDown);
         // 
         // label1
         // 
         this.label1.AutoSize = true;
         this.label1.Location = new System.Drawing.Point(12, 9);
         this.label1.Name = "label1";
         this.label1.Size = new System.Drawing.Size(71, 13);
         this.label1.TabIndex = 0;
         this.label1.Text = "Project Name";
         // 
         // AddProjectForm
         // 
         this.AcceptButton = this.buttonOK;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.CancelButton = this.buttonCancel;
         this.ClientSize = new System.Drawing.Size(251, 94);
         this.Controls.Add(this.buttonOK);
         this.Controls.Add(this.buttonCancel);
         this.Controls.Add(this.textBoxProjectName);
         this.Controls.Add(this.label1);
         this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
         this.MaximizeBox = false;
         this.MinimizeBox = false;
         this.Name = "AddProjectForm";
         this.Text = "Add Project";
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion

      private System.Windows.Forms.Label label1;
      private System.Windows.Forms.TextBox textBoxProjectName;
      private System.Windows.Forms.Button buttonCancel;
      private System.Windows.Forms.Button buttonOK;
   }
}
