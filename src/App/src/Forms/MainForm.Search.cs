using System;
using System.Threading.Tasks;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void searchMergeRequests(SearchQueryCollection queryCollection, ECurrentMode mode,
         Func<Exception, bool> exceptionHandler = null)
      {
         BeginInvoke(new Action(async () =>
            await searchMergeRequestsSafeAsync(queryCollection, mode, exceptionHandler)), null);
      }

      async private Task searchMergeRequestsSafeAsync(SearchQueryCollection queryCollection, ECurrentMode mode,
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
         ECurrentMode mode)
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
         ECurrentMode mode)
      {
         onSearchDataCacheConnecting(queryCollection, hostname, mode);

         DataCacheConnectionContext sessionContext = new DataCacheConnectionContext(
            new DataCacheCallbacks(null, null),
            new DataCacheUpdateRules(null, null),
            queryCollection);

         DataCache dataCache = getDataCache(mode);
         await dataCache.Connect(new GitLabInstance(hostname, Program.Settings), sessionContext);

         onSearchDataCacheConnected(mode);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onSearchDataCacheConnecting(SearchQueryCollection queryCollection, string hostname, ECurrentMode mode)
      {
         getListView(mode).Items.Clear();
         labelOperationStatus.Text = String.Format("Search in progress at {0}...", hostname);
      }

      private void onSearchDataCacheConnected(ECurrentMode mode)
      {
         bool areResults = getListView(mode).Items.Count > 0;
         labelOperationStatus.Text = areResults ? String.Empty : "Nothing found. Try more specific search query.";

         updateMergeRequestList(mode);

         // current mode may have changed during 'await'
         if (getMode() == ECurrentMode.Search)
         {
            getListView(mode).SelectMergeRequest(new MergeRequestKey?(), false);
         }
      }
   }
}

