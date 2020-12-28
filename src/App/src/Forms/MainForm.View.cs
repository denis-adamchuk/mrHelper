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
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Controls;

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
            _applySplitterDistanceOnNextRestore = true;
            WindowState = FormWindowState.Minimized;
         }
         else
         {
            WindowState = Program.Settings.WasMaximizedBeforeClose ? FormWindowState.Maximized : FormWindowState.Normal;
            applySavedSplitterDistance();
         }
      }

      private void applySavedSplitterDistance()
      {
         if (Program.Settings.MainWindowSplitterDistance != 0
            && splitContainer1.Panel1MinSize < Program.Settings.MainWindowSplitterDistance
            && splitContainer1.Width - splitContainer1.Panel2MinSize > Program.Settings.MainWindowSplitterDistance)
         {
            splitContainer1.SplitterDistance = Program.Settings.MainWindowSplitterDistance;
         }

         if (Program.Settings.RightPaneSplitterDistance != 0
            && splitContainer2.Panel1MinSize < Program.Settings.RightPaneSplitterDistance
            && splitContainer2.Width - splitContainer2.Panel2MinSize > Program.Settings.RightPaneSplitterDistance)
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
            updateTrayIcon();
            updateTaskbarIcon();
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

      private void updateTimeTrackingMergeRequestDetails(bool enabled, string title, ProjectKey projectKey, User author)
      {
         if (isTrackingTime())
         {
            return;
         }

         if (!TimeTrackingHelpers.IsTimeTrackingAllowed(author, projectKey.HostName, getCurrentUser(projectKey.HostName)))
         {
            enabled = false;
         }

         linkLabelTimeTrackingMergeRequest.Visible = enabled;
         buttonTimeTrackingStart.Enabled = enabled;
         buttonTimeTrackingCancel.Enabled = false;

         if (enabled)
         {
            Debug.Assert(!String.IsNullOrEmpty(title) && !projectKey.Equals(default(ProjectKey)));
            linkLabelTimeTrackingMergeRequest.Text = String.Format("{0}   [{1}]", title, projectKey.ProjectName);
         }

         linkLabelTimeTrackingMergeRequest.Refresh();
      }

      private void updateTotalTime(MergeRequestKey? mrk, User author, string hostname, ITotalTimeCache totalTimeCache)
      {
         if (isTrackingTime())
         {
            labelTimeTrackingTrackedLabel.Text =
               String.Format("Tracked Time: {0}", _timeTracker.Elapsed.ToString(@"hh\:mm\:ss"));
            buttonEditTime.Enabled = false;
            return;
         }

         if (!mrk.HasValue
          || !TimeTrackingHelpers.IsTimeTrackingAllowed(author, hostname, getCurrentUser(hostname))
          || totalTimeCache == null)
         {
            labelTimeTrackingTrackedLabel.Text = String.Empty;
            buttonEditTime.Enabled = false;
         }
         else
         {
            TrackedTime trackedTime = totalTimeCache.GetTotalTime(mrk.Value);
            labelTimeTrackingTrackedLabel.Text = String.Format("Total Time: {0}",
               TimeTrackingHelpers.ConvertTotalTimeToText(trackedTime, true));
            buttonEditTime.Enabled = trackedTime.Amount.HasValue;
         }

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
                  this.toolTip.SetToolTip(this.buttonDiffTool, String.Format(
                     "Launch diff tool to compare selected revision to {0}", targetBranch));
               }
               break;

            case 2:
               buttonDiffTool.Enabled = isEnabled;
               buttonDiffTool.Text = "Diff Tool";
               this.toolTip.SetToolTip(this.buttonDiffTool, "Launch diff tool to compare selected revisions");
               break;

            case 0:
            default:
               buttonDiffTool.Enabled = false;
               buttonDiffTool.Text = "Diff Tool";
               break;
         }
      }

      private void enableCustomActions(bool enabled, IEnumerable<string> labels, User author)
      {
         if (!enabled)
         {
            foreach (Control control in groupBoxActions.Controls) control.Enabled = false;
            return;
         }

         if (author == null)
         {
            Debug.Assert(false);
            return;
         }

         foreach (Control control in groupBoxActions.Controls)
         {
            string dependency = (string)control.Tag;
            string resolvedDependency =
               String.IsNullOrEmpty(dependency) ? String.Empty : _expressionResolver.Resolve(dependency);
            control.Enabled = isCustomActionEnabled(labels, author, resolvedDependency);
         }
      }

      private void onWindowStateChanged()
      {
         if (this.WindowState != FormWindowState.Minimized)
         {
            if (_prevWindowState == FormWindowState.Minimized)
            {
               bool isRestoring = this.WindowState == FormWindowState.Normal;
               if (isRestoring && _forceMaximizeOnNextRestore)
               {
                  _forceMaximizeOnNextRestore = false;
                  _prevWindowState = FormWindowState.Maximized; // prevent re-entrance on next line
                  this.WindowState = FormWindowState.Maximized;
               }

               if (isRestoring && _applySplitterDistanceOnNextRestore)
               {
                  _applySplitterDistanceOnNextRestore = false;
                  applySavedSplitterDistance();
               }
            }
         }

         _prevWindowState = WindowState;
      }

      private void onDataCacheSelectionChanged(bool isLiveDataCacheSelected)
      {
         forEachListView(listView => listView.DeselectAllListViewItems());
         labelTimeTrackingTrackedLabel.Visible = isLiveDataCacheSelected;
         buttonEditTime.Visible = isLiveDataCacheSelected;
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
         listView.EnsureSelectionVisible();
         if (fmk.MergeRequest == null)
         {
            return; // List view item with summary information for a collapsed group
         }

         Trace.TraceInformation(String.Format(
            "[MainForm] User requested to change merge request to IId {0}, mode = {1}",
            fmk.MergeRequest.IId.ToString(), getCurrentTabDataCacheType().ToString()));

         DataCache dataCache = getDataCache(mode);
         enableCustomActions(true, fmk.MergeRequest.Labels, fmk.MergeRequest.Author);
         enableMergeRequestActions(true);
         updateMergeRequestDetails(fmk);
         updateTimeTrackingMergeRequestDetails(
            true, fmk.MergeRequest.Title, fmk.ProjectKey, fmk.MergeRequest.Author);
         updateTotalTime(new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId),
            fmk.MergeRequest.Author, fmk.ProjectKey.HostName, dataCache.TotalTimeCache);
         updateAbortGitCloneButtonState();

         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
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
         Text = Constants.MainWindowCaption
           + " (" + Application.ProductVersion + ")"
           + (StaticUpdateChecker.NewVersionInformation != null
              ? String.Format("   New version {0} is available!",
                 StaticUpdateChecker.NewVersionInformation.VersionNumber)
              : String.Empty);
      }

      private void updateTrayIcon()
      {
         notifyIcon.Icon = Properties.Resources.DefaultAppIcon;
         if (_iconScheme == null || !_iconScheme.Any())
         {
            return;
         }

         void loadNotifyIconFromFile(string filename)
         {
            try
            {
               notifyIcon.Icon = new Icon(filename);
            }
            catch (ArgumentException ex)
            {
               ExceptionHandlers.Handle(String.Format("Cannot create an icon from file \"{0}\"", filename), ex);
            }
         }

         if (isTrackingTime())
         {
            if (_iconScheme.ContainsKey("Icon_Tracking"))
            {
               loadNotifyIconFromFile(_iconScheme["Icon_Tracking"]);
            }
            return;
         }

         foreach (KeyValuePair<string, string> nameToFilename in _iconScheme)
         {
            string resolved = _expressionResolver.Resolve(nameToFilename.Key);
            if (getListView(EDataCacheType.Live).GetMatchingFilterMergeRequests()
               .Select(x => x.MergeRequest)
               .Any(x => x.Labels.Any(y => StringUtils.DoesMatchPattern(resolved, "Icon_{{Label:{0}}}", y))))
            {
               loadNotifyIconFromFile(nameToFilename.Value);
               break;
            }
         }
      }

      private void updateTaskbarIcon()
      {
         CommonControls.Tools.WinFormsHelpers.SetOverlayEllipseIcon(null);
         if (_badgeScheme == null || !_badgeScheme.Any())
         {
            return;
         }

         if (isTrackingTime())
         {
            if (_badgeScheme.ContainsKey("Badge_Tracking"))
            {
               CommonControls.Tools.WinFormsHelpers.SetOverlayEllipseIcon(
                  Color.FromName(_badgeScheme["Badge_Tracking"]));
            }
            return;
         }

         foreach (KeyValuePair<string, string> nameToFilename in _badgeScheme)
         {
            string resolved = _expressionResolver.Resolve(nameToFilename.Key);
            if (getListView(EDataCacheType.Live).GetMatchingFilterMergeRequests()
               .Select(x => x.MergeRequest)
               .Any(x => x.Labels.Any(y => StringUtils.DoesMatchPattern(resolved, "Badge_{{Label:{0}}}", y))))
            {
               CommonControls.Tools.WinFormsHelpers.SetOverlayEllipseIcon(
                  Color.FromName(nameToFilename.Value));
               break;
            }
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

      private int calcHorzDistance(Control leftControl, Control rightControl, bool preventOverlap = false)
      {
         int res = 0;
         if (leftControl != null && rightControl != null)
         {
            res = rightControl.Location.X - (leftControl.Location.X + leftControl.Size.Width);
         }
         else if (leftControl == null && rightControl != null)
         {
            res = rightControl.Location.X;
         }
         else if (leftControl != null && rightControl == null)
         {
            res = leftControl.Parent.Size.Width - (leftControl.Location.X + leftControl.Size.Width);
         }

         if (!preventOverlap && res < 0)
         {
            Trace.TraceWarning(
               "calcHorzDistance() returns negative value ({0}). " +
               "leftControl: {1} (Location: {{{2}, {3}}}, Size: {{{4}, {5}}}), " +
               "rightControl: {6} (Location: {{{7}, {8}}}, Size: {{{9}, {10}}}), " +
               "PreventOverlap: {11}",
               res,
               leftControl?.Name ?? "null",
               leftControl?.Location.X.ToString() ?? "N/A", leftControl?.Location.Y.ToString() ?? "N/A",
               leftControl?.Size.Width.ToString() ?? "N/A", leftControl?.Size.Height.ToString() ?? "N/A",
               rightControl?.Name ?? "null",
               rightControl?.Location.X.ToString() ?? "N/A", rightControl?.Location.Y.ToString() ?? "N/A",
               rightControl?.Size.Width.ToString() ?? "N/A", rightControl?.Size.Height.ToString() ?? "N/A",
               preventOverlap);
            Debug.Assert(false);
         }

         return res < 0 && preventOverlap ? 10 : res;
      }

      private int calcVertDistance(Control topControl, Control bottomControl, bool preventOverlap = false)
      {
         int res = 0;
         if (topControl != null && bottomControl != null)
         {
            res = bottomControl.Location.Y - (topControl.Location.Y + topControl.Size.Height);
         }
         else if (topControl == null && bottomControl != null)
         {
            res = bottomControl.Location.Y;
         }
         else if (topControl != null && bottomControl == null)
         {
            res = topControl.Parent.Size.Height - (topControl.Location.Y + topControl.Size.Height);
         }

         if (!preventOverlap && res < 0)
         {
            // This may occur on small resolutions (e.g. 1366x768)
            Trace.TraceWarning(
               "calcVertDistance() returns negative value ({0}). " +
               "topControl: {1} (Location: {{{2}, {3}}}, Size: {{{4}, {5}}}), " +
               "bottomControl: {6} (Location: {{{7}, {8}}}, Size: {{{9}, {10}}}), " +
               "PreventOverlap: {11}",
               res,
               topControl?.Name ?? "null",
               topControl?.Location.X.ToString() ?? "N/A", topControl?.Location.Y.ToString() ?? "N/A",
               topControl?.Size.Width.ToString() ?? "N/A", topControl?.Size.Height.ToString() ?? "N/A",
               bottomControl?.Name ?? "null",
               bottomControl?.Location.X.ToString() ?? "N/A", bottomControl?.Location.Y.ToString() ?? "N/A",
               bottomControl?.Size.Width.ToString() ?? "N/A", bottomControl?.Size.Height.ToString() ?? "N/A",
               preventOverlap);
         }

         return res < 0 && preventOverlap ? 10 : res;
      }

      private void resetMergeRequestTabMinimumSizes()
      {
         int defaultSplitContainerPanelMinSize = 25;
         splitContainer1.Panel1MinSize = defaultSplitContainerPanelMinSize;
         splitContainer1.Panel2MinSize = defaultSplitContainerPanelMinSize;
         splitContainer2.Panel1MinSize = defaultSplitContainerPanelMinSize;
         splitContainer2.Panel2MinSize = defaultSplitContainerPanelMinSize;

         this.MinimumSize = new System.Drawing.Size(0, 0);

         _initializedMinimumSizes = false;
      }

      private bool _initializedMinimumSizes = true;

      private int getLeftPaneMinWidth()
      {
         int liveTabTopRowWidth =
            calcHorzDistance(null, tabControlMode)
          + calcHorzDistance(null, groupBoxSelectMergeRequest)
          + calcHorzDistance(null, checkBoxDisplayFilter)
          + checkBoxDisplayFilter.MinimumSize.Width
          + calcHorzDistance(checkBoxDisplayFilter, textBoxDisplayFilter)
          + 100 /* cannot use textBoxLabels.MinimumSize.Width, see 9b65d7413c */
          + calcHorzDistance(textBoxDisplayFilter, buttonReloadList, true)
          + buttonReloadList.Size.Width
          + calcHorzDistance(buttonReloadList, buttonCreateNew)
          + buttonCreateNew.Size.Width
          + calcHorzDistance(buttonCreateNew, null) // button has Right anchor
          + calcHorzDistance(groupBoxSelectMergeRequest, null)
          + calcHorzDistance(tabControlMode, null);

         int searchTabTopRowWidth =
            calcHorzDistance(null, tabControlMode)
          + calcHorzDistance(null, groupBoxSearchMergeRequest)
          + calcHorzDistance(null, checkBoxSearchByTitleAndDescription)
          + checkBoxSearchByTitleAndDescription.Width
          + calcHorzDistance(checkBoxSearchByTitleAndDescription, checkBoxSearchByTargetBranch)
          + checkBoxSearchByTargetBranch.Width
          + calcHorzDistance(checkBoxSearchByTargetBranch, checkBoxSearchByProject)
          + checkBoxSearchByProject.Width
          + calcHorzDistance(checkBoxSearchByProject, checkBoxSearchByAuthor)
          + checkBoxSearchByAuthor.Width
          + calcHorzDistance(checkBoxSearchByAuthor, linkLabelFindMe)
          + linkLabelFindMe.Width
          + calcHorzDistance(linkLabelFindMe, labelSearchByState)
          + labelSearchByState.Width
          + 50 /* a minimum gap between State label and right border */
          + calcHorzDistance(groupBoxSearchMergeRequest, null)
          + calcHorzDistance(tabControlMode, null);

         int searchTabBottomRowWidth =
            calcHorzDistance(null, tabControlMode)
          + calcHorzDistance(null, groupBoxSearchMergeRequest)
          + calcHorzDistance(null, textBoxSearchText)
          + textBoxSearchText.Width
          + calcHorzDistance(textBoxSearchText, textBoxSearchTargetBranch)
          + textBoxSearchTargetBranch.Width
          + calcHorzDistance(textBoxSearchTargetBranch, comboBoxProjectName)
          + comboBoxProjectName.Width
          + calcHorzDistance(comboBoxProjectName, comboBoxUser)
          + comboBoxUser.Width
          + calcHorzDistance(comboBoxUser, comboBoxSearchByState)
          + comboBoxSearchByState.Width
          + calcHorzDistance(comboBoxSearchByState, buttonSearch)
          + buttonSearch.Width
          + 50 /* a minimum gap between Search button and right border */
          + calcHorzDistance(groupBoxSearchMergeRequest, null)
          + calcHorzDistance(tabControlMode, null);

         int recentTabTopRowWidth =
            calcHorzDistance(null, tabControlMode)
          + calcHorzDistance(null, groupBoxRecentMergeRequest)
          + calcHorzDistance(null, textBoxRecentMergeRequestsHint)
          + textBoxRecentMergeRequestsHint.Width
          + calcHorzDistance(groupBoxRecentMergeRequest, null)
          + calcHorzDistance(tabControlMode, null);

         return Math.Max(liveTabTopRowWidth,
                  Math.Max(recentTabTopRowWidth,
                     Math.Max(searchTabBottomRowWidth, searchTabTopRowWidth)));
      }

      private int getRightPaneMinWidth()
      {
         int calcMinWidthOfControlGroup(IEnumerable<Control> controls, int minGap) =>
            controls.Cast<Control>().Sum(x => x.MinimumSize.Width) + (controls.Count() - 1) * minGap;

         int buttonMinDistance = calcHorzDistance(buttonAddComment, buttonNewDiscussion);

         int groupBoxReviewMinWidth =
            calcMinWidthOfControlGroup(groupBoxReview.Controls.Cast<Control>(), buttonMinDistance)
            + calcHorzDistance(null, groupBoxReview)
            + calcHorzDistance(null, buttonAddComment)
            + calcHorzDistance(buttonDiffTool, null)
            + calcHorzDistance(groupBoxReview, null);

         int groupBoxTimeTrackingMinWidth = calcMinWidthOfControlGroup(
            new Control[] { buttonTimeTrackingStart, buttonTimeTrackingCancel, buttonEditTime }, buttonMinDistance)
            + calcHorzDistance(null, groupBoxTimeTracking)
            + calcHorzDistance(null, buttonTimeTrackingStart)
            + calcHorzDistance(buttonEditTime, null)
            + calcHorzDistance(groupBoxTimeTracking, null);

         bool hasActions = groupBoxActions.Controls.Count > 0;
         int groupBoxActionsMinWidth =
            calcMinWidthOfControlGroup(groupBoxActions.Controls.Cast<Control>(), buttonMinDistance)
            + calcHorzDistance(null, groupBoxActions)
            + calcHorzDistance(null, hasActions ? buttonAddComment : null) // First button is aligned with "Add a comment"
            + calcHorzDistance(hasActions ? buttonDiffTool : null, null)   // Last button is aligned with "Diff Tool"
            + calcHorzDistance(groupBoxActions, null);

         bool hasPicture1 = pictureBox1.BackgroundImage != null;
         bool hasPicture2 = pictureBox2.BackgroundImage != null;

         int panelFreeSpaceMinWidth =
            calcHorzDistance(null, panelFreeSpace)
          + (hasPicture1 ? calcHorzDistance(null, pictureBox1) + pictureBox1.MinimumSize.Width : panelFreeSpace.MinimumSize.Width)
          + (hasPicture2 ? pictureBox2.MinimumSize.Width + calcHorzDistance(pictureBox2, null) : panelFreeSpace.MinimumSize.Width)
          + calcHorzDistance(panelFreeSpace, null);

         return Enumerable.Max(new int[]
            { groupBoxReviewMinWidth, groupBoxTimeTrackingMinWidth, groupBoxActionsMinWidth, panelFreeSpaceMinWidth });
      }

      private int getTopRightPaneMinHeight()
      {
         return
            +calcVertDistance(null, groupBoxSelectedMR)
            + calcVertDistance(null, richTextBoxMergeRequestDescription)
            + 100 /* cannot use richTextBoxMergeRequestDescription.MinimumSize.Height, see 9b65d7413c */
            + calcVertDistance(richTextBoxMergeRequestDescription, linkLabelConnectedTo, true)
            + linkLabelConnectedTo.Height
            + calcVertDistance(linkLabelConnectedTo, null)
            + calcVertDistance(groupBoxSelectedMR, null);
      }

      private int getBottomRightPaneMinHeight()
      {
         bool hasPicture1 = pictureBox1.BackgroundImage != null;
         bool hasPicture2 = pictureBox2.BackgroundImage != null;

         int panelFreeSpaceMinHeight =
            Math.Max(
               (hasPicture1 ?
                  calcVertDistance(null, pictureBox1)
                + pictureBox1.MinimumSize.Height
                + calcVertDistance(pictureBox1, null, true) : panelFreeSpace.MinimumSize.Height),
               (hasPicture2 ?
                  calcVertDistance(null, pictureBox2)
                + pictureBox2.MinimumSize.Height
                + calcVertDistance(pictureBox2, null, true) : panelFreeSpace.MinimumSize.Height));

         return
              calcVertDistance(null, groupBoxSelectRevisions)
            + groupBoxSelectRevisions.Height
            + calcVertDistance(groupBoxSelectRevisions, groupBoxReview)
            + groupBoxReview.Height
            + calcVertDistance(groupBoxReview, groupBoxTimeTracking)
            + groupBoxTimeTracking.Height
            + calcVertDistance(groupBoxTimeTracking, groupBoxActions)
            + groupBoxActions.Height
            + calcVertDistance(groupBoxActions, panelFreeSpace)
            + panelFreeSpaceMinHeight
            + calcVertDistance(panelFreeSpace, panelStatusBar, true)
            + panelStatusBar.Height
            + calcVertDistance(panelStatusBar, panelBottomMenu)
            + panelBottomMenu.Height
            + calcVertDistance(panelBottomMenu, null);
      }

      private void initializeMergeRequestTabMinimumSizes()
      {
         if (_initializedMinimumSizes || tabControl.SelectedTab != tabPageMR)
         {
            return;
         }

         if (Program.Settings.DisableSplitterRestrictions)
         {
            _initializedMinimumSizes = true;
            return;
         }

         int leftPaneMinWidth = getLeftPaneMinWidth();
         int rightPaneMinWidth = getRightPaneMinWidth();
         int topRightPaneMinHeight = getTopRightPaneMinHeight();
         int bottomRightPaneMinHeight = getBottomRightPaneMinHeight();

         int clientAreaMinWidth =
            calcHorzDistance(null, tabPageMR)
          + calcHorzDistance(null, splitContainer1)
          + leftPaneMinWidth
          + splitContainer1.SplitterWidth
          + rightPaneMinWidth
          + calcHorzDistance(splitContainer1, null)
          + calcHorzDistance(tabPageMR, null);
         int nonClientAreaWidth = this.Size.Width - this.ClientSize.Width;

         int clientAreaMinHeight =
            calcVertDistance(null, tabPageMR)
          + calcVertDistance(null, splitContainer1)
          + calcVertDistance(null, splitContainer2)
          + topRightPaneMinHeight
          + splitContainer2.SplitterWidth
          + bottomRightPaneMinHeight
          + calcVertDistance(splitContainer2, null)
          + calcVertDistance(splitContainer1, null)
          + calcVertDistance(tabPageMR, null);
         int nonClientAreaHeight = this.Size.Height - this.ClientSize.Height;

         // First, apply new size to the Form because this action resizes it the Format is too small for split containers
         this.MinimumSize = new Size(clientAreaMinWidth + nonClientAreaWidth, clientAreaMinHeight + nonClientAreaHeight);

         // Validate widths
         if (leftPaneMinWidth + rightPaneMinWidth > this.splitContainer1.Width ||
             topRightPaneMinHeight + bottomRightPaneMinHeight > this.splitContainer2.Height)
         {
            Trace.TraceError(String.Format(
               "[MainForm] SplitContainer size conflict. "
             + "SplitContainer1.Width = {0}, leftPaneMinWidth = {1}, rightPaneMinWidth = {2}. "
             + "SplitContainer2.Height = {3}, topRightPaneMinHeight = {4}, bottomRightPaneMinHeight = {5}",
               splitContainer1.Width, leftPaneMinWidth, rightPaneMinWidth,
               splitContainer2.Height, topRightPaneMinHeight, bottomRightPaneMinHeight));
            Debug.Assert(false);
            resetMergeRequestTabMinimumSizes();
            _initializedMinimumSizes = true;
            return;
         }

         // Then, apply new sizes to split containers
         this.splitContainer1.Panel1MinSize = leftPaneMinWidth;
         this.splitContainer1.Panel2MinSize = rightPaneMinWidth;
         this.splitContainer2.Panel1MinSize = topRightPaneMinHeight;
         this.splitContainer2.Panel2MinSize = bottomRightPaneMinHeight;

         // Set default position for splitter
         this.splitContainer1.SplitterDistance = this.splitContainer1.Width - this.splitContainer1.Panel2MinSize;
         this.splitContainer2.SplitterDistance = this.splitContainer2.Height - this.splitContainer2.Panel2MinSize;

         _initializedMinimumSizes = true;
      }

      private void repositionCustomCommands()
      {
         int getControlX(Control control, int index) =>
             control.Width * index +
                (groupBoxActions.Width - _customCommands.Count() * control.Width) *
                (index + 1) / (_customCommands.Count() + 1);

         for (int id = 0; id < groupBoxActions.Controls.Count; ++id)
         {
            Control c = groupBoxActions.Controls[id];
            c.Location = new Point { X = getControlX(c, id), Y = c.Location.Y };
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
         getListView(EDataCacheType.Live).DisableListView(true);
         setMergeRequestEditEnabled(false);
         enableMergeRequestFilterControls(false);
         enableMergeRequestListControls(false);
      }

      private void disableSearchTabControls()
      {
         getListView(EDataCacheType.Search).DisableListView(true);
         enableSimpleSearchControls(false);
         setSearchByProjectEnabled(false);
         setSearchByAuthorEnabled(false);
         updateSearchButtonState();
      }

      private void disableRecentTabControls()
      {
         getListView(EDataCacheType.Recent).DisableListView(true);
      }

      private void disableSelectedMergeRequestControls()
      {
         enableCustomActions(false, null, null);
         enableMergeRequestActions(false);
         updateMergeRequestDetails(null);
         updateTimeTrackingMergeRequestDetails(false, null, default(ProjectKey), null);
         updateTotalTime(null, null, null, null);
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

      private void onTimerStopped(ITotalTimeCache totalTimeCache)
      {
         buttonTimeTrackingStart.Text = buttonStartTimerDefaultText;
         buttonTimeTrackingStart.BackColor = System.Drawing.Color.Transparent;
         buttonTimeTrackingCancel.Enabled = false;
         buttonTimeTrackingCancel.BackColor = System.Drawing.Color.Transparent;

         bool isMergeRequestSelected = getMergeRequest(null) != null && getMergeRequestKey(null).HasValue;
         if (isMergeRequestSelected)
         {
            MergeRequest mergeRequest = getMergeRequest(null);
            MergeRequestKey mrk = getMergeRequestKey(null).Value;

            updateTimeTrackingMergeRequestDetails(true, mergeRequest.Title, mrk.ProjectKey, mergeRequest.Author);

            // Take care of controls that 'time tracking' mode shares with normal mode
            updateTotalTime(mrk, mergeRequest.Author, mrk.ProjectKey.HostName, totalTimeCache);
         }
         else
         {
            updateTimeTrackingMergeRequestDetails(false, null, default(ProjectKey), null);
            updateTotalTime(null, null, null, null);
         }

         updateTrayIcon();
         updateTaskbarIcon();

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
         if (!this.Created)
         {
            return;
         }

         Trace.TraceInformation(String.Format("[MainForm] Font changed, new emSize = {0}", this.Font.Size));
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
         Trace.TraceInformation(String.Format("[MainForm] DPI changed, new DPI = {0}", this.DeviceDpi));
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
         switchTabAndSelectMergeRequest(_timeTrackingMode.Value, _timeTracker.MergeRequest, true);
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

      private void moveCopyFromClipboardLinkLabel()
      {
         int tabCount = tabControlMode.TabPages.Count;
         Debug.Assert(tabCount > 0);

         Rectangle tabRect = tabControlMode.GetTabRect(tabCount - 1);

         int linkLabelTopRelativeToTabRect = tabRect.Height / 2 - linkLabelFromClipboard.Height / 2;
         int linkLabelTop = tabRect.Top + linkLabelTopRelativeToTabRect;

         int linkLabelHorizontalOffsetFromRightmostTab = 20;
         int linkLabelLeft = tabRect.X + tabRect.Width + linkLabelHorizontalOffsetFromRightmostTab;

         linkLabelFromClipboard.Location = new System.Drawing.Point(linkLabelLeft, linkLabelTop);
      }

      private bool switchTabAndSelectMergeRequest(EDataCacheType mode, MergeRequestKey? mrk, bool exact)
      {
         switchMode(mode);
         return getListView(mode).SelectMergeRequest(mrk, exact);
      }

      private void switchMode(EDataCacheType mode)
      {
         if (mode != getCurrentTabDataCacheType())
         {
            switch (mode)
            {
               case EDataCacheType.Live:
                  tabControlMode.SelectedTab = tabPageLive;
                  break;
               case EDataCacheType.Search:
                  tabControlMode.SelectedTab = tabPageSearch;
                  break;
               case EDataCacheType.Recent:
                  tabControlMode.SelectedTab = tabPageRecent;
                  break;
               default:
                  Debug.Assert(false);
                  break;
            }
         }
      }
   }
}

