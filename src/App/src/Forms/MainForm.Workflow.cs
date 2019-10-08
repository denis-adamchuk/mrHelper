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
         _workflow = new Workflow(_settings,
            (message) => MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information));

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

         _workflow.PostLoadAllMergeRequests += () => onAllMergeRequestsLoaded();

         _workflow.PrelLoadSingleMergeRequest += (id) => onLoadSingleMergeRequest(id);
         _workflow.PostLoadSingleMergeRequest += (_, mergeRequest) => onSingleMergeRequestLoaded(mergeRequest);
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

         await switchHostByUserAsync(hostname);
      }

      async private Task switchHostByUserAsync(string hostName)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] User requested to change host to {0}",
            hostName));

         bool shouldUseLastSelection = _lastMergeRequestsByHosts.ContainsKey(hostName);
         string projectname = shouldUseLastSelection ?
            _lastMergeRequestsByHosts[hostName].ProjectKey.ProjectName : String.Empty;
         int iid = shouldUseLastSelection ? _lastMergeRequestsByHosts[hostName].IId : 0;

         try
         {
            await startWorkflowAsync(hostName, projectname, iid, true, false);
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

      async private Task<bool> startWorkflowAsync(string hostname, string projectname, int iid,
         bool reloadAll, bool exact)
      {
         labelWorkflowStatus.Text = String.Empty;

         if (reloadAll)
         {
            await _workflow.LoadCurrentUserAsync(hostname);
            await _workflow.LoadAllMergeRequestsAsync(hostname);
         }

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
         Trace.TraceInformation(String.Format("[MainForm.Workflow] Connecting host to {0}", hostname));
      }

      private void onFailedLoadCurrentUser()
      {
         labelWorkflowStatus.Text = "Failed to connect";

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Failed to switch host"));
      }

      private void onCurrentUserLoaded(User currentUser)
      {
         _currentUser = currentUser;

         labelWorkflowStatus.Text = "Connected to host";

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

         _allMergeRequests.Clear();
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

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadSingleMergeRequest(int mergeRequestIId)
      {
         if (listViewMergeRequests.SelectedItems.Count != 0)
         {
            richTextBoxMergeRequestDescription.Text = "Loading...";
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

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Changing merge request to IId {0}",
            mergeRequestIId.ToString()));
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

      private void onLatestVersionLoaded(string hostname, string projectname, MergeRequest mergeRequest)
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
            }), new MergeRequestKey(hostname, projectname, mergeRequest.IId));
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

