using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text.RegularExpressions;
using GitLabSharp.Entities;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers
{
   internal class DiscussionBasedPlannedTimeProvider : IDisposable
   {
      internal DiscussionBasedPlannedTimeProvider(IDiscussionLoader discussionLoader)
      {
         _discussionLoader = discussionLoader;
         _discussionLoader.DiscussionsLoading += preProcessDiscussions;
         _discussionLoader.DiscussionsLoaded += processDiscussions;
      }

      public event Action Update;

      public void Dispose()
      {
         if (_discussionLoader != null)
         {
            _discussionLoader.DiscussionsLoading -= preProcessDiscussions;
            _discussionLoader.DiscussionsLoaded -= processDiscussions;
            _discussionLoader = null;
         }
      }

      public TimeSpan? GetPlannedTime(MergeRequestKey mrk, out string statusMessage)
      {
         statusMessage = getStatusMessage(mrk);
         return String.IsNullOrWhiteSpace(statusMessage)
            ? _plannedTimed[mrk].Value.Statistic
            : new TimeSpan?();
      }

      private string getStatusMessage(MergeRequestKey mrk)
      {
         if (!_plannedTimed.ContainsKey(mrk))
         {
            return "N/A";
         }
         else if (!_plannedTimed[mrk].HasValue)
         {
            return "Checking...";
         }
         else if (!_plannedTimed[mrk].Value.Statistic.HasValue)
         {
            return "Error";
         }
         return String.Empty;
      }

      private void processDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions)
      {
         Discussion discussion = discussions
            .Reverse()
            .FirstOrDefault(x => x.Individual_Note && x.Notes != null && x.Notes.Any()
              && x.Notes.First().Author.Username == Program.ServiceManager.GetServiceMessageUsername()
              && (x.Notes.First().Body.Contains("Planned time (minutes)")));
         if (discussion == null)
         {
            _plannedTimed.Remove(mrk);
            return;
         }

         TimeSpan? plannedTime = parsePlannedTime(discussion.Notes.First().Body.Replace("\n", ""), mrk);
         _plannedTimed[mrk] = new MergeRequestTimeSpan(plannedTime);
         Update?.Invoke();
      }

      public void preProcessDiscussions(MergeRequestKey mrk)
      {
         _plannedTimed[mrk] = null;
         Update?.Invoke();
      }

      private static readonly Regex plannedTimeRe =
         new Regex(
            @"\|Planned time \(minutes\)\:\|(?'minutes'\d+)",
               RegexOptions.Compiled);

      private TimeSpan? parsePlannedTime(string discussionBody, MergeRequestKey mrk)
      {
         Debug.Assert(!String.IsNullOrWhiteSpace(discussionBody));

         void traceError(string text)
         {
            Trace.TraceError(String.Format(
               "[DiscussionBasedSizeCollector] Cannot parse discussion note body\n\"{0}\"\n"
             + "This makes impossible to show planned time of MR with IID {1} in project {2}",
                text, mrk.IId, mrk.ProjectKey.ProjectName));
         }

         int parseOrZero(string x) => int.TryParse(x, out int result) ? result : 0;

         Match m = plannedTimeRe.Match(discussionBody);
         if (!m.Success || !m.Groups["minutes"].Success)
         {
            traceError(discussionBody);
            return null;
         }

         int minutes = parseOrZero(m.Groups["minutes"].Value);
         return TimeSpan.FromMinutes(minutes);
      }

      private struct MergeRequestTimeSpan
      {
         public MergeRequestTimeSpan(TimeSpan? plannedTime)
         {
            Statistic = plannedTime;
         }

         internal TimeSpan? Statistic { get; }
      }

      private readonly Dictionary<MergeRequestKey, MergeRequestTimeSpan?> _plannedTimed =
         new Dictionary<MergeRequestKey, MergeRequestTimeSpan?>();
      private IDiscussionLoader _discussionLoader;
   }
}

