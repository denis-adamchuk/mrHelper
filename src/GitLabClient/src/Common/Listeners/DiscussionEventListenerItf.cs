using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   public interface IDiscussionEventListener
   {
      void OnDiscussionEvent(UserEvents.DiscussionEvent e);
   }
}

