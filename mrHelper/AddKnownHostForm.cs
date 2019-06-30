using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mrHelper
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
