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

         _workflow.PreSwitchHost += (hostname) => onSwitchHost(hostname);
         _workflow.PostSwitchHost += (user) => onHostSwitched(user);
         _workflow.FailedSwitchHost += () => onFailedSwitchHost();

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

         _workflow.PostLoadAllMergeRequests += () => onAllMergeRequestsLoaded();

         _workflow.PrelLoadSingleMergeRequest += (id) => onLoadSingleMergeRequest(id);
         _workflow.PostLoadSingleMergeRequest += (project, mergeRequest) => onSingleMergeRequestLoaded(mergeRequest);
         _workflow.FailedLoadSingleMergeRequest += () => onFailedLoadSingleMergeRequest();

         _workflow.PreLoadCommits += () => onLoadCommits();
         _workflow.PostLoadCommits += (mergeRequest, commits) => onCommitsLoaded(mergeRequest, commits);
         _workflow.FailedLoadCommits += () => onFailedLoadCommits();

         _workflow.PreLoadLatestVersion += () => onLoadLatestVersion();
         _workflow.PostLoadLatestVersion +=
            (hostname, project, mergeRequest, _) => onLatestVersionLoaded(hostname, project, mergeRequest);
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

         await startWorkflowAsync(hostname);
      }

      async private Task switchHostByUserAsync(string hostName)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change host to {0}",
            hostName));

         try
         {
            await startWorkflowAsync(hostName);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch host");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      async private Task startWorkflowAsync(string hostname)
      {
         // TODO - Test a case when a selected MR is hidden by filters
         bool shouldUseLastSelection = _lastMergeRequestsByHosts.ContainsKey(hostname);
         string projectname =
            shouldUseLastSelection ? _lastMergeRequestsByHosts[hostname].ProjectKey.ProjectName : String.Empty;
         int iid = shouldUseLastSelection ? _lastMergeRequestsByHosts[hostname].IId : 0;
         await _workflow.StartAsync(hostname, projectname, iid);
      }

      async private Task<bool> switchMergeRequestByUserAsync(string hostname, Project project, int mergeRequestIId)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change merge request to IId {0}",
            mergeRequestIId.ToString()));

         try
         {
            await _workflow.LoadMergeRequestAsync(hostname, project, mergeRequestIId);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot switch merge request");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
         }

         return true;
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onSwitchHost(string hostname)
      {
         if (hostname != String.Empty)
         {
            comboBoxHost.SelectedItem = new HostComboBoxItem
            {
               Host = hostname,
               AccessToken = _settings.GetAccessToken(hostname)
            };

            labelWorkflowStatus.Text = String.Format("Loading projects from host {0}...", hostname);
         }

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Switching host to {0}", hostname));
      }

      private void onFailedSwitchHost()
      {
         labelWorkflowStatus.Text = "Failed to switch host";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to switch host"));
      }

      private void onHostSwitched(User currentUser)
      {
         _currentUser = currentUser;

         labelWorkflowStatus.Text = "Host switched";

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Current user details: Id: {0}, Name: {1}, Username: {2}",
            currentUser.Id.ToString(), currentUser.Name, currentUser.Username));
      }

      private void onLoadHostProjects(string hostname)
      {
         enableMergeRequestFilterControls(false);
         enableMergeRequestActions(false);
         enableCommitActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         updateTotalTime(null);
         disableListView(listViewMergeRequests, true);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loading projects from {0}",
            hostname));
      }

      private void onFailedLoadHostProjects()
      {
         labelWorkflowStatus.Text = "Failed to load projects";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to change host"));
      }

      private void onHostProjectsLoaded(List<Project> projects)
      {
         listViewMergeRequests.Items.Clear();
         listViewMergeRequests.Groups.Clear();
         foreach (Project project in projects)
         {
            listViewMergeRequests.Groups.Add(project.Path_With_Namespace, project.Path_With_Namespace);
         }

         labelWorkflowStatus.Text = "Projects loaded";

         if (listViewMergeRequests.Groups.Count > 0)
         {
            enableListView(listViewMergeRequests);
         }

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loaded {0} projects", projects.Count));
      }

      private void onLoadAllMergeRequests()
      {
         enableMergeRequestFilterControls(false);
         enableMergeRequestActions(false);
         enableCommitActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null);
         updateTotalTime(null);
         disableListView(listViewMergeRequests, false);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);
      }

      private void onLoadProjectMergeRequests(Project project)
      {
         labelWorkflowStatus.Text = String.Format("Loading merge requests of project {0}...",
            project.Path_With_Namespace);

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Loading project {0}", project.Path_With_Namespace));
      }

      private void onFailedLoadProjectMergeRequests()
      {
         labelWorkflowStatus.Text = "Failed to load project";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to load project"));
      }

      private void onProjectMergeRequestsLoaded(string hostname, Project project, List<MergeRequest> mergeRequests)
      {
         mergeRequests = FilterMergeRequests(mergeRequests, _settings);

         foreach (var mergeRequest in mergeRequests)
         {
            addListViewMergeRequestItem(listViewMergeRequests, hostname, project, mergeRequest);
         }
         recalcRowHeightForMergeRequestListView(listViewMergeRequests);

         labelWorkflowStatus.Text = String.Format("Project {0} loaded", project.Path_With_Namespace);

         Trace.TraceInformation(String.Format(
            "[MainForm.Workflow] Project loaded. Loaded {0} merge requests", mergeRequests.Count));
      }

      private void onAllMergeRequestsLoaded()
      {
         if (listViewMergeRequests.Items.Count > 0 || _settings.CheckedLabelsFilter)
         {
            enableMergeRequestFilterControls(true);
         }

         if (listViewMergeRequests.Groups.Count > 0)
         {
            enableListView(listViewMergeRequests);
         }
      }

      private void onLoadSingleMergeRequest(int mergeRequestId)
      {
         foreach (ListViewItem item in listViewMergeRequests.Items)
         {
            if (int.Parse(item.SubItems[0].Text) == mergeRequestId)
            {
               // note - the same item might be already selected if it was user who caused MR loading event
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

      private void onFailedLoadSingleMergeRequest()
      {
         richTextBoxMergeRequestDescription.Text = String.Empty;
         labelWorkflowStatus.Text = "Failed to load merge request";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to change merge request"));
      }

      private void onSingleMergeRequestLoaded(MergeRequest mergeRequest)
      {
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

      private void onCommitsLoaded(MergeRequest mergeRequest, List<Commit> commits)
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

      private void onLatestVersionLoaded(string hostname, Project project, MergeRequest mergeRequest)
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
            }), new MergeRequestKey(hostname, project.Path_With_Namespace, mergeRequest.IId));
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

