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
using mrHelper.Client.Workflow;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      async private Task initializeWorkflow()
      {
         _workflow = _workflowManager.CreateWorkflow();
         _workflow.PreSwitchHost += (sender, e) => onChangeHost(e);
         _workflow.PostSwitchHost += (sender, state) => onHostChanged(state);
         _workflow.FailedSwitchHost += (sender, e) => onFailedChangeHost(e);

         _workflow.PreSwitchProject += (sender, e) => onChangeProject(e);
         _workflow.PostSwitchProject += (sender, state) => onProjectChanged(state);
         _workflow.FailedSwitchProject += (sender, e) => onFailedChangeProject(e);

         _workflow.PreSwitchMergeRequest += (sender, e) => onChangeMergeRequest(e);
         _workflow.PostSwitchMergeRequest += (sender, state) => onMergeRequestChanged(state);
         _workflow.FailedSwitchMergeRequest += (sender, e) => onFailedChangeMergeRequest(e);

         _workflowUpdateChecker = _updateManager.GetWorkflowUpdateChecker(_workflow, this);
         _workflowUpdateChecker.OnUpdate += async (sender, updates) =>
         {
            notifyOnMergeRequestUpdates(updates);

            WorkflowState state = _workflow.State;
            if (updates.NewMergeRequests.Any(x => x.Project_Id == state.Project.Id)
             || updates.UpdatedMergeRequests.Any(x => x.Project_Id == state.Project.Id)
             || updates.ClosedMergeRequests.Any(x => x.Project_Id == state.Project.Id))
            {
               // emulate project change to reload merge request list
               // This will automatically update commit list (if there are new ones).
               // This will also remove closed merge requests from the list.
               try
               {
                  await _workflow.SwitchProjectAsync(state.Project.Path_With_Namespace);
               }
               catch (WorkflowException ex)
               {
                  ExceptionHandlers.Handle(ex, "Workflow error occurred during auto-update");
               }
            }
         };

         string hostname = getInitialHostName();
         await changeHostAsync(hostname);
      }

      async private Task changeHostAsync(string hostName)
      {
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
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         disableComboBox(comboBoxFilteredMergeRequests, String.Empty);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);
      }

      private void onFailedChangeHost(bool cancelled)
      {
         if (cancelled)
         {
            return;
         }

         disableComboBox(comboBoxProjects, String.Empty);
         labelWorkflowStatus.Text = "Failed to change host";
      }

      private void onHostChanged(WorkflowState state)
      {
         Debug.Assert(comboBoxProjects.Items.Count == 0);
         foreach (var project in state.Projects)
         {
            comboBoxProjects.Items.Add(project);
         }

         if (comboBoxProjects.Items.Count > 0)
         {
            enableComboBox(comboBoxProjects);
         }

         labelWorkflowStatus.Text = String.Format("Selected host {0}", state.HostName);
      }

      async private Task changeProjectAsync(string projectName)
      {
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
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);
      }

      private void onFailedChangeProject(bool cancelled)
      {
         if (cancelled)
         {
            return;
         }

         disableComboBox(comboBoxFilteredMergeRequests, String.Empty);
         labelWorkflowStatus.Text = "Failed to change project";
      }

      private void onProjectChanged(WorkflowState state)
      {
         Debug.Assert(comboBoxFilteredMergeRequests.Items.Count == 0);
         foreach (var mergeRequest in state.MergeRequests)
         {
            comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
         }

         if (state.MergeRequests.Count > 0 || _settings.CheckedLabelsFilter)
         {
            enableMergeRequestFilterControls(true);
         }

         if (state.MergeRequests.Count > 0)
         {
            enableComboBox(comboBoxFilteredMergeRequests);
         }
         else
         {
            disableComboBox(comboBoxFilteredMergeRequests, String.Empty);
         }

         labelWorkflowStatus.Text = String.Format("Selected project {0}", state.Project.Path_With_Namespace);
      }

      async private Task changeMergeRequestAsync(int mergeRequestIId)
      {
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

         enableMergeRequestActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         if (comboBoxFilteredMergeRequests.SelectedItem != null)
         {
            disableComboBox(comboBoxLeftCommit, "Loading...");
            disableComboBox(comboBoxRightCommit, "Loading...");
            richTextBoxMergeRequestDescription.Text = "Loading...";
            labelWorkflowStatus.Text = "Loading merge request...";
         }
         else
         {
            disableComboBox(comboBoxLeftCommit, String.Empty);
            disableComboBox(comboBoxRightCommit, String.Empty);
         }
      }

      private void onFailedChangeMergeRequest(bool cancelled)
      {
         if (cancelled)
         {
            return;
         }

         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);
         richTextBoxMergeRequestDescription.Text = String.Empty;
         labelWorkflowStatus.Text = "Failed to change merge request";
      }

      private void onMergeRequestChanged(WorkflowState state)
      {
         Debug.Assert(state.MergeRequest.IId != default(MergeRequest).IId);

         enableMergeRequestActions(true);
         updateMergeRequestDetails(state.MergeRequest);
         updateTimeTrackingMergeRequestDetails(state.MergeRequest);

         if (state.Commits.Count > 0)
         {
            addCommitsToComboBoxes(state.Commits,
               state.MergeRequest.Diff_Refs.Base_SHA, state.MergeRequest.Target_Branch);

            enableComboBox(comboBoxLeftCommit);
            enableComboBox(comboBoxRightCommit);
         }
         else
         {
            disableComboBox(comboBoxLeftCommit, String.Empty);
            disableComboBox(comboBoxRightCommit, String.Empty);
         }

         labelWorkflowStatus.Text = String.Format("Selected merge request #{0}", state.MergeRequest.IId);
      }
   }
}

