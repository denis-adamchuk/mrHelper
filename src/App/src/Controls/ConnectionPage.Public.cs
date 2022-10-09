using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Helpers;
using mrHelper.App.Interprocess;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.CustomActions;
using mrHelper.GitLabClient;
using mrHelper.StorageSupport;

namespace mrHelper.App.Controls
{
   internal enum DiffToolMode
   {
      DiffBetweenSelected,
      DiffSelectedToBase,
      DiffSelectedToParent,
      DiffLatestToBase
   }

   internal partial class ConnectionPage : IOperationController
   {
      internal void Activate()
      {
         _isActivePage = true;
         switchMode(EDataCacheType.Live);
         //readFilterFromConfig();
      }

      internal void Deactivate()
      {
         _isActivePage = false;
         switchMode(EDataCacheType.Live); // Switch tab on a page being deactivated back to the default one
      }

      internal Task Connect(Func<Exception, bool> exceptionHandler)
      {
         return connect(exceptionHandler);
      }

      internal Task ConnectToUrl<T>(string url, T parsedUrl)
      {
         return connectToUrlAsyncInternal(url, parsedUrl);
      }

      internal string GetSourceBranchTemplate()
      {
         return getSourceBranchTemplate();
      }

      internal void CreateNew()
      {
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
            Trace.TraceError("[ConnectionPage] Project List is not ready at the moment of Create New click");
            return;
         }

         IEnumerable<User> fullUserList = dataCache?.UserCache?.GetUsers();
         bool isUserListReady = fullUserList?.Any() ?? false;
         if (!isUserListReady)
         {
            Debug.Assert(false);
            Trace.TraceError("[ConnectionPage] User List is not ready at the moment of Create New click");
            return;
         }

         string projectName = getDefaultProjectName();
         NewMergeRequestProperties initialFormState = getDefaultNewMergeRequestProperties(
            HostName, CurrentUser, projectName);
         bool showIntegrationHint = _integratedInGitExtensions || _integratedInSourceTree;
         createNewMergeRequest(HostName, CurrentUser, initialFormState, fullProjectList, fullUserList,
            showIntegrationHint);
      }

      internal void CreateFromUrl(ParsedNewMergeRequestUrl parsedNewMergeRequestUrl)
      {
         createMergeRequestFromUrl(parsedNewMergeRequestUrl);
      }

      internal void GoLive()
      {
         selectTab(EDataCacheType.Live);

         string hostname = GetCurrentHostName();
         bool shouldUseLastSelection = _lastMergeRequestsByHosts.Data.ContainsKey(hostname);
         string projectname = shouldUseLastSelection ?
            _lastMergeRequestsByHosts[hostname].ProjectKey.ProjectName : String.Empty;
         int iid = shouldUseLastSelection ? _lastMergeRequestsByHosts[hostname].IId : 0;

         MergeRequestKey mrk = new MergeRequestKey(new ProjectKey(hostname, projectname), iid);
         getListView(EDataCacheType.Live).SelectMergeRequest(mrk, false);
      }

      internal void GoRecent()
      {
         selectTab(EDataCacheType.Recent);
      }

      internal void GoSearch()
      {
         selectTab(EDataCacheType.Search);
      }

      internal CommandState IsCommandEnabledForSelectedMergeRequest(ICommand command)
      {
         MergeRequestKey? mrk = getMergeRequestKey(null);
         if (!mrk.HasValue)
         {
            return new CommandState(false, false);
         }
         CommandState? commandStateOpt = isCommandEnabledInDiscussionsView(
            getCurrentTabDataCacheType(), mrk.Value, command);
         return commandStateOpt ?? new CommandState(false, false);
      }

      private CommandState? isCommandEnabledInDiscussionsView(EDataCacheType mode, MergeRequestKey mrk, ICommand command)
      {
         DataCache dataCache = getDataCache(mode);
         MergeRequest mergeRequest = dataCache?.MergeRequestCache?.GetMergeRequest(mrk);
         if (mergeRequest == null)
         {
            return null;
         }

         User author = mergeRequest.Author;
         IEnumerable<string> labels = mergeRequest.Labels;
         IEnumerable<User> approvedBy = dataCache.MergeRequestCache.GetApprovals(mrk)?.Approved_By?
            .Select(item => item.User) ?? Array.Empty<User>();
         if (author == null || labels == null || approvedBy == null || _expressionResolver == null)
         {
            Debug.Assert(false);
            return null;
         }

         IEnumerable<string> resolveCollection(IEnumerable<string> coll) =>
            coll.Select(item => String.IsNullOrEmpty(item) ? String.Empty : _expressionResolver.Resolve(item));

         IEnumerable<string> resolvedVisibleIf = resolveCollection(command.VisibleIf.Split(','));
         var isVisible = GitLabClient.Helpers.CheckConditions(resolvedVisibleIf, approvedBy, labels, author, false, false);

         IEnumerable<string> resolvedEnabledIf = resolveCollection(command.EnabledIf.Split(','));
         var isEnabled = GitLabClient.Helpers.CheckConditions(resolvedEnabledIf, approvedBy, labels, author, false, false);

         return new CommandState(isEnabled, isVisible);
      }

