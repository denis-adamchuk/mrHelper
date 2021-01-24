﻿using System;
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
      public DataCache(DataCacheContext context)
      {
         _cacheContext = context;
      }

      public event Action<string, User> Connected;
      public event Action<string> Connecting;
      public event Action Disconnected;

      async public Task Connect(GitLabInstance gitLabInstance, DataCacheConnectionContext connectionContext)
      {
         reset();

         string hostname = gitLabInstance.HostName;
         IHostProperties hostProperties = gitLabInstance.HostProperties;
         _operator = new DataCacheOperator(hostname, hostProperties, gitLabInstance.NetworkOperationStatusListener);

         Connecting?.Invoke(hostname);
         traceInformation(String.Format("Connecting data cache to {0}...", hostname));

         InternalCacheUpdater cacheUpdater = new InternalCacheUpdater(new InternalCache());
         IMergeRequestListLoader mergeRequestListLoader = new MergeRequestListLoader(
            hostname, _operator, new VersionLoader(_operator, cacheUpdater), cacheUpdater,
            _cacheContext.Callbacks, connectionContext.QueryCollection);

         string accessToken = hostProperties.GetAccessToken(hostname);
         try
         {
            await new CurrentUserLoader(_operator).Load(hostname, accessToken);
            await mergeRequestListLoader.Load();
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

         User currentUser = GlobalCache.GetAuthenticatedUser(hostname, accessToken);
         _internal = createCacheInternal(cacheUpdater, hostname, hostProperties, currentUser,
            connectionContext.QueryCollection, gitLabInstance.ModificationNotifier,
            gitLabInstance.NetworkOperationStatusListener);

         ConnectionContext = connectionContext;
         traceInformation(String.Format("Data cache connected to {0}", hostname));
         Connected?.Invoke(hostname, currentUser);
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
         INetworkOperationStatusListener networkOperationStatusListener)
      {
         MergeRequestManager mergeRequestManager = new MergeRequestManager(
            _cacheContext, cacheUpdater, hostname, hostProperties, queryCollection, networkOperationStatusListener);
         DiscussionManager discussionManager = new DiscussionManager(
            _cacheContext, hostname, hostProperties, user, mergeRequestManager,
            modificationNotifier, networkOperationStatusListener);
         TimeTrackingManager timeTrackingManager = new TimeTrackingManager(
            hostname, hostProperties, user, discussionManager, modificationNotifier, networkOperationStatusListener);

         IProjectListLoader loader = new ProjectListLoader(hostname, _operator);
         ProjectCache projectCache = new ProjectCache(loader, _cacheContext, hostname);
         IUserListLoader userListLoader = new UserListLoader(hostname, _operator);
         UserCache userCache = new UserCache(userListLoader, _cacheContext, hostname);
         return new DataCacheInternal(mergeRequestManager, discussionManager, timeTrackingManager, projectCache, userCache);
      }

      private DataCacheOperator _operator;
      private DataCacheInternal _internal;
      private readonly DataCacheContext _cacheContext;
   }
}

