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

      public ReplyOnRelatedNotePanel(bool isCloseDialogChecked, Action onInsertCode)
      {
         InitializeComponent();
         checkBoxCloseNewDiscussionDialog.Checked = isCloseDialogChecked;
         _onInsertCode = onInsertCode;
      }

      internal bool IsCloseDialogActionChecked => checkBoxCloseNewDiscussionDialog.Checked;

      private void buttonInsertCode_Click(object sender, EventArgs e)
      {
         _onInsertCode?.Invoke();
      }

      private readonly Action _onInsertCode;
   }
}

