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

namespace mrHelper.Client.Discussions
{
   /// <summary>
   /// Parses discussion threads and notifies about some events found in them
   /// TODO Clean up merged/closed merge requests
   /// </summary>
   public class DiscussionParser
   {
      public DiscussionParser(Workflow.Workflow workflow, DiscussionManager discussionManager,
         IEnumerable<string> keywords)
      {
         _keywords = keywords;
         workflow.PostLoadCurrentUser += (user) => _currentUser = user;
         discussionManager.PostLoadDiscussions +=
            (mrk, discussions, updatedAt, initialSnapshot) =>
               processDiscussions(mrk, discussions, updatedAt, initialSnapshot);
      }

      public enum Event
      {
         ResolvedAllThreads,
         MentionedCurrentUser,
         Keyword
      }

      public struct KeywordDescription
      {
         public string Keyword;
         public User Author;
      }

      public event Action<MergeRequestKey, Event, object> DiscussionEvent;

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
                  DiscussionEvent?.Invoke(mrk, Event.ResolvedAllThreads, note.Author);
               }
               // TODO Use regex to not treat @abcd as mentioning of @abcdef
               else if (note.Body.Contains('@' + _currentUser.Username) || note.Body.Contains(_currentUser.Name))
               {
                  DiscussionEvent?.Invoke(mrk, Event.MentionedCurrentUser, note.Author);
               }
               else if (_keywords != null)
               {
                  foreach (string keyword in _keywords)
                  {
                     if (note.Body.Trim().StartsWith(keyword, StringComparison.CurrentCultureIgnoreCase))
                     {
                        DiscussionEvent?.Invoke(mrk, Event.Keyword,
                           new KeywordDescription{ Keyword = keyword, Author = note.Author });
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
   }
}

