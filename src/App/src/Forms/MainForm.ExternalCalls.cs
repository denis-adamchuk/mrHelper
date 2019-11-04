using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitLabSharp;
using mrHelper.Core.Interprocess;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;
using mrHelper.Client.Discussions;
using mrHelper.Client.Git;

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
            ExceptionHandlers.Handle(ex, "Cannot de-serialize snapshot");
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
            ExceptionHandlers.Handle(ex, "Cannot parse diff tool arguments");
            MessageBox.Show("Bad arguments passed from diff tool", "Cannot create a discussion",
               MessageBoxButtons.OK, MessageBoxIcon.Error,
               MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            return;
         }

         Common.Interfaces.IGitRepository gitRepository = null;
         if (_gitClientFactory.ParentFolder == snapshot.TempFolder)
         {
            GitClient client = _gitClientFactory.GetClient(snapshot.Host, snapshot.Project);
            gitRepository = client;
         }

         try
         {
            await handler.HandleAsync(gitRepository);
         }
         catch (DiscussionCreatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot create a discussion from diff tool");
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
         MessageBox.Show(String.Format("{0}Cannot open merge request from URL. Reason: {1}", msg, ex?.Message ?? "N/A"),
            error ? "Error" : "Warning", MessageBoxButtons.OK,
            error ? MessageBoxIcon.Error : MessageBoxIcon.Exclamation,
            MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
         ExceptionHandlers.Handle(ex, String.Format("Cannot open URL {0}", url));
      }

      async private Task<bool> restartWorkflowByUrl(string url, string hostname)
      {
         _initialHostName = hostname;
         selectHost(PreferredSelection.Initial);

         try
         {
            return await startWorkflowAsync(hostname, null);
         }
         catch (Exception ex)
         {
            if (ex is NoProjectsException)
            {
               reportErrorOnConnect(url, String.Format("Check {0} file. ",
                  mrHelper.Common.Constants.Constants.ProjectListFileName), ex, true);
            }
            else if (ex is WorkflowException)
            {
               reportErrorOnConnect(url, String.Empty, ex, true);
            }
            else
            {
               Debug.Assert(false);
            }
         }
         return false;
      }

      private void unhideFilteredMergeRequestAsync(UrlParser.ParsedMergeRequestUrl mergeRequestUrl, string url)
      {
         Trace.TraceInformation("[MainForm] Notify user that MR is hidden");

         if (MessageBox.Show("Merge Request is hidden by filters, do you want to reset them?", "Warning",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
         {
            Trace.TraceInformation("[MainForm] User decided to not reset filters");
            return;
         }

         _lastMergeRequestsByHosts[mergeRequestUrl.Host] =
            new MergeRequestKey
         {
            ProjectKey = new ProjectKey { HostName = mergeRequestUrl.Host, ProjectName = mergeRequestUrl.Project },
            IId = mergeRequestUrl.IId
         };

         checkBoxLabels.Checked = false;

         if (!selectMergeRequest(mergeRequestUrl.Project, mergeRequestUrl.IId, true))
         {
            Trace.TraceError(String.Format("[MainForm] Cannot open URL {0}, although MR is cached", url));
         }
      }

      async private Task connectToUrlAsync(string url)
      {
         if (url == String.Empty)
         {
            return;
         }

         Trace.TraceInformation(String.Format("[MainForm.Workflow] Initializing Workflow with URL {0}", url));

         string prefix = mrHelper.Common.Constants.Constants.CustomProtocolName + "://";
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
            return;
         }

         HostComboBoxItem proposedSelectedItem = comboBoxHost.Items.Cast<HostComboBoxItem>().ToList().SingleOrDefault(
            x => x.Host == mergeRequestUrl.Host);
         if (proposedSelectedItem.Host == String.Empty)
         {
            reportErrorOnConnect(url, String.Format(
               "Cannot connect to host {0} because it is not in the list of known hosts", mergeRequestUrl.Host),
               null, true);
            return;
         }

         // We need to restart the workflow here because we possible have an outdated list of merge requests in the cache
         if (!await restartWorkflowByUrl(url, mergeRequestUrl.Host))
         {
            return;
         }

         if (selectMergeRequest(mergeRequestUrl.Project, mergeRequestUrl.IId, true))
         {
            return;
         }

         ProjectKey projectKey = new ProjectKey { HostName = mergeRequestUrl.Host, ProjectName = mergeRequestUrl.Project };
         if (_mergeRequestManager.GetMergeRequests(projectKey).Any(x => x.IId == mergeRequestUrl.IId))
         {
            unhideFilteredMergeRequestAsync(mergeRequestUrl, url);
         }
         else
         {
            reportErrorOnConnect(url, String.Format(
               "Current version supports connection to URL for Open merge requests of projects listed in {0} only. ",
               mrHelper.Common.Constants.Constants.ProjectListFileName), null, false);
         }
      }
   }
}

