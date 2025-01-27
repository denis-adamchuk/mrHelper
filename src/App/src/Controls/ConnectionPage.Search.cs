using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
   {
      private void searchMergeRequests(SearchQueryCollection queryCollection)
      {
         BeginInvoke(new Action(async () =>
            await searchMergeRequestsSafeAsync(queryCollection, EDataCacheType.Search, null)), null);
      }

      private void loadRecentMergeRequests()
      {
         Trace.TraceInformation("[MainForm.Search] Loading recent merge requests from {0}", HostName);
         IEnumerable<SearchQuery> queries = convertRecentMergeRequestsToSearchQueries(HostName);
         BeginInvoke(new Action(async () =>
            await searchMergeRequestsSafeAsync(new SearchQueryCollection(queries), EDataCacheType.Recent, null)), null);
      }

      async private Task searchMergeRequestsSafeAsync(SearchQueryCollection queryCollection, EDataCacheType mode,
         Func<Exception, bool> exceptionHandler = null)
      {
         try
         {
            await searchMergeRequestsAsync(HostName, queryCollection, mode);
         }
         catch (Exception ex) // rethrow in case of unexpected exceptions
         {
            enableSearchTabControls();
            if (exceptionHandler == null)
            {
               exceptionHandler = new Func<Exception, bool>((e) => startWorkflowDefaultExceptionHandler(e));
            }
            if (!exceptionHandler(ex))
            {
               throw;
            }
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      async private Task searchMergeRequestsAsync(string hostname, SearchQueryCollection queryCollection,
         EDataCacheType mode)
      {
         if (String.IsNullOrWhiteSpace(hostname) || getDataCache(mode) == null)
         {
            return;
         }

         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         await connectSearchDataCacheAsync(queryCollection, mode);
      }

      async private Task connectSearchDataCacheAsync(SearchQueryCollection queryCollection, EDataCacheType mode)
      {
         DataCache dataCache = getDataCache(mode);
         await dataCache.Disconnect();
         if (_gitLabInstance != null)
         {
            await dataCache.Connect(_gitLabInstance, new DataCacheConnectionContext(queryCollection));
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onSearchDataCacheDisconnected()
      {
         disableSearchTabControls();
         unsubscribeFromSearchDataCacheInternalEvents();
      }

      private void onSearchDataCacheConnecting(string hostname)
      {
         getListView(EDataCacheType.Search).Items.Clear();
         addOperationRecord(String.Format("Search at {0} has started", hostname));
      }

      private void onSearchDataCacheConnected(string hostname, User user)
      {
         subscribeToSearchDataCacheInternalEvents();
         updateMergeRequestList(EDataCacheType.Search);
         enableSearchTabControls();

         bool areResults = getListView(EDataCacheType.Search).Items.Count > 0;
         addOperationRecord(areResults ? "Search has finished" : "Nothing found. Try changing search query.");
      }

      private void onRecentDataCacheDisconnected()
      {
         disableRecentTabControls();
         unsubscribeFromRecentDataCacheInternalEvents();
      }

      private void onRecentDataCacheConnecting(string hostname)
      {
         getListView(EDataCacheType.Recent).Items.Clear();
         addOperationRecord("Loading a list of recently reviewed merge requests has started");
         setConnectionStatus(EConnectionStateInternal.ConnectingRecent);
      }

      private void onRecentDataCacheConnected(string hostname, User user)
      {
         subscribeToRecentDataCacheInternalEvents();
         updateMergeRequestList(EDataCacheType.Recent);

         addOperationRecord("List of recently reviewed merge requests has been loaded");

         IEnumerable<int> excludedMergeRequestIds = getExcludedMergeRequestIds(EDataCacheType.Recent);
         IEnumerable<int> oldExcludedIds = selectNotCachedMergeRequestIds(EDataCacheType.Recent, excludedMergeRequestIds);
         if (oldExcludedIds.Any())
         {
            Trace.TraceInformation("[ConnectionPage] Excluded Merge Requests are no longer in the cache {1}: {0}",
               String.Join(", ", oldExcludedIds), getDataCacheName(getDataCache(EDataCacheType.Recent)));
            toggleMergeRequestsExclusion(EDataCacheType.Recent, oldExcludedIds);
         }

         setConnectionStatus(EConnectionStateInternal.Connected);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void ensureMergeRequestInRecentDataCache(MergeRequestKey mrk)
      {
         DateTime currentTime = DateTime.Now;
         if (_recentMergeRequests.Data.ContainsKey(mrk))
         {
            _recentMergeRequests[mrk] = currentTime;
            return;
         }

         _recentMergeRequests.Add(mrk, currentTime);

         bool needUpdateFullList = cleanupOldRecentMergeRequests(mrk.ProjectKey.HostName);

         updateRecentDataCacheQueryColletion(mrk.ProjectKey.HostName);
         MergeRequestKey? keyForUpdate = needUpdateFullList ? new Nullable<MergeRequestKey>() : mrk;
         requestUpdates(EDataCacheType.Recent, keyForUpdate, new[] { PseudoTimerInterval });
      }

      private void updateRecentDataCacheQueryColletion(string hostname)
      {
         IEnumerable<SearchQuery> queries = convertRecentMergeRequestsToSearchQueries(hostname);
         getDataCache(EDataCacheType.Recent)?.ConnectionContext?.QueryCollection.Assign(queries);
      }

      private IEnumerable<SearchQuery> convertRecentMergeRequestsToSearchQueries(string hostname)
      {
         return _recentMergeRequests.Data
            .Where(key => key.Key.ProjectKey.HostName == hostname)
            .Select(key => new GitLabClient.SearchQuery
            {
               IId = key.Key.IId,
               ProjectName = key.Key.ProjectKey.ProjectName
            })
            .ToArray();
      }

      private bool cleanupOldRecentMergeRequests(string hostname)
      {
         if (String.IsNullOrEmpty(hostname))
         {
            return false;
         }

         if (!int.TryParse(Program.Settings.RecentMergeRequestsPerProjectCount, out int mergeRequestsPerProject))
         {
            mergeRequestsPerProject = Constants.RecentMergeRequestPerProjectDefaultCount;
         }

         bool changed = false;
         IEnumerable<IGrouping<ProjectKey, KeyValuePair<MergeRequestKey, DateTime>>> groups =
            _recentMergeRequests.Data
            .Where(key => key.Key.ProjectKey.HostName == hostname)
            .GroupBy(key => key.Key.ProjectKey);
         foreach (IGrouping<ProjectKey, KeyValuePair<MergeRequestKey, DateTime>> group in groups)
         {
            IEnumerable<KeyValuePair<MergeRequestKey, DateTime>> groupedKeys = group.AsEnumerable();
            if (groupedKeys.Any())
            {
               IEnumerable<MergeRequestKey> oldMergeRequests = groupedKeys
                  .OrderByDescending(kv => kv.Value)
                  .Skip(mergeRequestsPerProject)
                  .Select(kv => kv.Key)
                  .ToArray(); // copy
               foreach (MergeRequestKey mergeRequestKey in oldMergeRequests)
               {
                  _recentMergeRequests.Remove(mergeRequestKey);
                  changed = true;
               }
            }
         }
         return changed;
      }

   }
}
