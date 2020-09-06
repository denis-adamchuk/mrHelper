using System;
using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   public partial class DiscussionNoteEditPanel : UserControl
   {
      public DiscussionNoteEditPanel()
      {
         InitializeComponent();
      }

      public DiscussionNoteEditPanel(string resolveActionText, bool isResolveActionChecked)
      {
         InitializeComponent();
         checkBoxResolveAction.Text = resolveActionText;
         checkBoxResolveAction.Checked = isResolveActionChecked;
         checkBoxResolveAction.Visible = true;
      }

      internal void SetTextbox(System.Windows.Controls.TextBox textBox)
      {
         _textBox = textBox;
      }

      internal bool IsResolveActionChecked => checkBoxResolveAction.Checked;

      private void buttonInsertCode_Click(object sender, EventArgs e)
      {
         if (_textBox != null)
         {
            Helpers.WPFHelpers.InsertCodePlaceholderIntoTextBox(_textBox);
            _textBox.Focus();
         }
      }

      private System.Windows.Controls.TextBox _textBox;
   }
}

