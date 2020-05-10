using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Client.Workflow;
using mrHelper.Client.MergeRequests;
using mrHelper.Common.Tools;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Constants;

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
      IWorkflowEventListener,
      IDiscussionManager,
      IMergeRequestEventListener
   {
      internal DiscussionManager(GitLabClientContext clientContext, IWorkflowLoader workflowLoader)
      {
         _operator = new DiscussionOperator(clientContext.HostProperties);

         _parser = new DiscussionParser(workflowLoader.GetNotifier(), _notifierInternal, clientContext.Keywords);
         _parser.DiscussionEvent += onDiscussionParserEvent;

         _mergeRequestFilter = clientContext.MergeRequestFilter;

         _workflowEventNotifier = workflowLoader.GetNotifier();
         _workflowEventNotifier.AddListener(this);

         _timer = new System.Timers.Timer { Interval = clientContext.AutoUpdatePeriodMs };
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = clientContext.SynchronizeInvoke;
         _timer.Start();
      }

      public void Dispose()
      {
         _workflowEventNotifier.RemoveListener(this);

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
      }

      public INotifier<IDiscussionLoaderListener> GetNotifier() => _notifier;

      INotifier<IDiscussionEventListener> ILoader<IDiscussionEventListener>.GetNotifier() => _eventNotifier;

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

         return new DiscussionCount
         {
            Resolvable = resolvable,
            Resolved = resolved,
            Status = status
         };
      }

      async public Task<IEnumerable<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk)
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
            throw new DiscussionManagerException(String.Format(
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
      public void CheckForUpdates(MergeRequestKey? mrk, int[] intervals, Action onUpdateFinished)
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

         timer.Elapsed += (s, e) =>
         {
            if (mrk.HasValue)
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

      async private Task processScheduledUpdate(ScheduledUpdate scheduledUpdate)
      {
         getAllMergeRequests(
            out IEnumerable<MergeRequestKey> matchingFilter,
            out IEnumerable<MergeRequestKey> nonMatchingFilter);

         IEnumerable<MergeRequestKey> highPriorityMergeRequests =
            scheduledUpdate.MergeRequests ?? matchingFilter;
         IEnumerable<MergeRequestKey> lowPriorityMergeRequests =
            scheduledUpdate.MergeRequests == null ? nonMatchingFilter : new MergeRequestKey[] { };

         if (scheduledUpdate.MergeRequests == null)
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Processing scheduled update of discussions for {0}+{1} merge requests (ALL)",
               highPriorityMergeRequests.Count(), lowPriorityMergeRequests.Count()));
         }
         else
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Processing scheduled update of discussions for {0} merge requests",
               scheduledUpdate.MergeRequests.Count()));
         }

         async Task updateDiscussions(MergeRequestKey mrk)
         {
            if (_reconnect)
            {
               return;
            }

            try
            {
               await updateDiscussionsAsync(mrk, scheduledUpdate.Type);
            }
            catch (OperatorException ex)
            {
               ExceptionHandlers.Handle(String.Format(
                  "Cannot update discussions for MR: Host={0}, Project={1}, IId={2}",
                  mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()), ex);
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(highPriorityMergeRequests, x => updateDiscussions(x),
            Constants.CrossProjectMergeRequestsInBatch, Constants.CrossProjectMergeRequestsInterBatchDelay,
            () => _reconnect);

         await TaskUtils.RunConcurrentFunctionsAsync(lowPriorityMergeRequests, x => updateDiscussions(x),
            Constants.CrossProjectMergeRequestsInBatch, Constants.CrossProjectMergeRequestsInterBatchDelay,
            () => _reconnect);

         if (_reconnect)
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] update loop is cancelled due to _reconnect state"));
         }
      }

      private void scheduleUpdate(IEnumerable<MergeRequestKey> keys, EDiscussionUpdateType type)
      {
         _scheduledUpdates.Enqueue(new ScheduledUpdate
         {
            MergeRequests = keys?.ToArray(), // make a copy just in case
            Type = type
         });

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
            int noteCount = await _operator.GetNoteCount(mrk);
            if (_reconnect || !needToLoadDiscussions(mostRecentNote, mrk, noteCount))
            {
               return;
            }

            IEnumerable<Discussion> discussions;
            try
            {
               _loading.Add(mrk);
               _notifier.OnPreLoadDiscussions(mrk);
               discussions = await _operator.GetDiscussionsAsync(mrk);
            }
            catch (OperatorException)
            {
               _notifier.OnFailedLoadDiscussions(mrk);
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
            _notifier.OnPostLoadDiscussions(mrk, discussions);
            _notifierInternal.OnPostLoadDiscussionsInternal(mrk, discussions, type);
         }
         finally
         {
            _updating.Remove(mrk);
         }
      }

      async private Task waitForUpdateCompetion(MergeRequestKey? mrk)
      {
         if (mrk.HasValue)
         {
            if (_updating.Contains(mrk.Value))
            {
               Trace.TraceInformation(String.Format(
                  "[DiscussionManager] Waiting for completion of updating discussions for MR: "
                + "Host={0}, Project={1}, IId={2}",
                  mrk.Value.ProjectKey.HostName, mrk.Value.ProjectKey.ProjectName, mrk.Value.IId.ToString()));

               while (_updating.Contains(mrk.Value)) //-V3120
               {
                  await Task.Delay(50);
               }
            }
         }
         else
         {
            if (_updating.Any())
            {
               Trace.TraceInformation(String.Format(
                  "[DiscussionManager] Waiting for completion of updating discussions"));

               while (_updating.Any()) //-V3120
               {
                  await Task.Delay(50);
               }
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

            _cachedDiscussions[mrk] = new CachedDiscussions
            {
               PrevTimeStamp = prevUpdateTimestamp,
               TimeStamp = latestNoteTimestamp,
               NoteCount = noteCount,
               Discussions = discussions.ToArray(),
               ResolvableDiscussionCount = resolvableDiscussionCount,
               ResolvedDiscussionCount = resolvedDiscussionCount
            };
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
               _eventNotifier.OnDiscussionEvent(e);
               return;

            case EDiscussionUpdateType.PeriodicUpdate:
               // Notify about new events in merge requests that are cached already
               if (_cachedDiscussions.TryGetValue(e.MergeRequestKey, out CachedDiscussions cached)
                 && cached.PrevTimeStamp.HasValue && eventTimestamp > cached.PrevTimeStamp.Value)
               {
                  _eventNotifier.OnDiscussionEvent(e);
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
               MergeRequestKey mrk = new MergeRequestKey
               {
                  ProjectKey = e.FullMergeRequestKey.ProjectKey,
                  IId = e.FullMergeRequestKey.MergeRequest.IId
               };
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
                  MergeRequestKey closedMRK = new MergeRequestKey
                  {
                     ProjectKey = e.FullMergeRequestKey.ProjectKey,
                     IId = e.FullMergeRequestKey.MergeRequest.IId
                  };

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
         foreach (ProjectKey projectKey in _mergeRequestManager.GetProjects())
         {
            matchingFilterList.AddRange(_mergeRequestManager.GetMergeRequests(projectKey)
               .Where(x => _mergeRequestFilter.DoesMatchFilter(x))
               .Select(x => new MergeRequestKey
                  {
                     ProjectKey = projectKey,
                     IId = x.IId
                  })
               .ToList());

            nonMatchingFilterList.AddRange(_mergeRequestManager.GetMergeRequests(projectKey)
               .Where(x => !_mergeRequestFilter.DoesMatchFilter(x))
               .Select(x => new MergeRequestKey
                  {
                     ProjectKey = projectKey,
                     IId = x.IId
                  })
               .ToList());
         }
         matchingFilter = matchingFilterList;
         nonMatchingFilter = nonMatchingFilterList;
      }

      public void PostLoadWorkflow(string hostname, User user, IWorkflowContext context, IGitLabFacade facade)
      {
         Trace.TraceInformation(String.Format("[DiscussionManager] Connected to {0}", hostname));

         _currentUser = user;
         _mergeRequestManager = facade.MergeRequestManager;
         scheduleUpdate(null /* update all merge requests cached at the moment of update processing */,
            EDiscussionUpdateType.InitialSnapshot);
      }

      public void PreLoadWorkflow(string hostname,
         ILoader<IMergeRequestListLoaderListener> mergeRequestListLoader,
         ILoader<IVersionLoaderListener> versionLoader)
      {
         Trace.TraceInformation(String.Format("[DiscussionManager] Connecting to {0}", hostname));

         _reconnect = true;
         _scheduledUpdates.Clear();
         clearCache();
      }

      private IMergeRequestManager _mergeRequestManager;
      private readonly MergeRequestFilter _mergeRequestFilter;

      private readonly DiscussionLoaderNotifier _notifier = new DiscussionLoaderNotifier();
      private readonly DiscussionEventNotifier _eventNotifier = new DiscussionEventNotifier();
      private readonly DiscussionLoaderNotifierInternal _notifierInternal =
         new DiscussionLoaderNotifierInternal();
      private readonly DiscussionParser _parser;
      private readonly INotifier<IWorkflowEventListener> _workflowEventNotifier;

      private readonly System.Timers.Timer _timer;
      private readonly List<System.Timers.Timer> _oneShotTimers = new List<System.Timers.Timer>();

      private readonly DiscussionOperator _operator;
      private User _currentUser;

      private struct CachedDiscussions
      {
         public DateTime? PrevTimeStamp;
         public DateTime TimeStamp;
         public int NoteCount;
         public int ResolvableDiscussionCount;
         public int ResolvedDiscussionCount;
         public IEnumerable<Discussion> Discussions;
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
         internal IEnumerable<MergeRequestKey> MergeRequests;
         internal EDiscussionUpdateType Type;
      }

      /// <summary>
      /// Queue of updates scheduled in scheduleUpdates() method for asynchronous processing
      /// </summary>
      private readonly Queue<ScheduledUpdate> _scheduledUpdates = new Queue<ScheduledUpdate>();
   }
}

