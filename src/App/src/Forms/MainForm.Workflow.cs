using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using System.Collections;
using mrHelper.Common.Constants;
using mrHelper.GitLabClient;
using mrHelper.App.Helpers.GitLab;
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
               disableAllUIControls(true);
               ExceptionHandlers.Handle("Cannot switch host", ex);
               string message = ex.Message;
               if (ex is DataCacheException wx)
               {
                  message = wx.UserMessage;
               }
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

      private void onLiveMergeRequestSelectionChanged(FullMergeRequestKey fmk)
      {
         Debug.Assert(fmk.MergeRequest != null && fmk.MergeRequest.IId != 0);

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] User requested to change merge request to IId {0}",
            fmk.MergeRequest.IId.ToString()));

         onSingleMergeRequestLoaded(fmk);

         IMergeRequestCache cache = _liveDataCache.MergeRequestCache;
         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         GitLabSharp.Entities.Version latestVersion = cache.GetLatestVersion(mrk);
         onComparableEntitiesLoaded(latestVersion, fmk.MergeRequest, cache.GetCommits(mrk), cache.GetVersions(mrk));
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      /// <summary>
      /// Connects Live DataCache to GitLab
      /// </summary>
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

         disableAllUIControls(true);
         disableAllSearchUIControls(true);
         _searchDataCache.Disconnect();
         textBoxSearch.Enabled = false;
         labelWorkflowStatus.Text = String.Format("Connecting to {0}...", hostname);

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
         IEnumerable<ProjectKey> enabledProjects =
            ConfigurationHelper.GetEnabledProjectNames(hostname, Program.Settings)
            .Select(x => new ProjectKey(hostname, x));
         if (!enabledProjects.Any())
         {
            throw new NoProjectsException(hostname);
         }

         onLoadAllMergeRequests(enabledProjects, hostname);

         DataCacheConnectionContext connectionContext = new DataCacheConnectionContext(
            new DataCacheCallbacks(onForbiddenProject, onNotFoundProject),
            new DataCacheUpdateRules(Program.Settings.AutoUpdatePeriodMs, Program.Settings.AutoUpdatePeriodMs),
            new ProjectBasedContext(enabledProjects.ToArray()));

         await _liveDataCache.Connect(new GitLabInstance(hostname, Program.Settings), connectionContext);

         onAllMergeRequestsLoaded(hostname, enabledProjects);
         cleanupReviewedRevisions(hostname);
      }

      private async Task startUserBasedWorkflowAsync(string hostname)
      {
         onLoadAllMergeRequests(hostname);

         DataCacheConnectionContext connectionContext = new DataCacheConnectionContext(
            new DataCacheCallbacks(onForbiddenProject, onNotFoundProject),
            new DataCacheUpdateRules(Program.Settings.AutoUpdatePeriodMs, Program.Settings.AutoUpdatePeriodMs),
            getCustomDataForUserBasedWorkflow());

         await _liveDataCache.Connect(new GitLabInstance(hostname, Program.Settings), connectionContext);

         onAllMergeRequestsLoaded(hostname, _liveDataCache.MergeRequestCache.GetProjects());
         cleanupReviewedRevisions(hostname);
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

      private void onLoadAllMergeRequests(IEnumerable<ProjectKey> projects, string hostname)
      {
         // in Project-based workflow we want to create all groups at once in a user-defined order
         listViewMergeRequests.Items.Clear();
         listViewMergeRequests.Groups.Clear();
         foreach (ProjectKey projectKey in projects)
         {
            createListViewGroupForProject(listViewMergeRequests, projectKey, false);
         }

         disableAllUIControls(false);
         labelWorkflowStatus.Text = String.Format("Loading merge requests of {0} project{1} from {2}...",
            projects.Count(), projects.Count() > 1 ? "s" : "", hostname);
      }

      private void onLoadAllMergeRequests(string hostname)
      {
         disableAllUIControls(false);
         labelWorkflowStatus.Text = String.Format("Loading merge requests from {0}...", hostname);
      }

      private void onAllMergeRequestsLoaded(string hostName, IEnumerable<ProjectKey> projects)
      {
         labelWorkflowStatus.Text = "Merge requests loaded";

         updateVisibleMergeRequests();

         textBoxSearch.Enabled = true;
         buttonReloadList.Enabled = true;

         foreach (ProjectKey projectKey in projects)
         {
            requestCommitStorageUpdate(projectKey);
         }

         if (!isSearchMode())
         {
            bool shouldUseLastSelection = _lastMergeRequestsByHosts.ContainsKey(hostName);
            string projectname = shouldUseLastSelection ?
               _lastMergeRequestsByHosts[hostName].ProjectKey.ProjectName : String.Empty;
            int iid = shouldUseLastSelection ? _lastMergeRequestsByHosts[hostName].IId : 0;

            MergeRequestKey mrk = new MergeRequestKey(new ProjectKey(hostName, projectname), iid);
            selectMergeRequest(listViewMergeRequests, mrk, false);
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onSingleMergeRequestLoaded(FullMergeRequestKey fmk)
      {
         if (isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onSingleMergeRequestLoadedCommon(fmk, _liveDataCache);
      }

      private void onComparableEntitiesLoaded(GitLabSharp.Entities.Version latestVersion,
         MergeRequest mergeRequest, IEnumerable<Commit> commits, IEnumerable<GitLabSharp.Entities.Version> versions)
      {
         if (isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onComparableEntitiesLoadedCommon(latestVersion, mergeRequest, commits, versions, listViewMergeRequests);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void liveDataCacheDisconnected()
      {
         Trace.TraceInformation("[MainForm.Workflow] Reset GitLabInstance");

         closeAllFormsExceptMain();
         disposeGitHelpers();
         disposeLocalGitRepositoryFactory();
         unsubscribeFromLiveDataCacheInternalEvents();

         _projectCacheCheckTimer?.Stop();
      }

      private void liveDataCacheConnected(string hostname, User user)
      {
         subscribeToLiveDataCacheInternalEvents();
         createGitHelpers(_liveDataCache, getCommitStorageFactory(false));

         if (!_currentUser.ContainsKey(hostname))
         {
            _currentUser.Add(hostname, user);
         }
         Program.FeedbackReporter.SetUserEMail(user.EMail);
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

         labelWorkflowStatus.Text = "Preparing workflow to the first launch...";
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
         labelWorkflowStatus.Text = "Workflow prepared.";
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
         labelWorkflowStatus.Text = "Preparing workflow to the first launch...";
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
         labelWorkflowStatus.Text = "Workflow prepared.";

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

      private object getCustomDataForUserBasedWorkflow()
      {
         object[] criteria = listViewUsers
            .Items
            .Cast<ListViewItem>()
            .Select(x => new SearchByUsername(x.Text))
            .ToArray();
         return new SearchBasedContext(new SearchCriteria(criteria), null, true);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void disableAllUIControls(bool clearListView)
      {
         buttonReloadList.Enabled = false;
         buttonCreateNew.Enabled = false;
         disableListView(listViewMergeRequests, clearListView);
         enableMergeRequestFilterControls(false);

         if (isSearchMode())
         {
            // to avoid touching controls shared between Live and Search tabs
            return;
         }

         disableCommonUIControls();
      }
   }
}

