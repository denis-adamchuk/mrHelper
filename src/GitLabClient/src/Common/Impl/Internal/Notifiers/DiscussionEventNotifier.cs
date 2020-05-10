using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   internal class DiscussionEventNotifier : BaseNotifier<IDiscussionEventListener>, IDiscussionEventListener
   {
      public void OnDiscussionEvent(UserEvents.DiscussionEvent e) =>
         notifyAll(x => x.OnDiscussionEvent(e));
   }
}

