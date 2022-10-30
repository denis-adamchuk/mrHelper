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
         this.textBox = new System.Windows.Controls.TextBox();
         this.textBoxHost = new System.Windows.Forms.Integration.ElementHost();
         this.SuspendLayout();
         //
         // textBox
         //
         this.textBox.HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
         this.textBox.Margin = new System.Windows.Thickness(0);
         this.textBox.TextWrapping = System.Windows.TextWrapping.Wrap;
         this.textBox.VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto;
         this.textBox.TextChanged += textBox_TextChanged;
         this.textBox.LostFocus += textBox_LostFocus;
         this.textBox.KeyDown += textBox_KeyDown;
         this.textBox.PreviewKeyDown += textBox_PreviewKeyDown;
         // 
         // textBoxHost
         // 
         this.textBoxHost.Child = this.textBox;
         this.textBoxHost.Dock = System.Windows.Forms.DockStyle.Fill;
         this.textBoxHost.Location = new System.Drawing.Point(0, 0);
         this.textBoxHost.Margin = new System.Windows.Forms.Padding(0);
         this.textBoxHost.Padding = new System.Windows.Forms.Padding(0);
         this.textBoxHost.Name = "textBoxHost";
         this.textBoxHost.TabIndex = 3;
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
