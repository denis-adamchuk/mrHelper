using System.Windows.Forms;

namespace mrHelper.App.src.Controls
{
   public partial class VersionBrowser : UserControl
   {
      public VersionBrowser()
      {
         InitializeComponent();
         _treeView.Model = new VersionBrowserModel();
      }
   }
}

