namespace mrHelper.CommonControls.Controls
{
   partial class SmartTextBox
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

         _popupWindow?.Close();
         _popupWindow?.Dispose();

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
         this.textBoxHost = new System.Windows.Forms.Integration.ElementHost();
         this.SuspendLayout();
         // 
         // textBoxHost
         // 
         this.textBoxHost.Dock = System.Windows.Forms.DockStyle.Fill;
         this.textBoxHost.Location = new System.Drawing.Point(3, 3);
         this.textBoxHost.Name = "textBoxHost";
         this.textBoxHost.Size = new System.Drawing.Size(586, 82);
         this.textBoxHost.TabIndex = 3;
         this.textBoxHost.Text = "textBoxHost";
         this.textBoxHost.Child = null;
         // 
         // TextBoxWithUserAutoComplete
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
         this.BackColor = System.Drawing.SystemColors.Control;
         this.Controls.Add(this.textBoxHost);
         this.Name = "TextBoxWithUserAutoComplete";
         this.Size = new System.Drawing.Size(271, 20);
         this.ResumeLayout(false);
         this.PerformLayout();

      }
      #endregion

      private System.Windows.Forms.Integration.ElementHost textBoxHost;
      private System.Windows.Controls.TextBox textBox;
   }
}
