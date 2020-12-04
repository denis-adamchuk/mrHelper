using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.CommonControls.Tools;
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

            default:
               Debug.Assert(false);
               break;
         }
         return null;
      }

      private void onSearchDataCacheDisconnected()
      {
         disableSearchTabControls();
      }

      private void onSearchDataCacheConnecting(string hostname)
      {
         getListView(EDataCacheType.Search).Items.Clear();
         addOperationRecord(String.Format("Search at {0} has started", hostname));
      }

      private void onSearchDataCacheConnected(string hostname, User user)
      {
         updateMergeRequestList(EDataCacheType.Search);
         enableSimpleSearchControls(true);
         setSearchByAuthorEnabled(getDataCache(EDataCacheType.Live)?.UserCache?.GetUsers()?.Any() ?? false);
         setSearchByProjectEnabled(getDataCache(EDataCacheType.Live)?.ProjectCache?.GetProjects()?.Any() ?? false);
         updateSearchButtonState();

         bool areResults = getListView(EDataCacheType.Search).Items.Count > 0;
         addOperationRecord(areResults ? "Search has finished" : "Nothing found. Try changing search query.");

         // current mode may have changed during 'await'
         if (getCurrentTabDataCacheType() == EDataCacheType.Search)
         {
            getListView(EDataCacheType.Search).SelectMergeRequest(new MergeRequestKey?(), false);
         }
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
      }

      private void onRecentDataCacheConnected(string hostname, User user)
      {
         subscribeToRecentDataCacheInternalEvents();
         updateMergeRequestList(EDataCacheType.Recent);

         addOperationRecord("List of recently reviewed merge requests has been loaded");

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
         getDataCache(EDataCacheType.Recent)?.ConnectionContext?.QueryCollection.Assign(queries);
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

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void setSearchByProjectEnabled(bool isEnabled)
      {
         checkBoxSearchByProject.Enabled = isEnabled;

         bool wasEnabled = comboBoxProjectName.Enabled;
         comboBoxProjectName.Enabled = isEnabled;

         if (!wasEnabled && isEnabled)
         {
            DataCache dataCache = getDataCache(EDataCacheType.Live);
            string[] projectNames = dataCache?.ProjectCache?.GetProjects()
               .OrderBy(project => project.Path_With_Namespace)
               .Select(project => project.Path_With_Namespace)
               .ToArray() ?? Array.Empty<string>();
            string selectedProject = (string)comboBoxProjectName.SelectedItem;
            string previousSelection = selectedProject ?? String.Empty;
            string defaultProjectName = projectNames.SingleOrDefault(name => name == previousSelection) == null
               ? getDefaultProjectName() : previousSelection;
            WinFormsHelpers.FillComboBox(comboBoxProjectName, projectNames,
               projectName => projectName == defaultProjectName);
         }

         updateSearchButtonState();
      }

      private void setSearchByAuthorEnabled(bool isEnabled)
      {
         checkBoxSearchByAuthor.Enabled = isEnabled;
         linkLabelFindMe.Enabled = isEnabled;

         bool wasEnabled = comboBoxUser.Enabled;
         comboBoxUser.Enabled = isEnabled;

         if (!wasEnabled && isEnabled)
         {
            DataCache dataCache = getDataCache(EDataCacheType.Live);
            User[] users = dataCache?.UserCache?.GetUsers()
               .OrderBy(user => user.Name).ToArray() ?? Array.Empty<User>();
            User selectedUser = (User)comboBoxUser.SelectedItem;
            string previousSelection = selectedUser == null ? String.Empty : selectedUser.Name;
            string defaultUserFullName = users.SingleOrDefault(user => user.Name == previousSelection) == null
               ? getCurrentUser().Name : previousSelection;
            WinFormsHelpers.FillComboBox(comboBoxUser, users, user => user.Name == defaultUserFullName);
         }

         updateSearchButtonState();
      }
   }
}

