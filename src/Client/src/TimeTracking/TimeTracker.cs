using System;
using System.Threading.Tasks;
using System.Diagnostics;
using mrHelper.Client.Tools;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.TimeTracking
{
   public class TimeTrackerException : Exception {}

   internal delegate Task OnTrackerStoppedAsync(TimeSpan timeSpan, MergeRequestKey mrk);

   /// <summary>
   /// Implements a merge request time tracker with simple interface
   /// </summary>
   public class TimeTracker
   {
      internal TimeTracker(MergeRequestKey mrk, OnTrackerStoppedAsync onTrackerStopped)
      {
         MergeRequestKey = mrk;
         OnStopped = onTrackerStopped;
         Stopwatch = new Stopwatch();
      }

      public void Start()
      {
         Stopwatch.Reset();
         Stopwatch.Start();

         Trace.TraceInformation(String.Format(
            "[TimeTracker] Starting time tracking for MR IId {0} (project {1}",
            MergeRequestKey.IId, MergeRequestKey.ProjectKey.ProjectName));
      }

      async public Task StopAsync()
      {
         Stopwatch.Stop();
         try
         {
            TimeSpan span = Stopwatch.Elapsed;

            Trace.TraceInformation(String.Format(
               "[TimeTracker] Time tracking stopped. Sending {0} for MR IId {1} (project {2})",
               span.ToString(@"hh\:mm\:ss"), MergeRequestKey.IId, MergeRequestKey.ProjectKey.ProjectName));

            await OnStopped(span, MergeRequestKey);
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
            MergeRequestKey.IId, MergeRequestKey.ProjectKey.ProjectName));

         Stopwatch.Stop();
      }

      public TimeSpan Elapsed { get { return Stopwatch.Elapsed; } }

      private MergeRequestKey MergeRequestKey { get; }
      private Stopwatch Stopwatch { get; }
      private OnTrackerStoppedAsync OnStopped { get; }
   }
}

