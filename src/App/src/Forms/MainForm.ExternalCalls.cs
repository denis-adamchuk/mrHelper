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

      /// <summary>
      /// This flag helps to avoid re-entrance when external connection attempt occurs in the middle of internal
      /// connection procedure.
      /// </summary>
      private bool _suppressExternalConnections;

      async private Task onOpenCommand(string argumentsString)
      {
         string[] arguments = argumentsString.Split('|');
         string url = arguments[1];

         Trace.TraceInformation(String.Format("[Mainform] External request: connecting to URL {0}", url));
         if (_suppressExternalConnections)
         {
            Trace.TraceInformation("[Mainform] Cannot connect to URL because the app is connecting");
            return;
         }

         _suppressExternalConnections = true;
         try
         {
            await connectToUrlAsync(url);
         }
         finally
         {
            _suppressExternalConnections = false;
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

         try
         {
            return await startWorkflowAsync(hostname);
         }
         catch (Exception ex)
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
         }
         return false;
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

         if (MessageBox.Show("Selected project is not in the list of projects. Do you want to add it?", "Warning",
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

         if (MessageBox.Show("Selected project is not enabled. Do you want to enable it?", "Warning",
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

         try
         {
            if (await startSearchWorkflowAsync(mrk.ProjectKey.HostName,
                  new SearchByIId(mrk.ProjectKey.ProjectName, mrk.IId), null))
            {
               selectMergeRequest(listViewFoundMergeRequests, mrk.ProjectKey.ProjectName, mrk.IId, true);
            }
         }
         catch (Exception ex)
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
         }
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

         if (mrk.ProjectKey.HostName != getHostName() && !await restartWorkflowByUrl(url, mrk.ProjectKey.HostName))
         {
            return;
         }

         bool canOpenAtLiveTab = (mergeRequest.State == "opened")
          && (isProjectInTheList(mrk.ProjectKey) || addMissingProject(mrk.ProjectKey))
          && (isEnabledProject(mrk.ProjectKey))  || enableDisabledProject(mrk.ProjectKey);

         await (canOpenAtLiveTab ? openUrlAtLiveTab(mrk, url) : openUrlAtSearchTab(mrk, url));
      }

      async private Task openUrlAtLiveTab(MergeRequestKey mrk, string url)
      {
         ISession session = getSession(true);
         if (session == null)
         {
            Debug.Assert(false);
            return;
         }

         if (!session.MergeRequestCache.GetMergeRequests(mrk.ProjectKey).Any(x => x.IId == mrk.IId))
         {
            // We need to restart the workflow here because we possibly have an outdated list
            // of merge requests in the cache
            if (!await restartWorkflowByUrl(url, mrk.ProjectKey.HostName)
             || !isProjectInTheList(mrk.ProjectKey) || !isEnabledProject(mrk.ProjectKey))
            {
               // Could not restart workflow or...
               // ...this may happen if project list changed while we were in 'await'
               // or project has just been disabled in restartWorkflowByUrl()
               return;
            }
         }

         tabControlMode.SelectedTab = tabPageLive;
         if (!selectMergeRequest(listViewMergeRequests, mrk.ProjectKey.ProjectName, mrk.IId, true))
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
                     return; // user decided to not un-hide merge request
                  }

                  if (!selectMergeRequest(listViewMergeRequests, mrk.ProjectKey.ProjectName, mrk.IId, true))
                  {
                     Debug.Assert(false);
                     Trace.TraceError(String.Format("[MainForm] Cannot open URL {0}, although MR is cached", url));
                     reportErrorOnConnect(url, "Something went wrong. ", null, false);
                  }
               }
               else
               {
                  Debug.Assert(false);
                  Trace.TraceError(String.Format("[MainForm] Cannot open URL {0} by unknown reason", url));
                  reportErrorOnConnect(url, "Something went wrong. ", null, false);
               }
            }
         }
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
         HostComboBoxItem proposedSelectedItem = comboBoxHost.Items.Cast<HostComboBoxItem>().SingleOrDefault(
            x => x.Host == hostname); // `null` if not found
         if (proposedSelectedItem == null || String.IsNullOrEmpty(proposedSelectedItem.Host))
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
         return projects.Any(x => 0 == String.Compare(x.Item1, projectKey.ProjectName, true));
      }

      private static bool isEnabledProject(ProjectKey projectKey)
      {
         IEnumerable<Tuple<string, bool>> projects =
            ConfigurationHelper.GetProjectsForHost(projectKey.HostName, Program.Settings);
         return projects.Where(x => 0 == String.Compare(x.Item1, projectKey.ProjectName, true)).First().Item2;
      }
   }
}

