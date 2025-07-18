using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using mrHelper.App.Helpers;
using mrHelper.App.Controls;
using mrHelper.CustomActions;
using mrHelper.Common.Tools;
using System.Threading.Tasks;
using mrHelper.GitLabClient;
using mrHelper.Common.Exceptions;

namespace mrHelper.App.Forms.Helpers
{
   internal interface ITimeTrackerHolder
   {
      ITimeTracker GetTimeTracker();
   }
}

namespace mrHelper.App.Forms
{
   internal partial class MainForm : Helpers.ITimeTrackerHolder
   {
      public ITimeTracker GetTimeTracker()
      {
         return _timeTracker;
      }

      private ConnectionPage getCurrentConnectionPage()
      {
         if (!tabControlHost.Created)
         {
            return null;
         }
         return (tabControlHost.SelectedTab as ConnectionTabPage)?.ConnectionPage;
      }

      private IEnumerable<ConnectionPage> getConnectionPages()
      {
         return tabControlHost.TabPages?
            .Cast<ConnectionTabPage>()
            .Select(tabpage => tabpage.ConnectionPage) ?? Array.Empty<ConnectionPage>();
      }

      private ConnectionPage getConnectionPage(string hostname)
      {
         return getConnectionPages()
            .SingleOrDefault(connectionPage => connectionPage.GetCurrentHostName() == hostname);
      }

      private void connectToUrlFromClipboard()
      {
         string clipboardText = getClipboardText();
         bool isValidUrl = UrlHelper.CheckGitLabMergeRequestUrl(clipboardText);
         if (!isValidUrl)
         {
            return;
         }

         string url = clipboardText;
         Trace.TraceInformation(String.Format("[Mainform] Connecting to URL from clipboard: {0}", url.ToString()));
         reconnect(url);
      }

      private void subscribeToConnectionPage(ConnectionPage connectionPage)
      {
         connectionPage.CanReloadAllChanged += ConnectionPage_CanReloadAllChanged;
         connectionPage.CanCreateNewChanged += ConnectionPage_CanCreateNewChanged;
         connectionPage.CanAddCommentChanged += ConnectionPage_CanAddCommentChanged;
         connectionPage.CanNewThreadChanged += ConnectionPage_CanNewThreadChanged;
         connectionPage.CanEditChanged += ConnectionPage_CanEditChanged;
         connectionPage.CanMergeChanged += ConnectionPage_CanMergeChanged;
         connectionPage.CanToggleHideStatusChanged += ConnectionPage_CanToggleHideStatusChanged;
         connectionPage.CanTogglePinStatusChanged += ConnectionPage_CanTogglePinStatusChanged;
         connectionPage.CanDiscussionsChanged += ConnectionPage_CanDiscussionsChanged;
         connectionPage.CanDiffToolChanged += ConnectionPage_CanDiffToolChanged;
         connectionPage.CanAbortCloneChanged += ConnectionPage_CanAbortCloneChanged;
         connectionPage.CanTrackTimeChanged += ConnectionPage_CanTrackTimeChanged;
         connectionPage.StatusChanged += ConnectionPage_StatusChange;
         connectionPage.StorageStatusChanged += ConnectionPage_StorageStatusChange;
         connectionPage.ConnectionStatusChanged += ConnectionPage_ConnectionStatusChange;
         connectionPage.LatestListRefreshTimestampChanged += ConnectionPage_ListRefreshed;
         connectionPage.SummaryColorChanged += ConnectionPage_SummaryColorChanged;
         connectionPage.EnabledCustomActionsChanged += ConnectionPage_EnabledCustomActionsChanged;
         connectionPage.RequestLive += ConnectionPage_RequestLive;
         connectionPage.RequestRecent += ConnectionPage_RequestRecent;
         connectionPage.RequestSearch += ConnectionPage_RequestSearch;
         connectionPage.CustomActionListChanged += ConnectionPage_CustomActionListChanged;
      }

