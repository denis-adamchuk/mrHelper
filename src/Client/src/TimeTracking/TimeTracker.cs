using System;
using System.Threading.Tasks;
using System.Diagnostics;
using mrHelper.Client.Tools;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.TimeTracking
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
         Stopwatch = new Stopwatch();
      }

      public void Start()
      {
         Stopwatch.Reset();
         Stopwatch.Start();
      }

      async public Task StopAsync()
      {
         Stopwatch.Stop();
         try
         {
            await TimeTrackingOperator.AddSpanAsync(Stopwatch.Elapsed, MergeRequestDescriptor);
         }
         catch (OperatorException)
         {
            throw new TimeTrackerException();
         }
      }

      public void Cancel()
      {
         Stopwatch.Stop();
      }

      public TimeSpan Elapsed { get { return Stopwatch.Elapsed; } }

      private MergeRequestDescriptor MergeRequestDescriptor { get; }
      private Stopwatch Stopwatch { get; }
      private readonly TimeTrackingOperator TimeTrackingOperator;
   }
}

