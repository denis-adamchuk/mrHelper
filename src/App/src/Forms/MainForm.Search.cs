using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Client.Types;
using mrHelper.Client.Workflow;
using mrHelper.Client.Repository;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitClient;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void createSearchWorkflow()
      {
         _searchWorkflowManager = new WorkflowManager(Program.Settings);
      }

      private void subscribeToSearchWorkflow()
      {
         _searchWorkflowManager.PreLoadCurrentUser += onLoadCurrentUser;
         _searchWorkflowManager.PostLoadCurrentUser += onCurrentUserLoaded;
         _searchWorkflowManager.FailedLoadCurrentUser += onFailedLoadCurrentUser;

         _searchWorkflowManager.PostLoadProjectMergeRequests += onProjectSearchMergeRequestsLoaded;
         _searchWorkflowManager.FailedLoadProjectMergeRequests += onFailedLoadProjectSearchMergeRequests;

         _searchWorkflowManager.PreLoadSingleMergeRequest += onLoadSingleSearchMergeRequest;
         _searchWorkflowManager.PostLoadSingleMergeRequest += onSingleSearchMergeRequestLoaded;
         _searchWorkflowManager.FailedLoadSingleMergeRequest += onFailedLoadSingleSearchMergeRequest;

         _searchWorkflowManager.PreLoadCommits += onLoadSearchCommits;
         _searchWorkflowManager.PostLoadCommits += onSearchCommitsLoaded;
         _searchWorkflowManager.FailedLoadCommits +=  onFailedLoadSearchCommits;
      }

      private void unsubscribeFromSearchWorkflow()
      {
         _searchWorkflowManager.PreLoadCurrentUser -= onLoadCurrentUser;
         _searchWorkflowManager.PostLoadCurrentUser -= onCurrentUserLoaded;
         _searchWorkflowManager.FailedLoadCurrentUser -= onFailedLoadCurrentUser;

         _searchWorkflowManager.PostLoadProjectMergeRequests -= onProjectSearchMergeRequestsLoaded;
         _searchWorkflowManager.FailedLoadProjectMergeRequests -= onFailedLoadProjectSearchMergeRequests;

         _searchWorkflowManager.PreLoadSingleMergeRequest -= onLoadSingleSearchMergeRequest;
         _searchWorkflowManager.PostLoadSingleMergeRequest -= onSingleSearchMergeRequestLoaded;
         _searchWorkflowManager.FailedLoadSingleMergeRequest -= onFailedLoadSingleSearchMergeRequest;

         _searchWorkflowManager.PreLoadCommits -= onLoadSearchCommits;
         _searchWorkflowManager.PostLoadCommits -= onSearchCommitsLoaded;
         _searchWorkflowManager.FailedLoadCommits -=  onFailedLoadSearchCommits;
      }

      async private Task searchMergeRequests(string query)
      {
         try
         {
            await startSearchWorkflowAsync(getHostName(), query);
         }
         catch (Exception ex)
         {
            if (ex is WorkflowException || ex is UnknownHostException)
            {
               disableAllSearchUIControls(true);
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

         selectMergeRequest(listViewFoundMergeRequests, String.Empty, 0, false);
      }

      async private Task<bool> switchSearchMergeRequestByUserAsync(ProjectKey projectKey, int mergeRequestIId)
      {
         Trace.TraceInformation(String.Format("[MainForm.Search] User requested to change merge request to IId {0}",
            mergeRequestIId.ToString()));

         if (mergeRequestIId == 0)
         {
            onLoadSingleSearchMergeRequest(0);
            await _searchWorkflowManager.CancelAsync();
            return false;
         }

         await _searchWorkflowManager.CancelAsync();

         try
         {
            return await _searchWorkflowManager.LoadMergeRequestAsync(
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

      async private Task startSearchWorkflowAsync(string hostname, string query)
      {
         labelWorkflowStatus.Text = String.Empty;

         await _searchWorkflowManager.CancelAsync();
         if (String.IsNullOrWhiteSpace(hostname) || String.IsNullOrWhiteSpace(query))
         {
            disableAllSearchUIControls(true);
            return;
         }

         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UnknownHostException(hostname);
         }

         disableAllSearchUIControls(true);
         if (!_currentUser.ContainsKey(hostname))
         {
            if (!await _searchWorkflowManager.LoadCurrentUserAsync(hostname))
            {
               return;
            }
         }

         await loadAllSearchMergeRequests(hostname, query);
      }

      async private Task loadAllSearchMergeRequests(string hostname, string query)
      {
         onLoadAllSearchMergeRequests();

         if (!await _searchWorkflowManager.LoadAllMergeRequestsAsync(hostname, query))
         {
            return;
         }

         onAllSearchMergeRequestsLoaded(hostname);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadAllSearchMergeRequests()
      {
         disableAllSearchUIControls(false);
         listViewFoundMergeRequests.Items.Clear();
      }

      private void onFailedLoadProjectSearchMergeRequests()
      {
         labelWorkflowStatus.Text = "Failed to load merge requests";

         Trace.TraceInformation(String.Format("[MainForm.Search] Failed to load merge requests"));
      }

      private void onProjectSearchMergeRequestsLoaded(string hostname, Project project,
         IEnumerable<MergeRequest> mergeRequests)
      {
         labelWorkflowStatus.Text = String.Format("Project {0} loaded", project.Path_With_Namespace);

         Trace.TraceInformation(String.Format(
            "[MainForm.Search] Project {0} loaded. Loaded {1} merge requests",
           project.Path_With_Namespace, mergeRequests.Count()));

         createListViewGroupForProject(listViewFoundMergeRequests, hostname, project);
         fillListViewSearchMergeRequests(hostname, project, mergeRequests);
      }

      private void onAllSearchMergeRequestsLoaded(string hostname)
      {
         enableMergeRequestSearchControls(true);

         if (listViewFoundMergeRequests.Items.Count > 0)
         {
            enableListView(listViewFoundMergeRequests);
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLoadSingleSearchMergeRequest(int mergeRequestIId)
      {
         if (mergeRequestIId != 0)
         {
            labelWorkflowStatus.Text = String.Format("Loading merge request with IId {0}...", mergeRequestIId);
         }
         else
         {
            labelWorkflowStatus.Text = String.Empty;
         }

         enableMergeRequestActions(false);
         enableCommitActions(false, null, default(User));
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(false, String.Empty, default(ProjectKey));
         updateTotalTime(null);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);

         if (mergeRequestIId != 0)
         {
            richTextBoxMergeRequestDescription.Text = "Loading...";
         }

         Trace.TraceInformation(String.Format("[MainForm.Search] Loading merge request with IId {0}",
            mergeRequestIId.ToString()));
      }

      private void onFailedLoadSingleSearchMergeRequest()
      {
         richTextBoxMergeRequestDescription.Text = String.Empty;
         labelWorkflowStatus.Text = "Failed to load merge request";

         Trace.TraceInformation(String.Format("[MainForm.Search] Failed to load merge request"));
      }

      private void onSingleSearchMergeRequestLoaded(string hostname, string projectname, MergeRequest mergeRequest)
      {
         Debug.Assert(mergeRequest.Id != default(MergeRequest).Id);

         enableMergeRequestActions(true);
         FullMergeRequestKey fmk = new FullMergeRequestKey
         {
            ProjectKey = new ProjectKey { HostName = hostname, ProjectName = projectname },
            MergeRequest = mergeRequest
         };
         updateMergeRequestDetails(fmk);
         updateTimeTrackingMergeRequestDetails(true, mergeRequest.Title, fmk.ProjectKey);
         updateTotalTime(new MergeRequestKey { ProjectKey = fmk.ProjectKey, IId = fmk.MergeRequest.IId });

         labelWorkflowStatus.Text = String.Format("Merge request with Id {0} loaded", mergeRequest.Id);

         Trace.TraceInformation(String.Format("[MainForm.Search] Merge request loaded"));
      }

      private void onLoadSearchCommits()
      {
         enableCommitActions(false, null, default(User));

         if (listViewFoundMergeRequests.SelectedItems.Count != 0)
         {
            disableComboBox(comboBoxLeftCommit, "Loading...");
            disableComboBox(comboBoxRightCommit, "Loading...");
         }
         else
         {
            disableComboBox(comboBoxLeftCommit, String.Empty);
            disableComboBox(comboBoxRightCommit, String.Empty);
         }

         Trace.TraceInformation(String.Format("[MainForm.Search] Loading commits"));
      }

      private void onFailedLoadSearchCommits()
      {
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);
         labelWorkflowStatus.Text = "Failed to load commits";

         Trace.TraceInformation(String.Format("[MainForm.Search] Failed to load commits"));
      }

      private void onSearchCommitsLoaded(string hostname, string projectname, MergeRequest mergeRequest,
         IEnumerable<Commit> commits)
      {
         if (commits.Count() > 0)
         {
            enableComboBox(comboBoxLeftCommit);
            enableComboBox(comboBoxRightCommit);

            addCommitsToComboBoxes(comboBoxLeftCommit, comboBoxRightCommit, commits,
               mergeRequest.Diff_Refs.Base_SHA, mergeRequest.Target_Branch);
            comboBoxLeftCommit.SelectedIndex = 0;
            comboBoxRightCommit.SelectedIndex = comboBoxRightCommit.Items.Count - 1;

            enableCommitActions(true, mergeRequest.Labels, mergeRequest.Author);
         }
         else
         {
            disableComboBox(comboBoxLeftCommit, String.Empty);
            disableComboBox(comboBoxRightCommit, String.Empty);
         }

         labelWorkflowStatus.Text = String.Format("Loaded {0} commits", commits.Count());

         Trace.TraceInformation(String.Format("[MainForm.Search] Loaded {0} commits", commits.Count()));
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void disableAllSearchUIControls(bool clearListView)
      {
         enableMergeRequestSearchControls(false);
         disableListView(listViewFoundMergeRequests, clearListView);

         enableMergeRequestActions(false);
         enableCommitActions(false, null, default(User));
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(false, String.Empty, default(ProjectKey));
         updateTotalTime(null);
         disableComboBox(comboBoxLeftCommit, String.Empty);
         disableComboBox(comboBoxRightCommit, String.Empty);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void fillListViewSearchMergeRequests(string hostname, Project project,
         IEnumerable<MergeRequest> mergeRequests)
      {
         ProjectKey projectKey = new ProjectKey
         {
            HostName = hostname,
            ProjectName = project.Path_With_Namespace
         };

         foreach (MergeRequest mergeRequest in mergeRequests)
         {
            ListViewItem item = addListViewMergeRequestItem(listViewFoundMergeRequests, projectKey);
            setListViewItemTag(item, projectKey, mergeRequest);
         }

         int maxLineCount = 2;
         setListViewRowHeight(listViewFoundMergeRequests, listViewFoundMergeRequests.Font.Height * maxLineCount + 2);
      }

      async private Task restoreChainOfCommits(ILocalGitRepository repo, string baseSHA, IEnumerable<string> commits)
      {
         if (String.IsNullOrEmpty(baseSHA) || commits == null)
         {
            return;
         }

         enableControlsOnGitAsyncOperation(false);
         linkLabelAbortGit.Visible = false;

         Debug.Assert(repo.ContainsSHA(baseSHA));
         string prevSHA = baseSHA;

         List<Tuple<Comparison, string, string>> comparisons = new List<Tuple<Comparison, string, string>>();
         int iCommit = 1;
         foreach (string SHA in commits.Reverse())
         {
            labelWorkflowStatus.Text = String.Format(
               "Loading commit comparison results from GitLab: {0}/{1}", iCommit++, commits.Count());

            if (!repo.ContainsSHA(SHA) && !repo.ContainsBranch(GitTools.FakeSHA(SHA)))
            {
               try
               {
                  comparisons.Add(new Tuple<Comparison, string, string>(
                     await _repositoryManager.CompareAsync(repo.ProjectKey, prevSHA, SHA), prevSHA, SHA));
               }
               catch (RepositoryManagerException ex)
               {
                  ExceptionHandlers.Handle("Cannot obtain comparison result", ex);
                  labelWorkflowStatus.Text = "Failed to obtain comparison result";
                  return;
               }
            }

            prevSHA = SHA;
         }

         List<Tuple<string, string, string>> patches = new List<Tuple<string, string, string>>();
         int iComparison = 1;
         foreach (Tuple<Comparison, string, string> tuple in comparisons)
         {
            labelWorkflowStatus.Text = String.Format(
               "Creating patches: {0}/{1}", iComparison++, comparisons.Count());

            string patch = await createPatch(repo, tuple.Item1, tuple.Item2, tuple.Item3);
            if (String.IsNullOrEmpty(patch))
            {
               labelWorkflowStatus.Text = "Failed to create a patch";
               return;
            }
            patches.Add(new Tuple<string, string, string>(patch, tuple.Item2, tuple.Item3));
         }

         int iPatch = 1;
         foreach (Tuple<string, string, string> patch in patches)
         {
            labelWorkflowStatus.Text = String.Format(
               "Applying patches: {0}/{1}", iPatch++, patches.Count());

            try
            {
               repo.CreateBranchForPatch(patch.Item2, GitTools.FakeSHA(patch.Item3), patch.Item1);
            }
            catch (BranchCreationException ex)
            {
               ExceptionHandlers.Handle("Cannot create a branch for patch", ex);
               labelWorkflowStatus.Text = "Failed to apply a patch";
               return;
            }
         }

         enableControlsOnGitAsyncOperation(true);
      }

      async private Task<string> createPatch(ILocalGitRepository repo,
         Comparison comparison, string prevSHA, string SHA)
      {
         StringBuilder stringBuilder = new StringBuilder();
         foreach (DiffStruct diff in comparison.Diffs)
         {
            string gitDiff;
            if (diff.Diff == String.Empty)
            {
               try
               {
                  gitDiff = await createDiffFromRawFiles(repo, diff.Old_Path, prevSHA, diff.New_Path, SHA);
               }
               catch (Exception ex)
               {
                  ExceptionHandlers.Handle("Cannot create diff from raw files", ex);
                  return null;
               }
            }
            else
            {
               gitDiff = diff.Diff.Replace("\\n", "\n");
            }

            stringBuilder.AppendLine(String.Format("--- a/{0}", diff.Old_Path));
            stringBuilder.AppendLine(String.Format("+++ b/{0}", diff.New_Path));
            stringBuilder.AppendLine(gitDiff);
         }
         return stringBuilder.ToString();
      }

      async private Task<string> createDiffFromRawFiles(ILocalGitRepository repo,
         string oldFilename, string oldSha, string newFilename, string newSha)
      {
         GitLabSharp.Entities.File oldFile =
            await _repositoryManager.LoadFileAsync(repo.ProjectKey, oldFilename, oldSha);
         GitLabSharp.Entities.File newFile =
            await _repositoryManager.LoadFileAsync(repo.ProjectKey, newFilename, newSha);

         string oldTempFilename = System.IO.Path.Combine(repo.Path, "temp1___" + oldFile.File_Name);
         string newTempFilename = System.IO.Path.Combine(repo.Path, "temp2___" + newFile.File_Name);

         string diffArguments = String.Format("diff --no-index -- {0} {1}",
            StringUtils.EscapeSpaces(oldTempFilename), StringUtils.EscapeSpaces(newTempFilename));

         string oldFileContent = StringUtils.Base64Decode(oldFile.Content).Replace("\n", "\r\n");
         string newFileContent = StringUtils.Base64Decode(newFile.Content).Replace("\n", "\r\n");

         try
         {
            FileUtils.OverwriteFile(oldTempFilename, oldFileContent);
            FileUtils.OverwriteFile(newTempFilename, newFileContent);

            IEnumerable<string> gitDiffOutput = ExternalProcess.Start("git", diffArguments, true, repo.Path).StdOut;

            return String.Join("\n", gitDiffOutput.Skip(4));
         }
         finally
         {
            if (System.IO.File.Exists(oldTempFilename))
            {
               System.IO.File.Delete(oldTempFilename);
            }
            if (System.IO.File.Exists(newTempFilename))
            {
               System.IO.File.Delete(newTempFilename);
            }
         }
      }
   }
}

