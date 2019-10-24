using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.TimeTracking
{
   /// <summary>
   /// Manages time tracking for merge requests
   /// </summary>
   public class TimeTrackingManager
   {
      public TimeTrackingManager(UserDefinedSettings settings, Workflow.Workflow workflow)
      {
         Settings = settings;
         TimeTrackingOperator = new TimeTrackingOperator(Settings);
         workflow.PostLoadCurrentUser += (user) => _currentUser = user;
         workflow.PreLoadDiscussions += () => PreLoadTotalTime?.Invoke();
         workflow.PostLoadDiscussions += (mrk, discussions) => processDiscussions(mrk, discussions);
      }

      public event Action PreLoadTotalTime;
      public event Action<MergeRequestKey> PostLoadTotalTime;

      public TimeSpan GetTotalTime(MergeRequestKey mrk)
      {
         return MergeRequestTimes.ContainsKey(mrk) ? MergeRequestTimes[mrk] : default(TimeSpan);
      }

      async public Task AddSpanAsync(bool add, TimeSpan span, MergeRequestKey mrk)
      {
         await TimeTrackingOperator.AddSpanAsync(add, span, mrk);
         if (add)
         {
            MergeRequestTimes[mrk] += span;
         }
         else
         {
            MergeRequestTimes[mrk] -= span;
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

      private void processDiscussions(MergeRequestKey mrk, List<Discussion> discussions)
      {
         TimeSpan span = TimeSpan.Zero;
         foreach (Discussion discussion in discussions)
         {
            if (!discussion.Individual_Note || discussions.Notes.Count < 1)
            {
               continue;
            }

            Note note = discussions.Notes[0];
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

         MergeRequestTimes[mrk] = span;
         PostLoadTotalTime?.Invoke(mrk);
      }

      private UserDefinedSettings Settings { get; }
      private TimeTrackingOperator TimeTrackingOperator { get; }
      private Dictionary<MergeRequestKey, TimeSpan> MergeRequestTimes { get; } =
         new Dictionary<MergeRequestKey, TimeSpan>();
      private User _currentUser;
   }
}

