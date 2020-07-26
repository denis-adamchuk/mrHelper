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
using mrHelper.Client.Types;
using mrHelper.Client.Session;
using mrHelper.Client.Discussions;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;
using mrHelper.StorageSupport;

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
      Queue<DiffRequest> _requestedDiff = new Queue<DiffRequest>();
      private void enqueueDiffRequest(DiffRequest diffRequest)
      {
         _requestedDiff.Enqueue(diffRequest);
         if (_requestedDiff.Count == 1)
         {
            BeginInvoke(new Action(async () => await processDiffQueue()));
         }
      }

      async private Task processDiffQueue()
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

            ISession session = getSessionByName(snapshot.SessionName);
            if (session == null)
            {
               Debug.Assert(false);
               return;
            }

            DiffArgumentParser diffArgumentParser = new DiffArgumentParser(diffRequest.DiffArguments);
            DiffCallHandler handler;
            try
            {
               // TODO Add getter for current user
               handler = new DiffCallHandler(diffArgumentParser.Parse(), snapshot, getProjectAccessor(), _currentUser[getHostName()]);
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle("Cannot parse diff tool arguments", ex);
               MessageBox.Show("Bad arguments passed from diff tool", "Cannot create a discussion",
                  MessageBoxButtons.OK, MessageBoxIcon.Error,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
               return;
            }

            ICommitStorage storage = null;
            if (_storageFactory != null && _storageFactory.ParentFolder == snapshot.TempFolder)
            {
               ProjectKey projectKey = new ProjectKey(snapshot.Host, snapshot.Project);
               storage = getCommitStorage(projectKey, false);
            }

            try
            {
               await handler.HandleAsync(storage);
            }
            catch (DiscussionCreatorException)
            {
               Debug.Assert(false);
               return;
            }

            MergeRequestKey mrk = new MergeRequestKey(
               new ProjectKey(snapshot.Host, snapshot.Project), snapshot.MergeRequestIId);
            session.DiscussionCache?.RequestUpdate(mrk,
               new int[]{ Constants.DiscussionCheckOnNewThreadFromDiffToolInterval }, null);
         }
         finally
         {
            if (_requestedDiff.Any())
            {
               _requestedDiff.Dequeue();
               BeginInvoke(new Action(async () => await processDiffQueue()));
            }
         }
      }

      ///////////////////////////////////////////////////////////////////////////////////////////////////

      private void onOpenCommand(string argumentsString)
      {
         if (_gitlabClientManager?.SearchManager == null)
         {
            return;
         }

         string[] arguments = argumentsString.Split('|');
         string url = arguments[1];

         Trace.TraceInformation(String.Format("[Mainform] External request: connecting to URL {0}", url));
         enqueueUrlConnectionRequest(url, false);
      }

      Queue<string> _requestedUrl = new Queue<string>();
      private void enqueueUrlConnectionRequest(string url, bool startup)
      {
         _requestedUrl.Enqueue(url);
         if (_requestedUrl.Count == 1)
         {
            BeginInvoke(new Action(
               async () =>
            {
               if (!await processUrlConnectionQueue() && startup)
               {
                  selectHost(PreferredSelection.Initial);
                  switchHostToSelected();
               }
            }));
         }
      }

      async private Task<bool> processUrlConnectionQueue()
      {
         if (!_requestedUrl.Any())
         {
            return true;
         }

         string url = _requestedUrl.Peek();
         try
         {
            if (String.IsNullOrWhiteSpace(url))
            {
               return false;
            }

            await connectToUrlAsyncInternal(url);
            return true;
         }
         catch (UrlConnectionException ex)
         {
            if (ex.InnerException is SessionStartCancelledException)
            {
               return true;
            }

            reportErrorOnConnect(url, ex.OriginalMessage, ex.InnerException);
            return false;
         }
         finally
         {
            if (_requestedUrl.Any())
            {
               _requestedUrl.Dequeue();
               BeginInvoke(new Action(async () => await processUrlConnectionQueue()));
            }
         }
      }

      private class UrlConnectionException : ExceptionEx
      {
         internal UrlConnectionException(string message, Exception innerException)
            : base(message, innerException) { }
      }

      async private Task connectToUrlAsyncInternal(string originalUrl)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] Initializing Workflow with URL {0}", originalUrl));

         string prefix = Constants.CustomProtocolName + "://";
         string url = originalUrl.StartsWith(prefix) ? originalUrl.Substring(prefix.Length) : originalUrl;
         MergeRequestKey mrk = parseUrlIntoMergeRequestKey(url);
         if (Program.Settings.GetAccessToken(mrk.ProjectKey.HostName) == String.Empty)
         {
            throw new UrlConnectionException(String.Format(
               "Cannot connect to {0} because it is not in the list of known hosts. ", mrk.ProjectKey.HostName), null);
         }

         labelWorkflowStatus.Text = String.Format("Connecting to {0}...", url);
         MergeRequest mergeRequest = await _gitlabClientManager.SearchManager.SearchMergeRequestAsync(
            mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId);
         if (mergeRequest == null)
         {
            throw new UrlConnectionException("Merge request does not exist. ", null);
         }
         labelWorkflowStatus.Text = String.Empty;

         bool canOpenAtLiveTab = mergeRequest.State == "opened" && checkIfCanOpenAtLiveTab(mrk, true);
         bool needReload = (canOpenAtLiveTab && getSession(canOpenAtLiveTab).MergeRequestCache == null)
                        || mrk.ProjectKey.HostName != getHostName();
         if (needReload)
         {
            Trace.TraceInformation("[MainForm.ExternalCalls] Restart workflow for url {0}", url);
            await restartWorkflowByUrlAsync(mrk.ProjectKey.HostName);
         }

         if (!canOpenAtLiveTab || !await openUrlAtLiveTabAsync(mrk, url, !needReload))
         {
            await openUrlAtSearchTabAsync(mrk, url);
         }
      }

      private void reportErrorOnConnect(string url, string msg, Exception ex)
      {
         string exceptionMessage = ex != null ? ex.Message : String.Empty;
         if (ex is SessionException wfex)
         {
            exceptionMessage = wfex.UserMessage;
         }

         string msgBoxMessage = String.Format("{0}Cannot open merge request from URL. {1}", msg, exceptionMessage);

         MessageBox.Show(msgBoxMessage, "Warning", MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

         string errorMessage = String.Format("Cannot open URL {0}", url);
         ExceptionHandlers.Handle(errorMessage, ex);
         labelWorkflowStatus.Text = errorMessage;
      }

      async private Task restartWorkflowByUrlAsync(string hostname)
      {
         _initialHostName = hostname;
         selectHost(PreferredSelection.Initial);
         await switchHostToSelectedAsync(new Func<Exception, bool>(x =>
            throw new UrlConnectionException("Failed to connect to GitLab. ", x)));
      }

      async private Task<bool> openUrlAtLiveTabAsync(MergeRequestKey mrk, string url, bool updateIfNeeded)
      {
         ISession session = getSession(true);
         if (session?.MergeRequestCache == null)
         {
            throw new UrlConnectionException("Merge request loading was cancelled due to host switch. ", null);
         }

         if (!session.MergeRequestCache.GetMergeRequests(mrk.ProjectKey).Any(x => x.IId == mrk.IId))
         {
            // We need to update the MR list here because cached one is possible outdated
            if (updateIfNeeded)
            {
               labelWorkflowStatus.Text = String.Format(
                  "Merge Request with IId {0} is not found in the cache, updating the list...", mrk.IId);
               await checkForUpdatesAsync();
               if (getHostName() != mrk.ProjectKey.HostName || session.MergeRequestCache == null)
               {
                  throw new UrlConnectionException("Merge request loading was cancelled due to host switch. ", null);
               }
            }

            if (!checkIfCanOpenAtLiveTab(mrk, false))
            {
               // this may happen if project list changed while we were in 'await'
               return false;
            }
         }

         tabControlMode.SelectedTab = tabPageLive;
         if (!selectMergeRequest(listViewMergeRequests, mrk, true) && listViewMergeRequests.Enabled)
         {
            // We could not select MR, but let's check if it is cached or not.
            if (session.MergeRequestCache.GetMergeRequests(mrk.ProjectKey).Any(x => x.IId == mrk.IId))
            {
               // If it is cached, it is probably hidden by filters and user might want to un-hide it.
               if (!unhideFilteredMergeRequest(mrk))
               {
                  return false; // user decided to not un-hide merge request
               }

               if (!selectMergeRequest(listViewMergeRequests, mrk, true))
               {
                  Debug.Assert(false);
                  Trace.TraceError(String.Format("[MainForm] Cannot open URL {0}, although MR is cached", url));
                  throw new UrlConnectionException("Something went wrong. ", null);
               }
            }
            else
            {
               if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
               {
                  Debug.Assert(false);
                  Trace.TraceError(String.Format("[MainForm] Cannot open URL {0} by unknown reason", url));
                  throw new UrlConnectionException("Something went wrong. ", null);
               }
               return false;
            }
         }

         return true;
      }

      async private Task openUrlAtSearchTabAsync(MergeRequestKey mrk, string url)
      {
         tabControlMode.SelectedTab = tabPageSearch;
         await searchMergeRequestsAsync(new SearchByIId(mrk.ProjectKey.ProjectName, mrk.IId), null,
            new Func<Exception, bool>(x =>
               throw new UrlConnectionException("Failed to open merge request at Search tab. ", x)));
      }

      private bool unhideFilteredMergeRequest(MergeRequestKey mrk)
      {
         Trace.TraceInformation("[MainForm] Notify user that MR is hidden");

         if (MessageBox.Show("Merge Request is hidden by filters and cannot be opened. Do you want to reset filters?",
               "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification) != DialogResult.Yes)
         {
            Trace.TraceInformation("[MainForm] User decided not to reset filters");
            return false;
         }

         _lastMergeRequestsByHosts[mrk.ProjectKey.HostName] = mrk;
         checkBoxDisplayFilter.Checked = false;
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

         if (MessageBox.Show("Selected project is not enabled. Do you want to enable it?"
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

      private bool checkIfCanOpenAtLiveTab(MergeRequestKey mrk, bool proposeFix)
      {
         if (!ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            return true;
         }

         if (!isProjectInTheList(mrk.ProjectKey) && (!proposeFix || !addMissingProject(mrk.ProjectKey)))
         {
            return false;
         }

         if (!isEnabledProject(mrk.ProjectKey) && (!proposeFix || !enableDisabledProject(mrk.ProjectKey)))
         {
            return false;
         }

         return true;
      }

      private MergeRequestKey parseUrlIntoMergeRequestKey(string url)
      {
         try
         {
            UrlParser.ParsedMergeRequestUrl originalParsed = UrlParser.ParseMergeRequestUrl(url);
            UrlParser.ParsedMergeRequestUrl mergeRequestUrl = new UrlParser.ParsedMergeRequestUrl(
               StringUtils.GetHostWithPrefix(originalParsed.Host), originalParsed.Project, originalParsed.IId);

            return new MergeRequestKey(new ProjectKey(mergeRequestUrl.Host, mergeRequestUrl.Project),
               mergeRequestUrl.IId);
         }
         catch (Exception ex)
         {
            Debug.Assert(ex is UriFormatException);
            throw new UrlConnectionException(String.Empty, ex);
         }
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
         return enabled.Any() ? enabled.First().Item2 : false;
      }
   }
}

