using System;
using System.Diagnostics;
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

      public event Action Stopped;
      public event Action<string, User> Started;

      async public Task Start(string hostname, SessionContext context)
      {
         stop();

         _operator = new SessionOperator(hostname, _clientContext.HostProperties);

         try
         {
            InternalCacheUpdater cacheUpdater = new InternalCacheUpdater(new InternalCache());
            IMergeRequestListLoader mergeRequestListLoader =
               MergeRequestListLoaderFactory.CreateMergeRequestListLoader(hostname, _operator, context, cacheUpdater);

            Trace.TraceInformation(String.Format("[Session] Starting new session at {0}", hostname));

            User currentUser = await new CurrentUserLoader(_operator).Load(hostname);
            await mergeRequestListLoader.Load();
            _internal = createSessionInternal(cacheUpdater, hostname, currentUser, context);

            Trace.TraceInformation(String.Format("[Session] Started new session at {0}", hostname));
            Started?.Invoke(hostname, currentUser);
         }
         catch (BaseLoaderException ex)
         {
            if (ex is BaseLoaderCancelledException)
            {
               throw new SessionStartCancelledException();
            }
            throw new SessionException(ex.OriginalMessage, ex);
         }
      }

      public void Dispose()
      {
         stop();
      }

      private void stop()
      {
         Trace.TraceInformation("[Session] Canceling operations");

         _operator?.Dispose();
         _operator = null;

         _internal?.Dispose();
         _internal = null;

         Stopped?.Invoke();
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
            new DiscussionManager(_clientContext, hostname, user, mergeRequestManager, context);
         TimeTrackingManager timeTrackingManager =
            new TimeTrackingManager(_clientContext, hostname, user, discussionManager);
         return new SessionInternal(mergeRequestManager, discussionManager, timeTrackingManager);
      }

      private SessionOperator _operator;
      private SessionInternal _internal;
      private readonly GitLabClientContext _clientContext;
   }
}

