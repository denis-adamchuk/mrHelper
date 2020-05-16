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
         else
         {
         }

         bool shouldUseLastSelection = _lastMergeRequestsByHosts.ContainsKey(hostName);
         string projectname = shouldUseLastSelection ?
            _lastMergeRequestsByHosts[hostName].ProjectKey.ProjectName : String.Empty;
         int iid = shouldUseLastSelection ? _lastMergeRequestsByHosts[hostName].IId : 0;

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Changing host to {0}. Last selected project: {1}, IId: {2}",
            hostName != String.Empty ? hostName : "N/A",
            projectname != String.Empty ? projectname : "N/A",
            iid != 0 ? iid.ToString() : "N/A"));

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

         if (isSearchMode())
         {
            _suppressExternalConnections = false;
            return;
         }

         _suppressExternalConnections = _suppressExternalConnections
            && selectMergeRequest(listViewMergeRequests, projectname, iid, false);
      }

      private void switchMergeRequestByUser(FullMergeRequestKey fmk, bool showVersions)
      {
         Debug.Assert(fmk.MergeRequest != null && fmk.MergeRequest.IId != 0);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change merge request to IId {0}",
            fmk.MergeRequest.IId.ToString()));

         IEnumerable<Project> enabledProjects = ConfigurationHelper.GetEnabledProjects(
            fmk.ProjectKey.HostName, Program.Settings);

         string projectname = fmk.ProjectKey.ProjectName;
         if (projectname != String.Empty &&
            (!enabledProjects.Cast<Project>().Any(x => 0 == String.Compare(x.Path_With_Namespace, projectname, true))))
         {
            string message = String.Format("Project {0} is not in the list of enabled projects", projectname);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
         }

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

         if (radioButtonSelectByProjects.Checked)
         {
            return await startProjectBasedWorkflowAsync(hostname);
         }
         else if (radioButtonSelectByLabels.Checked)
         {
            throw new NotImplementedException();
         }

         Debug.Assert(false);
         return false;
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

         createListViewGroupsForProjects(listViewMergeRequests, enabledProjects);
         return await loadAllMergeRequests(hostname, enabledProjects);
      }

      async private Task<bool> loadAllMergeRequests(string hostname, IEnumerable<ProjectKey> enabledProjects)
      {
         onLoadAllMergeRequests(enabledProjects);

         SessionContext sessionContext = new SessionContext(
            new SessionCallbacks(onForbiddenProject, onNotFoundProject),
            new SessionUpdateRules(true, true),
            new ProjectBasedContext(enabledProjects.ToArray()));

         if (!await _liveSession.Start(hostname, sessionContext))
         {
            return false;
         }

         onAllMergeRequestsLoaded(hostname, enabledProjects);
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
         disableAllUIControls(false);
         labelWorkflowStatus.Text = String.Format(
            "Loading merge requests of {0} project{1}...", projects.Count(), projects.Count() > 1 ? "s" : "");
      }

      private void onAllMergeRequestsLoaded(string hostname, IEnumerable<ProjectKey> projects)
      {
         labelWorkflowStatus.Text = "Merge requests loaded";

         updateVisibleMergeRequests();

         textBoxSearch.Enabled = true;
         buttonReloadList.Enabled = true;

         if (listViewMergeRequests.Items.Count > 0 || Program.Settings.DisplayFilterEnabled)
         {
            enableMergeRequestFilterControls(true);
            enableListView(listViewMergeRequests);
         }

         foreach (ProjectKey projectKey in projects)
         {
            scheduleSilentUpdate(projectKey);
            cleanupReviewedCommits(projectKey, getCurrentSession()?.MergeRequestCache?.GetMergeRequests(projectKey));
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

         onSingleMergeRequestLoadedCommon(fmk);
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
         unsubscribeFromLiveSessionContent();
      }

      private void liveSessionStarted(string hostname, User user, SessionContext sessionContext)
      {
         subscribeToLiveSessionContent();

         if (!_currentUser.ContainsKey(hostname))
         {
            _currentUser.Add(hostname, user);
         }
         Program.FeedbackReporter.SetUserEMail(user.EMail);
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

