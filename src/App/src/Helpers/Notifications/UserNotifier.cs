using System;
using System.Diagnostics;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Common.Interfaces;
using static mrHelper.App.Helpers.TrayIcon;
using static mrHelper.Client.Types.UserEvents;

namespace mrHelper.App.Helpers
{
   internal class UserNotifier : IDisposable
   {
      internal UserNotifier(
         ICachedMergeRequestProvider mergeRequestProvider,
         IDiscussionProvider discussionProvider,
         EventFilter eventFilter,
         TrayIcon trayIcon)
      {
         _trayIcon = trayIcon;
         _eventFilter = eventFilter;

         _mergeRequestProvider = mergeRequestProvider;
         _mergeRequestProvider.MergeRequestEvent += notifyOnEvent;
         _mergeRequestProvider = mergeRequestProvider;

         _discussionProvider = discussionProvider;
         _discussionProvider.DiscussionEvent += notifyOnEvent;
      }

      public void Dispose()
      {
         _mergeRequestProvider.MergeRequestEvent -= notifyOnEvent;
         _discussionProvider.DiscussionEvent -= notifyOnEvent;
      }

      private TrayIcon.BalloonText getBalloonText(MergeRequestEvent e)
      {
         MergeRequest mergeRequest = e.FullMergeRequestKey.MergeRequest;
         string projectName = getProjectName(e.FullMergeRequestKey.ProjectKey);
         string title = String.Format("{0}: Merge Request Event", projectName);

         switch (e.EventType)
         {
            case MergeRequestEvent.Type.NewMergeRequest:
               return new BalloonText
               {
                  Title = title,
                  Text = String.Format("New merge request \"{0}\" from {1}",
                                       mergeRequest.Title, mergeRequest.Author.Name)
               };

            case MergeRequestEvent.Type.ClosedMergeRequest:
               return new BalloonText
               {
                  Title = title,
                  Text = String.Format("Merge request \"{0}\" from {1} was merged/closed",
                                       mergeRequest.Title, mergeRequest.Author.Name)
               };

            case MergeRequestEvent.Type.UpdatedMergeRequest:
               Debug.Assert(((MergeRequestEvent.UpdateScope)e.Scope).Commits);
               return new BalloonText
               {
                  Title = title,
                  Text = String.Format("New commits in merge request \"{0}\" from {1}",
                                       mergeRequest.Title, mergeRequest.Author.Name)
               };

            default:
               Debug.Assert(false);
               return new BalloonText();
         }
      }

      private BalloonText getBalloonText(DiscussionEvent e)
      {
         MergeRequest? mergeRequest = _mergeRequestProvider.GetMergeRequest(e.MergeRequestKey);
         string projectName = getProjectName(e.MergeRequestKey.ProjectKey);
         string title = String.Format("{0}: Discussion Event", projectName);

         switch (e.EventType)
         {
            case DiscussionEvent.Type.ResolvedAllThreads:
               return new BalloonText
               {
                  Title = title,
                  Text = String.Format("All discussions resolved in merge request \"{0}\"{1}",
                                    mergeRequest.HasValue ? mergeRequest.Value.Title : e.MergeRequestKey.IId.ToString(),
                                    mergeRequest.HasValue ? " from " + mergeRequest.Value.Author.Name : String.Empty)
               };

            case DiscussionEvent.Type.MentionedCurrentUser:
               User author = (User)e.Details;
               return new BalloonText
               {
                  Title = title,
                  Text = String.Format("{0} mentioned you in a discussion of merge request \"{1}\"",
                                    author.Name,
                                    mergeRequest.HasValue ? mergeRequest.Value.Title : e.MergeRequestKey.IId.ToString())
               };

            case DiscussionEvent.Type.Keyword:
               DiscussionEvent.KeywordDescription kd = (DiscussionEvent.KeywordDescription)e.Details;
               return new BalloonText
               {
                  Title = title,
                  Text = String.Format("{0} said \"{1}\" in merge request \"{2}\"",
                                    kd.Author.Name, kd.Keyword,
                                    mergeRequest.HasValue ? mergeRequest.Value.Title : e.MergeRequestKey.IId.ToString())
               };

            default:
               Debug.Assert(false);
               return new BalloonText();
         }
      }

      private void notifyOnEvent<EventT>(EventT e)
      {
         if ((e is DiscussionEvent   de && _eventFilter.NeedSuppressEvent(de))
          || (e is MergeRequestEvent me && _eventFilter.NeedSuppressEvent(me)))
         {
            return;
         }

         BalloonText balloonText = getBalloonText((dynamic)e);
         _trayIcon.ShowTooltipBalloon(balloonText);
      }

      private static string getProjectName(ProjectKey projectKey)
      {
         string projectName = projectKey.ProjectName.Split('/').ElementAtOrDefault(1);
         projectName = String.IsNullOrWhiteSpace(projectName) ? "N/A" : projectName;
         return projectName;
      }

      private readonly TrayIcon _trayIcon;
      private readonly EventFilter _eventFilter;
      private readonly ICachedMergeRequestProvider _mergeRequestProvider;
      private readonly IDiscussionProvider _discussionProvider;
   }
}
