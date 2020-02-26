using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Discussions;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.TimeTracking
{
   /// <summary>
   /// Manages time tracking for merge requests
   /// TODO Clean up merged/closed merge requests
   /// </summary>
   public class TimeTrackingManager : IDisposable
   {
      public TimeTrackingManager(IHostProperties settings, Workflow.Workflow workflow,
         DiscussionManager discussionManager)
      {
         _operator = new TimeTrackingOperator(settings);

         _workflow = workflow;
         _workflow.PostLoadCurrentUser += onPostLoadCurrentUser;

         _discussionManager = discussionManager;
         _discussionManager.PreLoadDiscussions += onPreLoadDiscussions;
         _discussionManager.PostLoadDiscussions += onPostLoadDiscussions;
         _discussionManager.FailedLoadDiscussions += onFailedLoadDiscussions;
      }

      public void Dispose()
      {
         _workflow.PostLoadCurrentUser -= onPostLoadCurrentUser;

         _discussionManager.PreLoadDiscussions -= onPreLoadDiscussions;
         _discussionManager.PostLoadDiscussions -= onPostLoadDiscussions;
         _discussionManager.FailedLoadDiscussions -= onFailedLoadDiscussions;
      }

      public event Action<MergeRequestKey> PreLoadTotalTime;
      public event Action FailedLoadTotalTime;
      public event Action<MergeRequestKey> PostLoadTotalTime;

      public TimeSpan? GetTotalTime(MergeRequestKey mrk)
      {
         return _times.ContainsKey(mrk) ? _times[mrk] : new Nullable<TimeSpan>();
      }

      async public Task AddSpanAsync(bool add, TimeSpan span, MergeRequestKey mrk)
      {
         await _operator.AddSpanAsync(add, span, mrk);
         if (!_times.ContainsKey(mrk))
         {
            Debug.Assert(add);
            _times[mrk] = span;
         }
         else if (add)
         {
            _times[mrk] += span;
         }
         else
         {
            _times[mrk] -= span;
         }
      }

      public TimeTracker GetTracker(MergeRequestKey mrk)
      {
         return new TimeTracker(mrk, async (span, key) => { await AddSpanAsync(true, span, key); });
      }

      private static readonly Regex spentTimeRe =
         new Regex(
            @"^(?'operation'added|subtracted)\s(?>(?'hours'\d*)h\s)?(?>(?'minutes'\d*)m\s)?(?>(?'seconds'\d*)s\s)?of time spent.*",
               RegexOptions.Compiled);

      private void processDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions)
      {
         TimeSpan span = TimeSpan.Zero;
         foreach (Discussion discussion in discussions)
         {
            if (!discussion.Individual_Note || discussion.Notes.Count() < 1)
            {
               continue;
            }

            DiscussionNote note = discussion.Notes.First();
            if (note.System && note.Author.Id == _currentUser.Id)
            {
               Match m = spentTimeRe.Match(note.Body);
               if (!m.Success)
               {
                  continue;
               }

               int hours = m.Groups["hours"].Success ? int.Parse(m.Groups["hours"].Value) : 0;
               int minutes = m.Groups["minutes"].Success ? int.Parse(m.Groups["minutes"].Value) : 0;
               int seconds = m.Groups["seconds"].Success ? int.Parse(m.Groups["seconds"].Value) : 0;
               if (m.Groups["operation"].Value == "added")
               {
                  span += new TimeSpan(hours, minutes, seconds);
               }
               else
               {
                  Debug.Assert(m.Groups["operation"].Value == "subtracted");
                  span -= new TimeSpan(hours, minutes, seconds);
               }
            }
         }

         _times[mrk] = span;
         PostLoadTotalTime?.Invoke(mrk);
      }

      private void onPreLoadDiscussions(MergeRequestKey mrk)
      {
         PreLoadTotalTime?.Invoke(mrk);
      }

      private void onPostLoadDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions,
         DateTime dateTime, bool b)
      {
         processDiscussions(mrk, discussions);
      }

      private void onFailedLoadDiscussions()
      {
         FailedLoadTotalTime?.Invoke();
      }

      private void onPostLoadCurrentUser(User user)
      {
         _currentUser = user;
      }

      private readonly TimeTrackingOperator _operator;
      private readonly Dictionary<MergeRequestKey, TimeSpan> _times =
         new Dictionary<MergeRequestKey, TimeSpan>();
      private User _currentUser;
      private readonly DiscussionManager _discussionManager;
      private readonly Workflow.Workflow _workflow;
   }
}