      private void unsubscribeFromConnectionPage(ConnectionPage connectionPage)
      {
         connectionPage.CanReloadAllChanged -= ConnectionPage_CanReloadAllChanged;
         connectionPage.CanCreateNewChanged -= ConnectionPage_CanCreateNewChanged;
         connectionPage.CanAddCommentChanged -= ConnectionPage_CanAddCommentChanged;
         connectionPage.CanNewThreadChanged -= ConnectionPage_CanNewThreadChanged;
         connectionPage.CanDiscussionsChanged -= ConnectionPage_CanDiscussionsChanged;
         connectionPage.CanDiffToolChanged -= ConnectionPage_CanDiffToolChanged;
         connectionPage.CanAbortCloneChanged -= ConnectionPage_CanAbortCloneChanged;
         connectionPage.CanTrackTimeChanged -= ConnectionPage_CanTrackTimeChanged;
         connectionPage.StatusChanged -= ConnectionPage_StatusChange;
         connectionPage.StorageStatusChanged -= ConnectionPage_StorageStatusChange;
         connectionPage.ConnectionStatusChanged -= ConnectionPage_ConnectionStatusChange;
         connectionPage.LatestListRefreshTimestampChanged -= ConnectionPage_ListRefreshed;
         connectionPage.SummaryColorChanged -= ConnectionPage_SummaryColorChanged;
         connectionPage.EnabledCustomActionsChanged -= ConnectionPage_EnabledCustomActionsChanged;
         connectionPage.RequestLive -= ConnectionPage_RequestLive;
         connectionPage.RequestRecent -= ConnectionPage_RequestRecent;
         connectionPage.RequestSearch -= ConnectionPage_RequestSearch;
         connectionPage.CustomActionListChanged -= ConnectionPage_CustomActionListChanged;
      }

      private void createConnectionPages()
      {
         // need to load old-style Filter once
         UserDefinedSettings.OldFilterSettings oldFilter = Program.Settings.LoadDisplayFilterAndRemoveProperty();
         tabControlHost.SuspendLayout();
         foreach (string hostname in getHostList())
         {
            ConnectionPage connectionPage = new ConnectionPage(hostname,
               _recentMergeRequests, _reviewedRevisions, _lastMergeRequestsByHosts,
               _newMergeRequestDialogStatesByHosts, _collapsedProjectsLive,
               _collapsedProjectsRecent, _collapsedProjectsSearch, _mutedMergeRequests,
               _filtersByHostsLive, _filtersByHostsRecent,
               _keywords, _trayIcon, toolTip,
               _integratedInGitExtensions,
               _integratedInSourceTree, oldFilter, this, onOpenCommand, onCommandAsync, findGlobal);
            subscribeToConnectionPage(connectionPage);
            ConnectionTabPage tabPage = new ConnectionTabPage(hostname, connectionPage);
            tabControlHost.TabPages.Add(tabPage);
         }
         tabControlHost.SelectedTab = null;
         onHostTabSelected(); // disable UI controls
         tabControlHost.ResumeLayout();
      }

      private void disposeAllConnectionPages()
      {
         foreach (ConnectionTabPage tab in tabControlHost.TabPages)
         {
            unsubscribeFromConnectionPage(tab.ConnectionPage);
            tab.Dispose();
         }
         tabControlHost.TabPages.Clear();
      }

      private void createHostToolbarButtons()
      {
         toolStripHosts.SuspendLayout();
         foreach (string hostname in getHostList().Reverse())
         {
            HostToolbarItem toolbarItem = new HostToolbarItem(hostname, this)
            {
               Margin = toolStripButtonLive.Margin,
               ImageAlign = ContentAlignment.MiddleLeft,
               TextAlign = ContentAlignment.MiddleLeft,
               DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
               // see also HostToolbarItem.updateIcon()
            };
            toolbarItem.Click += onHostToolbarButtonClicked;
            toolStripHosts.Items.Insert(0, toolbarItem);
         }
         toolStripHosts.Visible = isToolStripHostsVisible();
         toolStripHosts.ResumeLayout();
      }

