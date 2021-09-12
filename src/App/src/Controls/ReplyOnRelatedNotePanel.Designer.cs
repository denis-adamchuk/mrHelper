namespace mrHelper.App.Controls
{
   partial class ReplyOnRelatedNotePanel
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

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.checkBoxCloseNewDiscussionDialog = new System.Windows.Forms.CheckBox();
         this.buttonInsertCode = new System.Windows.Forms.Button();
         this.SuspendLayout();
         // 
         // checkBoxCloseNewDiscussionDialog
         // 
         this.checkBoxCloseNewDiscussionDialog.AutoSize = true;
         this.checkBoxCloseNewDiscussionDialog.Location = new System.Drawing.Point(99, 4);
         this.checkBoxCloseNewDiscussionDialog.Name = "checkBoxCloseNewDiscussionDialog";
         this.checkBoxCloseNewDiscussionDialog.Size = new System.Drawing.Size(160, 17);
         this.checkBoxCloseNewDiscussionDialog.TabIndex = 8;
         this.checkBoxCloseNewDiscussionDialog.Text = "Close \"Start a thread\" dialog";
         this.checkBoxCloseNewDiscussionDialog.UseVisualStyleBackColor = true;
         // 
         // buttonInsertCode
         // 
         this.buttonInsertCode.Location = new System.Drawing.Point(0, 0);
         this.buttonInsertCode.Name = "buttonInsertCode";
         this.buttonInsertCode.Size = new System.Drawing.Size(75, 23);
         this.buttonInsertCode.TabIndex = 5;
         this.buttonInsertCode.Text = "Insert Code";
         this.buttonInsertCode.UseVisualStyleBackColor = true;
         this.buttonInsertCode.Click += new System.EventHandler(this.buttonInsertCode_Click);
         // 
         // ReplyOnRelatedNotePanel
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
         this.Controls.Add(this.checkBoxCloseNewDiscussionDialog);
         this.Controls.Add(this.buttonInsertCode);
         this.Name = "ReplyOnRelatedNotePanel";
         this.Size = new System.Drawing.Size(264, 24);
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion
      private System.Windows.Forms.Button buttonInsertCode;
      private System.Windows.Forms.CheckBox checkBoxCloseNewDiscussionDialog;
   }
}
