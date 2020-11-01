using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void searchMergeRequests(SearchQueryCollection queryCollection)
      {
         BeginInvoke(new Action(async () =>
            await searchMergeRequestsSafeAsync(queryCollection, EDataCacheType.Search, null)), null);
      }

      private void loadRecentMergeRequests()
      {
         IEnumerable<SearchQuery> queries = convertRecentMergeRequestsToSearchQueries(getHostName());
         BeginInvoke(new Action(async () =>
            await searchMergeRequestsSafeAsync(new SearchQueryCollection(queries), EDataCacheType.Recent, null)), null);
      }

      async private Task searchMergeRequestsSafeAsync(SearchQueryCollection queryCollection, EDataCacheType mode,
         Func<Exception, bool> exceptionHandler = null)
      {
         try
         {
            await searchMergeRequestsAsync(getHostName(), queryCollection, mode);
         }
         catch (Exception ex)
         {
            if (exceptionHandler == null)
            {
               exceptionHandler = new Func<Exception, bool>((e) => startWorkflowDefaultExceptionHandler(e, mode));
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
         labelOperationStatus.Text = String.Empty;
         disableAllUIControls(true, mode);

         if (String.IsNullOrWhiteSpace(hostname))
         {
            return;
         }

         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         await connectSearchDataCacheAsync(hostname, queryCollection, mode);
      }

      async private Task connectSearchDataCacheAsync(string hostname, SearchQueryCollection queryCollection,
         EDataCacheType mode)
      {
         DataCacheConnectionContext sessionContext = new DataCacheConnectionContext(
            new DataCacheCallbacks(null, null),
            getDataCacheUpdateRules(mode),
            queryCollection);

         DataCache dataCache = getDataCache(mode);
         await dataCache.Connect(new GitLabInstance(hostname, Program.Settings), sessionContext);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private DataCacheUpdateRules getDataCacheUpdateRules(EDataCacheType mode)
      {
         switch (mode)
         {
            case EDataCacheType.Recent:
               return new DataCacheUpdateRules(Program.Settings.AutoUpdatePeriodMs,
                                               Program.Settings.AutoUpdatePeriodMs,
                                               false);

            case EDataCacheType.Search:
               return new DataCacheUpdateRules(null, null, false);
         }

         Debug.Assert(false);
         return null;
      }

      private void onSearchDataCacheConnecting(string hostname)
      {
         getListView(EDataCacheType.Search).Items.Clear();
         labelOperationStatus.Text = String.Format("Search in progress at {0}...", hostname);
      }

      private void onSearchDataCacheConnected(string hostname, User user)
      {
         updateMergeRequestList(EDataCacheType.Search);

         bool areResults = getListView(EDataCacheType.Search).Items.Count > 0;
         labelOperationStatus.Text = areResults ? String.Empty : "Nothing found. Try more specific search query.";

         // current mode may have changed during 'await'
         if (getCurrentTabDataCacheType() == EDataCacheType.Search)
         {
            getListView(EDataCacheType.Search).SelectMergeRequest(new MergeRequestKey?(), false);
         }
      }

      private void onRecentDataCacheDisconnected()
      {
         unsubscribeFromRecentDataCacheInternalEvents();
      }

      private void onRecentDataCacheConnecting(string hostname)
      {
         getListView(EDataCacheType.Recent).Items.Clear();
         labelOperationStatus.Text = "Loading a list of recently reviewed merge requests...";
      }

      private void onRecentDataCacheConnected(string hostname, User user)
      {
         subscribeToRecentDataCacheInternalEvents();
         updateMergeRequestList(EDataCacheType.Recent);

         labelOperationStatus.Text = String.Empty;

         // current mode may have changed during 'await'
         if (getCurrentTabDataCacheType() == EDataCacheType.Recent)
         {
            getListView(EDataCacheType.Recent).SelectMergeRequest(new MergeRequestKey?(), false);
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void addMergeRequestToRecentDataCache(MergeRequestKey mrk)
      {
         MergeRequestKey[] closedMergeRequests = new MergeRequestKey[] { mrk };
         addRecentMergeRequestKeys(closedMergeRequests);
         bool needUpdateFullList = cleanupOldRecentMergeRequests(mrk.ProjectKey.HostName);
         updateRecentDataCacheQueryColletion(mrk.ProjectKey.HostName);
         MergeRequestKey? keyForUpdate = needUpdateFullList ? new Nullable<MergeRequestKey>() : mrk;
         requestUpdates(EDataCacheType.Recent, keyForUpdate, new[] { PseudoTimerInterval });
      }

      private void updateRecentDataCacheQueryColletion(string hostname)
      {
         IEnumerable<SearchQuery> queries = convertRecentMergeRequestsToSearchQueries(hostname);
         _recentDataCache?.ConnectionContext?.QueryCollection.Assign(queries);
      }

      private IEnumerable<SearchQuery> convertRecentMergeRequestsToSearchQueries(string hostname)
      {
         return _recentMergeRequests
            .Where(key => key.ProjectKey.HostName == hostname)
            .Select(key => new GitLabClient.SearchQuery
            {
               IId = key.IId,
               ProjectName = key.ProjectKey.ProjectName
            })
            .ToArray();
      }
   }
}

