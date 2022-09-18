using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;
using mrHelper.GitLabClient.Loaders;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Managers
{
   /// <summary>
   /// Manages updates
   /// </summary>
   internal class UpdateManager : IDisposable
   {
      internal UpdateManager(
         DataCacheContext dataCacheContext,
         string hostname,
         IHostProperties hostProperties,
         SearchQueryCollection queryCollection,
         InternalCacheUpdater cacheUpdater,
         INetworkOperationStatusListener networkOperationStatusListener,
         bool isApprovalStatusSupported)
      {
         _updateOperator = new DataCacheOperator(hostname, hostProperties, networkOperationStatusListener);
         _mergeRequestListLoader = new MergeRequestListLoader(
            hostname, _updateOperator, cacheUpdater, null, queryCollection,
            isApprovalStatusSupported);
         _mergeRequestLoader = new MergeRequestLoader(_updateOperator, cacheUpdater,
            dataCacheContext.UpdateRules.UpdateOnlyOpenedMergeRequests, isApprovalStatusSupported);
         _extLogging = dataCacheContext.UpdateManagerExtendedLogging;
         _tagForLogging = dataCacheContext.TagForLogging;

         _cache = cacheUpdater.Cache;

         _timer = new System.Timers.Timer
         {
            Interval = dataCacheContext.UpdateRules.UpdateMergeRequestsPeriod.Value
         };
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = dataCacheContext.SynchronizeInvoke;
         _timer.Start();
      }

      internal event Action<UserEvents.MergeRequestEvent> MergeRequestEvent;
      internal event Action MergeRequestListRefreshed;
      internal event Action<MergeRequestKey> MergeRequestRefreshed;

      public void Dispose()
      {
         _timer?.Stop();
         _timer?.Dispose();
         _timer = null; // prevent accessing a timer which is disposed while waiting for async call

         foreach (System.Timers.Timer timer in _oneShotTimers)
         {
            timer.Stop();
            timer.Dispose();
         }
         _oneShotTimers.Clear();

         _mergeRequestListLoader = null;
         _mergeRequestLoader = null;
         _updateOperator?.Dispose();
         _updateOperator = null;
      }

      public void RequestOneShotUpdate(MergeRequestKey? mrk, int interval, Action onUpdateFinished)
      {
         enqueueOneShotTimer(mrk, interval, onUpdateFinished);
      }

      public void RequestOneShotUpdate(MergeRequestKey? mrk, int[] intervals)
      {
         foreach (int interval in intervals)
         {
            enqueueOneShotTimer(mrk, interval, null);
         }
      }

      private class OneShotTimer : System.Timers.Timer
      {
         internal OneShotTimer(System.ComponentModel.ISynchronizeInvoke synchronizeInvoke,
            int interval, MergeRequestKey? mrk)
            : base(interval)
         {
            SynchronizingObject = synchronizeInvoke;
            MergeRequestKey = mrk;
            AutoReset = false;
         }

         internal MergeRequestKey? MergeRequestKey { get; }
      }

      private void enqueueOneShotTimer(MergeRequestKey? mrk, int interval, Action onUpdateFinished)
      {
         string mrkString = mrk.HasValue
            ? String.Format("!{0} in {1}", mrk.Value.IId, mrk.Value.ProjectKey.ProjectName)
            : "null";
         traceDetailed(String.Format(
            "enqueueOneShotTimer() called for mrk {0}, interval={1}, onUpdateFinished={2}, _oneShotTimers.Count={3}",
            mrkString, interval, onUpdateFinished == null ? "null" : "non-null", _oneShotTimers.Count));

         if (interval < 1 || _timer == null)
         {
            return;
         }

         OneShotTimer oneShotTimer = new OneShotTimer(_timer?.SynchronizingObject, interval, mrk);
         oneShotTimer.Elapsed +=
            async (s, e) =>
         {
            IEnumerable<UserEvents.MergeRequestEvent> updates;
            if (mrk.HasValue)
            {
               try
               {
                  updates = await updateOneOnTimer(mrk.Value);
               }
               catch (RenamedProjectException)
               {
                  updates = await updateAllOnTimer();
               }
            }
            else
            {
               updates = await updateAllOnTimer();
            }
            notify(updates);

            onUpdateFinished?.Invoke();

            // the timer might have been dropped in updateAllOnTimer()
            if (_oneShotTimers.Contains(oneShotTimer))
            {
               _oneShotTimers.Remove(oneShotTimer);
               oneShotTimer.Dispose();
            }
         };

         oneShotTimer.Start();
         _oneShotTimers.Add(oneShotTimer);
      }

      async private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         IEnumerable<UserEvents.MergeRequestEvent> updates = await updateAllOnTimer();
         notify(updates);
      }

      async private Task<IEnumerable<UserEvents.MergeRequestEvent>> updateOneOnTimer(MergeRequestKey mrk)
      {
         if (_updating)
         {
            traceDetailed(String.Format("Cannot update !{0} in {1}", mrk.IId, mrk.ProjectKey.ProjectName));
            await TaskUtils.WhileAsync(() => _updating);
            traceDetailed(String.Format("Skipped update for !{0} in {1}", mrk.IId, mrk.ProjectKey.ProjectName));
            return null;
         }

         IInternalCache oldDetails = _cache.Clone();

         traceDetailed(String.Format("Ready to update !{0} in {1}", mrk.IId, mrk.ProjectKey.ProjectName));
         try
         {
            _updating = true;
            if (_mergeRequestLoader != null)
            {
               await _mergeRequestLoader.LoadMergeRequest(mrk);
            }
         }
         catch (RenamedProjectException)
         {
            throw;
         }
         catch (BaseLoaderException ex)
         {
            ExceptionHandlers.Handle("Cannot perform a one-shot update", ex);
         }
         finally
         {
            MergeRequestRefreshed?.Invoke(mrk);
            _updating = false;
            traceDetailed(String.Format("Updated !{0} in {1}", mrk.IId, mrk.ProjectKey.ProjectName));
         }

         IEnumerable<UserEvents.MergeRequestEvent> updates = _checker.CheckForUpdates(oldDetails, _cache);

         int legalUpdates = updates.Count(x => x.Labels);
         Debug.Assert(legalUpdates == 0 || legalUpdates == 1);

         if (legalUpdates > 0)
         {
            traceInformation(
               String.Format(
                  "Updated Labels: {0}. MRK: HostName={1}, ProjectName={2}, IId={3}",
                  legalUpdates, mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId));
         }

         return updates;
      }

      async private Task<IEnumerable<UserEvents.MergeRequestEvent>> updateAllOnTimer()
      {
         if (_updating)
         {
            traceDetailed("Cannot update the list");
            await TaskUtils.WhileAsync(() => _updating);
            traceDetailed("List update has been skipped");
            return null;
         }

         IInternalCache oldDetails = _cache.Clone();

         traceDetailed("Ready to update the list");
         try
         {
            _updating = true;
            if (_mergeRequestListLoader != null)
            {
               await _mergeRequestListLoader.Load();
            }
         }
         catch (BaseLoaderException ex)
         {
            ExceptionHandlers.Handle("Cannot update merge requests on timer", ex);
         }
         finally
         {
            MergeRequestListRefreshed?.Invoke();
            _updating = false;
            traceDetailed("Updated the list");
         }

         IEnumerable<UserEvents.MergeRequestEvent> updates = _checker.CheckForUpdates(oldDetails, _cache);

         int newMergeRequestsCount = updates.Count(x => x.AddedToCache);
         int mergeRequestsWithUpdatedCommitsCount = updates.Count(x => x.Commits);
         int mergeRequestsWithUpdatedLabelsCount = updates.Count(x => x.Labels);
         int mergeRequestsWithUpdatedDetailsCount = updates.Count(x => x.Details);
         int closedMergeRequestsCount = updates.Count(x => x.RemovedFromCache);
         if (newMergeRequestsCount > 0
          || mergeRequestsWithUpdatedCommitsCount > 0
          || mergeRequestsWithUpdatedLabelsCount > 0
          || mergeRequestsWithUpdatedDetailsCount > 0
          || closedMergeRequestsCount > 0)
         {
            traceInformation(
               String.Format(
                  "Merge Request Updates: " +
                  "New {0}, Updated commits {1}, Updated labels {2}, Updated details {3}, Closed {4}",
                  newMergeRequestsCount,
                  mergeRequestsWithUpdatedCommitsCount,
                  mergeRequestsWithUpdatedLabelsCount,
                  mergeRequestsWithUpdatedDetailsCount,
                  closedMergeRequestsCount));
         }
         IEnumerable<MergeRequestKey> removedMergeRequestKeys =
            updates
            .Where(x => x.RemovedFromCache)
            .Select(x => new MergeRequestKey(x.FullMergeRequestKey.ProjectKey,
                                             x.FullMergeRequestKey.MergeRequest.IId));
         stopOneShotTimers(removedMergeRequestKeys);

         return updates;
      }

      private void stopOneShotTimers(IEnumerable<MergeRequestKey> mergeRequestKeys)
      {
         foreach (MergeRequestKey mergeRequestKey in mergeRequestKeys)
         {
            for (int iTimer = 0; iTimer < _oneShotTimers.Count; ++iTimer)
            {
               OneShotTimer timer = _oneShotTimers[iTimer];
               if (timer.MergeRequestKey.Equals(mergeRequestKey))
               {
                  timer.Stop();
                  timer.Dispose();
                  _oneShotTimers[iTimer] = null;
                  traceDetailed(String.Format("Stopped timer with interval {0} for MRK !{1} in {2}",
                     timer.Interval, mergeRequestKey.IId, mergeRequestKey.ProjectKey.ProjectName));
               }
            }
         }
         _oneShotTimers.RemoveAll(x => x == null);
      }

      private void notify(IEnumerable<UserEvents.MergeRequestEvent> updates)
      {
         if (updates != null)
         {
            foreach (UserEvents.MergeRequestEvent update in updates) MergeRequestEvent?.Invoke(update);
         }
      }

      private void traceDetailed(string message)
      {
         if (_extLogging)
         {
            traceInformation(message);
         }
      }

      private void traceInformation(string message)
      {
         Trace.TraceInformation("[UpdateManager.{0}] {1}", _tagForLogging, message);
      }

      private System.Timers.Timer _timer;
      private readonly List<OneShotTimer> _oneShotTimers = new List<OneShotTimer>();

      private DataCacheOperator _updateOperator;
      private IMergeRequestListLoader _mergeRequestListLoader;
      private IMergeRequestLoader _mergeRequestLoader;
      private readonly IInternalCache _cache;
      private readonly InternalMergeRequestCacheComparator _checker =
         new InternalMergeRequestCacheComparator();
      private readonly bool _extLogging;
      private readonly string _tagForLogging;
      private bool _updating; /// prevents re-entrance in timer updates
   }
}

