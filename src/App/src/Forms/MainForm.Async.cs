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
using mrHelper.GitClient;
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
         ISession currentSession = getCurrentSession();
         if (currentSession == null)
         {
            Debug.Assert(false);
            return;
         }

         if (isSearchMode())
         {
            // Pre-load discussions for MR in Search mode
            Debug.Assert(currentSession == _searchSession);
            currentSession.DiscussionCache.RequestUpdate(
               mrk, new int[] { Constants.ReloadListPseudoTimerInterval }, null);
         }

         ILocalGitRepository repo = getRepository(mrk.ProjectKey, true);
         if (repo != null)
         {
            enableControlsOnGitAsyncOperation(false, "updating git repository");
            try
            {
               // Using remote-based provider as there are might be discussions from other users on newer commits
               IProjectUpdateContextProvider contextProvider =
                  currentSession?.MergeRequestCache?.GetRemoteBasedContextProvider(mrk);
               await _gitClientUpdater.UpdateAsync(repo, contextProvider, updateGitStatusText);
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

            Trace.TraceInformation("[MainForm] User decided to show Discussions w/o git repository");
         }

         IEnumerable<Discussion> discussions = await loadDiscussionsAsync(currentSession, mrk);
         if (discussions == null || _exiting)
         {
            return;
         }

         IEnumerable<string> headShaFromDiscussions =
               discussions
               .Where(x => x.Notes != null && x.Notes.Any() && x.Notes.First().Type == "DiffNote")
               .Select(x => x.Notes.First().Position.Head_SHA).Distinct();

         if (repo != null
          && headShaFromDiscussions.Any()
          && !await fetchMissingCommits(repo, headShaFromDiscussions))
         {
            labelWorkflowStatus.Text = "Could not open Discussions";
            return;
         }

         if (_exiting)
         {
            return;
         }

         labelWorkflowStatus.Text = "Rendering discussion contexts...";
         labelWorkflowStatus.Refresh();

         DiscussionsForm form;
         try
         {
            DiscussionsForm discussionsForm = new DiscussionsForm(currentSession, repo,
               currentUser, mrk, discussions, title, author,
               int.Parse(comboBoxDCDepth.Text), _colorScheme,
               async (session, key) =>
            {
               try
               {
                  ILocalGitRepository updatingRepo = getRepository(key.ProjectKey, true);
                  if (updatingRepo != null && !updatingRepo.ExpectingClone)
                  {
                     // Using remote-based provider as there are might be discussions from other users on newer commits
                     IProjectUpdateContextProvider contextProvider =
                        session?.MergeRequestCache?.GetRemoteBasedContextProvider(key);
                     await updatingRepo.Updater.SilentUpdate(contextProvider);
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
            },
            (session) => session?.DiscussionCache?.RequestUpdate(mrk,
               new int[] { Constants.DiscussionCheckOnNewThreadInterval }, null));
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

         labelWorkflowStatus.Text = "Opening Discussions view...";
         labelWorkflowStatus.Refresh();

         Trace.TraceInformation(String.Format("[MainForm] Opened Discussions for MR IId {0} (at {1})",
            mrk.IId, (repo?.Path ?? "null")));

         form.Show();

         labelWorkflowStatus.Text = "Discussions opened";
      }

      async private Task onLaunchDiffToolAsync(MergeRequestKey mrk)
      {
         if (comboBoxLatestCommit.SelectedItem == null || comboBoxEarliestCommit.SelectedItem == null)
         {
            Debug.Assert(false);
            return;
         }

         // Keep data before async/await
         string getSHA(ComboBox comboBox) => ((CommitComboBoxItem)comboBox.SelectedItem).SHA;
         string leftSHA = getSHA(comboBoxEarliestCommit);
         string rightSHA = getSHA(comboBoxLatestCommit);
         ISession currentSession = getCurrentSession();
         if (currentSession == null)
         {
            Debug.Assert(false);
            return;
         }

         List<string> includedSHA = new List<string>();
         for (int index = comboBoxLatestCommit.SelectedIndex; index < comboBoxLatestCommit.Items.Count; ++index)
         {
            string sha = ((CommitComboBoxItem)(comboBoxLatestCommit.Items[index])).SHA;
            includedSHA.Add(sha);
            if (sha == leftSHA)
            {
               break;
            }
         }

         ILocalGitRepository repo = getRepository(mrk.ProjectKey, true);
         if (repo != null)
         {
            enableControlsOnGitAsyncOperation(false, "updating git repository");
            try
            {
               // Using local-based provider because it does not make a GitLab request and it is quite enough here
               // because user may select only those commits that already loaded and cached and have timestamps less
               // than latest merge request version (this is possible for Open MR only)
               IProjectUpdateContextProvider contextProvider =
                  currentSession?.MergeRequestCache?.GetLocalBasedContextProvider(mrk.ProjectKey);
               await _gitClientUpdater.UpdateAsync(repo, contextProvider, updateGitStatusText);
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

         if (!await fetchMissingCommits(repo, new string[] { leftSHA, rightSHA }))
         {
            labelWorkflowStatus.Text = "Could not launch diff tool";
            return;
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

         saveInterprocessSnapshot(pid, leftSHA, rightSHA, currentSession);

         if (!_reviewedCommits.ContainsKey(mrk))
         {
            _reviewedCommits[mrk] = new HashSet<string>();
         }
         includedSHA.ForEach(x => _reviewedCommits[mrk].Add(x));

         comboBoxLatestCommit.Refresh();
         comboBoxEarliestCommit.Refresh();
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

               IDiscussionCreator creator = getCurrentSession()?.GetDiscussionCreator(mrk);

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

               ISession session = getCurrentSession();
               IDiscussionCreator creator = session?.GetDiscussionCreator(mrk);

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

      private void saveInterprocessSnapshot(int pid, string leftSHA, string rightSHA, ISession session)
      {
         // leftSHA - Base commit SHA in the source branch
         // rightSHA - SHA referencing HEAD of this merge request
         Snapshot snapshot = new Snapshot(
            GetCurrentMergeRequestIId(),
            GetCurrentHostName(),
            GetCurrentAccessToken(),
            GetCurrentProjectName(),
            new Core.Matching.DiffRefs(leftSHA, rightSHA),
            textBoxLocalGitFolder.Text,
            session == _liveSession ? "Live" : "Search");

         SnapshotSerializer serializer = new SnapshotSerializer();
         serializer.SerializeToDisk(snapshot, pid);
      }

      async private Task<IEnumerable<Discussion>> loadDiscussionsAsync(ISession session, MergeRequestKey mrk)
      {
         labelWorkflowStatus.Text = "Loading discussions...";
         IEnumerable<Discussion> discussions;
         try
         {
            discussions = await session?.DiscussionCache?.LoadDiscussions(mrk);
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

      async private Task performSilentUpdate(ProjectKey projectKey)
      {
         ILocalGitRepository repo = getRepository(projectKey, false);
         if (repo != null && !repo.ExpectingClone)
         {
            // Use local-based provider here because remote-based one looks an overkill.
            // We anyway update discussion remote on attempt to show Discussions view but it might be unneeded right now
            IProjectUpdateContextProvider contextProvider =
               // silent updates work in "live" session only
               _liveSession?.MergeRequestCache?.GetLocalBasedContextProvider(projectKey);
            await repo.Updater.SilentUpdate(contextProvider);
         }
      }

      private void scheduleSilentUpdate(ProjectKey pk)
      {
         BeginInvoke(new Action(async () => await performSilentUpdate(pk)));
      }

      async private Task<bool> fetchMissingCommits(ILocalGitRepository repo, IEnumerable<string> heads)
      {
         _commitChainCreator = new CommitChainCreator(Program.Settings,
            status => labelWorkflowStatus.Text = status, updateGitStatusText,
            onCommitChainCancelEnabled, this, repo, heads, GitTools.IsSingleCommitFetchSupported(repo.Path),
            _gitlabClientManager.RepositoryManager);
         return await fetchMissingCommits();
      }

      async private Task<bool> fetchMissingCommits()
      {
         enableControlsOnGitAsyncOperation(false, "restoring merged commits");
         try
         {
            return await _commitChainCreator.CreateChainAsync();
         }
         finally
         {
            enableControlsOnGitAsyncOperation(true, "restoring merged commits");
         }
      }
   }
}

