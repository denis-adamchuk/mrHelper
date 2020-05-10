using System;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.Repository;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.Workflow;

namespace mrHelper.Client.Common
{
   internal class GitLabFacade : IDisposable, IGitLabFacade
   {
      internal GitLabFacade(
         MergeRequestLoader mergeRequestLoader,
         MergeRequestManager mergeRequestManager,
         DiscussionManager discussionManager,
         TimeTrackingManager timeTrackingManager)
      {
         _mergeRequestLoader = mergeRequestLoader;
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

      public IMergeRequestLoader MergeRequestLoader => _mergeRequestLoader;

      public IMergeRequestManager MergeRequestManager => _mergeRequestManager;

      public IDiscussionManager DiscussionManager => _discussionManager;

      public ITimeTrackingManager TimeTrackingManager => _timeTrackingManager;

      private MergeRequestLoader _mergeRequestLoader;
      private MergeRequestManager _mergeRequestManager;
      private DiscussionManager _discussionManager;
      private TimeTrackingManager _timeTrackingManager;
   }
}

