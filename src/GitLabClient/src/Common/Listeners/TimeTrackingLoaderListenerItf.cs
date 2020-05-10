using System;
using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   public interface ITimeTrackingLoaderListener
   {
      void OnPreLoadTotalTime(MergeRequestKey mrk);
      void OnPostLoadTotalTime(MergeRequestKey mrk, TimeSpan timeSpan);
      void OnFailedLoadTotalTime(MergeRequestKey mrk);
   }
}


