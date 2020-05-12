using System;
using mrHelper.Client.Types;

namespace mrHelper.Client.TimeTracking
{
   public interface ITotalTimeLoader
   {
      event Action<MergeRequestKey, TimeSpan> TotalTimeLoaded;
   }
}

