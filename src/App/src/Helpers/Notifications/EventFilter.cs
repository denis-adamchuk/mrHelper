using System;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Types;
using mrHelper.Client.Session;
using static mrHelper.Client.Types.UserEvents;

namespace mrHelper.App.Helpers
{
   internal class EventFilter : IDisposable
   {
      internal EventFilter(UserDefinedSettings settings, ISession session, MergeRequestFilter mergeRequestFilter)
      {
         _settings = settings;

         _session = session;
         _session.Started += onSessionStarted;

         _mergeRequestFilter = mergeRequestFilter;
      }

      public void Dispose()
      {
         _session.Started -= onSessionStarted;
      }

      internal bool NeedSuppressEvent(MergeRequestEvent e)
      {
         if (_currentUser == null)
         {
            Debug.Assert(false);
            return false;
         }

         MergeRequest mergeRequest = e.FullMergeRequestKey.MergeRequest;

         return (!_mergeRequestFilter.DoesMatchFilter(mergeRequest)
            || (isServiceEvent(mergeRequest)                                 && !_settings.Notifications_Service)
            || (isCurrentUserActivity(_currentUser, mergeRequest)            && !_settings.Notifications_MyActivity)
            || (e.EventType == MergeRequestEvent.Type.NewMergeRequest        && !_settings.Notifications_NewMergeRequests)
            || (e.EventType == MergeRequestEvent.Type.UpdatedMergeRequest    && !_settings.Notifications_UpdatedMergeRequests)
            || (e.EventType == MergeRequestEvent.Type.UpdatedMergeRequest    && !((MergeRequestEvent.UpdateScope)e.Scope).Commits)
            || (e.EventType == MergeRequestEvent.Type.ClosedMergeRequest     && !_settings.Notifications_MergedMergeRequests));
      }

      internal bool NeedSuppressEvent(DiscussionEvent e)
      {
         if (_currentUser == null || _mergeRequestCache == null)
         {
            Debug.Assert(false);
            return false;
         }

         MergeRequest mergeRequest = _mergeRequestCache.GetMergeRequest(e.MergeRequestKey);
         if (mergeRequest == null)
         {
            return true;
         }

         bool onMention =
               (!isCurrentUserActivity(_currentUser ?? new User(), e) || _settings.Notifications_MyActivity)
            && (!isServiceEvent(e)                                    || _settings.Notifications_Service)
            && e.EventType == DiscussionEvent.Type.MentionedCurrentUser && _settings.Notifications_OnMention;
         if (onMention)
         {
            // `on mention` is a special case which should work disregarding to the _mergeRequestFilter
            return false;
         }

         return (!_mergeRequestFilter.DoesMatchFilter(mergeRequest)
            || (isServiceEvent(e)                                         && !_settings.Notifications_Service)
            || (isCurrentUserActivity(_currentUser, e)                    && !_settings.Notifications_MyActivity)
            || (e.EventType == DiscussionEvent.Type.ResolvedAllThreads    && !_settings.Notifications_AllThreadsResolved)
            || (e.EventType == DiscussionEvent.Type.Keyword               && !_settings.Notifications_Keywords));
      }

      private static bool isCurrentUserActivity(User currentUser, MergeRequest m)
      {
         return m.Author.Id == currentUser.Id;
      }

      private static bool isCurrentUserActivity(User currentUser, DiscussionEvent e)
      {
         switch (e.EventType)
         {
            case DiscussionEvent.Type.ResolvedAllThreads:
               return ((User)e.Details).Id == currentUser.Id;

            case DiscussionEvent.Type.MentionedCurrentUser:
               return ((User)e.Details).Id == currentUser.Id;

            case DiscussionEvent.Type.Keyword:
               return ((DiscussionEvent.KeywordDescription)e.Details).Author.Id == currentUser.Id;

            default:
               Debug.Assert(false);
               return false;
         }
      }

      private static bool isServiceEvent(MergeRequest m)
      {
         return m.Author.Username == Program.ServiceManager.GetServiceMessageUsername();
      }

      private static bool isServiceEvent(DiscussionEvent e)
      {
         switch (e.EventType)
         {
            case DiscussionEvent.Type.ResolvedAllThreads:
               return ((User)e.Details).Username == Program.ServiceManager.GetServiceMessageUsername();

            case DiscussionEvent.Type.MentionedCurrentUser:
               return ((User)e.Details).Username == Program.ServiceManager.GetServiceMessageUsername();

            case DiscussionEvent.Type.Keyword:
               return ((DiscussionEvent.KeywordDescription)e.Details).Author.Username ==
                  Program.ServiceManager.GetServiceMessageUsername();

            default:
               Debug.Assert(false);
               return false;
         }
      }

      private void onSessionStarted(string hostname, User user, SessionContext sessionContext)
      {
         _currentUser = user;
         _mergeRequestCache = _session.MergeRequestCache;
      }

      private User _currentUser;
      private IMergeRequestCache _mergeRequestCache;

      private readonly UserDefinedSettings _settings;
      private readonly ISession _session;
      private readonly MergeRequestFilter _mergeRequestFilter;
   }
}

