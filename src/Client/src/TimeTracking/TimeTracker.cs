using System;
using System.Threading.Tasks;
using System.Diagnostics;
using mrHelper.Client.Tools;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Common;

namespace mrHelper.Client.TimeTracking
{
   public class TimeTrackerException : Exception {}

   /// <summary>
   /// Implements a merge request time tracker with simple interface
   /// </summary>
   public class TimeTracker
   {
      internal TimeTracker(MergeRequestKey mrk, Func<TimeSpan, MergeRequestKey, Task> onTrackerStopped)
      {
         _mergeRequestKey = mrk;
         _onStopped = onTrackerStopped;
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
         try
         {
            TimeSpan span = _stopwatch.Elapsed;

            Trace.TraceInformation(String.Format(
               "[TimeTracker] Time tracking stopped. Sending {0} for MR IId {1} (project {2})",
               span.ToString(@"hh\:mm\:ss"), _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName));

            await _onStopped(span, _mergeRequestKey);
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
            _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName));

         _stopwatch.Stop();
      }

      public TimeSpan Elapsed { get { return _stopwatch.Elapsed; } }

      private MergeRequestKey _mergeRequestKey;
      private readonly Stopwatch _stopwatch;
      private readonly Func<TimeSpan, MergeRequestKey, Task> _onStopped;
   }
}

