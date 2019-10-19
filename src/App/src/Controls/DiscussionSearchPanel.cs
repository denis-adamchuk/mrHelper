using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class DiscussionSearchPanel : UserControl
   {
      public DiscussionSearchPanel(Action<string, bool> onFind)
      {
         InitializeComponent();

         _onFind = onFind;
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
         _onFind(textBoxSearch.Text, true);
      }

      private void buttonFindPrev_Click(object sender, EventArgs e)
      {
         _onFind(textBoxSearch.Text, false);
      }

      private void textBoxSearch_TextChanged(object sender, EventArgs e)
      {
         enableButtons();
      }

      private void textBoxSearch_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter)
         {
            buttonFindNext.PerformClick();
         }
      }

      readonly Action<string, bool> _onFind;
   }
}

