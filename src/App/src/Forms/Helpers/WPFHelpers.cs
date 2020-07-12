namespace mrHelper.App.Forms.Helpers
{
   internal static class WPFHelpers
   {
      internal static System.Windows.Controls.TextBox CreateWPFTextBox(
         System.Windows.Forms.Integration.ElementHost host, bool isReadOnly, string text)
      {
         System.Windows.Controls.TextBox textbox = new System.Windows.Controls.TextBox
         {
            AcceptsReturn = true,
            IsReadOnly = isReadOnly,
            Text = text,
            TextWrapping = System.Windows.TextWrapping.Wrap,
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
         };
         textbox.SpellCheck.IsEnabled = true;
         host.Child = textbox;
         return textbox;
      }
   }
}

