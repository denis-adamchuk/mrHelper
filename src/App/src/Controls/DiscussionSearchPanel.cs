using System;
using System.Windows.Forms;
using mrHelper.App.Helpers;

namespace mrHelper.App.Controls
{
   public partial class DiscussionSearchPanel : UserControl
   {
      internal DiscussionSearchPanel(Action<SearchQuery, bool> onFind, Action onTextChanged)
      {
         InitializeComponent();

         _onFind = onFind;
         _onTextChanged = onTextChanged;
         labelFoundCount.Visible = false;
         enableButtons();
      }

      public void DisplayFoundCount(int? count)
      {
         labelFoundCount.Visible = count.HasValue;
         labelFoundCount.Text = count.HasValue ? String.Format(
            "Found {0} results. {1}", count.Value,
            count.Value > 0 ? "Use F3/Shift-F3 to navigate between search results." : String.Empty) : String.Empty;
      }

      private void enableButtons()
      {
         bool hasText = textBoxSearch.Text.Length > 0;
         buttonFindNext.Enabled = hasText;
         buttonFindPrev.Enabled = hasText;
      }

      private void ButtonFind_Click(object sender, EventArgs e)
      {
         _onFind(new SearchQuery(textBoxSearch.Text, checkBoxCaseSensitive.Checked), true);
      }

      private void buttonFindPrev_Click(object sender, EventArgs e)
      {
         _onFind(new SearchQuery(textBoxSearch.Text, checkBoxCaseSensitive.Checked), false);
      }

      private void textBoxSearch_TextChanged(object sender, EventArgs e)
      {
         enableButtons();
         _onTextChanged();
      }

      private void textBoxSearch_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter)
         {
            buttonFindNext.PerformClick();
         }
      }

      private void checkBoxCaseSensitive_CheckedChanged(object sender, EventArgs e)
      {
         buttonFindNext.PerformClick();
      }

      private readonly Action<SearchQuery, bool> _onFind;
      private readonly Action _onTextChanged;
   }
}

