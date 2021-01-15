using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using GitLabSharp.Entities;
using mrHelper.GitLabClient.Operators;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient.Managers
{
   /// <summary>
   /// Manages time tracking for merge requests
   /// TODO Clean up merged/closed merge requests
   /// </summary>
   internal class TimeTrackingManager :
      IDisposable,
      ITotalTimeCache
   {
      public TimeTrackingManager(
         string hostname,
         IHostProperties hostProperties,
         User user,
         IDiscussionLoader discussionLoader,
         IModificationNotifier modificationNotifier,
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         _operator = new TimeTrackingOperator(hostname, hostProperties, networkOperationStatusListener);
         _currentUser = user;
         _modificationNotifier = modificationNotifier;

         _modificationNotifier.TrackedTimeModified += onTrackedTimeModified;

         _discussionLoader = discussionLoader;
         _discussionLoader.DiscussionsLoading += preProcessDiscussions;
         _discussionLoader.DiscussionsLoaded += processDiscussions;
      }

      public event Action<ITotalTimeCache, MergeRequestKey> TotalTimeLoading;
      public event Action<ITotalTimeCache, MergeRequestKey> TotalTimeLoaded;

      public void Dispose()
      {
         _modificationNotifier.TrackedTimeModified -= onTrackedTimeModified;

         _discussionLoader.DiscussionsLoading -= preProcessDiscussions;
         _discussionLoader.DiscussionsLoaded -= processDiscussions;

         _operator.Dispose();
      }

      public TrackedTime GetTotalTime(MergeRequestKey mrk)
      {
         TimeSpan? amount = null;
         TrackedTime.EStatus status = TrackedTime.EStatus.NotAvailable;

         if (_loading.Contains(mrk))
         {
            status = TrackedTime.EStatus.Loading;
         }
         else if (_cachedTrackedTime.ContainsKey(mrk))
         {
            status = TrackedTime.EStatus.Ready;
            amount = _cachedTrackedTime[mrk];
         }

         return new TrackedTime(amount, status);
      }


      private void onTrackedTimeModified(MergeRequestKey mrk, TimeSpan span, bool add)
      {
         if (!_cachedTrackedTime.ContainsKey(mrk))
         {
            Debug.Assert(add);
            _cachedTrackedTime[mrk] = span;
         }
         else if (add)
         {
            _cachedTrackedTime[mrk] += span;
         }
         else
         {
            _cachedTrackedTime[mrk] -= span;
         }
      }

      private static readonly Regex spentTimeRe =
         new Regex(
            @"^(?'operation'added|subtracted)\s(?>(?'hours'\d*)h\s)?(?>(?'minutes'\d*)m\s)?(?>(?'seconds'\d*)s\s)?of time spent.*",
               RegexOptions.Compiled);

      private void processDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions)
      {
         _loading.Remove(mrk);

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

         _cachedTrackedTime[mrk] = span;
         TotalTimeLoaded?.Invoke(this, mrk);
      }

      private void preProcessDiscussions(MergeRequestKey mrk)
      {
         _loading.Add(mrk);
         TotalTimeLoading?.Invoke(this, mrk);
      }

      /// <summary>
      /// temporary collection to track Loading status
      /// It cannot be a single value because we load multiple MR at once
      /// </summary>
      private readonly HashSet<MergeRequestKey> _loading = new HashSet<MergeRequestKey>();

      private readonly TimeTrackingOperator _operator;
      private readonly Dictionary<MergeRequestKey, TimeSpan> _cachedTrackedTime =
         new Dictionary<MergeRequestKey, TimeSpan>();
      private readonly User _currentUser;
      private readonly IDiscussionLoader _discussionLoader;
      private readonly IModificationNotifier _modificationNotifier;
   }
}

