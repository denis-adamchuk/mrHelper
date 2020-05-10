using System;
using System.Threading.Tasks;
using System.Diagnostics;
using mrHelper.Client.Types;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.TimeTracking
{
   public class TimeTrackerException : ExceptionEx
   {
      internal TimeTrackerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   /// <summary>
   /// Implements a merge request time tracker with simple interface
   /// </summary>
   public class TimeTracker
   {
      internal TimeTracker(MergeRequestKey mrk, TimeTrackingManager timeTrackingManager)
      {
         _mergeRequestKey = mrk;
         _timeTrackingManager = timeTrackingManager;
         _stopwatch = new Stopwatch();
      }

      public void Start()
      {
         _stopwatch.Reset();
         _stopwatch.Start();

         Trace.TraceInformation(String.Format(
            "[TimeTracker] Starting time tracking for MR IId {0} (project {1}",
            _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName));
      }

      async public Task StopAsync()
      {
         _stopwatch.Stop();
         TimeSpan span = _stopwatch.Elapsed;

         try
         {
            await _timeTrackingManager.AddSpanAsync(true, span, _mergeRequestKey);
         }
         catch (TimeTrackingManagerException ex)
         {
            throw new TimeTrackerException("Cannot stop timer", ex);
         }

         Trace.TraceInformation(String.Format(
            "[TimeTracker] Time tracking stopped. Sending {0} for MR IId {1} (project {2})",
            span.ToString(@"hh\:mm\:ss"), _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName));
      }

      public void Cancel()
      {
         Trace.TraceInformation(String.Format(
            "[TimeTracker] Time tracking for MR IId {0} (project {1}) cancelled",
            _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName));

         _stopwatch.Stop();
      }

      public TimeSpan Elapsed { get { return _stopwatch.Elapsed; } }

      private MergeRequestKey _mergeRequestKey;
      private readonly Stopwatch _stopwatch;
      private readonly TimeTrackingManager _timeTrackingManager;
   }
}

