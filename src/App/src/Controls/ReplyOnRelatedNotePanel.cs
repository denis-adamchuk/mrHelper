using System;
using System.Windows.Forms;
using mrHelper.App.Forms.Helpers;

namespace mrHelper.App.Controls
{
   public partial class ReplyOnRelatedNotePanel : UserControl
   {
      public ReplyOnRelatedNotePanel()
      {
         InitializeComponent();
      }

      public ReplyOnRelatedNotePanel(bool isCloseDialogChecked)
      {
         InitializeComponent();
         checkBoxCloseNewDiscussionDialog.Checked = isCloseDialogChecked;
      }

      internal void SetTextbox(System.Windows.Controls.TextBox textBox)
      {
         _textBox = textBox;
      }

      internal bool IsCloseDialogActionChecked => checkBoxCloseNewDiscussionDialog.Checked;

      private void buttonInsertCode_Click(object sender, EventArgs e)
      {
         if (_textBox != null)
         {
            WPFHelpers.InsertCodePlaceholderIntoTextBox(_textBox);
            _textBox.Focus();
         }
      }

      private System.Windows.Controls.TextBox _textBox;
   }
}

