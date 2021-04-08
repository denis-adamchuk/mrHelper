using System;

namespace mrHelper.App.Forms.Helpers
{
   internal static class WPFHelpers
   {
      internal static System.Windows.Controls.TextBox CreateWPFTextBox(
         System.Windows.Forms.Integration.ElementHost host, bool isReadOnly, string text, bool multiline,
         bool isSpellCheckEnabled, bool softwareOnlyRenderMode)
      {
         System.Windows.Controls.TextBox textbox = new System.Windows.Controls.TextBox
         {
            AcceptsReturn = multiline,
            IsReadOnly = isReadOnly,
            Text = text,
            TextWrapping = System.Windows.TextWrapping.Wrap,
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
         };
         if (softwareOnlyRenderMode)
         {
            textbox.Loaded += (s, e) =>
            {
               var source = System.Windows.PresentationSource.FromVisual(textbox);
               var hwndTarget = source.CompositionTarget as System.Windows.Interop.HwndTarget;
               if (hwndTarget != null)
               {
                  hwndTarget.RenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
               }
            };
         }
         textbox.SpellCheck.IsEnabled = isSpellCheckEnabled;
         host.Child = textbox;
         return textbox;
      }

      public static void InsertCodePlaceholderIntoTextBox(System.Windows.Controls.TextBox textBox)
      {
         int selectionStartIndex = textBox.SelectionStart;
         int selectionLength = textBox.SelectionLength;
         int selectionEndIndex = selectionStartIndex + selectionLength - 1; // less than start index if nothing selected

         string text = textBox.Text;
         bool addNewLineBeforeCursor = selectionStartIndex > 0 && text[selectionStartIndex - 1] != '\n';
         bool addNewLineAfterCursor = selectionEndIndex < text.Length - 1 && text[selectionEndIndex + 1] != '\r';

         string afterCursorText = String.Format("\r\n```{0}", addNewLineAfterCursor ? "\r\n" : String.Empty);
         string beforeCursorText = String.Format("{0}```\r\n", addNewLineBeforeCursor ? "\r\n" : String.Empty);

         int beforeCursorTextPos = Math.Max(0, selectionStartIndex);
         int afterCursorTextPos = beforeCursorTextPos + beforeCursorText.Length + selectionLength;

         textBox.Text = textBox.Text.Insert(beforeCursorTextPos, beforeCursorText);
         textBox.Text = textBox.Text.Insert(afterCursorTextPos, afterCursorText);

         textBox.SelectionStart = beforeCursorTextPos + beforeCursorText.Length;
      }
   }
}

