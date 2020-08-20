using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.App.Helpers;
using mrHelper.App.Interprocess;
using mrHelper.StorageSupport;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.App.Helpers.GitLab;
using mrHelper.GitLabClient;
using mrHelper.App.Forms.Helpers;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      async private Task showDiscussionsFormAsync(MergeRequestKey mrk, string title, User author)
      {
         Debug.Assert(getHostName() != String.Empty);
         Debug.Assert(getCurrentUser() != null);

         // Store data before async/await
         User currentUser = getCurrentUser();
         DataCache dataCache = getSession(!isSearchMode());
         GitLabInstance gitLabInstance = new GitLabInstance(getHostName(), Program.Settings);
         if (dataCache == null)
         {
            Debug.Assert(false);
            return;
         }

         if (isSearchMode())
         {
            // Pre-load discussions for MR in Search mode
            dataCache.DiscussionCache.RequestUpdate(mrk, Constants.ReloadListPseudoTimerInterval, null);
         }

         IEnumerable<Discussion> discussions = await loadDiscussionsAsync(dataCache, mrk);
         if (discussions == null || _exiting)
         {
            return;
         }

         // TODO WTF Try host switch while prepare storage for discussions is running
         ILocalCommitStorage storage = getCommitStorage(mrk.ProjectKey, true);
         if (!await prepareStorageForDiscussionsForm(mrk, storage, discussions) || _exiting)
         {
            return;
         }
         showDiscussionForm(gitLabInstance, dataCache, storage, currentUser, mrk, discussions, title, author);
      }

      async private Task<bool> prepareStorageForDiscussionsForm(MergeRequestKey mrk,
         ILocalCommitStorage storage, IEnumerable<Discussion> discussions)
      {
         if (storage == null)
         {
            if (MessageBox.Show("Without a storage, context code snippets will be missing. "
               + "Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                  DialogResult.No)
            {
               Trace.TraceInformation("[MainForm] User rejected to show discussions without a storage");
               return false;
            }
            else
            {
               Trace.TraceInformation("[MainForm] User decided to show Discussions without a storage");
               return true;
            }
         }

         ICommitStorageUpdateContextProvider contextProvider = new DiscussionBasedContextProvider(discussions);
         return await prepareCommitStorage(mrk, storage, contextProvider, false);
      }

      private void showDiscussionForm(GitLabInstance gitLabInstance, DataCache dataCache, ILocalCommitStorage storage,
         User currentUser, MergeRequestKey mrk, IEnumerable<Discussion> discussions, string title, User author)
      {
         labelWorkflowStatus.Text = "Rendering discussion contexts...";
         labelWorkflowStatus.Refresh();

         DiscussionsForm form;
         try
         {
            IAsyncGitCommandService git = storage?.Git;

            DiscussionsForm discussionsForm = new DiscussionsForm(dataCache, gitLabInstance, _modificationNotifier,
               git, currentUser, mrk, discussions, title, author, int.Parse(comboBoxDCDepth.Text), _colorScheme,
               async (key, discussionsUpdated) =>
            {
               if (storage != null && storage.Updater != null)
               {
                  try
                  {
                     await storage.Updater.StartUpdate(new DiscussionBasedContextProvider(discussionsUpdated),
                        status => onStorageUpdateProgressChange(status, mrk), () => onStorageUpdateStateChange());
                  }
                  catch (LocalCommitStorageUpdaterException ex)
                  {
                     ExceptionHandlers.Handle("Cannot update a storage on refreshing discussions", ex);
                  }
               }
               else
               {
                  Trace.TraceInformation("[MainForm] User tried to refresh Discussions without a storage");
                  MessageBox.Show("Cannot update a storage, some context code snippets may be missing. ",
                     "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
               }
            },
            () => dataCache?.DiscussionCache?.RequestUpdate(mrk, Constants.DiscussionCheckOnNewThreadInterval, null));
            form = discussionsForm;
         }
         catch (NoDiscussionsToShow)
         {
            MessageBox.Show("No discussions to show.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

         labelWorkflowStatus.Text = "Opening Discussions view...";
         labelWorkflowStatus.Refresh();

         Trace.TraceInformation(String.Format("[MainForm] Opened Discussions for MR IId {0} (at {1})",
            mrk.IId, (storage?.Path ?? "null")));

         form.Show();

         labelWorkflowStatus.Text = "Discussions opened";
      }

      async private Task onLaunchDiffToolAsync(MergeRequestKey mrk)
      {
         // Keep data before async/await
         DataCache dataCache = getSession(!isSearchMode());
         getShaForDiffTool(out string leftSHA, out string rightSHA,
            out IEnumerable<string> includedSHA, out RevisionType? type);
         string accessToken = Program.Settings.GetAccessToken(mrk.ProjectKey.HostName);
         if (dataCache == null
          || String.IsNullOrWhiteSpace(accessToken)
          || String.IsNullOrWhiteSpace(leftSHA)
          || String.IsNullOrWhiteSpace(rightSHA)
          || includedSHA == null
          || !includedSHA.Any()
          || !type.HasValue)
         {
            Debug.Assert(false);
            return;
         }

         ILocalCommitStorage storage = getCommitStorage(mrk.ProjectKey, true);
         if (!await prepareStorageForDiffTool(mrk, storage, leftSHA, rightSHA) || _exiting)
         {
            return;
         }

         launchDiffTool(leftSHA, rightSHA, storage, mrk, accessToken, getDataCacheName(dataCache));

         HashSet<string> reviewedRevisions = getReviewedRevisions(mrk);
         foreach (string sha in includedSHA)
         {
            reviewedRevisions.Add(sha);
         }

         MergeRequestKey? currentMrk = getMergeRequestKey(null);
         if (currentMrk.HasValue && currentMrk.Value.Equals(mrk))
         {
            revisionBrowser.UpdateReviewedRevisions(reviewedRevisions, type.Value);
         }
      }

      private void launchDiffTool(string leftSHA, string rightSHA, ILocalCommitStorage storage,
         MergeRequestKey mrk, string accessToken, string sessionName)
      {
         labelWorkflowStatus.Text = "Launching diff tool...";

         int? pid = null;
         try
         {
            DiffToolArguments arg = new DiffToolArguments(true, Constants.GitDiffToolName, leftSHA, rightSHA);
            pid = storage.Git?.LaunchDiffTool(arg) ?? null;
         }
         catch (DiffToolLaunchException)
         {
            MessageBox.Show("Cannot launch diff tool", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelWorkflowStatus.Text = String.Empty;
         }

         if (!pid.HasValue)
         {
            return; // e.g. storage.Git got disposed
         }

         Trace.TraceInformation(String.Format("[MainForm] Launched DiffTool for SHA {0} vs SHA {1} (at {2}). PID {3}",
            leftSHA, rightSHA, storage.Path, pid.Value.ToString()));

         if (pid == -1)
         {
            labelWorkflowStatus.Text = "Diff tool was not launched. Most likely the difference is empty.";
         }
         else
         {
            labelWorkflowStatus.Text = "Diff tool launched";
            saveInterprocessSnapshot(pid.Value, leftSHA, rightSHA, mrk, accessToken, sessionName);
         }
      }

      async private Task<bool> prepareStorageForDiffTool(MergeRequestKey mrk, ILocalCommitStorage storage,
         string leftSHA, string rightSHA)
      {
         if (storage == null)
         {
            MessageBox.Show("Cannot launch a diff tool without up-to-date storage.",
               "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
         }

         ICommitStorageUpdateContextProvider contextProvider =
            new CommitBasedContextProvider(new string[] { rightSHA }, leftSHA);
         return await prepareCommitStorage(mrk, storage, contextProvider, true);
      }

      async private Task onAddCommentAsync(MergeRequestKey mrk, string title)
      {
         string caption = String.Format("Add comment to merge request \"{0}\"", title);
         using (TextEditForm form = new TextEditForm(caption, "", true, true, true))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Comment body cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               labelWorkflowStatus.Text = "Adding a comment...";
               try
               {
                  GitLabInstance gitLabInstance = new GitLabInstance(mrk.ProjectKey.HostName, Program.Settings);
                  IDiscussionCreator creator = Shortcuts.GetDiscussionCreator(
                     gitLabInstance, _modificationNotifier, mrk, getCurrentUser());
                  await creator.CreateNoteAsync(new CreateNewNoteParameters(form.Body));
               }
               catch (DiscussionCreatorException)
               {
                  MessageBox.Show("Cannot create a discussion at GitLab. Check your connection and try again",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  labelWorkflowStatus.Text = "Cannot create a discussion";
                  return;
               }
               labelWorkflowStatus.Text = "Comment added";
            }
         }
      }

      async private Task onNewDiscussionAsync(MergeRequestKey mrk, string title)
      {
         string caption = String.Format("Create a new thread in merge request \"{0}\"", title);
         using (TextEditForm form = new TextEditForm(caption, "", true, true, true))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Discussion body cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               DataCache dataCache = getSession(!isSearchMode());
               if (dataCache == null)
               {
                  Debug.Assert(false);
                  return;
               }

               labelWorkflowStatus.Text = "Creating a discussion...";
               try
               {
                  GitLabInstance gitLabInstance = new GitLabInstance(mrk.ProjectKey.HostName, Program.Settings);
                  IDiscussionCreator creator = Shortcuts.GetDiscussionCreator(
                     gitLabInstance, _modificationNotifier, mrk, getCurrentUser());
                  await creator.CreateDiscussionAsync(new NewDiscussionParameters(form.Body, null), false);
               }
               catch (DiscussionCreatorException)
               {
                  MessageBox.Show("Cannot create a discussion at GitLab. Check your connection and try again",
                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  labelWorkflowStatus.Text = "Cannot create a discussion";
                  return;
               }
               labelWorkflowStatus.Text = "Thread started";

               dataCache.DiscussionCache?.RequestUpdate(mrk, Constants.DiscussionCheckOnNewThreadInterval, null);
            }
         }
      }

      private void saveInterprocessSnapshot(int pid, string leftSHA, string rightSHA, MergeRequestKey mrk,
         string accessToken, string sessionName)
      {
         // leftSHA - Base commit SHA in the source branch
         // rightSHA - SHA referencing HEAD of this merge request
         Snapshot snapshot = new Snapshot(
            mrk.IId,
            mrk.ProjectKey.HostName,
            accessToken,
            mrk.ProjectKey.ProjectName,
            new Core.Matching.DiffRefs(leftSHA, rightSHA),
            textBoxStorageFolder.Text,
            sessionName);

         SnapshotSerializer serializer = new SnapshotSerializer();
         try
         {
            serializer.SerializeToDisk(snapshot, pid);
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle("Cannot serialize Snapshot object", ex);
         }
      }

      async private Task<IEnumerable<Discussion>> loadDiscussionsAsync(DataCache dataCache, MergeRequestKey mrk)
      {
         if (dataCache?.DiscussionCache == null)
         {
            return null;
         }

         labelWorkflowStatus.Text = "Loading discussions...";
         IEnumerable<Discussion> discussions;
         try
         {
            discussions = await dataCache.DiscussionCache.LoadDiscussions(mrk);
         }
         catch (DiscussionCacheException ex)
         {
            string message = "Cannot load discussions from GitLab";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelWorkflowStatus.Text = message;
            return null;
         }

         bool anyDiscussions = discussions == null || !discussions.Any();
         labelWorkflowStatus.Text = anyDiscussions
            ? "Discussions loaded"
            : "There are no discussions in this Merge Request";
         return discussions;
      }

      private void requestCommitStorageUpdate(ProjectKey projectKey)
      {
         DataCache dataCache = getSession(true /* supported in Live only */);

         IEnumerable<GitLabSharp.Entities.Version> versions = dataCache?.MergeRequestCache?.GetVersions(projectKey);
         if (versions != null)
         {
            VersionBasedContextProvider contextProvider = new VersionBasedContextProvider(versions);
            ILocalCommitStorage storage = getCommitStorage(projectKey, false);
            storage?.Updater?.RequestUpdate(contextProvider, null);
         }
      }

      async private Task checkForUpdatesAsync()
      {
         bool updateReceived = false;

         string oldButtonText = buttonReloadList.Text;
         onUpdating();
         requestUpdates(null, 100, () => { updateReceived = true; onUpdated(oldButtonText); });
         await TaskUtils.WhileAsync(() => !updateReceived);
      }

      private async Task<bool> prepareCommitStorage(
         MergeRequestKey mrk, ILocalCommitStorage storage, ICommitStorageUpdateContextProvider contextProvider,
         bool isLimitExceptionFatal)
      {
         try
         {
            _mergeRequestsUpdatingByUserRequest.Add(mrk);
            updateStorageDependentControlState(mrk);
            labelWorkflowStatus.Text = getStorageSummaryUpdateInformation();
            await storage.Updater.StartUpdate(contextProvider, status => onStorageUpdateProgressChange(status, mrk),
               () => onStorageUpdateStateChange());
            return true;
         }
         catch (Exception ex)
         {
            if (ex is LocalCommitStorageUpdaterCancelledException)
            {
               MessageBox.Show("Cannot perform requested action without up-to-date storage", "Warning",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
               labelWorkflowStatus.Text = "Storage update cancelled by user";
            }
            else if (ex is LocalCommitStorageUpdaterFailedException fex)
            {
               ExceptionHandlers.Handle(ex.Message, ex);
               MessageBox.Show(fex.OriginalMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               labelWorkflowStatus.Text = "Failed to update storage";
            }
            else if (ex is LocalCommitStorageUpdaterLimitException mex)
            {
               ExceptionHandlers.Handle(ex.Message, mex);
               if (!isLimitExceptionFatal)
               {
                  return true;
               }
               MessageBox.Show(mex.OriginalMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               labelWorkflowStatus.Text = "Failed to update storage";
            }
            return false;
         }
         finally
         {
            _mergeRequestsUpdatingByUserRequest.Remove(mrk);
            updateStorageDependentControlState(mrk);
            labelWorkflowStatus.Text = getStorageSummaryUpdateInformation();
         }
      }

      private async Task createNewMergeRequestAsync(SubmitNewMergeRequestParameters parameters, string firstNote)
      {
         buttonCreateNew.Enabled = false;
         labelWorkflowStatus.Text = "Creating a merge request at GitLab...";

         GitLabInstance gitLabInstance = new GitLabInstance(parameters.ProjectKey.HostName, Program.Settings);
         MergeRequestKey? mrkOpt = await MergeRequestEditHelper.SubmitNewMergeRequestAsync(gitLabInstance,
            _modificationNotifier, parameters, firstNote, getCurrentUser());
         if (mrkOpt == null)
         {
            // all error handling is done at the callee side
            labelWorkflowStatus.Text = "Merge Request has not been created";
            buttonCreateNew.Enabled = true;
            return;
         }

         requestUpdates(null, new int[] { Constants.CreateNewMergeRequestRefreshListTimerInterval });

         labelWorkflowStatus.Text = String.Format("Merge Request !{0} has been created in project {1}",
            mrkOpt.Value.IId, parameters.ProjectKey.ProjectName);
         buttonCreateNew.Enabled = true;

         _newMergeRequestDialogStatesByHosts[getHostName()] = new NewMergeRequestProperties(
            parameters.ProjectKey.ProjectName, null, null, parameters.AssigneeUserName, parameters.Squash,
            parameters.DeleteSourceBranch);
      }

      private async Task applyChangesToMergeRequestAsync(string hostname, User currentUser, FullMergeRequestKey item)
      {
         MergeRequestKey mrk = new MergeRequestKey(item.ProjectKey, item.MergeRequest.IId);
         string noteText = await MergeRequestEditHelper.GetLatestSpecialNote(_liveDataCache.DiscussionCache, mrk);
         MergeRequestPropertiesForm form = new EditMergeRequestPropertiesForm(hostname,
            getProjectAccessor(), currentUser, item.ProjectKey, item.MergeRequest, noteText);
         if (form.ShowDialog() != DialogResult.OK)
         {
            return;
         }

         ApplyMergeRequestChangesParameters parameters =
            new ApplyMergeRequestChangesParameters(form.Title, form.AssigneeUsername,
            form.Description, form.TargetBranch, form.DeleteSourceBranch, form.Squash);

         GitLabInstance gitLabInstance = new GitLabInstance(hostname, Program.Settings);
         bool updated = await MergeRequestEditHelper.ApplyChangesToMergeRequest(gitLabInstance, _modificationNotifier,
            item.ProjectKey, item.MergeRequest, parameters, noteText, form.SpecialNote, currentUser);
         if (!updated)
         {
            labelWorkflowStatus.Text = String.Format("No changes have been made to Merge Request !{0}", mrk.IId);
            return;
         }

         requestUpdates(mrk,
            new int[] {
                     100,
                     Program.Settings.OneShotUpdateFirstChanceDelayMs,
                     Program.Settings.OneShotUpdateSecondChanceDelayMs
            });

         labelWorkflowStatus.Text = String.Format("Merge Request !{0} has been updated", mrk.IId);
      }
   }
}

