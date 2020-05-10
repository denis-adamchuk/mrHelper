using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   public interface IMergeRequestEventListener
   {
      void OnMergeRequestEvent(UserEvents.MergeRequestEvent e);
   }
}

