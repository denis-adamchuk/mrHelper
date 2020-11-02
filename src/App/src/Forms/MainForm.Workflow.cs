using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Constants;
using mrHelper.GitLabClient;
using mrHelper.App.Helpers.GitLab;
using mrHelper.CommonControls.Tools;
using SearchQuery = mrHelper.GitLabClient.SearchQuery;
using mrHelper.Common.Tools;

namespace mrHelper.App.Forms
{
   internal class UnknownHostException : Exception
   {
      internal UnknownHostException(string hostname): base(
         String.Format("Cannot find access token for host {0}", hostname)) {}
   }

   internal class NoProjectsException : Exception
   {
      internal NoProjectsException(string hostname): base(
         String.Format("Project list for hostname {0} is empty", hostname)) {}
   }

   internal partial class MainForm
   {
      private bool startWorkflowDefaultExceptionHandler(Exception ex)
      {
         if (ex is DataCacheException || ex is UnknownHostException || ex is NoProjectsException)
         {
            if (!(ex is DataCacheConnectionCancelledException))
            {
               ExceptionHandlers.Handle("Cannot switch host", ex);
               string message = ex.Message;
               if (ex is DataCacheException wx)
               {
                  message = wx.UserMessage;
               }
               labelOperationStatus.Text = message;
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return true;
         }
         return false;
      }

      async private Task switchHostToSelectedAsync(Func<Exception, bool> exceptionHandler)
      {
         updateTabControlSelection();
         try
         {
            await startWorkflowAsync(getHostName());
         }
         catch (Exception ex)
         {
            dropConnectionToHost();
            if (exceptionHandler == null)
            {
               exceptionHandler = new Func<Exception, bool>(e => startWorkflowDefaultExceptionHandler(e));
            }
            if (!exceptionHandler(ex))
            {
               throw;
            }
         }
      }

      private void dropConnectionToHost()
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            getDataCache(mode).Disconnect();
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      async private Task startWorkflowAsync(string hostname)
      {
         // When this thing happens, everything reconnects. If there are some things at gitlab that user
         // wants to be notified about and we did not cache them yet (e.g. mentions in discussions)
         // we will miss them. It might be ok when host changes, but if this method used to "refresh"
         // things, missed events are not desirable.
         // This is why "Update List" button implemented not by means of switchHostToSelected().

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Starting workflow at host {0}. Workflow type is {1}",
            hostname, Program.Settings.WorkflowType));

         dropConnectionToHost();
         labelOperationStatus.Text = String.Format("Connecting to {0}...", hostname);

