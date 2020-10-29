using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void searchMergeRequests(SearchQueryCollection queryCollection, EDataCacheType mode,
         Func<Exception, bool> exceptionHandler = null)
      {
         BeginInvoke(new Action(async () =>
            await searchMergeRequestsSafeAsync(queryCollection, mode, exceptionHandler)), null);
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
            new DataCacheUpdateRules(null, null),
            queryCollection);

         DataCache dataCache = getDataCache(mode);
         await dataCache.Connect(new GitLabInstance(hostname, Program.Settings), sessionContext);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

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

      private void onRecentDataCacheConnecting(string hostname)
      {
         getListView(EDataCacheType.Recent).Items.Clear();
         labelOperationStatus.Text = "Loading a list of recently reviewed merge requests...";
      }

      private void onRecentDataCacheConnected(string hostname, User user)
      {
         updateMergeRequestList(EDataCacheType.Recent);

         // current mode may have changed during 'await'
         if (getCurrentTabDataCacheType() == EDataCacheType.Recent)
         {
            getListView(EDataCacheType.Recent).SelectMergeRequest(new MergeRequestKey?(), false);
         }
      }
   }
}

