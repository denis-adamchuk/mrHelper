using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class AddItemForm : CustomFontForm
   {
      internal AddItemForm()
      {
         InitializeComponent();

         applyFont(Program.Settings.MainWindowFontSizeName);
      }

      internal string Item => textBox.Text;

      private void textBox_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            e.Handled = false;

            buttonOK.PerformClick();
         }
      }
   }
}

