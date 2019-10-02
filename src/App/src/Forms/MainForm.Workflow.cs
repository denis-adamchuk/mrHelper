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
         _workflow = _workflowFactory.CreateWorkflow();
         _workflow.PreSwitchHost += (hostname) => onChangeHost(hostname);
         _workflow.PostSwitchHost += (user, projects) => onHostChanged(user, projects);
         _workflow.FailedSwitchHost += () => onFailedChangeHost();

         _workflow.PreLoadProject += (projectname) => onChangeProject(projectname);
         _workflow.PostLoadProject +=
            (project, mergeRequests) =>
         {
            onProjectChanged(project, mergeRequests);
            cleanupReviewedCommits(_workflow.State.HostName, project.Path_With_Namespace, mergeRequests);
         };
         _workflow.FailedLoadProject += () => onFailedChangeProject();

         _workflow.PreSwitchMergeRequest += (id) => onChangeMergeRequest(id);
         _workflow.PostSwitchMergeRequest += () => onMergeRequestChanged();
         _workflow.FailedSwitchMergeRequest += () => onFailedChangeMergeRequest();

         _workflow.PreLoadCommits += () => onLoadCommits();
         _workflow.PostLoadCommits += (commits) => onCommitsLoaded(commits);
         _workflow.FailedLoadCommits += () => onFailedLoadCommits();

         _workflow.PreLoadLatestVersion += () => onLoadLatestVersion();
         _workflow.PostLoadLatestVersion += (version) => onLatestVersionLoaded();
         _workflow.FailedLoadLatestVersion += () => onFailedLoadLatestVersion();
      }

      async private Task initializeWorkflow()
      {
         string[] arguments = Environment.GetCommandLineArgs();
         if (arguments.Length > 1 && await connectToUrlAsync(arguments[1]))
         {
            return;
         }

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

      private void onHostChanged(User currentUser, List<Project> projects)
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
            currentUser.Id.ToString(), currentUser.Name, currentUser.Username));
      }

      async private Task changeProjectAsync(string projectName)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change project to {0}",
            projectName));

         try
         {
            await _workflow.LoadProjectAsync(projectName);
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

      private void onProjectChanged(Project project, List<MergeRequest> mergeRequests)
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

         labelWorkflowStatus.Text = String.Format("Project {0} selected", project.Path_With_Namespace);

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Project changed. Loaded {0} merge requests", mergeRequests.Count));
      }

      async private Task changeMergeRequestAsync(int mergeRequestId)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change merge request to Id {0}",
            mergeRequestId.ToString()));

         try
         {
            await _workflow.SwitchMergeRequestAsync(mergeRequestId);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch merge request");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void onChangeMergeRequest(int mergeRequestId)
      {
         foreach (MergeRequest mergeRequest in comboBoxFilteredMergeRequests.Items.Cast<MergeRequest>())
         {
            if (mergeRequest.Id == mergeRequestId)
            {
               comboBoxFilteredMergeRequests.SelectedItem = mergeRequest;
            }
         }

         if (comboBoxFilteredMergeRequests.SelectedItem != null)
         {
            richTextBoxMergeRequestDescription.Text = "Loading...";
            labelWorkflowStatus.Text = String.Format("Loading merge request with Id {0}...", mergeRequestId);
         }

         enableMergeRequestActions(false);
         enableCommitActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         updateTotalTime(null);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Changing merge request to Id {0}",
            mergeRequestId.ToString()));
      }

      private void onFailedChangeMergeRequest()
      {
         richTextBoxMergeRequestDescription.Text = String.Empty;
         labelWorkflowStatus.Text = "Failed to load merge request";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to change merge request"));
      }

      private void onMergeRequestChanged()
      {
         MergeRequest mergeRequest = _workflow.State.MergeRequest;
         Debug.Assert(mergeRequest.Id != default(MergeRequest).Id);

         enableMergeRequestActions(true);
         updateMergeRequestDetails(mergeRequest);
         updateTimeTrackingMergeRequestDetails(mergeRequest);

         labelWorkflowStatus.Text = String.Format("Merge request with Id {0} loaded", mergeRequest.Id);

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

      private void onCommitsLoaded(List<Commit> commits)
      {
         if (commits.Count > 0)
         {
            enableComboBox(comboBoxLeftCommit);
            enableComboBox(comboBoxRightCommit);

            MergeRequest mergeRequest = _workflow.State.MergeRequest;
            addCommitsToComboBoxes(commits, mergeRequest.Diff_Refs.Base_SHA, mergeRequest.Target_Branch);

            enableCommitActions(true);
         }

         labelWorkflowStatus.Text = String.Format("Loaded {0} commits", commits.Count);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loaded {0} commits", commits.Count));
      }

      private void onLoadLatestVersion()
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loading latest version"));
      }

      private void onFailedLoadLatestVersion()
      {
         labelWorkflowStatus.Text = "Failed to load latest version";
         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to load latest version"));
      }

      private void onLatestVersionLoaded()
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] Latest version loaded"));

         MergeRequestKey mrk = _workflow.State.MergeRequestKey;
         MergeRequestDescriptor mrd = _workflow.State.MergeRequestDescriptor;

         // Making it asynchronous here guarantees that UpdateManager updates the cache before we access it
         BeginInvoke(new Action<string, string, MergeRequestKey>(
            async (hostname, projectname, key) =>
            {
               GitClient client = getGitClient(hostname, projectname);
               if (client == null || client.DoesRequireClone())
               {
                  Trace.TraceInformation(String.Format("[MainForm.Workflow] Cannot update git repository silently: {0}",
                     (client == null ? "client is null" : "must be cloned first")));
                  return;
               }

               Trace.TraceInformation("[MainForm.Workflow] Going to update git repository silently");

               IInstantProjectChecker instantChecker = _updateManager.GetLocalProjectChecker(key);
               try
               {
                  await client.Updater.ManualUpdateAsync(instantChecker, null);
               }
               catch (GitOperationException)
               {
                  Trace.TraceInformation("[MainForm.Workflow] Silent update cancelled");
                  return;
               }

               Trace.TraceInformation("[MainForm.Workflow] Silent update finished");
            }), mrd.HostName, mrd.ProjectName, mrk);
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

