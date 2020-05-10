using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   internal class MergeRequestEventNotifier : BaseNotifier<IMergeRequestEventListener>, IMergeRequestEventListener
   {
      public void OnMergeRequestEvent(UserEvents.MergeRequestEvent e) =>
         notifyAll(x => x.OnMergeRequestEvent(e));
   }
}

