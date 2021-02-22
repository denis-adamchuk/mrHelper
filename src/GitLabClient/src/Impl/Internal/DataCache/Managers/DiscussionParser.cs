using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.GitLabClient.Managers
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
         if (_discussionCache != null)
         {
            _discussionCache.DiscussionsLoadedInternal -= onDiscussionsLoaded;
            _discussionCache = null;
         }
      }

      internal event Action<UserEvents.DiscussionEvent, DateTime, DiscussionUpdateType> DiscussionEvent;

      private void onDiscussionsLoaded(MergeRequestKey mrk, IEnumerable<Discussion> discussions,
         DiscussionUpdateType type)
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
                  DiscussionEvent?.Invoke(new UserEvents.DiscussionEvent(
                     mrk, UserEvents.DiscussionEvent.Type.ResolvedAllThreads,
                        note.Author),
                     note.Updated_At, type);
               }
               else if (note.System && note.Body == "approved this merge request")
               {
                  DiscussionEvent?.Invoke(new UserEvents.DiscussionEvent(
                     mrk, UserEvents.DiscussionEvent.Type.ApprovalStatusChange,
                        new UserEvents.DiscussionEvent.ApprovalStatusChangeDescription(true, note.Author)),
                     note.Updated_At, type);
               }
               else if (note.System && note.Body == "unapproved this merge request")
               {
                  DiscussionEvent?.Invoke(new UserEvents.DiscussionEvent(
                     mrk, UserEvents.DiscussionEvent.Type.ApprovalStatusChange,
                        new UserEvents.DiscussionEvent.ApprovalStatusChangeDescription(false, note.Author)),
                     note.Updated_At, type);
               }
               else if (Helpers.IsUserMentioned(note.Body, _currentUser))
               {
                  DiscussionEvent?.Invoke(new UserEvents.DiscussionEvent(
                     mrk, UserEvents.DiscussionEvent.Type.MentionedCurrentUser,
                        note.Author),
                     note.Updated_At, type);
               }
               else if (_keywords != null)
               {
                  foreach (string keyword in _keywords)
                  {
                     if (note.Body.Trim().StartsWith(keyword, StringComparison.CurrentCultureIgnoreCase))
                     {
                        DiscussionEvent?.Invoke(new UserEvents.DiscussionEvent(
                           mrk, UserEvents.DiscussionEvent.Type.Keyword,
                              new UserEvents.DiscussionEvent.KeywordDescription(keyword, note.Author)),
                           note.Updated_At, type);
                     }
                  }
               }
            }
         }
      }

      private readonly User _currentUser;
      private readonly IEnumerable<string> _keywords;
      private IDiscussionCacheInternal _discussionCache;
   }
}

