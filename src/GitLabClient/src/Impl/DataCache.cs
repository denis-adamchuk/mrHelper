using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient.Accessors;
using mrHelper.GitLabClient.Loaders;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Managers;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient
{
   public class DataCache : IDisposable
   {
      public DataCache(DataCacheContext dataCacheContext)
      {
         _dataCacheContext = dataCacheContext;
      }

      public event Action<string, User> Connected;
      public event Action Disconnected;

      async public Task Connect(GitLabInstance gitLabInstance, DataCacheConnectionContext context)
      {
         _modificationNotifier = gitLabInstance.ModificationNotifier;

         reset();

         string hostname = gitLabInstance.HostName;
         IHostProperties hostProperties = gitLabInstance.HostProperties;
         _operator = new DataCacheOperator(hostname, hostProperties);

         try
         {
            InternalCacheUpdater cacheUpdater = new InternalCacheUpdater(new InternalCache());
            IMergeRequestListLoader mergeRequestListLoader =
               MergeRequestListLoaderFactory.CreateMergeRequestListLoader(hostname, _operator, context, cacheUpdater);

            Trace.TraceInformation(String.Format("[DataCache] Starting new dataCache at {0}", hostname));

            User currentUser = await new CurrentUserLoader(_operator).Load(hostname);
            await mergeRequestListLoader.Load();
            _internal = createSessionInternal(cacheUpdater, hostname, hostProperties, currentUser, context);

            Trace.TraceInformation(String.Format("[DataCache] Started new dataCache at {0}", hostname));
            Connected?.Invoke(hostname, currentUser);
         }
         catch (BaseLoaderException ex)
         {
            if (ex is BaseLoaderCancelledException)
            {
               throw new DataCacheConnectionCancelledException();
            }
            throw new DataCacheException(ex.OriginalMessage, ex);
         }
      }

      public void Disconnect()
      {
         reset();
      }

      public void Dispose()
      {
         reset();
      }

      private void reset()
      {
         Trace.TraceInformation("[Session] Canceling operations");

         _operator?.Dispose();
         _operator = null;

         _internal?.Dispose();
         _internal = null;

         Disconnected?.Invoke();
      }

      public IMergeRequestCache MergeRequestCache => _internal?.MergeRequestCache;

      public IDiscussionCache DiscussionCache => _internal?.DiscussionCache;

      public ITotalTimeCache TotalTimeCache => _internal?.TotalTimeCache;

      private DataCacheInternal createSessionInternal(
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
         return new DataCacheInternal(mergeRequestManager, discussionManager, timeTrackingManager);
      }

      private DataCacheOperator _operator;
      private DataCacheInternal _internal;
      private readonly DataCacheContext _dataCacheContext;
      private IModificationNotifier _modificationNotifier;
   }
}