      internal bool AreCommandsEnabled()
      {
         MergeRequestKey? mrk = getMergeRequestKey(null);
         if (!mrk.HasValue)
         {
            return false;
         }

         DataCache dataCache = getDataCache(getCurrentTabDataCacheType());
         User author = dataCache?.MergeRequestCache?.GetMergeRequest(mrk.Value)?.Author;
         IEnumerable<string> labels = dataCache?.MergeRequestCache?.GetMergeRequest(mrk.Value)?.Labels;
         IEnumerable<User> approvedBy = dataCache?.MergeRequestCache?.GetApprovals(mrk.Value)?.Approved_By?
            .Select(item => item.User) ?? Array.Empty<User>();
         if (author == null || labels == null || approvedBy == null || _expressionResolver == null)
         {
            Debug.Assert(false);
            return false;
         }

         return true;
      }

      internal IEnumerable<ICommand> GetCustomActionList()
      {
         if (!_isApprovalStatusSupported.HasValue)
         {
            return null;
         }

         string filename = _isApprovalStatusSupported.Value
            ? Constants.CustomActionsWithApprovalStatusSupportFileName
            : Constants.CustomActionsFileName;
         var filepath = Path.Combine(Directory.GetCurrentDirectory(), filename);
         return CommandLoadHelper.LoadSafe(filepath);
      }

      internal void DiffTool(DiffToolMode mode)
      {
         launchDiffTool(mode);
      }

      internal void Discussions()
      {
         showDiscussionsForSelectedMergeRequest();
      }

      internal void ReloadLive()
      {
         reloadMergeRequestsByUserRequest(getDataCache(EDataCacheType.Live));
      }

      internal void ReloadSelected()
      {
         refreshSelectedMergeRequest();
      }

