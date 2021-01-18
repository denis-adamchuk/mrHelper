using System;

namespace mrHelper.GitLabClient.Managers
{
   internal class DataCacheInternal : IDisposable
   {
      internal DataCacheInternal(
         MergeRequestManager mergeRequestManager,
         DiscussionManager discussionManager,
         TimeTrackingManager timeTrackingManager,
         ProjectCache projectCache,
         UserCache userCache)
      {
         _mergeRequestManager = mergeRequestManager;
         _discussionManager = discussionManager;
         _timeTrackingManager = timeTrackingManager;
         _projectCache = projectCache;
         _userCache = userCache;
      }

      public void Dispose()
      {
         _mergeRequestManager?.Dispose();
         _mergeRequestManager = null;

         _discussionManager?.Dispose();
         _discussionManager = null;

         _timeTrackingManager?.Dispose();
         _timeTrackingManager = null;
      }

      public IMergeRequestCache MergeRequestCache => _mergeRequestManager;

      public IDiscussionCache DiscussionCache => _discussionManager;

      public ITotalTimeCache TotalTimeCache => _timeTrackingManager;

      public IProjectCache ProjectCache => _projectCache;

      public IUserCache UserCache => _userCache;

      private MergeRequestManager _mergeRequestManager;
      private DiscussionManager _discussionManager;
      private TimeTrackingManager _timeTrackingManager;
      private readonly ProjectCache _projectCache;
      private readonly UserCache _userCache;
   }
}

