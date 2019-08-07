using System.Windows.Forms;

namespace mrHelper.UI
{
   public partial class AddKnownHostForm : Form
   {
      public AddKnownHostForm()
      {
         InitializeComponent();
      }

      public string Host => textBoxHost.Text;

      public string AccessToken => textBoxAccessToken.Text;

      private void textBoxAccessToken_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter && Control.ModifierKeys == Keys.Control)
         {
            e.Handled = false;

            buttonOK.PerformClick(); 
         }
      }
   }
}
