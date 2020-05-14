using System;
using mrHelper.Client.Types;

namespace mrHelper.Client.TimeTracking
{
   public interface ITotalTimeLoader
   {
      event Action<MergeRequestKey> TotalTimeLoading;
      event Action<MergeRequestKey> TotalTimeLoaded;
   }
}

