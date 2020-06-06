using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Client.MergeRequests;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Constants;
using mrHelper.Client.Session;

namespace mrHelper.Client.Discussions
{
   /// TODO This class was not designed well. Consider splitting it into:
   /// - Manager
   /// - Storage
   /// - State
   /// - Updater/Loader
   /// by analogy with MergeRequestCache and its internals
   /// <summary>
   /// Manages merge request discussions
   /// </summary>
   internal class DiscussionManager :
      IDisposable,
      IDiscussionCacheInternal
   {
      internal DiscussionManager(GitLabClientContext clientContext,
         string hostname, User user, IMergeRequestCache mergeRequestCache, SessionContext sessionContext)
      {
         _operator = new DiscussionOperator(hostname, clientContext.HostProperties);

         _parser = new DiscussionParser(this, clientContext.DiscussionKeywords, user);
         _parser.DiscussionEvent += onDiscussionParserEvent;

         _mergeRequestFilterChecker = clientContext.MergeRequestFilterChecker;

         _currentUser = user;
         _mergeRequestCache = mergeRequestCache;
         _mergeRequestCache.MergeRequestEvent += OnMergeRequestEvent;

         if (sessionContext.UpdateRules.UpdateDiscussions)
         {
            _timer = new System.Timers.Timer { Interval = clientContext.AutoUpdatePeriodMs };
            _timer.Elapsed += onTimer;
            _timer.SynchronizingObject = clientContext.SynchronizeInvoke;
            _timer.Start();

            scheduleUpdate(null /* update all merge requests cached at the moment of update processing */,
               EDiscussionUpdateType.InitialSnapshot);
         }
      }

      public void Dispose()
      {
         _mergeRequestCache.MergeRequestEvent -= OnMergeRequestEvent;

         _parser.DiscussionEvent -= onDiscussionParserEvent;
         _parser.Dispose();

         _timer?.Stop();
         _timer?.Dispose();

         foreach (System.Timers.Timer timer in _oneShotTimers)
         {
            timer.Stop();
            timer.Dispose();
         }
         _oneShotTimers.Clear();

         _operator.Dispose();
      }

      public event Action<MergeRequestKey> DiscussionsLoading;
      public event Action<MergeRequestKey, IEnumerable<Discussion>> DiscussionsLoaded;

      public event Action<UserEvents.DiscussionEvent> DiscussionEvent;
      public event Action<MergeRequestKey, IEnumerable<Discussion>, EDiscussionUpdateType> DiscussionsLoadedInternal;

      public DiscussionCount GetDiscussionCount(MergeRequestKey mrk)
      {
         int? resolvable = null;
         int? resolved = null;
         DiscussionCount.EStatus status = DiscussionCount.EStatus.NotAvailable;

         if (_loading.Contains(mrk))
         {
            status = DiscussionCount.EStatus.Loading;
         }
         else if (_cachedDiscussions.ContainsKey(mrk))
         {
            status = DiscussionCount.EStatus.Ready;
            resolvable = _cachedDiscussions[mrk].ResolvableDiscussionCount;
            resolved = _cachedDiscussions[mrk].ResolvedDiscussionCount;
         }

         return new DiscussionCount(resolvable, resolved, status);
      }

      async public Task<IEnumerable<Discussion>> LoadDiscussions(MergeRequestKey mrk)
      {
         if (isUpdating(mrk))
         {
            // To avoid re-entrance in updateDiscussionsAsync()
            await waitForUpdateCompetion(mrk);
         }
         Debug.Assert(!_updating.Contains(mrk));

         try
         {
            await updateDiscussionsAsync(mrk, EDiscussionUpdateType.PeriodicUpdate);
         }
         catch (OperatorException ex)
         {
            throw new DiscussionCacheException(String.Format(
               "Cannot update discussions for MR: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()), ex);
         }

         Debug.Assert(!_reconnect || !_cachedDiscussions.ContainsKey(mrk));
         return _cachedDiscussions.ContainsKey(mrk) ? _cachedDiscussions[mrk].Discussions : null;
      }

      public DiscussionCreator GetDiscussionCreator(MergeRequestKey mrk)
      {
         return new DiscussionCreator(mrk, _operator, _currentUser);
      }

      public DiscussionEditor GetDiscussionEditor(MergeRequestKey mrk, string discussionId)
      {
         return new DiscussionEditor(mrk, discussionId, _operator,
            () =>
            {
               // TODO It can be removed when GitLab issue is fixed, see commit message
               if (!_cachedDiscussions.ContainsKey(mrk))
               {
                  return;
               }

               Trace.TraceInformation(String.Format(
                  "[DiscussionManager] Remove MR from cache after a Thread is (un)resolved: "
                + "Host={0}, Project={1}, IId={2}",
                  mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
               _cachedDiscussions.Remove(mrk);
            });
      }

      /// <summary>
      /// Request to update discussions of the specified MR after the specified time period (in milliseconds)
      /// </summary>
      public void RequestUpdate(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished)
      {
         if (_timer == null)
         {
            // updates are disabled
            return;
         }

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

         timer.Elapsed += (s, e) =>
         {
            if (mrk != null)
            {
               Trace.TraceInformation(String.Format(
                  "[DiscussionManager] Scheduling update of discussions for a merge request with IId {0}",
               mrk.Value.IId));
               scheduleUpdate(new MergeRequestKey[] { mrk.Value }, EDiscussionUpdateType.PeriodicUpdate);
            }
            else
            {
               onTimer(null, null);
            }

            onUpdateFinished?.Invoke();
            _timer?.Start();
         };
         _timer?.Stop();
         timer.Start();

         _oneShotTimers.Add(timer);
      }

      private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         Trace.TraceInformation(
            "[DiscussionManager] Scheduling update of discussions for ALL merge requests on a timer update");

         scheduleUpdate(null /* update all merge requests cached at the moment of update processing */,
            EDiscussionUpdateType.PeriodicUpdate);
      }

      async Task updateDiscussions(MergeRequestKey mrk, EDiscussionUpdateType type)
      {
         if (_reconnect)
         {
            return;
         }

         try
         {
            await updateDiscussionsAsync(mrk, type);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(String.Format(
               "Cannot update discussions for MR: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()), ex);
         }
      }

      async private Task processScheduledUpdate(ScheduledUpdate scheduledUpdate)
      {
         if (scheduledUpdate.MergeRequests == null)
         {
            getAllMergeRequests(
               out IEnumerable<MergeRequestKey> matchingFilter,
               out IEnumerable<MergeRequestKey> nonMatchingFilter);

            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Processing scheduled update of discussions for {0}+{1} merge requests (ALL)",
               matchingFilter.Count(), nonMatchingFilter.Count()));

            await TaskUtils.RunConcurrentFunctionsAsync(matchingFilter,
               x => updateDiscussions(x, scheduledUpdate.Type),
               Constants.CrossProjectMergeRequestsInBatch, Constants.CrossProjectMergeRequestsInterBatchDelay,
               () => _reconnect);

            await TaskUtils.RunConcurrentFunctionsAsync(nonMatchingFilter,
               x => updateDiscussions(x, scheduledUpdate.Type),
               Constants.CrossProjectMergeRequestsInBatch, Constants.CrossProjectMergeRequestsInterBatchDelay,
               () => _reconnect);
         }
         else
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Processing scheduled update of discussions for {0} merge requests",
               scheduledUpdate.MergeRequests.Count()));

            await TaskUtils.RunConcurrentFunctionsAsync(scheduledUpdate.MergeRequests,
               x => updateDiscussions(x, scheduledUpdate.Type),
               Constants.CrossProjectMergeRequestsInBatch, Constants.CrossProjectMergeRequestsInterBatchDelay,
               () => _reconnect);
         }

         if (_reconnect)
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] update loop is cancelled due to _reconnect state"));
         }
      }

      private void scheduleUpdate(IEnumerable<MergeRequestKey> keys, EDiscussionUpdateType type)
      {
         if (type == EDiscussionUpdateType.InitialSnapshot)
         {
            _reconnect = true;
         }

         // make a copy of `keys` just in case
         _scheduledUpdates.Enqueue(new ScheduledUpdate(keys, type));

         _timer?.SynchronizingObject.BeginInvoke(new Action(async () =>
         {
            // 1. To avoid re-entrance in updateDiscussionsAsync()
            // 2. Make it before resetting _reconnect flag to allow an ongoing update loop to break
            await waitForUpdateCompetion(null);
            Debug.Assert(!_updating.Any());

            if (_scheduledUpdates.Any())
            {
               ScheduledUpdate scheduledUpdate = _scheduledUpdates.Dequeue();

               if (_reconnect)
               {
                  if (scheduledUpdate.Type != EDiscussionUpdateType.InitialSnapshot)
                  {
                     Trace.TraceInformation("[DiscussionManager] update is skipped due to _reconnect state");
                     return;
                  }
                  Debug.Assert(!_cachedDiscussions.Any() && !_closed.Any());
                  _reconnect = false;
               }

               await processScheduledUpdate(scheduledUpdate);
            }
         }), null);
      }

      async private Task updateDiscussionsAsync(MergeRequestKey mrk, EDiscussionUpdateType type)
      {
         if (_updating.Contains(mrk))
         {
            // Such update can be caused by GetDiscussionsAsync() called while we are looping in processScheduledUpdate()
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] update is skipped due to concurrent update request for MR: " +
               "Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
            return;
         }

         try
         {
            _updating.Add(mrk);
            if (_reconnect)
            {
               return;
            }

            Note mostRecentNote = await _operator.GetMostRecentUpdatedNoteAsync(mrk);
            if (mostRecentNote == null)
            {
               return;
            }

            int noteCount = await _operator.GetNoteCount(mrk);
            if (_reconnect || !needToLoadDiscussions(mostRecentNote, mrk, noteCount))
            {
               return;
            }

            IEnumerable<Discussion> discussions;
            try
            {
               _loading.Add(mrk);
               DiscussionsLoading?.Invoke(mrk);
               discussions = await _operator.GetDiscussionsAsync(mrk);
            }
            catch (OperatorException)
            {
               throw;
            }
            finally
            {
               _loading.Remove(mrk);
            }

            if (!_reconnect)
            {
               cacheDiscussions(mrk, discussions);
            }
            DiscussionsLoaded?.Invoke(mrk, discussions);
            DiscussionsLoadedInternal?.Invoke(mrk, discussions, type);
         }
         finally
         {
            _updating.Remove(mrk);
         }
      }

      async private Task waitForUpdateCompetion(MergeRequestKey? mrk)
      {
         if (mrk != null)
         {
            if (_updating.Contains(mrk.Value))
            {
               Trace.TraceInformation(String.Format(
                  "[DiscussionManager] Waiting for completion of updating discussions for MR: "
                + "Host={0}, Project={1}, IId={2}",
                  mrk.Value.ProjectKey.HostName, mrk.Value.ProjectKey.ProjectName, mrk.Value.IId.ToString()));
               await TaskUtils.WhileAsync(() => _updating.Contains(mrk.Value));
            }
         }
         else
         {
            if (_updating.Any())
            {
               Trace.TraceInformation(String.Format(
                  "[DiscussionManager] Waiting for completion of updating discussions"));
               await TaskUtils.WhileAsync(() => _updating.Any());
            }
         }
      }

      private bool isUpdating(MergeRequestKey mrk)
      {
         return _updating.Contains(mrk);
      }

      private bool needToLoadDiscussions(Note mostRecentNote, MergeRequestKey mrk, int noteCount)
      {
         DateTime mergeRequestUpdatedAt = mostRecentNote.Updated_At;
         if (_cachedDiscussions.ContainsKey(mrk)
          && mergeRequestUpdatedAt <= _cachedDiscussions[mrk].TimeStamp
          && noteCount == _cachedDiscussions[mrk].NoteCount)
         {
            Debug.WriteLine(String.Format(
               "[DiscussionManager] Discussions are up-to-date (Project={0}, IId={1}), "
             + "remote time stamp {2}, cached time stamp {3}, note count {4}, resolved {5}, resolvable {6}",
               mrk.ProjectKey.ProjectName, mrk.IId.ToString(),
               mergeRequestUpdatedAt.ToLocalTime().ToString(),
               _cachedDiscussions[mrk].TimeStamp.ToLocalTime().ToString(),
               noteCount,
               _cachedDiscussions[mrk].ResolvedDiscussionCount, _cachedDiscussions[mrk].ResolvableDiscussionCount));
            return false;
         }

         if (_closed.Contains(mrk))
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Will not update MR because it is closed: Project={0}, IId={1}",
               mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
            _closed.Remove(mrk);
            return false;
         }

         return true;
      }

      private void cacheDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions)
      {
         if (discussions == null || !discussions.Any())
         {
            return;
         }

         if (!_closed.Contains(mrk))
         {
            int noteCount = discussions.Select(x => x.Notes?.Count() ?? 0).Sum();
            DateTime latestNoteTimestamp = discussions
               .Select(x => x.Notes
                  .OrderBy(y => y.Updated_At)
                  .LastOrDefault())
               .OrderBy(z => z.Updated_At)
               .LastOrDefault().Updated_At;
            calcDiscussionCount(discussions, out int resolvableDiscussionCount, out int resolvedDiscussionCount);

            DateTime? prevUpdateTimestamp = _cachedDiscussions.ContainsKey(mrk) ?
               _cachedDiscussions[mrk].TimeStamp : new Nullable<DateTime>();

            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Cached {0} discussions for MR: Project={1}, IId={2},"
             + " cached time stamp {3} (was {4} before update), note count = {5}, resolved = {6}, resolvable = {7}",
               discussions.Count(), mrk.ProjectKey.ProjectName, mrk.IId.ToString(),
               latestNoteTimestamp.ToLocalTime().ToString(),
               prevUpdateTimestamp?.ToLocalTime().ToString() ?? "N/A",
               noteCount, resolvedDiscussionCount, resolvableDiscussionCount));

            _cachedDiscussions[mrk] = new CachedDiscussions(
               prevUpdateTimestamp, latestNoteTimestamp, noteCount,
               discussions, resolvableDiscussionCount, resolvedDiscussionCount);
         }
         else
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Will not cache MR because it is closed: Project={0}, IId={1}",
               mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
            _closed.Remove(mrk);
         }
      }

      private void calcDiscussionCount(IEnumerable<Discussion> discussions, out int resolvable, out int resolved)
      {
         resolvable = 0;
         resolved = 0;

         foreach (Discussion discussion in discussions)
         {
            if (discussion.Notes.Any(x => x.Resolvable))
            {
               ++resolvable;
               if (discussion.Notes.All(x => !x.Resolvable || x.Resolved))
               {
                  ++resolved;
               }
            }
         }
      }

      private void onDiscussionParserEvent(UserEvents.DiscussionEvent e,
         DateTime eventTimestamp, EDiscussionUpdateType type)
      {
         switch (type)
         {
            case EDiscussionUpdateType.InitialSnapshot:
               // Don't send out any notifications on initial snapshot, e.g. when just connected to host
               // because we don't want to notify about all old events
               return;

            case EDiscussionUpdateType.NewMergeRequest:
               // Notify about whatever is found in a new merge request
               DiscussionEvent?.Invoke(e);
               return;

            case EDiscussionUpdateType.PeriodicUpdate:
               // Notify about new events in merge requests that are cached already
               if (_cachedDiscussions.TryGetValue(e.MergeRequestKey, out CachedDiscussions cached)
                 && cached.PrevTimeStamp.HasValue && eventTimestamp > cached.PrevTimeStamp.Value)
               {
                  DiscussionEvent?.Invoke(e);
               }
               return;
         }

         Debug.Assert(false);
      }

      public void OnMergeRequestEvent(UserEvents.MergeRequestEvent e)
      {
         switch (e.EventType)
         {
            case UserEvents.MergeRequestEvent.Type.NewMergeRequest:
               Trace.TraceInformation(String.Format(
                  "[DiscussionManager] Scheduling update of discussions for a new merge request with IId {0}",
                  e.FullMergeRequestKey.MergeRequest.IId));

               MergeRequestKey mrk = new MergeRequestKey(
                  e.FullMergeRequestKey.ProjectKey, e.FullMergeRequestKey.MergeRequest.IId);

               if (_closed.Contains(mrk))
               {
                  Trace.TraceInformation(String.Format(
                     "[DiscussionManager] Merge Request with IId {0} was reopened",
                     e.FullMergeRequestKey.MergeRequest.IId));
                  _closed.Remove(mrk);
               }
               scheduleUpdate(new MergeRequestKey[] { mrk }, EDiscussionUpdateType.NewMergeRequest);
               break;

            case UserEvents.MergeRequestEvent.Type.ClosedMergeRequest:
               {
               MergeRequestKey closedMRK = new MergeRequestKey(
                  e.FullMergeRequestKey.ProjectKey, e.FullMergeRequestKey.MergeRequest.IId);

                  Trace.TraceInformation(String.Format(
                     "[DiscussionManager] Clean up closed MR: Project={0}, IId={1}",
                     closedMRK.ProjectKey.ProjectName, closedMRK.IId.ToString()));
                  _cachedDiscussions.Remove(closedMRK);
                  _closed.Add(closedMRK);
               }
               break;

            case UserEvents.MergeRequestEvent.Type.UpdatedMergeRequest:
               // do nothing
               break;

            default:
               Debug.Assert(false);
               break;
         }
      }

      private void clearCache()
      {
         Trace.TraceInformation(String.Format(
            "[DiscussionManager] State cleaned up ({0} cached and {1} closed)",
            _cachedDiscussions.Count(), _closed.Count()));
         _cachedDiscussions.Clear();
         _closed.Clear();
      }

      private void getAllMergeRequests(
         out IEnumerable<MergeRequestKey> matchingFilter,
         out IEnumerable<MergeRequestKey> nonMatchingFilter)
      {
         List<MergeRequestKey> matchingFilterList = new List<MergeRequestKey>();
         List<MergeRequestKey> nonMatchingFilterList = new List<MergeRequestKey>();
         foreach (ProjectKey projectKey in _mergeRequestCache.GetProjects())
         {
            matchingFilterList.AddRange(
               _mergeRequestCache.GetMergeRequests(projectKey)
                  .Where(x => _mergeRequestFilterChecker.DoesMatchFilter(x))
                  .Select(x => new MergeRequestKey(projectKey, x.IId)));

            nonMatchingFilterList.AddRange(
               _mergeRequestCache.GetMergeRequests(projectKey)
                  .Where(x => !_mergeRequestFilterChecker.DoesMatchFilter(x))
                  .Select(x => new MergeRequestKey(projectKey, x.IId)));
         }
         matchingFilter = matchingFilterList;
         nonMatchingFilter = nonMatchingFilterList;
      }

      private readonly User _currentUser;
      private readonly IMergeRequestCache _mergeRequestCache;
      private readonly IMergeRequestFilterChecker _mergeRequestFilterChecker;

      private readonly DiscussionParser _parser;
      private readonly DiscussionOperator _operator;

      private readonly System.Timers.Timer _timer;
      private readonly List<System.Timers.Timer> _oneShotTimers = new List<System.Timers.Timer>();

      private class CachedDiscussions
      {
         public CachedDiscussions(DateTime? prevTimeStamp, DateTime timeStamp, int noteCount,
            IEnumerable<Discussion> discussions, int resolvableDiscussionCount, int resolvedDiscussionCount)
         {
            PrevTimeStamp = prevTimeStamp;
            TimeStamp = timeStamp;
            NoteCount = noteCount;
            Discussions = discussions;
            ResolvableDiscussionCount = resolvableDiscussionCount;
            ResolvedDiscussionCount = resolvedDiscussionCount;
         }

         public DateTime? PrevTimeStamp { get; }
         public DateTime TimeStamp { get; }
         public int NoteCount { get; }
         public IEnumerable<Discussion> Discussions { get; }
         public int ResolvableDiscussionCount { get; }
         public int ResolvedDiscussionCount { get; }
      }

      private readonly Dictionary<MergeRequestKey, CachedDiscussions> _cachedDiscussions =
         new Dictionary<MergeRequestKey, CachedDiscussions>();

      /// <summary>
      /// _updating collection allows to avoid re-entrance in updateDiscussionsAsync()
      /// It cannot be a single value because GetDiscussionsAsync() may interleave with processScheduledUpdate()
      /// and because we load multiple MR at once
      /// </summary>
      private readonly HashSet<MergeRequestKey> _updating = new HashSet<MergeRequestKey>();

      /// <summary>
      /// temporary collection to track Loading status
      /// It cannot be a single value because we load multiple MR at once
      /// </summary>
      private readonly HashSet<MergeRequestKey> _loading = new HashSet<MergeRequestKey>();

      /// <summary>
      /// temporary _closed collection serves to not cache what is not needed to cache
      /// </summary>
      private readonly HashSet<MergeRequestKey> _closed = new HashSet<MergeRequestKey>();

      /// <summary>
      /// Shows that reconnect is in progress, and all updates are ignored within this period
      /// </summary>
      private bool _reconnect;

      private struct ScheduledUpdate
      {
         internal ScheduledUpdate(IEnumerable<MergeRequestKey> mergeRequests,
            EDiscussionUpdateType type)
         {
            MergeRequests = mergeRequests;
            Type = type;
         }

         internal IEnumerable<MergeRequestKey> MergeRequests { get; }
         internal EDiscussionUpdateType Type { get; }
      }

      /// <summary>
      /// Queue of updates scheduled in scheduleUpdates() method for asynchronous processing
      /// </summary>
      private readonly Queue<ScheduledUpdate> _scheduledUpdates = new Queue<ScheduledUpdate>();
   }
}