      private void emulateClickOnHostToolbarButton(string hostname)
      {
         // This function is called when we need to switch a host programatically.
         // PerformClick() calls onHostToolbarButtonClicked() which in turn calls
         // one of Go*() function. Some Go*() functions select a merge request in the list view,
         // but host is never changed from ConnectionPage, only from MainForm.
         getHostToolbarButtons().SingleOrDefault(item => item.HostName == hostname)?.PerformClick();
      }

      private IEnumerable<HostToolbarItem> getHostToolbarButtons()
      {
         return toolStripHosts.Items
            .Cast<ToolStripItem>()
            .Where(item => item is HostToolbarItem)
            .Select(item => item as HostToolbarItem);
      }

      private void onHostToolbarButtonClicked(object sender, EventArgs e)
      {
         HostToolbarItem button = sender as HostToolbarItem;
         string hostname = button.HostName;
         string currentHostName = getCurrentConnectionPage()?.GetCurrentHostName() ?? String.Empty;
         Trace.TraceInformation(
            "[MainForm] onHostToolbarButtonClicked(), hostname={0}, currentHostName={1}", hostname, currentHostName);

         if (currentHostName == hostname)
         {
            return;
         }

         getCurrentConnectionPage()?.Deactivate();

         ConnectionTabPage tab = tabControlHost.TabPages
            .Cast<ConnectionTabPage>()
            .ToList()
            .SingleOrDefault(item => item.HostName == hostname);
         tabControlHost.SelectedTab = tab;

         getHostToolbarButtons().ToList().ForEach(item => item.Checked = false);
         button.Checked = true;

         _defaultHostName = hostname;

         getCurrentConnectionPage()?.Activate();

         // Host change calls a Go*() function here, what causes a switch of a visible tab inside a connection page.
         synchronizePageWithSelectedMode();

         if (WindowState != FormWindowState.Minimized)
         {
            getCurrentConnectionPage()?.RestoreSplitterDistance();
         }
      }

      private void onHostTabSelected()
      {
         ConnectionPage connectionPage = getCurrentConnectionPage();
         onCustomActionListChanged(connectionPage);
         onCanTrackTimeChanged(connectionPage);
         onCanAbortCloneChanged(connectionPage);
         onCanDiffToolChanged(connectionPage);
         onCanDiscussionsChanged(connectionPage);
         onCanNewThreadChanged(connectionPage);
         onCanAddCommentChanged(connectionPage);
         onCanCreateNewChanged(connectionPage);
         onCanReloadAllChanged(connectionPage);
         onStorageStatusChanged(connectionPage);
         onConnectionStatusChanged(connectionPage);
         onEnabledCustomActionsChanged(connectionPage);
         onCanEditChanged(connectionPage);
         onCanMergeChanged(connectionPage);
         onCanToggleHideStatusChanged(connectionPage);
         onCanTogglePinStatusChanged(connectionPage);
      }

      private void onCanTrackTimeChanged(ConnectionPage connectionPage)
      {
         if (!isTrackingTime() && connectionPage == getCurrentConnectionPage())
         {
            toolStripButtonStartStopTimer.Enabled = connectionPage != null && connectionPage.CanTrackTime();
            toolStripButtonEditTrackedTime.Enabled = connectionPage != null && connectionPage.CanTrackTime();
            toolStripTextBoxTrackedTime.Text = connectionPage?.GetTrackedTimeAsText() ?? DefaultTimeTrackingTextBoxText;
         }
      }

