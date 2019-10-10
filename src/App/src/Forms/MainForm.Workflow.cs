using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.CustomActions;
using mrHelper.Common.Interfaces;
using mrHelper.Core;
using mrHelper.Client;
using mrHelper.Client.Tools;
using mrHelper.Client.Updates;
using mrHelper.Client.Workflow;
using mrHelper.Common.Exceptions;
using mrHelper.Client.TimeTracking;
using mrHelper.Client.Git;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void createWorkflow()
      {
         _workflow = new Workflow(_settings);

         _workflow.PreLoadCurrentUser += (hostname) => onLoadCurrentUser(hostname);
         _workflow.PostLoadCurrentUser += (user) => onCurrentUserLoaded(user);
         _workflow.FailedLoadCurrentUser += () => onFailedLoadCurrentUser();

         _workflow.PreLoadHostProjects += (hostname) => onLoadHostProjects(hostname);
         _workflow.PostLoadHostProjects += (hostname, projects) => onHostProjectsLoaded(projects);
         _workflow.FailedLoadHostProjects += () => onFailedLoadHostProjects();

         _workflow.PreLoadAllMergeRequests += () => onLoadAllMergeRequests();

         _workflow.PreLoadProjectMergeRequests += (project) => onLoadProjectMergeRequests(project);
         _workflow.PostLoadProjectMergeRequests +=
            (hostname, project, mergeRequests) =>
         {
            onProjectMergeRequestsLoaded(hostname, project, mergeRequests);
            cleanupReviewedCommits(hostname, project.Path_With_Namespace, mergeRequests);
         };
         _workflow.FailedLoadProjectMergeRequests += () => onFailedLoadProjectMergeRequests();

         _workflow.PostLoadAllMergeRequests += (hostname, projects) => onAllMergeRequestsLoaded(hostname, projects);

         _workflow.PrelLoadSingleMergeRequest += (id) => onLoadSingleMergeRequest(id);
         _workflow.PostLoadSingleMergeRequest += (_, mergeRequest) => onSingleMergeRequestLoaded(mergeRequest);
         _workflow.FailedLoadSingleMergeRequest += () => onFailedLoadSingleMergeRequest();

         _workflow.PreLoadCommits += () => onLoadCommits();
         _workflow.PostLoadCommits += (hostname, projectname, mergeRequest, commits) =>
            onCommitsLoaded(hostname, projectname, mergeRequest, commits);
         _workflow.FailedLoadCommits += () => onFailedLoadCommits();
      }

      async private Task switchHostAsync(string hostName)
      {
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

         try
         {
            if (await startWorkflowAsync(hostName, (message) =>
               MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information)))
            {
               selectMergeRequest(projectname, iid, false);
            }
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch host");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      async private Task<bool> switchMergeRequestByUserAsync(string hostname, Project project, int mergeRequestIId)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change merge request to IId {0}",
            mergeRequestIId.ToString()));

         try
         {
            return await _workflow.LoadMergeRequestAsync(hostname, project.Path_With_Namespace, mergeRequestIId);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch merge request");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }

         return false;
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      async private Task<bool> startWorkflowAsync(string hostname, Action<string> onNonFatalError)
      {
         labelWorkflowStatus.Text = String.Empty;

         return await _workflow.LoadCurrentUserAsync(hostname)
             && await _workflow.LoadAllMergeRequestsAsync(hostname, onNonFatalError);
      }

      private bool selectMergeRequest(string projectname, int iid, bool exact)
      {
         foreach (ListViewItem item in listViewMergeRequests.Items)
         {
            FullMergeRequestKey key = (FullMergeRequestKey)(item.Tag);
            if (projectname == String.Empty ||
                (iid == key.MergeRequest.IId && projectname == key.Project.Path_With_Namespace))
            {
               item.Selected = true;
               return true;
            }
         }

         if (exact)
         {
            return false;
         }

         // selected an item from the proper group
         foreach (ListViewGroup group in listViewMergeRequests.Groups)
         {
            if (projectname == group.Name && group.Items.Count > 0)
            {
               group.Items[0].Selected = true;
               return true;
            }
         }

         // select whatever
         foreach (ListViewGroup group in listViewMergeRequests.Groups)
         {
            if (group.Items.Count > 0)
            {
               group.Items[0].Selected = true;
               return true;
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

      private void onCurrentUserLoaded(User currentUser)
      {
         _currentUser = currentUser;

         labelWorkflowStatus.Text = "Loaded current user";

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Current user details: Id: {0}, Name: {1}, Username: {2}",
            currentUser.Id.ToString(), currentUser.Name, currentUser.Username));
      }

      private void onLoadHostProjects(string hostname)
      {
         if (hostname != String.Empty)
         {
            comboBoxHost.SelectedItem = new HostComboBoxItem
            {
               Host = hostname,
               AccessToken = _settings.GetAccessToken(hostname)
            };

            labelWorkflowStatus.Text = String.Format("Connecting to host {0}...", hostname);
         }

         disableAllUIControls(true);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loading projects from {0}", hostname));
      }

      private void onFailedLoadHostProjects()
      {
         labelWorkflowStatus.Text = "Failed to load projects";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to load projects"));
      }

      private void onHostProjectsLoaded(List<Project> projects)
      {
         buttonUpdateList.Enabled = true;

         listViewMergeRequests.Items.Clear();
         listViewMergeRequests.Groups.Clear();
         foreach (Project project in projects)
         {
            listViewMergeRequests.Groups.Add(project.Path_With_Namespace, project.Path_With_Namespace);
         }

         labelWorkflowStatus.Text = "Projects loaded";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loaded {0} projects", projects.Count));
      }

      private void onLoadAllMergeRequests()
      {
         disableAllUIControls(false);

         _allMergeRequests.Clear();
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

      private void onProjectMergeRequestsLoaded(string hostname, Project project, List<MergeRequest> mergeRequests)
      {
         List<FullMergeRequestKey> keys = new List<FullMergeRequestKey>();
         foreach (var mergeRequest in mergeRequests)
         {
            keys.Add(new FullMergeRequestKey(hostname, project, mergeRequest));
         }
         _allMergeRequests.AddRange(keys);

         fillListViewMergeRequests(keys, false);

         labelWorkflowStatus.Text = String.Format("Project {0} loaded", project.Path_With_Namespace);

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Project loaded. Loaded {0} merge requests", mergeRequests.Count));
      }

      private void onAllMergeRequestsLoaded(string hostname, List<Project> projects)
      {
         buttonUpdateList.Enabled = true;

         if (listViewMergeRequests.Items.Count > 0 || _settings.CheckedLabelsFilter)
         {
            enableMergeRequestFilterControls(true);
         }

         if (listViewMergeRequests.Items.Count > 0)
         {
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

      private void onSingleMergeRequestLoaded(MergeRequest mergeRequest)
      {
         Debug.Assert(mergeRequest.Id != default(MergeRequest).Id);

         enableMergeRequestActions(true);
         updateMergeRequestDetails(mergeRequest);
         updateTimeTrackingMergeRequestDetails(mergeRequest);

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

      private void onCommitsLoaded(string hostname, string projectname, MergeRequest mergeRequest, List<Commit> commits)
      {
         if (commits.Count > 0)
         {
            enableComboBox(comboBoxLeftCommit);
            enableComboBox(comboBoxRightCommit);

            addCommitsToComboBoxes(commits, mergeRequest.Diff_Refs.Base_SHA, mergeRequest.Target_Branch);

            enableCommitActions(true);
         }

         labelWorkflowStatus.Text = String.Format("Loaded {0} commits", commits.Count);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loaded {0} commits", commits.Count));

         scheduleSilentUpdate(new MergeRequestKey(hostname, projectname, mergeRequest.IId));
      }

      private void onLoadTotalTime()
      {
         updateTotalTime(null);
         if (!isTrackingTime())
         {
            labelTimeTrackingTrackedLabel.Text = "Total Time:";
            labelTimeTrackingTrackedTime.Text = "Loading...";
         }

         labelWorkflowStatus.Text = "Loading total spent time";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loading total spent time"));
      }

      private void onFailedLoadTotalTime()
      {
         updateTotalTime(null);
         labelWorkflowStatus.Text = "Failed to load total spent time";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to load total spent time"));
      }

      private void onTotalTimeLoaded(MergeRequestKey mrk)
      {
         updateTotalTime(mrk);
         labelWorkflowStatus.Text = "Total spent time loaded";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Total spent time loaded"));
      }

      private void disableAllUIControls(bool clearListView)
      {
         buttonUpdateList.Enabled = false;
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

