using System;
using mrHelper.CommonControls.Controls;

namespace mrHelper.App.Controls
{
   class FilterTextBox : DelayedTextBox
   {
      private static System.Drawing.Color NormalTextColor = System.Drawing.Color.Black;
      private static System.Drawing.Color ExcludedTextColor = System.Drawing.Color.Gray;

      protected override void OnTextChanged(EventArgs e)
      {
         applySpecialColoring();
         base.OnTextChanged(e);
      }

      private void applySpecialColoring()
      {
         // save state
         SuspendLayout();
         int prevSelectionStart = SelectionStart;
         int prevSelectionLength = SelectionLength;

         // paint text with in black
         SelectionStart = 0;
         SelectionLength = Text.Length;
         SelectionColor = NormalTextColor;

         // paint some words in gray
         int index = 0;
         string[] words = Text.Split(',');
         foreach (string word in words)
         {
            SelectionStart = index;
            SelectionLength = word.Length;
            if (word.TrimStart(' ').StartsWith(Common.Constants.Constants.ExcludeLabelPrefix))
            {
               SelectionColor = ExcludedTextColor;
            }
            index += word.Length + 1; // + comma
         }

         // restore state
         SelectionStart = prevSelectionStart;
         SelectionLength = prevSelectionLength;
         SelectionColor = NormalTextColor;
         ResumeLayout();
      }
   }
}

