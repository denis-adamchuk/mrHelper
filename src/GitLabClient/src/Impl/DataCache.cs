using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient.Loaders;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Managers;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public class DataCache : IDisposable
   {
      public DataCache(DataCacheContext context)
      {
         _cacheContext = context;
      }

      public event Action<string, User> Connected;
      public event Action<string> Connecting;
      public event Action Disconnected;

      async public Task Connect(GitLabInstance gitLabInstance, DataCacheConnectionContext connectionContext)
      {
         assertNotConnected();

         string hostname = gitLabInstance.HostName;
         IHostProperties hostProperties = gitLabInstance.HostProperties;
         _operator = new DataCacheOperator(hostname, hostProperties, gitLabInstance.NetworkOperationStatusListener);
         DataCacheOperator myOperator = _operator;

         _isConnecting = true;
         try
         {
            Connecting?.Invoke(hostname);

            InternalCacheUpdater cacheUpdater = new InternalCacheUpdater(new InternalCache());
            bool isApprovalStatusSupported = await checkIsApprovalSupportedAsync(gitLabInstance);

            traceInformation(String.Format("Connecting data cache to {0}...", hostname));
            string accessToken = hostProperties.GetAccessToken(hostname);
            await new CurrentUserLoader(_operator).Load(hostname, accessToken);
            User currentUser = GlobalCache.GetAuthenticatedUser(hostname, accessToken);

            using (MergeRequestListLoader mergeRequestListLoader = new MergeRequestListLoader(
               hostname, _operator, cacheUpdater, _cacheContext.Callbacks, connectionContext.QueryCollection,
               isApprovalStatusSupported))
            {
               await mergeRequestListLoader.Load();
            }

            _internal = createCacheInternal(cacheUpdater, hostname, hostProperties, currentUser,
               connectionContext.QueryCollection, gitLabInstance.ModificationNotifier,
               gitLabInstance.NetworkOperationStatusListener, isApprovalStatusSupported);

            ConnectionContext = connectionContext;
            traceInformation(String.Format("Data cache connected to {0}", hostname));
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
         finally
         {
            _isConnecting = false;
         }
      }

      async private static Task<bool> checkIsApprovalSupportedAsync(GitLabInstance gitLabInstance)
      {
         RawDataAccessor rawDataAccessor = new RawDataAccessor(gitLabInstance);
         GitLabVersion gitLabVersion = await rawDataAccessor.VersionAccessor.GetGitLabVersionAsync();
         if (gitLabVersion == null)
         {
            Debug.Assert(false);
            return false;
         }
         return Helpers.DoesGitLabVersionSupportApprovals(gitLabVersion);
      }

      private void assertNotConnected()
      {
         Debug.Assert(!_isConnecting);
         Debug.Assert(_operator == null);
         Debug.Assert(_internal == null);
         Debug.Assert(ConnectionContext == null);
      }

      async public Task Disconnect()
      {
         if (_isConnecting)
         {
            traceInformation("Disconnecting data cache (async)");
            _operator?.Dispose(); // causes BaseLoaderCancelledException
            await TaskUtils.WhileAsync(() => _operator != null);
            assertNotConnected();
         }
         else
         {
            traceInformation("Disconnecting data cache (sync)");
            reset();
         }
      }

      public void Dispose()
      {
         traceInformation("Disposing data cache");
         reset();
      }

      public IMergeRequestCache MergeRequestCache => _internal?.MergeRequestCache;

      public IDiscussionCache DiscussionCache => _internal?.DiscussionCache;

      public ITotalTimeCache TotalTimeCache => _internal?.TotalTimeCache;

      public IProjectCache ProjectCache => _internal?.ProjectCache;

      public IUserCache UserCache => _internal?.UserCache;

      public IAvatarCache AvatarCache => _internal?.AvatarCache;

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

      private void traceInformation(string message)
      {
         Trace.TraceInformation("[DataCache.{0}] {1}", _cacheContext.TagForLogging, message);
      }

      private DataCacheInternal createCacheInternal(
         InternalCacheUpdater cacheUpdater,
         string hostname,
         IHostProperties hostProperties,
         User user,
         SearchQueryCollection queryCollection,
         IModificationNotifier modificationNotifier,
         INetworkOperationStatusListener networkOperationStatusListener,
         bool isApprovalStatusSupported)
      {
         MergeRequestManager mergeRequestManager = new MergeRequestManager(
            _cacheContext, cacheUpdater, hostname, hostProperties, queryCollection, networkOperationStatusListener,
            isApprovalStatusSupported);

         AvatarLoader avatarLoaderForDiscussionManager = new AvatarLoader(cacheUpdater);
         DiscussionManager discussionManager = new DiscussionManager(
            _cacheContext, hostname, hostProperties, user, mergeRequestManager,
            modificationNotifier, networkOperationStatusListener, avatarLoaderForDiscussionManager);

         TimeTrackingManager timeTrackingManager = new TimeTrackingManager(
            hostname, hostProperties, user, discussionManager, modificationNotifier, networkOperationStatusListener);

         ProjectCache projectCache = null;
         if (_cacheContext.SupportProjectCache)
         {
            IProjectListLoader loader = new ProjectListLoader(hostname, _operator);
            projectCache = new ProjectCache(loader, _cacheContext, hostname);
         }

         UserCache userCache = null;
         if (_cacheContext.SupportUserCache)
         {
            AvatarLoader avatarLoaderForUserListLoader = new AvatarLoader(cacheUpdater);
            UserListLoader userListLoader = new UserListLoader(hostname, _operator, avatarLoaderForUserListLoader);
            userCache = new UserCache(userListLoader, _cacheContext, hostname);
         }

         AvatarCache avatarCache = new AvatarCache(cacheUpdater.Cache);
         return new DataCacheInternal(mergeRequestManager, discussionManager,
            timeTrackingManager, projectCache, userCache, avatarCache);
      }

      private bool _isConnecting;
      private DataCacheOperator _operator;
      private DataCacheInternal _internal;
      private readonly DataCacheContext _cacheContext;
   }
}

