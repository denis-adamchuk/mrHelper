using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.Client.Types;
using mrHelper.Client.Workflow;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void createHistWorkflow()
      {
         _histWorkflowManager = new WorkflowManager(Program.Settings);
      }

      private void subscribeToHistWorkflow()
      {
         _histWorkflowManager.PreLoadCurrentUser += onLoadCurrentUser;
         _histWorkflowManager.PostLoadCurrentUser += onCurrentUserLoaded;
         _histWorkflowManager.FailedLoadCurrentUser += onFailedLoadCurrentUser;

         _histWorkflowManager.PostLoadProjectMergeRequests += onProjectHistMergeRequestsLoaded;
         _histWorkflowManager.FailedLoadProjectMergeRequests += onFailedLoadProjectHistMergeRequests;

         _histWorkflowManager.PreLoadSingleMergeRequest += onLoadSingleHistMergeRequest;
         _histWorkflowManager.PostLoadSingleMergeRequest += onSingleHistMergeRequestLoaded;
         _histWorkflowManager.FailedLoadSingleMergeRequest += onFailedLoadSingleHistMergeRequest;

         _histWorkflowManager.PreLoadCommits += onLoadHistCommits;
         _histWorkflowManager.PostLoadCommits += onHistCommitsLoaded;
         _histWorkflowManager.FailedLoadCommits +=  onFailedLoadHistCommits;
      }

      private void unsubscribeFromHistWorkflow()
      {
         _histWorkflowManager.PreLoadCurrentUser -= onLoadCurrentUser;
         _histWorkflowManager.PostLoadCurrentUser -= onCurrentUserLoaded;
         _histWorkflowManager.FailedLoadCurrentUser -= onFailedLoadCurrentUser;

         _histWorkflowManager.PostLoadProjectMergeRequests -= onProjectHistMergeRequestsLoaded;
         _histWorkflowManager.FailedLoadProjectMergeRequests -= onFailedLoadProjectHistMergeRequests;

         _histWorkflowManager.PreLoadSingleMergeRequest -= onLoadSingleHistMergeRequest;
         _histWorkflowManager.PostLoadSingleMergeRequest -= onSingleHistMergeRequestLoaded;
         _histWorkflowManager.FailedLoadSingleMergeRequest -= onFailedLoadSingleHistMergeRequest;

         _histWorkflowManager.PreLoadCommits -= onLoadHistCommits;
         _histWorkflowManager.PostLoadCommits -= onHistCommitsLoaded;
         _histWorkflowManager.FailedLoadCommits -=  onFailedLoadHistCommits;
      }

      async private void TextBoxHistSearch_KeyDown(object sender, KeyEventArgs e)
      {
         if (e.KeyCode == Keys.Enter)
         {
            await searchHistMergeRequests(textBoxHistSearch.Text);
         }
      }

      async private void ListViewHistMergeRequests_ItemSelectionChanged(
         object sender, ListViewItemSelectionChangedEventArgs e)
      {
         ListView listView = (sender as ListView);
         listView.Refresh();

         if (listView.SelectedItems.Count < 1)
         {
            // had to use this hack, because it is not possible to prevent deselect on a click on empty area in ListView
            await switchHistMergeRequestByUserAsync(default(ProjectKey), 0);
            return;
         }

         FullMergeRequestKey key = (FullMergeRequestKey)(listView.SelectedItems[0].Tag);
         await switchHistMergeRequestByUserAsync(key.ProjectKey, key.MergeRequest.IId);
      }

      async private Task searchHistMergeRequests(string query)
      {
         try
         {
            await startHistWorkflowAsync(getHostName(), query);
         }
         catch (Exception ex)
         {
            if (ex is WorkflowException || ex is UnknownHostException)
            {
               disableAllHistUIControls(true);
               ExceptionHandlers.Handle("Cannot perform merge request search", ex);
               string message = ex.Message;
               if (ex is WorkflowException wx)
               {
                  message = wx.UserMessage;
               }
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }
            throw;
         }
      }

      async private Task<bool> switchHistMergeRequestByUserAsync(ProjectKey projectKey, int mergeRequestIId)
      {
         Trace.TraceInformation(String.Format("[MainForm.History] User requested to change merge request to IId {0}",
            mergeRequestIId.ToString()));

         if (mergeRequestIId == 0)
         {
            onLoadSingleHistMergeRequest(0);
            await _histWorkflowManager.CancelAsync();
            return false;
         }

         await _histWorkflowManager.CancelAsync();

         try
         {
            return await _histWorkflowManager.LoadMergeRequestAsync(
               projectKey.HostName, projectKey.ProjectName, mergeRequestIId);
         }
         catch (WorkflowException ex)
         {
            ExceptionHandlers.Handle("Cannot switch merge request", ex);
            MessageBox.Show(ex.UserMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }

         return false;
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      async private Task startHistWorkflowAsync(string hostname, string query)
      {
         labelHistWorkflowStatus.Text = String.Empty;

         await _histWorkflowManager.CancelAsync();
         if (String.IsNullOrWhiteSpace(hostname) || String.IsNullOrWhiteSpace(query))
         {
            disableAllHistUIControls(true);
            return;
         }

         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         disableAllHistUIControls(true);
         if (!_currentUser.ContainsKey(hostname))
         {
            if (!await _histWorkflowManager.LoadCurrentUserAsync(hostname))
            {
               return;
            }
         }

         await loadAllHistMergeRequests(hostname, query);
      }

      async private Task loadAllHistMergeRequests(string hostname, string query)
      {
         onLoadAllHistMergeRequests();

         if (!await _histWorkflowManager.LoadAllMergeRequestsAsync(hostname, query))
         {
            return;
         }

         onAllHistMergeRequestsLoaded(hostname);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadAllHistMergeRequests()
      {
         disableAllHistUIControls(false);
         listViewHistMergeRequests.Items.Clear();
      }

      private void onFailedLoadProjectHistMergeRequests()
      {
         labelHistWorkflowStatus.Text = "Failed to load merge requests";

         Trace.TraceInformation(String.Format("[MainForm.History] Failed to load merge requests"));
      }

      private void onProjectHistMergeRequestsLoaded(string hostname, Project project,
         IEnumerable<MergeRequest> mergeRequests)
      {
         labelHistWorkflowStatus.Text = String.Format("Project {0} loaded", project.Path_With_Namespace);

         Trace.TraceInformation(String.Format(
            "[MainForm.History] Project {0} loaded. Loaded {1} merge requests",
           project.Path_With_Namespace, mergeRequests.Count()));

         createListViewGroupForProject(listViewHistMergeRequests, hostname, project);
         fillListViewHistMergeRequests(hostname, project, mergeRequests);
      }

      private void onAllHistMergeRequestsLoaded(string hostname)
      {
         enableHistMergeRequestFilterControls(true);

         if (listViewHistMergeRequests.Items.Count > 0)
         {
            enableListView(listViewHistMergeRequests);
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadSingleHistMergeRequest(int mergeRequestIId)
      {
         if (mergeRequestIId != 0)
         {
            labelHistWorkflowStatus.Text = String.Format("Loading merge request with IId {0}...", mergeRequestIId);
         }
         else
         {
            labelHistWorkflowStatus.Text = String.Empty;
         }

         enableHistMergeRequestActions(false);
         enableHistCommitActions(false);
         updateHistMergeRequestDetails(null);
         disableComboBox(comboBoxHistLeftCommit, String.Empty);
         disableComboBox(comboBoxHistRightCommit, String.Empty);

         if (mergeRequestIId != 0)
         {
            htmlPanelHistMergeRequestDescription.Text = "Loading...";
         }

         Trace.TraceInformation(String.Format("[MainForm.History] Loading merge request with IId {0}",
            mergeRequestIId.ToString()));
      }

      private void onFailedLoadSingleHistMergeRequest()
      {
         htmlPanelHistMergeRequestDescription.Text = String.Empty;
         labelHistWorkflowStatus.Text = "Failed to load merge request";

         Trace.TraceInformation(String.Format("[MainForm.History] Failed to load merge request"));
      }

      private void onSingleHistMergeRequestLoaded(string hostname, string projectname, MergeRequest mergeRequest)
      {
         Debug.Assert(mergeRequest.Id != default(MergeRequest).Id);

         enableHistMergeRequestActions(true);
         FullMergeRequestKey fmk = new FullMergeRequestKey
         {
            ProjectKey = new ProjectKey { HostName = hostname, ProjectName = projectname },
            MergeRequest = mergeRequest
         };
         updateHistMergeRequestDetails(fmk);

         labelHistWorkflowStatus.Text = String.Format("Merge request with Id {0} loaded", mergeRequest.Id);

         Trace.TraceInformation(String.Format("[MainForm.History] Merge request loaded"));
      }

      private void onLoadHistCommits()
      {
         enableHistCommitActions(false);

         if (listViewHistMergeRequests.SelectedItems.Count != 0)
         {
            disableComboBox(comboBoxHistLeftCommit, "Loading...");
            disableComboBox(comboBoxHistRightCommit, "Loading...");
         }
         else
         {
            disableComboBox(comboBoxHistLeftCommit, String.Empty);
            disableComboBox(comboBoxHistRightCommit, String.Empty);
         }

         Trace.TraceInformation(String.Format("[MainForm.History] Loading commits"));
      }

      private void onFailedLoadHistCommits()
      {
         disableComboBox(comboBoxHistLeftCommit, String.Empty);
         disableComboBox(comboBoxHistRightCommit, String.Empty);
         labelHistWorkflowStatus.Text = "Failed to load commits";

         Trace.TraceInformation(String.Format("[MainForm.History] Failed to load commits"));
      }

      private void onHistCommitsLoaded(string hostname, string projectname, MergeRequest mergeRequest,
         IEnumerable<Commit> commits)
      {
         if (commits.Count() > 0)
         {
            enableComboBox(comboBoxHistLeftCommit);
            enableComboBox(comboBoxHistRightCommit);

            addCommitsToComboBoxes(comboBoxHistLeftCommit, comboBoxHistRightCommit, commits,
               mergeRequest.Diff_Refs.Base_SHA, mergeRequest.Target_Branch);
            comboBoxHistLeftCommit.SelectedIndex = 0;
            comboBoxHistRightCommit.SelectedIndex = comboBoxHistRightCommit.Items.Count - 1;

            enableHistCommitActions(true);
         }
         else
         {
            disableComboBox(comboBoxHistLeftCommit, String.Empty);
            disableComboBox(comboBoxHistRightCommit, String.Empty);
         }

         labelHistWorkflowStatus.Text = String.Format("Loaded {0} commits", commits.Count());

         Trace.TraceInformation(String.Format("[MainForm.History] Loaded {0} commits", commits.Count()));
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void disableAllHistUIControls(bool clearListView)
      {
         enableHistMergeRequestFilterControls(false);
         enableHistMergeRequestActions(false);
         enableHistCommitActions(false);
         disableListView(listViewHistMergeRequests, clearListView);
         disableComboBox(comboBoxHistLeftCommit, String.Empty);
         disableComboBox(comboBoxHistRightCommit, String.Empty);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void fillListViewHistMergeRequests(string hostname, Project project,
         IEnumerable<MergeRequest> mergeRequests)
      {
         ProjectKey projectKey = new ProjectKey
         {
            HostName = hostname,
            ProjectName = project.Path_With_Namespace
         };

         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            ListViewItem item = addListViewMergeRequestItem(listViewHistMergeRequests, projectKey);
            setListViewItemTag(item, projectKey, mergeRequest);
         }

         int maxLineCount = 2;
         setListViewRowHeight(listViewHistMergeRequests, listViewHistMergeRequests.Font.Height * maxLineCount + 2);
      }
   }
}

