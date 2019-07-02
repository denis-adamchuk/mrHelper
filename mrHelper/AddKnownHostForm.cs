using System.Windows.Forms;

namespace mrHelperUI
{
   public partial class AddKnownHostForm : Form
   {
      public AddKnownHostForm()
      {
         InitializeComponent();
      }

      public string Host => textBoxHost.Text;

      public string AccessToken => textBoxAccessToken.Text;
   }
}
