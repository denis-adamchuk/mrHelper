using System;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.GitLabClient;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Controls;
using SearchQuery = mrHelper.GitLabClient.SearchQuery;
using Newtonsoft.Json.Linq;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      public string GetCurrentHostName()
      {
         return getHostName();
      }

      public string GetCurrentAccessToken()
      {
         return Program.Settings.GetAccessToken(getHostName());
      }

      public string GetCurrentProjectName()
      {
         return getMergeRequestKey(null)?.ProjectKey.ProjectName ?? String.Empty;
      }

      public int GetCurrentMergeRequestIId()
      {
         return getMergeRequestKey(null)?.IId ?? 0;
      }

      private User getCurrentUser()
      {
         return getCurrentUser(getHostName());
      }

      private User getCurrentUser(string hostname)
      {
         bool isValidHostname = !String.IsNullOrWhiteSpace(hostname);
         return isValidHostname && _currentUser.TryGetValue(hostname, out User value) ? value : null;
      }

      private enum EDataCacheType
      {
         Live,
         Search,
         Recent
      }

      private EDataCacheType getCurrentTabDataCacheType()
      {
         if (tabControlMode.SelectedTab == tabPageSearch)
         {
            return EDataCacheType.Search;
         }
         else if (tabControlMode.SelectedTab == tabPageRecent)
         {
            return EDataCacheType.Recent;
         }

         Debug.Assert(tabControlMode.SelectedTab == tabPageLive);
         return EDataCacheType.Live;
      }

      private EDataCacheType getListViewType(MergeRequestListView listView)
      {
         if (listView == listViewLiveMergeRequests)
         {
            return EDataCacheType.Live;
         }
         else if (listView == listViewFoundMergeRequests)
         {
            return EDataCacheType.Search;
         }
         else if (listView == listViewRecentMergeRequests)
         {
            return EDataCacheType.Recent;
         }

         Debug.Assert(false);
         return EDataCacheType.Live;
      }

      private MergeRequestListView getListView(EDataCacheType mode)
      {
         switch (mode)
         {
            case EDataCacheType.Live:
               return listViewLiveMergeRequests;

            case EDataCacheType.Search:
               return listViewFoundMergeRequests;

            case EDataCacheType.Recent:
               return listViewRecentMergeRequests;
         }

         Debug.Assert(false);
         return null;
      }

      private MergeRequest getMergeRequest(MergeRequestListView proposedListView)
      {
         MergeRequestListView listView = proposedListView ?? getListView(getCurrentTabDataCacheType());
         FullMergeRequestKey? fmk = listView.GetSelectedMergeRequest();
         return fmk.HasValue ? fmk.Value.MergeRequest : null;
      }

      private MergeRequestKey? getMergeRequestKey(MergeRequestListView proposedListView)
      {
         MergeRequestListView listView = proposedListView ?? getListView(getCurrentTabDataCacheType());
         FullMergeRequestKey? fmk = listView.GetSelectedMergeRequest();
         return fmk.HasValue && fmk.Value.MergeRequest != null
            ? new MergeRequestKey(fmk.Value.ProjectKey, fmk.Value.MergeRequest.IId)
            : new Nullable<MergeRequestKey>();
      }

      private string getDefaultProjectName()
      {
         MergeRequestListView listView = getListView(EDataCacheType.Live);
         MergeRequestKey? currentMergeRequestKey = getMergeRequestKey(listView);
         if (currentMergeRequestKey.HasValue)
         {
            return currentMergeRequestKey.Value.ProjectKey.ProjectName;
         }

         if (listView.Groups.Count > 0)
         {
            return listView.Groups[0].Name;
         }

         ProjectKey? project = getDataCache(EDataCacheType.Live)?.MergeRequestCache?.GetProjects()?.FirstOrDefault();
         if (project.HasValue)
         {
            return project.Value.ProjectName;
         }

         return String.Empty;
      }

      private DataCache getDataCache(EDataCacheType mode)
      {
         switch (mode)
         {
            case EDataCacheType.Live:
               return _liveDataCache;

            case EDataCacheType.Search:
               return _searchDataCache;

            case EDataCacheType.Recent:
               return _recentDataCache;
         }

         Debug.Assert(false);
         return null;
      }

      private DataCache getDataCacheByName(string name)
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            if (name == mode.ToString())
            {
               return getDataCache(mode);
            }
         }
         Debug.Assert(false);
         return null;
      }

      private string getDataCacheName(DataCache dataCache)
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            if (getDataCache(mode) == dataCache)
            {
               return mode.ToString();
            }
         }
         Debug.Assert(false);
         return String.Empty;
      }

      private enum PreferredSelection
      {
         Initial,
         Latest
      }

      private bool isCustomActionEnabled(IEnumerable<string> labels, User author, string dependency)
      {
         if (String.IsNullOrEmpty(dependency))
         {
            return true;
         }

         if (labels.Any(x => StringUtils.DoesMatchPattern(dependency, "{{Label:{0}}}", x)))
         {
            return true;
         }

         if (StringUtils.DoesMatchPattern(dependency, "{{Author:{0}}}", author.Username))
         {
            return true;
         }

         return false;
      }

      private ProjectAccessor getProjectAccessor()
      {
         if (getHostName() == String.Empty)
         {
            Debug.Assert(false);
            return null;
         }

         GitLabInstance gitLabInstance = new GitLabInstance(getHostName(), Program.Settings);
         RawDataAccessor rawDataAccessor = new RawDataAccessor(gitLabInstance);
         return rawDataAccessor.GetProjectAccessor(_modificationNotifier);
      }

      private bool isTrackingTime()
      {
         return _timeTracker != null;
      }

      private bool notifyAboutNewVersion()
      {
         Debug.Assert(StaticUpdateChecker.NewVersionInformation != null);
         if (isTrackingTime())
         {
            _applicationUpdateReminderPostponedTillTimerStop = false;
            _applicationUpdateNotificationPostponedTillTimerStop = true;
            Trace.TraceInformation("[MainForm] New version appeared during time tracking");
         }
         else if (ApplicationUpdateHelper.ShowCheckForUpdatesDialog())
         {
            doCloseOnUpgrade();
            return true;
         }
         return false;
      }

      private bool remindAboutNewVersion()
      {
         Trace.TraceInformation("[MainForm] Reminder timer triggered (or re-triggered after timer stop)");
         if (StaticUpdateChecker.NewVersionInformation != null && Program.Settings.RemindAboutAvailableNewVersion)
         {
            if (isTrackingTime())
            {
               _applicationUpdateReminderPostponedTillTimerStop = true;
               _applicationUpdateNotificationPostponedTillTimerStop = false;
               Trace.TraceInformation("[MainForm] Reminder triggered during time tracking");
            }
            else if (ApplicationUpdateHelper.RemindAboutAvailableVersion())
            {
               doCloseOnUpgrade();
               return true;
            }
         }
         return false;
      }

      private void upgradeApplicationByUserRequest()
      {
         if (StaticUpdateChecker.NewVersionInformation == null)
         {
            Debug.Assert(false); // Should not UI control be disabled now?..
            return;
         }

         Trace.TraceInformation("[MainForm] User clicked at new version label in UI");
         if (ApplicationUpdateHelper.InstallUpdate(StaticUpdateChecker.NewVersionInformation.InstallerFilePath))
         {
            doCloseOnUpgrade();
         }
         else
         {
            Trace.TraceInformation("[MainForm] User discarded to install a new version");
         }
      }

      private void changeProjectEnabledState(ProjectKey projectKey, bool state)
      {
         Dictionary<string, bool> projects = ConfigurationHelper.GetProjectsForHost(
            projectKey.HostName, Program.Settings).ToDictionary(item => item.Item1, item => item.Item2);
         Debug.Assert(projects.ContainsKey(projectKey.ProjectName));
         projects[projectKey.ProjectName] = state;

         ConfigurationHelper.SetProjectsForHost(projectKey.HostName,
            Enumerable.Zip(projects.Keys, projects.Values, (x, y) => new Tuple<string, bool>(x, y)), Program.Settings);
         updateProjectsListView();
      }

      private void getShaForDiffTool(out string left, out string right,
         out IEnumerable<string> included, out RevisionType? type)
      {
         string[] selected = revisionBrowser.GetSelectedSha(out type);
         switch (selected.Count())
         {
            case 0:
               left = String.Empty;
               right = String.Empty;
               included = new List<string>();
               break;

            case 1:
               left = revisionBrowser.GetBaseCommitSha();
               right = selected[0];
               included = revisionBrowser.GetIncludedSha();
               break;

            case 2:
               left = selected[0];
               right = selected[1];
               included = revisionBrowser.GetIncludedSha();
               break;

            default:
               Debug.Assert(false);
               left = String.Empty;
               right = String.Empty;
               included = new List<string>();
               break;
         }
      }

      private bool checkIfMergeRequestCanBeCreated()
      {
         string hostname = getHostName();
         User currentUser = getCurrentUser();
         if (hostname == String.Empty || currentUser == null || _expressionResolver == null)
         {
            Debug.Assert(false);
            MessageBox.Show("Cannot create a merge request", "Internal error",
               MessageBoxButtons.OK, MessageBoxIcon.Error);
            Trace.TraceError("Unexpected application state." +
               "hostname is empty string={0}, currentUser is null={1}, _expressionResolver is null={2}",
               hostname == String.Empty, currentUser == null, _expressionResolver == null);
            return false;
         }
         return true;
      }

      private bool checkIfMergeRequestCanBeEdited()
      {
         string hostname = getHostName();
         User currentUser = getCurrentUser();
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

      private NewMergeRequestProperties getDefaultNewMergeRequestProperties(string hostname,
         User currentUser, string projectName)
      {
         // This state is expected to be used only once, next times 'persistent storage' is used
         NewMergeRequestProperties factoryProperties = new NewMergeRequestProperties(
            projectName, null, null, currentUser.Username, true, true);
         return _newMergeRequestDialogStatesByHosts.TryGetValue(hostname, out var value) ? value : factoryProperties;
      }

      private bool doesClipboardContainValidUrl()
      {
         return UrlHelper.CheckMergeRequestUrl(getClipboardText());
      }

      private string getClipboardText()
      {
         try
         {
            return Clipboard.GetText();
         }
         catch (Exception ex)
         {
            Debug.Assert(ex is System.Runtime.InteropServices.ExternalException);
            return String.Empty;
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

      private void onStartSearch()
      {
         SearchQuery query = new SearchQuery
         {
            MaxResults = Constants.MaxSearchResults
         };

         if (checkBoxSearchByTargetBranch.Checked && !String.IsNullOrWhiteSpace(textBoxSearchTargetBranch.Text))
         {
            query.TargetBranchName = textBoxSearchTargetBranch.Text;
         }
         if (checkBoxSearchByTitleAndDescription.Checked && !String.IsNullOrWhiteSpace(textBoxSearchText.Text))
         {
            query.Text = textBoxSearchText.Text;
         }
         if (checkBoxSearchByProject.Checked && comboBoxProjectName.SelectedItem != null)
         {
            query.ProjectName = comboBoxProjectName.Text;
         }
         if (checkBoxSearchByAuthor.Checked && comboBoxUser.SelectedItem != null)
         {
            query.AuthorUserName = (comboBoxUser.SelectedItem as User).Username;
         }

         string stateToSearch = comboBoxSearchByState.SelectedItem.ToString();
         bool unspecifiedState = stateToSearch == "any";
         query.State = unspecifiedState ? null : stateToSearch;

         Debug.Assert(query != null);
         searchMergeRequests(new SearchQueryCollection(query));
      }

      private void doClose()
      {
         setExitingFlag();
         Close();
      }

      private void doCloseOnUpgrade()
      {
         Trace.TraceInformation("[MainForm] Application is exiting to install a new version...");
         doClose();
      }

      private void setExitingFlag()
      {
         Trace.TraceInformation(String.Format("[MainForm] Set _exiting flag"));
         _exiting = true;
      }

      private void onHideToTray()
      {
         if (Program.Settings.ShowWarningOnHideToTray)
         {
            _trayIcon.ShowTooltipBalloon(new TrayIcon.BalloonText("Information", "I will now live in your tray"));
            Program.Settings.ShowWarningOnHideToTray = false;
         }
         Hide();
      }

      private void onPersistentStorageSerialize(IPersistentStateSetter writer)
      {
         writer.Set("SelectedHost", getHostName());

         Dictionary<string, HashSet<string>> reviewedRevisions = _reviewedRevisions.ToDictionary(
               item => item.Key.ProjectKey.HostName
               + "|" + item.Key.ProjectKey.ProjectName
               + "|" + item.Key.IId.ToString(),
               item => item.Value);
         writer.Set("ReviewedCommits", reviewedRevisions);

         Dictionary<string, string> recentMergeRequests = _recentMergeRequests.ToDictionary(
               item => item.Key.ProjectKey.HostName
               + "|" + item.Key.ProjectKey.ProjectName
               + "|" + item.Key.IId.ToString(),
               item => item.Value.ToLongTimeString());
         writer.Set("RecentMergeRequestsWithDateTime", recentMergeRequests);

         Dictionary<string, string> mergeRequestsByHosts = _lastMergeRequestsByHosts.ToDictionary(
               item => item.Value.ProjectKey.HostName
               + "|" + item.Value.ProjectKey.ProjectName,
               item => item.Value.IId.ToString());
         writer.Set("MergeRequestsByHosts", mergeRequestsByHosts);

         Dictionary<string, string> newMergeRequestDialogStatesByHosts =
            _newMergeRequestDialogStatesByHosts.ToDictionary(
               item => item.Key,
               item => item.Value.DefaultProject
               + "|" + item.Value.AssigneeUsername
               + "|" + item.Value.IsBranchDeletionNeeded.ToString()
               + "|" + item.Value.IsSquashNeeded.ToString());
         writer.Set("NewMergeRequestDialogStatesByHosts", newMergeRequestDialogStatesByHosts);
      }

      private void onPersistentStorageDeserialize(IPersistentStateGetter reader)
      {
         string hostname = (string)reader.Get("SelectedHost");
         if (hostname != null)
         {
            setInitialHostName(StringUtils.GetHostWithPrefix(hostname));
         }

         JObject reviewedRevisionsObj = (JObject)reader.Get("ReviewedCommits");
         Dictionary<string, object> reviewedRevisions =
            reviewedRevisionsObj?.ToObject<Dictionary<string, object>>();
         if (reviewedRevisions != null)
         {
            _reviewedRevisions = reviewedRevisions.ToDictionary(
               item =>
               {
                  string[] splitted = item.Key.Split('|');

                  Debug.Assert(splitted.Length == 3);

                  string host = splitted[0];
                  string projectName = splitted[1];
                  int iid = int.Parse(splitted[2]);
                  return new MergeRequestKey(new ProjectKey(host, projectName), iid);
               },
               item =>
               {
                  HashSet<string> commits = new HashSet<string>();
                  foreach (string commit in (JArray)item.Value)
                  {
                     commits.Add(commit);
                  }
                  return commits;
               });
         }

         DateTime defaultDateTimeForRecentMergeRequests = DateTime.Now;

         JArray recentMergeRequests = (JArray)reader.Get("RecentMergeRequests");
         if (recentMergeRequests != null)
         {
            foreach (JToken token in recentMergeRequests)
            {
               if (token.Type == JTokenType.String)
               {
                  string item = token.Value<string>();
                  string[] splitted = item.Split('|');
                  Debug.Assert(splitted.Length == 3);

                  string hostname2 = StringUtils.GetHostWithPrefix(splitted[0]);
                  string projectname = splitted[1];
                  int iid = int.TryParse(splitted[2], out int parsedIId) ? parsedIId : 0;
                  _recentMergeRequests.Add(new MergeRequestKey(new ProjectKey(hostname2, projectname), iid),
                     defaultDateTimeForRecentMergeRequests);
               }
            }
         }

         JObject recentMergeRequestsWithDateTimeObj = (JObject)reader.Get("RecentMergeRequestsWithDateTime");
         Dictionary<string, object> recentMergeRequestsWithDateTime =
            recentMergeRequestsWithDateTimeObj?.ToObject<Dictionary<string, object>>();
         if (recentMergeRequestsWithDateTime != null)
         {
            _recentMergeRequests = recentMergeRequestsWithDateTime.ToDictionary(
               item =>
               {
                  string[] splitted = item.Key.Split('|');

                  Debug.Assert(splitted.Length == 3);

                  string host = splitted[0];
                  string projectName = splitted[1];
                  int iid = int.Parse(splitted[2]);
                  return new MergeRequestKey(new ProjectKey(host, projectName), iid);
               },
               item => DateTime.TryParse(item.Value.ToString(), out DateTime dt)
                  ? dt : defaultDateTimeForRecentMergeRequests);
         }

         JObject lastMergeRequestsByHostsObj = (JObject)reader.Get("MergeRequestsByHosts");
         Dictionary<string, object> lastMergeRequestsByHosts =
            lastMergeRequestsByHostsObj?.ToObject<Dictionary<string, object>>();
         if (lastMergeRequestsByHosts != null)
         {
            _lastMergeRequestsByHosts = lastMergeRequestsByHosts.ToDictionary(
               item =>
               {
                  string[] splitted = item.Key.Split('|');
                  Debug.Assert(splitted.Length == 2);
                  return splitted[0];
               },
               item =>
               {
                  string[] splitted = item.Key.Split('|');
                  Debug.Assert(splitted.Length == 2);

                  string hostname2 = StringUtils.GetHostWithPrefix(splitted[0]);
                  string projectname = splitted[1];
                  int iid = int.Parse((string)item.Value);
                  return new MergeRequestKey(new ProjectKey(hostname2, projectname), iid);
               });
         }

         JObject newMergeRequestDialogStatesByHostsObj = (JObject)reader.Get("NewMergeRequestDialogStatesByHosts");
         Dictionary<string, object> newMergeRequestDialogStatesByHosts =
            newMergeRequestDialogStatesByHostsObj?.ToObject<Dictionary<string, object>>();
         if (newMergeRequestDialogStatesByHosts != null)
         {
            _newMergeRequestDialogStatesByHosts = newMergeRequestDialogStatesByHosts
               .Where(x => ((string)x.Value).Split('|').Length == 4).ToDictionary(
               item => item.Key,
               item =>
               {
                  string[] splitted = ((string)item.Value).Split('|');
                  return new NewMergeRequestProperties(
                     splitted[0], null, null, splitted[1],
                     splitted[2] == bool.TrueString, splitted[3] == bool.TrueString);
               });
         }
      }

      private void onTimeTrackingTimer(object sender, EventArgs e)
      {
         if (isTrackingTime())
         {
            updateTotalTime(null, null, null, null);
         }
      }

      private void connectToUrlFromClipboard()
      {
         if (doesClipboardContainValidUrl())
         {
            string url = getClipboardText();
            Trace.TraceInformation(String.Format("[Mainform] Connecting to URL from clipboard: {0}", url.ToString()));
            reconnect(url);
         }
      }

      private void onClipboardCheckingTimer(object sender, EventArgs e)
      {
         bool isValidUrl = doesClipboardContainValidUrl();
         linkLabelFromClipboard.Enabled = isValidUrl;
         linkLabelFromClipboard.Text = isValidUrl ? openFromClipboardEnabledText : openFromClipboardDisabledText;

         string tooltip = isValidUrl ? getClipboardText() : "N/A";
         toolTip.SetToolTip(linkLabelFromClipboard, tooltip);
      }

      private static void sendFeedback()
      {
         try
         {
            if (Program.ServiceManager.GetBugReportEmail() != String.Empty)
            {
               Program.FeedbackReporter.SendEMail("Merge Request Helper Feedback Report",
                  "Please provide your feedback here", Program.ServiceManager.GetBugReportEmail(),
                  Constants.BugReportLogArchiveName);
            }
         }
         catch (FeedbackReporterException ex)
         {
            string message = "Cannot send feedback";
            ExceptionHandlers.Handle(message, ex);
            MessageBox.Show(ex.InnerException?.Message ?? "Unknown error", message,
               MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private static void showHelp()
      {
         string helpUrl = Program.ServiceManager.GetHelpUrl();
         if (helpUrl != String.Empty)
         {
            UrlHelper.OpenBrowser(helpUrl);
         }
      }

      private void applyHostChange(string hostname)
      {
         Trace.TraceInformation(String.Format("[MainForm] User requested to change host to {0}", hostname));
         updateProjectsListView();
         updateUsersListView();
         reconnect();
         saveState();
      }

      private void reconnect(string url = null)
      {
         addOperationRecord("Reconnection request has been queued");
         enqueueUrl(url);
      }

      private void requestUpdates(EDataCacheType mode, MergeRequestKey? mrk, int[] intervals)
      {
         DataCache dataCache = getDataCache(mode);
         dataCache?.MergeRequestCache?.RequestUpdate(mrk, intervals);
         dataCache?.DiscussionCache?.RequestUpdate(mrk, intervals);
      }

      private static void showWarningOnReloadList()
      {
         if (Program.Settings.ShowWarningOnReloadList)
         {
            int autoUpdateMs = Program.Settings.AutoUpdatePeriodMs;
            double oneMinuteMs = 60000;
            double autoUpdateMinutes = autoUpdateMs / oneMinuteMs;

            string periodicity = autoUpdateMs > oneMinuteMs
               ? (autoUpdateMs % Convert.ToInt32(oneMinuteMs) == 0
                  ? String.Format("{0} minutes", autoUpdateMinutes)
                  : String.Format("{0:F1} minutes", autoUpdateMinutes))
               : String.Format("{0} seconds", autoUpdateMs / 1000);

            string message = String.Format(
               "Merge Request list updates each {0} and you don't usually need to update it manually", periodicity);
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Program.Settings.ShowWarningOnReloadList = false;
         }
      }

      private void showWarningAboutIntegrationWithGitUI()
      {
         if (Program.Settings.ShowWarningOnCreateMergeRequest && (_integratedInGitExtensions || _integratedInSourceTree))
         {
            string tools = "Git Extensions or Source Tree";
            if (_integratedInSourceTree && !_integratedInGitExtensions)
            {
               tools = "Source Tree";
            }
            else if (_integratedInGitExtensions && !_integratedInSourceTree)
            {
               tools = "Git Extensions";
            }

            string message = String.Format(
               "Note: It is much easier to create a new merge request using integration of mrHelper with {0}. " +
               "Just select a commit of a remote branch in {0} and press Create Merge Request in menu.", tools);
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Program.Settings.ShowWarningOnCreateMergeRequest = false;
         }
      }

      private void onWorkstationLocked()
      {
         if (isTrackingTime())
         {
            stopTimeTrackingTimer();
            MessageBox.Show("mrHelper stopped time tracking because workstation was locked", "Warning",
               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Trace.TraceInformation("[MainForm] Time tracking stopped because workstation was locked");
         }
      }

      private void updateNewVersionStatus()
      {
         linkLabelNewVersion.Visible = StaticUpdateChecker.NewVersionInformation != null;
         updateCaption();
      }

      private void onNewVersionAvailable()
      {
         Debug.Assert(StaticUpdateChecker.NewVersionInformation != null);
         updateNewVersionStatus();

         // when a new version appears in the middle of work, re-schedule a reminder to trigger in 24 hours
         stopNewVersionReminderTimer();
         if (!notifyAboutNewVersion())
         {
            Trace.TraceInformation("[MainForm] Reminder timer restarted");
            startNewVersionReminderTimer();
         }
      }

      private void addOperationRecord(string text)
      {
         string textWithTimestamp = String.Format("{0} {1}",
            DateTime.Now.ToLocalTime().ToString(Constants.TimeStampFormat), text);
         _operationRecordHistory.Add(textWithTimestamp);
         if (_operationRecordHistory.Count() > OperationRecordHistoryDepth)
         {
            _operationRecordHistory.RemoveAt(0);
         }

         labelOperationStatus.Text = text;
         Trace.TraceInformation("[MainForm] {0}", text);

         StringBuilder builder = new StringBuilder(OperationRecordHistoryDepth);
         foreach (string record in _operationRecordHistory)
         {
            builder.AppendLine(record);
         }
         toolTip.SetToolTip(labelOperationStatus, builder.ToString());
      }
   }
}

