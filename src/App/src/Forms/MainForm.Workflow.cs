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
using mrHelper.Client.Workflow;
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
         _workflowManager.PreLoadCurrentUser += onLoadCurrentUser;
         _workflowManager.PostLoadCurrentUser += onCurrentUserLoaded;
         _workflowManager.FailedLoadCurrentUser += onFailedLoadCurrentUser;

         _workflowManager.PreLoadProjectMergeRequests += onLoadProjectMergeRequests;
         _workflowManager.PostLoadProjectMergeRequests += onProjectMergeRequestsLoaded;
         _workflowManager.FailedLoadProjectMergeRequests += onFailedLoadProjectMergeRequests;

         _workflowManager.PreLoadSingleMergeRequest += onLoadSingleMergeRequest;
         _workflowManager.PostLoadSingleMergeRequest += onSingleMergeRequestLoaded;
         _workflowManager.FailedLoadSingleMergeRequest += onFailedLoadSingleMergeRequest;

         _workflowManager.PreLoadCommits += onLoadCommits;
         _workflowManager.PostLoadCommits += onCommitsLoaded;
         _workflowManager.FailedLoadCommits +=  onFailedLoadCommits;

         _workflowManager.PostLoadLatestVersion += onLatestVersionLoaded;
      }

      private void unsubscribeFromWorkflow()
      {
         _workflowManager.PreLoadCurrentUser -= onLoadCurrentUser;
         _workflowManager.PostLoadCurrentUser -= onCurrentUserLoaded;
         _workflowManager.FailedLoadCurrentUser -= onFailedLoadCurrentUser;

         _workflowManager.PreLoadProjectMergeRequests -= onLoadProjectMergeRequests;
         _workflowManager.PostLoadProjectMergeRequests -= onProjectMergeRequestsLoaded;
         _workflowManager.FailedLoadProjectMergeRequests -= onFailedLoadProjectMergeRequests;

         _workflowManager.PreLoadSingleMergeRequest -= onLoadSingleMergeRequest;
         _workflowManager.PostLoadSingleMergeRequest -= onSingleMergeRequestLoaded;
         _workflowManager.FailedLoadSingleMergeRequest -= onFailedLoadSingleMergeRequest;

         _workflowManager.PreLoadCommits -= onLoadCommits;
         _workflowManager.PostLoadCommits -= onCommitsLoaded;
         _workflowManager.FailedLoadCommits -=  onFailedLoadCommits;

         _workflowManager.PostLoadLatestVersion -= onLatestVersionLoaded;
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
            hostName, projectname != String.Empty ? projectname : "N/A", iid != 0 ? iid.ToString() : "N/A"));

         _suppressExternalConnections = true;
         try
         {
            _suppressExternalConnections = await startWorkflowAsync(hostName);
         }
         catch (Exception ex)
         {
            _suppressExternalConnections = false;
            if (ex is WorkflowException || ex is UnknownHostException || ex is NoProjectsException)
            {
               disableAllUIControls(true);
               ExceptionHandlers.Handle("Cannot switch host", ex);
               string message = ex.Message;
               if (ex is WorkflowException wx)
               {
                  message = wx.UserMessage;
               }
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }
            throw;
         }
         _suppressExternalConnections = _suppressExternalConnections && selectMergeRequest(projectname, iid, false);
      }

      async private Task<bool> switchMergeRequestByUserAsync(ProjectKey projectKey, int mergeRequestIId)
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
            (!enabledProjects.Cast<Project>().Any((x) => (x.Path_With_Namespace == projectname))))
         {
            string message = String.Format("Project {0} is not in the list of enabled projects", projectname);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
         }

         _suppressExternalConnections = true;
         try
         {
            return await _workflowManager.LoadMergeRequestAsync(
               projectKey.HostName, projectKey.ProjectName, mergeRequestIId);
         }
         catch (WorkflowException ex)
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
         labelWorkflowStatus.Text = String.Empty;

         await _workflowManager.CancelAsync();
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

         if (!_currentUser.ContainsKey(hostname))
         {
            if (!await _workflowManager.LoadCurrentUserAsync(hostname))
            {
               return false;
            }
         }
         else
         {
            disableAllUIControls(true);
         }

         buttonReloadList.Enabled = true;
         createListViewGroupsForProjects(listViewMergeRequests, hostname, enabledProjects);

         Connected?.Invoke(hostname, _currentUser[hostname], enabledProjects);
         return await loadAllMergeRequests(hostname, enabledProjects);
      }

      async private Task<bool> loadAllMergeRequests(string hostname, IEnumerable<Project> enabledProjects)
      {
         onLoadAllMergeRequests();

         foreach (Project project in enabledProjects)
         {
            try
            {
               if (!await _workflowManager.LoadAllMergeRequestsAsync(hostname, project))
               {
                  return false;
               }
            }
            catch (WorkflowException ex)
            {
               if (!handleWorkflowException(hostname, project, ex))
               {
                  throw;
               }
               ExceptionHandlers.Handle("Cannot load merge requests for project (handled)", ex);
            }
         }

         onAllMergeRequestsLoaded(hostname, enabledProjects);
         return true;
      }

      private bool handleWorkflowException(string hostname, Project project, WorkflowException ex)
      {
         if (ex.InnerException?.InnerException is GitLabRequestException rx)
         {
            if (rx.InnerException is System.Net.WebException wx)
            {
               System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;

               if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
               {
                  string message = String.Format(
                     "You don't have access to project {0} at {1}. "
                   + "Loading of this project will be disabled. You may turn it on at Settings tab.",
                     project.Path_With_Namespace, hostname);
                  MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  Trace.TraceInformation("[MainForm.Workflow] User notified that project will be disabled");

                  changeProjectEnabledState(hostname, project.Path_With_Namespace, false);

                  return true;
               }
            }
         }

         return false;
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadCurrentUser(string hostname)
      {
         disableAllUIControls(true);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loading user from host {0}", hostname));
      }

      private void onFailedLoadCurrentUser()
      {
         labelWorkflowStatus.Text = "Failed to load current user";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to load a user"));
      }

      private void onCurrentUserLoaded(string hostname, User currentUser)
      {
         _currentUser.Add(hostname, currentUser);

         labelWorkflowStatus.Text = "Loaded current user";

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Current user details: Id: {0}, Name: {1}, Username: {2}",
            currentUser.Id.ToString(), currentUser.Name, currentUser.Username));
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadAllMergeRequests()
      {
         disableAllUIControls(false);
      }

      private void onLoadProjectMergeRequests(Project project)
      {
         labelWorkflowStatus.Text = String.Format("Loading merge requests of project {0}...",
            project.Path_With_Namespace);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loading merge requests of project {0}",
            project.Path_With_Namespace));
      }

      private void onFailedLoadProjectMergeRequests()
      {
         labelWorkflowStatus.Text = "Failed to load merge requests";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to load merge requests"));
      }

      private void onProjectMergeRequestsLoaded(string hostname, Project project,
         IEnumerable<MergeRequest> mergeRequests)
      {
         LoadedMergeRequests?.Invoke(hostname, project, mergeRequests);

         labelWorkflowStatus.Text = String.Format("Project {0} loaded", project.Path_With_Namespace);

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Project {0} loaded. Loaded {1} merge requests",
           project.Path_With_Namespace, mergeRequests.Count()));

         cleanupReviewedCommits(hostname, project.Path_With_Namespace, mergeRequests);
      }

      private void onAllMergeRequestsLoaded(string hostname, IEnumerable<Project> projects)
      {
         updateVisibleMergeRequests();

         buttonReloadList.Enabled = true;

         if (listViewMergeRequests.Items.Count > 0 || Program.Settings.CheckedLabelsFilter)
         {
            enableMergeRequestFilterControls(true);
            enableListView(listViewMergeRequests);
         }

         foreach (Project project in projects)
         {
            scheduleSilentUpdate(new ProjectKey{ HostName = hostname, ProjectName = project.Path_With_Namespace });
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadSingleMergeRequest(int mergeRequestIId)
      {
         if (mergeRequestIId != 0)
         {
            labelWorkflowStatus.Text = String.Format("Loading merge request with IId {0}...", mergeRequestIId);
         }
         else
         {
            labelWorkflowStatus.Text = String.Empty;
         }

         enableMergeRequestActions(false);
         enableCommitActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         updateTotalTime(null);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);

         if (mergeRequestIId != 0)
         {
            richTextBoxMergeRequestDescription.Text = "Loading...";
         }

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loading merge request with IId {0}",
            mergeRequestIId.ToString()));
      }

      private void onFailedLoadSingleMergeRequest()
      {
         richTextBoxMergeRequestDescription.Text = String.Empty;
         labelWorkflowStatus.Text = "Failed to load merge request";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to load merge request"));
      }

      private void onSingleMergeRequestLoaded(string hostname, string projectname, MergeRequest mergeRequest)
      {
         Debug.Assert(mergeRequest.Id != default(MergeRequest).Id);

         enableMergeRequestActions(true);
         FullMergeRequestKey fmk = new FullMergeRequestKey
         {
            ProjectKey = new ProjectKey { HostName = hostname, ProjectName = projectname },
            MergeRequest = mergeRequest
         };
         updateMergeRequestDetails(fmk);
         updateTimeTrackingMergeRequestDetails(mergeRequest);
         updateTotalTime(getMergeRequestKey().Value);

         labelWorkflowStatus.Text = String.Format("Merge request with Id {0} loaded", mergeRequest.Id);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Merge request loaded"));
      }

      private void onLoadCommits()
      {
         enableCommitActions(false);
         if (listViewMergeRequests.SelectedItems.Count != 0)
         {
            disableComboBox(comboBoxLeftCommit, "Loading...");
            disableComboBox(comboBoxRightCommit, "Loading...");
         }
         else
         {
            disableComboBox(comboBoxLeftCommit, String.Empty);
            disableComboBox(comboBoxRightCommit, String.Empty);
         }

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loading commits"));
      }

      private void onFailedLoadCommits()
      {
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);
         labelWorkflowStatus.Text = "Failed to load commits";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to load commits"));
      }

      private void onCommitsLoaded(string hostname, string projectname, MergeRequest mergeRequest,
         IEnumerable<Commit> commits)
      {
         if (commits.Count() > 0)
         {
            enableComboBox(comboBoxLeftCommit);
            enableComboBox(comboBoxRightCommit);

            addCommitsToComboBoxes(commits, mergeRequest.Diff_Refs.Base_SHA, mergeRequest.Target_Branch);
            selectNotReviewedCommits(out int left, out int right);
            comboBoxLeftCommit.SelectedIndex = left;
            comboBoxRightCommit.SelectedIndex = right;

            enableCommitActions(true);
         }
         else
         {
            disableComboBox(comboBoxLeftCommit, String.Empty);
            disableComboBox(comboBoxRightCommit, String.Empty);
         }

         labelWorkflowStatus.Text = String.Format("Loaded {0} commits", commits.Count());

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loaded {0} commits", commits.Count()));

         scheduleSilentUpdate(new MergeRequestKey
         {
            ProjectKey = new ProjectKey { HostName = hostname, ProjectName = projectname },
            IId = mergeRequest.IId
         });
      }

      private void onLatestVersionLoaded(string hostname, string projectname,
         MergeRequest mergeRequest, GitLabSharp.Entities.Version version)
      {
         LoadedMergeRequestVersion?.Invoke(hostname, projectname, mergeRequest, version);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void disableAllUIControls(bool clearListView)
      {
         buttonReloadList.Enabled = false;
         enableMergeRequestFilterControls(false);
         enableMergeRequestActions(false);
         enableCommitActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         updateTotalTime(null);
         disableListView(listViewMergeRequests, clearListView);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);
      }
   }
}

