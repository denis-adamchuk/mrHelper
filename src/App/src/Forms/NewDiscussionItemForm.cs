using System;
using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class NewDiscussionItemForm : CustomFontForm
   {
      internal NewDiscussionItemForm(string caption)
      {
         InitializeComponent();
         this.Text = caption;

         applyFont(Program.Settings.MainWindowFontSizeName);
      }

      internal string Body => textBox.Text;

      private void textBox_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            e.Handled = false;

            buttonOK.PerformClick();
         }
      }

      private void NewDiscussionItemForm_Load(object sender, EventArgs e)
      {
         this.ActiveControl = textBox;
      }
   }
}
