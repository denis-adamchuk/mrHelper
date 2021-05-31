using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using GitLabSharp.Entities;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Traces git diff statistic change for all merge requests within one or more repositories
   /// </summary>
   internal class DiscussionBasedDiffStatProvider : IDisposable, IDiffStatisticProvider
   {
      internal DiscussionBasedDiffStatProvider(IDiscussionLoader discussionLoader)
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

      /// <summary>
      /// Returns statistic for the given MR
      /// Statistic is collected for hash tags that match the last version of a merge request
      /// </summary>
      public DiffStatistic? GetStatistic(MergeRequestKey mrk, out string statusMessage)
      {
         statusMessage = getStatusMessage(mrk);
         return String.IsNullOrWhiteSpace(statusMessage)
            ? _statistic[mrk].Value.Statistic
            : new DiffStatistic?();
      }

      private string getStatusMessage(MergeRequestKey mrk)
      {
         if (!_statistic.ContainsKey(mrk))
         {
            return "N/A";
         }
         else if (!_statistic[mrk].HasValue)
         {
            return "Checking...";
         }
         else if (!_statistic[mrk].Value.Statistic.HasValue)
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
              && (x.Notes.First().Body.Contains("<summary>Code change summary</summary>")
               || x.Notes.First().Body.Contains("<summary><b>Code change summary</b></summary>")));
         if (discussion == null)
         {
            _statistic.Remove(mrk);
            return;
         }

         DiffStatistic? statistic = parseGitDiffStatistic(discussion.Notes.First().Body.Replace("\n", ""), mrk);
         _statistic[mrk] = new MergeRequestStatistic(statistic);
         Update?.Invoke();
      }

      public void preProcessDiscussions(MergeRequestKey mrk)
      {
         _statistic[mrk] = null;
         Update?.Invoke();
      }

      private static readonly Regex gitDiffStatRe =
         new Regex(
            @"\*\*Files\((?'files'\d+)\)\*\*.*\"
          + @"|\*\*(?'add'\d+)\*\*(?>\(manually\))?\"
          + @"|\*\*(?'mod'\d+)\*\*(?>\(manually\))?\"
          + @"|\*\*(?'del'\d+)\*\*(?>\(manually\))?\|",
               RegexOptions.Compiled);

      private DiffStatistic? parseGitDiffStatistic(string discussionBody, MergeRequestKey mrk)
      {
         Debug.Assert(!String.IsNullOrWhiteSpace(discussionBody));

         void traceError(string text)
         {
            Trace.TraceError(String.Format(
               "[DiscussionBasedSizeCollector] Cannot parse discussion note body\n\"{0}\"\n"
             + "This makes impossible to show size of MR with IID {1} in project {2}",
                text, mrk.IId, mrk.ProjectKey.ProjectName));
         }

         int parseOrZero(string x) => int.TryParse(x, out int result) ? result : 0;

         Match m = gitDiffStatRe.Match(discussionBody);
         if (!m.Success
          || !m.Groups["files"].Success
          || !m.Groups["add"].Success
          || !m.Groups["mod"].Success
          || !m.Groups["del"].Success)
         {
            traceError(discussionBody);
            return null;
         }

         int added = parseOrZero(m.Groups["add"].Value);
         int modified = parseOrZero(m.Groups["mod"].Value);

         return new DiffStatistic(parseOrZero(m.Groups["files"].Value),
            added + modified, parseOrZero(m.Groups["del"].Value));
      }

      private struct MergeRequestStatistic
      {
         public MergeRequestStatistic(DiffStatistic? statistic)
         {
            Statistic = statistic;
         }

         internal DiffStatistic? Statistic { get; }
      }

      private readonly Dictionary<MergeRequestKey, MergeRequestStatistic?> _statistic =
         new Dictionary<MergeRequestKey, MergeRequestStatistic?>();
      private IDiscussionLoader _discussionLoader;
   }
}

