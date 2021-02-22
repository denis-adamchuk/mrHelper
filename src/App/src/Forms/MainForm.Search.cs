using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
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
         Trace.TraceInformation("[MainForm.Search] Loading recent merge requests from {0}", getHostName());
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
         setConnectionStatus(EConnectionState.ConnectingRecent);
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

         setConnectionStatus(EConnectionState.Connected);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void ensureMergeRequestInRecentDataCache(MergeRequestKey mrk)
      {
         DateTime currentTime = DateTime.Now;
         if (_recentMergeRequests.ContainsKey(mrk))
         {
            _recentMergeRequests[mrk] = currentTime;
            return;
         }

         _recentMergeRequests.Add(mrk, currentTime);

         bool needUpdateFullList = cleanupOldRecentMergeRequests(mrk.ProjectKey.HostName);
         saveState();

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
            _recentMergeRequests
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

