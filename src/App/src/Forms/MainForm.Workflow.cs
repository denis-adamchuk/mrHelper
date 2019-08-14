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
   internal partial class mrHelperForm
   {
      async private Task initializeWorkflow()
      {
         Debug.WriteLine("Initializing workflow");

         _workflow = _workflowManager.CreateWorkflow();
         _workflow.HostSwitched += (sender, state) => onHostChanged(state);
         _workflow.ProjectSwitched += (sender, state) => onProjectChanged(state);
         _workflow.MergeRequestSwitched += (sender, state) => onMergeRequestChanged(state);

         _workflowUpdateChecker = _updateManager.GetWorkflowUpdateChecker(_workflow);
         _workflowUpdateChecker.OnUpdate += async (sender, updates) =>
         {
            notifyOnMergeRequestUpdates(updates);

            WorkflowState state = _workflow.State;
            if (updates.NewMergeRequests.Any(x => x.Project_Id == state.Project.Id)
             || updates.UpdatedMergeRequests.Any(x => x.Project_Id == state.Project.Id))
            {
               // emulate project change to reload merge request list
               // This will automatically update version list (if there are new ones).
               // This will also remove merged merge requests from the list.
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

         Debug.WriteLine("Disable projects combo box");
         prepareComboBoxToAsyncLoading(comboBoxProjects);

         labelWorkflowStatus.Text = "Initializing...";
         await _workflow.SwitchHostAsync(getInitialHostName());
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("Enable projects combo box");
         fixComboBoxAfterAsyncLoading(comboBoxProjects);
      }

      async private Task onChangeHost(string hostName)
      {
         Debug.WriteLine("Update projects dropdown list");

         Debug.WriteLine("Disable projects combo box");
         prepareComboBoxToAsyncLoading(comboBoxProjects);

         labelWorkflowStatus.Text = "Loading projects...";
         try
         {
            await _workflow.SwitchHostAsync(hostName);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch host");
            MessageBox.Show("Cannot select this host", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("Enable projects combo box");
         fixComboBoxAfterAsyncLoading(comboBoxProjects);
      }

      private void onHostChanged(WorkflowState state)
      {
         comboBoxHost.SelectedItem = new HostComboBoxItem
         {
            Host = state.HostName,
            AccessToken = Tools.GetAccessToken(state.HostName, _settings)
         };
         foreach (var project in state.Projects)
         {
            comboBoxProjects.Items.Add(project);
         }
      }

      async private Task onChangeProject(string projectName)
      {
         Debug.WriteLine("Update merge requests dropdown list");

         Debug.WriteLine("Disable merge requests combo box");
         prepareComboBoxToAsyncLoading(comboBoxFilteredMergeRequests);

         labelWorkflowStatus.Text = "Loading merge requests...";
         try
         {
            await _workflow.SwitchProjectAsync(projectName);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch project");
            MessageBox.Show("Cannot select this project", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("Enable merge requests combo box");
         fixComboBoxAfterAsyncLoading(comboBoxFilteredMergeRequests);
      }

      private void onProjectChanged(WorkflowState state)
      {
         comboBoxProjects.SelectedItem = state.Project;
         foreach (var mergeRequest in state.MergeRequests)
         {
            comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
         }
      }

      async private Task onChangeMergeRequest(int mergeRequestIId)
      {
         Debug.WriteLine("Let's handle merge request selection");

         Debug.WriteLine("Disable UI controls");
         enableControlsOnChangedMergeRequest(null);
         addVersionsToComboBoxes(null, null, null);

         textBoxMergeRequestName.Text = "Loading...";
         richTextBoxMergeRequestDescription.Text = "Loading...";

         prepareComboBoxToAsyncLoading(comboBoxLeftVersion);
         prepareComboBoxToAsyncLoading(comboBoxRightVersion);

         Debug.WriteLine("Let's load merge request with Id " + mergeRequestIId.ToString());
         try
         {
            await _workflow.SwitchMergeRequestAsync(mergeRequestIId);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch merge request");
            MessageBox.Show("Cannot select this merge request", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         Debug.WriteLine("Finished loading MR. Current selected MR is " + _workflow.State.MergeRequest.IId);

         fixComboBoxAfterAsyncLoading(comboBoxLeftVersion);
         fixComboBoxAfterAsyncLoading(comboBoxRightVersion);
      }

      private void onMergeRequestChanged(WorkflowState state)
      {
         comboBoxFilteredMergeRequests.SelectedItem = state.MergeRequest;
         enableControlsOnChangedMergeRequest(state.MergeRequest);
         addVersionsToComboBoxes(state.Versions,
            state.MergeRequest.Diff_Refs.Base_SHA, state.MergeRequest.Target_Branch);

         _commitChecker = _updateManager.GetCommitChecker(_workflow.State.MergeRequestDescriptor);
      }
   }
}

