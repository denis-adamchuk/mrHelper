using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.App.Helpers;
using mrHelper.App.Interprocess;
using mrHelper.Common.Exceptions;
using mrHelper.Client.Types;
using mrHelper.Client.Discussions;
using mrHelper.Client.MergeRequests;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      async private Task showDiscussionsFormAsync()
      {
         Debug.Assert(getMergeRequestKey().HasValue);

         // Store data before async/await
         User currentUser = _currentUser.Value;
         MergeRequestKey mrk = getMergeRequestKey().Value;
         MergeRequest mergeRequest = getMergeRequest().Value;

         GitClient client = getGitClient(mrk.ProjectKey, true);
         if (client != null)
         {
            enableControlsOnGitAsyncOperation(false);
            try
            {
               // Using remote checker because there are might be discussions reported by other users on newer commits
               await _gitClientUpdater.UpdateAsync(client,
                  _mergeRequestManager.GetUpdateManager().GetRemoteProjectChecker(mrk), updateGitStatusText);
            }
            catch (Exception ex)
            {
               if (ex is RepeatOperationException)
               {
                  return;
               }
               else if (ex is CancelledByUserException)
               {
                  if (MessageBox.Show("Without up-to-date git repository, some context code snippets might be missing. "
                     + "Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                        DialogResult.No)
                  {
                     return;
                  }
                  else
                  {
                     client = null;
                  }
               }
               else
               {
                  Debug.Assert(ex is GitOperationException);
                  MessageBox.Show("Cannot initialize git repository",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return;
               }
            }
            finally
            {
               enableControlsOnGitAsyncOperation(true);
            }
         }
         else
         {
            if (MessageBox.Show("Without git repository, context code snippets will be missing. "
               + "Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                  DialogResult.No)
            {
               return;
            }
            else
            {
               Trace.TraceInformation("[MainForm] User decided to show Discussions w/o git repository");
               client = null;
            }
         }

         IEnumerable<Discussion> discussions = await loadDiscussionsAsync(mrk);
         if (discussions == null)
         {
            return;
         }

         labelWorkflowStatus.Text = "Rendering Discussions Form...";
         labelWorkflowStatus.Update();

         DiscussionsForm form;
         try
         {
            DiscussionsForm discussionsForm = new DiscussionsForm(mrk, mergeRequest.Title, mergeRequest.Author, client,
               int.Parse(comboBoxDCDepth.Text), _colorScheme, discussions, _discussionManager, currentUser,
                  async (key) =>
               {
                  try
                  {
                     GitClient gitClient = getGitClient(key.ProjectKey, true);
                     if (gitClient != null && !gitClient.DoesRequireClone())
                     {
                        // Using remote checker because there are might be discussions reported
                        // by other users on newer commits
                        await gitClient.Updater.ManualUpdateAsync(
                           _mergeRequestManager.GetUpdateManager().GetRemoteProjectChecker(key), null);
                        return gitClient;
                     }
                     else
                     {
                        Trace.TraceInformation("[MainForm] User tried to refresh Discussions w/o git repository");
                        MessageBox.Show("Cannot update git folder, some context code snippets may be missing. ",
                           "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                     }
                  }
                  catch (GitOperationException ex)
                  {
                     ExceptionHandlers.Handle(ex, "Cannot update git repository on refreshing discussions");
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
            ExceptionHandlers.Handle(ex, "Cannot show Discussions form");
            MessageBox.Show("Cannot show Discussions form", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelWorkflowStatus.Text = "Cannot show Discussions";
            return;
         }

         labelWorkflowStatus.Text = "Discussions opened";

         Trace.TraceInformation(String.Format("[MainForm] Opened Discussions for MR IId {0} (at {1})",
            mrk.IId, (client?.Path ?? "null")));

         form.Show();
      }

      async private Task onLaunchDiffToolAsync()
      {
         if (comboBoxLeftCommit.SelectedItem == null || comboBoxRightCommit.SelectedItem == null)
         {
            Debug.Assert(false);
            return;
         }

         // Keep data before async/await
         string leftSHA = getGitTag(true /* left */);
         string rightSHA = getGitTag(false /* right */);

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

         Debug.Assert(getMergeRequestKey().HasValue);
         MergeRequestKey mrk = getMergeRequestKey().Value;

         GitClient client = getGitClient(mrk.ProjectKey, true);
         if (client != null)
         {
            enableControlsOnGitAsyncOperation(false);
            try
            {
               // Using local checker because it does not make a GitLab request and it is quite enough here because
               // user may select only those commits that already loaded and cached and have timestamps less
               // than latest merge request version
               await _gitClientUpdater.UpdateAsync(client,
                  _mergeRequestManager.GetUpdateManager().GetLocalProjectChecker(mrk), updateGitStatusText);
            }
            catch (Exception ex)
            {
               if (ex is CancelledByUserException)
               {
                  // User declined to create a repository
                  MessageBox.Show("Cannot launch a diff tool without up-to-date git repository", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Warning);
               }
               else if (!(ex is RepeatOperationException))
               {
                  Debug.Assert(ex is GitOperationException);
                  MessageBox.Show("Cannot initialize git repository",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               }
               return;
            }
            finally
            {
               enableControlsOnGitAsyncOperation(true);
            }
         }
         else
         {
            MessageBox.Show("Cannot launch a diff tool without up-to-date git repository", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
         }

         labelWorkflowStatus.Text = "Launching diff tool...";

         int pid;
         try
         {
            pid = client.DiffTool(mrHelper.DiffTool.DiffToolIntegration.GitDiffToolName, leftSHA, rightSHA);
         }
         catch (GitOperationException ex)
         {
            string message = "Could not launch diff tool";
            ExceptionHandlers.Handle(ex, message);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelWorkflowStatus.Text = message;
            return;
         }

         labelWorkflowStatus.Text = "Diff tool launched";

         Trace.TraceInformation(String.Format("[MainForm] Launched DiffTool for SHA {0} vs SHA {1} (at {2}). PID {3}",
            leftSHA, rightSHA, client.Path, pid.ToString()));

         saveInterprocessSnapshot(pid, leftSHA, rightSHA);

         if (!_reviewedCommits.ContainsKey(mrk))
         {
            _reviewedCommits[mrk] = new HashSet<string>();
         }
         includedSHA.ForEach(x => _reviewedCommits[mrk].Add(x));

         comboBoxLeftCommit.Refresh();
         comboBoxRightCommit.Refresh();
      }

      async private Task onAddCommentAsync()
      {
         Debug.Assert(getMergeRequestKey().HasValue);

         // Store data before opening a modal dialog
         string title = getMergeRequest().Value.Title;
         MergeRequestKey mrk = getMergeRequestKey().Value;

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

      async private Task onNewDiscussionAsync()
      {
         Debug.Assert(getMergeRequestKey().HasValue);

         // Store data before opening a modal dialog
         string title = getMergeRequest().Value.Title;
         MergeRequestKey mrk = getMergeRequestKey().Value;

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
                  await creator.CreateDiscussionAsync(new NewDiscussionParameters { Body = form.Body });
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
         catch (DiscussionManagerException)
         {
            string message = "Cannot load discussions from GitLab";
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

         GitClient client = getGitClient(pk, false);
         if (client == null || client.DoesRequireClone())
         {
            Trace.TraceInformation(String.Format("[MainForm] Cannot update git repository {0} silently: {1}",
               pk.ProjectName, (client == null ? "client is null" : "must be cloned first")));
            _silentUpdateInProgress.Remove(pk);
            return;
         }

         Trace.TraceInformation(String.Format(
            "[MainForm] Going to update git repository {0} silently", pk.ProjectName));

         // Use Local Project Checker here because Remote Project Checker looks overkill.
         // We anyway update discussion remote on attempt to show Discussions view but it might be unneeded right now.
         IInstantProjectChecker instantChecker =
            _mergeRequestManager.GetUpdateManager().GetLocalProjectChecker((dynamic)key);
         try
         {
            await client.Updater.ManualUpdateAsync(instantChecker, null);
         }
         catch (GitOperationException)
         {
            Trace.TraceInformation(String.Format("[MainForm] Silent update of {0} cancelled", pk.ProjectName));
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
   }
}

