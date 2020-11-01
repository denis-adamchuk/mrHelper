using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.App.Interprocess;
using mrHelper.StorageSupport;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      async private Task showDiscussionsFormAsync(MergeRequestKey mrk, string title, User author, string webUrl)
      {
         Debug.Assert(getHostName() != String.Empty);
         Debug.Assert(getCurrentUser() != null);

         // Store data before async/await
         User currentUser = getCurrentUser();
         DataCache dataCache = getDataCache(getCurrentTabDataCacheType());
         GitLabInstance gitLabInstance = new GitLabInstance(getHostName(), Program.Settings);
         if (dataCache == null)
         {
            Debug.Assert(false);
            return;
         }

         if (getCurrentTabDataCacheType() == EDataCacheType.Search)
         {
            // Pre-load discussions for MR in Search mode
            dataCache.DiscussionCache.RequestUpdate(mrk, PseudoTimerInterval, null);
         }

         IEnumerable<Discussion> discussions = await loadDiscussionsAsync(dataCache, mrk);
         if (discussions == null || _exiting)
         {
            return;
         }

         ILocalCommitStorage storage = getCommitStorage(mrk.ProjectKey, true);
         if (!await prepareStorageForDiscussionsForm(mrk, storage, discussions) || _exiting)
         {
            return;
         }
         showDiscussionForm(gitLabInstance, dataCache, storage, currentUser, mrk, discussions, title, author, webUrl);
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
         User currentUser, MergeRequestKey mrk, IEnumerable<Discussion> discussions, string title, User author,
         string webUrl)
      {
         if (currentUser == null || discussions == null || author == null || currentUser.Id == 0)
         {
            return;
         }

         bool doesMatchTag(object tag) => tag != null && ((MergeRequestKey)(tag)).Equals(mrk);
         Form formExisting = WinFormsHelpers.FindFormByTag("DiscussionsForm", doesMatchTag);
         if (formExisting != null)
         {
            formExisting.Activate();
            Trace.TraceInformation(String.Format("[MainForm] Activated an existing Discussions view for MR {0}", mrk.IId));
            return;
         }

         labelOperationStatus.Text = "Rendering discussion contexts...";
         labelOperationStatus.Refresh();

         DiscussionsForm form;
         try
         {
            IAsyncGitCommandService git = storage?.Git;

            DiscussionsForm discussionsForm = new DiscussionsForm(dataCache, gitLabInstance, _modificationNotifier,
               git, currentUser, mrk, discussions, title, author, _colorScheme,
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
            () => dataCache?.DiscussionCache?.RequestUpdate(mrk, Constants.DiscussionCheckOnNewThreadInterval, null),
            webUrl)
            {
               Tag = mrk
            };
            form = discussionsForm;
         }
         catch (NoDiscussionsToShow)
         {
            MessageBox.Show("No discussions to show.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Trace.TraceInformation(String.Format("[MainForm] No discussions to show for MR IID {0}", mrk.IId));
            labelOperationStatus.Text = "No discussions to show";
            return;
         }

         labelOperationStatus.Text = "Opening Discussions view...";
         labelOperationStatus.Refresh();

         form.Show();

         Trace.TraceInformation(String.Format("[MainForm] Opened Discussions for MR IId {0} (at {1})",
            mrk.IId, (storage?.Path ?? "null")));

         labelOperationStatus.Text = "Discussions opened";
      }

      async private Task onLaunchDiffToolAsync(MergeRequestKey mrk)
      {
         // Keep data before async/await
         DataCache dataCache = getDataCache(getCurrentTabDataCacheType());
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
         setReviewedRevisions(mrk, reviewedRevisions);

         MergeRequestKey? currentMrk = getMergeRequestKey(null);
         if (currentMrk.HasValue && currentMrk.Value.Equals(mrk))
         {
            revisionBrowser.UpdateReviewedRevisions(reviewedRevisions, type.Value);
         }
      }

      private void launchDiffTool(string leftSHA, string rightSHA, ILocalCommitStorage storage,
         MergeRequestKey mrk, string accessToken, string sessionName)
      {
         labelOperationStatus.Text = "Launching diff tool...";

         int? pid = null;
         try
         {
            DiffToolArguments arg = new DiffToolArguments(true, Constants.GitDiffToolName, leftSHA, rightSHA);
            pid = storage.Git?.LaunchDiffTool(arg) ?? null;
         }
         catch (DiffToolLaunchException)
         {
            MessageBox.Show("Cannot launch diff tool", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            labelOperationStatus.Text = String.Empty;
         }

         if (!pid.HasValue)
         {
            Trace.TraceInformation(String.Format("[MainForm] Cannot launch Diff Tool for MR with IId {0}", mrk.IId));
            return; // e.g. storage.Git got disposed
         }

         Trace.TraceInformation(String.Format("[MainForm] Launched DiffTool for SHA {0} vs SHA {1} (at {2}). PID {3}",
            leftSHA, rightSHA, storage.Path, pid.Value.ToString()));

         if (pid == -1)
         {
            labelOperationStatus.Text = "Diff tool was not launched. Most likely the difference is empty.";
         }
         else
         {
            labelOperationStatus.Text = "Diff tool launched";
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

      private void launchDiffToolForSelectedMergeRequest()
      {
         BeginInvoke(new Action(async () =>
         {
            Debug.Assert(getMergeRequestKey(null).HasValue);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;
            await onLaunchDiffToolAsync(mrk);
         }));
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

         labelOperationStatus.Text = "Loading discussions...";
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
            labelOperationStatus.Text = message;
            return null;
         }

         bool anyDiscussions = discussions != null && discussions.Any();
         labelOperationStatus.Text = anyDiscussions
            ? "Discussions loaded"
            : "There are no discussions in this Merge Request";
         return discussions;
      }

      /// <summary>
      /// Clean up records that correspond to merge requests that are missing in the cache
      /// </summary>
      private IEnumerable<MergeRequestKey> gatherClosedReviewedMergeRequests(DataCache dataCache, string hostname)
      {
         if (dataCache?.MergeRequestCache == null)
         {
            return Array.Empty<MergeRequestKey>();
         }

         IEnumerable<ProjectKey> projectKeys = dataCache.MergeRequestCache.GetProjects();

         // gather all MR from projects that no longer in use
         IEnumerable<MergeRequestKey> toRemove1 = _reviewedRevisions.Keys
            .Where(x => !projectKeys.Any(y => y.Equals(x.ProjectKey)));

         // gather all closed MR from existing projects
         IEnumerable<MergeRequestKey> toRemove2 = _reviewedRevisions.Keys
            .Where(x => projectKeys.Any(y => y.Equals(x.ProjectKey))
               && !dataCache.MergeRequestCache.GetMergeRequests(x.ProjectKey).Any(y => y.IId == x.IId));

         return toRemove1
            .Concat(toRemove2)
            .Where(key => key.ProjectKey.HostName == hostname)
            .ToArray();
      }

      /// <summary>
      /// Clean up records that correspond to the passed merge requests
      /// </summary>
      private void cleanupReviewedMergeRequests(IEnumerable<MergeRequestKey> keys)
      {
         foreach (MergeRequestKey key in keys)
         {
            _reviewedRevisions.Remove(key);
         }

         if (keys.Any())
         {
            saveState();
         }
      }

      private HashSet<string> getReviewedRevisions(MergeRequestKey mrk)
      {
         if (isReviewedMergeRequest(mrk))
         {
            return _reviewedRevisions[mrk].ToHashSet(); // copy
         }
         return new HashSet<string>();
      }

      private void setReviewedRevisions(MergeRequestKey mrk, HashSet<string> revisions)
      {
         _reviewedRevisions[mrk] = revisions;
         saveState();
      }

      private void ensureMergeRequestIsReviewed(MergeRequestKey mrk)
      {
         if (!isReviewedMergeRequest(mrk))
         {
            setReviewedRevisions(mrk, new HashSet<string>());
         }
      }

      private bool isReviewedMergeRequest(MergeRequestKey mrk)
      {
         return _reviewedRevisions.ContainsKey(mrk);
      }

      private void addRecentMergeRequestKeys(IEnumerable<MergeRequestKey> keys)
      {
         foreach (MergeRequestKey key in keys)
         {
            _recentMergeRequests.Add(key);
         }

         if (keys.Any())
         {
            saveState();
         }
      }

      private bool cleanupOldRecentMergeRequests(string hostname)
      {
         bool changed = false;
         IEnumerable<IGrouping<ProjectKey, MergeRequestKey>> groups =
            _recentMergeRequests
            .Where(key => key.ProjectKey.HostName == hostname)
            .GroupBy(key => key.ProjectKey);
         foreach (IGrouping<ProjectKey, MergeRequestKey> group in groups)
         {
            IEnumerable<MergeRequestKey> groupedKeys = group.AsEnumerable();
            if (groupedKeys.Any())
            {
               IEnumerable<MergeRequestKey> oldMergeRequests = groupedKeys
                  .OrderByDescending(mergeRequestKey => mergeRequestKey.IId)
                  .Skip(Constants.RecentMergeRequestPerProjectCount)
                  .ToArray(); // copy
               foreach (MergeRequestKey mergeRequestKey in oldMergeRequests)
               {
                  _recentMergeRequests.Remove(mergeRequestKey);
                  changed = true;
               }
               cleanupReviewedMergeRequests(oldMergeRequests);
            }
         }

         if (changed)
         {
            saveState();
         }

         return changed;
      }

      private bool cleanupReopenedRecentMergeRequests()
      {
         bool changed = false;
         List<MergeRequestKey> reopened = new List<MergeRequestKey>();
         foreach (MergeRequestKey key in _recentMergeRequests)
         {
            MergeRequest mergeRequest = getDataCache(EDataCacheType.Live)?.MergeRequestCache?.GetMergeRequest(key);
            if (mergeRequest != null && mergeRequest.State == "opened")
            {
               reopened.Add(key);
            }
         }

         foreach (MergeRequestKey key in reopened)
         {
            _recentMergeRequests.Remove(key);
            changed = true;
         }

         if (changed)
         {
            saveState();
         }

         return changed;
      }
   }
}

