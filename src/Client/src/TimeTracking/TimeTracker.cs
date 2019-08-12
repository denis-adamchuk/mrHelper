using System;

namespace mrHelper.Client
{
   public class TimeTrackerException : Exception {}

   /// <summary>
   /// Implements a merge request time tracker with simple interface
   /// </summary>
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
         try
         {
            TimeTrackingOperator.AddSpanAsync(_stopwatch.Elapsed, MergeRequestDescriptor);
         }
         catch (OperatorException)
         {
            throw new TimeTrackerException();
         }
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

