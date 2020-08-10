using System;
using System.Threading.Tasks;
using System.Diagnostics;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Accessors
{
   /// <summary>
   /// Implements a merge request time tracker with simple interface
   /// </summary>
   internal class TimeTracker : ITimeTracker
   {
      internal TimeTracker(MergeRequestKey mrk, IHostProperties hostProperties,
         IModificationListener modificationListener)
      {
         _mergeRequestKey = mrk;
         _hostProperties = hostProperties;
         _modificationListener = modificationListener;
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

      async public Task Stop()
      {
         _stopwatch.Stop();
         TimeSpan span = _stopwatch.Elapsed;

         MergeRequestEditor editor = new MergeRequestEditor(_hostProperties, _mergeRequestKey, _modificationListener);
         try
         {
            await editor.AddTrackedTime(span, true);
         }
         catch (TimeTrackingException ex)
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

      public MergeRequestKey MergeRequest => _mergeRequestKey;

      public TimeSpan Elapsed { get { return _stopwatch.Elapsed; } }

      private readonly MergeRequestKey _mergeRequestKey;
      private readonly IHostProperties _hostProperties;
      private readonly IModificationListener _modificationListener;
      private readonly Stopwatch _stopwatch;
   }
}

