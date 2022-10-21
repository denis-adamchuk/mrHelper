using System;
using mrHelper.CommonControls.Controls;

namespace mrHelper.CommonControls.Tools
{
   public static class SmartTextBoxHelpers
   {
      public static void InsertCodePlaceholderIntoTextBox(SmartTextBox textBox)
      {
         int selectionStartIndex = textBox.GetSelectionStart();
         int selectionLength = textBox.GetSelectionLength();
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

         textBox.SetSelectionStart(beforeCursorTextPos + beforeCursorText.Length);
      }
   }
}