      internal void ReloadOne(MergeRequestKey mrk)
      {
         // In general case MR can belong to multiple DataCache at once
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            requestUpdates(mode, mrk, new int[] {
               Program.Settings.OneShotUpdateFirstChanceDelayMs,
               Program.Settings.OneShotUpdateSecondChanceDelayMs });
         }
      }

      internal void ReloadAllOnConnectionRestore()
      {
         ReloadLive();
         reloadMergeRequestsByUserRequest(getDataCache(EDataCacheType.Recent));
      }

      internal void AddComment()
      {
         addCommentForSelectedMergeRequest();
      }

      internal void NewThread()
      {
         newDiscussionForSelectedMergeRequest();
      }

      internal void AbortClone()
      {
         onAbortGitByUserRequest();
      }

      internal void EditTime()
      {
         editTimeOfSelectedMergeRequest();
      }

      internal void FindMergeRequest(MergeRequestKey mrk)
      {
         // We want to check lists in specific order:
         EDataCacheType[] modes = new EDataCacheType[]
         {
            EDataCacheType.Live,
            EDataCacheType.Recent,
            EDataCacheType.Search
         };
         foreach (EDataCacheType mode in modes)
         {
            if (switchTabAndSelectMergeRequest(mode, mrk))
            {
               break;
            }
         }
      }

      internal ITimeTracker CreateTimeTracker()
      {
         MergeRequestKey? mrkOpt = getMergeRequestKey(null);
         if (!mrkOpt.HasValue)
         {
            Debug.Assert(false);
            return null;
         }
         return _shortcuts.GetTimeTracker(mrkOpt.Value);
      }

      internal string GetTrackedTimeAsText()
      {
         DataCache dataCache = getDataCache(getCurrentTabDataCacheType());
         ITotalTimeCache totalTimeCache = dataCache?.TotalTimeCache;
         IMergeRequestCache mergeRequestCache = dataCache?.MergeRequestCache;
         MergeRequestKey? mrkOpt = getMergeRequestKey(null);
         if (mergeRequestCache == null || totalTimeCache == null || !mrkOpt.HasValue)
         {
            return String.Empty;
         }

         MergeRequestKey mrk = mrkOpt.Value;
         User author = mergeRequestCache.GetMergeRequest(mrk)?.Author;
         bool isTimeTrackingAllowed = TimeTrackingHelpers.IsTimeTrackingAllowed(
            author, mrk.ProjectKey.HostName, CurrentUser);
         return TimeTrackingHelpers.ConvertTotalTimeToText(totalTimeCache.GetTotalTime(mrk), isTimeTrackingAllowed);
      }

      internal enum EConnectionState
      {
         NotConnected,
         Connecting,
         Connected,
         ConnectionLost
      }

      internal EConnectionState GetConnectionState(out string details)
      {
         details = String.Empty;
         if (isConnectionLost())
         {
            details = String.Format("Connection was lost at {0}",
               TimeUtils.DateTimeToString(_lostConnectionInfo.Value.TimeStamp));
            return EConnectionState.ConnectionLost;
         }
         else if (!_connectionStatus.HasValue)
         {
            return EConnectionState.NotConnected;
         }
         else if (_connectionStatus == EConnectionStateInternal.Connected)
         {
            return EConnectionState.Connected;
         }
         else if (_connectionStatus == EConnectionStateInternal.ConnectingLive
               || _connectionStatus == EConnectionStateInternal.ConnectingRecent)
         {
            return EConnectionState.Connecting;
         }
         Debug.Assert(false);
         return EConnectionState.NotConnected;
      }

      internal string GetStorageStatus()
      {
         MergeRequestKey? mrk = getMergeRequestKey(null);
         if (mrk.HasValue && _latestStorageUpdateStatus.TryGetValue(mrk.Value, out string latestStatus))
         {
            return latestStatus;
         }
         return String.Empty;
      }

      internal DateTime? GetLatestListRefreshTimestamp()
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         return dataCache?.MergeRequestCache?.GetListRefreshTime();
      }

      internal void ProcessDiffToolRequest(Snapshot snapshot, string[] diffArguments)
      {
         var storageFactory = getCommitStorageFactory(false);
         if (storageFactory == null || storageFactory.ParentFolder != snapshot.TempFolder)
         {
            Trace.TraceWarning("[ConnectionPage] File Storage folder was changed after launching diff tool");
            MessageBox.Show("It seems that file storage folder was changed after launching diff tool. " +
               "Please restart diff tool.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return;
         }

         Core.Matching.MatchInfo matchInfo;
         try
         {
            DiffArgumentParser diffArgumentParser = new DiffArgumentParser(diffArguments);
            matchInfo = diffArgumentParser.Parse(getDiffTempFolder(snapshot));
            Debug.Assert(matchInfo != null);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot parse diff tool arguments", ex);
            MessageBox.Show("Bad arguments passed from diff tool", "Cannot create a discussion",
               MessageBoxButtons.OK, MessageBoxIcon.Error,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return;
         }

         ProjectKey projectKey = new ProjectKey(snapshot.Host, snapshot.Project);
         ILocalCommitStorage storage = getCommitStorage(projectKey, false);
         if (storage.Git == null)
         {
            Trace.TraceError("[ConnectionPage] storage.Git is null");
            Debug.Assert(false);
            return;
         }

         DataCache dataCache = getDataCacheByName(snapshot.DataCacheName);
         if (dataCache == null || CurrentUser == null)
         {
            // It is unexpected to get here when we are not connected to a host
            Debug.Assert(false);
            return;
         }

         if ((dataCache.ConnectionContext?.GetHashCode() ?? 0) != snapshot.DataCacheHashCode)
         {
            Trace.TraceWarning("[ConnectionPage] Data Cache was changed after launching diff tool");
            MessageBox.Show("It seems that data cache changed seriously after launching diff tool. " +
               "Please restart diff tool.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return;
         }

         IEnumerable<User> fullUserList = dataCache.UserCache?.GetUsers();
         DiffCallHandler handler = new DiffCallHandler(storage.Git, CurrentUser,
            (mrk) => dataCache.DiscussionCache?.RequestUpdate(
               mrk, Constants.DiscussionCheckOnNewThreadFromDiffToolInterval, null),
            (mrk) => dataCache.DiscussionCache?.GetDiscussions(mrk) ?? Array.Empty<Discussion>(),
            _shortcuts, fullUserList);
         handler.Handle(matchInfo, snapshot);
      }

      public bool CanDiffTool(DiffToolMode mode)
      {
         MergeRequestKey? mrkOpt = getMergeRequestKey(null);
         if (!mrkOpt.HasValue || !canUpdateStorageForMergeRequest(mrkOpt.Value))
         {
            return false;
         }

         int selectedRevisions = getRevisionBrowser().GetSelectedSha(out _).Count();
         switch (mode)
         {
            case DiffToolMode.DiffBetweenSelected:
               return selectedRevisions == 2;

            case DiffToolMode.DiffSelectedToBase:
               return selectedRevisions == 1;

            case DiffToolMode.DiffSelectedToParent:
               bool hasParent = getRevisionBrowser().GetParentShaForSelected() != null;
               return selectedRevisions == 1 && hasParent;

            case DiffToolMode.DiffLatestToBase:
               return true;

            default:
               Debug.Assert(false);
               break;
         }

         return false;
      }

      public bool CanDiscussions()
      {
         MergeRequestKey? mrkOpt = getMergeRequestKey(null);
         return mrkOpt.HasValue && canUpdateStorageForMergeRequest(mrkOpt.Value);
      }

      internal bool CanReloadAll()
      {
         return getListView(EDataCacheType.Live).Enabled;
      }

      internal bool CanAddComment()
      {
         MergeRequestKey? mrkOpt = getMergeRequestKey(null);
         return mrkOpt.HasValue && canUpdateStorageForMergeRequest(mrkOpt.Value);
      }

      internal bool CanNewThread()
      {
         MergeRequestKey? mrkOpt = getMergeRequestKey(null);
         return mrkOpt.HasValue && canUpdateStorageForMergeRequest(mrkOpt.Value);
      }

      internal bool CanAbortClone()
      {
         ProjectKey? projectKey = getMergeRequestKey(null)?.ProjectKey ?? null;
         ILocalCommitStorage repo = projectKey.HasValue ? getCommitStorage(projectKey.Value, false) : null;
         return repo?.Updater?.CanBeStopped() ?? false;
      }

      internal bool CanTrackTime()
      {
         MergeRequestKey? mrk = getMergeRequestKey(null);
         if (!mrk.HasValue)
         {
            return false;
         }

         User author = getDataCache(getCurrentTabDataCacheType())?.MergeRequestCache?.GetMergeRequest(mrk.Value)?.Author;
         string hostname = mrk.Value.ProjectKey.HostName;
         return TimeTrackingHelpers.IsTimeTrackingAllowed(author, hostname, CurrentUser);
      }

      internal bool CanCreateNew()
      {
         return areLongCachesReady(getDataCache(EDataCacheType.Live));
      }

      public bool CanEdit()
      {
         return areLongCachesReady(getDataCache(EDataCacheType.Live));
      }

      public bool CanMerge()
      {
         return areLongCachesReady(getDataCache(EDataCacheType.Live));
      }

      internal void RestoreSplitterDistance()
      {
         Trace.TraceInformation(
            "[ConnectionPage] RestoreSplitterDistance(), me={0}", HostName);

         resetSplitterDistance(splitContainerPrimary, ResetSplitterDistanceMode.UserDefined);
         resetSplitterDistance(splitContainerSecondary, ResetSplitterDistanceMode.UserDefined);
         resetSplitterDistance(descriptionSplitContainerSite.SplitContainer, ResetSplitterDistanceMode.UserDefined);
         resetSplitterDistance(revisionSplitContainerSite.SplitContainer, ResetSplitterDistanceMode.UserDefined);
      }

      internal void StoreSplitterDistance()
      {
         Trace.TraceInformation(
            "[ConnectionPage] StoreSplitterDistance(), me={0}", HostName);

         saveSplitterDistanceToConfig(splitContainerPrimary);
         saveSplitterDistanceToConfig(splitContainerSecondary);
         saveSplitterDistanceToConfig(descriptionSplitContainerSite.SplitContainer);
         saveSplitterDistanceToConfig(revisionSplitContainerSite.SplitContainer);
      }

      internal Color? GetSummaryColor()
      {
         if (isConnectionLost())
         {
            return _colorScheme?.GetColor("Status_LostConnection")?.Color;
         }
         return getListView(EDataCacheType.Live).GetSummaryColor();
      }

      internal void SetExiting()
      {
         _exiting = true;
      }

      internal event Action<ConnectionPage> RequestLive;
      internal event Action<ConnectionPage> RequestRecent;
      internal event Action<ConnectionPage> RequestSearch;

      internal event Action<ConnectionPage> CustomActionListChanged;
      internal event Action<ConnectionPage> EnabledCustomActionsChanged;

      internal event Action<ConnectionPage> CanCreateNewChanged;
      internal event Action<ConnectionPage> CanEditChanged;
      internal event Action<ConnectionPage> CanMergeChanged;
      internal event Action<ConnectionPage> CanDiffToolChanged;
      internal event Action<ConnectionPage> CanDiscussionsChanged;
      internal event Action<ConnectionPage> CanReloadAllChanged;
      internal event Action<ConnectionPage> CanAddCommentChanged;
      internal event Action<ConnectionPage> CanNewThreadChanged;
      internal event Action<ConnectionPage> CanAbortCloneChanged;
      internal event Action<ConnectionPage> CanTrackTimeChanged;

      internal event Action<ConnectionPage> SummaryColorChanged;
      internal event Action<ConnectionPage> ConnectionStatusChanged;
      internal event Action<ConnectionPage> StorageStatusChanged;
      internal event Action<ConnectionPage> LatestListRefreshTimestampChanged;

      internal event Action<string> StatusChanged;
   }
}

