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
   internal class DiscussionParser :
      IDisposable,
      IWorkflowEventListener,
      IDiscussionLoaderListenerInternal
   {
      internal DiscussionParser(
         INotifier<IWorkflowEventListener> workflowEventNotifier,
         INotifier<IDiscussionLoaderListenerInternal> discussionLoaderNotifier,
         IEnumerable<string> keywords)
      {
         _keywords = keywords;

         _workflowEventNotifier = workflowEventNotifier;
         _workflowEventNotifier.AddListener(this);

         _discussionLoaderNotifier = discussionLoaderNotifier;
         _discussionLoaderNotifier.AddListener(this);
      }

      public void Dispose()
      {
         _workflowEventNotifier.RemoveListener(this);

         _discussionLoaderNotifier.RemoveListener(this);
      }

      internal event Action<UserEvents.DiscussionEvent, DateTime, EDiscussionUpdateType> DiscussionEvent;

      public void OnPostLoadDiscussionsInternal(MergeRequestKey mrk, IEnumerable<Discussion> discussions,
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

      public void PreLoadWorkflow(string hostname,
         ILoader<IMergeRequestListLoaderListener> mergeRequestListLoaderListener,
         ILoader<IVersionLoaderListener> versionLoaderListener)
      {
         _latestParsingTime.Clear();
      }

      public void PostLoadWorkflow(string hostname, User user, IWorkflowContext context, IGitLabFacade facade)
      {
         Debug.Assert(!_latestParsingTime.Any());
         _currentUser = user;
      }

      private readonly Dictionary<MergeRequestKey, DateTime> _latestParsingTime =
         new Dictionary<MergeRequestKey, DateTime>();
      private User _currentUser;
      private readonly IEnumerable<string> _keywords;

      private readonly INotifier<IDiscussionLoaderListenerInternal> _discussionLoaderNotifier;
      private readonly INotifier<IWorkflowEventListener> _workflowEventNotifier;
   }
}

