using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Client.Session;
using mrHelper.Client.Discussions;

namespace mrHelper.Client.TimeTracking
{
   /// <summary>
   /// Manages time tracking for merge requests
   /// TODO Clean up merged/closed merge requests
   /// </summary>
   internal class TimeTrackingManager :
      IDisposable,
      ITotalTimeCache
   {
      public TimeTrackingManager(GitLabClientContext clientContext, string hostname,
         User user, IDiscussionLoader discussionLoader)
      {
         _operator = new TimeTrackingOperator(hostname, clientContext.HostProperties);
         _currentUser = user;

         _discussionLoader = discussionLoader;
         _discussionLoader.DiscussionsLoading += preProcessDiscussions;
         _discussionLoader.DiscussionsLoaded += processDiscussions;
      }

      public event Action<ITotalTimeCache, MergeRequestKey> TotalTimeLoading;
      public event Action<ITotalTimeCache, MergeRequestKey> TotalTimeLoaded;

      public void Dispose()
      {
         _discussionLoader.DiscussionsLoading -= preProcessDiscussions;
         _discussionLoader.DiscussionsLoaded -= processDiscussions;
      }

      public TimeSpan? GetTotalTime(MergeRequestKey mrk)
      {
         return _times.ContainsKey(mrk) ? _times[mrk] : new Nullable<TimeSpan>();
      }

      async public Task AddSpan(bool add, TimeSpan span, MergeRequestKey mrk)
      {
         try
         {
            await _operator.AddSpanAsync(add, span, mrk);
         }
         catch (OperatorException ex)
         {
            throw new TimeTrackingException("Cannot add a span", ex);
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
         TotalTimeLoaded?.Invoke(this, mrk);
      }

      public void preProcessDiscussions(MergeRequestKey mrk)
      {
         // TODO TimeSpan.MinValue is a bad design decision, consider implementing States
         // by analogy with DiscussionManager.GetDiscussionCount()
         _times[mrk] = TimeSpan.MinValue;
         TotalTimeLoading?.Invoke(this, mrk);
      }

      private readonly TimeTrackingOperator _operator;
      private readonly Dictionary<MergeRequestKey, TimeSpan> _times =
         new Dictionary<MergeRequestKey, TimeSpan>();
      private readonly User _currentUser;
      private readonly IDiscussionLoader _discussionLoader;
   }
}