         if (String.IsNullOrWhiteSpace(hostname))
         {
            return;
         }

         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            initializeProjectListIfEmpty(hostname);
            await upgradeProjectListFromOldVersion(hostname);
            await startProjectBasedWorkflowAsync(hostname);
         }
         else
         {
            await initializeLabelListIfEmpty(hostname);
            await startUserBasedWorkflowAsync(hostname);
         }
      }

      private async Task startProjectBasedWorkflowAsync(string hostname)
      {
         IEnumerable<ProjectKey> projects = getEnabledProjects(hostname);
         SearchQueryCollection queryCollection = getCustomDataForProjectBasedWorkflow(projects);
         await connectLiveDataCacheAsync(hostname, queryCollection);
      }

      private IEnumerable<ProjectKey> getEnabledProjects(string hostname)
      {
         IEnumerable<ProjectKey> enabledProjects =
            ConfigurationHelper.GetEnabledProjectNames(hostname, Program.Settings)
            .Select(x => new ProjectKey(hostname, x));
         if (!enabledProjects.Any())
         {
            throw new NoProjectsException(hostname);
         }
         return enabledProjects;
      }

      private async Task startUserBasedWorkflowAsync(string hostname)
      {
         IEnumerable<string> usernames = listViewUsers.Items.Cast<ListViewItem>().Select(item => item.Text);
         SearchQueryCollection queryCollection = getCustomDataForUserBasedWorkflow(usernames);
         await connectLiveDataCacheAsync(hostname, queryCollection);
      }

      async private Task connectLiveDataCacheAsync(string hostname, SearchQueryCollection queryCollection)
      {
         // The idea is that:
         // 1. Already cached MR that became closed remotely will not be removed from the cache
         // 2. Open MR that are missing in the cache, will be added to the cache
         // 3. Open MR that exist in the cache, will be updated
         // 4. Non-cached MR that are closed remotely, will not be added to the cache even if directly requested by IId
         bool updateOnlyOpened = true;

         DataCacheConnectionContext connectionContext = new DataCacheConnectionContext(
            new DataCacheCallbacks(onForbiddenProject, onNotFoundProject),
            new DataCacheUpdateRules(Program.Settings.AutoUpdatePeriodMs, Program.Settings.AutoUpdatePeriodMs,
               updateOnlyOpened),
            queryCollection);

         DataCache dataCache = getDataCache(EDataCacheType.Live);
         await dataCache.Connect(new GitLabInstance(hostname, Program.Settings), connectionContext);
      }

      private void onForbiddenProject(ProjectKey projectKey)
      {
         string message = String.Format(
            "You don't have access to project {0} at {1}. "
          + "Loading of this project will be disabled. You may turn it on at Settings tab.",
            projectKey.ProjectName, projectKey.HostName);
         MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
         Trace.TraceInformation("[MainForm.Workflow] Forbidden project. User notified that project will be disabled");

         changeProjectEnabledState(projectKey, false);
      }

      private void onNotFoundProject(ProjectKey projectKey)
      {
         string message = String.Format(
            "There is no project {0} at {1}. "
          + "Loading of this project will be disabled. You may turn it on at Settings tab.",
            projectKey.ProjectName, projectKey.HostName);
         MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
         Trace.TraceInformation("[MainForm.Workflow] Project not found. User notified that project will be disabled");

         changeProjectEnabledState(projectKey, false);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLiveDataCacheDisconnected()
      {
         disableLiveTabControls();
         stopListViewRefreshTimer();
         WinFormsHelpers.CloseAllFormsExceptOne(this);
         disposeGitHelpers();
         disposeLocalGitRepositoryFactory();
         unsubscribeFromLiveDataCacheInternalEvents();
      }

      private void onLiveDataCacheConnecting(string hostname)
      {
         if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            // in Project-based workflow we want to create all groups at once in a user-defined order
            Controls.MergeRequestListView listView = getListView(EDataCacheType.Live);
            listView.Items.Clear();
            listView.Groups.Clear();

            IEnumerable<ProjectKey> projects = getEnabledProjects(hostname);
            foreach (ProjectKey projectKey in projects)
            {
               listView.CreateGroupForProject(projectKey, false);
            }

            labelOperationStatus.Text = String.Format("Loading merge requests of {0} project{1} from {2}...",
               projects.Count(), projects.Count() > 1 ? "s" : "", hostname);
         }
         else
         {
            labelOperationStatus.Text = String.Format("Loading merge requests from {0}...", hostname);
         }
      }

      private void onLiveDataCacheConnected(string hostname, User user)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         subscribeToLiveDataCacheInternalEvents();
         createGitHelpers(dataCache, getCommitStorageFactory(false));

         if (!_currentUser.ContainsKey(hostname))
         {
            _currentUser.Add(hostname, user);
         }
         Program.FeedbackReporter.SetUserEMail(user.EMail);
         startListViewRefreshTimer();
         startEventPendingTimer(() => (dataCache?.ProjectCache?.GetProjects()?.Any() ?? false)
                                   && (dataCache?.UserCache?.GetUsers()?.Any() ?? false),
                                ProjectAndUserCacheCheckTimerInterval,
                                () => setMergeRequestEditEnabled(true));
         startEventPendingTimer(() => (dataCache?.ProjectCache?.GetProjects()?.Any() ?? false),
                                ProjectAndUserCacheCheckTimerInterval,
                                () => setSearchByProjectEnabled(true));
         startEventPendingTimer(() => (dataCache?.UserCache?.GetUsers()?.Any() ?? false),
                                ProjectAndUserCacheCheckTimerInterval,
                                () => setSearchByAuthorEnabled(true));

         IEnumerable<MergeRequestKey> closedReviewed = gatherClosedReviewedMergeRequests(dataCache, hostname);
         addRecentMergeRequestKeys(closedReviewed);
         cleanupOldRecentMergeRequests(hostname);
         cleanupReopenedRecentMergeRequests();
         loadRecentMergeRequests();

         updateMergeRequestList(EDataCacheType.Live);
         enableMergeRequestListControls(true);
         enableSimpleSearchControls(true);
         labelOperationStatus.Text = "Merge requests loaded";

         IEnumerable<ProjectKey> projects = getDataCache(EDataCacheType.Live).MergeRequestCache.GetProjects();
         foreach (ProjectKey projectKey in projects)
         {
            requestCommitStorageUpdate(projectKey);
         }

         // current mode may have changed during 'await'
         if (getCurrentTabDataCacheType() == EDataCacheType.Live)
         {
            bool shouldUseLastSelection = _lastMergeRequestsByHosts.ContainsKey(hostname);
            string projectname = shouldUseLastSelection ?
               _lastMergeRequestsByHosts[hostname].ProjectKey.ProjectName : String.Empty;
            int iid = shouldUseLastSelection ? _lastMergeRequestsByHosts[hostname].IId : 0;

            MergeRequestKey mrk = new MergeRequestKey(new ProjectKey(hostname, projectname), iid);
            getListView(EDataCacheType.Live).SelectMergeRequest(mrk, false);
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void initializeProjectListIfEmpty(string hostname)
      {
         if (!ConfigurationHelper.GetProjectsForHost(hostname, Program.Settings).Any())
         {
            setupDefaultProjectList();
            updateProjectsListView();
         }
      }

      async private Task upgradeProjectListFromOldVersion(string hostname)
      {
         if (Program.Settings.SelectedProjectsUpgraded)
         {
            return;
         }

         GitLabClient.ProjectAccessor projectAccessor = Shortcuts.GetProjectAccessor(
            new GitLabInstance(hostname, Program.Settings), _modificationNotifier);

         labelOperationStatus.Text = "Preparing workflow to the first launch...";
         IEnumerable<Tuple<string, bool>> projects = ConfigurationHelper.GetProjectsForHost(
            hostname, Program.Settings);
         List<Tuple<string, bool>> upgraded = new List<Tuple<string, bool>>();
         foreach (var project in projects)
         {
            Project p = await projectAccessor.SearchProjectAsync(project.Item1);
            if (p != null)
            {
               if (!upgraded.Any(x => x.Item1 == p.Path_With_Namespace))
               {
                  upgraded.Add(new Tuple<string, bool>(p.Path_With_Namespace, project.Item2));
               }
            }
         }
         ConfigurationHelper.SetProjectsForHost(hostname, upgraded, Program.Settings);
         updateProjectsListView();
         Program.Settings.SelectedProjectsUpgraded = true;
         labelOperationStatus.Text = "Workflow prepared.";
      }

      async private Task initializeLabelListIfEmpty(string hostname)
      {
         if (ConfigurationHelper.GetUsersForHost(hostname, Program.Settings).Any())
         {
            return;
         }

         GitLabClient.UserAccessor userAccessor = Shortcuts.GetUserAccessor(
            new GitLabInstance(hostname, Program.Settings));

         bool migratedLabels = false;
         labelOperationStatus.Text = "Preparing workflow to the first launch...";
         List<Tuple<string, bool>> labels = new List<Tuple<string, bool>>();
         MergeRequestFilterState filter = _mergeRequestFilter.Filter;
         if (filter.Enabled)
         {
            foreach (string keyword in filter.Keywords)
            {
               string adjustedKeyword = keyword;
               if (keyword.StartsWith(Constants.GitLabLabelPrefix) || keyword.StartsWith(Constants.AuthorLabelPrefix))
               {
                  adjustedKeyword = keyword.Substring(1);
               }
               User user = await userAccessor.SearchUserByUsernameAsync(adjustedKeyword);
               if (user != null)
               {
                  if (!labels.Any(x => x.Item1 == user.Username))
                  {
                     labels.Add(new Tuple<string, bool>(user.Username, true));
                     migratedLabels |= true;
                  }
               }
            }
         }

         User currentUser = await userAccessor.GetCurrentUserAsync();
         if (currentUser != null)
         {
            if (!labels.Any(x => x.Item1 == currentUser.Username))
            {
               labels.Add(new Tuple<string, bool>(currentUser.Username, true));
            }
         }
         ConfigurationHelper.SetUsersForHost(hostname, labels, Program.Settings);
         updateUsersListView();
         labelOperationStatus.Text = "Workflow prepared.";

         if (Program.Settings.ShowWarningOnFilterMigration)
         {
            if (migratedLabels)
            {
               MessageBox.Show(
                  "By default, new versions of mrHelper select user-based workflow. "
                + "Some of your filters are moved to Settings and only merge requests that match them are loaded from GitLab. "
                + "You don't need to specify projects manually.\n"
                + "Note that old Filter entry still works as additional filtering of loaded merge requests.",
                  "Important news",
                  MessageBoxButtons.OK, MessageBoxIcon.Information);
               checkBoxDisplayFilter.Checked = false;
            }
            else if (currentUser != null)
            {
               MessageBox.Show(
                  "By default, new versions of mrHelper select user-based workflow. "
                + "Only merge requests that authored by you OR expected to be reviewed by you are loaded from GitLab.\n"
                + "If you want to track merge requests of other users, specify them in Settings. ",
                  "Important news",
                  MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            Program.Settings.ShowWarningOnFilterMigration = false;
         }
      }

      private SearchQueryCollection getCustomDataForUserBasedWorkflow(IEnumerable<string> usernames)
      {
         SearchQuery[] queries = usernames
            .SelectMany(username => new SearchQuery[]
               {
                  new SearchQuery
                  {
                     Labels = new string[]{ Constants.GitLabLabelPrefix + username.ToLower() },
                     State = "opened"
                  },
                  // OR
                  new SearchQuery
                  {
                     AuthorUserName = username,
                     State = "opened"
                  }
               })
            .ToArray();
         return new SearchQueryCollection(queries);
      }

      private SearchQueryCollection getCustomDataForProjectBasedWorkflow(IEnumerable<ProjectKey> enabledProjects)
      {
         SearchQuery[] queries = enabledProjects
            .Select(project => new SearchQuery
               {
                  ProjectName = project.ProjectName,
                  State = "opened"
               })
            .ToArray();
         return new SearchQueryCollection(queries);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      [Flags]
      private enum DataCacheUpdateKind
      {
         MergeRequest = 1,
         Discussions = 2,
         MergeRequestAndDiscussions = MergeRequest | Discussions
      }

      private void requestUpdates(DataCache dataCache, MergeRequestKey? mrk, int interval, Action onUpdateFinished,
         DataCacheUpdateKind kind = DataCacheUpdateKind.MergeRequestAndDiscussions)
      {
         bool needUpdateMergeRequest = kind.HasFlag(DataCacheUpdateKind.MergeRequest);
         bool needUpdateDiscussions = kind.HasFlag(DataCacheUpdateKind.Discussions);

         bool mergeRequestUpdateFinished = !needUpdateMergeRequest;
         bool discussionUpdateFinished = !needUpdateDiscussions;

         void onSingleUpdateFinished()
         {
            if (mergeRequestUpdateFinished && discussionUpdateFinished)
            {
               onUpdateFinished?.Invoke();
            }
         }

         if (needUpdateMergeRequest)
         {
            dataCache?.MergeRequestCache?.RequestUpdate(mrk, interval,
               () =>
               {
                  mergeRequestUpdateFinished = true;
                  onSingleUpdateFinished();
               });
         }

         if (needUpdateDiscussions)
         {
            dataCache?.DiscussionCache?.RequestUpdate(mrk, interval,
               () =>
               {
                  discussionUpdateFinished = true;
                  onSingleUpdateFinished();
               });
         }
      }

      async private Task checkForUpdatesAsync(DataCache dataCache, MergeRequestKey? mrk,
         DataCacheUpdateKind kind = DataCacheUpdateKind.MergeRequestAndDiscussions)
      {
         bool updateReceived = false;
         bool updatingWholeList = !mrk.HasValue;

         string oldButtonText = buttonReloadList.Text;
         if (updatingWholeList)
         {
            onUpdating();
         }
         requestUpdates(dataCache, mrk, 100,
            () =>
            {
               updateReceived = true;
               if (updatingWholeList)
               {
                  onUpdated(oldButtonText);
               }
            },
            kind);
         await TaskUtils.WhileAsync(() => !updateReceived);
      }

      private void reloadMergeRequestsByUserRequest(DataCache dataCache)
      {
         showWarningOnReloadList();

         if (getHostName() != String.Empty)
         {
            Trace.TraceInformation("[MainForm] User decided to Reload List");

            string oldButtonText = buttonReloadList.Text;
            onUpdating();

            requestUpdates(dataCache, null, PseudoTimerInterval,
               () =>
               {
                  onUpdated(oldButtonText);
                  Trace.TraceInformation("[MainForm] Finished updating by user request");
               });
         }
      }
   }
}

