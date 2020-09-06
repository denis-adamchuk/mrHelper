using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class DiscussionActionsPanel : UserControl
   {
      public DiscussionActionsPanel(Action onRefresh)
      {
         _onRefresh = onRefresh;

         InitializeComponent();
      }

      private void ButtonDiscussionsRefresh_Click(object sender, EventArgs e)
      {
         _onRefresh();
      }

      private readonly Action _onRefresh;
   }
}
