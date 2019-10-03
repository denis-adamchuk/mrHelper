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

         _workflow.PreLoadProject += (projectname) => onLoadProject(projectname);
         _workflow.PostLoadProject +=
            (project, mergeRequests) =>
         {
            onProjectLoaded(project, mergeRequests);
            cleanupReviewedCommits(_workflow.State.HostName, project.Path_With_Namespace, mergeRequests);
         };
         _workflow.FailedLoadProject += () => onFailedLoadProject();

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

            labelWorkflowStatus.Text = "Loading projects...";
         }
         else
         {
         }

         enableMergeRequestFilterControls(false);
         enableMergeRequestActions(false);
         enableCommitActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         updateTotalTime(null);
         disableListView(listViewMergeRequests, true);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Changing host to {0}",
            hostname));
      }

      private void onFailedChangeHost()
      {
         labelWorkflowStatus.Text = "Failed to load projects";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to change host"));
      }

      private void onHostChanged(User currentUser, List<Project> projects)
      {
         labelWorkflowStatus.Text = "Projects loaded";

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Host changed. Loaded {0} projects", projects.Count));
         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Current user details: Id: {0}, Name: {1}, Username: {2}",
            currentUser.Id.ToString(), currentUser.Name, currentUser.Username));
      }

      private void onLoadProject(string projectName)
      {
         disableListView(listViewMergeRequests, false);
         if (projectName != String.Empty)
         {
            labelWorkflowStatus.Text = "Loading merge requests...";
         }

         enableMergeRequestFilterControls(false);
         enableMergeRequestActions(false);
         enableCommitActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         updateTotalTime(null);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loading project {0}",
            projectName));
      }

      private void onFailedLoadProject()
      {
         labelWorkflowStatus.Text = "Failed to load project";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to load project"));
      }

      private void onProjectLoaded(Project project, List<MergeRequest> mergeRequests)
      {
         mergeRequests = Tools.FilterMergeRequests(mergeRequests, _settings);

         foreach (var mergeRequest in mergeRequests)
         {
            var item = new ListViewItem(mergeRequest.Id.ToString());
            item.SubItems.Add(String.Empty);
            item.SubItems.Add(String.Empty);
            item.SubItems.Add(String.Empty);
            item.Tag = new Tuple<string, MergeRequest>(project.Path_With_Namespace, mergeRequest);
            listViewMergeRequests.Items.Add(item);
         }

         if (mergeRequests.Count > 0 || _settings.CheckedLabelsFilter)
         {
            enableMergeRequestFilterControls(true);
         }

         if (listViewMergeRequests.Items.Count > 0)
         {
            enableListView(listViewMergeRequests);
         }
         else
         {
            disableListView(listViewMergeRequests, true);
         }

         labelWorkflowStatus.Text = String.Format("Project {0} loaded", project.Path_With_Namespace);

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Project loaded. Loaded {0} merge requests", mergeRequests.Count));
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
         foreach (ListViewItem item in listViewMergeRequests.Items)
         {
            if (int.Parse(item.SubItems[0].Text) == mergeRequestId)
            {
               item.Selected = true;
               break;
            }
         }

         if (listViewMergeRequests.SelectedItems.Count != 0)
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

         // Making it asynchronous here guarantees that UpdateManager updates the cache before we access it
         BeginInvoke(new Action<MergeRequestKey>(
            async (mrk) =>
            {
               GitClient client = getGitClient(mrk.ProjectKey);
               if (client == null || client.DoesRequireClone())
               {
                  Trace.TraceInformation(String.Format("[MainForm.Workflow] Cannot update git repository silently: {0}",
                     (client == null ? "client is null" : "must be cloned first")));
                  return;
               }

               Trace.TraceInformation("[MainForm.Workflow] Going to update git repository silently");

               IInstantProjectChecker instantChecker = _updateManager.GetLocalProjectChecker(mrk);
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
            }), _workflow.State.MergeRequestKey);
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
   }
}

