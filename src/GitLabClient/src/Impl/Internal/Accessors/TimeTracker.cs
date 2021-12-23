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
         IModificationListener modificationListener, INetworkOperationStatusListener networkOperationStatusListener)
      {
         _mergeRequestKey = mrk;
         _hostProperties = hostProperties;
         _modificationListener = modificationListener;
         _stopwatch = new Stopwatch();
         _networkOperationStatusListener = networkOperationStatusListener;
      }

      public void Start()
      {
         _stopwatch.Reset();
         _stopwatch.Start();

         Trace.TraceInformation(String.Format(
            "[TimeTracker] Starting time tracking for MR IId {0} (project {1})",
            _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName));
      }

      public void Pause()
      {
         if (_stopwatch.IsRunning)
         {
            _stopwatch.Stop();
         }
      }

      public void Resume()
      {
         if (!_stopwatch.IsRunning)
         {
            _stopwatch.Start();
         }
      }

      async public Task<TimeSpan> Stop()
      {
         _stopwatch.Stop();
         TimeSpan span = Elapsed;

         MergeRequestEditor editor = new MergeRequestEditor(
            _hostProperties, _mergeRequestKey, _modificationListener, _networkOperationStatusListener);
         try
         {
            await editor.AddTrackedTime(span, true);
         }
         catch (TimeTrackingException ex)
         {
            if (ex.InnerException is OperatorException opex)
            {
               if (opex.InnerException is GitLabSharp.Accessors.GitLabRequestException glex)
               {
                  if (glex.InnerException is System.Net.WebException wex)
                  {
                     if (wex.Response is System.Net.HttpWebResponse response)
                     {
                        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                        {
                           throw new ForbiddenTimeTrackerException(ex, span);
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                           if (span.TotalSeconds < 1)
                           {
                              throw new TooSmallSpanTimeTrackerException(ex, span);
                           }
                        }
                     }
                  }
               }
            }
            throw new TimeTrackerException("Cannot stop timer", ex, span);
         }

         Trace.TraceInformation(String.Format(
            "[TimeTracker] Time tracking stopped. Sending {0} for MR IId {1} (project {2})",
            span.ToString(@"hh\:mm\:ss"), _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName));
         return span;
      }

      public void Cancel()
      {
         Trace.TraceInformation(String.Format(
            "[TimeTracker] Time tracking for MR IId {0} (project {1}) cancelled",
            _mergeRequestKey.IId, _mergeRequestKey.ProjectKey.ProjectName));

         _stopwatch.Stop();
      }

      public MergeRequestKey MergeRequest => _mergeRequestKey;

      public TimeSpan Elapsed
      {
         get
         {
            // trim milliseconds
            TimeSpan spanTemp = _stopwatch.Elapsed;
            return new TimeSpan(spanTemp.Hours, spanTemp.Minutes, spanTemp.Seconds);
         }
      }

      private readonly MergeRequestKey _mergeRequestKey;
      private readonly IHostProperties _hostProperties;
      private readonly IModificationListener _modificationListener;
      private readonly Stopwatch _stopwatch;
      private readonly INetworkOperationStatusListener _networkOperationStatusListener;
   }
}

