using System;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.Types;

namespace mrHelper.Client.Session
{
   internal class SessionInternal : IDisposable
   {
      internal SessionInternal(
         MergeRequestManager mergeRequestManager,
         DiscussionManager discussionManager,
         TimeTrackingManager timeTrackingManager)
      {
         _mergeRequestManager = mergeRequestManager;
         _discussionManager = discussionManager;
         _timeTrackingManager = timeTrackingManager;
      }

      public void Dispose()
      {
         _mergeRequestManager.Dispose();
         _discussionManager.Dispose();
         _timeTrackingManager.Dispose();
      }

      public IMergeRequestCache MergeRequestCache => _mergeRequestManager;

      public IDiscussionCache DiscussionCache => _discussionManager;

      public ITotalTimeCache TotalTimeCache => _timeTrackingManager;

      public ITimeTracker GetTimeTracker(MergeRequestKey mrk)
      {
         return _timeTrackingManager.GetTracker(mrk);
      }

      public IDiscussionEditor GetDiscussionEditor(MergeRequestKey mrk, string discussionId)
      {
         return _discussionManager.GetDiscussionEditor(mrk, discussionId);
      }

      public IDiscussionCreator GetDiscussionCreator(MergeRequestKey mrk)
      {
         return _discussionManager.GetDiscussionCreator(mrk);
      }

      private MergeRequestManager _mergeRequestManager;
      private DiscussionManager _discussionManager;
      private TimeTrackingManager _timeTrackingManager;

   }
}

