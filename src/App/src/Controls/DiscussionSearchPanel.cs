using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class DiscussionSearchPanel : UserControl
   {
      public DiscussionSearchPanel(Func<string, bool, int> onFind, Action onCancel)
      {
         InitializeComponent();

         _onFind = onFind;
         _onCancel = onCancel;

         Reset();
      }

      public void Reset()
      {
         labelFoundCount.Visible = false;
         enableButtons();
      }

      private int foundCount
      {
         set
         {
            labelFoundCount.Text = String.Format(
               "Found {0} results. {1}",
               value, value > 0 ? "Use F3/Shift-F3 to navigate between search results." : String.Empty);
            labelFoundCount.Visible = true;
         }
      }

      private void enableButtons()
      {
         bool hasText = textBoxSearch.Text.Length > 0;
         buttonFindNext.Enabled = hasText;
         buttonFindPrev.Enabled = hasText;
      }

      private void ButtonFind_Click(object sender, EventArgs e)
      {
         foundCount = _onFind(textBoxSearch.Text, true);
      }

      private void buttonFindPrev_Click(object sender, EventArgs e)
      {
         foundCount = _onFind(textBoxSearch.Text, false);
      }

      private void textBoxSearch_TextChanged(object sender, EventArgs e)
      {
         enableButtons();
         if (labelFoundCount.Visible)
         {
            _onCancel();
         }
      }

      private void textBoxSearch_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter)
         {
            buttonFindNext.PerformClick();
         }
      }

      Func<string, bool, int> _onFind;
      Action _onCancel;
   }
}

