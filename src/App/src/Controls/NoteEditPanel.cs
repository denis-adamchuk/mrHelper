using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class NoteEditPanel : UserControl
   {
      public NoteEditPanel(Action onInsertCode)
      {
         InitializeComponent();
         _onInsertCode = onInsertCode;
      }

      public NoteEditPanel(string resolveActionText, bool isResolveActionChecked, Action onInsertCode)
      {
         InitializeComponent();
         checkBoxResolveAction.Text = resolveActionText;
         checkBoxResolveAction.Checked = isResolveActionChecked;
         checkBoxResolveAction.Visible = true;
         _onInsertCode = onInsertCode;
      }

      internal bool IsResolveActionChecked => checkBoxResolveAction.Checked;

      private void buttonInsertCode_Click(object sender, EventArgs e)
      {
         _onInsertCode();
      }

      private readonly Action _onInsertCode;
   }
}

