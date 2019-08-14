using System;
using System.Threading.Tasks;
using mrHelper.Client.Tools;

namespace mrHelper.Client.TimeTracking
{
   /// <summary>
   /// Manages time tracking for merge requests
   /// </summary>
   public class TimeTrackingManager
   {
      public TimeTrackingManager(UserDefinedSettings settings)
      {
         Settings = settings;
         TimeTrackingOperator = new TimeTrackingOperator(Settings);
      }

      /*async*/ public Task<TimeSpan> GetTotalAsync(MergeRequestDescriptor mrd)
      {
         throw new NotImplementedException();
      }

      /*async*/ public Task SetTotalAsync(TimeSpan span, MergeRequestDescriptor mrd)
      {
         throw new NotImplementedException();
      }

      public TimeTracker GetTracker(MergeRequestDescriptor mrd)
      {
         return new TimeTracker(mrd, TimeTrackingOperator);
      }

      private UserDefinedSettings Settings { get; }
      private TimeTrackingOperator TimeTrackingOperator { get; }
   }
}

