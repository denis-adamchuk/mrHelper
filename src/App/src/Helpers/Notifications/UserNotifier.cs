using System;
using System.Diagnostics;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using static mrHelper.App.Helpers.TrayIcon;
using static mrHelper.GitLabClient.UserEvents;

namespace mrHelper.App.Helpers
{
   internal class UserNotifier : IDisposable
   {
      internal UserNotifier(DataCache dataCache, EventFilter eventFilter, TrayIcon trayIcon)
      {
         _trayIcon = trayIcon;
         _eventFilter = eventFilter;

         _dataCache = dataCache;
         _dataCache.Connected += onDataCacheConnected;
      }

      private void onDataCacheConnected(string hostname, User user)
      {
         if (_mergeRequestCache != null)
         {
            _mergeRequestCache.MergeRequestEvent -= notifyOnMergeRequestEvent;
            _mergeRequestCache = null;
         }

         if (_discussionCache != null)
         {
            _discussionCache.DiscussionEvent -= notifyOnDiscussionEvent;
            _discussionCache = null;
         }

         _mergeRequestCache = _dataCache.MergeRequestCache;
         _mergeRequestCache.MergeRequestEvent += notifyOnMergeRequestEvent;

         _discussionCache = _dataCache.DiscussionCache;
         _discussionCache.DiscussionEvent += notifyOnDiscussionEvent;
      }

      public void Dispose()
      {
         if (_dataCache != null)
         {
            _dataCache.Connected -= onDataCacheConnected;
            _dataCache = null;
         }

         if (_mergeRequestCache != null)
         {
            _mergeRequestCache.MergeRequestEvent -= notifyOnMergeRequestEvent;
            _mergeRequestCache = null;
         }

         if (_discussionCache != null)
         {
            _discussionCache.DiscussionEvent -= notifyOnDiscussionEvent;
            _discussionCache = null;
         }
      }

      private TrayIcon.BalloonText getBalloonText(MergeRequestEvent e)
      {
         MergeRequest mergeRequest = e.FullMergeRequestKey.MergeRequest;
         string projectName = getProjectName(e.FullMergeRequestKey.ProjectKey);
         string title = String.Format("{0}: Merge Request Event", projectName);

         switch (e.EventType)
         {
            case MergeRequestEvent.Type.AddedMergeRequest:
               return new BalloonText
               (
                  title,
                  String.Format("New merge request \"{0}\" from {1}",
                                mergeRequest.Title, mergeRequest.Author.Name)
               );

            case MergeRequestEvent.Type.RemovedMergeRequest:
               return new BalloonText
               (
                  title,
                  String.Format("Merge request \"{0}\" from {1} moved to Recent tab",
                                mergeRequest.Title, mergeRequest.Author.Name)
               );

            case MergeRequestEvent.Type.UpdatedMergeRequest:
               Debug.Assert(((MergeRequestEvent.UpdateScope)e.Scope).Commits);
               return new BalloonText
               (
                  title,
                  String.Format("New commits in merge request \"{0}\" from {1}",
                                mergeRequest.Title, mergeRequest.Author.Name)
               );

            default:
               Debug.Assert(false);
               return new BalloonText();
         }
      }

      private BalloonText getBalloonText(DiscussionEvent e)
      {
         MergeRequest mergeRequest = _dataCache?.MergeRequestCache?.GetMergeRequest(e.MergeRequestKey);
         string projectName = getProjectName(e.MergeRequestKey.ProjectKey);
         string title = String.Format("{0}: Discussion Event", projectName);

         switch (e.EventType)
         {
            case DiscussionEvent.Type.ResolvedAllThreads:
               return new BalloonText
               (
                  title,
                  String.Format("All discussions resolved in merge request \"{0}\"{1}",
                                mergeRequest != null ? mergeRequest.Title : e.MergeRequestKey.IId.ToString(),
                                mergeRequest != null ? " from " + mergeRequest.Author.Name : String.Empty)
               );

            case DiscussionEvent.Type.MentionedCurrentUser:
               User author = (User)e.Details;
               return new BalloonText
               (
                  title,
                  String.Format("{0} mentioned you in a discussion of merge request \"{1}\"",
                                author.Name,
                                mergeRequest != null ? mergeRequest.Title : e.MergeRequestKey.IId.ToString())
               );

            case DiscussionEvent.Type.Keyword:
               DiscussionEvent.KeywordDescription kd = (DiscussionEvent.KeywordDescription)e.Details;
               return new BalloonText
               (
                  title,
                  String.Format("{0} said \"{1}\" in merge request \"{2}\"",
                                kd.Author.Name, kd.Keyword,
                                mergeRequest != null ? mergeRequest.Title : e.MergeRequestKey.IId.ToString())
               );

            case DiscussionEvent.Type.ApprovalStatusChange:
               DiscussionEvent.ApprovalStatusChangeDescription asd =
                  (DiscussionEvent.ApprovalStatusChangeDescription)e.Details;
               return new BalloonText
               (
                  title,
                  String.Format("{0} \"{1}\" merge request \"{2}\"",
                                asd.Author.Name, asd.IsApproved ? "approved" : "unapproved",
                                mergeRequest != null ? mergeRequest.Title : e.MergeRequestKey.IId.ToString())
               );

            default:
               Debug.Assert(false);
               return new BalloonText();
         }
      }

      private void notifyOnMergeRequestEvent(MergeRequestEvent e)
      {
         if (!_eventFilter.NeedSuppressEvent(e))
         {
            BalloonText balloonText = getBalloonText(e);
            _trayIcon.ShowTooltipBalloon(balloonText);
         }
      }

      private void notifyOnDiscussionEvent(DiscussionEvent e)
      {
         if (!_eventFilter.NeedSuppressEvent(e))
         {
            BalloonText balloonText = getBalloonText(e);
            _trayIcon.ShowTooltipBalloon(balloonText);
         }
      }

      private static string getProjectName(ProjectKey projectKey)
      {
         string projectName = projectKey.ProjectName.Split('/').ElementAtOrDefault(1);
         projectName = String.IsNullOrWhiteSpace(projectName) ? "N/A" : projectName;
         return projectName;
      }

      private IMergeRequestCache _mergeRequestCache;
      private IDiscussionCache _discussionCache;

      private DataCache _dataCache;
      private readonly TrayIcon _trayIcon;
      private readonly EventFilter _eventFilter;
   }
}
