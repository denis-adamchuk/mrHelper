using System;
using mrHelper.Client.Types;

namespace mrHelper.Client.TimeTracking
{
   public interface ITotalTimeLoader
   {
      event Action<ITotalTimeCache, MergeRequestKey> TotalTimeLoading;
      event Action<ITotalTimeCache, MergeRequestKey> TotalTimeLoaded;
   }
}

