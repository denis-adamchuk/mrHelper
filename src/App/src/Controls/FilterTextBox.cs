using System;
using System.Linq;
using mrHelper.CommonControls.Controls;

namespace mrHelper.App.Controls
{
   class FilterTextBox : DelayedTextBox
   {
      private static System.Drawing.Color NormalTextColor = System.Drawing.Color.Black;
      private static System.Drawing.Color ExcludedTextColor = System.Drawing.Color.LightGray;

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
         System.Drawing.Color prevSelectionColor = SelectionColor;

         // paint text with in black
         SelectionStart = 0;
         SelectionLength = Text.Length;
         SelectionColor = NormalTextColor;

         // paint some words in gray
         int index = 0;
         string[] words = Text.Split(',');
         foreach (string word in words)
         {
            bool isThereCommaAfterWord = !ReferenceEquals(word, words.Last());
            int commaCount = isThereCommaAfterWord ? 1 : 0;
            if (word.Trim(' ').StartsWith(Common.Constants.Constants.ExcludeLabelPrefix))
            {
               SelectionStart = index;
               SelectionLength = word.Length + commaCount;
               SelectionColor = ExcludedTextColor;
            }
            index += word.Length + commaCount;
         }

         // restore state
         SelectionStart = prevSelectionStart;
         SelectionLength = prevSelectionLength;
         SelectionColor = prevSelectionColor;
         ResumeLayout();
      }
   }
}

