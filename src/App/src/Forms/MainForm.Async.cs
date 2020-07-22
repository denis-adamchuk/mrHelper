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
using mrHelper.Client.Types;
using mrHelper.Client.Discussions;
using mrHelper.Client.Session;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      async private Task showDiscussionsFormAsync(MergeRequestKey mrk, string title, User author)
      {
         Debug.Assert(getHostName() != String.Empty);
         Debug.Assert(_currentUser.ContainsKey(getHostName()));

         // Store data before async/await
         User currentUser = _currentUser[getHostName()];
         ISession session = getSession(!isSearchMode());
         if (session == null)
         {
            Debug.Assert(false);
            return;
         }

         if (isSearchMode())
         {
            // Pre-load discussions for MR in Search mode
            session.DiscussionCache.RequestUpdate(mrk, new int[] { Constants.ReloadListPseudoTimerInterval }, null);
         }

         IEnumerable<Discussion> discussions = await loadDiscussionsAsync(session, mrk);
         if (discussions == null || _exiting)
         {
            return;
         }

         ILocalCommitStorage storage = getCommitStorage(mrk.ProjectKey, true);
         if (!await prepareStorageForDiscussionsForm(mrk, storage, discussions) || _exiting)
         {
            return;
         }
         showDiscussionForm(session, storage, currentUser, mrk, discussions, title, author);
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

      private void showDiscussionForm(ISession session, ILocalCommitStorage storage,
         User currentUser, MergeRequestKey mrk, IEnumerable<Discussion> discussions, string title, User author)
      {
         labelWorkflowStatus.Text = "Rendering discussion contexts...";
         labelWorkflowStatus.Refresh();

         DiscussionsForm form;
         try
         {
            IAsyncGitCommandService git = storage?.Git;
            DiscussionsForm discussionsForm = new DiscussionsForm(session, git, currentUser, mrk, discussions, title,
               author, int.Parse(comboBoxDCDepth.Text), _colorScheme,
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
            () => session?.DiscussionCache?.RequestUpdate(mrk,
               new int[] { Constants.DiscussionCheckOnNewThreadInterval }, null));
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
         ISession session = getSession(!isSearchMode());
         getShaForDiffTool(out string baseSHA, out string leftSHA, out string rightSHA,
            out IEnumerable<string> includedSHA, out RevisionType? type);
         string accessToken = Program.Settings.GetAccessToken(mrk.ProjectKey.HostName);
         if (session == null
          || String.IsNullOrWhiteSpace(accessToken)
          || String.IsNullOrWhiteSpace(baseSHA)
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
         if (!await prepareStorageForDiffTool(mrk, storage, baseSHA, leftSHA, rightSHA) || _exiting)
         {
            return;
         }

         launchDiffTool(leftSHA, rightSHA, storage, mrk, accessToken, getSessionName(session));

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
         string baseSHA, string leftSHA, string rightSHA)
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
         using (ViewDiscussionItemForm form = new ViewDiscussionItemForm(caption))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Comment body cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               ISession session = getSession(!isSearchMode());
               IDiscussionCreator creator = session?.GetDiscussionCreator(mrk);
               if (creator == null)
               {
                  return;
               }

               labelWorkflowStatus.Text = "Adding a comment...";
               try
               {
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
         using (ViewDiscussionItemForm form = new ViewDiscussionItemForm(caption))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               if (form.Body.Length == 0)
               {
                  MessageBox.Show("Discussion body cannot be empty", "Warning",
                     MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                  return;
               }

               ISession session = getSession(!isSearchMode());
               IDiscussionCreator creator = session?.GetDiscussionCreator(mrk);
               if (creator == null)
               {
                  return;
               }

               labelWorkflowStatus.Text = "Creating a discussion...";
               try
               {
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

               session?.DiscussionCache?.RequestUpdate(
                  mrk, new int[]{ Constants.DiscussionCheckOnNewThreadInterval }, null);
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

      async private Task<IEnumerable<Discussion>> loadDiscussionsAsync(ISession session, MergeRequestKey mrk)
      {
         if (session?.DiscussionCache == null)
         {
            return null;
         }

         labelWorkflowStatus.Text = "Loading discussions...";
         IEnumerable<Discussion> discussions = null;
         try
         {
            discussions = await session.DiscussionCache.LoadDiscussions(mrk);
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
         ISession session = getSession(true /* supported in Live only */);

         IEnumerable<GitLabSharp.Entities.Version> versions = session?.MergeRequestCache?.GetVersions(projectKey);
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
         requestUpdates(null, new int[] { 1 }, () => { updateReceived = true; onUpdated(oldButtonText); });
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
   }
}

