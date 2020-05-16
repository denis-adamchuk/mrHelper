using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Client.Common;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.Types;

namespace mrHelper.Client.Session
{
   internal class Session : ISession
   {
      internal Session(GitLabClientContext clientContext)
      {
         _clientContext = clientContext;
      }

      public event Action<string> Starting;
      public event Action<string, User, SessionContext> Started;

      async public Task<bool> Start(string hostname, SessionContext context)
      {
         await Stop();

         SessionOperator op = new SessionOperator(
            hostname, _clientContext.HostProperties.GetAccessToken(hostname));

         User currentUser = await new CurrentUserLoader(op).Load(hostname);
         if (currentUser == null)
         {
            return false;
         }

         InternalCacheUpdater cacheUpdater = new InternalCacheUpdater(new InternalCache());
         IMergeRequestListLoader mergeRequestListLoader =
            MergeRequestListLoaderFactory.CreateMergeRequestListLoader(op, context, cacheUpdater);

         Starting?.Invoke(hostname);

         if (await mergeRequestListLoader.Load())
         {
            _operator = op;
            _internal = createSessionInternal(cacheUpdater, hostname, currentUser, context);
            Started?.Invoke(hostname, currentUser, context);
            return true;
         }

         return false;
      }

      async public Task Stop()
      {
         if (_operator != null)
         {
            await _operator.CancelAsync();
            _operator = null;
         }

         _internal?.Dispose();
         _internal = null;
      }

      public ITimeTracker GetTimeTracker(MergeRequestKey mrk) =>
         _internal?.GetTimeTracker(mrk);

      public IDiscussionEditor GetDiscussionEditor(MergeRequestKey mrk, string discussionId) =>
         _internal?.GetDiscussionEditor(mrk, discussionId);

      public IDiscussionCreator GetDiscussionCreator(MergeRequestKey mrk) =>
         _internal?.GetDiscussionCreator(mrk);

      public IMergeRequestCache MergeRequestCache => _internal?.MergeRequestCache;

      public IDiscussionCache DiscussionCache => _internal?.DiscussionCache;

      public ITotalTimeCache TotalTimeCache => _internal?.TotalTimeCache;

      public IProjectUpdateContextProviderFactory UpdateContextProviderFactory =>
         _internal?.UpdateContextProviderFactory;

      private SessionInternal createSessionInternal(InternalCacheUpdater cacheUpdater,
         string hostname, User user, SessionContext context)
      {
         MergeRequestManager mergeRequestManager =
            new MergeRequestManager(_clientContext, cacheUpdater, hostname, context);
         DiscussionManager discussionManager =
            new DiscussionManager(_clientContext, user, mergeRequestManager, context);
         TimeTrackingManager timeTrackingManager =
            new TimeTrackingManager(_clientContext, user, discussionManager);
         return new SessionInternal(mergeRequestManager, discussionManager, timeTrackingManager);
      }

      private SessionOperator _operator;
      private SessionInternal _internal;
      private readonly GitLabClientContext _clientContext;
   }
}

