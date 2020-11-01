using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers.GitLab;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void createNewMergeRequestByUserRequest()
      {
         Debug.Assert(getCurrentTabDataCacheType() == EDataCacheType.Live);
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         if (!checkIfMergeRequestCanBeCreated())
         {
            return;
         }

         IEnumerable<Project> fullProjectList = dataCache?.ProjectCache?.GetProjects();
         bool isProjectListReady = fullProjectList?.Any() ?? false;
         if (!isProjectListReady)
         {
            Debug.Assert(false);
            Trace.TraceError("[MainForm] Project List is not ready at the moment of Create New click");
            return;
         }

         IEnumerable<User> fullUserList = dataCache?.UserCache?.GetUsers();
         bool isUserListReady = fullUserList?.Any() ?? false;
         if (!isUserListReady)
         {
            Debug.Assert(false);
            Trace.TraceError("[MainForm] User List is not ready at the moment of Create New click");
            return;
         }

         string projectName = getDefaultProjectName();
         NewMergeRequestProperties initialFormState = getDefaultNewMergeRequestProperties(
            getHostName(), getCurrentUser(), projectName);
         createNewMergeRequest(getHostName(), getCurrentUser(), initialFormState, fullProjectList, fullUserList);
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
            Trace.TraceError("[MainForm] User List is not ready at the moment of Edit click");
            return;
         }

         BeginInvoke(new Action(async () => await applyChangesToMergeRequestAsync(
            dataCache, getHostName(), getCurrentUser(), fmk.Value, fullUserList)));
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
            Trace.TraceError("[MainForm] Project List is not ready at the moment of Accept click");
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

         BeginInvoke(new Action(async () => await closeMergeRequestAsync(getHostName(), fmk.Value)));
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
         requestUpdates(getDataCache(type), mrk, 100, () =>
            labelOperationStatus.Text = String.Format("Merge Request !{0} refreshed", mrk.IId));
      }

      private void createNewMergeRequest(string hostname, User currentUser, NewMergeRequestProperties initialProperties,
         IEnumerable<Project> fullProjectList, IEnumerable<User> fullUserList)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         var sourceBranchesInUse = GitLabClient.Helpers.GetSourceBranchesByUser(getCurrentUser(), dataCache);

         MergeRequestPropertiesForm form = new NewMergeRequestForm(hostname,
            getProjectAccessor(), currentUser, initialProperties, fullProjectList, fullUserList, sourceBranchesInUse,
            _expressionResolver.Resolve(Program.ServiceManager.GetSourceBranchTemplate()));
         if (form.ShowDialog() != DialogResult.OK)
         {
            Trace.TraceInformation("[MainForm] User declined to create a merge request");
            return;
         }

         BeginInvoke(new Action(
            async () =>
            {
               ProjectKey projectKey = new ProjectKey(hostname, form.ProjectName);
               SubmitNewMergeRequestParameters parameters = new SubmitNewMergeRequestParameters(
                  projectKey, form.SourceBranch, form.TargetBranch, form.Title,
                  form.AssigneeUsername, form.Description, form.DeleteSourceBranch, form.Squash);
               await createNewMergeRequestAsync(parameters, form.SpecialNote);
            }));
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
               labelOperationStatus.Text = String.Format("Merge Request !{0} has been merged successfully", mrk.IId);
               requestUpdates(EDataCacheType.Live, null, new int[] { NewOrClosedMergeRequestRefreshListTimerInterval });
            },
            showDiscussionsFormAsync,
            () => dataCache,
            async () =>
            {
               await checkForUpdatesAsync(dataCache, mrk, DataCacheUpdateKind.MergeRequest);
               return dataCache;
            },
            () => Shortcuts.GetMergeRequestAccessor(getProjectAccessor(), mrk.ProjectKey.ProjectName))
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

            Debug.Assert(getMergeRequest(null) != null);
            MergeRequest mr = getMergeRequest(null);

            await editTrackedTimeAsync(mrk, mr);
         }));
      }

      private async Task editTrackedTimeAsync(MergeRequestKey mrk, MergeRequest mr)
      {
         Debug.Assert(getCurrentTabDataCacheType() == EDataCacheType.Live);
         GitLabInstance gitLabInstance = new GitLabInstance(getHostName(), Program.Settings);
         IMergeRequestEditor editor = Shortcuts.GetMergeRequestEditor(gitLabInstance, _modificationNotifier, mrk);
         DataCache dataCache = getDataCache(getCurrentTabDataCacheType());
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
               TimeSpan diff = add ? newSpan - oldSpan : oldSpan - newSpan;
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

               updateTotalTime(mrk, mr.Author, mrk.ProjectKey.HostName, dataCache.TotalTimeCache);

               labelOperationStatus.Text = "Total spent time updated";

               Trace.TraceInformation(String.Format("[MainForm] Total time for MR {0} (project {1}) changed to {2}",
                  mrk.IId, mrk.ProjectKey.ProjectName, diff.ToString()));
            }
         }
      }

      private void startTimeTrackingTimer()
      {
         Debug.Assert(!isTrackingTime());

         // Start timer
         _timeTrackingTimer.Start();

         // Reset and start stopwatch
         Debug.Assert(getMergeRequestKey(null).HasValue);
         _timeTrackingTabPage = tabControlMode.SelectedTab;

         GitLabInstance gitLabInstance = new GitLabInstance(getHostName(), Program.Settings);
         _timeTracker = Shortcuts.GetTimeTracker(gitLabInstance, _modificationNotifier, getMergeRequestKey(null).Value);
         _timeTracker.Start();

         // Take care of controls that 'time tracking' mode shares with normal mode
         updateTotalTime(null, null, null, null);

         updateTrayIcon();
         updateTaskbarIcon();

         onTimerStarted();
      }

      private void stopTimeTrackingTimer()
      {
         BeginInvoke(new Action(async () => await stopTimeTrackingTimerAsync()));
      }

      async private Task stopTimeTrackingTimerAsync()
      {
         if (!isTrackingTime())
         {
            return;
         }

         DataCache dataCache = getDataCache(getCurrentTabDataCacheType());

         // Stop timer
         _timeTrackingTimer.Stop();

         // Reset member right now to not send tracked time again on re-entrance
         ITimeTracker timeTracker = _timeTracker;
         _timeTracker = null;
         _timeTrackingTabPage = null;

         // Stop stopwatch and send tracked time
         TimeSpan span = timeTracker.Elapsed;
         if (span.TotalSeconds > 1)
         {
            labelOperationStatus.Text = "Sending tracked time...";
            string duration = String.Format("{0}h {1}m {2}s",
               span.ToString("hh"), span.ToString("mm"), span.ToString("ss"));
            string status = String.Format("Tracked time {0} sent successfully", duration);
            try
            {
               await timeTracker.Stop();
            }
            catch (ForbiddenTimeTrackerException)
            {
               status = String.Format(
                  "Cannot report tracked time ({0}).\r\n"
                + "You don't have permissions to track time in {1} project.\r\n"
                + "Please contact {2} administrator or SCM team.",
                  duration, timeTracker.MergeRequest.ProjectKey.ProjectName, timeTracker.MergeRequest.ProjectKey.HostName);
               MessageBox.Show(status, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
               status = String.Format("Tracked time is not set. Set up permissions and report {0} manually", duration);
            }
            catch (TimeTrackerException ex)
            {
               status = String.Format("Error occurred. Tracked time {0} is not sent", duration);
               ExceptionHandlers.Handle(status, ex);
               MessageBox.Show(status, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            labelOperationStatus.Text = status;
         }
         else
         {
            labelOperationStatus.Text = "Tracked time less than 1 second is ignored";
         }

         onTimerStopped(dataCache?.TotalTimeCache);
      }

      private void cancelTimeTrackingTimer()
      {
         if (!isTrackingTime())
         {
            return;
         }

         // Stop timer
         _timeTrackingTimer.Stop();

         _timeTracker.Cancel();
         _timeTracker = null;
         _timeTrackingTabPage = null;
         labelOperationStatus.Text = "Time tracking cancelled";

         onTimerStopped(getDataCache(getCurrentTabDataCacheType())?.TotalTimeCache);
      }

      private void addCommentForSelectedMergeRequest()
      {
         BeginInvoke(new Action(async () =>
         {
            MergeRequest mergeRequest = getMergeRequest(null);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;

            bool res = await DiscussionHelper.AddCommentAsync(
               mrk, mergeRequest.Title, _modificationNotifier, getCurrentUser());
            labelOperationStatus.Text = res ? "Added a comment" : "Comment is not added";
         }));
      }

      private void newDiscussionForSelectedMergeRequest()
      {
         BeginInvoke(new Action(async () =>
         {
            MergeRequest mergeRequest = getMergeRequest(null);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;

            bool res = (await DiscussionHelper.AddThreadAsync(mrk, mergeRequest.Title, _modificationNotifier,
               getCurrentUser(), getDataCache(getCurrentTabDataCacheType()))) != null;
            labelOperationStatus.Text = res ? "Added a discussion thread" : "Discussion thread is not added";
         }));
      }

      async private Task createNewMergeRequestAsync(SubmitNewMergeRequestParameters parameters, string firstNote)
      {
         buttonCreateNew.Enabled = false;
         labelOperationStatus.Text = "Creating a merge request at GitLab...";

         GitLabInstance gitLabInstance = new GitLabInstance(parameters.ProjectKey.HostName, Program.Settings);
         MergeRequestKey? mrkOpt = await MergeRequestEditHelper.SubmitNewMergeRequestAsync(gitLabInstance,
            _modificationNotifier, parameters, firstNote, getCurrentUser());
         if (mrkOpt == null)
         {
            // all error handling is done at the callee side
            string message = "Merge Request has not been created";
            labelOperationStatus.Text = message;
            buttonCreateNew.Enabled = true;
            Trace.TraceInformation("[MainForm] {0}", message);
            return;
         }

         requestUpdates(EDataCacheType.Live, null, new int[] { NewOrClosedMergeRequestRefreshListTimerInterval });

         labelOperationStatus.Text = String.Format("Merge Request !{0} has been created in project {1}",
            mrkOpt.Value.IId, parameters.ProjectKey.ProjectName);
         buttonCreateNew.Enabled = true;

         _newMergeRequestDialogStatesByHosts[getHostName()] = new NewMergeRequestProperties(
            parameters.ProjectKey.ProjectName, null, null, parameters.AssigneeUserName, parameters.Squash,
            parameters.DeleteSourceBranch);
         saveState();

         Trace.TraceInformation(
            "[MainForm] Created a new merge request. " +
            "Project: {0}, SourceBranch: {1}, TargetBranch: {2}, Assignee: {3}, firstNote: {4}",
            parameters.ProjectKey.ProjectName, parameters.SourceBranch, parameters.TargetBranch,
            parameters.AssigneeUserName, firstNote);
      }

      async private Task applyChangesToMergeRequestAsync(DataCache dataCache, string hostname, User currentUser,
         FullMergeRequestKey item, IEnumerable<User> fullUserList)
      {
         MergeRequestKey mrk = new MergeRequestKey(item.ProjectKey, item.MergeRequest.IId);
         string noteText = await MergeRequestEditHelper.GetLatestSpecialNote(dataCache.DiscussionCache, mrk);
         MergeRequestPropertiesForm form = new EditMergeRequestPropertiesForm(hostname,
            getProjectAccessor(), currentUser, item.ProjectKey, item.MergeRequest, noteText, fullUserList);
         if (form.ShowDialog() != DialogResult.OK)
         {
            Trace.TraceInformation("[MainForm] User declined to modify a merge request");
            return;
         }

         ApplyMergeRequestChangesParameters parameters =
            new ApplyMergeRequestChangesParameters(form.Title, form.AssigneeUsername,
            form.Description, form.TargetBranch, form.DeleteSourceBranch, form.Squash);

         GitLabInstance gitLabInstance = new GitLabInstance(hostname, Program.Settings);
         bool modified = await MergeRequestEditHelper.ApplyChangesToMergeRequest(gitLabInstance, _modificationNotifier,
            item.ProjectKey, item.MergeRequest, parameters, noteText, form.SpecialNote, currentUser);

         string statusMessage = modified
            ? String.Format("Merge Request !{0} has been modified", mrk.IId)
            : String.Format("No changes have been made to Merge Request !{0}", mrk.IId);
         labelOperationStatus.Text = statusMessage;
         Trace.TraceInformation("[MainForm] {0}", statusMessage);

         if (modified)
         {
            requestUpdates(EDataCacheType.Live, mrk,
               new int[] {
                        100,
                        Program.Settings.OneShotUpdateFirstChanceDelayMs,
                        Program.Settings.OneShotUpdateSecondChanceDelayMs
               });
         }
      }

      async private Task closeMergeRequestAsync(string hostname, FullMergeRequestKey item)
      {
         MergeRequestKey mrk = new MergeRequestKey(item.ProjectKey, item.MergeRequest.IId);
         string message =
            "Do you really want to close (cancel) merge request? It will not be merged to the target branch.";
         if (MessageBox.Show(message, "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
         {
            GitLabInstance gitLabInstance = new GitLabInstance(hostname, Program.Settings);
            await MergeRequestEditHelper.CloseMergeRequest(gitLabInstance, _modificationNotifier, mrk);

            string statusMessage = String.Format("Merge Request !{0} has been closed", mrk.IId);
            labelOperationStatus.Text = statusMessage;
            Trace.TraceInformation("[MainForm] {0}", statusMessage);

            requestUpdates(EDataCacheType.Live, null, new int[] { NewOrClosedMergeRequestRefreshListTimerInterval });
         }
         else
         {
            Trace.TraceInformation("[MainForm] User declined to close a merge request");
         }
      }
   }
}

