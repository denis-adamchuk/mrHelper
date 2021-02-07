using System;
using System.Windows.Forms;

namespace mrHelper.App.Controls
{
   public partial class DiscussionActionsPanel : UserControl
   {
      public DiscussionActionsPanel(Action onRefresh, Action onAddComment, Action onAddThread)
      {
         InitializeComponent();
         _onRefresh = onRefresh;
         _onAddComment = onAddComment;
         _onAddThread = onAddThread;
      }

      private void ButtonDiscussionsRefresh_Click(object sender, EventArgs e)
      {
         _onRefresh();
      }

      private void buttonAddComment_Click(object sender, EventArgs e)
      {
         _onAddComment();
      }

      private void buttonNewThread_Click(object sender, EventArgs e)
      {
         _onAddThread();
      }

      private readonly Action _onRefresh;
      private readonly Action _onAddComment;
      private readonly Action _onAddThread;
   }
}
