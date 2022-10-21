using System;
using GitLabSharp.Entities;

namespace mrHelper.GitLabClient.Managers
{
   internal class DataCacheInternal : IDisposable
   {
      internal DataCacheInternal(
         MergeRequestManager mergeRequestManager,
         DiscussionManager discussionManager,
         TimeTrackingManager timeTrackingManager,
         ProjectCache projectCache,
         UserCache userCache,
         AvatarCache avatarCache)
      {
         _mergeRequestManager = mergeRequestManager;
         _discussionManager = discussionManager;
         _timeTrackingManager = timeTrackingManager;
         _projectCache = projectCache;
         _userCache = userCache;
         _avatarCache = avatarCache;
      }

      public void Dispose()
      {
         _mergeRequestManager?.Dispose();
         _mergeRequestManager = null;

         _discussionManager?.Dispose();
         _discussionManager = null;

         _timeTrackingManager?.Dispose();
         _timeTrackingManager = null;

         _userCache?.Dispose();
         _userCache = null;

         _projectCache = null;
         _avatarCache = null;
      }

      public IMergeRequestCache MergeRequestCache => _mergeRequestManager;

      public IDiscussionCache DiscussionCache => _discussionManager;

      public ITotalTimeCache TotalTimeCache => _timeTrackingManager;

      public IProjectCache ProjectCache => _projectCache;

      public IUserCache UserCache => _userCache;

      public IAvatarCache AvatarCache => _avatarCache;

      private MergeRequestManager _mergeRequestManager;
      private DiscussionManager _discussionManager;
      private TimeTrackingManager _timeTrackingManager;
      private ProjectCache _projectCache;
      private UserCache _userCache;
      private AvatarCache _avatarCache;
   }
}

