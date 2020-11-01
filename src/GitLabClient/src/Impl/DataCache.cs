using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Loaders;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Managers;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public class DataCache : IDisposable
   {
      public DataCache(DataCacheContext dataCacheContext, IModificationNotifier modificationNotifier)
      {
         _dataCacheContext = dataCacheContext;
         _modificationNotifier = modificationNotifier;
      }

      public event Action<string, User> Connected;
      public event Action<string> Connecting;
      public event Action Disconnected;

      async public Task Connect(GitLabInstance gitLabInstance, DataCacheConnectionContext context)
      {
         Disconnect();

         string hostname = gitLabInstance.HostName;
         IHostProperties hostProperties = gitLabInstance.HostProperties;
         _operator = new DataCacheOperator(hostname, hostProperties);

         try
         {
            Connecting?.Invoke(hostname);

            InternalCacheUpdater cacheUpdater = new InternalCacheUpdater(new InternalCache());
            IMergeRequestListLoader mergeRequestListLoader = new MergeRequestListLoader(
               hostname, _operator, new VersionLoader(_operator, cacheUpdater), cacheUpdater, context);

            Trace.TraceInformation("[DataCache] Connecting data cache to {0}...", hostname);
            ConnectionContext = context;

            string accessToken = hostProperties.GetAccessToken(hostname);
            await new CurrentUserLoader(_operator).Load(hostname, accessToken);
            User currentUser = GlobalCache.GetAuthenticatedUser(hostname, accessToken);

            await mergeRequestListLoader.Load();
            _internal = createCacheInternal(cacheUpdater, hostname, hostProperties, currentUser, context);

            Trace.TraceInformation("[DataCache] Data cache connected to {0}", hostname);
            Connected?.Invoke(hostname, currentUser);
         }
         catch (BaseLoaderException ex)
         {
            reset();

            if (ex is BaseLoaderCancelledException)
            {
               throw new DataCacheConnectionCancelledException();
            }
            throw new DataCacheException(ex.OriginalMessage, ex);
         }
      }

      public void Disconnect()
      {
         Trace.TraceInformation("[DataCache] Disconnecting data cache");
         reset();
      }

      public void Dispose()
      {
         Trace.TraceInformation("[DataCache] Disposing data cache");
         reset();
      }

      public IMergeRequestCache MergeRequestCache => _internal?.MergeRequestCache;

      public IDiscussionCache DiscussionCache => _internal?.DiscussionCache;

      public ITotalTimeCache TotalTimeCache => _internal?.TotalTimeCache;

      public IProjectCache ProjectCache => _internal?.ProjectCache;

      public IUserCache UserCache => _internal?.UserCache;

      public DataCacheConnectionContext ConnectionContext { get; private set; }

      private void reset()
      {
         _operator?.Dispose();
         _operator = null;

         _internal?.Dispose();
         _internal = null;

         ConnectionContext = null;

         Disconnected?.Invoke();
      }

      private DataCacheInternal createCacheInternal(
         InternalCacheUpdater cacheUpdater,
         string hostname,
         IHostProperties hostProperties,
         User user,
         DataCacheConnectionContext context)
      {
         MergeRequestManager mergeRequestManager = new MergeRequestManager(
            _dataCacheContext, cacheUpdater, hostname, hostProperties, context, _modificationNotifier);
         DiscussionManager discussionManager = new DiscussionManager(
            _dataCacheContext, hostname, hostProperties, user, mergeRequestManager, context, _modificationNotifier);
         TimeTrackingManager timeTrackingManager = new TimeTrackingManager(
            hostname, hostProperties, user, discussionManager, _modificationNotifier);

         IProjectListLoader loader = new ProjectListLoader(hostname, _operator);
         ProjectCache projectCache = new ProjectCache(loader, _dataCacheContext, hostname);
         IUserListLoader userListLoader = new UserListLoader(hostname, _operator);
         UserCache userCache = new UserCache(userListLoader, _dataCacheContext, hostname);
         return new DataCacheInternal(mergeRequestManager, discussionManager, timeTrackingManager, projectCache, userCache);
      }

      private DataCacheOperator _operator;
      private DataCacheInternal _internal;
      private readonly DataCacheContext _dataCacheContext;
      private readonly IModificationNotifier _modificationNotifier;
   }
}

