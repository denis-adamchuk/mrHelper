namespace mrHelper.App.Controls
{
   partial class NoteEditPanel
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
         this.checkBoxResolveAction = new System.Windows.Forms.CheckBox();
         this.buttonInsertCode = new System.Windows.Forms.Button();
         this.SuspendLayout();
         // 
         // checkBoxResolveAction
         // 
         this.checkBoxResolveAction.AutoSize = true;
         this.checkBoxResolveAction.Location = new System.Drawing.Point(99, 4);
         this.checkBoxResolveAction.Name = "checkBoxResolveAction";
         this.checkBoxResolveAction.Size = new System.Drawing.Size(102, 17);
         this.checkBoxResolveAction.TabIndex = 8;
         this.checkBoxResolveAction.Text = "Resolve Thread";
         this.checkBoxResolveAction.UseVisualStyleBackColor = true;
         this.checkBoxResolveAction.Visible = false;
         // 
         // buttonInsertCode
         // 
         this.buttonInsertCode.AutoSize = true;
         this.buttonInsertCode.Location = new System.Drawing.Point(0, 0);
         this.buttonInsertCode.Name = "buttonInsertCode";
         this.buttonInsertCode.Size = new System.Drawing.Size(75, 23);
         this.buttonInsertCode.TabIndex = 5;
         this.buttonInsertCode.Text = "Insert Code";
         this.buttonInsertCode.UseVisualStyleBackColor = true;
         this.buttonInsertCode.Click += new System.EventHandler(this.buttonInsertCode_Click);
         // 
         // DiscussionNoteEditPanel
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
         this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
         this.Controls.Add(this.checkBoxResolveAction);
         this.Controls.Add(this.buttonInsertCode);
         this.Name = "DiscussionNoteEditPanel";
         this.Size = new System.Drawing.Size(215, 24);
         this.ResumeLayout(false);
         this.PerformLayout();

      }

      #endregion
      private System.Windows.Forms.Button buttonInsertCode;
      private System.Windows.Forms.CheckBox checkBoxResolveAction;
   }
}
