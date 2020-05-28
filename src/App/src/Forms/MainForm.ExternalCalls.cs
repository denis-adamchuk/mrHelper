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
using mrHelper.Client.Common;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      async private Task onDiffCommand(string argumentsString)
      {
         string[] argumentsEx = argumentsString.Split('|');
         int gitPID = int.Parse(argumentsEx[argumentsEx.Length - 1]);

         string[] arguments = new string[argumentsEx.Length - 1];
         Array.Copy(argumentsEx, 0, arguments, 0, argumentsEx.Length - 1);

         SnapshotSerializer serializer = new SnapshotSerializer();
         Snapshot snapshot;
         try
         {
            snapshot = serializer.DeserializeFromDisk(gitPID);
         }
         catch (System.IO.IOException ex)
         {
            ExceptionHandlers.Handle("Cannot de-serialize snapshot", ex);
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

         DiffArgumentParser diffArgumentParser = new DiffArgumentParser(arguments);
         DiffCallHandler handler;
         try
         {
            handler = new DiffCallHandler(diffArgumentParser.Parse(), snapshot, session);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot parse diff tool arguments", ex);
            MessageBox.Show("Bad arguments passed from diff tool", "Cannot create a discussion",
               MessageBoxButtons.OK, MessageBoxIcon.Error,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return;
         }

         IGitRepository gitRepository = null;
         if (_gitClientFactory != null && _gitClientFactory.ParentFolder == snapshot.TempFolder)
         {
            ProjectKey projectKey = new ProjectKey(snapshot.Host, snapshot.Project);
            gitRepository = _gitClientFactory.GetRepository(projectKey);
         }

         try
         {
            await handler.HandleAsync(gitRepository);
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

      async private Task onOpenCommand(string argumentsString)
      {
         string[] arguments = argumentsString.Split('|');
         string url = arguments[1];

         Trace.TraceInformation(String.Format("[Mainform] External request: connecting to URL {0}", url));
         //if (_suppressExternalConnections)
         {
            Trace.TraceInformation("[Mainform] Cannot connect to URL because the app is connecting");
            //return;
         }

         //_suppressExternalConnections = true;
         try
         {
            await connectToUrlAsync(url);
         }
         finally
         {
            //_suppressExternalConnections = false;
         }
      }

      private void reportErrorOnConnect(string url, string msg, Exception ex, bool error)
      {
         string exceptionMessage = ex != null ? ex.Message : String.Empty;
         if (ex is SessionException wfex)
         {
            exceptionMessage = wfex.UserMessage;
         }

         string msgBoxMessage = String.Format("{0}Cannot open merge request from URL. {1}", msg, exceptionMessage);

         MessageBox.Show(msgBoxMessage, error ? "Error" : "Warning", MessageBoxButtons.OK,
            error ? MessageBoxIcon.Error : MessageBoxIcon.Exclamation,
            MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

         string errorMessage = String.Format("Cannot open URL {0}", url);
         ExceptionHandlers.Handle(errorMessage, ex);
         labelWorkflowStatus.Text = errorMessage;
      }

      async private Task<bool> restartWorkflowByUrl(string url, string hostname)
      {
         _initialHostName = hostname;
         selectHost(PreferredSelection.Initial);

         return await switchHostToSelected(new Func<Exception, bool>(
            (ex) =>
         {
            if (ex is UnknownHostException)
            {
               reportErrorOnConnect(url, String.Empty, ex, true);
            }
            else if (ex is NoProjectsException)
            {
               reportErrorOnConnect(url, String.Empty, ex, true);
            }
            else if (ex is SessionException)
            {
               reportErrorOnConnect(url, String.Empty, ex, true);
            }
            else
            {
               Debug.Assert(false);
               ExceptionHandlers.Handle(String.Format("Unexpected error on attempt to open URL {0}", url), ex);
            }
            return true;
         }));
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

      async private Task openUrlAtSearchTab(MergeRequestKey mrk, string url)
      {
         tabControlMode.SelectedTab = tabPageSearch;

         await searchMergeRequests(new SearchByIId(mrk.ProjectKey.ProjectName, mrk.IId), null,
            new Func<Exception, bool>(
               (ex) =>
         {
            if (ex is UnknownHostException)
            {
               reportErrorOnConnect(url, String.Empty, ex, true);
            }
            else if (ex is SessionException)
            {
               reportErrorOnConnect(url, String.Empty, ex, true);
            }
            else
            {
               Debug.Assert(false);
               ExceptionHandlers.Handle(String.Format("Unexpected error on attempt to open URL {0}", url), ex);
            }
            return true;
         }));
      }

      async private Task connectToUrlAsync(string originalUrl)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] Initializing Workflow with URL {0}", originalUrl));

         string prefix = Constants.CustomProtocolName + "://";
         string url = originalUrl.StartsWith(prefix) ? originalUrl.Substring(prefix.Length) : originalUrl;
         MergeRequestKey? mrkOpt = parseUrlIntoMergeRequestKey(url);
         if (!mrkOpt.HasValue)
         {
            return;
         }

         MergeRequestKey mrk = mrkOpt.Value;
         if (!isKnownHostInUrl(mrk.ProjectKey.HostName, url))
         {
            return;
         }

         labelWorkflowStatus.Text = String.Format("Connecting to {0}...", url);
         MergeRequest mergeRequest = await _gitlabClientManager?.SearchManager?.SearchMergeRequestAsync(
            mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId);
         if (mergeRequest == null)
         {
            reportErrorOnConnect(url, "Merge Request does not exist. ", null, false);
            return;
         }
         labelWorkflowStatus.Text = String.Empty;

         bool changeHost = mrk.ProjectKey.HostName != getHostName();
         if (changeHost && !await restartWorkflowByUrl(url, mrk.ProjectKey.HostName))
         {
            return;
         }

         bool canOpenAtLiveTab =
               // TODO - Opened/WIP should be kept in _liveSesion context and checked here and inside Session
               (mergeRequest.State == "opened")
            && (mergeRequest.Work_In_Progress)
            && checkIfCanOpenAtLiveTab(mrk, true);
         if (!canOpenAtLiveTab || !await openUrlAtLiveTab(mrk, url, !changeHost))
         {
            await openUrlAtSearchTab(mrk, url);
         }
      }

      async private Task<bool> openUrlAtLiveTab(MergeRequestKey mrk, string url, bool updateIfNeeded)
      {
         ISession session = getSession(true);
         if (session == null)
         {
            Debug.Assert(false);
            return false;
         }

         if (!session.MergeRequestCache.GetMergeRequests(mrk.ProjectKey).Any(x => x.IId == mrk.IId))
         {
            // We need to update the MR list here because cached one is possible outdated
            if (updateIfNeeded)
            {
               await checkForUpdatesAsync();
            }

            if (!checkIfCanOpenAtLiveTab(mrk, false))
            {
               // this may happen if project list changed while we were in 'await'
               // or project has just been disabled in restartWorkflowByUrl()
               return false;
            }
         }

         tabControlMode.SelectedTab = tabPageLive;
         if (!selectMergeRequest(listViewMergeRequests, mrk, true))
         {
            if (!listViewMergeRequests.Enabled)
            {
               // This may happen if Reload is in progress now
               reportErrorOnConnect(url, "Merge Request list is being updated. ", null, false);
            }
            else
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
                     reportErrorOnConnect(url, "Something went wrong. ", null, false);
                     return false;
                  }
               }
               else
               {
                  if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
                  {
                     Debug.Assert(false);
                     Trace.TraceError(String.Format("[MainForm] Cannot open URL {0} by unknown reason", url));
                     reportErrorOnConnect(url, "Something went wrong. ", null, false);
                  }
                  return false;
               }
            }
         }

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

      private MergeRequestKey? parseUrlIntoMergeRequestKey(string url)
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
            reportErrorOnConnect(url, String.Empty, ex, true);
            return null; // URL parsing failed
         }
      }

      private bool isKnownHostInUrl(string hostname, string url)
      {
         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            reportErrorOnConnect(url, String.Format(
               "Cannot connect to host {0} because it is not in the list of known hosts. ", hostname),
               null, true);
            return false; // unknown host
         }
         return true;
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

