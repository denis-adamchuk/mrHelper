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
using mrHelper.Common.Exceptions;
using mrHelper.Client.Common;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.TimeTracking
{
   public class TimeTrackingManagerException : ExceptionEx
   {
      internal TimeTrackingManagerException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   /// <summary>
   /// Manages time tracking for merge requests
   /// TODO Clean up merged/closed merge requests
   /// </summary>
   public class TimeTrackingManager : IDisposable
   {
      public TimeTrackingManager(IHostProperties settings, IWorkflowEventNotifier workflowEventNotifier,
         DiscussionManager discussionManager)
      {
         _operator = new TimeTrackingOperator(settings);

         _workflowEventNotifier = workflowEventNotifier;
         _workflowEventNotifier.Connected += onConnected;

         _discussionManager = discussionManager;
         _discussionManager.PreLoadDiscussions += onPreLoadDiscussions;
         _discussionManager.PostLoadDiscussions += onPostLoadDiscussions;
         _discussionManager.FailedLoadDiscussions += onFailedLoadDiscussions;
      }

      public void Dispose()
      {
         _workflowEventNotifier.Connected -= onConnected;

         _discussionManager.PreLoadDiscussions -= onPreLoadDiscussions;
         _discussionManager.PostLoadDiscussions -= onPostLoadDiscussions;
         _discussionManager.FailedLoadDiscussions -= onFailedLoadDiscussions;
      }

      public event Action<MergeRequestKey> PreLoadTotalTime;
      public event Action<MergeRequestKey> FailedLoadTotalTime;
      public event Action<MergeRequestKey> PostLoadTotalTime;

      public TimeSpan? GetTotalTime(MergeRequestKey mrk)
      {
         return _times.ContainsKey(mrk) ? _times[mrk] : new Nullable<TimeSpan>();
      }

      async public Task AddSpanAsync(bool add, TimeSpan span, MergeRequestKey mrk)
      {
         try
         {
            await _operator.AddSpanAsync(add, span, mrk);
         }
         catch (OperatorException ex)
         {
            throw new TimeTrackingManagerException("Cannot add a span", ex);
         }

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
         return new TimeTracker(mrk, this);
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
         // TODO TimeSpan.MinValue is a bad design decision, consider implementing States
         // by analogy with DiscussionManager.GetDiscussionCount()
         _times[mrk] = TimeSpan.MinValue;
         PreLoadTotalTime?.Invoke(mrk);
      }

      private void onPostLoadDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions)
      {
         processDiscussions(mrk, discussions);
      }

      private void onFailedLoadDiscussions(MergeRequestKey mrk)
      {
         if (_times.ContainsKey(mrk))
         {
            _times.Remove(mrk);
         }
         FailedLoadTotalTime?.Invoke(mrk);
      }

      private void onConnected(string hostname, User user, IEnumerable<Project> projects)
      {
         _currentUser = user;
         _times.Clear();
      }

      private readonly TimeTrackingOperator _operator;
      private readonly Dictionary<MergeRequestKey, TimeSpan> _times =
         new Dictionary<MergeRequestKey, TimeSpan>();
      private User _currentUser;
      private readonly DiscussionManager _discussionManager;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;
   }
}

