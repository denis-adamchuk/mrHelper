using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   internal partial class AddKnownHostForm : Form
   {
      internal AddKnownHostForm()
      {
         InitializeComponent();
      }

      internal string Host => textBoxHost.Text;

      internal string AccessToken => textBoxAccessToken.Text;

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

