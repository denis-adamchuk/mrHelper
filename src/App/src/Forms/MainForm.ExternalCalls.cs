using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using GitLabSharp;
using mrHelper.App.Helpers;
using mrHelper.App.Interprocess;
using mrHelper.Client.Types;
using mrHelper.Client.Workflow;
using mrHelper.Client.Discussions;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

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

         DiffArgumentParser diffArgumentParser = new DiffArgumentParser(arguments);
         DiffCallHandler handler;
         try
         {
            handler = new DiffCallHandler(diffArgumentParser.Parse(), snapshot, _discussionManager);
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
            gitRepository = _gitClientFactory.GetRepository(snapshot.Host, snapshot.Project);
         }

         try
         {
            await handler.HandleAsync(gitRepository);
         }
         catch (DiscussionCreatorException ex)
         {
            ExceptionHandlers.Handle("Cannot create a discussion from diff tool", ex);
            MessageBox.Show(
               "Something went wrong at GitLab. See Merge Request Helper log files for details",
               "Cannot create a discussion",
               MessageBoxButtons.OK, MessageBoxIcon.Error,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
         }
      }

      async private Task onOpenCommand(string argumentsString)
      {
         string[] arguments = argumentsString.Split('|');
         string url = arguments[1];

         Trace.TraceInformation(String.Format("[Mainform] External request: connecting to URL {0}", url));
         await connectToUrlAsync(url);
      }

      private void reportErrorOnConnect(string url, string msg, Exception ex, bool error)
      {
         string exceptionMessage = ex != null ? ex.Message : String.Empty;
         if (ex is WorkflowException wfex)
         {
            exceptionMessage = wfex.UserMessage;
         }

         string message = String.Format("{0}Cannot open merge request from URL. {1}", msg, exceptionMessage);

         MessageBox.Show(message, error ? "Error" : "Warning", MessageBoxButtons.OK,
            error ? MessageBoxIcon.Error : MessageBoxIcon.Exclamation,
            MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

         ExceptionHandlers.Handle(String.Format("Cannot open URL {0}", url), ex);
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
            else if (ex is WorkflowException)
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

      private bool unhideFilteredMergeRequest(UrlParser.ParsedMergeRequestUrl mergeRequestUrl)
      {
         Trace.TraceInformation("[MainForm] Notify user that MR is hidden");

         if (MessageBox.Show("Merge Request is hidden by filters and cannot be opened. Do you want to reset filters?",
               "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification) != DialogResult.Yes)
         {
            Trace.TraceInformation("[MainForm] User decided not to reset filters");
            return false;
         }

         _lastMergeRequestsByHosts[mergeRequestUrl.Host] =
            new MergeRequestKey
         {
            ProjectKey = new ProjectKey { HostName = mergeRequestUrl.Host, ProjectName = mergeRequestUrl.Project },
            IId = mergeRequestUrl.IId
         };

         checkBoxLabels.Checked = false;
         return true;
      }

      private bool addMissingProject(UrlParser.ParsedMergeRequestUrl mergeRequestUrl)
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
            mergeRequestUrl.Host, Program.Settings).ToDictionary(item => item.Item1, item => item.Item2);
         Debug.Assert(!projects.ContainsKey(mergeRequestUrl.Project));
         projects.Add(mergeRequestUrl.Project, true);

         ConfigurationHelper.SetProjectsForHost(mergeRequestUrl.Host,
            Enumerable.Zip(projects.Keys, projects.Values, (x, y) => new Tuple<string, bool>(x, y)), Program.Settings);
         updateProjectsListView();

         return true;
      }

      private bool enableDisabledProject(UrlParser.ParsedMergeRequestUrl mergeRequestUrl)
      {
         Trace.TraceInformation("[MainForm] Notify that selected project is disabled");

         if (MessageBox.Show("Selected project is not enabled. Do you want to enable it?", "Warning",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification) != DialogResult.Yes)
         {
            Trace.TraceInformation("[MainForm] User decided not to enable project");
            return false;
         }

         changeProjectEnabledState(mergeRequestUrl.Host, mergeRequestUrl.Project, true);

         return true;
      }

      async private Task connectToUrlAsync(string url)
      {
         Trace.TraceInformation(String.Format("[MainForm.Workflow] Initializing Workflow with URL {0}", url));

         string prefix = Constants.CustomProtocolName + "://";
         url = url.StartsWith(prefix) ? url.Substring(prefix.Length) : url;

         UrlParser.ParsedMergeRequestUrl mergeRequestUrl;
         try
         {
            mergeRequestUrl = UrlParser.ParseMergeRequestUrl(url);
            mergeRequestUrl.Host = getHostWithPrefix(mergeRequestUrl.Host);
         }
         catch (Exception ex)
         {
            Debug.Assert(ex is UriFormatException);
            reportErrorOnConnect(url, String.Empty, ex, true);
            return; // URL parsing failed
         }

         HostComboBoxItem proposedSelectedItem = comboBoxHost.Items.Cast<HostComboBoxItem>().ToList().SingleOrDefault(
            x => x.Host == mergeRequestUrl.Host);
         if (proposedSelectedItem.Host == String.Empty)
         {
            reportErrorOnConnect(url, String.Format(
               "Cannot connect to host {0} because it is not in the list of known hosts. ", mergeRequestUrl.Host),
               null, true);
            return; // unknown host
         }

         IEnumerable<Tuple<string, bool>> projects = ConfigurationHelper.GetProjectsForHost(
            mergeRequestUrl.Host, Program.Settings);
         if (!projects.Any(x => x.Item1 == mergeRequestUrl.Project))
         {
            if (!addMissingProject(mergeRequestUrl))
            {
               return; // user decided to not add a missing project
            }
         }
         else if (projects.Where(x => x.Item1 == mergeRequestUrl.Project).First().Item2 == false)
         {
            if (!enableDisabledProject(mergeRequestUrl))
            {
               return; // user decided to not enable a disabled project
            }
         }

         ProjectKey projectKey = new ProjectKey
         {
            HostName = mergeRequestUrl.Host,
            ProjectName = mergeRequestUrl.Project
         };

         if (!_mergeRequestCache.GetMergeRequests(projectKey).Any(x => x.IId == mergeRequestUrl.IId))
         {
            // We need to restart the workflow here because we possibly have an outdated list
            // of merge requests in the cache
            if (!await restartWorkflowByUrl(url, mergeRequestUrl.Host))
            {
               return; // could not restart workflow
            }

            projects = ConfigurationHelper.GetProjectsForHost(mergeRequestUrl.Host, Program.Settings);
            if (!projects.Any(x => x.Item1 == mergeRequestUrl.Project))
            {
               // This may happen if project list changed while we were in 'await'
               return;
            }
            else if (projects.Where(x => x.Item1 == mergeRequestUrl.Project).First().Item2 == false)
            {
               return; // project has just been disabled in restartWorkflowByUrl()
            }
         }

         if (!selectMergeRequest(mergeRequestUrl.Project, mergeRequestUrl.IId, true))
         {
            if (!listViewMergeRequests.Enabled)
            {
               // This may happen if Reload is in progress now
               reportErrorOnConnect(url, "Merge Request list is being updated. ", null, false);
            }
            else
            {
               // We could not select MR, but let's check if it is cached or not.

               if (_mergeRequestCache.GetMergeRequests(projectKey).Any(x => x.IId == mergeRequestUrl.IId))
               {
                  // If it is cached, it is probably hidden by filters and user might want to un-hide it.
                  if (!unhideFilteredMergeRequest(mergeRequestUrl))
                  {
                     return; // user decided to not un-hide merge request
                  }

                  if (!selectMergeRequest(mergeRequestUrl.Project, mergeRequestUrl.IId, true))
                  {
                     Debug.Assert(false);
                     Trace.TraceError(String.Format("[MainForm] Cannot open URL {0}, although MR is cached", url));
                     reportErrorOnConnect(url, "Something went wrong. ", null, false);
                  }
               }
               else
               {
                  // But if this MR is not cached, it is most likely is not in Open state and cannot be shown.
                  reportErrorOnConnect(url, "Current version supports Open merge requests only. ", null, false);
               }
            }
         }
      }
   }
}
