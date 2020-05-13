using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Common.Tools;
using mrHelper.Client.Session;

namespace mrHelper.Client.Discussions
{
   /// <summary>
   /// Parses discussion threads and notifies about some events found in them
   /// TODO Clean up merged/closed merge requests
   /// </summary>
   internal class DiscussionParser : IDisposable
   {
      internal DiscussionParser(IDiscussionCacheInternal discussionCache, IEnumerable<string> keywords, User user)
      {
         _keywords = keywords;
         _currentUser = user;

         _discussionCache = discussionCache;
         _discussionCache.DiscussionsLoadedInternal += onDiscussionsLoaded;
      }

      public void Dispose()
      {
         _discussionCache.DiscussionsLoadedInternal -= onDiscussionsLoaded;
      }

      internal event Action<UserEvents.DiscussionEvent, DateTime, EDiscussionUpdateType> DiscussionEvent;

      private void onDiscussionsLoaded(MergeRequestKey mrk, IEnumerable<Discussion> discussions,
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

      private readonly User _currentUser;
      private readonly IEnumerable<string> _keywords;
      private readonly IDiscussionCacheInternal _discussionCache;
   }
}

