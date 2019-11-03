using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;
using mrHelper.Client.Discussions;
using mrHelper.Client.Common;

namespace mrHelper.Client.Discussions
{
   /// <summary>
   /// Parses discussion threads and notifies about some events found in them
   /// TODO Clean up merged/closed merge requests
   /// </summary>
   internal class DiscussionParser
   {
      internal DiscussionParser(Workflow.Workflow workflow, DiscussionManager discussionManager,
         IEnumerable<string> keywords, Action<UserEvents.DiscussionEvent> onEvent)
      {
         _keywords = keywords;
         workflow.PostLoadCurrentUser += (user) => _currentUser = user;
         discussionManager.PostLoadDiscussions +=
            (mrk, discussions, updatedAt, initialSnapshot) =>
               processDiscussions(mrk, discussions, updatedAt, initialSnapshot);
         _onEvent = onEvent;
      }

      private void processDiscussions(MergeRequestKey mrk, List<Discussion> discussions,
         DateTime updatedAt, bool initialSnapshot)
      {
         if (discussions.Count == 0)
         {
            return;
         }

         if (initialSnapshot)
         {
            // consider all notes already parsed and skip checking
            _latestParsingTime[mrk] = updatedAt;
            return;
         }

         foreach (Discussion discussion in discussions)
         {
            foreach (DiscussionNote note in discussion.Notes)
            {
               Debug.Assert(_latestParsingTime.ContainsKey(mrk) || !initialSnapshot);
               if (_latestParsingTime.ContainsKey(mrk) && note.Updated_At <= _latestParsingTime[mrk])
               {
                  continue;
               }

               if (note.System && note.Body == "resolved all threads")
               {
                  _onEvent(new UserEvents.DiscussionEvent
                  {
                     EventType = UserEvents.DiscussionEvent.Type.ResolvedAllThreads,
                     Details = note.Author,
                     MergeRequestKey = mrk
                  });
               }
               // TODO Use regex to not treat @abcd as mentioning of @abcdef
               else if (note.Body.Contains('@' + _currentUser.Username) || note.Body.Contains(_currentUser.Name))
               {
                  _onEvent(new UserEvents.DiscussionEvent
                  {
                     EventType = UserEvents.DiscussionEvent.Type.MentionedCurrentUser,
                     Details = note.Author,
                     MergeRequestKey = mrk
                  });
               }
               else if (_keywords != null)
               {
                  foreach (string keyword in _keywords)
                  {
                     if (note.Body.Trim().StartsWith(keyword, StringComparison.CurrentCultureIgnoreCase))
                     {
                        _onEvent(new UserEvents.DiscussionEvent
                        {
                           EventType = UserEvents.DiscussionEvent.Type.Keyword,
                           Details = new UserEvents.DiscussionEvent.KeywordDescription { Keyword = keyword, Author = note.Author },
                           MergeRequestKey = mrk
                        });
                     }
                  }
               }
            }
         }

         _latestParsingTime[mrk] = updatedAt;
      }

      private readonly Dictionary<MergeRequestKey, DateTime> _latestParsingTime =
         new Dictionary<MergeRequestKey, DateTime>();
      private User _currentUser;
      private readonly IEnumerable<string> _keywords;
      private Action<UserEvents.DiscussionEvent> _onEvent;
   }
}

