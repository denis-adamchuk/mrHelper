using System;
using System.Drawing;
using System.Linq;
using mrHelper.App.Helpers;
using mrHelper.CommonControls.Controls;

namespace mrHelper.App.Controls
{
   internal class FilterTextBox : DelayedTextBox
   {
      internal FilterTextBox()
      {
         ColorScheme.Modified += onColorSchemeModified;
      }

      protected override void Dispose(bool disposing)
      {
         ColorScheme.Modified -= onColorSchemeModified;
         base.Dispose(disposing);
      }

      public override string Text
      {
         get
         {
            return _visibleText;
         }
         set
         {
            setVisibleText(value);
            setHiddenText(value);
            base.Text = _visibleText;
         }
      }

      internal string GetFullText()
      {
         return String.Format("{0}{1}{2}", _visibleText, _hiddenText.Length > 0 ? ", " : String.Empty, _hiddenText);
      }

      protected override void OnTextChanged(EventArgs e)
      {
         base.OnTextChanged(e);
         setVisibleText(base.Text);
         if (base.Text != _visibleText)
         {
            base.Text = _visibleText;
         }
         applySpecialColoring();
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
         SelectionColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive;

         // paint some words in gray
         int index = 0;
         string[] words = Text.Split(',');
         foreach (string word in words)
         {
            bool isThereCommaAfterWord = !ReferenceEquals(word, words.Last());
            int commaCount = isThereCommaAfterWord ? 1 : 0;
            if (hasSpecialPrefix(word))
            {
               SelectionStart = index;
               SelectionLength = word.Length + commaCount;
               SelectionColor = ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextInactive;
            }
            index += word.Length + commaCount;
         }

         // restore state
         SelectionStart = prevSelectionStart;
         SelectionLength = prevSelectionLength;
         SelectionColor = prevSelectionColor;
         ResumeLayout();
      }

      private void setVisibleText(string text)
      {
         string getVisibleText() =>
            String.Join(",", text.Split(',').Where(word => !hasSpecialPrefix(word)).ToArray());

         if (!Program.Settings.ShowHiddenMergeRequestIds)
         {
            _visibleText = getVisibleText();
         }
         else
         {
            _visibleText = text;
         }
      }

      private void setHiddenText(string text)
      {
         string getHiddenText() =>
            String.Join(",", text.Split(',').Where(word => hasSpecialPrefix(word)).ToArray());

         if (!Program.Settings.ShowHiddenMergeRequestIds)
         {
            _hiddenText = getHiddenText();
         }
         else
         {
            _hiddenText = String.Empty;
         }
      }

      private void onColorSchemeModified()
      {
         applySpecialColoring();
      }

      private static bool hasSpecialPrefix(string text) =>
         hasExcludePrefix(text) || hasPinPrefix(text);

      private static bool hasExcludePrefix(string word) =>
         word.Trim(' ').StartsWith(Common.Constants.Constants.ExcludeLabelPrefix);

      private static bool hasPinPrefix(string word) =>
         word.Trim(' ').StartsWith(Common.Constants.Constants.PinLabelPrefix);

      private string _visibleText;
      private string _hiddenText;
   }
}

