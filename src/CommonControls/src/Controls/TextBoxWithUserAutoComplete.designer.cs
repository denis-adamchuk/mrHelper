namespace mrHelper.CommonControls.Controls
{
   partial class TextBoxWithUserAutoComplete
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

         _delayedHidingTimer?.Stop();
         _delayedHidingTimer?.Dispose();
      }

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.textBoxAutoComplete = new System.Windows.Forms.RichTextBox();
         this.SuspendLayout();
         // 
         // textBoxAutoComplete
         // 
         this.textBoxAutoComplete.Dock = System.Windows.Forms.DockStyle.Fill;
         this.textBoxAutoComplete.Location = new System.Drawing.Point(0, 0);
         this.textBoxAutoComplete.Name = "textBoxAutoComplete";
         this.textBoxAutoComplete.Size = new System.Drawing.Size(271, 20);
         this.textBoxAutoComplete.TabIndex = 0;
         this.textBoxAutoComplete.TextChanged += new System.EventHandler(this.textBoxAutoComplete_TextChanged);
         this.textBoxAutoComplete.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxAutoComplete_KeyDown);
         this.textBoxAutoComplete.Leave += new System.EventHandler(this.textBoxAutoComplete_Leave);
         this.textBoxAutoComplete.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.textBoxAutoComplete_PreviewKeyDown);
         // 
         // TextBoxWithUserAutoComplete
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
         this.BackColor = System.Drawing.SystemColors.Control;
         this.Controls.Add(this.textBoxAutoComplete);
         this.Name = "TextBoxWithUserAutoComplete";
         this.Size = new System.Drawing.Size(271, 20);
         this.ResumeLayout(false);
         this.PerformLayout();

      }
      #endregion

      private System.Windows.Forms.RichTextBox textBoxAutoComplete;
   }
}
