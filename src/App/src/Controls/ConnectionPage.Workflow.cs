using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static mrHelper.App.Helpers.ConfigurationHelper;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;
using mrHelper.GitLabClient;

namespace mrHelper.App.Controls
{
   internal partial class ConnectionPage
   {
      internal class UnknownHostException : Exception
      {
         internal UnknownHostException(string hostname): base(
            String.Format("Cannot find access token for host {0}", hostname)) {}
      }

      internal class NothingToLoadException : Exception
      {
         internal NothingToLoadException(string hostname): base(
            String.Format("Nothing to load for {0}. Add user or project in Settings.", hostname)) {}
      }

      internal class CannotLoadGitLabVersionException : Exception
      {
         internal CannotLoadGitLabVersionException(string hostname): base(
            String.Format("Cannot load GitLab version from host {0}. Check access token and network connection.", hostname)) {}
      }

      internal class CannotLoadCurentUserException : Exception
      {
         internal CannotLoadCurentUserException(string hostname): base(
            String.Format("Cannot load current user from host {0}. Check access token and network connection.", hostname)) {}
      }

      private bool startWorkflowDefaultExceptionHandler(Exception ex)
      {
         if (ex is DataCacheException
          || ex is UnknownHostException
          || ex is NothingToLoadException
          || ex is CannotLoadGitLabVersionException
          || ex is CannotLoadCurentUserException)
         {
            if (!(ex is DataCacheConnectionCancelledException))
            {
               ExceptionHandlers.Handle("Cannot switch host", ex);
               string message = ex.Message;
               if (ex is DataCacheException wx)
               {
                  message = wx.UserMessage;
               }
               addOperationRecord(message);
               MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }
            return true;
         }
         return false;
      }

      async private Task connect(Func<Exception, bool> exceptionHandler)
      {
         await dropCacheConnectionsAsync();
         initializeGitLabInstance();

         try
         {
            await preStartWorkflowAsync();
            await startWorkflowAsync();
         }
         catch (Exception ex) // rethrow in case of unexpected exceptions
         {
            await dropCacheConnectionsAsync();
            if (exceptionHandler == null)
            {
               exceptionHandler = new Func<Exception, bool>(e => startWorkflowDefaultExceptionHandler(e));
            }
            if (!exceptionHandler(ex))
            {
               throw;
            }
         }
      }

      async private Task dropCacheConnectionsAsync()
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            DataCache dataCache = getDataCache(mode);
            if (dataCache != null)
            {
               await dataCache.Disconnect();
            }
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      async private Task preStartWorkflowAsync()
      {
         Trace.TraceInformation("[MainForm.Workflow] Starting workflow at host {0}. Workflow type is {1}",
            HostName, Program.Settings.WorkflowType);

         if (String.IsNullOrWhiteSpace(HostName) || getDataCache(EDataCacheType.Live) == null)
         {
            return;
         }

         addOperationRecord(String.Format("Connecting to {0}...", HostName));
         if (Program.Settings.GetAccessToken(HostName) == String.Empty)
         {
            throw new UnknownHostException(HostName);
         }

         await loadGitlabVersion();
         await loadCurrentUserAsync();
         checkApprovalSupport();

         await upgradeProjectListFromOldVersion();
         await initializeLabelListIfEmpty();
      }

      async private Task loadGitlabVersion()
      {
         if (GitLabVersion == null && _shortcuts != null)
         {
            GitLabVersion = await _shortcuts.GetGitLabVersionAccessor().GetGitLabVersionAsync();
         }
         if (GitLabVersion == null)
         {
            throw new CannotLoadGitLabVersionException(HostName);
         }
      }

      async private Task loadCurrentUserAsync()
      {
         if (CurrentUser == null && _shortcuts != null)
         {
            CurrentUser = await _shortcuts.GetUserAccessor().GetCurrentUserAsync();
         }
         if (CurrentUser == null)
         {
            throw new CannotLoadCurentUserException(HostName);
         }
      }

      private void checkApprovalSupport()
      {
         if (GitLabVersion == null || _isApprovalStatusSupported.HasValue)
         {
            return;
         }

         _isApprovalStatusSupported = GitLabClient.Helpers.DoesGitLabVersionSupportApprovals(GitLabVersion);
         CustomActionListChanged?.Invoke(this);
      }

      // Everything reconnects inside startWorkflowAsync(). If there are some things at gitlab that user
      // wants to be notified about and we did not cache them yet (e.g. mentions in discussions)
      // we will miss them. It might be ok in some cases, but if this method used to "refresh"
      // things, missed events are not desirable.
      // This is why "Refresh List" button implemented not by means of startWorkflowAsync().
      async private Task startWorkflowAsync()
      {
         SearchQueryCollection queryCollection = buildQueryCollection();
         if (!queryCollection.Queries.Any())
         {
            throw new NothingToLoadException(HostName);
         }
         await connectLiveDataCacheAsync(queryCollection);

         addOperationRecord(String.Format("Connection to {0} is established", HostName));
      }

