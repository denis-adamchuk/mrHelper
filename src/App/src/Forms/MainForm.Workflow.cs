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

      private void onChangeHost(string hostname)
      {
         Debug.WriteLine("onChangeHost(): Update selected item in the list of Hosts");
         comboBoxHost.SelectedItem = new HostComboBoxItem
         {
            Host = hostname,
            AccessToken = Tools.GetAccessToken(hostname, _settings)
         };

         Debug.WriteLine("onChangeHost(): Disable projects combo box and change its text to Loading...");
         prepareComboBoxToAsyncLoading(comboBoxProjects, comboBoxHost.SelectedItem != null);

         Debug.WriteLine("onChangeHost(): Disable merge requests combo box");
         prepareComboBoxToAsyncLoading(comboBoxFilteredMergeRequests, false);

         Debug.WriteLine("onChangeHost(): Disable combo boxes with Versions...");
         prepareComboBoxToAsyncLoading(comboBoxLeftVersion, false);
         prepareComboBoxToAsyncLoading(comboBoxRightVersion, false);

         Debug.WriteLine("onChangeHost(): Disable UI buttons and clean up text boxes related to current merge request");
         buttonApplyLabels.Enabled = false;
         enableControlsOnChangedMergeRequest(null);

         Debug.WriteLine("onChangeHost(): Clean up comboboxes with lists of versions");
         addVersionsToComboBoxes(null, null, null);

         if (comboBoxHost.SelectedItem != null)
         {
            Debug.WriteLine("onChangeHost(): Update status bar");
            labelWorkflowStatus.Text = "Loading projects...";
         }
      }

      private void onFailedChangeHost(bool cancelled)
      {
         if (cancelled)
         {
            return;
         }

         Debug.WriteLine("onFailedChangeHost(): Update status bar");
         labelWorkflowStatus.Text = "Failed to change host";

         Debug.WriteLine("onFailedChangeHost(): Projects combo box remains disabled");
         Debug.WriteLine("onFailedChangeHost(): Merge Requests combo box remains disabled");
         Debug.WriteLine("onFailedChangeHost(): UI buttons remain disabled");
      }

      private void onHostChanged(WorkflowState state)
      {
         Debug.WriteLine("onHostChanged(): Update lists of Projects");
         Debug.Assert(comboBoxProjects.Items.Count == 0);
         foreach (var project in state.Projects)
         {
            comboBoxProjects.Items.Add(project);
         }

         Debug.WriteLine("onHostChanged(): Update status bar");
         labelWorkflowStatus.Text = String.Format("Selected host {0}", state.HostName);

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

      private void onChangeProject(string projectName)
      {
         Debug.WriteLine("onChangeProject(): Update selected item in the list of Projects");
         foreach (Project project in comboBoxProjects.Items.Cast<Project>())
         {
            if (project.Path_With_Namespace == projectName)
            {
               comboBoxProjects.SelectedItem = project;
            }
         }

         Debug.WriteLine("onChangeProject(): Disable UI buttons and clean up text boxes related to current merge request");
         buttonApplyLabels.Enabled = false;
         enableControlsOnChangedMergeRequest(null);

         Debug.WriteLine("onChangeProject(): Clean up comboboxes with lists of versions");
         addVersionsToComboBoxes(null, null, null);

         Debug.WriteLine("onChangeProject(): Disable combo box with Merge Requests and change its text to Loading...");
         prepareComboBoxToAsyncLoading(comboBoxFilteredMergeRequests, comboBoxProjects.SelectedItem != null);

         Debug.WriteLine("onChangeProject(): Disable combo boxes with Versions...");
         prepareComboBoxToAsyncLoading(comboBoxLeftVersion, false);
         prepareComboBoxToAsyncLoading(comboBoxRightVersion, false);

         if (comboBoxProjects.SelectedItem != null)
         {
            Debug.WriteLine("onChangeProject(): Update status bar");
            labelWorkflowStatus.Text = "Loading merge requests...";
         }
      }

      private void onFailedChangeProject(bool cancelled)
      {
         if (cancelled)
         {
            return;
         }

         Debug.WriteLine("onFailedChangeProject(): Update status bar");
         labelWorkflowStatus.Text = "Failed to change project";

         Debug.WriteLine("onFailedChangeProject(): Merge Requests combo box remains disabled");
         Debug.WriteLine("onFailedChangeProject(): UI buttons remain disabled");
      }

      private void onProjectChanged(WorkflowState state)
      {
         Debug.WriteLine("onProjectChanged(): Update lists of Merge Requests");
         Debug.Assert(comboBoxFilteredMergeRequests.Items.Count == 0);
         foreach (var mergeRequest in state.MergeRequests)
         {
            comboBoxFilteredMergeRequests.Items.Add(mergeRequest);
         }

         if (state.MergeRequests.Count > 0)
         {
            buttonApplyLabels.Enabled = true;
         }

         Debug.WriteLine("onProjectChanged(): Update status bar");
         labelWorkflowStatus.Text = String.Format("Selected project {0}", state.Project.Path_With_Namespace);

         Debug.WriteLine("onProjectChanged(): Enable merge requests combo box");
         fixComboBoxAfterAsyncLoading(comboBoxFilteredMergeRequests);

         setCommitChecker();
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

      private void onChangeMergeRequest(int mergeRequestIId)
      {
         Debug.WriteLine("onMergeRequestChanged(): Update selected item in the list of Merge Requests immediately");
         foreach (MergeRequest mergeRequest in comboBoxFilteredMergeRequests.Items.Cast<MergeRequest>())
         {
            if (mergeRequest.IId == mergeRequestIId)
            {
               comboBoxFilteredMergeRequests.SelectedItem = mergeRequest;
            }
         }

         Debug.WriteLine("onChangeMergeRequest(): Disable UI buttons and clean up text boxes related to current merge request");
         enableControlsOnChangedMergeRequest(null);

         Debug.WriteLine("onChangeMergeRequest(): Clean up comboboxes with lists of versions");
         addVersionsToComboBoxes(null, null, null);

         Debug.WriteLine("onChangeMergeRequest(): Change text in textboxes to Loading...");
         richTextBoxMergeRequestDescription.Text = "Loading...";

         Debug.WriteLine("onChangeMergeRequest(): Disable combo boxes with Versions and change their texts to Loading...");
         prepareComboBoxToAsyncLoading(comboBoxLeftVersion, comboBoxFilteredMergeRequests.SelectedItem != null);
         prepareComboBoxToAsyncLoading(comboBoxRightVersion, comboBoxFilteredMergeRequests.SelectedItem != null);

         if (comboBoxFilteredMergeRequests.SelectedItem != null)
         {
            Debug.WriteLine("onChangeMergeRequest(): Update status bar");
            labelWorkflowStatus.Text = "Loading merge request...";
         }
      }

      private void onFailedChangeMergeRequest(bool cancelled)
      {
         if (cancelled)
         {
            return;
         }

         Debug.WriteLine("onFailedChangeMergeRequest(): Update status bar");
         labelWorkflowStatus.Text = "Failed to change merge request";

         Debug.WriteLine("onFailedChangeMergeRequest(): Comboboxes with Versions remain disabled");
         Debug.WriteLine("onFailedChangeMergeRequest(): UI buttons remain disabled");
      }

      private void onMergeRequestChanged(WorkflowState state)
      {
         Debug.WriteLine("onMergeRequestChanged(): Finished loading MR. Current selected MR is " + _workflow.State.MergeRequest.IId);

         Debug.WriteLine("onMergeRequestChanged(): Enable UI buttons and fill text boxes related to current merge request");
         enableControlsOnChangedMergeRequest(state.MergeRequest);

         Debug.WriteLine("onMergeRequestChanged(): Fill comboboxes with lists of versions");
         addVersionsToComboBoxes(state.Versions,
            state.MergeRequest.Diff_Refs.Base_SHA, state.MergeRequest.Target_Branch);

         Debug.WriteLine("onMergeRequestChanged(): Enable combo boxes with lists of versions");
         fixComboBoxAfterAsyncLoading(comboBoxLeftVersion);
         fixComboBoxAfterAsyncLoading(comboBoxRightVersion);

         Debug.WriteLine("onMergeRequestChanged(): Update status bar");
         labelWorkflowStatus.Text = String.Format("Selected merge request #{0}", state.MergeRequest.IId);

         setCommitChecker();
      }
   }
}

