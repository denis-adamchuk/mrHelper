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

      public event Action<string> Starting;
      public event Action<string, User, ISessionContext, ISession> Started;

      async public Task<bool> Start(string hostname, ISessionContext context)
      {
         await Stop();

         _operator = new SessionOperator(hostname, _clientContext.HostProperties.GetAccessToken(hostname));

         User? currentUser = await loadCurrentUserAsync(hostname);
         if (!currentUser.HasValue)
         {
            return false;
         }

         InternalCacheUpdater cacheUpdater = new InternalCacheUpdater(new InternalCache());
         IMergeRequestListLoader mergeRequestListLoader =
            MergeRequestListLoaderFactory.CreateMergeRequestListLoader(
               _clientContext, _operator, context, cacheUpdater);

         Starting?.Invoke(hostname);

         if (await mergeRequestListLoader.Load(context))
         {
            _internal = createWorkflow(cacheUpdater, hostname, currentUser.Value, context);
            Started?.Invoke(hostname, currentUser.Value, context, this);
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

      async private Task<User?> loadCurrentUserAsync(string hostName)
      {
         try
         {
            return await _operator.GetCurrentUserAsync();
         }
         catch (OperatorException ex)
         {
            string cancelMessage = String.Format("Cancelled loading current user from host \"{0}\"", hostName);
            string errorMessage = String.Format("Cannot load user from host \"{0}\"", hostName);

            bool cancelled = ex.InnerException is GitLabSharp.GitLabClientCancelled;
            if (cancelled)
            {
               Trace.TraceInformation(String.Format("[WorkflowLoader] {0}", cancelMessage));
               return null;
            }

            throw new WorkflowException(errorMessage, ex);
         }
      }

      private SessionInternal createWorkflow(InternalCacheUpdater cacheUpdater,
         string hostname, User user, ISessionContext context)
      {
         MergeRequestManager mergeRequestManager =
            new MergeRequestManager(_clientContext, this, cacheUpdater, hostname, context);
         DiscussionManager discussionManager =
            new DiscussionManager(_clientContext, user, mergeRequestManager);
         TimeTrackingManager timeTrackingManager =
            new TimeTrackingManager(_clientContext, user, discussionManager);
         return new SessionInternal(mergeRequestManager, discussionManager, timeTrackingManager);
      }

      private SessionOperator _operator;
      private SessionInternal _internal;
      private readonly GitLabClientContext _clientContext;
   }
}

