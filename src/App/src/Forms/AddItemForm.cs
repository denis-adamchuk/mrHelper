using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class AddItemForm : ThemedForm
   {
      internal AddItemForm(string caption, string hint)
      {
         InitializeComponent();
         this.Text = caption;
         labelAddItemHint.Text = hint;

         applyFont(Program.Settings.MainWindowFontSizeName);
      }

      internal string Item => textBox.Text;

      private void textBox_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            buttonOK.PerformClick();
         }
      }
   }
}

