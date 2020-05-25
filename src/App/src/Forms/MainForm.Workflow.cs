using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Client.Types;
using mrHelper.Client.Session;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Client.MergeRequests;
using System.Collections;
using mrHelper.Common.Constants;

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
      async private Task switchHostToSelected()
      {
         string hostName = getHostName();
         if (hostName != String.Empty)
         {
            tabControl.SelectedTab = tabPageMR;
         }

         bool shouldUseLastSelection = _lastMergeRequestsByHosts.ContainsKey(hostName);
         string projectname = shouldUseLastSelection ?
            _lastMergeRequestsByHosts[hostName].ProjectKey.ProjectName : String.Empty;
         int iid = shouldUseLastSelection ? _lastMergeRequestsByHosts[hostName].IId : 0;

         _suppressExternalConnections = true;
         try
         {
            _suppressExternalConnections = await startWorkflowAsync(hostName);
         }
         catch (Exception ex)
         {
            _suppressExternalConnections = false;
            if (ex is SessionException || ex is UnknownHostException || ex is NoProjectsException)
            {
               disableAllUIControls(true);
               ExceptionHandlers.Handle("Cannot switch host", ex);
               string message = ex.Message;
               if (ex is SessionException wx)
               {
                  message = wx.UserMessage;
               }
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }
            throw;
         }
         _suppressExternalConnections = false;
         if (!isSearchMode())
         {
            MergeRequestKey mrk = new MergeRequestKey(new ProjectKey(hostName, projectname), iid);
            selectMergeRequest(listViewMergeRequests, mrk, false);
         }
      }

      private void switchMergeRequestByUser(FullMergeRequestKey fmk, bool showVersions)
      {
         Debug.Assert(fmk.MergeRequest != null && fmk.MergeRequest.IId != 0);

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] User requested to change merge request to IId {0}",
            fmk.MergeRequest.IId.ToString()));

         _suppressExternalConnections = true;
         try
         {
            onSingleMergeRequestLoaded(fmk);

            IMergeRequestCache cache = _liveSession.MergeRequestCache;
            MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
            GitLabSharp.Entities.Version latestVersion = cache.GetLatestVersion(mrk);
            onComparableEntitiesLoaded(latestVersion, fmk.MergeRequest,
               showVersions ? (IEnumerable)cache.GetVersions(mrk) : (IEnumerable)cache.GetCommits(mrk));
         }
         finally
         {
            _suppressExternalConnections = false;
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      async private Task<bool> startWorkflowAsync(string hostname)
      {
         // When this thing happens, everything reconnects. If there are some things at gitlab that user
         // wants to be notified about and we did not cache them yet (e.g. mentions in discussions)
         // we will miss them. It might be ok when host changes, but if this method used to "refresh"
         // things, missed events are not desirable.
         // This is why "Update List" button implemented not by means of switchHostToSelected().

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Starting workflow at host {0}. Workflow type is {1}",
            hostname, Program.Settings.WorkflowType));

         await disposeLocalGitRepositoryFactory();

         labelWorkflowStatus.Text = String.Empty;
         textBoxSearch.Enabled = false;

         await _liveSession.Stop();
         disableAllUIControls(true);

         await _searchSession.Stop();
         disableAllSearchUIControls(true);

         if (String.IsNullOrWhiteSpace(hostname))
         {
            return false;
         }

         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            initializeProjectListIfEmpty(hostname);
            await upgradeProjectListFromOldVersion(hostname);
            return await startProjectBasedWorkflowAsync(hostname);
         }
         else
         {
            await initializeLabelListIfEmpty(hostname);
            return await startUserBasedWorkflowAsync(hostname);
         }
      }

      private async Task<bool> startProjectBasedWorkflowAsync(string hostname)
      {
         IEnumerable<ProjectKey> enabledProjects =
            ConfigurationHelper.GetEnabledProjects(hostname, Program.Settings)
            .Select(x => new ProjectKey(hostname, x.Path_With_Namespace));
         if (enabledProjects.Count() == 0)
         {
            throw new NoProjectsException(hostname);
         }

         onLoadAllMergeRequests(enabledProjects);

         SessionContext sessionContext = new SessionContext(
            new SessionCallbacks(onForbiddenProject, onNotFoundProject),
            new SessionUpdateRules(true, true),
            new ProjectBasedContext(enabledProjects.ToArray()));

         if (!await _liveSession.Start(hostname, sessionContext))
         {
            return false;
         }

         onAllMergeRequestsLoaded(enabledProjects);
         cleanupReviewedCommits(hostname);
         return true;
      }

      private async Task<bool> startUserBasedWorkflowAsync(string hostname)
      {
         onLoadAllMergeRequests();

         SessionContext sessionContext = new SessionContext(
            new SessionCallbacks(onForbiddenProject, onNotFoundProject),
            new SessionUpdateRules(true, true),
            getCustomDataForUserBasedWorkflow());

         if (!await _liveSession.Start(hostname, sessionContext))
         {
            return false;
         }

         onAllMergeRequestsLoaded(_liveSession.MergeRequestCache.GetProjects());
         cleanupReviewedCommits(hostname);
         return true;
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

      private void onLoadAllMergeRequests(IEnumerable<ProjectKey> projects)
      {
         createListViewGroupsForProjects(listViewMergeRequests, projects);

         disableAllUIControls(false);
         labelWorkflowStatus.Text = String.Format(
            "Loading merge requests of {0} project{1}...", projects.Count(), projects.Count() > 1 ? "s" : "");
      }

      private void onLoadAllMergeRequests()
      {
         disableAllUIControls(false);
         labelWorkflowStatus.Text = "Loading merge requests...";
      }

      private void onAllMergeRequestsLoaded(IEnumerable<ProjectKey> projects)
      {
         labelWorkflowStatus.Text = "Merge requests loaded";

         updateVisibleMergeRequests();

         textBoxSearch.Enabled = true;
         buttonReloadList.Enabled = true;

         foreach (ProjectKey projectKey in projects)
         {
            scheduleSilentUpdate(projectKey);
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

         onSingleMergeRequestLoadedCommon(fmk, _liveSession);
      }

      private void onComparableEntitiesLoaded(GitLabSharp.Entities.Version latestVersion,
         MergeRequest mergeRequest, IEnumerable entities)
      {
         if (isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onComparableEntitiesLoadedCommon(latestVersion, mergeRequest, entities, listViewMergeRequests);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void liveSessionStarting(string hostname)
      {
         unsubscribeFromLiveSessionInternalEvents();
      }

      private void liveSessionStarted(string hostname, User user)
      {
         subscribeToLiveSessionInternalEvents();

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

         labelWorkflowStatus.Text = "Preparing workflow to the first launch...";
         IEnumerable<Tuple<string, bool>> projects = ConfigurationHelper.GetProjectsForHost(
            hostname, Program.Settings);
         List<Tuple<string, bool>> upgraded = new List<Tuple<string, bool>>();
         foreach (var project in projects)
         {
            Project p = await _gitlabClientManager.SearchManager.SearchProjectAsync(hostname, project.Item1);
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
               User user = await _gitlabClientManager.SearchManager.
                  SearchUserByNameAsync(hostname, adjustedKeyword, true);
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

         User currentUser = await _gitlabClientManager.SearchManager.GetCurrentUserAsync(hostname);
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

