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

         showWarningAboutIntegrationWithGitUI();

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
         requestUpdates(getDataCache(type), mrk, 100, () =>
            addOperationRecord(String.Format("Merge Request !{0} has been refreshed", mrk.IId)));
      }

      private void createNewMergeRequest(string hostname, User currentUser, NewMergeRequestProperties initialProperties,
         IEnumerable<Project> fullProjectList, IEnumerable<User> fullUserList)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         var sourceBranchesInUse = GitLabClient.Helpers.GetSourceBranchesByUser(getCurrentUser(), dataCache);

         MergeRequestPropertiesForm form = new NewMergeRequestForm(hostname,
            _shortcuts.GetProjectAccessor(), currentUser, initialProperties, fullProjectList, fullUserList,
            sourceBranchesInUse, _expressionResolver.Resolve(Program.ServiceManager.GetSourceBranchTemplate()));
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
               addOperationRecord(String.Format("Merge Request !{0} has been merged successfully", mrk.IId));
               requestUpdates(EDataCacheType.Live, null, new int[] { NewOrClosedMergeRequestRefreshListTimerInterval });
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
         Debug.Assert(dataCache == getDataCache(EDataCacheType.Live));
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

               updateTotalTime(mrk, dataCache);
               addOperationRecord("Total spent time has been updated");

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
         _timeTrackingMode = getCurrentTabDataCacheType();

         _timeTracker = _shortcuts.GetTimeTracker(getMergeRequestKey(null).Value);
         _timeTracker.Start();

         // Take care of controls that 'time tracking' mode shares with normal mode
         updateTotalTime(null, null);

         updateTrayIcon();
         updateTaskbarIcon();

         onTimerStarted();
         addOperationRecord("Time tracking has started");
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

         // Stop timer
         _timeTrackingTimer.Stop();

         // Reset member right now to not send tracked time again on re-entrance
         ITimeTracker timeTracker = _timeTracker;
         _timeTracker = null;
         _timeTrackingMode = null;

         // Stop stopwatch and send tracked time
         TimeSpan span = timeTracker.Elapsed;
         if (span.TotalSeconds > 1)
         {
            addOperationRecord("Sending tracked time has started");
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
            addOperationRecord(status);
         }
         else
         {
            addOperationRecord("Tracked time less than 1 second is ignored");
         }

         if (!isTrackingTime())
         {
            onTimerStopped();
         }
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
         _timeTrackingMode = null;
         addOperationRecord("Time tracking cancelled");

         onTimerStopped();
      }

      private void addCommentForSelectedMergeRequest()
      {
         BeginInvoke(new Action(async () =>
         {
            MergeRequest mergeRequest = getMergeRequest(null);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;

            bool res = await DiscussionHelper.AddCommentAsync(mrk, mergeRequest.Title, getCurrentUser(), _shortcuts);
            addOperationRecord(res ? "New comment has been added" : "Comment has not been added");
         }));
      }

      private void newDiscussionForSelectedMergeRequest()
      {
         BeginInvoke(new Action(async () =>
         {
            MergeRequest mergeRequest = getMergeRequest(null);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;

            bool res = (await DiscussionHelper.AddThreadAsync(mrk, mergeRequest.Title,
               getCurrentUser(), getDataCache(getCurrentTabDataCacheType()), _shortcuts)) != null;
            addOperationRecord(res ? "A new discussion thread has been added" : "Discussion thread has not been added");
         }));
      }

      async private Task createNewMergeRequestAsync(SubmitNewMergeRequestParameters parameters, string firstNote)
      {
         setMergeRequestEditEnabled(false);
         addOperationRecord("Creating a merge request at GitLab has started");

         MergeRequestKey? mrkOpt = await MergeRequestEditHelper.SubmitNewMergeRequestAsync(
            parameters, firstNote, getCurrentUser(), _shortcuts);
         if (mrkOpt == null)
         {
            // all error handling is done at the callee side
            string message = "Merge Request has not been created";
            addOperationRecord(message);
            setMergeRequestEditEnabled(true);
            return;
         }

         requestUpdates(EDataCacheType.Live, null, new int[] { NewOrClosedMergeRequestRefreshListTimerInterval });

         addOperationRecord(String.Format("Merge Request !{0} has been created in project {1}",
            mrkOpt.Value.IId, parameters.ProjectKey.ProjectName));
         setMergeRequestEditEnabled(true);

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
            _shortcuts.GetProjectAccessor(), currentUser, item.ProjectKey, item.MergeRequest, noteText, fullUserList);
         if (form.ShowDialog() != DialogResult.OK)
         {
            Trace.TraceInformation("[MainForm] User declined to modify a merge request");
            return;
         }

         ApplyMergeRequestChangesParameters parameters =
            new ApplyMergeRequestChangesParameters(form.Title, form.AssigneeUsername,
            form.Description, form.TargetBranch, form.DeleteSourceBranch, form.Squash);

         bool modified = await MergeRequestEditHelper.ApplyChangesToMergeRequest(
            item.ProjectKey, item.MergeRequest, parameters, noteText, form.SpecialNote, currentUser,
            _shortcuts);

         string statusMessage = modified
            ? String.Format("Merge Request !{0} has been modified", mrk.IId)
            : String.Format("No changes have been made to Merge Request !{0}", mrk.IId);
         addOperationRecord(statusMessage);

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

            requestUpdates(EDataCacheType.Live, null, new int[] { NewOrClosedMergeRequestRefreshListTimerInterval });
         }
         else
         {
            Trace.TraceInformation("[MainForm] User declined to close a merge request");
         }
      }
   }
}