      async private Task connectLiveDataCacheAsync(SearchQueryCollection queryCollection)
      {
         DataCacheConnectionContext connectionContext = new DataCacheConnectionContext(queryCollection);
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         await dataCache?.Connect(_gitLabInstance, connectionContext);
      }

      private void onForbiddenProject(ProjectKey projectKey)
      {
         string message = String.Format(
            "You don't have access to project {0} at {1}. "
          + "Loading of this project will be disabled. You may turn it on at Settings tab.",
            projectKey.ProjectName, projectKey.HostName);
         MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
         Trace.TraceInformation("[MainForm.Workflow] Forbidden project. User notified that project will be disabled");

         changeProjectEnabledState(projectKey, false);
      }

      private void onNotFoundProject(ProjectKey projectKey)
      {
         string message = String.Format(
            "There is no project {0} at {1}. "
          + "Loading of this project will be disabled. You may turn it on at Settings tab.",
            projectKey.ProjectName, projectKey.HostName);
         MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
         Trace.TraceInformation("[MainForm.Workflow] Project not found. User notified that project will be disabled");

         changeProjectEnabledState(projectKey, false);
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onLiveDataCacheDisconnected()
      {
         disableLiveTabControls();
         stopRedrawTimer();
         WinFormsHelpers.CloseAllFormsExceptOne("MainForm");
         disposeGitHelpers();
         disposeLocalGitRepositoryFactory();
         unsubscribeFromLiveDataCacheInternalEvents();
         disableSelectedMergeRequestControls();
         setConnectionStatus(null);
      }

      private void onLiveDataCacheConnecting(string hostname)
      {
         getListView(EDataCacheType.Live).Items.Clear();

         addOperationRecord(String.Format("Loading merge requests from {0} has started", hostname));

         setConnectionStatus(EConnectionStateInternal.ConnectingLive);
      }

      private void onLiveDataCacheConnected(string hostname, User user)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         subscribeToLiveDataCacheInternalEvents();
         createGitHelpers(dataCache, getCommitStorageFactory(false));

         Program.FeedbackReporter.SetUserEMail(user.EMail);
         startRedrawTimer();
         startEventPendingTimer(() => areLongCachesReady(dataCache), ProjectAndUserCacheCheckTimerInterval,
            () => onLongCachesReady());

         IEnumerable<MergeRequestKey> closedReviewed = gatherClosedReviewedMergeRequests(dataCache, hostname);
         cleanupReviewedMergeRequests(closedReviewed);
         loadRecentMergeRequests();

         IEnumerable<int> excludedMergeRequestIds = getExcludedMergeRequestIds(EDataCacheType.Live);
         IEnumerable<int> oldExcludedIds = selectNotCachedMergeRequestIds(EDataCacheType.Live, excludedMergeRequestIds);
         if (oldExcludedIds.Any())
         {
            Trace.TraceInformation("[ConnectionPage] Excluded Merge Requests are no longer in the cache {1}: {0}",
               String.Join(", ", oldExcludedIds), getDataCacheName(getDataCache(EDataCacheType.Live)));
            toggleMergeRequestExclusion(EDataCacheType.Live, oldExcludedIds);
         }

         updateMergeRequestList(EDataCacheType.Live);
         CanReloadAllChanged?.Invoke(this);
         addOperationRecord("Loading merge requests has completed");

         IEnumerable<ProjectKey> projects = getDataCache(EDataCacheType.Live).MergeRequestCache.GetProjects();
         foreach (ProjectKey projectKey in projects)
         {
            requestCommitStorageUpdate(projectKey);
         }

         enableSearchTabControls();

         // current mode may have changed during 'await'
         if (getCurrentTabDataCacheType() == EDataCacheType.Live && _isActivePage)
         {
            bool shouldUseLastSelection = _lastMergeRequestsByHosts.Data.ContainsKey(hostname);
            string projectname = shouldUseLastSelection ?
               _lastMergeRequestsByHosts[hostname].ProjectKey.ProjectName : String.Empty;
            int iid = shouldUseLastSelection ? _lastMergeRequestsByHosts[hostname].IId : 0;

            MergeRequestKey mrk = new MergeRequestKey(new ProjectKey(hostname, projectname), iid);
            getListView(EDataCacheType.Live).SelectMergeRequest(mrk, false);
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      async private Task upgradeProjectListFromOldVersion()
      {
         if (Program.Settings.SelectedProjectsUpgraded)
         {
            return;
         }

         GitLabClient.ProjectAccessor projectAccessor = _shortcuts.GetProjectAccessor();

         addOperationRecord("Preparing workflow to the first launch has started");
         StringToBooleanCollection projects = ConfigurationHelper.GetProjectsForHost(
            HostName, Program.Settings);
         StringToBooleanCollection upgraded = new StringToBooleanCollection();
         foreach (var project in projects)
         {
            Project p = await projectAccessor.SearchProjectAsync(project.Item1);
            if (p != null)
            {
               if (!upgraded.Any(x => x.Item1 == p.Path_With_Namespace))
               {
                  upgraded.Add(new Tuple<string, bool>(p.Path_With_Namespace, project.Item2));
               }
            }
         }
         ConfigurationHelper.SetProjectsForHost(HostName, upgraded, Program.Settings);
         Program.Settings.SelectedProjectsUpgraded = true;
         addOperationRecord("Workflow has been prepared to the first launch");
      }

      async private Task initializeLabelListIfEmpty()
      {
         if (ConfigurationHelper.GetUsersForHost(HostName, Program.Settings).Any())
         {
            return;
         }

         // on the first start users/projects are empty
         addOperationRecord("Preparing workflow to the first launch has started");
         string username = await DefaultWorkflowLoader.GetDefaultUserForHost(_gitLabInstance, CurrentUser);
         Tuple<string, bool>[] collection = new Tuple<string, bool>[] { new Tuple<string, bool>(username, true) };
         StringToBooleanCollection users = new StringToBooleanCollection(collection);
         ConfigurationHelper.SetUsersForHost(HostName, users, Program.Settings);
         addOperationRecord("Workflow has been prepared to the first launch");
      }

      private SearchQueryCollection buildQueryCollection()
      {
         IEnumerable<string> usernames = ConfigurationHelper.GetEnabledUsers(HostName, Program.Settings);
         IEnumerable<string> projectnames = ConfigurationHelper.GetEnabledProjects(HostName, Program.Settings);

         GitLabClient.SearchQuery[] queriesByUser = usernames
            .SelectMany(username => new GitLabClient.SearchQuery[]
               {
                  new GitLabClient.SearchQuery
                  {
                     Labels = new string[]{ Constants.GitLabLabelPrefix + username.ToLower() },
                     State = "opened"
                  },
                  // OR
                  new GitLabClient.SearchQuery
                  {
                     AuthorUserName = username,
                     State = "opened"
                  }
               })
            .ToArray();

         GitLabClient.SearchQuery[] queriesByProjects = projectnames
            .Select(projectName => new GitLabClient.SearchQuery
               {
                  ProjectName = projectName,
                  State = "opened"
               })
            .ToArray();

         return new SearchQueryCollection(queriesByUser.Concat(queriesByProjects));
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      [Flags]
      private enum DataCacheUpdateKind
      {
         MergeRequest = 1,
         Discussions = 2,
         MergeRequestAndDiscussions = MergeRequest | Discussions
      }

      private void requestUpdates(DataCache dataCache, MergeRequestKey? mrk, int interval, Action onUpdateFinished,
         DataCacheUpdateKind kind = DataCacheUpdateKind.MergeRequestAndDiscussions)
      {
         bool needUpdateMergeRequest = kind.HasFlag(DataCacheUpdateKind.MergeRequest);
         bool needUpdateDiscussions = kind.HasFlag(DataCacheUpdateKind.Discussions);

         bool mergeRequestUpdateFinished = !needUpdateMergeRequest;
         bool discussionUpdateFinished = !needUpdateDiscussions;

         void onSingleUpdateFinished()
         {
            if (mergeRequestUpdateFinished && discussionUpdateFinished)
            {
               onUpdateFinished?.Invoke();
            }
         }

         if (needUpdateMergeRequest)
         {
            dataCache?.MergeRequestCache?.RequestUpdate(mrk, interval,
               () =>
               {
                  mergeRequestUpdateFinished = true;
                  onSingleUpdateFinished();
               });
         }

         if (needUpdateDiscussions)
         {
            dataCache?.DiscussionCache?.RequestUpdate(mrk, interval,
               () =>
               {
                  discussionUpdateFinished = true;
                  onSingleUpdateFinished();
               });
         }
      }

      async private Task checkForUpdatesAsync(DataCache dataCache, MergeRequestKey? mrk,
         DataCacheUpdateKind kind = DataCacheUpdateKind.MergeRequestAndDiscussions)
      {
         bool updateReceived = false;
         bool updatingWholeList = !mrk.HasValue;

         requestUpdates(dataCache, mrk, PseudoTimerInterval, () => updateReceived = true, kind);
         await TaskUtils.WhileAsync(() => !updateReceived);
      }

      private void reloadMergeRequestsByUserRequest(DataCache dataCache)
      {
         if (HostName != String.Empty)
         {
            addOperationRecord("List refresh has started");

            requestUpdates(dataCache, null, PseudoTimerInterval,
               () => addOperationRecord("List refresh has completed"));
         }
      }
   }
}

