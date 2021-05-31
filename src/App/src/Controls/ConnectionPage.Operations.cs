using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Forms;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.App.Helpers.GitLab;
using mrHelper.App.Interprocess;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;
using mrHelper.CustomActions;
using mrHelper.GitLabClient;
using mrHelper.StorageSupport;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
   {
      internal enum EDataCacheType
      {
         Live,
         Search,
         Recent
      }

      private void editSelectedMergeRequest()
      {
         Debug.Assert(getCurrentTabDataCacheType() == EDataCacheType.Live);
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         FullMergeRequestKey? fmk = getListView(EDataCacheType.Live).GetSelectedMergeRequest();
         if (!fmk.HasValue || !checkIfMergeRequestCanBeEdited())
         {
            return;
         }

         IEnumerable<User> fullUserList = dataCache?.UserCache?.GetUsers();
         bool isUserListReady = fullUserList?.Any() ?? false;
         if (!isUserListReady)
         {
            Debug.Assert(false);
            Trace.TraceError("[ConnectionPage] User List is not ready at the moment of Edit click");
            return;
         }

         BeginInvoke(new Action(async () => await applyChangesToMergeRequestAsync(
            dataCache, HostName, CurrentUser, fmk.Value, fullUserList)));
      }

      private void acceptSelectedMergeRequest()
      {
         Debug.Assert(getCurrentTabDataCacheType() == EDataCacheType.Live);
         FullMergeRequestKey? fmk = getListView(EDataCacheType.Live).GetSelectedMergeRequest();
         if (!fmk.HasValue || !checkIfMergeRequestCanBeEdited())
         {
            return;
         }

         IEnumerable<Project> fullProjectList = getDataCache(EDataCacheType.Live)?.ProjectCache?.GetProjects();
         bool isProjectListReady = fullProjectList?.Any() ?? false;
         if (!isProjectListReady)
         {
            Debug.Assert(false); // full project list is needed to check project properties inside the dialog code
            Trace.TraceError("[ConnectionPage] Project List is not ready at the moment of Accept click");
            return;
         }

         acceptMergeRequest(fmk.Value);
      }

      private void closeSelectedMergeRequest()
      {
         Debug.Assert(getCurrentTabDataCacheType() == EDataCacheType.Live);
         FullMergeRequestKey? fmk = getListView(EDataCacheType.Live).GetSelectedMergeRequest();
         if (!fmk.HasValue || !checkIfMergeRequestCanBeEdited())
         {
            return;
         }

         BeginInvoke(new Action(async () => await closeMergeRequestAsync(fmk.Value)));
      }

      private void refreshSelectedMergeRequest()
      {
         EDataCacheType type = getCurrentTabDataCacheType();
         FullMergeRequestKey? fmk = getListView(type).GetSelectedMergeRequest();
         if (!fmk.HasValue)
         {
            return;
         }

         MergeRequestKey mrk = new MergeRequestKey(fmk.Value.ProjectKey, fmk.Value.MergeRequest.IId);
         requestUpdates(getDataCache(type), mrk, PseudoTimerInterval, () =>
            addOperationRecord(String.Format("Merge Request !{0} has been refreshed", mrk.IId)));
      }

      private void muteSelectedMergeRequestUntilTomorrow()
      {
         EDataCacheType type = getCurrentTabDataCacheType();
         getListView(type).MuteSelectedMergeRequestFor(TimeUtils.GetTimeTillMorning());
      }

      private void muteSelectedMergeRequestUntilMonday()
      {

         EDataCacheType type = getCurrentTabDataCacheType();
         getListView(type).MuteSelectedMergeRequestFor(TimeUtils.GetTimeTillMonday());
      }

      private void unMuteSelectedMergeRequest()
      {
         EDataCacheType type = getCurrentTabDataCacheType();
         getListView(type).UnmuteSelectedMergeRequest();
      }

      private void createNewMergeRequest(string hostname, User currentUser, NewMergeRequestProperties initialProperties,
         IEnumerable<Project> fullProjectList, IEnumerable<User> fullUserList)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         var sourceBranchesInUse = GitLabClient.Helpers.GetSourceBranchesByUser(CurrentUser, dataCache);

         using (MergeRequestPropertiesForm form = new NewMergeRequestForm(hostname,
            _shortcuts.GetProjectAccessor(), currentUser, initialProperties, fullProjectList, fullUserList,
            sourceBranchesInUse, _expressionResolver.Resolve(Program.ServiceManager.GetSourceBranchTemplate())))
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               Trace.TraceInformation("[ConnectionPage] User declined to create a merge request");
               return;
            }

            BeginInvoke(new Action(
               async () =>
               {
                  ProjectKey projectKey = new ProjectKey(hostname, form.ProjectName);
                  SubmitNewMergeRequestParameters parameters = new SubmitNewMergeRequestParameters(
                     projectKey, form.SourceBranch, form.TargetBranch, form.Title,
                     form.AssigneeUsername, form.Description, form.DeleteSourceBranch, form.Squash,
                     form.IsHighPriority);
                  await createNewMergeRequestAsync(parameters, form.SpecialNote);
               }));
         }
      }

      private void acceptMergeRequest(FullMergeRequestKey item)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         MergeRequestKey mrk = new MergeRequestKey(item.ProjectKey, item.MergeRequest.IId);
         bool doesMatchTag(object tag) => tag != null && ((MergeRequestKey)(tag)).Equals(mrk);
         Form formExisting = WinFormsHelpers.FindFormByTag("AcceptMergeRequestForm", doesMatchTag);
         if (formExisting != null)
         {
            formExisting.Activate();
            return;
         }

         AcceptMergeRequestForm form = new AcceptMergeRequestForm(
            mrk,
            getCommitStorage(mrk.ProjectKey, false)?.Path,
            () =>
            {
               addOperationRecord(String.Format("Merge Request !{0} has been merged successfully", mrk.IId));
               requestUpdates(EDataCacheType.Live, null, new int[] {
                  Program.Settings.NewOrClosedMergeRequestRefreshListDelayMs });
            },
            showDiscussionsFormAsync,
            () => dataCache,
            async () =>
            {
               await checkForUpdatesAsync(dataCache, mrk, DataCacheUpdateKind.MergeRequest);
               return dataCache;
            },
            () => _shortcuts.GetMergeRequestAccessor(mrk.ProjectKey.ProjectName))
         {
            Tag = mrk
         };
         form.Show();
      }

      private void editTimeOfSelectedMergeRequest()
      {
         BeginInvoke(new Action(async () =>
         {
            // Store data before opening a modal dialog
            Debug.Assert(getMergeRequestKey(null).HasValue);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;

            await editTrackedTimeAsync(mrk, getDataCache(getCurrentTabDataCacheType()));
         }));
      }

      private async Task editTrackedTimeAsync(MergeRequestKey mrk, DataCache dataCache)
      {
         IMergeRequestEditor editor = _shortcuts.GetMergeRequestEditor(mrk);
         TimeSpan? oldSpanOpt = dataCache?.TotalTimeCache?.GetTotalTime(mrk).Amount;
         if (!oldSpanOpt.HasValue)
         {
            return;
         }

         TimeSpan oldSpan = oldSpanOpt.Value;
         using (EditTimeForm form = new EditTimeForm(oldSpan))
         {
            if (form.ShowDialog() == DialogResult.OK)
            {
               TimeSpan newSpan = form.TimeSpan;
               bool add = newSpan > oldSpan;
               TimeSpan diffTemp = add ? newSpan - oldSpan : oldSpan - newSpan;
               TimeSpan diff = new TimeSpan(diffTemp.Hours, diffTemp.Minutes, diffTemp.Seconds);
               if (diff == TimeSpan.Zero || dataCache?.TotalTimeCache == null)
               {
                  return;
               }

               try
               {
                  await editor.AddTrackedTime(diff, add);
               }
               catch (TimeTrackingException ex)
               {
                  string message = "Cannot edit total tracked time";
                  ExceptionHandlers.Handle(message, ex);
                  MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return;
               }

               addOperationRecord("Total spent time has been updated");

               Trace.TraceInformation(String.Format("[ConnectionPage] Total time for MR {0} (project {1}) changed to {2}",
                  mrk.IId, mrk.ProjectKey.ProjectName, diff.ToString()));
            }
         }
      }

      private void addCommentForSelectedMergeRequest()
      {
         BeginInvoke(new Action(async () =>
         {
            MergeRequest mergeRequest = getMergeRequest(null);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;

            DataCache dataCache = getDataCache(getCurrentTabDataCacheType());
            AsyncDiscussionHelper discussionHelper = new AsyncDiscussionHelper(
               mrk, mergeRequest.Title, CurrentUser, _shortcuts);
            bool res = await discussionHelper.AddCommentAsync();
            addOperationRecord(res ? "New comment has been added" : "Comment has not been added");
         }));
      }

      private void newDiscussionForSelectedMergeRequest()
      {
         BeginInvoke(new Action(async () =>
         {
            MergeRequest mergeRequest = getMergeRequest(null);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;

            DataCache dataCache = getDataCache(getCurrentTabDataCacheType());
            AsyncDiscussionHelper discussionHelper = new AsyncDiscussionHelper(
               mrk, mergeRequest.Title, CurrentUser, _shortcuts);
            bool res = await discussionHelper.AddThreadAsync();
            addOperationRecord(res ? "A new discussion thread has been added" : "Discussion thread has not been added");
         }));
      }

      async private Task createNewMergeRequestAsync(SubmitNewMergeRequestParameters parameters, string firstNote)
      {
         addOperationRecord("Creating a merge request at GitLab has started");

         MergeRequestKey? mrkOpt = await MergeRequestEditHelper.SubmitNewMergeRequestAsync(
            parameters, firstNote, CurrentUser, _shortcuts);
         if (mrkOpt == null)
         {
            // all error handling is done at the callee side
            string message = "Merge Request has not been created";
            addOperationRecord(message);
            return;
         }

         requestUpdates(EDataCacheType.Live, null, new int[] {
            Program.Settings.NewOrClosedMergeRequestRefreshListDelayMs });

         addOperationRecord(String.Format("Merge Request !{0} has been created in project {1}",
            mrkOpt.Value.IId, parameters.ProjectKey.ProjectName));

         _newMergeRequestDialogStatesByHosts[HostName] = new NewMergeRequestProperties(
            parameters.ProjectKey.ProjectName, null, null, parameters.AssigneeUserName, parameters.Squash,
            parameters.DeleteSourceBranch);

         Trace.TraceInformation(
            "[ConnectionPage] Created a new merge request. " +
            "Project: {0}, SourceBranch: {1}, TargetBranch: {2}, Assignee: {3}, firstNote: {4}",
            parameters.ProjectKey.ProjectName, parameters.SourceBranch, parameters.TargetBranch,
            parameters.AssigneeUserName, firstNote);
      }

      async private Task applyChangesToMergeRequestAsync(DataCache dataCache, string hostname, User currentUser,
         FullMergeRequestKey item, IEnumerable<User> fullUserList)
      {
         MergeRequestKey mrk = new MergeRequestKey(item.ProjectKey, item.MergeRequest.IId);
         string noteText = await MergeRequestEditHelper.GetLatestSpecialNote(dataCache.DiscussionCache, mrk);
         using (MergeRequestPropertiesForm form = new EditMergeRequestPropertiesForm(hostname,
            _shortcuts.GetProjectAccessor(), currentUser, item.ProjectKey, item.MergeRequest, noteText, fullUserList))
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               Trace.TraceInformation("[ConnectionPage] User declined to modify a merge request");
               return;
            }

            ApplyMergeRequestChangesParameters parameters =
               new ApplyMergeRequestChangesParameters(form.Title, form.AssigneeUsername,
               form.Description, form.TargetBranch, form.DeleteSourceBranch, form.Squash,
               form.IsHighPriority);

            bool modified = await MergeRequestEditHelper.ApplyChangesToMergeRequest(
               item.ProjectKey, item.MergeRequest, parameters, noteText, form.SpecialNote, currentUser,
               _shortcuts);

            string statusMessage = modified
               ? String.Format("Merge Request !{0} has been modified", mrk.IId)
               : String.Format("No changes have been made to Merge Request !{0}", mrk.IId);
            addOperationRecord(statusMessage);

            if (modified)
            {
               requestUpdates(EDataCacheType.Live, mrk, new int[] {
               Program.Settings.OneShotUpdateFirstChanceDelayMs,
               Program.Settings.OneShotUpdateSecondChanceDelayMs });
            }
         }
      }

      async private Task closeMergeRequestAsync(FullMergeRequestKey item)
      {
         MergeRequestKey mrk = new MergeRequestKey(item.ProjectKey, item.MergeRequest.IId);
         string message =
            "Do you really want to close (cancel) merge request? It will not be merged to the target branch.";
         if (MessageBox.Show(message, "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
         {
            await MergeRequestEditHelper.CloseMergeRequest(mrk, _shortcuts);

            string statusMessage = String.Format("Merge Request !{0} has been closed", mrk.IId);
            addOperationRecord(statusMessage);

            requestUpdates(EDataCacheType.Live, null, new int[] {
               Program.Settings.NewOrClosedMergeRequestRefreshListDelayMs });
         }
         else
         {
            Trace.TraceInformation("[ConnectionPage] User declined to close a merge request");
         }
      }

      private void showDiscussionsForSelectedMergeRequest()
      {
         BeginInvoke(new Action(async () =>
         {
            if (getMergeRequest(null) == null || !getMergeRequestKey(null).HasValue)
            {
               Debug.Assert(false);
               return;
            }

            MergeRequest mergeRequest = getMergeRequest(null);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;

            await showDiscussionsFormAsync(mrk, mergeRequest.Title, mergeRequest.Author, mergeRequest.Web_Url);
         }));
      }

      async private Task showDiscussionsFormAsync(MergeRequestKey mrk, string title, User author, string webUrl)
      {
         Debug.Assert(HostName != String.Empty);
         Debug.Assert(CurrentUser != null);

         // Store data before async/await
         User currentUser = CurrentUser;
         DataCache dataCache = getDataCache(getCurrentTabDataCacheType());
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
         showDiscussionForm(dataCache, storage, currentUser, mrk, discussions, title, author, webUrl);
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
               Trace.TraceInformation("[ConnectionPage] User rejected to show discussions without a storage");
               return false;
            }
            else
            {
               Trace.TraceInformation("[ConnectionPage] User decided to show Discussions without a storage");
               return true;
            }
         }

         ICommitStorageUpdateContextProvider contextProvider = new DiscussionBasedContextProvider(discussions);
         return await prepareCommitStorage(mrk, storage, contextProvider, false);
      }

      private void showDiscussionForm(DataCache dataCache, ILocalCommitStorage storage, User currentUser,
         MergeRequestKey mrk, IEnumerable<Discussion> discussions, string title, User author, string webUrl)
      {
         if (currentUser == null || discussions == null || author == null || currentUser.Id == 0)
         {
            return;
         }

         bool doesMatchTag(object tag) => tag != null && ((MergeRequestKey)(tag)).Equals(mrk);
         Form formExisting = WinFormsHelpers.FindFormByTag("DiscussionsForm", doesMatchTag);
         if (formExisting is DiscussionsForm existingDiscussionsForm)
         {
            existingDiscussionsForm.Restore();
            Trace.TraceInformation(String.Format("[ConnectionPage] Activated an existing Discussions view for MR {0}", mrk.IId));
            return;
         }

         addOperationRecord("Rendering discussion contexts has started");

         DiscussionsForm form;
         try
         {
            IAsyncGitCommandService git = storage?.Git;

            AsyncDiscussionLoader discussionLoader = new AsyncDiscussionLoader(mrk, dataCache,
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
                  Trace.TraceInformation("[ConnectionPage] User tried to refresh Discussions without a storage");
                  MessageBox.Show("Cannot update a storage, some context code snippets may be missing. ",
                     "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
               }
            }, this);

            AsyncDiscussionHelper discussionHelper = new AsyncDiscussionHelper(mrk, title, currentUser, _shortcuts);

            DiscussionsForm discussionsForm = new DiscussionsForm(
               git, currentUser, mrk, discussions, title, author, _colorScheme,
               discussionLoader, discussionHelper, webUrl, _shortcuts, GetCustomActionList())
            {
               Tag = mrk
            };
            form = discussionsForm;
         }
         catch (NoDiscussionsToShow)
         {
            MessageBox.Show("No discussions to show.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Trace.TraceInformation(String.Format("[ConnectionPage] No discussions to show for MR IID {0}", mrk.IId));
            addOperationRecord("No discussions to show");
            return;
         }

         addOperationRecord("Opening Discussions view has started");

         form.Show();

         Trace.TraceInformation(String.Format("[ConnectionPage] Opened Discussions for MR IId {0} (at {1})",
            mrk.IId, (storage?.Path ?? "null")));

         addOperationRecord("Discussions view has opened");
         ensureMergeRequestInRecentDataCache(mrk);
      }

      private void onColorSchemeChanged()
      {
         getListView(getCurrentTabDataCacheType())?.Invalidate();
      }

      async private Task onLaunchDiffToolAsync(MergeRequestKey mrk,
         string leftSHA, string rightSHA, IEnumerable<string> includedSHA, RevisionType? type)
      {
         // Keep data before async/await
         DataCache dataCache = getDataCache(getCurrentTabDataCacheType());
         if (dataCache == null
          || String.IsNullOrWhiteSpace(leftSHA)
          || String.IsNullOrWhiteSpace(rightSHA)
          || includedSHA == null
          || !includedSHA.Any()
          || !type.HasValue)
         {
            Debug.Assert(false);
            return;
         }

         // Discussions are needed to show previous and related discussions in Start New Thread dialog.
         // In many cases they are already cached.
         IEnumerable<Discussion> discussions = await loadDiscussionsAsync(dataCache, mrk);
         if (_exiting)
         {
            return;
         }

         ILocalCommitStorage storage = getCommitStorage(mrk.ProjectKey, true);
         if (!await prepareStorageForDiffTool(mrk, storage, leftSHA, rightSHA, discussions) || _exiting)
         {
            return;
         }

         launchDiffTool(leftSHA, rightSHA, storage, mrk, dataCache);

         HashSet<string> reviewedRevisions = getReviewedRevisions(mrk);
         foreach (string sha in includedSHA)
         {
            reviewedRevisions.Add(sha);
         }
         setReviewedRevisions(mrk, reviewedRevisions);
         ensureMergeRequestInRecentDataCache(mrk);

         MergeRequestKey? currentMrk = getMergeRequestKey(null);
         if (currentMrk.HasValue && currentMrk.Value.Equals(mrk))
         {
            revisionBrowser.UpdateReviewedRevisions(reviewedRevisions, type.Value);
         }
      }

      private void launchDiffTool(string leftSHA, string rightSHA, ILocalCommitStorage storage,
         MergeRequestKey mrk, DataCache dataCache)
      {
         addOperationRecord("Launching diff tool has started");

         int? pid = null;
         try
         {
            DiffToolArguments arg = new DiffToolArguments(true, Constants.GitDiffToolName, leftSHA, rightSHA);
            pid = storage.Git?.LaunchDiffTool(arg) ?? null;
         }
         catch (DiffToolLaunchException)
         {
            string message = "Cannot launch diff tool";
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            addOperationRecord(message);
         }

         if (!pid.HasValue)
         {
            Trace.TraceInformation(String.Format("[ConnectionPage] Cannot launch Diff Tool for MR with IId {0}", mrk.IId));
            return; // e.g. storage.Git got disposed
         }

         Trace.TraceInformation(String.Format("[ConnectionPage] Launched DiffTool for SHA {0} vs SHA {1} (at {2}). PID {3}",
            leftSHA, rightSHA, storage.Path, pid.Value.ToString()));

         if (pid == -1)
         {
            addOperationRecord("Diff tool was not launched. Most likely the difference is empty.");
         }
         else
         {
            addOperationRecord("Diff tool has launched");
            saveInterprocessSnapshot(pid.Value, leftSHA, rightSHA, mrk, dataCache);
         }
      }

      async private Task<bool> prepareStorageForDiffTool(MergeRequestKey mrk,
         ILocalCommitStorage storage, string leftSHA, string rightSHA, IEnumerable<Discussion> discussions)
      {
         if (storage == null)
         {
            MessageBox.Show("Cannot launch a diff tool without up-to-date storage.",
               "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
         }

         ICommitStorageUpdateContextProvider contextProvider =
            new CommitBasedContextProvider(new string[] { rightSHA }, leftSHA);
         if (!await prepareCommitStorage(mrk, storage, contextProvider, true) || _exiting)
         {
            return false;
         }

         ICommitStorageUpdateContextProvider contextProvider2 = new DiscussionBasedContextProvider(discussions);
         return await prepareCommitStorage(mrk, storage, contextProvider2, false);
      }

      private void launchDiffTool(DiffToolMode mode)
      {
         MergeRequestKey? mrkOpt = getMergeRequestKey(null);
         if (!mrkOpt.HasValue || !CanDiffTool(mode))
         {
            Debug.Assert(false);
            return;
         }

         string leftSHA;
         string rightSHA;
         string[] includedSHA = revisionBrowser.GetIncludedBySelectedSha();
         string[] selected = revisionBrowser.GetSelectedSha(out RevisionType? type);
         switch (mode)
         {
            case DiffToolMode.DiffBetweenSelected:
               leftSHA = selected[0];
               rightSHA = selected[1];
               break;

            case DiffToolMode.DiffSelectedToBase:
               leftSHA = revisionBrowser.GetBaseCommitSha();
               rightSHA = selected[0];
               break;

            case DiffToolMode.DiffSelectedToParent:
               leftSHA = revisionBrowser.GetParentShaForSelected();
               rightSHA = selected[0];
               break;

            case DiffToolMode.DiffLatestToBase:
               type = ConfigurationHelper.GetDefaultRevisionType(Program.Settings);
               includedSHA = revisionBrowser.GetIncludedSha(type.Value);
               leftSHA = revisionBrowser.GetBaseCommitSha();
               rightSHA = revisionBrowser.GetHeadSha(type.Value);
               break;

            default:
               Debug.Assert(false);
               return;
         }

         BeginInvoke(new Action(async () =>
            await onLaunchDiffToolAsync(mrkOpt.Value, leftSHA, rightSHA, includedSHA, type)));
      }

      private void launchDiffWithBaseForSelectedMergeRequest()
      {
         if (CanDiffTool(DiffToolMode.DiffSelectedToBase))
         {
            launchDiffTool(DiffToolMode.DiffSelectedToBase);
         }
      }

      private void launchDiffToolForSelectedMergeRequest()
      {
         if (CanDiffTool(DiffToolMode.DiffBetweenSelected))
         {
            launchDiffTool(DiffToolMode.DiffBetweenSelected);
         }
         else
         {
            launchDiffWithBaseForSelectedMergeRequest();
         }
      }

      private void saveInterprocessSnapshot(int pid, string leftSHA, string rightSHA, MergeRequestKey mrk,
         DataCache dataCache)
      {
         // leftSHA - Base commit SHA in the source branch
         // rightSHA - SHA referencing HEAD of this merge request
         Snapshot snapshot = new Snapshot(
            mrk.IId,
            mrk.ProjectKey.HostName,
            mrk.ProjectKey.ProjectName,
            new Core.Matching.DiffRefs(leftSHA, rightSHA),
            Program.Settings.LocalStorageFolder,
            getDataCacheName(dataCache),
            dataCache.ConnectionContext?.GetHashCode() ?? 0);

         SnapshotSerializer serializer = new SnapshotSerializer();
         try
         {
            serializer.SerializeToDisk(snapshot, pid);
         }
         catch (Exception ex) // Any exception from serialization code
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

         addOperationRecord("Loading discussions has started");
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
            addOperationRecord(message);
            return null;
         }

         bool anyDiscussions = discussions != null && discussions.Any();
         addOperationRecord(anyDiscussions
            ? "Discussions have been loaded"
            : "There are no discussions in this Merge Request");
         return discussions;
      }

      /// <summary>
      /// Collect records that correspond to merge requests that are missing in the cache
      /// </summary>
      private IEnumerable<MergeRequestKey> gatherClosedReviewedMergeRequests(DataCache dataCache, string hostname)
      {
         if (dataCache?.MergeRequestCache == null)
         {
            return Array.Empty<MergeRequestKey>();
         }

         IEnumerable<ProjectKey> projectKeys = dataCache.MergeRequestCache.GetProjects();

         // gather all MR from projects that no longer in use
         IEnumerable<MergeRequestKey> toRemove1 = _reviewedRevisions.Data.Keys
            .Where(x => !projectKeys.Any(y => y.Equals(x.ProjectKey)));

         // gather all closed MR from existing projects
         IEnumerable<MergeRequestKey> toRemove2 = _reviewedRevisions.Data.Keys
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
         _reviewedRevisions.RemoveMany(keys);
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
      }

      private bool isReviewedMergeRequest(MergeRequestKey mrk)
      {
         return _reviewedRevisions.Data.ContainsKey(mrk);
      }

      private bool checkIfMergeRequestCanBeEdited()
      {
         string hostname = HostName;
         User currentUser = CurrentUser;
         FullMergeRequestKey item = getListView(EDataCacheType.Live).GetSelectedMergeRequest().Value;
         if (hostname == String.Empty || currentUser == null || item.MergeRequest == null)
         {
            Debug.Assert(false);
            MessageBox.Show("Cannot modify a merge request", "Internal error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("Unexpected application state." +
               "hostname is empty string={0}, currentUser is null={1}, item.MergeRequest is null={2}",
               hostname == String.Empty, currentUser == null, item.MergeRequest == null);
            return false;
         }
         return true;
      }


      ///

      private ILocalCommitStorageFactory getCommitStorageFactory(bool showMessageBoxOnError)
      {
         if (_storageFactory == null)
         {
            try
            {
               _storageFactory = new LocalCommitStorageFactory(Program.Settings.LocalStorageFolder,
                  this, _shortcuts.GetProjectAccessor(), Program.Settings);
               _storageFactory.GitRepositoryCloned += onGitRepositoryCloned;
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle("Cannot create LocalGitCommitStorageFactory", ex);
            }
         }

         if (_storageFactory == null && showMessageBoxOnError)
         {
            MessageBox.Show(String.Format("Cannot create folder {0}", Program.Settings.LocalStorageFolder),
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         return _storageFactory;
      }

      private void disposeLocalGitRepositoryFactory()
      {
         if (_storageFactory != null)
         {
            _storageFactory.GitRepositoryCloned -= onGitRepositoryCloned;
            _storageFactory.Dispose();
            _storageFactory = null;
         }
      }

      private void onGitRepositoryCloned(ILocalCommitStorage storage)
      {
         requestCommitStorageUpdate(storage.ProjectKey);
      }

      /// <summary>
      /// Make some checks and create a commit storage
      /// </summary>
      /// <returns>null if could not create a repository</returns>
      private ILocalCommitStorage getCommitStorage(ProjectKey projectKey, bool showMessageBoxOnError)
      {
         ILocalCommitStorageFactory factory = getCommitStorageFactory(showMessageBoxOnError);
         if (factory == null)
         {
            return null;
         }

         LocalCommitStorageType type = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
         ILocalCommitStorage repo = factory.GetStorage(projectKey, type);
         if (repo == null && showMessageBoxOnError)
         {
            MessageBox.Show(String.Format(
               "Cannot obtain disk storage for project {0} in \"{1}\"",
               projectKey.ProjectName, Program.Settings.LocalStorageFolder),
               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
         return repo;
      }

      private void onAbortGitByUserRequest()
      {
         MergeRequestKey? mrk = getMergeRequestKey(null);
         if (!mrk.HasValue)
         {
            Debug.Assert(mrk.HasValue);
            return;
         }

         ILocalCommitStorage repo = getCommitStorage(mrk.Value.ProjectKey, false);
         if (repo == null || repo.Updater == null || !repo.Updater.CanBeStopped())
         {
            Debug.Assert(mrk.HasValue);
            return;
         }

         string message = String.Format("Do you really want to abort current git update operation for {0}?",
            mrk.Value.ProjectKey.ProjectName);
         if (MessageBox.Show(message, "Confirmation",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
         {
            Trace.TraceInformation(String.Format("[ConnectionPage] User declined to abort current operation for project {0}",
               mrk.Value.ProjectKey.ProjectName));
            return;
         }

         Trace.TraceInformation(String.Format("[ConnectionPage] User decided to abort current operation for project {0}",
            mrk.Value.ProjectKey.ProjectName));
         repo.Updater.StopUpdate();
      }

      private void requestCommitStorageUpdate(ProjectKey projectKey)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);

         IEnumerable<GitLabSharp.Entities.Version> versions = dataCache?.MergeRequestCache?.GetVersions(projectKey);
         if (versions != null)
         {
            VersionBasedContextProvider contextProvider = new VersionBasedContextProvider(versions);
            ILocalCommitStorage storage = getCommitStorage(projectKey, false);
            storage?.Updater?.RequestUpdate(contextProvider, null);
         }
      }

      async private Task<bool> prepareCommitStorage(
         MergeRequestKey mrk, ILocalCommitStorage storage, ICommitStorageUpdateContextProvider contextProvider,
         bool isLimitExceptionFatal)
      {
         Trace.TraceInformation(String.Format(
            "[ConnectionPage] Preparing commit storage by user request for MR IId {0} (at {1})...",
            mrk.IId, storage.Path));

         try
         {
            _mergeRequestsUpdatingByUserRequest.Add(mrk);
            CanDiffToolChanged?.Invoke(this);
            CanDiscussionsChanged?.Invoke(this);
            addOperationRecord(getStorageSummaryUpdateInformation());
            await storage.Updater.StartUpdate(contextProvider, status => onStorageUpdateProgressChange(status, mrk),
               () => onStorageUpdateStateChange());
            return true;
         }
         catch (LocalCommitStorageUpdaterCancelledException)
         {
            MessageBox.Show("Cannot perform requested action without up-to-date storage", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            addOperationRecord("Storage update cancelled by user");
            return false;
         }
         catch (LocalCommitStorageUpdaterFailedException fex)
         {
            ExceptionHandlers.Handle(fex.Message, fex);
            MessageBox.Show(fex.OriginalMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            addOperationRecord("Failed to update storage");
            return false;
         }
         catch (LocalCommitStorageUpdaterLimitException mex)
         {
            ExceptionHandlers.Handle(mex.Message, mex);
            if (!isLimitExceptionFatal)
            {
               return true;
            }
            string extraMessage = "If there are multiple revisions try selecting two other ones";
            MessageBox.Show(mex.OriginalMessage + ". " + extraMessage, "Error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            addOperationRecord("Failed to update storage");
            return false;
         }
         catch (Exception) // just in case
         {
            Debug.Assert(false);
            return false;
         }
         finally
         {
            if (!_exiting)
            {
               _mergeRequestsUpdatingByUserRequest.Remove(mrk);
               CanDiffToolChanged?.Invoke(this);
               CanDiscussionsChanged?.Invoke(this);
               addOperationRecord(getStorageSummaryUpdateInformation());
            }
         }
      }

      private string getStorageSummaryUpdateInformation()
      {
         if (!_mergeRequestsUpdatingByUserRequest.Any())
         {
            return "All storages are up-to-date";
         }

         var mergeRequestGroups = _mergeRequestsUpdatingByUserRequest
            .Distinct()
            .GroupBy(
               group => group.ProjectKey,
               group => group,
               (group, groupedMergeRequests) => new
               {
                  Project = group.ProjectName,
                  MergeRequests = groupedMergeRequests
               });

         List<string> storages = new List<string>();
         foreach (var group in mergeRequestGroups)
         {
            IEnumerable<string> mergeRequestIds = group.MergeRequests.Select(x => String.Format("#{0}", x.IId));
            string mergeRequestIdsString = String.Join(", ", mergeRequestIds);
            string storage = String.Format("{0} ({1})", group.Project, mergeRequestIdsString);
            storages.Add(storage);
         }

         return String.Format("Updating storage{0}: {1}...",
            storages.Count() > 1 ? "s" : "", String.Join(", ", storages));
      }

      private string formatStorageStatusText(string text, MergeRequestKey? mrk)
      {
         return String.IsNullOrEmpty(text) || !mrk.HasValue
            ? String.Empty
            : String.Format("{0} #{1}: {2}", mrk.Value.ProjectKey.ProjectName, mrk.Value.IId.ToString(), text);
      }
   }
}

