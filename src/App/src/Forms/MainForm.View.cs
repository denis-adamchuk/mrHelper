using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;
using mrHelper.App.Controls;
using mrHelper.CustomActions;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void prepareControlsToStart()
      {
         disableLiveTabControls();
         disableSearchTabControls();
         disableRecentTabControls();
         disableSelectedMergeRequestControls();

         buttonTimeTrackingStart.Text = buttonStartTimerDefaultText;
         labelOperationStatus.Text = String.Empty;
         labelStorageStatus.Text = String.Empty;
         comboBoxSearchByState.SelectedIndex = 0;

         if (_keywords == null)
         {
            checkBoxShowKeywords.Enabled = false;
         }
         else
         {
            checkBoxShowKeywords.Text = "Keywords: " + String.Join(", ", _keywords);
         }

         if (Program.ServiceManager.GetHelpUrl() != String.Empty)
         {
            linkLabelHelp.Visible = true;
            toolTip.SetToolTip(linkLabelHelp, Program.ServiceManager.GetHelpUrl());
            toolTip.SetToolTip(linkLabelCommitStorageDescription, Program.ServiceManager.GetHelpUrl());
            toolTip.SetToolTip(linkLabelWorkflowDescription, Program.ServiceManager.GetHelpUrl());
         }

         if (Program.ServiceManager.GetBugReportEmail() != String.Empty)
         {
            linkLabelSendFeedback.Visible = true;
            toolTip.SetToolTip(linkLabelSendFeedback, Program.ServiceManager.GetBugReportEmail());
         }

         _timeTrackingTimer.Tick += new System.EventHandler(onTimeTrackingTimer);

         setTooltipsForSearchOptions();
         startClipboardCheckTimer();
         startNewVersionReminderTimer();
         subscribeToNewVersionReminderTimer();
         createListViewContextMenu();
         updateNewVersionStatus();

         forEachListView(listView => listView.SetCurrentUserGetter(hostname => getCurrentUser(hostname)));
      }

      private void forEachListView(Action<MergeRequestListView> action)
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            action(getListView(mode));
         }
      }

      private void prepareSizeToStart()
      {
         if (_startMinimized)
         {
            _forceMaximizeOnNextRestore = Program.Settings.WasMaximizedBeforeClose;
            WindowState = FormWindowState.Minimized;
         }
         else
         {
            WindowState = Program.Settings.WasMaximizedBeforeClose ? FormWindowState.Maximized : FormWindowState.Normal;
         }
         applySavedSplitterDistance();
      }

      private void applySavedSplitterDistance()
      {
         if (WindowState == FormWindowState.Minimized)
         {
            _applySplitterDistanceOnNextRestore = true;
            return;
         }

         if (Program.Settings.MainWindowSplitterDistance != 0
            && splitContainer1.Panel1MinSize <= Program.Settings.MainWindowSplitterDistance
            && splitContainer1.Width - splitContainer1.Panel2MinSize >= Program.Settings.MainWindowSplitterDistance)
         {
            splitContainer1.SplitterDistance = Program.Settings.MainWindowSplitterDistance;
         }

         if (Program.Settings.RightPaneSplitterDistance != 0
            && splitContainer2.Panel1MinSize <= Program.Settings.RightPaneSplitterDistance
            && splitContainer2.Height - splitContainer2.Panel2MinSize >= Program.Settings.RightPaneSplitterDistance)
         {
            splitContainer2.SplitterDistance = Program.Settings.RightPaneSplitterDistance;
         }
      }

      private void selectHost(PreferredSelection preferred)
      {
         if (comboBoxHost.Items.Count == 0)
         {
            return;
         }

         comboBoxHost.SelectedIndex = -1;

         HostComboBoxItem defaultSelectedItem = (HostComboBoxItem)comboBoxHost.Items[comboBoxHost.Items.Count - 1];
         switch (preferred)
         {
            case PreferredSelection.Initial:
               HostComboBoxItem initialSelectedItem = comboBoxHost.Items
                  .Cast<HostComboBoxItem>()
                  .SingleOrDefault(x => x.Host == getInitialHostNameIfKnown()); // `null` if not found
               bool isValidSelection = initialSelectedItem != null && !String.IsNullOrEmpty(initialSelectedItem.Host);
               comboBoxHost.SelectedItem = isValidSelection ? initialSelectedItem : defaultSelectedItem;
               break;

            case PreferredSelection.Latest:
               comboBoxHost.SelectedItem = defaultSelectedItem;
               break;
         }

         updateProjectsListView();
         updateUsersListView();
      }

      private void disableComboBox(ComboBox comboBox, string text)
      {
         comboBox.DroppedDown = false;
         comboBox.SelectedIndex = -1;
         comboBox.Items.Clear();
         comboBox.Enabled = false;

         comboBox.DropDownStyle = ComboBoxStyle.DropDown;
         comboBox.Text = text;
      }

      private void enableComboBox(ComboBox comboBox)
      {
         comboBox.Enabled = true;
         comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      }

      private void updateStorageDependentControlState(MergeRequestKey? mrk)
      {
         bool isEnabled = mrk.HasValue
            && !_mergeRequestsUpdatingByUserRequest.Contains(mrk.Value)
            && _mergeRequestsUpdatingByUserRequest.Count() < Constants.MaxMergeRequestStorageUpdatesInParallel;
         buttonDiscussions.Enabled = isEnabled;
         updateDiffToolButtonState(isEnabled);
      }

      private void updateStorageStatusText(string text, MergeRequestKey? mrk)
      {
         string message = String.IsNullOrEmpty(text) || !mrk.HasValue
            ? String.Empty
            : String.Format("{0} #{1}: {2}", mrk.Value.ProjectKey.ProjectName, mrk.Value.IId.ToString(), text);
         labelStorageStatus.Text = message;
      }

      private void onStorageUpdateStateChange()
      {
         updateAbortGitCloneButtonState();
      }

      private void onStorageUpdateProgressChange(string text, MergeRequestKey mrk)
      {
         if (labelStorageStatus.InvokeRequired)
         {
            Invoke(new Action<string, MergeRequestKey>(onStorageUpdateProgressChange), new object[] { text, mrk });
         }
         else
         {
            _latestStorageUpdateStatus[mrk] = text;

            MergeRequestKey? currentMRK = getMergeRequestKey(null);
            if (currentMRK.HasValue && currentMRK.Value.Equals(mrk))
            {
               updateStorageStatusText(text, mrk);
            }
         }
      }

      private void updateAbortGitCloneButtonState()
      {
         ProjectKey? projectKey = getMergeRequestKey(null)?.ProjectKey ?? null;
         ILocalCommitStorage repo = projectKey.HasValue ? getCommitStorage(projectKey.Value, false) : null;

         bool enabled = repo?.Updater?.CanBeStopped() ?? false;
         linkLabelAbortGitClone.Visible = enabled;
      }

      private void initializeListViewGroups(EDataCacheType mode, string hostname)
      {
         Controls.MergeRequestListView listView = getListView(mode);
         listView.Items.Clear();
         listView.Groups.Clear();

         IEnumerable<ProjectKey> projectKeys = getEnabledProjects(hostname);
         foreach (ProjectKey projectKey in projectKeys)
         {
            listView.CreateGroupForProject(projectKey, false);
         }
      }

      private void updateMergeRequestList(EDataCacheType mode)
      {
         DataCache dataCache = getDataCache(mode);
         IMergeRequestCache mergeRequestCache = dataCache?.MergeRequestCache;
         if (mergeRequestCache == null)
         {
            return;
         }

         MergeRequestListView listView = getListView(mode);
         if (!doesRequireFixedGroupCollection(mode))
         {
            listView.UpdateGroups();
         }
         listView.UpdateItems();

         if (mode == EDataCacheType.Live)
         {
            if (listView.Items.Count > 0 || Program.Settings.DisplayFilterEnabled)
            {
               enableMergeRequestFilterControls(true);
               listView.Enabled = true;
            }
            updateTrayAndTaskBar();
            onLiveMergeRequestListRefreshed();
         }
         else if (listView.Items.Count > 0)
         {
            listView.Enabled = true;
         }
      }

      private void enableMergeRequestFilterControls(bool enabled)
      {
         checkBoxDisplayFilter.Enabled = enabled;
         textBoxDisplayFilter.Enabled = enabled;
      }

      private void enableMergeRequestListControls(bool enabled)
      {
         buttonReloadList.Enabled = enabled;
      }

      private void updateMergeRequestDetails(FullMergeRequestKey? fmkOpt)
      {
         if (!fmkOpt.HasValue)
         {
            richTextBoxMergeRequestDescription.Text = String.Empty;
            linkLabelConnectedTo.Text = String.Empty;
         }
         else
         {
            FullMergeRequestKey fmk = fmkOpt.Value;

            string rawTitle = !String.IsNullOrEmpty(fmk.MergeRequest.Title) ? fmk.MergeRequest.Title : "Title is empty";
            string title = MarkDownUtils.ConvertToHtml(rawTitle, String.Empty, _mdPipeline);

            string rawDescription = !String.IsNullOrEmpty(fmk.MergeRequest.Description)
               ? fmk.MergeRequest.Description : "Description is empty";
            string uploadsPrefix = StringUtils.GetUploadsPrefix(fmk.ProjectKey);
            string description = MarkDownUtils.ConvertToHtml(rawDescription, uploadsPrefix, _mdPipeline);

            string body = String.Format("<b>Title</b><br>{0}<br><b>Description</b><br>{1}", title, description);
            richTextBoxMergeRequestDescription.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
            linkLabelConnectedTo.Text = fmk.MergeRequest.Web_Url;
         }

         richTextBoxMergeRequestDescription.Update();
         toolTip.SetToolTip(linkLabelConnectedTo, linkLabelConnectedTo.Text);
      }

      private void updateTimeTrackingMergeRequestDetails(MergeRequestKey? mrk, DataCache dataCache)
      {
         if (isTrackingTime())
         {
            updateTotalTime(mrk, dataCache);
            return;
         }

         bool enabled = true;
         if (!mrk.HasValue)
         {
            enabled = false;
         }
         else
         {
            User author = dataCache?.MergeRequestCache?.GetMergeRequest(mrk.Value)?.Author;
            string hostname = mrk.Value.ProjectKey.HostName;
            if (!TimeTrackingHelpers.IsTimeTrackingAllowed(author, hostname, getCurrentUser(hostname)))
            {
               enabled = false;
            }
         }

         linkLabelTimeTrackingMergeRequest.Visible = enabled;
         buttonTimeTrackingStart.Enabled = enabled;
         buttonTimeTrackingCancel.Enabled = false;

         if (mrk.HasValue && enabled)
         {
            string title = dataCache?.MergeRequestCache?.GetMergeRequest(mrk.Value)?.Title;
            Debug.Assert(!String.IsNullOrEmpty(title) && !mrk.Value.ProjectKey.Equals(default(ProjectKey)));
            linkLabelTimeTrackingMergeRequest.Text = String.Format("{0}   [{1}]", title, mrk.Value.ProjectKey.ProjectName);
         }

         linkLabelTimeTrackingMergeRequest.Refresh();

         updateTotalTime(mrk, dataCache);
      }

      private void updateTotalTime(MergeRequestKey? mrk, DataCache dataCache)
      {
         if (isTrackingTime())
         {
            labelTimeTrackingTrackedLabel.Text =
               String.Format("Tracked Time: {0}", _timeTracker.Elapsed.ToString(@"hh\:mm\:ss"));
            buttonEditTime.Enabled = false;
            return;
         }

         if (!mrk.HasValue || dataCache?.TotalTimeCache == null)
         {
            labelTimeTrackingTrackedLabel.Text = String.Empty;
            buttonEditTime.Enabled = false;
            return;
         }
         else
         {
            User author = dataCache.MergeRequestCache?.GetMergeRequest(mrk.Value)?.Author;
            string hostname = mrk.Value.ProjectKey.HostName;
            if (!TimeTrackingHelpers.IsTimeTrackingAllowed(author, hostname, getCurrentUser(hostname)))
            {
               labelTimeTrackingTrackedLabel.Text = String.Empty;
               buttonEditTime.Enabled = false;
               return;
            }
         }

         TrackedTime trackedTime = dataCache.TotalTimeCache.GetTotalTime(mrk.Value);
         labelTimeTrackingTrackedLabel.Text = String.Format("Total Time: {0}",
            TimeTrackingHelpers.ConvertTotalTimeToText(trackedTime, true));
         buttonEditTime.Enabled = trackedTime.Amount.HasValue;

         // Update total time column in the table
         getListView(EDataCacheType.Live).Invalidate();
         labelTimeTrackingTrackedLabel.Refresh();
      }

      private void enableMergeRequestActions(bool enabled)
      {
         linkLabelConnectedTo.Enabled = enabled;
         buttonAddComment.Enabled = enabled;
         buttonNewDiscussion.Enabled = enabled;
      }

      private void updateDiffToolButtonState(bool isEnabled)
      {
         string[] selected = revisionBrowser.GetSelectedSha(out _);
         switch (selected.Count())
         {
            case 1:
               buttonDiffTool.Enabled = isEnabled;
               buttonDiffTool.Text = "Diff to Base";
               string targetBranch = getMergeRequest(null)?.Target_Branch;
               if (targetBranch != null)
               {
                  toolTip.SetToolTip(buttonDiffTool, String.Format(
                     "Launch diff tool to compare selected revision to {0}", targetBranch));
               }
               break;

            case 2:
               buttonDiffTool.Enabled = isEnabled;
               buttonDiffTool.Text = "Diff Tool";
               toolTip.SetToolTip(buttonDiffTool, "Launch diff tool to compare selected revisions");
               break;

            case 0:
            default:
               buttonDiffTool.Enabled = false;
               buttonDiffTool.Text = "Diff Tool";
               break;
         }
      }

      private void enableCustomActions(MergeRequestKey? mrk, DataCache dataCache)
      {
         if (!mrk.HasValue)
         {
            foreach (Control control in groupBoxActions.Controls) control.Enabled = false;
            return;
         }

         User author = dataCache?.MergeRequestCache?.GetMergeRequest(mrk.Value)?.Author;
         IEnumerable<string> labels = dataCache?.MergeRequestCache?.GetMergeRequest(mrk.Value)?.Labels;
         IEnumerable<User> approvedBy = dataCache?.MergeRequestCache?.GetApprovals(mrk.Value)?.Approved_By?
            .Select(item => item.User) ?? Array.Empty<User>();
         if (author == null || labels == null || approvedBy == null)
         {
            Debug.Assert(false);
            return;
         }

         foreach (Control control in groupBoxActions.Controls)
         {
            string enabledIfFullString = ((ICommand)control.Tag).EnabledIf;
            string[] enabledIfCollection = enabledIfFullString.Split(',');
            bool isControlEnabled = true;
            foreach (string enabledIf in enabledIfCollection)
            {
               string resolvedEnabledIf =
                  String.IsNullOrEmpty(enabledIf) ? String.Empty : _expressionResolver.Resolve(enabledIf);
               isControlEnabled &= isCustomActionEnabled(approvedBy, labels, author, resolvedEnabledIf);
            }
            control.Enabled = isControlEnabled;

            string visibleIfFullString = ((ICommand)control.Tag).VisibleIf;
            string[] visibleIfCollection = visibleIfFullString.Split(',');
            bool isControlVisible = true;
            foreach (string visibleIf in visibleIfCollection)
            {
               string resolvedVisibleIf =
                  String.IsNullOrEmpty(visibleIf) ? String.Empty : _expressionResolver.Resolve(visibleIf);
               isControlVisible &= isCustomActionEnabled(approvedBy, labels, author, resolvedVisibleIf);
            }
            control.Visible = isControlVisible;
         }

         repositionCustomCommands();
      }

      private void onWindowStateChanged()
      {
         if (WindowState != FormWindowState.Minimized)
         {
            if (_prevWindowState == FormWindowState.Minimized)
            {
               bool isRestoring = WindowState == FormWindowState.Normal
                               || WindowState == FormWindowState.Maximized;
               if (isRestoring && _forceMaximizeOnNextRestore)
               {
                  _forceMaximizeOnNextRestore = false;
                  _prevWindowState = FormWindowState.Maximized; // prevent re-entrance on next line
                  WindowState = FormWindowState.Maximized;
               }

               initializeMergeRequestTabMinimumSizes();

               if (isRestoring && _applySplitterDistanceOnNextRestore)
               {
                  _applySplitterDistanceOnNextRestore = false;
                  applySavedSplitterDistance();
               }
            }
         }

         _prevWindowState = WindowState;
      }

      private void onDataCacheSelectionChanged()
      {
         forEachListView(listView => listView.DeselectAllListViewItems());
      }

      private void onMergeRequestSelectionChanged(EDataCacheType mode)
      {
         MergeRequestListView listView = getListView(mode);
         FullMergeRequestKey? fmkOpt = listView.GetSelectedMergeRequest();
         if (!fmkOpt.HasValue)
         {
            Trace.TraceInformation(String.Format(
               "[MainForm] User deselected merge request. Mode={0}",
               getCurrentTabDataCacheType().ToString()));
            disableSelectedMergeRequestControls();
            return;
         }

         FullMergeRequestKey fmk = fmkOpt.Value;
         if (fmk.MergeRequest == null)
         {
            return; // List view item with summary information for a collapsed group
         }

         Trace.TraceInformation(String.Format(
            "[MainForm] User requested to change merge request to IId {0}, mode = {1}",
            fmk.MergeRequest.IId.ToString(), getCurrentTabDataCacheType().ToString()));

         DataCache dataCache = getDataCache(mode);
         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         enableCustomActions(mrk, dataCache);
         enableMergeRequestActions(true);
         updateMergeRequestDetails(fmk);
         updateTimeTrackingMergeRequestDetails(mrk, dataCache);
         updateAbortGitCloneButtonState();

         string status = _latestStorageUpdateStatus.TryGetValue(mrk, out string value) ? value : String.Empty;
         updateStorageStatusText(status, mrk);
         updateStorageDependentControlState(mrk);
         updateRevisionBrowserTree(dataCache, mrk);

         if (mode == EDataCacheType.Live)
         {
            _lastMergeRequestsByHosts[fmk.ProjectKey.HostName] = mrk;
            saveState();
         }
      }

      private void updateCaption()
      {
         string mainCaption = Constants.MainWindowCaption;
         string currentVersion = " (" + Application.ProductVersion + ")";
         string newVersion = StaticUpdateChecker.NewVersionInformation != null
              ? String.Format("   New version {0} is available!", StaticUpdateChecker.NewVersionInformation.VersionNumber)
              : String.Empty;
         Text = String.Format("{0} {1} {2}", mainCaption, currentVersion, newVersion);
      }


      private void applyConnectionStatus(string text, Color color, string tooltipText)
      {
         labelConnectionStatus.Text = text;
         labelConnectionStatus.ForeColor = color;
         toolTip.SetToolTip(labelConnectionStatus, tooltipText);
      }

      Icon getCachedIcon(Color color)
      {
         if (_iconCache.TryGetValue(color, out IconGroup icon))
         {
            bool useBorder = WinFormsHelpers.IsLightThemeUsed();
            return useBorder ? icon.IconWithBorder : icon.IconWithoutBorder;
         }
         return null;
      }

      private void setNotifyIconByColor(Color? colorOpt)
      {
         if (colorOpt == null)
         {
            notifyIcon.Icon = Properties.Resources.DefaultAppIcon;
            return;
         }

         Icon icon = getCachedIcon(colorOpt.Value);
         if (icon == null)
         {
            notifyIcon.Icon = Properties.Resources.DefaultAppIcon;
            return;
         }

         notifyIcon.Icon = icon;
      }

      private void updateTrayAndTaskBar()
      {
         void applyColor(Color? colorOpt)
         {
            if (colorOpt != null)
            {
               setNotifyIconByColor(colorOpt.Value);
               WinFormsHelpers.SetOverlayEllipseIcon(colorOpt.Value);
            }
            else
            {
               setNotifyIconByColor(null);
               WinFormsHelpers.SetOverlayEllipseIcon(null);
            }
         }

         if (_colorScheme == null)
         {
            applyColor(null);
         }
         else if (isConnectionLost())
         {
            applyColor(_colorScheme.GetColor("Status_LostConnection")?.Color);
         }
         else if (isTrackingTime())
         {
            applyColor(_colorScheme.GetColor("Status_Tracking")?.Color);
         }
         else
         {
            applyColor(getListView(EDataCacheType.Live).GetSummaryColor());
         }
      }

      private void applyTheme(string theme)
      {
         string cssEx = String.Format("body div {{ font-size: {0}px; }}",
            CommonControls.Tools.WinFormsHelpers.GetFontSizeInPixels(richTextBoxMergeRequestDescription));

         if (theme == "New Year 2020")
         {
            pictureBox1.BackgroundImage = mrHelper.App.Properties.Resources.PleaseInspect;
            pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            pictureBox1.Visible = true;
            pictureBox2.BackgroundImage = mrHelper.App.Properties.Resources.Tree;
            pictureBox2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            pictureBox2.Visible = true;
            forEachListView(listView => listView.BackgroundImage = mrHelper.App.Properties.Resources.SnowflakeBg);
            forEachListView(listView => listView.BackgroundImageTiled = true);
            richTextBoxMergeRequestDescription.BaseStylesheet =
               String.Format("{0}{1}{2}", mrHelper.App.Properties.Resources.NewYear2020_CSS,
                  mrHelper.App.Properties.Resources.Common_CSS, cssEx);
         }
         else
         {
            pictureBox1.BackgroundImage = null;
            pictureBox1.Visible = false;
            pictureBox2.BackgroundImage = null;
            pictureBox2.Visible = false;
            forEachListView(listView => listView.BackgroundImage = null);
            richTextBoxMergeRequestDescription.BaseStylesheet =
               String.Format("{0}{1}", mrHelper.App.Properties.Resources.Common_CSS, cssEx);
         }

         Program.Settings.VisualThemeName = theme;
      }

      private void updateUsersListView()
      {
         listViewUsers.Items.Clear();

         foreach (string label in ConfigurationHelper.GetEnabledUsers(getHostName(), Program.Settings))
         {
            listViewUsers.Items.Add(label);
         }
      }

      private void updateProjectsListView()
      {
         listViewProjects.Items.Clear();

         foreach (string projectName in
            ConfigurationHelper.GetEnabledProjectNames(getHostName(), Program.Settings)
            .Select(x => x))
         {
            listViewProjects.Items.Add(projectName);
         }
      }

      private void resetMergeRequestTabMinimumSizes()
      {
         _initializedMinimumSizes = false;
      }

      private bool _initializedMinimumSizes = true;

      private int getRightPaneMinWidth()
      {
         int calcMinWidthOfControlGroup(IEnumerable<Control> controls, int minGap) =>
            controls.Cast<Control>().Sum(x => x.MinimumSize.Width) + (controls.Count() - 1) * minGap;

         int calcHorzDistance(Control leftControl, Control rightControl) =>
            rightControl.Location.X - (leftControl.Location.X + leftControl.Size.Width);
         int buttonMinDistance = calcHorzDistance(buttonAddComment, buttonNewDiscussion);

         int groupBoxReviewMinWidth =
            calcMinWidthOfControlGroup(groupBoxReview.Controls.Cast<Control>(), buttonMinDistance)
            + buttonAddComment.Left
               * 2; // for symmetry

         // TODO No idea how to make it more flexible, leave a fixed number so far
         int maximumNumberOfVisibleCustomActionControl = 7;
         bool hasActions = groupBoxActions.Controls.Count > 0;

         // If even we don't have actions, reserve some space for them
         int potentialWidth = getCustomActionButtonSize().Width;
         int defaultWidthOfCustomActionControl = hasActions ? groupBoxActions.Controls[0].Width : potentialWidth;

         int groupBoxActionsMinWidth =
            maximumNumberOfVisibleCustomActionControl * defaultWidthOfCustomActionControl
            + (maximumNumberOfVisibleCustomActionControl - 1) * buttonMinDistance
            + (hasActions ? buttonAddComment.Left : 0) // First button is aligned with "Add a comment"
               * 2; // for symmetry

         return Enumerable.Max(new int[]{ groupBoxReviewMinWidth, groupBoxActionsMinWidth });
      }

      int calcVertDistance(Control topControl, Control bottomControl) =>
         bottomControl.Location.Y - (topControl.Location.Y + topControl.Size.Height);

      private int getTopRightPaneMinHeight()
      {
         return 100 /* cannot use richTextBoxMergeRequestDescription.MinimumSize.Height, see 9b65d7413c */
               + calcVertDistance(richTextBoxMergeRequestDescription, linkLabelConnectedTo)
               + linkLabelConnectedTo.Height
               + 20;
      }

      private int getBottomRightPaneMinHeight()
      {
         bool hasPicture1 = pictureBox1.BackgroundImage != null;
         bool hasPicture2 = pictureBox2.BackgroundImage != null;

         int panelFreeSpaceMinHeight =
            Math.Max(
               (hasPicture1 ?  pictureBox1.Top * 2 // for symmetry
                + pictureBox1.MinimumSize.Height : panelFreeSpace.MinimumSize.Height),
               (hasPicture2 ?
                  pictureBox2.Top * 2 // for symmetry
                + pictureBox2.MinimumSize.Height : panelFreeSpace.MinimumSize.Height));

         return
              groupBoxSelectRevisions.Top
            + groupBoxSelectRevisions.Height
            + calcVertDistance(groupBoxSelectRevisions, groupBoxReview)
            + groupBoxReview.Height
            + calcVertDistance(groupBoxReview, groupBoxTimeTracking)
            + groupBoxTimeTracking.Height
            + calcVertDistance(groupBoxTimeTracking, groupBoxActions)
            + groupBoxActions.Height
            + calcVertDistance(groupBoxActions, panelFreeSpace)
            + panelFreeSpaceMinHeight
            + calcVertDistance(panelFreeSpace, panelStatusBar)
            + panelStatusBar.Height
            + calcVertDistance(panelStatusBar, panelBottomMenu)
            + panelBottomMenu.Height;
      }

      private static void setSplitterPanelsMinSize(SplitContainer splitContainer, int panel1MinSize, int panel2MinSize)
      {
         splitContainer.Panel1MinSize = panel1MinSize;
         splitContainer.Panel2MinSize = panel2MinSize;
         int splitContainerSize = splitContainer.Orientation == Orientation.Vertical
            ? splitContainer.Width : splitContainer.Height;
         bool canResetToMinimum = panel1MinSize + panel2MinSize <= splitContainerSize;
         splitContainer.SplitterDistance = canResetToMinimum ? splitContainerSize - panel2MinSize : panel1MinSize;
      }

      private void initializeMergeRequestTabMinimumSizes()
      {
         if (WindowState == FormWindowState.Minimized)
         {
            resetMergeRequestTabMinimumSizes();
            return;
         }

         if (_initializedMinimumSizes || tabControl.SelectedTab != tabPageMR)
         {
            return;
         }

         _initializedMinimumSizes = true;

         // KISS
         int leftPaneMinWidth = 200;
         int rightPaneMinWidth = getRightPaneMinWidth();
         int topRightPaneMinHeight = getTopRightPaneMinHeight();
         int bottomRightPaneMinHeight = getBottomRightPaneMinHeight();

         int clientAreaMinWidth = leftPaneMinWidth + rightPaneMinWidth;
         int nonClientAreaWidth = 50;
         int clientAreaMinHeight = topRightPaneMinHeight + bottomRightPaneMinHeight;
         int nonClientAreaHeight = 150;

         int minimumWidth = clientAreaMinWidth + nonClientAreaWidth;
         int minimumHeight = clientAreaMinHeight + nonClientAreaHeight;
         if (Program.Settings.DisableSplitterRestrictions
          || Screen.GetWorkingArea(this).Width < minimumWidth
          || Screen.GetWorkingArea(this).Height < minimumHeight)
         {
            MinimumSize = new Size(0, 0);
            int defaultPanelSize = 25; // from documentation
            setSplitterPanelsMinSize(splitContainer1, defaultPanelSize, defaultPanelSize);
            setSplitterPanelsMinSize(splitContainer2, defaultPanelSize, defaultPanelSize);
            return;
         }

         // Setting MinimumSize here adjusts Splitter Height/Width so that it is safe to change its Panel Sizes
         MinimumSize = new Size(minimumWidth, minimumHeight);
         setSplitterPanelsMinSize(splitContainer1, leftPaneMinWidth, rightPaneMinWidth);
         setSplitterPanelsMinSize(splitContainer2, topRightPaneMinHeight, bottomRightPaneMinHeight);
      }

      private void repositionCustomCommands()
      {
         Control[] visibleControls = groupBoxActions
            .Controls
            .Cast<Control>()
            .Where(control => control.Visible)
            .ToArray();

         int controlWidth = getCustomActionButtonSize().Width;
         int totalFreeSpaceWidth = groupBoxActions.Width - visibleControls.Count() * controlWidth;
         int controlHorzPadding = totalFreeSpaceWidth / (visibleControls.Count() + 1);
         for (int index = 0; index < visibleControls.Count(); ++index)
         {
            Control control = visibleControls[index];
            int controlX = controlHorzPadding + index * (controlHorzPadding + controlWidth);
            control.Location = new Point { X = controlX, Y = control.Location.Y };
         }
      }

      private void updateRevisionBrowserTree(DataCache dataCache, MergeRequestKey mrk)
      {
         IMergeRequestCache cache = dataCache.MergeRequestCache;
         if (cache != null)
         {
            GitLabSharp.Entities.Version latestVersion = cache.GetLatestVersion(mrk);
            IEnumerable<GitLabSharp.Entities.Version> versions = cache.GetVersions(mrk);
            IEnumerable<Commit> commits = cache.GetCommits(mrk);

            bool hasObjects = commits.Any() || versions.Any();
            if (hasObjects)
            {
               RevisionBrowserModelData data = new RevisionBrowserModelData(latestVersion?.Base_Commit_SHA,
                  commits, versions, getReviewedRevisions(mrk));
               revisionBrowser.SetData(data, ConfigurationHelper.GetDefaultRevisionType(Program.Settings));
            }
            else
            {
               revisionBrowser.ClearData(ConfigurationHelper.GetDefaultRevisionType(Program.Settings));
            }
         }
      }

      private void disableLiveTabControls()
      {
         getListView(EDataCacheType.Live).DisableListView();
         setMergeRequestEditEnabled(false);
         enableMergeRequestFilterControls(false);
         enableMergeRequestListControls(false);
      }

      private void disableSearchTabControls()
      {
         getListView(EDataCacheType.Search).DisableListView();
         enableSimpleSearchControls(false);
         setSearchByProjectEnabled(false);
         setSearchByAuthorEnabled(false, getHostName());
         updateSearchButtonState();
      }

      private void enableSearchTabControls()
      {
         enableSimpleSearchControls(true);
         setSearchByAuthorEnabled(getDataCache(EDataCacheType.Live)?.UserCache?.GetUsers()?.Any() ?? false, getHostName());
         setSearchByProjectEnabled(getDataCache(EDataCacheType.Live)?.ProjectCache?.GetProjects()?.Any() ?? false);
         updateSearchButtonState();
      }

      private void disableRecentTabControls()
      {
         getListView(EDataCacheType.Recent).DisableListView();
      }

      private void disableSelectedMergeRequestControls()
      {
         enableCustomActions(null, null);
         enableMergeRequestActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(null, null);
         updateAbortGitCloneButtonState();

         updateStorageStatusText(null, null);
         updateStorageDependentControlState(null);
         revisionBrowser.ClearData(ConfigurationHelper.GetDefaultRevisionType(Program.Settings));
      }

      private void setMergeRequestEditEnabled(bool enabled)
      {
         buttonCreateNew.Enabled = enabled;
         MergeRequestListViewContextMenu contextMenu = getListView(EDataCacheType.Live).GetContextMenu();
         contextMenu?.SetEditActionEnabled(enabled);
         contextMenu?.SetMergeActionEnabled(enabled);
      }

      private void enableSimpleSearchControls(bool enabled)
      {
         // unlike comboBoxUser and checkBoxSearchByProject, the following controls don't depend on external data
         checkBoxSearchByTargetBranch.Enabled = enabled;
         textBoxSearchTargetBranch.Enabled = enabled;
         checkBoxSearchByTitleAndDescription.Enabled = enabled;
         textBoxSearchText.Enabled = enabled;
         comboBoxSearchByState.Enabled = enabled;
         labelSearchByState.Enabled = enabled;
      }

      private void updateSearchButtonState()
      {
         buttonSearch.Enabled =
              (checkBoxSearchByAuthor.Enabled
            && checkBoxSearchByAuthor.Checked
            && comboBoxUser.Enabled
            && comboBoxUser.SelectedItem != null)
         ||   (checkBoxSearchByProject.Enabled
            && checkBoxSearchByProject.Checked
            && comboBoxProjectName.Enabled
            && comboBoxProjectName.SelectedItem != null)
         ||   (checkBoxSearchByTargetBranch.Enabled
            && checkBoxSearchByTargetBranch.Checked
            && textBoxSearchTargetBranch.Enabled
            && !String.IsNullOrWhiteSpace(textBoxSearchTargetBranch.Text))
         ||   (checkBoxSearchByTitleAndDescription.Enabled
            && checkBoxSearchByTitleAndDescription.Checked
            && textBoxSearchText.Enabled
            && !String.IsNullOrWhiteSpace(textBoxSearchText.Text));
      }

      private void onSearchTextBoxKeyDown(Keys keys)
      {
         updateSearchButtonState();
         if (keys == Keys.Enter && buttonSearch.Enabled)
         {
            onStartSearch();
         }
      }

      private void onSearchTextBoxTextChanged(TextBox textBox, CheckBox associatedCheckBox)
      {
         associatedCheckBox.Checked = textBox.TextLength > 0;
      }

      private void onSearchComboBoxSelectionChangeCommitted(CheckBox associatedCheckBox)
      {
         associatedCheckBox.Checked = true;
      }

      private void applyKnownHostSelectionChange()
      {
         bool enableRemoveButton = listViewKnownHosts.SelectedItems.Count > 0;
         buttonRemoveKnownHost.Enabled = enableRemoveButton;
      }

      private bool isUserMovingSplitter(SplitContainer splitter)
      {
         Debug.Assert(splitter == splitContainer1 || splitter == splitContainer2);

         return splitter == splitContainer1 ? _userIsMovingSplitter1 : _userIsMovingSplitter2;
      }

      private void onUserIsMovingSplitter(SplitContainer splitter, bool value)
      {
         Debug.Assert(splitter == splitContainer1 || splitter == splitContainer2);

         if (splitter == splitContainer1)
         {
            if (!value)
            {
               // move is finished, store the value
               Program.Settings.MainWindowSplitterDistance = splitter.SplitterDistance;
            }
            _userIsMovingSplitter1 = value;
         }
         else
         {
            if (!value)
            {
               // move is finished, store the value
               Program.Settings.RightPaneSplitterDistance = splitter.SplitterDistance;
            }
            _userIsMovingSplitter2 = value;
         }
      }

      private void onTimerStarted()
      {
         buttonTimeTrackingStart.Text = buttonStartTimerTrackingText;
         buttonTimeTrackingStart.BackColor = System.Drawing.Color.LightGreen;
         buttonTimeTrackingCancel.Enabled = true;
         buttonTimeTrackingCancel.BackColor = System.Drawing.Color.Tomato;
      }

      private void onTimerStopped()
      {
         buttonTimeTrackingStart.Text = buttonStartTimerDefaultText;
         buttonTimeTrackingStart.BackColor = System.Drawing.Color.Transparent;
         buttonTimeTrackingCancel.Enabled = false;
         buttonTimeTrackingCancel.BackColor = System.Drawing.Color.Transparent;

         updateTimeTrackingMergeRequestDetails(
            getMergeRequestKey(null), getDataCache(getCurrentTabDataCacheType()));

         updateTrayAndTaskBar();

         Debug.Assert(!_applicationUpdateNotificationPostponedTillTimerStop
                   || !_applicationUpdateReminderPostponedTillTimerStop); // cannot have both enabled
         if (_applicationUpdateNotificationPostponedTillTimerStop)
         {
            notifyAboutNewVersion();
         }
         else if (_applicationUpdateReminderPostponedTillTimerStop)
         {
            remindAboutNewVersion();
         }
      }

      private void processFontChange()
      {
         if (!Created)
         {
            return;
         }

         Trace.TraceInformation(String.Format("[MainForm] Font changed, new emSize = {0}", Font.Size));
         CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         // see 9b65d7413c
         if (richTextBoxMergeRequestDescription.Location.X < 0
          || richTextBoxMergeRequestDescription.Location.Y < 0)
         {
            Trace.TraceWarning(
                  "Detected negative Location of Html Panel. "
                + "Location: {{{0}, {1}}}, Size: {{{2}, {3}}}. GroupBox Size: {{{4}, {5}}}",
               richTextBoxMergeRequestDescription.Location.X,
               richTextBoxMergeRequestDescription.Location.Y,
               richTextBoxMergeRequestDescription.Size.Width,
               richTextBoxMergeRequestDescription.Size.Height,
               groupBoxSelectedMR.Size.Width,
               groupBoxSelectedMR.Size.Height);
            Debug.Assert(false);
         }

         updateMergeRequestList(EDataCacheType.Live); // update row height of List View
         updateMergeRequestList(EDataCacheType.Search); // update row height of List View
         applyTheme(Program.Settings.VisualThemeName); // update CSS in MR Description
         resetMergeRequestTabMinimumSizes();
      }

      private void processDpiChange()
      {
         Trace.TraceInformation(String.Format("[MainForm] DPI changed, new DPI = {0}", DeviceDpi));
            CommonControls.Tools.WinFormsHelpers.LogScaleDimensions(this);

         _trayIcon.ShowTooltipBalloon(new TrayIcon.BalloonText
         (
            "System DPI has changed",
            "It is recommended to restart application to update layout"
         ));
      }

      private void gotoTimeTrackingMergeRequest()
      {
         if (_timeTracker == null || !_timeTrackingMode.HasValue)
         {
            return;
         }
         switchTabAndSelectMergeRequest(_timeTrackingMode.Value, _timeTracker.MergeRequest);
      }

      private void formatHostListItem(ListControlConvertEventArgs e)
      {
         HostComboBoxItem item = (HostComboBoxItem)(e.ListItem);
         e.Value = item.Host;
      }

      private static void formatProjectListItem(ListControlConvertEventArgs e)
      {
         e.Value = e.ListItem.ToString();
      }

      private static void formatUserListItem(ListControlConvertEventArgs e)
      {
         e.Value = (e.ListItem as User).Name;
      }

      private void formatColorSchemeItemSelectorItem(ListControlConvertEventArgs e)
      {
         ColorSchemeItem colorSchemeItem = _colorScheme.GetColor(e.ListItem.ToString());
         e.Value = colorSchemeItem == null ? e.ListItem as string : colorSchemeItem.DisplayName;
      }

      private void onUpdating()
      {
         buttonReloadList.Text = "Updating...";
         enableMergeRequestListControls(false);
      }

      private void onUpdated(string oldButtonText)
      {
         buttonReloadList.Text = oldButtonText;
         enableMergeRequestListControls(true);
      }

      private void selectCurrentUserInSearchDropdown()
      {
         foreach (object item in comboBoxUser.Items)
         {
            if ((item as User).Name == getCurrentUser().Name)
            {
               comboBoxUser.SelectedItem = item;
               checkBoxSearchByAuthor.Checked = true;
               break;
            }
         }
      }

      private void updateTabControlSelection()
      {
         bool areAnyHosts = listViewKnownHosts.Items.Count > 0;
         bool isStorageValid = textBoxStorageFolder.Text.Length > 0 && Directory.Exists(textBoxStorageFolder.Text);
         if (areAnyHosts && isStorageValid)
         {
            tabControl.SelectedTab = tabPageMR;
         }
         else if (!isStorageValid)
         {
            tabControl.SelectedTab = tabPageSettings;
            tabControlSettings.SelectedTab = tabPageSettingsStorage;
         }
         else if (!areAnyHosts)
         {
            tabControl.SelectedTab = tabPageSettings;
            tabControlSettings.SelectedTab = tabPageSettingsAccessTokens;
         }
      }

      private void placeControlNearToRightmostTab(TabControl tabControl, Control control, int horzOffset)
      {
         int tabCount = tabControl.TabPages.Count;
         Debug.Assert(tabCount > 0);

         Rectangle tabRect = tabControl.GetTabRect(tabCount - 1);

         int controlTopRelativeToTabRect = tabRect.Height / 2 - linkLabelFromClipboard.Height / 2;
         int controlTop = tabRect.Top + controlTopRelativeToTabRect;

         int controlLeft = tabRect.X + tabRect.Width + horzOffset;

         control.Location = new System.Drawing.Point(controlLeft, controlTop);
      }

      private void switchTabAndSelectMergeRequestOrAnythingElse(EDataCacheType mode, MergeRequestKey? mrk)
      {
         switchMode(mode).SelectMergeRequest(mrk, false);
      }

      private bool switchTabAndSelectMergeRequest(EDataCacheType mode, MergeRequestKey? mrk)
      {
         return switchMode(mode).SelectMergeRequest(mrk, true);
      }

      private MergeRequestListView switchMode(EDataCacheType mode)
      {
         if (mode != getCurrentTabDataCacheType())
         {
            switch (mode)
            {
               case EDataCacheType.Live:
                  tabControlMode.SelectedTab = tabPageLive;
                  return listViewLiveMergeRequests;

               case EDataCacheType.Search:
                  tabControlMode.SelectedTab = tabPageSearch;
                  return listViewFoundMergeRequests;

               case EDataCacheType.Recent:
                  tabControlMode.SelectedTab = tabPageRecent;
                  return listViewRecentMergeRequests;

               default:
                  Debug.Assert(false);
                  break;
            }
         }
         return getListView(getCurrentTabDataCacheType());
      }

      private void clearCustomActionControls()
      {
         groupBoxActions.Controls.Clear();
      }

      private void updateCustomActionControls()
      {
         BeginInvoke(new Action(async () =>
         {
            string customActionFileName = await getCustomActionFileNameAsync();
            IEnumerable<ICommand> commands = loadCustomCommands(customActionFileName, this);
            recreateCustomActionControls(commands);
            repositionCustomCommands();

            resetMergeRequestTabMinimumSizes();
            initializeMergeRequestTabMinimumSizes();
            applySavedSplitterDistance();

            onMergeRequestSelectionChanged(getCurrentTabDataCacheType());
         }), null);
      }

      private Size getCustomActionButtonSize()
      {
         SizeF rate = WinFormsHelpers.GetAutoScaleDimensionsChangeRate(this);
         return new System.Drawing.Size
         {
            Width = Convert.ToInt32(64 * rate.Width),
            Height = Convert.ToInt32(40 * rate.Height)
         };
      }

      private void recreateCustomActionControls(IEnumerable<ICommand> commands)
      {
         clearCustomActionControls();
         if (commands == null)
         {
            return;
         }

         int id = 0;
         Size buttonSize = getCustomActionButtonSize();
         foreach (ICommand command in commands)
         {
            string name = command.Name;
            var button = new System.Windows.Forms.Button
            {
               Name = "customAction" + id,
               Location = new System.Drawing.Point { X = 0, Y = 19 },
               Size = buttonSize,
               MinimumSize = buttonSize,
               MaximumSize = buttonSize,
               Text = name,
               UseVisualStyleBackColor = true,
               TabStop = false,
               Tag = command,
               Visible = command.InitiallyVisible
            };
            toolTip.SetToolTip(button, command.Hint);
            button.Click += async (x, y) =>
            {
               MergeRequestKey? mergeRequestKey = getMergeRequestKey(null);
               if (!mergeRequestKey.HasValue)
               {
                  return;
               }

               addOperationRecord(String.Format("Command {0} execution has started", name));
               try
               {
                  await command.Run();
               }
               catch (Exception ex) // Whatever happened in Run()
               {
                  string errorMessage = "Custom action failed";
                  ExceptionHandlers.Handle(errorMessage, ex);
                  MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  addOperationRecord(String.Format("Command {0} failed", name));
                  return;
               }

               string statusMessage = String.Format(
                  "Command {0} execution has completed for merge request !{1} in project {2}",
                  name, mergeRequestKey.Value.IId, mergeRequestKey.Value.ProjectKey.ProjectName);
               addOperationRecord(statusMessage);
               Trace.TraceInformation("[MainForm] EnabledIf: {0}", command.EnabledIf);
               Trace.TraceInformation("[MainForm] VisibleIf: {0}", command.VisibleIf);

               if (command.StopTimer)
               {
                  await stopTimeTrackingTimerAsync();
               }

               bool reload = command.Reload;
               if (reload)
               {
                  requestUpdates(EDataCacheType.Live, mergeRequestKey, new int[] {
                     Program.Settings.OneShotUpdateFirstChanceDelayMs,
                     Program.Settings.OneShotUpdateSecondChanceDelayMs });
               }

               ensureMergeRequestInRecentDataCache(mergeRequestKey.Value);
            };
            groupBoxActions.Controls.Add(button);
            id++;
         }
      }

      private void onRedrawTimer(object sender, EventArgs e)
      {
         foreach (EDataCacheType mode in Enum.GetValues(typeof(EDataCacheType)))
         {
            getListView(mode).Refresh();
         }

         revisionBrowser.Refresh();

         updateRefreshButtonToolTip();
      }
   }
}

