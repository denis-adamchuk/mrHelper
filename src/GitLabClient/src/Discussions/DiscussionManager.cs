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
   public class DiscussionManagerException : ExceptionEx
   {
      internal DiscussionManagerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   /// TODO This class was not designed well. Consider splitting it into:
   /// - Manager
   /// - Storage
   /// - State
   /// - Updater/Loader
   /// by analogy with MergeRequestCached and its internals
   /// <summary>
   /// Manages merge request discussions
   /// </summary>
   public class DiscussionManager : IDisposable
   {
      public event Action<MergeRequestKey> PreLoadDiscussions;
      public event Action<MergeRequestKey, IEnumerable<Discussion>, DateTime, bool> PostLoadDiscussions;
      public event Action<MergeRequestKey> FailedLoadDiscussions;

      public event Action<UserEvents.DiscussionEvent> DiscussionEvent;

      public DiscussionManager(IHostProperties settings, IWorkflowEventNotifier workflowEventNotifier,
         MergeRequestCache mergeRequestCache, ISynchronizeInvoke synchronizeInvoke, IEnumerable<string> keywords,
         int autoUpdatePeriodMs)
      {
         _operator = new DiscussionOperator(settings);

         _parser = new DiscussionParser(workflowEventNotifier, this, keywords);
         _parser.DiscussionEvent += onDiscussionParserEvent;

         _mergeRequestCache = mergeRequestCache;
         _mergeRequestCache.MergeRequestEvent += onMergeRequestEvent;

         _workflowEventNotifier = workflowEventNotifier;
         _workflowEventNotifier.Connected += onConnected;
         _workflowEventNotifier.LoadedMergeRequests += onLoadedMergeRequests;

         _timer = new System.Timers.Timer { Interval = autoUpdatePeriodMs };
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = synchronizeInvoke;
         _timer.Start();
      }

      public void Dispose()
      {
         _workflowEventNotifier.Connected -= onConnected;
         _workflowEventNotifier.LoadedMergeRequests -= onLoadedMergeRequests;

         _mergeRequestCache.MergeRequestEvent -= onMergeRequestEvent;

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

      public struct DiscussionCount
      {
         public enum EStatus
         {
            NotAvailable,
            Loading,
            Ready
         }

         public int? Resolvable;
         public int? Resolved;
         public EStatus Status;
      }

      public DiscussionCount GetDiscussionCount(MergeRequestKey mrk)
      {
         int? resolvable = null;
         int? resolved = null;
         DiscussionCount.EStatus status = DiscussionCount.EStatus.NotAvailable;

         if (_loading.HasValue && _loading.Value.Equals(mrk))
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
            await updateDiscussionsAsync(mrk, true, false);
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
               scheduleUpdate(new MergeRequestKey[] { mrk.Value }, false);
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

         scheduleUpdate(null /* update all cached at the moment of update processing */, false);
      }

      async private Task processScheduledUpdate(ScheduledUpdate scheduledUpdate)
      {
         if (scheduledUpdate.MergeRequests == null)
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Processing scheduled update of discussions for {0} merge requests (ALL)",
               _cachedDiscussions.Keys.Count()));
         }
         else
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Processing scheduled update of discussions for {0} merge requests",
               scheduledUpdate.MergeRequests.Count()));
         }

         IEnumerable<MergeRequestKey> mergeRequests = scheduledUpdate.MergeRequests == null ?
            _cachedDiscussions.Keys.ToArray() : scheduledUpdate.MergeRequests;

         async Task updateDiscussions(MergeRequestKey mrk)
         {
            if (_reconnect)
            {
               return;
            }

            try
            {
               await updateDiscussionsAsync(mrk, false, scheduledUpdate.InitialSnapshot);
            }
            catch (OperatorException ex)
            {
               ExceptionHandlers.Handle(String.Format(
                  "Cannot update discussions for MR: Host={0}, Project={1}, IId={2}",
                  mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()), ex);
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(mergeRequests, x => updateDiscussions(x),
            Constants.MergeRequestsInBatch, Constants.MergeRequestsInterBatchDelay, () => _reconnect);

         if (_reconnect)
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] update loop is cancelled due to _reconnect state"));
         }
      }

      private void scheduleUpdate(IEnumerable<MergeRequestKey> keys, bool initialSnapshot)
      {
         _scheduledUpdates.Enqueue(new ScheduledUpdate
         {
            MergeRequests = keys?.ToArray(), // make a copy just in case
            InitialSnapshot = initialSnapshot
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
                  if (!scheduledUpdate.InitialSnapshot)
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

      async private Task updateDiscussionsAsync(MergeRequestKey mrk, bool additionalLogging, bool initialSnapshot)
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
            if (_reconnect || !needToLoadDiscussions(mostRecentNote, mrk, noteCount, additionalLogging))
            {
               return;
            }

            IEnumerable<Discussion> discussions;
            try
            {
               _loading = mrk;
               PreLoadDiscussions?.Invoke(mrk);
               discussions = await _operator.GetDiscussionsAsync(mrk);
            }
            catch (OperatorException)
            {
               FailedLoadDiscussions?.Invoke(mrk);
               throw;
            }
            finally
            {
               _loading = null;
            }

            if (!_reconnect)
            {
               cacheDiscussions(mrk, noteCount, mostRecentNote.Updated_At, discussions);
            }
            PostLoadDiscussions?.Invoke(mrk, discussions, mostRecentNote.Updated_At, initialSnapshot);
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

      private bool needToLoadDiscussions(Note mostRecentNote, MergeRequestKey mrk,
         int noteCount, bool additionalLogging)
      {
         DateTime mergeRequestUpdatedAt = mostRecentNote.Updated_At;
         if (_cachedDiscussions.ContainsKey(mrk)
          && mergeRequestUpdatedAt <= _cachedDiscussions[mrk].TimeStamp
          && noteCount == _cachedDiscussions[mrk].NoteCount)
         {
            if (additionalLogging)
            {
               Trace.TraceInformation(String.Format(
                  "[DiscussionManager] Discussions are up-to-date, "
                + "remote time stamp {0}, cached time stamp {1}, note count {2}, resolved {3}, resolvable {4}",
                  mergeRequestUpdatedAt.ToLocalTime().ToString(),
                  _cachedDiscussions[mrk].TimeStamp.ToLocalTime().ToString(),
                  noteCount,
                  _cachedDiscussions[mrk].ResolvedDiscussionCount, _cachedDiscussions[mrk].ResolvableDiscussionCount));
            }
            return false;
         }

         if (_closed.Contains(mrk))
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Will not update MR because it is closed: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
            _closed.Remove(mrk);
            return false;
         }

         return true;
      }

      private void cacheDiscussions(MergeRequestKey mrk, int noteCount, DateTime mergeRequestUpdatedAt,
         IEnumerable<Discussion> discussions)
      {
         if (!_closed.Contains(mrk))
         {
            calcDiscussionCount(discussions, out int resolvableDiscussionCount, out int resolvedDiscussionCount);

            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Cached {0} discussions for MR: Host={1}, Project={2}, IId={3},"
             + " cached time stamp {4} (was {5} before update), note count = {6}, resolved = {7}, resolvable = {8}",
               discussions.Count(), mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString(),
               mergeRequestUpdatedAt.ToLocalTime().ToString(),
               _cachedDiscussions.ContainsKey(mrk) ?
                  _cachedDiscussions[mrk].TimeStamp.ToLocalTime().ToString() : "N/A",
               noteCount, resolvedDiscussionCount, resolvableDiscussionCount));

            _cachedDiscussions[mrk] = new CachedDiscussions
            {
               TimeStamp = mergeRequestUpdatedAt,
               NoteCount = noteCount,
               Discussions = discussions.ToArray(),
               ResolvableDiscussionCount = resolvableDiscussionCount,
               ResolvedDiscussionCount = resolvedDiscussionCount
            };
         }
         else
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Will not cache MR because it is closed: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
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

      private void onDiscussionParserEvent(UserEvents.DiscussionEvent e)
      {
         DiscussionEvent?.Invoke(e);
      }

      private void onMergeRequestEvent(Common.UserEvents.MergeRequestEvent e)
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
               scheduleUpdate(new MergeRequestKey[] { mrk }, false);
               break;

            case UserEvents.MergeRequestEvent.Type.ClosedMergeRequest:
               {
                  MergeRequestKey closedMRK = new MergeRequestKey
                  {
                     ProjectKey = e.FullMergeRequestKey.ProjectKey,
                     IId = e.FullMergeRequestKey.MergeRequest.IId
                  };

                  Trace.TraceInformation(String.Format(
                     "[DiscussionManager] Clean up closed MR: Host={0}, Project={1}, IId={2}",
                     closedMRK.ProjectKey.HostName, closedMRK.ProjectKey.ProjectName, closedMRK.IId.ToString()));
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

      private void onLoadedMergeRequests(string hostname, Project project,
         IEnumerable<MergeRequest> mergeRequests)
      {
         Trace.TraceInformation(String.Format(
            "[DiscussionManager] Scheduling update of discussions for {0} merge requests of {1} on Workflow event",
            mergeRequests.Count(), project.Path_With_Namespace));

         IEnumerable<MergeRequestKey> mergeRequestKeys = mergeRequests
            .Select(x => new MergeRequestKey
            {
               ProjectKey = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace },
               IId = x.IId
            });

         scheduleUpdate(mergeRequestKeys.ToArray(), true);
      }

      private void onConnected(string hostname, User user, IEnumerable<Project> projects)
      {
         Trace.TraceInformation(String.Format("[DiscussionManager] Connected to {0}", hostname));

         _currentUser = user;
         _reconnect = true;
         _scheduledUpdates.Clear();
         clearCache();
      }

      private readonly DiscussionParser _parser;
      private readonly MergeRequestCache _mergeRequestCache;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;

      private System.Timers.Timer _timer;
      private List<System.Timers.Timer> _oneShotTimers = new List<System.Timers.Timer>();

      private readonly DiscussionOperator _operator;
      private User _currentUser;

      private struct CachedDiscussions
      {
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
      /// </summary>
      private readonly HashSet<MergeRequestKey> _updating = new HashSet<MergeRequestKey>();

      /// <summary>
      /// temporary key to track Loading status
      /// </summary>
      private MergeRequestKey? _loading;

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
         internal bool InitialSnapshot;
      }

      /// <summary>
      /// Queue of updates scheduled in scheduleUpdates() method for asynchronous processing
      /// </summary>
      private readonly Queue<ScheduledUpdate> _scheduledUpdates = new Queue<ScheduledUpdate>();
   }
}

