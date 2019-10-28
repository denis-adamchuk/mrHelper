using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Discussions;
using mrHelper.Common.Exceptions;
using System.ComponentModel;
using GitLabSharp;
using mrHelper.Client.Common;
using System.Diagnostics;

namespace mrHelper.Client.Discussions
{
   public class DiscussionManagerException : Exception {}

   /// <summary>
   /// Manages merge request discussions
   /// </summary>
   public class DiscussionManager : IDisposable
   {
      public event Action PreLoadDiscussions;
      public event Action<MergeRequestKey, List<Discussion>> PostLoadDiscussions;
      public event Action FailedLoadDiscussions;

      public DiscussionManager(UserDefinedSettings settings, Workflow.Workflow workflow, ISynchronizeInvoke synchronizeInvoke)
      {
         _settings = settings;
         _operator = new DiscussionOperator(settings);
         workflow.PostLoadProjectMergeRequests +=
            (hostname, project, mergeRequests) =>
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Scheduling update of discussions for {0} merge requests on Workflow event",
               mergeRequests.Count));

            _synchronizeInvoke.BeginInvoke(new Action(
               async () =>
               {
                  try
                  {
                     foreach (MergeRequest mergeRequest in mergeRequests)
                     {
                        MergeRequestKey mrk = new MergeRequestKey
                        {
                           ProjectKey = new ProjectKey { HostName = hostname, ProjectName = project.Path_With_Namespace },
                           IId = mergeRequest.IId
                        };
                        await updateDiscussionsAsync(mrk);
                     }
                  }
                  catch (OperatorException)
                  {
                     // already handled
                  }
               }), null);
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
         if (Updating.Contains(mrk))
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Waiting for completion of updating discussions for MR: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));
         }

         while (Updating.Contains(mrk))
         {
            await Task.Delay(50);
         }

         try
         {
            await updateDiscussionsAsync(mrk);
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
            "[DiscussionManager] Scheduling update of discussions for {0} merge requests on-timer",
            _cachedDiscussions.Count));

         _synchronizeInvoke.BeginInvoke(new Action(
            async () =>
            {
               try
               {
                  foreach (MergeRequestKey mrk in _cachedDiscussions.Keys)
                  {
                     await updateDiscussionsAsync(mrk);
                  }
               }
               catch (OperatorException)
               {
                  // already handled
               }
            }), null);
      }

      async private Task updateDiscussionsAsync(MergeRequestKey mrk)
      {
         GitLabClient client =
            new GitLabClient(mrk.ProjectKey.HostName, _settings.GetAccessToken(mrk.ProjectKey.HostName));
         DateTime mergeRequestUpdatedAt =
            (await CommonOperator.GetMergeRequestAsync(client, mrk.ProjectKey.ProjectName, mrk.IId)).Updated_At;

         if (_cachedDiscussions.ContainsKey(mrk) && mergeRequestUpdatedAt <= _cachedDiscussions[mrk].TimeStamp)
         {
            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Discussions are up-to-date, remote time stamp {0}, cached time stamp {1}",
               mergeRequestUpdatedAt.ToLocalTime().ToString(),
               _cachedDiscussions[mrk].TimeStamp.ToLocalTime().ToString()));
            return;
         }

         try
         {
            PreLoadDiscussions?.Invoke();

            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Caching discussions for MR: Host={0}, Project={1}, IId={2}",
               mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString()));

            Updating.Add(mrk);
            List<Discussion> discussions = await _operator.GetDiscussionsAsync(client, mrk);

            Trace.TraceInformation(String.Format(
               "[DiscussionManager] Cached {0} discussions for MR: Host={1}, Project={2}, IId={3}, cached time stamp {4}",
               discussions.Count, mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId.ToString(),
               mergeRequestUpdatedAt.ToLocalTime().ToString()));

            _cachedDiscussions[mrk] = new CachedDiscussions
            {
               TimeStamp = mergeRequestUpdatedAt,
               Discussions = discussions
            };

            PostLoadDiscussions?.Invoke(mrk, _cachedDiscussions[mrk].Discussions);
         }
         catch (OperatorException)
         {
            FailedLoadDiscussions?.Invoke();
            throw;
         }
         finally
         {
            Updating.Remove(mrk);
         }
      }

      private System.Timers.Timer _timer = new System.Timers.Timer
      {
         Interval = 5 * 60000 // five minutes in ms
      };

      private ISynchronizeInvoke _synchronizeInvoke;
      private readonly UserDefinedSettings _settings;
      private readonly DiscussionOperator _operator;

      private struct CachedDiscussions
      {
         public DateTime TimeStamp;
         public List<Discussion> Discussions;
      }

      private Dictionary<MergeRequestKey, CachedDiscussions> _cachedDiscussions =
         new Dictionary<MergeRequestKey, CachedDiscussions>();

      private HashSet<MergeRequestKey> Updating = new HashSet<MergeRequestKey>();
   }
}

