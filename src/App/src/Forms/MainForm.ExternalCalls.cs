using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.App.Interprocess;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;
using mrHelper.App.Helpers.GitLab;
using mrHelper.App.Forms.Helpers;
using SearchQuery = mrHelper.GitLabClient.SearchQuery;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void onDiffCommand(string argumentString)
      {
         string[] argumentsEx = argumentString.Split('|');
         int gitPID = int.Parse(argumentsEx[argumentsEx.Length - 1]);

         string[] arguments = new string[argumentsEx.Length - 1];
         Array.Copy(argumentsEx, 0, arguments, 0, argumentsEx.Length - 1);

         enqueueDiffRequest(new DiffRequest(gitPID, arguments));
      }

      struct DiffRequest
      {
         internal int GitPID { get; }
         internal string[] DiffArguments { get; }

         public DiffRequest(int gitPID, string[] diffArguments)
         {
            GitPID = gitPID;
            DiffArguments = diffArguments;
         }
      }

      readonly Queue<DiffRequest> _requestedDiff = new Queue<DiffRequest>();
      private void enqueueDiffRequest(DiffRequest diffRequest)
      {
         _requestedDiff.Enqueue(diffRequest);
         if (_requestedDiff.Count == 1)
         {
            BeginInvoke(new Action(() => processDiffQueue()));
         }
      }

      private void processDiffQueue()
      {
         if (!_requestedDiff.Any())
         {
            return;
         }

         DiffRequest diffRequest = _requestedDiff.Peek();
         try
         {
            SnapshotSerializer serializer = new SnapshotSerializer();
            Snapshot snapshot;
            try
            {
               snapshot = serializer.DeserializeFromDisk(diffRequest.GitPID);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle("Cannot read serialized Snapshot object", ex);
               MessageBox.Show(
                  "Make sure that diff tool was launched from Merge Request Helper which is still running",
                  "Cannot create a discussion",
                  MessageBoxButtons.OK, MessageBoxIcon.Error,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               return;
            }

            if (_storageFactory == null || _storageFactory.ParentFolder != snapshot.TempFolder)
            {
               Trace.TraceWarning("[MainForm] File Storage folder was changed after launching diff tool");
               MessageBox.Show("It seems that file storage folder was changed after launching diff tool. " +
                  "Please restart diff tool.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               return;
            }

            Core.Matching.MatchInfo matchInfo;
            try
            {
               DiffArgumentParser diffArgumentParser = new DiffArgumentParser(diffRequest.DiffArguments);
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
               Trace.TraceError("[MainForm] storage.Git is null");
               Debug.Assert(false);
               return;
            }

            DataCache dataCache = getDataCacheByName(snapshot.DataCacheName);
            if (dataCache == null || getCurrentUser() == null)
            {
               // It is unexpected to get here when we are not connected to a host
               Debug.Assert(false);
               return;
            }

            if ((dataCache.ConnectionContext?.GetHashCode() ?? 0) != snapshot.DataCacheHashCode)
            {
               Trace.TraceWarning("[MainForm] Data Cache was changed after launching diff tool");
               MessageBox.Show("It seems that data cache changed seriously after launching diff tool. " +
                  "Please restart diff tool.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               return;
            }

            DiffCallHandler handler = new DiffCallHandler(storage.Git, getCurrentUser(),
               (mrk) => dataCache.DiscussionCache?.RequestUpdate(
                  mrk, Constants.DiscussionCheckOnNewThreadFromDiffToolInterval, null),
               (mrk) => dataCache.DiscussionCache?.GetDiscussions(mrk) ?? Array.Empty<Discussion>(),
               _shortcuts);
            handler.Handle(matchInfo, snapshot);
         }
         finally
         {
            if (_requestedDiff.Any())
            {
               _requestedDiff.Dequeue();
               BeginInvoke(new Action(() => processDiffQueue()));
            }
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onOpenCommand(string argumentsString)
      {
         string[] arguments = argumentsString.Split('|');
         string url = arguments[1];

         Trace.TraceInformation(String.Format("[Mainform] External request: connecting to URL {0}", url));
         reconnect(url);
      }

      readonly Queue<string> _requestedUrl = new Queue<string>();
      private void enqueueUrl(string url)
      {
         _requestedUrl.Enqueue(url);
         if (_requestedUrl.Count == 1)
         {
            BeginInvoke(new Action(async () => await processUrlQueue()));
         }
      }

      async private Task processUrlQueue()
      {
         if (!_requestedUrl.Any())
         {
            return;
         }

         string url = _requestedUrl.Peek();
         try
         {
            await processUrl(url);
         }
         finally
         {
            if (_requestedUrl.Any())
            {
               _requestedUrl.Dequeue();
               BeginInvoke(new Action(async () => await processUrlQueue()));
            }
         }
      }

      private async Task processUrl(string url)
      {
         if (String.IsNullOrEmpty(url))
         {
            await switchHostToSelectedAsync(null);
            return;
         }

         try
         {
            object parsed = UrlHelper.Parse(url);
            if (parsed is UrlParser.ParsedMergeRequestUrl parsedMergeRequestUrl)
            {
               throwOnUnknownHost(parsedMergeRequestUrl.Host);
               await connectToUrlAsyncInternal(url, parsedMergeRequestUrl);
               return;
            }
            else if (parsed is ParsedNewMergeRequestUrl parsedNewMergeRequestUrl)
            {
               if (getHostName() != parsedNewMergeRequestUrl.ProjectKey.HostName || getCurrentUser() == null)
               {
                  throwOnUnknownHost(parsedNewMergeRequestUrl.ProjectKey.HostName);
                  await restartWorkflowByUrlAsync(parsedNewMergeRequestUrl.ProjectKey.HostName);
               }
               createMergeRequestFromUrl(parsedNewMergeRequestUrl);
               return;
            }
            else if (parsed == null)
            {
               MessageBox.Show("Failed to parse URL", "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }
         }
         catch (UrlConnectionException ex)
         {
            if (ex.InnerException is DataCacheConnectionCancelledException
             || ex.InnerException is MergeRequestAccessorCancelledException)
            {
               return;
            }

            showMessageBoxOnUrlConnectionException(ex);

            string briefMessage = String.Format("Cannot open URL {0}", url);
            addOperationRecord(briefMessage);
            ExceptionHandlers.Handle(briefMessage, ex.InnerException);
         }

         await switchHostToSelectedAsync(null);
      }

      private void throwOnUnknownHost(string hostname)
      {
         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UrlConnectionException(String.Format(
               "Cannot connect to {0} because it is not in the list of known hosts. ", hostname));
         }
      }

      private void createMergeRequestFromUrl(ParsedNewMergeRequestUrl parsedNewMergeRequestUrl)
      {
         if (!checkIfMergeRequestCanBeCreated())
         {
            return;
         }

         NewMergeRequestProperties defaultProperties = getDefaultNewMergeRequestProperties(
            getHostName(), getCurrentUser(), null);
         NewMergeRequestProperties initialProperties = new NewMergeRequestProperties(
            parsedNewMergeRequestUrl.ProjectKey.ProjectName, parsedNewMergeRequestUrl.SourceBranch,
            parsedNewMergeRequestUrl.TargetBranchCandidates, defaultProperties.AssigneeUsername,
            defaultProperties.IsSquashNeeded, defaultProperties.IsBranchDeletionNeeded);
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         var fullProjectList = dataCache?.ProjectCache?.GetProjects() ?? Array.Empty<Project>();
         var fullUserList = dataCache?.UserCache?.GetUsers() ?? Array.Empty<User>();
         if (!fullUserList.Any())
         {
            Trace.TraceInformation("[MainForm] User list is not ready at the moment of creating a MR from URL");
         }

         createNewMergeRequest(getHostName(), getCurrentUser(), initialProperties, fullProjectList, fullUserList);
      }

      private class UrlConnectionException : ExceptionEx
      {
         internal UrlConnectionException(string message, Exception innerException = null)
            : base(message, innerException) { }
      }

      async private Task connectToUrlAsyncInternal(string url, UrlParser.ParsedMergeRequestUrl parsedUrl)
      {
         MergeRequestKey mrk = parseUrlIntoMergeRequestKey(parsedUrl);

         // First, try to select a MR from lists of visible MRs
         bool tryOpenAtLiveTab = true;
         switch (trySelectMergeRequest(mrk))
         {
            case SelectionResult.NotFound:
               break;
            case SelectionResult.Selected:
               addOperationRecord("Merge Request was found in cache and selected");
               return;
            case SelectionResult.Hidden:
               tryOpenAtLiveTab = false;
               break;
         }

         bool isConnected = getDataCache(EDataCacheType.Live)?.ConnectionContext != null;
         bool needReload = mrk.ProjectKey.HostName != getHostName() || !isConnected;
         if (needReload)
         {
            Trace.TraceInformation("[MainForm.ExternalCalls] Restart workflow for url {0}", url);
            await restartWorkflowByUrlAsync(mrk.ProjectKey.HostName);
         }

         // If MR is not found at the Live tab at all or user rejected to unhide it,
         // don't try to open it at the Live tab.
         // Otherwise, check if requested MR match workflow filters.
         tryOpenAtLiveTab = tryOpenAtLiveTab && (await checkLiveDataCacheFilterAsync(mrk, url));
         if (!tryOpenAtLiveTab || !await openUrlAtLiveTabAsync(mrk, url, !needReload))
         {
            await openUrlAtSearchTabAsync(mrk);
         }
      }

      private enum SelectionResult
      {
         NotFound,
         Hidden,
         Selected,
      }

      private SelectionResult trySelectMergeRequest(MergeRequestKey mrk)
      {
         bool isCached(EDataCacheType mode) => getDataCache(mode)?.MergeRequestCache?.GetMergeRequest(mrk) != null;

         // We want to check lists in specific order:
         EDataCacheType[] modes = new EDataCacheType[]
         {
            EDataCacheType.Live,
            EDataCacheType.Recent,
            EDataCacheType.Search
         };

         // Check if requested MR is cached
         if (modes.All(mode => !isCached(mode)))
         {
            return SelectionResult.NotFound;
         }

         // Try selecting an item which is not hidden by filters
         foreach (EDataCacheType mode in modes)
         {
            if (isCached(mode) && switchTabAndSelectMergeRequest(mode, mrk))
            {
               return SelectionResult.Selected;
            }
         }

         // If we are here, requested MR is hidden on each tab where it is cached
         foreach (EDataCacheType mode in modes)
         {
            if (isCached(mode))
            {
               if (unhideFilteredMergeRequest(mode, mrk))
               {
                  if (switchTabAndSelectMergeRequest(mode, mrk))
                  {
                     return SelectionResult.Selected;
                  }
                  Debug.Assert(false);
               }
               else
               {
                  break; // don't ask more than once
               }
            }
         }

         return SelectionResult.Hidden;
      }

      async private Task<MergeRequest> searchMergeRequestAsync(MergeRequestKey mrk)
      {
         try
         {
            MergeRequest mergeRequest = await _shortcuts
               .GetMergeRequestAccessor(mrk.ProjectKey)
               .SearchMergeRequestAsync(mrk.IId, false);
            if (mergeRequest == null)
            {
               throw new UrlConnectionException("Merge request does not exist. ");
            }
            return mergeRequest;
         }
         catch (MergeRequestAccessorException ex)
         {
            throw new UrlConnectionException("Failed to check if merge request exists at GitLab. ", ex);
         }
      }

      private void showMessageBoxOnUrlConnectionException(UrlConnectionException ex)
      {
         string errorDescription = ex.OriginalMessage;
         string innerMessage = ex.InnerException == null ? String.Empty : ex.InnerException.Message;
         string errorDetails = ex.InnerException is ExceptionEx exex ? exex.UserMessage : innerMessage;

         string msgBoxMessage = String.Format("Cannot open merge request from URL. {0} {1}",
            errorDescription, errorDetails);

         MessageBox.Show(msgBoxMessage, "Warning", MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
      }

      async private Task restartWorkflowByUrlAsync(string hostname)
      {
         setInitialHostName(Common.Tools.StringUtils.GetHostWithPrefix(hostname));
         selectHost(PreferredSelection.Initial);
         await switchHostToSelectedAsync(new Func<Exception, bool>(x =>
            throw new UrlConnectionException("Failed to connect to GitLab. ", x)));
      }

      async private Task<bool> openUrlAtLiveTabAsync(MergeRequestKey mrk, string url, bool updateIfNeeded)
      {
         DataCache dataCache = getDataCache(EDataCacheType.Live);
         if (dataCache?.MergeRequestCache == null)
         {
            throw new UrlConnectionException("Merge request loading was cancelled due to host switch. ");
         }

         if (!dataCache.MergeRequestCache.GetMergeRequests(mrk.ProjectKey).Any(x => x.IId == mrk.IId))
         {
            // We need to update the MR list here because cached one is possible outdated
            if (updateIfNeeded)
            {
               addOperationRecord(String.Format(
                  "Merge Request with IId {0} is not found in the cache. List update has started.", mrk.IId));
               await checkForUpdatesAsync(getDataCache(EDataCacheType.Live), null);
               addOperationRecord("Merge request list update has completed");
               if (getHostName() != mrk.ProjectKey.HostName || dataCache.MergeRequestCache == null)
               {
                  throw new UrlConnectionException("Merge request loading was cancelled due to host switch. ");
               }
            }

            if (!checkProjectWorkflowFilters(mrk))
            {
               // this may happen if project list changed while we were in 'await'
               return false;
            }
         }

         if (!switchTabAndSelectMergeRequest(EDataCacheType.Live, mrk) && getListView(EDataCacheType.Live).Enabled)
         {
            // We could not select MR, but let's check if it is cached or not.
            if (dataCache.MergeRequestCache.GetMergeRequests(mrk.ProjectKey).Any(x => x.IId == mrk.IId))
            {
               // If it is cached, it is probably hidden by filters and user might want to un-hide it.
               if (!unhideFilteredMergeRequest(EDataCacheType.Live, mrk))
               {
                  return false; // user decided to not un-hide merge request
               }

               if (!getListView(EDataCacheType.Live).SelectMergeRequest(mrk, true))
               {
                  Debug.Assert(false);
                  Trace.TraceError(String.Format("[MainForm] Cannot open URL {0}, although MR is cached", url));
                  throw new UrlConnectionException("Something went wrong. ");
               }
               getListView(EDataCacheType.Live).EnsureSelectionVisible();
            }
            else
            {
               if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
               {
                  Debug.Assert(false);
                  Trace.TraceError(String.Format("[MainForm] Cannot open URL {0} by unknown reason", url));
                  throw new UrlConnectionException("Something went wrong. ");
               }
               return false;
            }
         }

         return true;
      }

      async private Task openUrlAtSearchTabAsync(MergeRequestKey mrk)
      {
         switchMode(EDataCacheType.Search);
         await searchMergeRequestsSafeAsync(
            new SearchQueryCollection(new SearchQuery
            {
               IId = mrk.IId,
               ProjectName = mrk.ProjectKey.ProjectName,
               MaxResults = 1
            }),
            EDataCacheType.Search,
            new Func<Exception, bool>(x =>
               throw new UrlConnectionException("Failed to open merge request at Search tab. ", x)));
         getListView(EDataCacheType.Search).EnsureGroupIsNotCollapsed(mrk.ProjectKey);
      }

      private bool unhideFilteredMergeRequest(EDataCacheType dataCacheType, MergeRequestKey mrk)
      {
         Trace.TraceInformation("[MainForm] Notify user that MR is hidden");

         if (MessageBox.Show("Merge Request is hidden by filters and cannot be opened. Do you want to reset filters?",
               "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification) != DialogResult.Yes)
         {
            Trace.TraceInformation("[MainForm] User decided not to reset filters");
            return false;
         }

         if (dataCacheType == EDataCacheType.Live)
         {
            checkBoxDisplayFilter.Checked = false;
         }
         getListView(dataCacheType).EnsureGroupIsNotCollapsed(mrk.ProjectKey);
         return true;
      }

      private bool addMissingProject(ProjectKey projectKey)
      {
         Trace.TraceInformation("[MainForm] Notify that selected project is not in the list");

         if (MessageBox.Show("Selected project is not in the list of projects. Do you want to add it? "
               + "Selecting 'Yes' will cause reload of all projects. "
               + "Selecting 'No' will open the merge request at Search tab. ",
               "Warning",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification) != DialogResult.Yes)
         {
            Trace.TraceInformation("[MainForm] User decided not to add project");
            return false;
         }

         Dictionary<string, bool> projects = ConfigurationHelper.GetProjectsForHost(
            projectKey.HostName, Program.Settings).ToDictionary(item => item.Item1, item => item.Item2);
         Debug.Assert(!projects.ContainsKey(projectKey.ProjectName));
         projects.Add(projectKey.ProjectName, true);

         ConfigurationHelper.SetProjectsForHost(projectKey.HostName,
            Enumerable.Zip(projects.Keys, projects.Values, (x, y) => new Tuple<string, bool>(x, y)), Program.Settings);
         updateProjectsListView();
         return true;
      }

      private bool enableDisabledProject(ProjectKey projectKey)
      {
         Trace.TraceInformation("[MainForm] Notify that selected project is disabled");

         if (MessageBox.Show("Selected project is not enabled. Do you want to enable it? "
               + "Selecting 'Yes' will cause reload of all projects. "
               + "Selecting 'No' will open the merge request at Search tab. ",
               "Warning",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification) != DialogResult.Yes)
         {
            Trace.TraceInformation("[MainForm] User decided not to enable project");
            return false;
         }

         changeProjectEnabledState(projectKey, true);
         return true;
      }

      private bool checkProjectWorkflowFilters(MergeRequestKey mrk)
      {
         if (!ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            return true;
         }

         return isProjectInTheList(mrk.ProjectKey) && isEnabledProject(mrk.ProjectKey);
      }

      async private Task<bool> fixProjectWorkflowFiltersAsync(MergeRequestKey mrk)
      {
         if (!ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            return true;
         }

         if (!isProjectInTheList(mrk.ProjectKey))
         {
            if (addMissingProject(mrk.ProjectKey))
            {
               await restartWorkflowByUrlAsync(mrk.ProjectKey.HostName);
               return true;
            }
            return false;
         }

         if (!isEnabledProject(mrk.ProjectKey))
         {
            if (enableDisabledProject(mrk.ProjectKey))
            {
               await restartWorkflowByUrlAsync(mrk.ProjectKey.HostName);
               return true;
            }
            return false;
         }

         return true;
      }

      async private Task<bool> checkLiveDataCacheFilterAsync(MergeRequestKey mrk, string url)
      {
         if (!await fixProjectWorkflowFiltersAsync(mrk))
         {
            return false;
         }

         addOperationRecord(String.Format("Checking merge request at {0} started", url));
         MergeRequest mergeRequest = await searchMergeRequestAsync(mrk);
         Debug.Assert(mergeRequest != null);
         addOperationRecord(String.Format("Checking merge request at {0} has completed", url));

         DataCache dataCache = getDataCache(EDataCacheType.Live);
         if (dataCache == null)
         {
            return false;
         }

         Debug.Assert(dataCache.ConnectionContext != null);
         SearchQueryCollection queries = dataCache.ConnectionContext.QueryCollection;
         return GitLabClient.Helpers.DoesMatchSearchQuery(queries, mergeRequest, mrk.ProjectKey);
      }

      private MergeRequestKey parseUrlIntoMergeRequestKey(UrlParser.ParsedMergeRequestUrl parsedUrl)
      {
         return new MergeRequestKey(new ProjectKey(parsedUrl.Host, parsedUrl.Project), parsedUrl.IId);
      }

      private static bool isProjectInTheList(ProjectKey projectKey)
      {
         IEnumerable<Tuple<string, bool>> projects =
            ConfigurationHelper.GetProjectsForHost(projectKey.HostName, Program.Settings);
         return projects.Any(x => projectKey.MatchProject(x.Item1));
      }

      private static bool isEnabledProject(ProjectKey projectKey)
      {
         IEnumerable<Tuple<string, bool>> projects =
            ConfigurationHelper.GetProjectsForHost(projectKey.HostName, Program.Settings);
         IEnumerable<Tuple<string, bool>> enabled =
            projects.Where(x => projectKey.MatchProject(x.Item1));
         return enabled.Any() && enabled.First().Item2;
      }
   }
}

