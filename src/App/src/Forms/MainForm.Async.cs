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

         ILocalGitCommitStorage repo = getCommitStorage(mrk, true);
         if (!await prepareRepositoryForDiscussionsForm(repo, mrk, discussions) || _exiting)
         {
            return;
         }
         showDiscussionForm(session, repo, currentUser, mrk, discussions, title, author);
      }

      async private Task<bool> prepareRepositoryForDiscussionsForm(ILocalGitCommitStorage repo,
         MergeRequestKey mrk, IEnumerable<Discussion> discussions)
      {
         if (repo == null)
         {
            if (MessageBox.Show("Without git repository, context code snippets will be missing. "
               + "Do you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                  DialogResult.No)
            {
               Trace.TraceInformation("[MainForm] User rejected to show discussions without git repository");
               return false;
            }
            else
            {
               Trace.TraceInformation("[MainForm] User decided to show Discussions w/o git repository");
               return true;
            }
         }

         try
         {
            ICommitStorageUpdateContextProvider contextProvider = new DiscussionBasedContextProvider(discussions);
            await repo.Updater.StartUpdate(contextProvider, updateGitStatusText,
               () => updateGitAbortState(false));
            return true;
         }
         catch (Exception ex)
         {
            if (ex is LocalGitCommitStorageUpdaterCancelledException)
            {
               MessageBox.Show("Cannot show Discussions without up-to-date git repository", "Warning",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (ex is LocalGitCommitStorageUpdaterFailedException inex)
            {
               ExceptionHandlers.Handle(ex.Message, ex);
               MessageBox.Show(inex.OriginalMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
         }
      }

      private void showDiscussionForm(ISession session, ILocalGitCommitStorage repo,
         User currentUser, MergeRequestKey mrk, IEnumerable<Discussion> discussions, string title, User author)
      {
         labelWorkflowStatus.Text = "Rendering discussion contexts...";
         labelWorkflowStatus.Refresh();

         DiscussionsForm form;
         try
         {
            DiscussionsForm discussionsForm = new DiscussionsForm(session, repo,
               currentUser, mrk, discussions, title, author,
               int.Parse(comboBoxDCDepth.Text), _colorScheme,
               async (key, discussions2) =>
            {
               try
               {
                  if (repo != null && repo.Updater != null)
                  {
                     ICommitStorageUpdateContextProvider contextProvider = new DiscussionBasedContextProvider(discussions2);
                     await repo.Updater.StartUpdate(contextProvider, updateGitStatusText,
                        () => updateGitAbortState(false));
                  }
                  else
                  {
                     Trace.TraceInformation("[MainForm] User tried to refresh Discussions w/o git repository");
                     MessageBox.Show("Cannot update git folder, some context code snippets may be missing. ",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  }
               }
               catch (LocalGitCommitStorageUpdaterException ex)
               {
                  ExceptionHandlers.Handle("Cannot update git repository on refreshing discussions", ex);
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
            mrk.IId, (repo?.Path ?? "null")));

         form.Show();

         labelWorkflowStatus.Text = "Discussions opened";
      }

      async private Task onLaunchDiffToolAsync(MergeRequestKey mrk)
      {
         // Keep data before async/await
         ISession session = getSession(!isSearchMode());
         getShaForDiffTool(out string leftSHA, out string rightSHA, out IEnumerable<string> includedSHA,
            out RevisionType? type);
         string accessToken = Program.Settings.GetAccessToken(mrk.ProjectKey.HostName);
         if (session == null
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

         ILocalGitCommitStorage repo = getCommitStorage(mrk, true);
         if (!await prepareRepositoryForDiffTool(repo, leftSHA, rightSHA) || _exiting)
         {
            return;
         }

         launchDiffTool(leftSHA, rightSHA, repo, mrk, accessToken, getSessionName(session));

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

      private void launchDiffTool(string leftSHA, string rightSHA, ILocalGitCommitStorage repo,
         MergeRequestKey mrk, string accessToken, string sessionName)
      {
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

         Trace.TraceInformation(String.Format("[MainForm] Launched DiffTool for SHA {0} vs SHA {1} (at {2}). PID {3}",
            leftSHA, rightSHA, repo.Path, pid.ToString()));

         if (pid == -1)
         {
            labelWorkflowStatus.Text = "Diff tool was not launched. Most likely the difference is empty.";
         }
         else
         {
            labelWorkflowStatus.Text = "Diff tool launched";
            saveInterprocessSnapshot(pid, leftSHA, rightSHA, mrk, accessToken, sessionName);
         }
      }

      async private Task<bool> prepareRepositoryForDiffTool(ILocalGitCommitStorage repo, string leftSHA, string rightSHA)
      {
         if (repo == null)
         {
            MessageBox.Show("Cannot launch a diff tool without up-to-date git repository. Check git folder in Settings",
               "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
         }

         try
         {
            ICommitStorageUpdateContextProvider contextProvider =
               new CommitBasedContextProvider(new string[] { leftSHA, rightSHA });
            await repo.Updater.StartUpdate(contextProvider, updateGitStatusText,
               () => updateGitAbortState(false));
            return true;
         }
         catch (Exception ex)
         {
            if (ex is LocalGitCommitStorageUpdaterCancelledException)
            {
               // User declined to create a repository
               MessageBox.Show("Cannot launch a diff tool without up-to-date git repository", "Warning",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (ex is LocalGitCommitStorageUpdaterFailedException inex)
            {
               ExceptionHandlers.Handle(ex.Message, ex);
               MessageBox.Show(inex.OriginalMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
         }
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
            textBoxLocalGitFolder.Text,
            sessionName);

         SnapshotSerializer serializer = new SnapshotSerializer();
         serializer.SerializeToDisk(snapshot, pid);
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
         labelWorkflowStatus.Text = "Discussions loaded";
         return discussions;
      }

      private void requestRepositoryUpdate(MergeRequestKey mrk)
      {
         ISession session = getSession(true /* supported in Live only */);

         IEnumerable<GitLabSharp.Entities.Version> versions = session?.MergeRequestCache?.GetVersions(mrk);
         if (versions != null)
         {
            VersionBasedContextProvider contextProvider = new VersionBasedContextProvider(versions);
            ILocalGitCommitStorage repo = getCommitStorage(mrk, false);
            repo?.Updater?.RequestUpdate(contextProvider, null);
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
   }
}

