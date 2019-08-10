using System;

namespace mrHelper.Client
{
   public class TimeTrackingManager
   {
      public TimeTrackingManager(UserDefinedSettings settings)
      {
         throw new NotImplementedException();
      }

      public Task<TimeSpan> GetTotalAsync(MergeRequestDescriptor mrd)
      {
         throw new NotImplementedException();
      }

      public Task SetTotalAsync(TimeSpan span, MergeRequestDescriptor mrd)
      {
         throw new NotImplementedException();
      }

      public TimeTracker GetTimeTracker(MergeRequestDescriptor mrd)
      {
         throw new NotImplementedException();
      }
   }
}

