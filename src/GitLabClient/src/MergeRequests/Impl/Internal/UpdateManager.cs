using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using mrHelper.Client.Common;
using mrHelper.Client.Types;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.MergeRequests
{
   /// <summary>
   /// Manages updates
   /// </summary>
   internal class UpdateManager :
      IDisposable,
      ILoader<IMergeRequestEventListener>,
      ILoader<IMergeRequestListLoaderListener>,
      ILoader<IMergeRequestLoaderListener>,
      ILoader<IVersionLoaderListener>
   {
      internal UpdateManager(
         GitLabClientContext clientContext,
         string hostname,
         IWorkflowContext context,
         IWorkflowDetailsCacheReader cache)
      {
         // We don't need to toggle these callbacks during updates
         clientContext.OnNotFoundProject = null;
         clientContext.OnForbiddenProject = null;

         WorkflowDataOperator updateOperator = new WorkflowDataOperator(
            hostname, clientContext.HostProperties.GetAccessToken(hostname));
         _versionLoader = new VersionLoader(updateOperator);
         _mergeRequestListLoader = MergeRequestListLoaderFactory.CreateMergeRequestListLoader(
            clientContext, updateOperator, context, _versionLoader);
         _mergeRequestLoader = new MergeRequestLoader(updateOperator, _versionLoader);

         _cache = cache;
         _context = context;

         _timer = new System.Timers.Timer { Interval = clientContext.AutoUpdatePeriodMs };
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = clientContext.SynchronizeInvoke;
         _timer.Start();
      }

      public void Dispose()
      {
         _timer?.Stop();
         _timer?.Dispose();
         _timer = null;

         foreach (System.Timers.Timer timer in _oneShotTimers)
         {
            timer.Stop();
            timer.Dispose();
         }
         _oneShotTimers.Clear();
      }

      public INotifier<IMergeRequestEventListener> GetNotifier() => _eventNotifier;

      INotifier<IMergeRequestListLoaderListener> ILoader<IMergeRequestListLoaderListener>.GetNotifier() =>
         _mergeRequestListLoader.GetNotifier();

      INotifier<IMergeRequestLoaderListener> ILoader<IMergeRequestLoaderListener>.GetNotifier() =>
         _mergeRequestLoader.GetNotifier();

      INotifier<IVersionLoaderListener> ILoader<IVersionLoaderListener>.GetNotifier() =>
         _versionLoader.GetNotifier();

      public void RequestOneShotUpdate(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished)
      {
         foreach (int interval in intervals)
         {
            enqueueOneShotTimer(mrk, interval, onUpdateFinished);
         }
      }

      private void enqueueOneShotTimer(MergeRequestKey? mrk, int interval, Action onUpdateFinished)
      {
         if (interval < 1)
         {
            return;
         }

         System.Timers.Timer timer = new System.Timers.Timer
         {
            Interval = interval,
            AutoReset = false,
            SynchronizingObject = _timer?.SynchronizingObject
         };

         timer.Elapsed +=
            async (s, e) =>
         {
            IEnumerable<UserEvents.MergeRequestEvent> updates =
               await (mrk.HasValue ? updateOneOnTimer(mrk.Value) : updateAllOnTimer());

            if (updates != null)
            {
               updates.ToList().ForEach(x => _eventNotifier.OnMergeRequestEvent(x));
            }

            onUpdateFinished?.Invoke();
            _timer?.Start();
         };
         _timer?.Stop();
         timer.Start();

         _oneShotTimers.Add(timer);
      }

      /// <summary>
      /// Process a timer event
      /// </summary>
      async private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         IEnumerable<UserEvents.MergeRequestEvent> updates = await updateAllOnTimer();

         if (updates != null)
         {
            updates.ToList().ForEach(x => _eventNotifier.OnMergeRequestEvent(x));
         }
      }

      async private Task<IEnumerable<UserEvents.MergeRequestEvent>> updateOneOnTimer(MergeRequestKey mrk)
      {
         if (_updating)
         {
            return null;
         }

         IWorkflowDetails oldDetails = _cache.Details.Clone();

         try
         {
            _updating = true;
            await _mergeRequestLoader.LoadMergeRequest(mrk, EComparableEntityType.None);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle("Cannot perform a one-shot update", ex);
            return null;
         }
         finally
         {
            _updating = false;
         }

         IEnumerable<UserEvents.MergeRequestEvent> updates = _checker.CheckForUpdates(oldDetails, _cache.Details);

         int legalUpdates = updates.Count(x => x.Labels);
         Debug.Assert(legalUpdates == 0 || legalUpdates == 1);

         Trace.TraceInformation(
            String.Format(
               "[UpdateManager] Updated Labels: {0}. MRK: HostName={1}, ProjectName={2}, IId={3}",
               legalUpdates, mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId));

         return updates;
      }

      async private Task<IEnumerable<UserEvents.MergeRequestEvent>> updateAllOnTimer()
      {
         if (_updating)
         {
            return null;
         }

         IWorkflowDetails oldDetails = _cache.Details.Clone();

         try
         {
            _updating = true;
            await _mergeRequestListLoader.Load(_context);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle("Cannot update merge requests on timer", ex);
         }
         finally
         {
            _updating = false;
         }

         IEnumerable<UserEvents.MergeRequestEvent> updates = _checker.CheckForUpdates(oldDetails, _cache.Details);

         Trace.TraceInformation(
            String.Format(
               "[UpdateManager] Merge Request Updates: New {0}, Updated commits {1}, Updated labels {2}, Closed {3}",
               updates.Count(x => x.New),
               updates.Count(x => x.Commits),
               updates.Count(x => x.Labels),
               updates.Count(x => x.Closed)));

         return updates;
      }

      private System.Timers.Timer _timer;
      private List<System.Timers.Timer> _oneShotTimers = new List<System.Timers.Timer>();

      private readonly IWorkflowContext _context;
      private readonly IMergeRequestListLoader _mergeRequestListLoader;
      private readonly IMergeRequestLoader _mergeRequestLoader;
      private readonly IVersionLoader _versionLoader;
      private readonly IWorkflowDetailsCacheReader _cache;
      private readonly WorkflowDetailsChecker _checker = new WorkflowDetailsChecker();
      private readonly MergeRequestEventNotifier _eventNotifier = new MergeRequestEventNotifier();

      private bool _updating; /// prevents re-entrance in timer updates
   }
}

