using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Common.Tools;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.Discussions
{
   /// <summary>
   /// Parses discussion threads and notifies about some events found in them
   /// TODO Clean up merged/closed merge requests
   /// </summary>
   internal class DiscussionParser : IDisposable
   {
      internal DiscussionParser(
         IWorkflowEventNotifier workflowEventNotifier,
         IDiscussionLoaderInternal discussionLoader,
         IEnumerable<string> keywords)
      {
         _keywords = keywords;

         _workflowEventNotifier = workflowEventNotifier;
         _workflowEventNotifier.Connecting += onConnecting;
         _workflowEventNotifier.Connected += onConnected;

         _discussionLoader = discussionLoader;
         _discussionLoader.PostLoadDiscussionsInternal += processDiscussions;
      }

      public void Dispose()
      {
         _workflowEventNotifier.Connecting -= onConnecting;
         _workflowEventNotifier.Connected -= onConnected;

         _discussionLoader.PostLoadDiscussionsInternal -= processDiscussions;
      }

      internal event Action<UserEvents.DiscussionEvent, DateTime, EDiscussionUpdateType> DiscussionEvent;

      private void processDiscussions(MergeRequestKey mrk, IEnumerable<Discussion> discussions,
         EDiscussionUpdateType type)
      {
         if (discussions.Count() == 0)
         {
            return;
         }

         foreach (Discussion discussion in discussions)
         {
            foreach (DiscussionNote note in discussion.Notes)
            {
               if (note.System && note.Body == "resolved all threads")
               {
                  DiscussionEvent?.Invoke(new UserEvents.DiscussionEvent
                  {
                     EventType = UserEvents.DiscussionEvent.Type.ResolvedAllThreads,
                     Details = note.Author,
                     MergeRequestKey = mrk
                  }, note.Updated_At, type);
               }
               else if (Helpers.IsUserMentioned(note.Body, _currentUser))
               {
                  DiscussionEvent?.Invoke(new UserEvents.DiscussionEvent
                  {
                     EventType = UserEvents.DiscussionEvent.Type.MentionedCurrentUser,
                     Details = note.Author,
                     MergeRequestKey = mrk
                  }, note.Updated_At, type);
               }
               else if (_keywords != null)
               {
                  foreach (string keyword in _keywords)
                  {
                     if (note.Body.Trim().StartsWith(keyword, StringComparison.CurrentCultureIgnoreCase))
                     {
                        DiscussionEvent?.Invoke(new UserEvents.DiscussionEvent
                        {
                           EventType = UserEvents.DiscussionEvent.Type.Keyword,
                           Details = new UserEvents.DiscussionEvent.KeywordDescription
                           {
                              Keyword = keyword,
                              Author = note.Author
                           },
                           MergeRequestKey = mrk
                        }, note.Updated_At, type);
                     }
                  }
               }
            }
         }
      }

      private void onConnecting(string hostname)
      {
         _latestParsingTime.Clear();
      }

      private void onConnected(string hostname, User user, IEnumerable<Project> projects)
      {
         Debug.Assert(!_latestParsingTime.Any());
         _currentUser = user;
      }

      private readonly Dictionary<MergeRequestKey, DateTime> _latestParsingTime =
         new Dictionary<MergeRequestKey, DateTime>();
      private User _currentUser;
      private readonly IEnumerable<string> _keywords;

      private readonly IDiscussionLoaderInternal _discussionLoader;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;
   }
}

