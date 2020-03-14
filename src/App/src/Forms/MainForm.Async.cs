using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.App.Helpers;
using mrHelper.App.Interprocess;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Types;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;
using mrHelper.GitClient;
using mrHelper.Common.Interfaces;
using System.Linq;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      async private Task showDiscussionsFormAsync(MergeRequestKey mrk, string title, User author, string state)
      {
         Debug.Assert(getHostName() != String.Empty);
         Debug.Assert(_currentUser.ContainsKey(getHostName()));

         // Store data before async/await
         User currentUser = _currentUser[getHostName()];

         if (isSearchMode())
         {
            _discussionManager.ForceUpdate(mrk); // Pre-load discussions for MR in Search mode
         }

         ILocalGitRepository repo = await getRepository(mrk.ProjectKey, true);
         if (repo != null)
         {
            enableControlsOnGitAsyncOperation(false, "updating git repository");
            try
            {
               // Using remote checker because there are might be discussions reported by other users on newer commits
               await _gitClientUpdater.UpdateAsync(repo,
                  _mergeRequestCache.GetProjectCheckerFactory().GetRemoteProjectChecker(mrk), updateGitStatusText);
            }
            catch (Exception ex)
            {
               if (ex is InteractiveUpdateSSLFixedException)
               {
                  // SSL check is disabled, let's try to update in the background
                  scheduleSilentUpdate(mrk.ProjectKey);
                  return;
               }
               else if (ex is InteractiveUpdateCancelledException)
               {
                  if (MessageBox.Show("Without up-to-date git repository, some context code snippets might be missing. "
                     + "Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                        DialogResult.No)
                  {
                     Trace.TraceInformation(
                        "[MainForm] User rejected to show discussions without up-to-date git repository");
                     return;
                  }
                  else
                  {
                     Trace.TraceInformation(
                        "[MainForm] User agreed to show discussions without up-to-date git repository");
                     repo = null;
                  }
               }
               else
               {
                  Debug.Assert(ex is InteractiveUpdateFailed);
                  MessageBox.Show("Cannot initialize git repository",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return;
               }
            }
            finally
            {
               enableControlsOnGitAsyncOperation(true, "updating git repository");
            }

            if (state == "merged")
            {
               await restoreChainOfMergedCommits(repo, mrk);
            }
         }
         else
         {
            if (MessageBox.Show("Without git repository, context code snippets will be missing. "
               + "Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                  DialogResult.No)
            {
               Trace.TraceInformation("[MainForm] User rejected to show discussions without git repository");
               return;
            }
            else
            {
               Trace.TraceInformation("[MainForm] User decided to show Discussions w/o git repository");
               repo = null;
            }
         }

         IEnumerable<Discussion> discussions = await loadDiscussionsAsync(mrk);
         if (discussions == null || _exiting)
         {
            return;
         }

         labelWorkflowStatus.Text = "Rendering Discussions Form...";
         labelWorkflowStatus.Update();

         DiscussionsForm form;
         try
         {
            DiscussionsForm discussionsForm = new DiscussionsForm(mrk, title, author, repo,
               int.Parse(comboBoxDCDepth.Text), _colorScheme, discussions, _discussionManager, currentUser,
                  async (key) =>
               {
                  try
                  {
                     ILocalGitRepository updatingRepo = await getRepository(key.ProjectKey, true);
                     if (updatingRepo != null && !updatingRepo.DoesRequireClone())
                     {
                        // Using remote checker because there are might be discussions reported
                        // by other users on newer commits
                        await updatingRepo.Updater.ForceUpdate(
                           _mergeRequestCache.GetProjectCheckerFactory().GetRemoteProjectChecker(key), null);
                        return updatingRepo;
                     }
                     else
                     {
                        Trace.TraceInformation("[MainForm] User tried to refresh Discussions w/o git repository");
                        MessageBox.Show("Cannot update git folder, some context code snippets may be missing. ",
                           "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                     }
                  }
                  catch (RepositoryUpdateException ex)
                  {
                     ExceptionHandlers.Handle("Cannot update git repository on refreshing discussions", ex);
                  }
                  return null;
               });
            form = discussionsForm;
         }
         catch (NoDiscussionsToShow)
         {
            MessageBox.Show("No discussions to show.", "Information",
               MessageBoxButtons.OK, MessageBoxIcon.Information);
            labelWorkflowStatus.Text = "No discussions to show";
            return;
         }
         catch (ArgumentException ex)
         {
            string errorMessage = "Cannot show Discussions form";
            ExceptionHandlers.Handle(errorMessage, ex);
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelWorkflowStatus.Text = "Cannot show Discussions";
            return;
         }

         labelWorkflowStatus.Text = "Discussions opened";

         Trace.TraceInformation(String.Format("[MainForm] Opened Discussions for MR IId {0} (at {1})",
            mrk.IId, (repo?.Path ?? "null")));

         form.Show();
      }

      async private Task onLaunchDiffToolAsync(MergeRequestKey mrk, string state)
      {
         if (comboBoxLeftCommit.SelectedItem == null || comboBoxRightCommit.SelectedItem == null)
         {
            Debug.Assert(false);
            return;
         }

         // Keep data before async/await
         string leftSHA = getGitTag(comboBoxLeftCommit, comboBoxRightCommit, true /* left */);
         string rightSHA = getGitTag(comboBoxLeftCommit, comboBoxRightCommit, false /* right */);

         List<string> includedSHA = new List<string>();
         for (int index = comboBoxLeftCommit.SelectedIndex; index < comboBoxLeftCommit.Items.Count; ++index)
         {
            string sha = ((CommitComboBoxItem)(comboBoxLeftCommit.Items[index])).SHA;
            includedSHA.Add(sha);
            if (sha == leftSHA)
            {
               break;
            }
         }

         ILocalGitRepository repo = await getRepository(mrk.ProjectKey, true);
         if (repo != null)
         {
            enableControlsOnGitAsyncOperation(false, "updating git repository");
            try
            {
               IInstantProjectChecker checker = _mergeRequestCache.GetMergeRequest(mrk).HasValue
                  ? _mergeRequestCache.GetProjectCheckerFactory().GetLocalProjectChecker(mrk)
                  : _mergeRequestCache.GetProjectCheckerFactory().GetRemoteProjectChecker(mrk);

               // Using local checker because it does not make a GitLab request and it is quite enough here because
               // user may select only those commits that already loaded and cached and have timestamps less
               // than latest merge request version (this is possible for Open MR only)
               await _gitClientUpdater.UpdateAsync(repo, checker, updateGitStatusText);
            }
            catch (Exception ex)
            {
               if (ex is InteractiveUpdateCancelledException)
               {
                  // User declined to create a repository
                  MessageBox.Show("Cannot launch a diff tool without up-to-date git repository", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Warning);
               }
               else if (ex is InteractiveUpdateFailed)
               {
                  string errorMessage = "Cannot initialize git repository";
                  ExceptionHandlers.Handle(errorMessage, ex);
                  MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
               else if (ex is InteractiveUpdateSSLFixedException)
               {
                  // SSL check is disabled, let's try to update in the background
                  scheduleSilentUpdate(mrk.ProjectKey);
               }
               else
               {
                  Debug.Assert(false);
               }
               return;
            }
            finally
            {
               enableControlsOnGitAsyncOperation(true, "updating git repository");
            }
         }
         else
         {
            MessageBox.Show("Cannot launch a diff tool without up-to-date git repository. Check git folder in Settings",
               "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
         }

         if (state == "merged")
         {
            await restoreChainOfMergedCommits(repo, getBaseCommitSha(), getChainOfCommits());
            leftSHA = Helpers.GitTools.AdjustSHA(leftSHA, repo);
            rightSHA = Helpers.GitTools.AdjustSHA(rightSHA, repo);
         }

         labelWorkflowStatus.Text = "Launching diff tool...";

         int pid;
         try
         {
            string arguments = "difftool --no-symlinks --dir-diff --tool=" +
               DiffTool.DiffToolIntegration.GitDiffToolName + " " + leftSHA + " " + rightSHA;
            pid = ExternalProcess.Start("git", arguments, false, repo.Path).ExitCode;
         }
         catch (Exception ex)
         {
            if (ex is ExternalProcessFailureException || ex is ExternalProcessSystemException)
            {
               string message = "Could not launch diff tool";
               ExceptionHandlers.Handle(message, ex);
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               labelWorkflowStatus.Text = message;
               return;
            }
            throw;
         }

         if (_exiting)
         {
            return;
         }

         labelWorkflowStatus.Text = "Diff tool launched";

         Trace.TraceInformation(String.Format("[MainForm] Launched DiffTool for SHA {0} vs SHA {1} (at {2}). PID {3}",
            leftSHA, rightSHA, repo.Path, pid.ToString()));

         saveInterprocessSnapshot(pid, leftSHA, rightSHA);

         if (!_reviewedCommits.ContainsKey(mrk))
         {
            _reviewedCommits[mrk] = new HashSet<string>();
         }
         includedSHA.ForEach(x => _reviewedCommits[mrk].Add(x));

         comboBoxLeftCommit.Refresh();
         comboBoxRightCommit.Refresh();
      }

      async private Task onAddCommentAsync(MergeRequestKey mrk, string title)
      {
         string caption = String.Format("Add comment to merge request \"{0}\"", title);
         using (NewDiscussionItemForm form = new NewDiscussionItemForm(caption))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Comment body cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               DiscussionCreator creator = _discussionManager.GetDiscussionCreator(mrk);

               labelWorkflowStatus.Text = "Adding a comment...";
               try
               {
                  await creator.CreateNoteAsync(new CreateNewNoteParameters { Body = form.Body });
               }
               catch (DiscussionCreatorException)
               {
                  MessageBox.Show("Cannot create a discussion at GitLab. Check your connection and try again",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
               labelWorkflowStatus.Text = "Comment added";
            }
         }
      }

      async private Task onNewDiscussionAsync(MergeRequestKey mrk, string title)
      {
         string caption = String.Format("Create a new discussion in merge request \"{0}\"", title);
         using (NewDiscussionItemForm form = new NewDiscussionItemForm(caption))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Discussion body cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               DiscussionCreator creator = _discussionManager.GetDiscussionCreator(mrk);

               labelWorkflowStatus.Text = "Creating a discussion...";
               try
               {
                  await creator.CreateDiscussionAsync(new NewDiscussionParameters { Body = form.Body }, false);
               }
               catch (DiscussionCreatorException)
               {
                  MessageBox.Show("Cannot create a discussion at GitLab. Check your connection and try again",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
               labelWorkflowStatus.Text = "Discussion created";
            }
         }
      }

      private void saveInterprocessSnapshot(int pid, string leftSHA, string rightSHA)
      {
         Snapshot snapshot;
         snapshot.AccessToken = GetCurrentAccessToken();
         snapshot.Refs.LeftSHA = leftSHA;     // Base commit SHA in the source branch
         snapshot.Refs.RightSHA = rightSHA;   // SHA referencing HEAD of this merge request
         snapshot.Host = GetCurrentHostName();
         snapshot.MergeRequestIId = GetCurrentMergeRequestIId();
         snapshot.Project = GetCurrentProjectName();
         snapshot.TempFolder = textBoxLocalGitFolder.Text;

         SnapshotSerializer serializer = new SnapshotSerializer();
         serializer.SerializeToDisk(snapshot, pid);
      }

      async private Task<IEnumerable<Discussion>> loadDiscussionsAsync(MergeRequestKey mrk)
      {
         labelWorkflowStatus.Text = "Loading discussions...";
         IEnumerable<Discussion> discussions;
         try
         {
            discussions = await _discussionManager.GetDiscussionsAsync(mrk);
         }
         catch (DiscussionManagerException ex)
         {
            string message = "Cannot load discussions from GitLab";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelWorkflowStatus.Text = message;
            return null;
         }
         labelWorkflowStatus.Text = "Discussions loaded";
         return discussions;
      }

      ProjectKey GetProjectKey(ProjectKey pk) => pk;
      ProjectKey GetProjectKey(MergeRequestKey mrk) => mrk.ProjectKey;

      ProjectKey GetKeyForUpdate(ProjectKey pk) => pk;
      MergeRequestKey GetKeyForUpdate(MergeRequestKey mrk) => mrk;

      async private Task performSilentUpdate<T>(T key)
      {
         ProjectKey pk = GetProjectKey((dynamic)key);

         if (_silentUpdateInProgress.Contains(pk))
         {
            Trace.TraceInformation(String.Format(
               "[MainForm] Silent update for {0} is skipped due to a concurrent silent update", pk.ProjectName));
            return;
         }

         _silentUpdateInProgress.Add(pk);

         ILocalGitRepository repo = await getRepository(pk, false);
         if (repo == null || repo.DoesRequireClone())
         {
            Trace.TraceInformation(String.Format("[MainForm] Cannot update git repository {0} silently: {1}",
               pk.ProjectName, (repo == null ? "repo is null" : "must be cloned first")));
            _silentUpdateInProgress.Remove(pk);
            return;
         }

         Trace.TraceInformation(String.Format(
            "[MainForm] Going to update git repository {0} silently", pk.ProjectName));

         // Use Local Project Checker here because Remote Project Checker looks overkill.
         // We anyway update discussion remote on attempt to show Discussions view but it might be unneeded right now.
         IInstantProjectChecker instantChecker =
            _mergeRequestCache.GetProjectCheckerFactory().GetLocalProjectChecker((dynamic)key);
         try
         {
            await repo.Updater.ForceUpdate(instantChecker, null);
         }
         catch (RepositoryUpdateException ex)
         {
            ExceptionHandlers.Handle(String.Format("[MainForm] Silent update of {0} cancelled", pk.ProjectName), ex);
            _silentUpdateInProgress.Remove(pk);
            return;
         }

         Trace.TraceInformation(String.Format("[MainForm] Silent update of {0} finished", pk.ProjectName));
         _silentUpdateInProgress.Remove(pk);
      }

      private void scheduleSilentUpdate(ProjectKey pk)
      {
         BeginInvoke(new Action(async () => await performSilentUpdate(pk)));
      }

      private void scheduleSilentUpdate(MergeRequestKey mrk)
      {
         BeginInvoke(new Action(async () => await performSilentUpdate(mrk)));
      }

      private readonly HashSet<ProjectKey> _silentUpdateInProgress = new HashSet<ProjectKey>();

      async private Task restoreChainOfMergedCommits(ILocalGitRepository repo, MergeRequestKey mrk)
      {
         _commitChainCreator = new CommitChainCreator(Program.Settings,
            status => labelWorkflowStatus.Text = status, repo, mrk);
         await restoreChainOfMergedCommits();
      }

      async private Task restoreChainOfMergedCommits(ILocalGitRepository repo,
         string baseSha, IEnumerable<string> commits)
      {
         _commitChainCreator = new CommitChainCreator(Program.Settings,
            status => labelWorkflowStatus.Text = status, repo, baseSha, commits);
         await restoreChainOfMergedCommits();
      }

      async private Task restoreChainOfMergedCommits()
      {
         enableControlsOnGitAsyncOperation(false, "restoring merged commits");
         try
         {
            await _commitChainCreator.CreateChainAsync();
         }
         finally
         {
            _commitChainCreator = null;
         }
         enableControlsOnGitAsyncOperation(true, "restoring merged commits");
      }
   }
}

