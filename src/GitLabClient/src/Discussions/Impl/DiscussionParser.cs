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
         _workflowEventNotifier.Connected += onConnected;

         _discussionLoader = discussionLoader;
         _discussionLoader.PostLoadDiscussionsInternal += processDiscussions;
      }

      public void Dispose()
      {
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
               else if (isUserMentioned(note.Body, _currentUser))
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

      private static bool isUserMentioned(string text, User user)
      {
         if (StringUtils.ContainsNoCase(text, user.Name))
         {
            return true;
         }

         string label = Constants.GitLabLabelPrefix + user.Username;
         int idx = text.IndexOf(label, StringComparison.CurrentCultureIgnoreCase);
         while (idx >= 0)
         {
            if (idx == text.Length - label.Length)
            {
               // text ends with label
               return true;
            }

            if (!Char.IsLetter(text[idx + label.Length]))
            {
               // label is in the middle of text
               return true;
            }

            Debug.Assert(idx != text.Length - 1);
            idx = text.IndexOf(label, idx + 1, StringComparison.CurrentCultureIgnoreCase);
         }

         return false;
      }

      private void onConnected(string hostname, User user)
      {
         _currentUser = user;
         _latestParsingTime.Clear();
      }

      private readonly Dictionary<MergeRequestKey, DateTime> _latestParsingTime =
         new Dictionary<MergeRequestKey, DateTime>();
      private User _currentUser;
      private readonly IEnumerable<string> _keywords;

      private readonly IDiscussionLoaderInternal _discussionLoader;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;
   }
}

