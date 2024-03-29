﻿using System;
using System.Diagnostics;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using static mrHelper.GitLabClient.UserEvents;

namespace mrHelper.App.Helpers
{
   internal class EventFilter : IDisposable
   {
      internal EventFilter(UserDefinedSettings settings, DataCache dataCache, MergeRequestFilter mergeRequestFilter)
      {
         _settings = settings;

         _dataCache = dataCache;
         _dataCache.Disconnected += onDataCacheDisconnected;
         _dataCache.Connected += onDataCacheConnected;

         _mergeRequestFilter = mergeRequestFilter;
      }

      public void Dispose()
      {
         if (_dataCache != null)
         {
            _dataCache.Disconnected -= onDataCacheDisconnected;
            _dataCache.Connected -= onDataCacheConnected;
            _dataCache = null;
         }
      }

      internal bool NeedSuppressEvent(MergeRequestEvent e)
      {
         if (_currentUser == null)
         {
            Debug.Assert(false);
            return false;
         }

         MergeRequest mergeRequest = e.FullMergeRequestKey.MergeRequest;

         return (!_mergeRequestFilter.DoesMatchFilter(e.FullMergeRequestKey)
            || (isServiceEvent(mergeRequest)                                 && !_settings.Notifications_Service)
            || (isCurrentUserActivity(_currentUser, mergeRequest)            && !_settings.Notifications_MyActivity)
            || (e.EventType == MergeRequestEvent.Type.AddedMergeRequest      && !_settings.Notifications_NewMergeRequests)
            || (e.EventType == MergeRequestEvent.Type.UpdatedMergeRequest    && !_settings.Notifications_UpdatedMergeRequests)
            || (e.EventType == MergeRequestEvent.Type.UpdatedMergeRequest    && !((MergeRequestEvent.UpdateScope)e.Scope).Commits)
            || (e.EventType == MergeRequestEvent.Type.RemovedMergeRequest    && !isMergedNotificationsEnabled(e.FullMergeRequestKey.ProjectKey)));
      }

      private bool isMergedNotificationsEnabled(ProjectKey projectKey)
      {
         // notify about "merged" merge requests if only the whole project is watched
         return _settings.Notifications_MergedMergeRequests
             && ConfigurationHelper.GetEnabledProjects(projectKey.HostName, _settings)
                .Any(projectName => projectKey.MatchProject(projectName));
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

         if (e.EventType == DiscussionEvent.Type.MentionedCurrentUser)
         {
            // `on mention` is a special case which should work disregarding to the _mergeRequestFilter
            return
                (isCurrentUserActivity(_currentUser, e) && !_settings.Notifications_MyActivity)
             || (isServiceEvent(e)                      && !_settings.Notifications_Service)
             ||                                            !_settings.Notifications_OnMention;
         }

         FullMergeRequestKey fmk = new FullMergeRequestKey(e.MergeRequestKey.ProjectKey, mergeRequest);
         return (!_mergeRequestFilter.DoesMatchFilter(fmk)
            || (isServiceEvent(e)                                         && !_settings.Notifications_Service)
            || (isCurrentUserActivity(_currentUser, e)                    && !_settings.Notifications_MyActivity)
            || (e.EventType == DiscussionEvent.Type.ResolvedAllThreads    && !_settings.Notifications_AllThreadsResolved)
            || (e.EventType == DiscussionEvent.Type.Keyword               && !_settings.Notifications_Keywords)
            || (e.EventType == DiscussionEvent.Type.ApprovalStatusChange  && !_settings.Notifications_Keywords));
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

            case DiscussionEvent.Type.ApprovalStatusChange:
               return ((DiscussionEvent.ApprovalStatusChangeDescription)e.Details).Author.Id == currentUser.Id;

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

            case DiscussionEvent.Type.ApprovalStatusChange:
               return ((DiscussionEvent.ApprovalStatusChangeDescription)e.Details).Author.Username ==
                  Program.ServiceManager.GetServiceMessageUsername();

            default:
               Debug.Assert(false);
               return false;
         }
      }

      private void onDataCacheDisconnected()
      {
         _currentUser = null;
         _mergeRequestCache = null;
      }

      private void onDataCacheConnected(string hostname, User user)
      {
         _currentUser = user;
         _mergeRequestCache = _dataCache?.MergeRequestCache;
      }

      private User _currentUser;
      private IMergeRequestCache _mergeRequestCache;

      private readonly UserDefinedSettings _settings;
      private DataCache _dataCache;
      private readonly MergeRequestFilter _mergeRequestFilter;
   }
}

