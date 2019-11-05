using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Tools;
using static mrHelper.App.Helpers.TrayIcon;
using static mrHelper.Client.Common.UserEvents;

namespace mrHelper.App.Helpers
{
   internal class UserNotifier
   {
      internal UserNotifier(TrayIcon trayIcon, UserDefinedSettings settings,
         MergeRequestManager mergeRequestManager, DiscussionManager discussionManager, EventFilter eventFilter)
      {
         _settings = settings;
         _trayIcon = trayIcon;
         _eventFilter = eventFilter;
         mergeRequestManager.MergeRequestEvent += (e) => notifyOnEvent(e);
         discussionManager.DiscussionEvent += (e) => notifyOnEvent(e);
         _mergeRequestProvider = mergeRequestManager;
      }

      private TrayIcon.BalloonText getBalloonText(UserEvents.MergeRequestEvent e)
      {
         MergeRequest mergeRequest = e.FullMergeRequestKey.MergeRequest;

         string projectName = e.FullMergeRequestKey.ProjectKey.ProjectName;
         projectName = projectName == String.Empty ? "N/A" : projectName;

         switch (e.EventType)
         {
            case MergeRequestEvent.Type.NewMergeRequest:
               return new BalloonText
               {
                  Title = "Merge Request Event",
                  Text = String.Format("New merge request \"{0}\" from {1} in project {2}",
                                       mergeRequest.Title, mergeRequest.Author.Name, projectName)
               };

            case MergeRequestEvent.Type.ClosedMergeRequest:
               return new BalloonText
               {
                  Title = "Merge Request Event",
                  Text = String.Format("Merge request \"{0}\" from {1} in project {2} was merged/closed",
                                       mergeRequest.Title, mergeRequest.Author.Name, projectName)
               };

            case MergeRequestEvent.Type.UpdatedMergeRequest:
               Debug.Assert(((MergeRequestEvent.UpdateDetails)e.Details).NewCommits);
               return new BalloonText
               {
                  Title = "Merge Request Event",
                  Text = String.Format("New commits in merge request \"{0}\" from {1} in project {2}",
                                       mergeRequest.Title, mergeRequest.Author.Name, projectName)
               };

            default:
               Debug.Assert(false);
               return new BalloonText();
         }
      }

      private BalloonText getBalloonText(UserEvents.DiscussionEvent e)
      {
         MergeRequest? mergeRequest = _mergeRequestProvider.GetMergeRequest(e.MergeRequestKey);

         switch (e.EventType)
         {
            case DiscussionEvent.Type.ResolvedAllThreads:
               return new BalloonText
               {
                  Title = "Discussion Event",
                  Text = String.Format("All discussions were resolved in merge request \"{0}\"{1}",
                                    mergeRequest.HasValue ? mergeRequest.Value.Title : e.MergeRequestKey.IId.ToString(),
                                    mergeRequest.HasValue ? " from " + mergeRequest.Value.Author.Name : String.Empty)
               };

            case DiscussionEvent.Type.MentionedCurrentUser:
               User author = (User)e.Details;
               return new BalloonText
               {
                  Title = "Discussion Event",
                  Text = String.Format("{0} mentioned you in a discussion of merge request \"{1}\"",
                                    author.Name,
                                    mergeRequest.HasValue ? mergeRequest.Value.Title : e.MergeRequestKey.IId.ToString())
               };

            case DiscussionEvent.Type.Keyword:
               DiscussionEvent.KeywordDescription kd = (DiscussionEvent.KeywordDescription)e.Details;
               return new BalloonText
               {
                  Title = "Discussion Event",
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
         if (_eventFilter.NeedSuppressEvent((dynamic)e))
         {
            return;
         }

         BalloonText balloonText = getBalloonText((dynamic)e);
         _trayIcon.ShowTooltipBalloon(balloonText);
      }

      private readonly UserDefinedSettings _settings;
      private readonly TrayIcon _trayIcon;
      private readonly EventFilter _eventFilter;
      private readonly IMergeRequestProvider _mergeRequestProvider;
   }
}