      private void onCanAbortCloneChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            linkLabelAbortGitClone.Visible = connectionPage != null && connectionPage.CanAbortClone();
         }
      }

      private void onCanDiffToolChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            if (connectionPage == null)
            {
               toolStripButtonDiffTool.Enabled = false;
               diffToolToolStripMenuItem.Enabled = false;
               diffToBaseToolStripMenuItem.Enabled = false;
            }
            else
            {
               toolStripButtonDiffTool.Enabled =
                     connectionPage.CanDiffTool(DiffToolMode.DiffBetweenSelected)
                  || connectionPage.CanDiffTool(DiffToolMode.DiffSelectedToBase);
               diffToolToolStripMenuItem.Enabled = toolStripButtonDiffTool.Enabled;
               diffToBaseToolStripMenuItem.Enabled =
                  connectionPage.CanDiffTool(DiffToolMode.DiffSelectedToBase);
            }
         }
      }

      private void onCanDiscussionsChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            toolStripButtonDiscussions.Enabled = connectionPage != null && connectionPage.CanDiscussions();
         }
      }

      private void onCanNewThreadChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            toolStripButtonNewThread.Enabled = connectionPage != null && connectionPage.CanNewThread();
         }
      }

      private void onCanEditChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            toolStripButtonEditMergeRequest.Enabled = connectionPage != null && connectionPage.CanEdit();
         }
      }

      private void onCanMergeChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            toolStripButtonMergeMergeRequest.Enabled = connectionPage != null && connectionPage.CanMerge();
         }
      }

      private void onCanToggleHideStatusChanged(ConnectionPage connectionPage)
      {
         if (connectionPage != getCurrentConnectionPage())
         {
            return;
         }

         if (connectionPage == null)
         {
            toolStripButtonHideMergeRequest.Enabled = false;
            return;
         }

         toolStripButtonHideMergeRequest.Image = ToolbarIconSelector.GetHideIcon();
         toolStripButtonHideMergeRequest.ToolTipText = HideButtonTooltip;

         if (!connectionPage.CanToggleHideStatus())
         {
            toolStripButtonHideMergeRequest.Enabled = false;
            return;
         }

         toolStripButtonHideMergeRequest.Enabled = true;
         if (!connectionPage.IsSelectedMergeRequestHidden())
         {
            return;
         }

         toolStripButtonHideMergeRequest.Image = ToolbarIconSelector.GetUnhideIcon();
         toolStripButtonHideMergeRequest.ToolTipText = UnhideButtonTooltip;
      }

      private void onCanTogglePinStatusChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            if (connectionPage != null)
            {
               toolStripButtonPinMergeRequest.Enabled = connectionPage.CanTogglePinStatus();
               if (connectionPage.IsSelectedMergeRequestPinned())
               {
                  toolStripButtonPinMergeRequest.Image = ToolbarIconSelector.GetUnpinIcon();
                  toolStripButtonPinMergeRequest.ToolTipText = UnpinButtonTooltip;
               }
               else
               {
                  toolStripButtonPinMergeRequest.Image = ToolbarIconSelector.GetPinIcon();
                  toolStripButtonPinMergeRequest.ToolTipText = PinButtonTooltip;
               }
            }
            else
            {
               toolStripButtonPinMergeRequest.Enabled = false;
            }
         }
      }

      private void onCanAddCommentChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            toolStripButtonAddComment.Enabled = connectionPage != null && connectionPage.CanAddComment();
         }
      }

      private void onCanCreateNewChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            toolStripButtonCreateNew.Enabled = connectionPage != null && connectionPage.CanCreateNew();
         }
      }

      private void onCanReloadAllChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            toolStripButtonRefreshList.Enabled = connectionPage != null && connectionPage.CanReloadAll();
         }
      }

      private void onConnectionStatusChanged(ConnectionPage connectionPage)
      {
         Trace.TraceInformation("[MainForm] onConnectionStatusChanged({0})",
            connectionPage?.GetCurrentHostName() ?? "null");

         if (connectionPage == getCurrentConnectionPage())
         {
            if (connectionPage != null)
            {
               ConnectionPage.EConnectionState state = connectionPage.GetConnectionState(out string details);
               processConnectionStatusChange(state, details);
            }
            else
            {
               processConnectionStatusChange(ConnectionPage.EConnectionState.NotConnected, String.Empty);
            }
         }
         onSummaryColorChanged(connectionPage);
      }

      private void onStorageStatusChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            updateStorageStatusLabel(connectionPage?.GetStorageStatus() ?? String.Empty);
         }
      }

      private void onListRefreshed(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            if (connectionPage == null)
            {
               toolStripButtonRefreshList.ToolTipText = String.Empty;
            }
            else
            {
               DateTime? refreshTimestamp = connectionPage.GetLatestListRefreshTimestamp();
               string refreshedAgo = refreshTimestamp.HasValue
                  ? String.Format("Refreshed {0}", TimeUtils.DateTimeToStringAgo(refreshTimestamp.Value))
                  : String.Empty;
               toolStripButtonRefreshList.ToolTipText = String.Format("{0}{1}{2}",
                  RefreshButtonTooltip, refreshedAgo == String.Empty ? String.Empty : "\r\n", refreshedAgo);
            }
         }
      }

      private void onSummaryColorChanged(ConnectionPage connectionPage)
      {
         updateTrayAndTaskBar();
         if (connectionPage != null)
         {
            updateToolbarHostIcon(connectionPage.GetCurrentHostName());
         }
      }

      private void onCustomActionListChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            toolStripCustomActions.SuspendLayout();
            clearCustomActionControls();
            IEnumerable<ICommand> commands = connectionPage?.GetCustomActionList();
            if (commands != null)
            {
               createCustomActionControls(commands);
            }
            toolStripCustomActions.ResumeLayout();
         }
      }

      private void onEnabledCustomActionsChanged(ConnectionPage connectionPage)
      {
         if (connectionPage == getCurrentConnectionPage())
         {
            // to not hide custom actions on MR de-select, we have a special method
            if (connectionPage == null || !connectionPage.AreCommandsEnabled())
            {
               getCustomActionMenuItems()
                  .ToList()
                  .ForEach(item => item.Enabled = false);
               return;
            }

            toolStripCustomActions.SuspendLayout();
            foreach (ToolStripItem menuItem in getCustomActionMenuItems())
            {
               ICommand command = (ICommand)menuItem.Tag;
               CommandState commandState = connectionPage.IsCommandEnabledForSelectedMergeRequest(command);
               menuItem.Enabled = commandState.Enabled;
               menuItem.Visible = commandState.Visible;
            }
            toolStripCustomActions.ResumeLayout();
         }
      }

      private void onOpenCommand(string url)
      {
         Trace.TraceInformation("[Mainform] External request: connecting to URL {0}", url);
         reconnect(url);
      }

      private void reconnect(string url = null)
      {
         if (!getConnectionPages().Any() || String.IsNullOrEmpty(url))
         {
            SuspendLayout();
            disposeAllConnectionPages();
            removeToolbarButtons(toolStripHosts);
            removeToolbarButtons(toolStripCustomActions);
            createConnectionPages();
            createHostToolbarButtons();
            ResumeLayout();
         }

         enqueueUrl(url);
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
         if (!getConnectionPages().Any())
         {
            return;
         }

         string suffix = String.IsNullOrEmpty(url) ? String.Empty : String.Format(" ({0})", url);
         addOperationRecord(String.Format("Reconnection request has been queued {0}", suffix));

         HostToolbarItem defaultHostButton = getHostToolbarButtons()?
            .SingleOrDefault(item => item.HostName == _defaultHostName);
         if (defaultHostButton == null)
         {
            defaultHostButton = getHostToolbarButtons()?.First();
         }
         emulateClickOnHostToolbarButton(defaultHostButton?.HostName);

         await connectAll();

         if (!String.IsNullOrEmpty(url))
         {
            await processNonEmptyUrl(url);
         }
      }

      private Dictionary<string, string> getSourceBranchTemplates()
      {
         return getConnectionPages()?.ToDictionary(
            connectionPage => connectionPage.GetCurrentHostName(),
            ConnectionPage => ConnectionPage.GetSourceBranchTemplate());
      }

      private async Task processNonEmptyUrl(string url)
      {
         try
         {
            object parsed = UrlHelper.Parse(url, getSourceBranchTemplates());
            if (parsed is UrlParser.ParsedMergeRequestUrl parsedMergeRequestUrl)
            {
               throwOnUnknownHost(parsedMergeRequestUrl.Host);
               await connectToUrlAsyncInternal(url, parsedMergeRequestUrl.Host, parsedMergeRequestUrl);
            }
            else if (parsed is ParsedNewMergeRequestUrl parsedNewMergeRequestUrl)
            {
               createMergeRequestFromUrl(parsedNewMergeRequestUrl);
            }
            else if (parsed is UrlParser.ParsedNoteUrl parsedNoteUrl)
            {
               throwOnUnknownHost(parsedNoteUrl.Host);
               await connectToUrlAsyncInternal(url, parsedNoteUrl.Host, parsedNoteUrl);
            }
            else if (parsed == null)
            {
               MessageBox.Show("Failed to parse URL", "Error",
                  MessageBoxButtons.OK, MessageBoxIcon.Error,
                  MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);
            }
            else
            {
               Debug.Assert(false);
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
      }

      private void throwOnUnknownHost(string hostname)
      {
         if (Program.Settings.GetAccessToken(hostname) == String.Empty)
         {
            throw new UrlConnectionException(String.Format(
               "Cannot connect to {0} because it is missing in the list of known hosts. " +
               "Add this host and access token for it in Settings and try again.", hostname));
         }
      }

      private void createMergeRequestFromUrl(ParsedNewMergeRequestUrl parsedNewMergeRequestUrl)
      {
         string hostname = parsedNewMergeRequestUrl.ProjectKey.HostName;
         emulateClickOnHostToolbarButton(hostname);
         if (getCurrentConnectionPage()?.GetCurrentHostName() == hostname)
         {
            getCurrentConnectionPage()?.CreateFromUrl(parsedNewMergeRequestUrl);
         }
         else
         {
            string message = String.Format(
               "{0} is missing in the list of known hosts and cannot be used to create a merge request. " +
               "Add this host and access token for it in Settings and try again.", hostname);
            Trace.TraceWarning("[MainForm] Cannot create merge request at unknown host {0}", hostname);
            MessageBox.Show(message, "Cannot create a merge request", MessageBoxButtons.OK,
               MessageBoxIcon.Information);
         }
      }

      private Task connectToUrlAsyncInternal<T>(string url, string hostname, T parsedUrl)
      {
         emulateClickOnHostToolbarButton(hostname);
         if (getCurrentConnectionPage()?.GetCurrentHostName() == hostname)
         {
            return getCurrentConnectionPage()?.ConnectToUrl(url, parsedUrl);
         }
         return null;
      }

      private Task connectAll()
      {
         return Task.WhenAll(getConnectionPages()
            .Where(connectionPage =>
               connectionPage.GetConnectionState(out var _)
                  == ConnectionPage.EConnectionState.NotConnected)
            .Select(connectionPage => connectionPage.Connect(null)));
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

      private void synchronizePageWithSelectedMode()
      {
         // GoLive -> selectTab(Live) -> tabControlMode_SelectedIndexChanged() -> onDataCacheSelectionChanged
         if (toolStripButtonLive.Checked)
         {
            getCurrentConnectionPage()?.GoLive();
         }
         else if (toolStripButtonRecent.Checked)
         {
            getCurrentConnectionPage()?.GoRecent();
         }
         else if (toolStripButtonSearch.Checked)
         {
            getCurrentConnectionPage()?.GoSearch();
         }
         else
         {
            Debug.Assert(false);
         }
      }
   }
}

