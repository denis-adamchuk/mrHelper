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
using mrHelper.Client.TimeTracking;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void createWorkflow()
      {
         _workflow = _workflowFactory.CreateWorkflow();
         _workflow.PreSwitchHost += (hostname) => onChangeHost(hostname);
         _workflow.PostSwitchHost += (state, projects) => onHostChanged(state, projects);
         _workflow.FailedSwitchHost += () => onFailedChangeHost();

         _workflow.PreSwitchProject += (projectname) => onChangeProject(projectname);
         _workflow.PostSwitchProject +=
            (state, mergeRequests) =>
         {
            onProjectChanged(state, mergeRequests);
            cleanupReviewedCommits(state.HostName, state.Project.Path_With_Namespace, mergeRequests);
         };
         _workflow.FailedSwitchProject += () => onFailedChangeProject();

         _workflow.PreSwitchMergeRequest += (iid) => onChangeMergeRequest(iid);
         _workflow.PostSwitchMergeRequest += (state) => onMergeRequestChanged(state);
         _workflow.FailedSwitchMergeRequest += () => onFailedChangeMergeRequest();

         _workflow.PreLoadCommits += () => onLoadCommits();
         _workflow.PostLoadCommits += (state, commits) => onCommitsLoaded(state, commits);
         _workflow.FailedLoadCommits += () => onFailedLoadCommits();
      }

      async private Task initializeWorkflow()
      {
         string hostname = getInitialHostName();
         Trace.TraceInformation(String.Format("[MainForm.Workflow] Initializing workflow for host {0}", hostname));

         await _workflow.InitializeAsync(hostname);
      }

      async private Task changeHostAsync(string hostName)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change host to {0}",
            hostName));

         try
         {
            await _workflow.SwitchHostAsync(hostName);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch host");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void onChangeHost(string hostname)
      {
         if (hostname != String.Empty)
         {
            comboBoxHost.SelectedItem = new HostComboBoxItem
            {
               Host = hostname,
               AccessToken = Tools.GetAccessToken(hostname, _settings)
            };

            disableComboBox(comboBoxProjects, "Loading...");
            labelWorkflowStatus.Text = "Loading projects...";
         }
         else
         {
            disableComboBox(comboBoxProjects, String.Empty);
         }

         enableMergeRequestFilterControls(false);
         enableMergeRequestActions(false);
         enableCommitActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         updateTotalTime(null);
         disableComboBox(comboBoxFilteredMergeRequests, String.Empty);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Changing host to {0}",
            hostname));
      }

      private void onFailedChangeHost()
      {
         disableComboBox(comboBoxProjects, String.Empty);
         labelWorkflowStatus.Text = "Failed to load projects";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to change host"));
      }

      private void onHostChanged(WorkflowState state, List<Project> projects)
      {
         Debug.Assert(comboBoxProjects.Items.Count == 0);
         foreach (var project in projects)
         {
            comboBoxProjects.Items.Add(project);
         }

         if (comboBoxProjects.Items.Count > 0)
         {
            enableComboBox(comboBoxProjects);
         }

         labelWorkflowStatus.Text = "Projects loaded";

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Host changed. Loaded {0} projects", projects.Count));
         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Current user details: Id: {0}, Name: {1}, Username: {2}",
            state.CurrentUser.Id.ToString(), state.CurrentUser.Name, state.CurrentUser.Username));
      }

      async private Task changeProjectAsync(string projectName)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change project to {0}",
            projectName));

         try
         {
            await _workflow.SwitchProjectAsync(projectName);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch project");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void onChangeProject(string projectName)
      {
         foreach (Project project in comboBoxProjects.Items.Cast<Project>())
         {
            if (project.Path_With_Namespace == projectName)
            {
               comboBoxProjects.SelectedItem = project;
            }
         }

         if (comboBoxProjects.SelectedItem != null)
         {
            disableComboBox(comboBoxFilteredMergeRequests, "Loading...");
            labelWorkflowStatus.Text = "Loading merge requests...";
         }
         else
         {
            disableComboBox(comboBoxFilteredMergeRequests, String.Empty);
         }

         enableMergeRequestFilterControls(false);
         enableMergeRequestActions(false);
         enableCommitActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         updateTotalTime(null);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Changing project to {0}",
            projectName));
      }

      private void onFailedChangeProject()
      {
         disableComboBox(comboBoxFilteredMergeRequests, String.Empty);
         labelWorkflowStatus.Text = "Failed to change project";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to change project"));
      }

      private void onProjectChanged(WorkflowState state, List<MergeRequest> mergeRequests)
      {
         mergeRequests = Tools.FilterMergeRequests(mergeRequests, _settings);

         Debug.Assert(comboBoxFilteredMergeRequests.Items.Count == 0);
         foreach (var mergeRequest in mergeRequests)
         {
            comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
         }

         if (mergeRequests.Count > 0 || _settings.CheckedLabelsFilter)
         {
            enableMergeRequestFilterControls(true);
         }

         if (mergeRequests.Count > 0)
         {
            enableComboBox(comboBoxFilteredMergeRequests);
         }
         else
         {
            disableComboBox(comboBoxFilteredMergeRequests, String.Empty);
         }

         labelWorkflowStatus.Text = String.Format("Project {0} selected", state.Project.Path_With_Namespace);

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Project changed. Loaded {0} merge requests", mergeRequests.Count));
      }

      async private Task changeMergeRequestAsync(int mergeRequestIId)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change merge request to IId {0}",
            mergeRequestIId.ToString()));

         try
         {
            await _workflow.SwitchMergeRequestAsync(mergeRequestIId);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch merge request");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void onChangeMergeRequest(int mergeRequestIId)
      {
         foreach (MergeRequest mergeRequest in comboBoxFilteredMergeRequests.Items.Cast<MergeRequest>())
         {
            if (mergeRequest.IId == mergeRequestIId)
            {
               comboBoxFilteredMergeRequests.SelectedItem = mergeRequest;
            }
         }

         if (comboBoxFilteredMergeRequests.SelectedItem != null)
         {
            richTextBoxMergeRequestDescription.Text = "Loading...";
            labelWorkflowStatus.Text = String.Format("Loading merge request with IId {0}...", mergeRequestIId);
         }

         enableMergeRequestActions(false);
         enableCommitActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         updateTotalTime(null);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Changing merge request to IId {0}",
            mergeRequestIId.ToString()));
      }

      private void onFailedChangeMergeRequest()
      {
         richTextBoxMergeRequestDescription.Text = String.Empty;
         labelWorkflowStatus.Text = "Failed to load merge request";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to change merge request"));
      }

      private void onMergeRequestChanged(WorkflowState state)
      {
         Debug.Assert(state.MergeRequest.IId != default(MergeRequest).IId);

         enableMergeRequestActions(true);
         updateMergeRequestDetails(state.MergeRequest);
         updateTimeTrackingMergeRequestDetails(state.MergeRequest);

         labelWorkflowStatus.Text = String.Format("Merge request with IId {0} loaded", state.MergeRequest.IId);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Merge request changed"));
      }

      private void onLoadCommits()
      {
         enableCommitActions(false);
         if (comboBoxFilteredMergeRequests.SelectedItem != null)
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

      private void onCommitsLoaded(WorkflowState state, List<Commit> commits)
      {
         if (commits.Count > 0)
         {
            enableCommitActions(true);

            addCommitsToComboBoxes(commits,
               state.MergeRequest.Diff_Refs.Base_SHA, state.MergeRequest.Target_Branch);

            enableComboBox(comboBoxLeftCommit);
            enableComboBox(comboBoxRightCommit);
         }

         labelWorkflowStatus.Text = String.Format("Loaded {0} commits", commits.Count);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loaded {0} commits", commits.Count));
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

      private void onTotalTimeLoaded(MergeRequestDescriptor mrd)
      {
         updateTotalTime(mrd);
         labelWorkflowStatus.Text = "Total spent time loaded";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Total spent time loaded"));
      }
   }
}

