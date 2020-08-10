using System;

namespace mrHelper.GitLabClient.Managers
{
   internal class DataCacheInternal : IDisposable
   {
      internal DataCacheInternal(
         MergeRequestManager mergeRequestManager,
         DiscussionManager discussionManager,
         TimeTrackingManager timeTrackingManager,
         ProjectCache projectCache)
      {
         _mergeRequestManager = mergeRequestManager;
         _discussionManager = discussionManager;
         _timeTrackingManager = timeTrackingManager;
         _projectCache = projectCache;
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

      public IProjectCache ProjectCache => _projectCache;

      private readonly MergeRequestManager _mergeRequestManager;
      private readonly DiscussionManager _discussionManager;
      private readonly TimeTrackingManager _timeTrackingManager;
      private readonly ProjectCache _projectCache;
   }
}

