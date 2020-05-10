using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Common;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.TimeTracking
{
   /// <summary>
   /// Manages time tracking for merge requests
   /// TODO Clean up merged/closed merge requests
   /// </summary>
   internal class TimeTrackingManager :
      IDisposable,
      IWorkflowEventListener,
      IDiscussionLoaderListener,
      ITimeTrackingManager
   {
      public TimeTrackingManager(GitLabClientContext clientContext, IWorkflowLoader workflowLoader)
      {
         _operator = new TimeTrackingOperator(clientContext.HostProperties);

         _workflowEventNotifier = workflowLoader.GetNotifier();
         _workflowEventNotifier.AddListener(this);
      }

      public void Dispose()
      {
         _workflowEventNotifier.RemoveListener(this);

         _discussionLoaderNotifier?.RemoveListener(this);
      }

      public INotifier<ITimeTrackingLoaderListener> GetNotifier() => _notifier;

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
         _notifier.OnPostLoadTotalTime(mrk, span);
      }

      public void OnPreLoadDiscussions(MergeRequestKey mrk)
      {
         // TODO TimeSpan.MinValue is a bad design decision, consider implementing States
         // by analogy with DiscussionManager.GetDiscussionCount()
         _times[mrk] = TimeSpan.MinValue;
         _notifier.OnPreLoadTotalTime(mrk);
      }

      public void OnPostLoadDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions)
      {
         processDiscussions(mrk, discussions);
      }

      public void OnFailedLoadDiscussions(MergeRequestKey mrk)
      {
         if (_times.ContainsKey(mrk))
         {
            _times.Remove(mrk);
         }
         _notifier.OnFailedLoadTotalTime(mrk);
      }

      public void PreLoadWorkflow(string hostname,
         ILoader<IMergeRequestListLoaderListener> mergeRequestListLoaderListener,
         ILoader<IVersionLoaderListener> versionLoaderListener)
      {
         _times.Clear();

         _discussionLoaderNotifier.RemoveListener(this);
         _discussionLoaderNotifier = null;
      }

      public void PostLoadWorkflow(string hostname, User user, IWorkflowContext context, IGitLabFacade facade)
      {
         _currentUser = user;

         _discussionLoaderNotifier = (facade.DiscussionManager as ILoader<IDiscussionLoaderListener>).GetNotifier();
         _discussionLoaderNotifier.AddListener(this);
      }

      private readonly TimeTrackingOperator _operator;
      private readonly Dictionary<MergeRequestKey, TimeSpan> _times =
         new Dictionary<MergeRequestKey, TimeSpan>();
      private User _currentUser;
      private INotifier<IDiscussionLoaderListener> _discussionLoaderNotifier;
      private readonly INotifier<IWorkflowEventListener> _workflowEventNotifier;
      private readonly TimeTrackingLoaderNotifier _notifier = new TimeTrackingLoaderNotifier();
   }
}

