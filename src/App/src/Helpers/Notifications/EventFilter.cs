using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;
using static mrHelper.Client.Common.UserEvents;

namespace mrHelper.App.Helpers
{
   internal class EventFilter
   {
      internal EventFilter(UserDefinedSettings settings, Workflow workflow,
         Func<MergeRequestKey, MergeRequest?> getMergeRequest)
      {
         _settings = settings;
         _getMergeRequest = getMergeRequest;
         workflow.PostLoadCurrentUser += (user) => _currentUser = user;
      }

      internal bool NeedSuppressEvent(MergeRequestEvent e)
      {
         MergeRequest mergeRequest = e.FullMergeRequestKey.MergeRequest;

         return (MergeRequestFilter.IsFilteredMergeRequest(mergeRequest, ConfigurationHelper.GetLabels(_settings))
            || (isCurrentUserActivity(_currentUser ?? new User(), mergeRequest) && !_settings.Notifications_MyActivity)
            || (e.EventType == MergeRequestEvent.Type.NewMergeRequest           && !_settings.Notifications_NewMergeRequests)
            || (e.EventType == MergeRequestEvent.Type.UpdatedMergeRequest       && !_settings.Notifications_UpdatedMergeRequests)
            || (e.EventType == MergeRequestEvent.Type.UpdatedMergeRequest       && !((MergeRequestEvent.UpdateDetails)e.Details).NewCommits)
            || (e.EventType == MergeRequestEvent.Type.ClosedMergeRequest        && !_settings.Notifications_MergedMergeRequests));
      }

      internal bool NeedSuppressEvent(DiscussionEvent e)
      {
         MergeRequest? mergeRequest = _getMergeRequest(e.MergeRequestKey);
         if (!mergeRequest.HasValue)
         {
            // TODO Consider not supressing OnMention event even if MR is closed
            return true;
         }

         return (MergeRequestFilter.IsFilteredMergeRequest(mergeRequest.Value, ConfigurationHelper.GetLabels(_settings))
            || (isCurrentUserActivity(_currentUser ?? new User(), e)            && !_settings.Notifications_MyActivity)
            || (e.EventType == DiscussionEvent.Type.ResolvedAllThreads          && !_settings.Notifications_AllThreadsResolved)
            || (e.EventType == DiscussionEvent.Type.MentionedCurrentUser        && !_settings.Notifications_OnMention)
            || (e.EventType == DiscussionEvent.Type.Keyword                     && !_settings.Notifications_Keywords));
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

      private readonly UserDefinedSettings _settings;
      private User? _currentUser;
      private readonly Func<MergeRequestKey, MergeRequest?> _getMergeRequest;
   }
}

