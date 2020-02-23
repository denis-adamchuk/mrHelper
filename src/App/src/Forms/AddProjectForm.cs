using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class AddProjectForm : CustomFontForm
   {
      internal AddProjectForm()
      {
         InitializeComponent();

         applyFont(Program.Settings.MainWindowFontSizeName);
      }

      internal string ProjectName => textBoxProjectName.Text;

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

