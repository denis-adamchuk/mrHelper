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
         _workflow.BeforeSwitchHost += (sender, e) => onChangeHost();
         _workflow.AfterHostSwitched += (sender, state) => onHostChanged(state);
         _workflow.FailedSwitchHost += (sender, e) => onFailedChangeHost();

         _workflow.BeforeSwitchProject += (sender, e) => onChangeProject();
         _workflow.AfterProjectSwitched += (sender, state) => onProjectChanged(state);
         _workflow.FailedSwitchProject += (sender, e) => onFailedChangeProject();

         _workflow.BeforeSwitchMergeRequest += (sender, e) => onChangeMergeRequest();
         _workflow.AfterMergeRequestSwitched += (sender, state) => onMergeRequestChanged(state);
         _workflow.FailedSwitchMergeRequest += (sender, e) => onFailedChangeMergeRequest();

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

         string hostname = getInitialHostName();
         await changeHostAsync(hostname);
      }

      async private Task changeHostAsync(string hostName)
      {
         Debug.WriteLine("changeHostAsync(): Let's load host " + hostName);

         try
         {
            await _workflow.SwitchHostAsync(hostName);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch host");
            MessageBox.Show(String.Format("Cannot select host \"{0}\"", hostName), "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void onChangeHost()
      {
         Debug.WriteLine("onChangeHost(): Disable projects combo box and change its text to Loading...");
         prepareComboBoxToAsyncLoading(comboBoxProjects, true);

         Debug.WriteLine("onChangeHost(): Disable merge requests combo box");
         prepareComboBoxToAsyncLoading(comboBoxFilteredMergeRequests, false);

         Debug.WriteLine("onChangeHost(): Disable UI buttons and clean up text boxes related to current merge request");
         enableControlsOnChangedMergeRequest(null);

         Debug.WriteLine("onChangeHost(): Clean up comboboxes with lists of versions");
         addVersionsToComboBoxes(null, null, null);

         Debug.WriteLine("onChangeHost(): Update status bar");
         labelWorkflowStatus.Text = "Loading projects...";
      }

      private void onFailedChangeHost()
      {
         Debug.WriteLine("onFailedChangeHost(): Update status bar");
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("onFailedChangeHost(): Projects combo box remains disabled");
         Debug.WriteLine("onFailedChangeHost(): Merge Requests combo box remains disabled");
         Debug.WriteLine("onFailedChangeHost(): UI buttons remain disabled");
      }

      private void onHostChanged(WorkflowState state)
      {
         Debug.WriteLine("onHostChanged(): Update selected item in the list of Hosts");
         Debug.Assert(comboBoxHost.SelectedItem == null);
         comboBoxHost.SelectedItem = new HostComboBoxItem
         {
            Host = state.HostName,
            AccessToken = Tools.GetAccessToken(state.HostName, _settings)
         };

         Debug.WriteLine("onHostChanged(): Update lists of Projects");
         Debug.Assert(comboBoxProjects.Items.Count == 0);
         foreach (var project in state.Projects)
         {
            comboBoxProjects.Items.Add(project);
         }

         Debug.WriteLine("onHostChanged(): Update status bar");
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("onHostChanged(): Enable combo box with Projects");
         fixComboBoxAfterAsyncLoading(comboBoxProjects);

         Debug.WriteLine("onHostChanged(): Merge Requests combo box remains disabled");
      }

      async private Task changeProjectAsync(string projectName)
      {
         Debug.WriteLine("changeProjectsAsync(): Let's load project " + projectName);

         try
         {
            await _workflow.SwitchProjectAsync(projectName);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch project");
            MessageBox.Show("Cannot select this project", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void onChangeProject()
      {
         Debug.WriteLine("onChangeProject(): Disable UI buttons and clean up text boxes related to current merge request");
         enableControlsOnChangedMergeRequest(null);

         Debug.WriteLine("onChangeProject(): Clean up comboboxes with lists of versions");
         addVersionsToComboBoxes(null, null, null);

         Debug.WriteLine("onChangeProject(): Disable combo box with Merge Requests and change its text to Loading...");
         prepareComboBoxToAsyncLoading(comboBoxFilteredMergeRequests, true);

         Debug.WriteLine("onChangeProject(): Update status bar");
         labelWorkflowStatus.Text = "Loading merge requests...";
      }

      private void onFailedChangeProject()
      {
         Debug.WriteLine("onFailedChangeProject(): Update status bar");
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("onFailedChangeProject(): Merge Requests combo box remains disabled");
         Debug.WriteLine("onFailedChangeProject(): UI buttons remain disabled");
      }

      private void onProjectChanged(WorkflowState state)
      {
         Debug.WriteLine("onProjectChanged(): Update selected item in the list of Projects");
         //Debug.Assert(comboBoxProjects.SelectedItem == null);
         comboBoxProjects.SelectedItem = state.Project;

         Debug.WriteLine("onProjectChanged(): Update lists of Merge Requests");
         Debug.Assert(comboBoxFilteredMergeRequests.Items.Count == 0);
         foreach (var mergeRequest in state.MergeRequests)
         {
            comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
         }

         Debug.WriteLine("onProjectChanged(): Update status bar");
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("onProjectChanged(): Enable merge requests combo box");
         fixComboBoxAfterAsyncLoading(comboBoxFilteredMergeRequests);
      }

      async private Task changeMergeRequestAsync(int mergeRequestIId)
      {
         Debug.WriteLine("changeMergeRequestAsync(): Let's load merge request with Id " + mergeRequestIId.ToString());
         try
         {
            await _workflow.SwitchMergeRequestAsync(mergeRequestIId);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch merge request");
            MessageBox.Show("Cannot select this merge request", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void onChangeMergeRequest()
      {
         Debug.WriteLine("onChangeMergeRequest(): Disable UI buttons and clean up text boxes related to current merge request");
         enableControlsOnChangedMergeRequest(null);

         Debug.WriteLine("onChangeMergeRequest(): Clean up comboboxes with lists of versions");
         addVersionsToComboBoxes(null, null, null);

         Debug.WriteLine("onChangeMergeRequest(): Change text in textboxes to Loading...");
         textBoxMergeRequestName.Text = "Loading...";
         richTextBoxMergeRequestDescription.Text = "Loading...";

         Debug.WriteLine("onChangeMergeRequest(): Disable combo boxes with Versions and change their texts to Loading...");
         prepareComboBoxToAsyncLoading(comboBoxLeftVersion, true);
         prepareComboBoxToAsyncLoading(comboBoxRightVersion, true);

         Debug.WriteLine("onChangeMergeRequest(): Update status bar");
         labelWorkflowStatus.Text = "Loading merge request...";
      }

      private void onFailedChangeMergeRequest()
      {
         Debug.WriteLine("onFailedChangeMergeRequest(): Update status bar");
         labelWorkflowStatus.Text = String.Empty;

         Debug.WriteLine("onFailedChangeMergeRequest(): Comboboxes with Versions remain disabled");
         Debug.WriteLine("onFailedChangeMergeRequest(): UI buttons remain disabled");
      }

      private void onMergeRequestChanged(WorkflowState state)
      {
         Debug.WriteLine("onMergeRequestChanged(): Finished loading MR. Current selected MR is " + _workflow.State.MergeRequest.IId);

         Debug.WriteLine("onMergeRequestChanged(): Update selected item in the list of Merge Requests");
         Debug.Assert(comboBoxFilteredMergeRequests.SelectedItem == null);
         comboBoxFilteredMergeRequests.SelectedItem = state.MergeRequest;

         Debug.WriteLine("onMergeRequestChanged(): Enable UI buttons and fill text boxes related to current merge request");
         enableControlsOnChangedMergeRequest(state.MergeRequest);

         Debug.WriteLine("onMergeRequestChanged(): Fill comboboxes with lists of versions");
         addVersionsToComboBoxes(state.Versions,
            state.MergeRequest.Diff_Refs.Base_SHA, state.MergeRequest.Target_Branch);

         Debug.WriteLine("onMergeRequestChanged(): Enable combo boxes with lists of versions");
         fixComboBoxAfterAsyncLoading(comboBoxLeftVersion);
         fixComboBoxAfterAsyncLoading(comboBoxRightVersion);

         _commitChecker = _updateManager.GetCommitChecker(_workflow.State.MergeRequestDescriptor);
      }
   }
}

