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
      private void subscribeToWorkflow()
      {
         _workflowManager.PreLoadMergeRequest += onLoadSingleMergeRequest;
         _workflowManager.PostLoadMergeRequest += onSingleMergeRequestLoaded;
         _workflowManager.FailedLoadMergeRequest += onFailedLoadSingleMergeRequest;

         _workflowManager.PreLoadComparableEntities += onLoadComparableEntities;
         _workflowManager.PostLoadComparableEntities += onComparableEntitiesLoaded;
         _workflowManager.FailedLoadComparableEntities +=  onFailedLoadComparableEntities;

         _workflowManager.Connected += workflowManager_Connected;
      }

      private void unsubscribeFromWorkflow()
      {
         if (_workflowManager == null)
         {
            return;
         }

         _workflowManager.PreLoadMergeRequest -= onLoadSingleMergeRequest;
         _workflowManager.PostLoadMergeRequest -= onSingleMergeRequestLoaded;
         _workflowManager.FailedLoadMergeRequest -= onFailedLoadSingleMergeRequest;

         _workflowManager.PreLoadComparableEntities -= onLoadComparableEntities;
         _workflowManager.PostLoadComparableEntities -= onComparableEntitiesLoaded;
         _workflowManager.FailedLoadComparableEntities -=  onFailedLoadComparableEntities;

         _workflowManager.Connected -= workflowManager_Connected;
      }

      async private Task switchHostToSelected()
      {
         string hostName = getHostName();
         if (hostName != String.Empty)
         {
            tabControl.SelectedTab = tabPageMR;
         }
         else
         {
            disableAllUIControls(true);
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

      async private Task<bool> switchMergeRequestByUserAsync(ProjectKey projectKey, int mergeRequestIId,
         bool showVersions)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change merge request to IId {0}",
            mergeRequestIId.ToString()));

         if (mergeRequestIId == 0)
         {
            onLoadSingleMergeRequest(0);
            await _workflowManager.CancelAsync();
            return false;
         }

         await _workflowManager.CancelAsync();

         IEnumerable<Project> enabledProjects = ConfigurationHelper.GetEnabledProjects(
            projectKey.HostName, Program.Settings);

         string projectname = projectKey.ProjectName;
         if (projectname != String.Empty &&
            (!enabledProjects.Cast<Project>().Any(x => 0 == String.Compare(x.Path_With_Namespace, projectname, true))))
         {
            string message = String.Format("Project {0} is not in the list of enabled projects", projectname);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
         }

         _suppressExternalConnections = true;
         try
         {
            return await _workflowManager.LoadMergeRequestAsync(
               projectKey.HostName, projectKey.ProjectName, mergeRequestIId,
               showVersions ? EComparableEntityType.Version : EComparableEntityType.Commit);
         }
         catch (SessionException ex)
         {
            ExceptionHandlers.Handle("Cannot switch merge request", ex);
            MessageBox.Show(ex.UserMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         finally
         {
            _suppressExternalConnections = false;
         }

         return false;
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

         await _workflowManager.CancelAsync();
         await _searchWorkflowManager.CancelAsync();
         if (hostname == String.Empty)
         {
            return false;
         }

         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         IEnumerable<Project> enabledProjects = ConfigurationHelper.GetEnabledProjects(
            hostname, Program.Settings);
         if (enabledProjects.Count() == 0)
         {
            throw new NoProjectsException(hostname);
         }

         disableAllUIControls(true);
         disableAllSearchUIControls(true);
         buttonReloadList.Enabled = true;
         createListViewGroupsForProjects(listViewMergeRequests, hostname, enabledProjects);

         return await loadAllMergeRequests(hostname, enabledProjects);
      }

      async private Task<bool> loadAllMergeRequests(string hostname, IEnumerable<Project> enabledProjects)
      {
         onLoadAllMergeRequests(enabledProjects);

         if (!await _workflowManager.LoadAllMergeRequestsAsync(
            hostname, enabledProjects, (x, y) => onForbiddenProject(x, y), (x, y) => onNotFoundProject(x, y)))
         {
            return false;
         }

         onAllMergeRequestsLoaded(hostname, enabledProjects);
         return true;
      }

      private void onForbiddenProject(string hostname, string projectname)
      {
         string message = String.Format(
            "You don't have access to project {0} at {1}. "
          + "Loading of this project will be disabled. You may turn it on at Settings tab.",
            projectname, hostname);
         MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
         Trace.TraceInformation("[MainForm.Workflow] Forbidden project. User notified that project will be disabled");

         changeProjectEnabledState(hostname, projectname, false);
      }

      private void onNotFoundProject(string hostname, string projectname)
      {
         string message = String.Format(
            "There is no project {0} at {1}. "
          + "Loading of this project will be disabled. You may turn it on at Settings tab.",
            projectname, hostname);
         MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
         Trace.TraceInformation("[MainForm.Workflow] Project not found. User notified that project will be disabled");

         changeProjectEnabledState(hostname, projectname, false);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadAllMergeRequests(IEnumerable<Project> projects)
      {
         disableAllUIControls(false);
         labelWorkflowStatus.Text = String.Format(
            "Loading merge requests of {0} project{1}...", projects.Count(), projects.Count() > 1 ? "s" : "");
      }

      private void onAllMergeRequestsLoaded(string hostname, IEnumerable<Project> projects)
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

         foreach (Project project in projects)
         {
            ProjectKey projectKey = new ProjectKey
            {
               HostName = hostname,
               ProjectName = project.Path_With_Namespace
            };
            scheduleSilentUpdate(projectKey);
            cleanupReviewedCommits(projectKey, _mergeRequestCache?.GetMergeRequests(projectKey));
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadSingleMergeRequest(int mergeRequestIId)
      {
         if (isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onLoadSingleMergeRequestCommon(mergeRequestIId);
      }

      private void onFailedLoadSingleMergeRequest()
      {
         if (isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onFailedLoadSingleMergeRequestCommon();
      }

      private void onSingleMergeRequestLoaded(string hostname, string projectname, MergeRequest mergeRequest)
      {
         if (isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onSingleMergeRequestLoadedCommon(hostname, projectname, mergeRequest);
      }

      private void onLoadComparableEntities()
      {
         if (isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onLoadComparableEntitiesCommon(listViewMergeRequests);
      }

      private void onFailedLoadComparableEntities()
      {
         if (isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onFailedLoadComparableEntitiesCommon();
      }

      private void onComparableEntitiesLoaded(string hostname, string projectname, MergeRequest mergeRequest,
         System.Collections.IEnumerable commits)
      {
         if (isSearchMode())
         {
            // because this callback updates controls shared between Live and Search tabs
            return;
         }

         onComparableEntitiesLoadedCommon(mergeRequest, commits, listViewMergeRequests);

         scheduleSilentUpdate(new ProjectKey { HostName = hostname, ProjectName = projectname });
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void workflowManager_Connected(string hostname, User user, IEnumerable<Project> projects)
      {
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

