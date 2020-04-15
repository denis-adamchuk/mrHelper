using System;
using System.Collections.Generic;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Types;
using mrHelper.Client.Workflow;
using static mrHelper.Client.Common.UserEvents;

namespace mrHelper.App.Helpers
{
   internal class EventFilter : IDisposable
   {
      internal EventFilter(UserDefinedSettings settings, IWorkflowEventNotifier workflowEventNotifier,
         ICachedMergeRequestProvider mergeRequestProvider, MergeRequestFilter mergeRequestFilter)
      {
         _settings = settings;
         _mergeRequestProvider = mergeRequestProvider;

         _workflowEventNotifier = workflowEventNotifier;
         _workflowEventNotifier.Connected += onConnected;

         _mergeRequestFilter = mergeRequestFilter;
      }

      public void Dispose()
      {
         _workflowEventNotifier.Connected -= onConnected;
      }

      internal bool NeedSuppressEvent(MergeRequestEvent e)
      {
         MergeRequest mergeRequest = e.FullMergeRequestKey.MergeRequest;

         return (!_mergeRequestFilter.DoesMatchFilter(mergeRequest)
            || (isServiceEvent(mergeRequest)                                    && !_settings.Notifications_Service)
            || (isCurrentUserActivity(_currentUser ?? new User(), mergeRequest) && !_settings.Notifications_MyActivity)
            || (e.EventType == MergeRequestEvent.Type.NewMergeRequest           && !_settings.Notifications_NewMergeRequests)
            || (e.EventType == MergeRequestEvent.Type.UpdatedMergeRequest       && !_settings.Notifications_UpdatedMergeRequests)
            || (e.EventType == MergeRequestEvent.Type.UpdatedMergeRequest       && !((MergeRequestEvent.UpdateScope)e.Scope).Commits)
            || (e.EventType == MergeRequestEvent.Type.ClosedMergeRequest        && !_settings.Notifications_MergedMergeRequests));
      }

      internal bool NeedSuppressEvent(DiscussionEvent e)
      {
         MergeRequest? mergeRequest = _mergeRequestProvider.GetMergeRequest(e.MergeRequestKey);
         if (!mergeRequest.HasValue)
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

         return (!_mergeRequestFilter.DoesMatchFilter(mergeRequest.Value)
            || (isServiceEvent(e)                                         && !_settings.Notifications_Service)
            || (isCurrentUserActivity(_currentUser ?? new User(), e)      && !_settings.Notifications_MyActivity)
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

      private void onConnected(string hostname, User user, IEnumerable<Project> projects)
      {
         _currentUser = user;
      }

      private readonly UserDefinedSettings _settings;
      private User? _currentUser;
      private readonly ICachedMergeRequestProvider _mergeRequestProvider;
      private readonly IWorkflowEventNotifier _workflowEventNotifier;
      private readonly MergeRequestFilter _mergeRequestFilter;
   }
}

