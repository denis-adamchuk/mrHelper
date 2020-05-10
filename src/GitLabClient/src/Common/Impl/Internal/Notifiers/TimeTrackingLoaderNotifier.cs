using System;
using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   internal class TimeTrackingLoaderNotifier : BaseNotifier<ITimeTrackingLoaderListener>, ITimeTrackingLoaderListener
   {
      public void OnPreLoadTotalTime(MergeRequestKey mrk) =>
         notifyAll(x => x.OnPreLoadTotalTime(mrk));

      public void OnPostLoadTotalTime(MergeRequestKey mrk, TimeSpan timeSpan) =>
         notifyAll(x => x.OnPostLoadTotalTime(mrk, timeSpan));

      public void OnFailedLoadTotalTime(MergeRequestKey mrk) =>
         notifyAll(x => x.OnFailedLoadTotalTime(mrk));
   }
}

