using System;
using System.Threading.Tasks;
using System.Diagnostics;
using mrHelper.Client.Tools;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.TimeTracking
{
   public class TimeTrackerException : Exception {}

   internal delegate Task OnTrackerStoppedAsync(TimeSpan timeSpan, MergeRequestDescriptor mrd);

   /// <summary>
   /// Implements a merge request time tracker with simple interface
   /// </summary>
   public class TimeTracker
   {
      internal TimeTracker(MergeRequestDescriptor mrd, OnTrackerStoppedAsync onTrackerStopped)
      {
         MergeRequestDescriptor = mrd;
         OnStopped = onTrackerStopped;
         Stopwatch = new Stopwatch();
      }

      public void Start()
      {
         Stopwatch.Reset();
         Stopwatch.Start();

         Trace.TraceInformation(String.Format(
            "[TimeTracker] Starting time tracking for MR IId {0} (project {1}",
            MergeRequestDescriptor.IId, MergeRequestDescriptor.ProjectName));
      }

      async public Task StopAsync()
      {
         Stopwatch.Stop();
         try
         {
            TimeSpan span = Stopwatch.Elapsed;

            Trace.TraceInformation(String.Format(
               "[TimeTracker] Time tracking stopped. Sending {0} for MR IId {1} (project {2})",
               span.ToString(), MergeRequestDescriptor.IId, MergeRequestDescriptor.ProjectName));

            await OnStopped(span, MergeRequestDescriptor);
         }
         catch (OperatorException)
         {
            throw new TimeTrackerException();
         }
      }

      public void Cancel()
      {
         Trace.TraceInformation(String.Format(
            "[TimeTracker] Time tracking for MR IId {0} (project {1}) cancelled",
            MergeRequestDescriptor.IId, MergeRequestDescriptor.ProjectName));

         Stopwatch.Stop();
      }

      public TimeSpan Elapsed { get { return Stopwatch.Elapsed; } }

      private MergeRequestDescriptor MergeRequestDescriptor { get; }
      private Stopwatch Stopwatch { get; }
      private OnTrackerStoppedAsync OnStopped { get; }
   }
}

