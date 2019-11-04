using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Client.MergeRequests;

namespace mrHelper.Client.Discussions
{
   public class DiscussionManagerException : Exception {}

   /// <summary>
   /// Manages merge request discussions
   /// </summary>
   public class DiscussionManager : IDisposable
   {
      public event Action PreLoadDiscussions;
      public event Action<MergeRequestKey, List<Discussion>, DateTime, bool> PostLoadDiscussions;
      public event Action FailedLoadDiscussions;

      public event Action<UserEvents.DiscussionEvent> OnEvent;

      public DiscussionManager(UserDefinedSettings settings, Workflow.Workflow workflow,
         MergeRequestManager mergeRequestManager, ISynchronizeInvoke synchronizeInvoke, IEnumerable<string> keywords)
      {
         _settings = settings;
         _operator = new DiscussionOperator(settings);
         _parser = new DiscussionParser(workflow, this, keywords, e => OnEvent?.Invoke(e));

         workflow.PostLoadProjectMergeRequests +=
            (hostname, project, mergeRequests) =>
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Scheduling update of discussions for {0} merge requests of {1} on Workflow event",
               mergeRequests.Count, project.Path_With_Namespace));

            IEnumerable<MergeRequestKey> keys = mergeRequests
               .Select(x => new MergeRequestKey
               {
                  ProjectKey = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace },
                  IId = x.IId
               });

            scheduleUpdate(keys, true);

            MergeRequestKey[] toRemove = _cachedDiscussions.Keys.Where(
               x => x.ProjectKey.HostName == hostname
                 && x.ProjectKey.ProjectName == project.Path_With_Namespace
                 && !mergeRequests.Any(y => x.IId == y.IId)).ToArray();
            cleanup(toRemove);
         };

         mergeRequestManager.OnEvent +=
            (e) =>
         {
            switch (e.EventType)
            {
               case UserEvents.MergeRequestEvent.Type.NewMergeRequest:
                  Trace.TraceInformation(String.Format(
                     "[DiscussionManager] Scheduling update of discussions for a new merge request with IId",
                     e.FullMergeRequestKey.MergeRequest.IId));
                  scheduleUpdate(new List<MergeRequestKey>
                  {
                     new MergeRequestKey
                     {
                        ProjectKey = e.FullMergeRequestKey.ProjectKey,
                        IId = e.FullMergeRequestKey.MergeRequest.IId
                     }
                  }, false);
                  break;

               case UserEvents.MergeRequestEvent.Type.ClosedMergeRequest:
                  cleanup(new List<MergeRequestKey>
                  {
                     new MergeRequestKey
                     {
                        ProjectKey = e.FullMergeRequestKey.ProjectKey,
                        IId = e.FullMergeRequestKey.MergeRequest.IId
                     }
                  });
                  break;
            }
         };

         _synchronizeInvoke = synchronizeInvoke;
         _timer.Elapsed += onTimer;
         _timer.SynchronizingObject = synchronizeInvoke;
         _timer.Start();
      }

      public void Dispose()
      {
         _timer.Dispose();
      }

      async public Task<List<Discussion>> GetDiscussionsAsync(MergeRequestKey mrk)
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
            await updateDiscussionsAsync(mrk, true, false);
         }
         catch (OperatorException)
         {
            throw new DiscussionManagerException();
         }

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

      private void onTimer(object sender, System.Timers.ElapsedEventArgs e)
      {
         Trace.TraceInformation(String.Format(
            "[DiscussionManager] Scheduling update of discussions for {0} merge requests on a timer update",
            _cachedDiscussions.Count));

         MergeRequestKey[] cachedKeys = new MergeRequestKey[_cachedDiscussions.Keys.Count];
         _cachedDiscussions.Keys.CopyTo(cachedKeys, 0);
         scheduleUpdate(cachedKeys, false);
      }

      private void scheduleUpdate(IEnumerable<MergeRequestKey> keys, bool initialSnapshot)
      {
         _synchronizeInvoke.BeginInvoke(new Action(
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
         GitLabClient client =
            new GitLabClient(mrk.ProjectKey.HostName,
            ConfigurationHelper.GetAccessToken(mrk.ProjectKey.HostName, _settings));
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

         if (!initialSnapshot && !_cachedDiscussions.ContainsKey(mrk))
         {
            return;
         }

         try
         {
            PreLoadDiscussions?.Invoke();

            _updating.Add(mrk);
            List<Discussion> discussions = await _operator.GetDiscussionsAsync(client, mrk);

            if (_updating.Contains(mrk))
            {
               Trace.TraceInformation(String.Format(
                  "[DiscussionManager] Cached {0} discussions for MR: Host={1}, Project={2}, IId={3},"
                + " cached time stamp {4} (was {5} before update)",
                  discussions.Count, mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString(),
                  mergeRequestUpdatedAt.ToLocalTime().ToString(),
                  _cachedDiscussions.ContainsKey(mrk) ?
                     _cachedDiscussions[mrk].TimeStamp.ToLocalTime().ToString() : "N/A"));

               _cachedDiscussions[mrk] = new CachedDiscussions
               {
                  TimeStamp = mergeRequestUpdatedAt,
                  Discussions = discussions
               };
            }
            else
            {
               // Seems that MR was closed while loading discussions, don't cache it
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
         }
      }

      private System.Timers.Timer _timer = new System.Timers.Timer
      {
         Interval = 5 * 60000 // five minutes in ms
      };

      private ISynchronizeInvoke _synchronizeInvoke;
      private readonly UserDefinedSettings _settings;
      private readonly DiscussionOperator _operator;
      private readonly DiscussionParser _parser;

      private struct CachedDiscussions
      {
         public DateTime TimeStamp;
         public List<Discussion> Discussions;
      }

      private Dictionary<MergeRequestKey, CachedDiscussions> _cachedDiscussions =
         new Dictionary<MergeRequestKey, CachedDiscussions>();

      private HashSet<MergeRequestKey> _updating = new HashSet<MergeRequestKey>();
   }
}

