using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Client.MergeRequests;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.Discussions
{
   public class DiscussionManagerException : ExceptionEx
   {
      internal DiscussionManagerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   /// <summary>
   /// Manages merge request discussions
   /// </summary>
   public class DiscussionManager : IDisposable
   {
      public event Action<MergeRequestKey> PreLoadDiscussions;
      public event Action<MergeRequestKey, IEnumerable<Discussion>, DateTime, bool> PostLoadDiscussions;
      public event Action FailedLoadDiscussions;

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

         _timer.Stop();
         _timer.Dispose();

         foreach (System.Timers.Timer timer in _oneShotTimers)
         {
            timer.Stop();
            timer.Dispose();
         }
         _oneShotTimers.Clear();
      }

      async public Task<IEnumerable<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk)
      {
         if (_updating.Contains(mrk))
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Waiting for completion of updating discussions for MR: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
         }

         while (_updating.Contains(mrk))
         {
            await Task.Delay(50);
         }

         try
         {
            await updateDiscussionsAsync(mrk, true, !_updating.Contains(mrk));
         }
         catch (OperatorException ex)
         {
            throw new DiscussionManagerException(String.Format(
               "Cannot update discussions for MR: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()), ex);
         }

         Debug.Assert(!_closed.Contains(mrk));
         Debug.Assert(_cachedDiscussions.ContainsKey(mrk));
         return _cachedDiscussions[mrk].Discussions;
      }

      public DiscussionCreator GetDiscussionCreator(MergeRequestKey mrk)
      {
         return new DiscussionCreator(mrk, _operator, _currentUser);
      }

      public DiscussionEditor GetDiscussionEditor(MergeRequestKey mrk, string discussionId)
      {
         return new DiscussionEditor(mrk, discussionId, _operator);
      }

      /// <summary>
      /// Request to update discussions of the specified MR after the specified time period (in milliseconds)
      /// </summary>
      public void CheckForUpdates(MergeRequestKey mrk, int firstChanceDelay, int secondChanceDelay)
      {
         enqueueOneShotTimer(mrk, firstChanceDelay);
         enqueueOneShotTimer(mrk, secondChanceDelay);
      }

      private void enqueueOneShotTimer(MergeRequestKey mrk, int interval)
      {
         if (interval < 1)
         {
            return;
         }

         System.Timers.Timer timer = new System.Timers.Timer
         {
            Interval = interval,
            AutoReset = false,
            SynchronizingObject = _timer.SynchronizingObject
         };

         timer.Elapsed += (s, e) =>
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Scheduling update of discussions for a merge request with IId {0}",
               mrk.IId));

            scheduleUpdate(new MergeRequestKey[] { mrk }, false);
         };
         timer.Start();

         _oneShotTimers.Add(timer);
      }

      private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         Trace.TraceInformation(String.Format(
            "[DiscussionManager] Scheduling update of discussions for {0} merge requests on a timer update",
            _cachedDiscussions.Count));

         scheduleUpdate(_cachedDiscussions.Keys.ToArray(), false);
      }

      private void scheduleUpdate(IEnumerable<MergeRequestKey> keys, bool initialSnapshot)
      {
         _timer.SynchronizingObject.BeginInvoke(new Action(
            async () =>
         {
            if (initialSnapshot && _reconnect)
            {
               Trace.TraceInformation("[DiscussionManager] _reconnect state is reset due to initial snapshot request");
               _reconnect = false;
            }
            else if (_reconnect)
            {
               Trace.TraceInformation("[DiscussionManager] update is skipped due to _reconnect state");
               return;
            }

            foreach (MergeRequestKey mrk in keys)
            {
               try
               {
                  await updateDiscussionsAsync(mrk, false, initialSnapshot);
               }
               catch (OperatorException ex)
               {
                  ExceptionHandlers.Handle(String.Format(
                     "Cannot update discussions for MR: Host={0}, Project={1}, IId={2}",
                     mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()), ex);
                  continue;
               }

               if (_reconnect)
               {
                  Trace.TraceInformation("[DiscussionManager] update loop is cancelled due to _reconnect state");
                  break;
               }
            }
         }), null);
      }

      private void scheduleCleanup()
      {
         _timer.SynchronizingObject.BeginInvoke(new Action(
            async () =>
         {
            if (_updating.Any())
            {
               Trace.TraceInformation("[DiscussionManager] Waiting for updates completion to clean up state");
            }

            while (_updating.Any())
            {
               await Task.Delay(50);
            }

            _cachedDiscussions.Clear();
            _closed.Clear();
            Trace.TraceInformation("[DiscussionManager] State cleaned up");
         }), null);
      }

      async private Task updateDiscussionsAsync(MergeRequestKey mrk, bool additionalLogging, bool initialSnapshot)
      {
         Note mostRecentNote = await _operator.GetMostRecentUpdatedNoteAsync(mrk);

         DateTime mergeRequestUpdatedAt = mostRecentNote.Updated_At;
         if (_cachedDiscussions.ContainsKey(mrk) && mergeRequestUpdatedAt <= _cachedDiscussions[mrk].TimeStamp)
         {
            if (additionalLogging)
            {
               Trace.TraceInformation(String.Format(
                  "[DiscussionManager] Discussions are up-to-date, remote time stamp {0}, cached time stamp {1}",
                  mergeRequestUpdatedAt.ToLocalTime().ToString(),
                  _cachedDiscussions[mrk].TimeStamp.ToLocalTime().ToString()));
            }
            return;
         }

         if (_closed.Contains(mrk))
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Will not update MR because it is closed: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
            _closed.Remove(mrk);
            return;
         }

         IEnumerable<Discussion> discussions;

         PreLoadDiscussions?.Invoke(mrk);
         try
         {
            _updating.Add(mrk);
            discussions = await _operator.GetDiscussionsAsync(mrk);
         }
         catch (OperatorException)
         {
            FailedLoadDiscussions?.Invoke();
            throw;
         }
         finally
         {
            _updating.Remove(mrk);
         }

         if (!_closed.Contains(mrk))
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Cached {0} discussions for MR: Host={1}, Project={2}, IId={3},"
             + " cached time stamp {4} (was {5} before update)",
               discussions.Count(), mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString(),
               mergeRequestUpdatedAt.ToLocalTime().ToString(),
               _cachedDiscussions.ContainsKey(mrk) ?
                  _cachedDiscussions[mrk].TimeStamp.ToLocalTime().ToString() : "N/A"));

            _cachedDiscussions[mrk] = new CachedDiscussions
            {
               TimeStamp = mergeRequestUpdatedAt,
               Discussions = discussions.ToArray()
            };
         }
         else
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Will not cache MR because it is closed: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
            _closed.Remove(mrk);
         }

         PostLoadDiscussions?.Invoke(mrk, discussions, mergeRequestUpdatedAt, initialSnapshot);
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
                  _updating.Remove(closedMRK);
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
         _currentUser = user;
         _reconnect = true;

         Trace.TraceInformation("[DiscussionManager] Scheduling clean-up");
         scheduleCleanup();
      }

      private readonly DiscussionParser _parser;
      private readonly MergeRequestCache _mergeRequestCache;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;

      private readonly System.Timers.Timer _timer;
      private List<System.Timers.Timer> _oneShotTimers = new List<System.Timers.Timer>();

      private readonly DiscussionOperator _operator;
      private User _currentUser;

      private struct CachedDiscussions
      {
         public DateTime TimeStamp;
         public IEnumerable<Discussion> Discussions;
      }

      private readonly Dictionary<MergeRequestKey, CachedDiscussions> _cachedDiscussions =
         new Dictionary<MergeRequestKey, CachedDiscussions>();

      /// <summary>
      /// temporary _updating collection allows to avoid re-entrance in updateDiscussionsAsync()
      /// </summary>
      private readonly HashSet<MergeRequestKey> _updating = new HashSet<MergeRequestKey>();

      /// <summary>
      /// temporary _closed collection serves to not cache what is not needed to cache
      /// </summary>
      private readonly HashSet<MergeRequestKey> _closed = new HashSet<MergeRequestKey>();

      /// <summary>
      /// Shows that reconnect is in progress, and all updates are ignored within this period
      /// </summary>
      private bool _reconnect;
   }
}

