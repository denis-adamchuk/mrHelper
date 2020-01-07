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

namespace mrHelper.Client.Discussions
{
   public class DiscussionManagerException : Exception {}

   /// <summary>
   /// Manages merge request discussions
   /// </summary>
   public class DiscussionManager : IDisposable
   {
      public event Action<MergeRequestKey> PreLoadDiscussions;
      public event Action<MergeRequestKey, IEnumerable<Discussion>, DateTime, bool> PostLoadDiscussions;
      public event Action FailedLoadDiscussions;

      public event Action<UserEvents.DiscussionEvent> DiscussionEvent;

      public DiscussionManager(IHostProperties settings, Workflow.Workflow workflow,
         MergeRequestManager mergeRequestManager, ISynchronizeInvoke synchronizeInvoke, IEnumerable<string> keywords,
         int autoUpdatePeriodMs)
      {
         _settings = settings;
         _operator = new DiscussionOperator(settings);

         DiscussionParser parser = new DiscussionParser(workflow, this, keywords);
         parser.DiscussionEvent += e => DiscussionEvent?.Invoke(e);

         workflow.PostLoadProjectMergeRequests +=
            (hostname, project, mergeRequests) =>
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

            IEnumerable<MergeRequestKey> toRemove = _cachedDiscussions.Keys.Where(
               x => x.ProjectKey.HostName == hostname
                 && x.ProjectKey.ProjectName == project.Path_With_Namespace
                 && !mergeRequestKeys.Any(y => x.IId == y.IId));
            cleanup(toRemove.ToArray());
         };

         mergeRequestManager.MergeRequestEvent +=
            (e) =>
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
                  cleanup(new MergeRequestKey[]
                  {
                     new MergeRequestKey
                     {
                        ProjectKey = e.FullMergeRequestKey.ProjectKey,
                        IId = e.FullMergeRequestKey.MergeRequest.IId
                     }
                  });
                  break;

               case UserEvents.MergeRequestEvent.Type.UpdatedMergeRequest:
                  // do nothing
                  break;

               default:
                  Debug.Assert(false);
                  break;
            }
         };

         _timer = new System.Timers.Timer { Interval = autoUpdatePeriodMs };
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = synchronizeInvoke;
         _timer.Start();
      }

      public void Dispose()
      {
         _timer.Stop();
         _timer.Dispose();
         _oneShotTimer?.Stop();
         _oneShotTimer?.Dispose();
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
         catch (OperatorException)
         {
            throw new DiscussionManagerException();
         }

         Debug.Assert(_cachedDiscussions.ContainsKey(mrk));
         return _cachedDiscussions[mrk].Discussions;
      }

      public DiscussionCreator GetDiscussionCreator(MergeRequestKey mrk)
      {
         return new DiscussionCreator(mrk, _operator);
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
         cancelOneShotTimer();

         _oneShotTimer = new System.Timers.Timer
         {
            Interval = firstChanceDelay,
            AutoReset = false,
            SynchronizingObject = _timer.SynchronizingObject
         };

         _oneShotTimer.Elapsed += (s, e) =>
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Scheduling update of discussions for a merge request with IId {0}",
               mrk.IId));

            scheduleUpdate(new MergeRequestKey[] { mrk }, false);

            if (Convert.ToInt32(_oneShotTimer.Interval) == firstChanceDelay)
            {
               _oneShotTimer.Interval = secondChanceDelay;
               _oneShotTimer.Start();
            }
         };

         _oneShotTimer.Start();
      }

      private void cancelOneShotTimer()
      {
         if (_oneShotTimer?.Enabled ?? false)
         {
            Trace.TraceInformation("[UpdateManager] One-Shot Timer cancelled");
            _oneShotTimer.Stop();
            _oneShotTimer.Dispose();
         }
      }

      private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         cancelOneShotTimer();

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
            try
            {
               foreach (MergeRequestKey mrk in keys)
               {
                  await updateDiscussionsAsync(mrk, false, initialSnapshot);
               }
            }
            catch (OperatorException)
            {
               // already handled
            }
         }), null);
      }

      async private Task updateDiscussionsAsync(MergeRequestKey mrk, bool additionalLogging, bool initialSnapshot)
      {
         GitLabClient client = new GitLabClient(mrk.ProjectKey.HostName,
            _settings.GetAccessToken(mrk.ProjectKey.HostName));
         DateTime mergeRequestUpdatedAt =
            (await CommonOperator.GetMostRecentUpdatedNoteAsync(client, mrk.ProjectKey.ProjectName, mrk.IId)).Updated_At;

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

         try
         {
            PreLoadDiscussions?.Invoke(mrk);

            _updating.Add(mrk);
            IEnumerable<Discussion> discussions = await _operator.GetDiscussionsAsync(client, mrk);

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
         catch (OperatorException)
         {
            FailedLoadDiscussions?.Invoke();
            throw;
         }
         finally
         {
            _updating.Remove(mrk);
         }
      }

      private void cleanup(IEnumerable<MergeRequestKey> keys)
      {
         foreach (MergeRequestKey mrk in keys)
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Clean up closed MR: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
            _cachedDiscussions.Remove(mrk);
            _updating.Remove(mrk);
            _closed.Add(mrk);
         }
      }

      private readonly System.Timers.Timer _timer;
      private System.Timers.Timer _oneShotTimer;

      private readonly IHostProperties _settings;
      private readonly DiscussionOperator _operator;

      private struct CachedDiscussions
      {
         public DateTime TimeStamp;
         public IEnumerable<Discussion> Discussions;
      }

      private readonly Dictionary<MergeRequestKey, CachedDiscussions> _cachedDiscussions =
         new Dictionary<MergeRequestKey, CachedDiscussions>();

      private readonly HashSet<MergeRequestKey> _updating = new HashSet<MergeRequestKey>();
      private readonly HashSet<MergeRequestKey> _closed = new HashSet<MergeRequestKey>();
   }
}

