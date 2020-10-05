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
         DataCacheConnectionContext context,
         InternalCacheUpdater cacheUpdater)
      {
         DataCacheOperator updateOperator = new DataCacheOperator(hostname, hostProperties);
         _mergeRequestListLoader = MergeRequestListLoaderFactory.CreateMergeRequestListLoader(
            hostname, updateOperator, context, cacheUpdater);
         _mergeRequestLoader = new MergeRequestLoader(updateOperator, cacheUpdater);

         _cache = cacheUpdater.Cache;

         _timer = new System.Timers.Timer
         {
            Interval = context.UpdateRules.UpdateMergeRequestsPeriod.Value
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

      private void enqueueOneShotTimer(MergeRequestKey? mrk, int interval, Action onUpdateFinished)
      {
         if (interval < 1)
         {
            return;
         }

         System.Timers.Timer oneShotTimer = new System.Timers.Timer
         {
            Interval = interval,
            AutoReset = false,
            SynchronizingObject = _timer?.SynchronizingObject
         };

         oneShotTimer.Elapsed +=
            async (s, e) =>
         {
            IEnumerable<UserEvents.MergeRequestEvent> updates =
               await (mrk.HasValue ? updateOneOnTimer(mrk.Value) : updateAllOnTimer());
            notify(updates);

            onUpdateFinished?.Invoke();
            _oneShotTimers.Remove(oneShotTimer);
            oneShotTimer.Dispose();
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
            await TaskUtils.WhileAsync(() => _updating);
            return null;
         }

         IInternalCache oldDetails = _cache.Clone();

         try
         {
            _updating = true;
            await _mergeRequestLoader.LoadMergeRequest(mrk);
            MergeRequestRefreshed?.Invoke(mrk);
         }
         catch (BaseLoaderException ex)
         {
            ExceptionHandlers.Handle("Cannot perform a one-shot update", ex);
            return null;
         }
         finally
         {
            _updating = false;
         }

         IEnumerable<UserEvents.MergeRequestEvent> updates = _checker.CheckForUpdates(oldDetails, _cache);

         int legalUpdates = updates.Count(x => x.Labels);
         Debug.Assert(legalUpdates == 0 || legalUpdates == 1);

         if (legalUpdates > 0)
         {
            Trace.TraceInformation(
               String.Format(
                  "[UpdateManager] Updated Labels: {0}. MRK: HostName={1}, ProjectName={2}, IId={3}",
                  legalUpdates, mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId));
         }

         return updates;
      }

      async private Task<IEnumerable<UserEvents.MergeRequestEvent>> updateAllOnTimer()
      {
         if (_updating)
         {
            await TaskUtils.WhileAsync(() => _updating);
            return null;
         }

         IInternalCache oldDetails = _cache.Clone();

         try
         {
            _updating = true;
            await _mergeRequestListLoader.Load();
            MergeRequestListRefreshed?.Invoke();
         }
         catch (BaseLoaderException ex)
         {
            ExceptionHandlers.Handle("Cannot update merge requests on timer", ex);
         }
         finally
         {
            _updating = false;
         }

         IEnumerable<UserEvents.MergeRequestEvent> updates = _checker.CheckForUpdates(oldDetails, _cache);

         int newMergeRequestsCount = updates.Count(x => x.New);
         int mergeRequestsWithUpdatedCommitsCount = updates.Count(x => x.Commits);
         int mergeRequestsWithUpdatedLabelsCount = updates.Count(x => x.Labels);
         int mergeRequestsWithUpdatedDetailsCount = updates.Count(x => x.Details);
         int closedMergeRequestsCount = updates.Count(x => x.Closed);
         if (newMergeRequestsCount > 0
          || mergeRequestsWithUpdatedCommitsCount > 0
          || mergeRequestsWithUpdatedLabelsCount > 0
          || mergeRequestsWithUpdatedDetailsCount > 0
          || closedMergeRequestsCount > 0)
         {
            Trace.TraceInformation(
               String.Format(
                  "[UpdateManager] Merge Request Updates: " +
                  "New {0}, Updated commits {1}, Updated labels {2}, Updated details {3}, Closed {4}",
                  newMergeRequestsCount,
                  mergeRequestsWithUpdatedCommitsCount,
                  mergeRequestsWithUpdatedLabelsCount,
                  mergeRequestsWithUpdatedDetailsCount,
                  closedMergeRequestsCount));
         }

         return updates;
      }

      private void notify(IEnumerable<UserEvents.MergeRequestEvent> updates)
      {
         if (updates != null)
         {
            foreach (UserEvents.MergeRequestEvent update in updates) MergeRequestEvent?.Invoke(update);
         }
      }

      private System.Timers.Timer _timer;
      private readonly List<System.Timers.Timer> _oneShotTimers = new List<System.Timers.Timer>();

      private readonly IMergeRequestListLoader _mergeRequestListLoader;
      private readonly IMergeRequestLoader _mergeRequestLoader;
      private readonly IInternalCache _cache;
      private readonly InternalMergeRequestCacheComparator _checker =
         new InternalMergeRequestCacheComparator();

      private bool _updating; /// prevents re-entrance in timer updates
   }
}

