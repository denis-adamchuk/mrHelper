using System;

namespace mrHelper.Client
{
   public class TimeTracker
   {
      internal TimeTracker(MergeRequestDescriptor mrd, TimeTrackingOperator trackingOperator)
      {
         MergeRequestDescriptor = mrd;
         TimeTrackingOperator = trackingOperator;
      }

      public void Start()
      {
         _stopwatch.Reset();
         _stopwatch.Start();
      }

      public void Stop()
      {
         _stopwatch.Stop();
         TimeTrackingOperator.AddSpanAsync(_stopwatch.Elapsed, MergeRequestDescriptor);
      }

      public void Cancel()
      {
         _stopwatch.Stop();
      }

      public string Elapsed { get { return _stopWatch.Elapsed; } }

      private MergeRequestDescriptor MergeRequestDescriptor { get; }
      private StopWatch _stopwatch { get; }
      private TimeTrackingOperator TimeTrackingOperator;
   }
}

