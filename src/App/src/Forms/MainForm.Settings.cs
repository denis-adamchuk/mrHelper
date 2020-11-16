using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.StorageSupport;
using mrHelper.GitLabClient;
using mrHelper.App.Forms.Helpers;
using mrHelper.App.Interprocess;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void setControlStateFromConfiguration()
      {
         Program.Settings.PropertyChanged += onSettingsPropertyChanged;

         _loadingConfiguration = true;
         Trace.TraceInformation("[MainForm] Loading configuration");

         textBoxStorageFolder.Text = Program.Settings.LocalGitFolder;
         checkBoxDisplayFilter.Checked = Program.Settings.DisplayFilterEnabled;
         textBoxDisplayFilter.Text = Program.Settings.DisplayFilter;
         checkBoxMinimizeOnClose.Checked = Program.Settings.MinimizeOnClose;
         checkBoxRemindAboutAvailableNewVersion.Checked = Program.Settings.RemindAboutAvailableNewVersion;
         checkBoxRunWhenWindowsStarts.Checked = Program.Settings.RunWhenWindowsStarts;
         checkBoxDisableSplitterRestrictions.Checked = Program.Settings.DisableSplitterRestrictions;
         checkBoxNewDiscussionIsTopMostForm.Checked = Program.Settings.NewDiscussionIsTopMostForm;
         checkBoxDisableSpellChecker.Checked = Program.Settings.DisableSpellChecker;
         checkBoxFlatReplies.Checked = !Program.Settings.NeedShiftReplies;
         checkBoxDiscussionColumnFixedWidth.Checked = Program.Settings.IsDiscussionColumnWidthFixed;
         checkBoxShowNewMergeRequests.Checked = Program.Settings.Notifications_NewMergeRequests;
         checkBoxShowMergedMergeRequests.Checked = Program.Settings.Notifications_MergedMergeRequests;
         checkBoxShowUpdatedMergeRequests.Checked = Program.Settings.Notifications_UpdatedMergeRequests;
         checkBoxShowResolvedAll.Checked = Program.Settings.Notifications_AllThreadsResolved;
         checkBoxShowOnMention.Checked = Program.Settings.Notifications_OnMention;
         checkBoxShowKeywords.Checked = Program.Settings.Notifications_Keywords;
         checkBoxShowMyActivity.Checked = Program.Settings.Notifications_MyActivity;
         checkBoxShowServiceNotifications.Checked = Program.Settings.Notifications_Service;

         setKnownHostsDropdownValue();
         setDiffContextPositionRadioValue();
         setDiscussionWidthRadioValue();
         setShowWarningOnFileMismatchRadioValue();
         setDefaultRevisionTypeRadioValue();
         setWorkflowTypeRadioValue();
         setDontUseGitRadioValue();
         selectDiifContextDepthDropdownValue();
         setColumnWidthsFromSettings();
         setColumnIndicesFromSettings();
         setFontFromSettings();
         setThemeFromSettings();
         createMessageFilterFromSettings();

         Trace.TraceInformation("[MainForm] Configuration loaded");
         _loadingConfiguration = false;
      }

      private void createMessageFilterFromSettings()
      {
         _mergeRequestFilter = new MergeRequestFilter(createMergeRequestFilterState());
         _mergeRequestFilter.FilterChanged += () => updateMergeRequestList(EDataCacheType.Live);
      }

      private void setColumnWidthsFromSettings()
      {
         getListView(EDataCacheType.Live).SetColumnWidths(Program.Settings.ListViewMergeRequestsColumnWidths);
         getListView(EDataCacheType.Search).SetColumnWidths(Program.Settings.ListViewFoundMergeRequestsColumnWidths);
         getListView(EDataCacheType.Recent).SetColumnWidths(Program.Settings.ListViewRecentMergeRequestsColumnWidths);
      }

      private void setColumnIndicesFromSettings()
      {
         getListView(EDataCacheType.Live).SetColumnIndices(Program.Settings.ListViewMergeRequestsDisplayIndices,
            x => Program.Settings.ListViewMergeRequestsDisplayIndices = x);
         getListView(EDataCacheType.Search).SetColumnIndices(Program.Settings.ListViewFoundMergeRequestsDisplayIndices,
            x => Program.Settings.ListViewFoundMergeRequestsDisplayIndices = x);
         getListView(EDataCacheType.Recent).SetColumnIndices(Program.Settings.ListViewRecentMergeRequestsDisplayIndices,
            x => Program.Settings.ListViewRecentMergeRequestsDisplayIndices = x);
      }

      private void setThemeFromSettings()
      {
         WinFormsHelpers.FillComboBox(comboBoxThemes,
            Constants.ThemeNames, name => name == Program.Settings.VisualThemeName);
         applyTheme(Program.Settings.VisualThemeName);
      }

      private void setFontFromSettings()
      {
         WinFormsHelpers.FillComboBox(comboBoxFonts,
            Constants.MainWindowFontSizeChoices, name => name == Program.Settings.MainWindowFontSizeName);
         applyFont(Program.Settings.MainWindowFontSizeName);
      }

      private void selectDiifContextDepthDropdownValue()
      {
         if (comboBoxDCDepth.Items.Contains(Program.Settings.DiffContextDepth))
         {
            comboBoxDCDepth.Text = Program.Settings.DiffContextDepth;
         }
         else
         {
            comboBoxDCDepth.SelectedIndex = 0;
         }
      }

      private void setKnownHostsDropdownValue()
      {
         Debug.Assert(Program.Settings.KnownHosts.Count() == Program.Settings.KnownAccessTokens.Count());
         // Remove all items except header
         for (int iListViewItem = 1; iListViewItem < listViewKnownHosts.Items.Count; ++iListViewItem)
         {
            listViewKnownHosts.Items.RemoveAt(iListViewItem);
         }

         List<string> newKnownHosts = new List<string>();
         List<string> newAccessTokens = new List<string>();
         for (int iKnownHost = 0; iKnownHost < Program.Settings.KnownHosts.Count(); ++iKnownHost)
         {
            // Upgrade from old versions which did not have prefix
            string host = StringUtils.GetHostWithPrefix(Program.Settings.KnownHosts[iKnownHost]);
            string accessToken = Program.Settings.KnownAccessTokens.Length > iKnownHost
               ? Program.Settings.KnownAccessTokens[iKnownHost]
               : String.Empty;
            if (addKnownHost(host, accessToken))
            {
               newKnownHosts.Add(host);
               newAccessTokens.Add(accessToken);
            }
         }
         Program.Settings.KnownHosts = newKnownHosts.ToArray();
         Program.Settings.KnownAccessTokens = newAccessTokens.ToArray();
      }

      private void setDefaultRevisionTypeRadioValue()
      {
         RevisionType defaultRevisionType = ConfigurationHelper.GetDefaultRevisionType(Program.Settings);
         switch (defaultRevisionType)
         {
            case RevisionType.Commit:
               radioButtonCommits.Checked = true;
               break;

            case RevisionType.Version:
               radioButtonVersions.Checked = true;
               break;
         }
      }

      private void setWorkflowTypeRadioValue()
      {
         if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
         {
            radioButtonSelectByProjects.Checked = true;
         }
         else
         {
            radioButtonSelectByUsernames.Checked = true;
         }
      }

      private void setDontUseGitRadioValue()
      {
         LocalCommitStorageType type = ConfigurationHelper.GetPreferredStorageType(Program.Settings);
         switch (type)
         {
            case LocalCommitStorageType.FileStorage:
               radioButtonDontUseGit.Checked = true;
               break;

            case LocalCommitStorageType.FullGitRepository:
               radioButtonUseGitFullClone.Checked = true;
               break;

            case LocalCommitStorageType.ShallowGitRepository:
               radioButtonUseGitShallowClone.Checked = true;
               break;
         }
      }

      private void setShowWarningOnFileMismatchRadioValue()
      {
         var showWarningsOnFileMismatchMode = ConfigurationHelper.GetShowWarningsOnFileMismatchMode(Program.Settings);
         switch (showWarningsOnFileMismatchMode)
         {
            case ConfigurationHelper.ShowWarningsOnFileMismatchMode.Always:
               radioButtonShowWarningsAlways.Checked = true;
               break;

            case ConfigurationHelper.ShowWarningsOnFileMismatchMode.Never:
               radioButtonShowWarningsNever.Checked = true;
               break;

            case ConfigurationHelper.ShowWarningsOnFileMismatchMode.UntilUserIgnoresFile:
               radioButtonShowWarningsOnce.Checked = true;
               break;
         }
      }

      private void setDiscussionWidthRadioValue()
      {
         var discussionColumnWidth = ConfigurationHelper.GetDiscussionColumnWidth(Program.Settings);
         switch (discussionColumnWidth)
         {
            case ConfigurationHelper.DiscussionColumnWidth.Narrow:
               radioButtonDiscussionColumnWidthNarrow.Checked = true;
               break;

            case ConfigurationHelper.DiscussionColumnWidth.NarrowPlus:
               radioButtonDiscussionColumnWidthNarrowPlus.Checked = true;
               break;

            case ConfigurationHelper.DiscussionColumnWidth.Medium:
               radioButtonDiscussionColumnWidthMedium.Checked = true;
               break;

            case ConfigurationHelper.DiscussionColumnWidth.MediumPlus:
               radioButtonDiscussionColumnWidthMediumPlus.Checked = true;
               break;

            case ConfigurationHelper.DiscussionColumnWidth.Wide:
               radioButtonDiscussionColumnWidthWide.Checked = true;
               break;
         }
      }

      private void setDiffContextPositionRadioValue()
      {
         var diffContextPosition = ConfigurationHelper.GetDiffContextPosition(Program.Settings);
         switch (diffContextPosition)
         {
            case ConfigurationHelper.DiffContextPosition.Top:
               radioButtonDiffContextPositionTop.Checked = true;
               break;

            case ConfigurationHelper.DiffContextPosition.Left:
               radioButtonDiffContextPositionLeft.Checked = true;
               break;

            case ConfigurationHelper.DiffContextPosition.Right:
               radioButtonDiffContextPositionRight.Checked = true;
               break;
         }
      }

      private string getHostName()
      {
         return comboBoxHost.SelectedItem != null ? ((HostComboBoxItem)comboBoxHost.SelectedItem).Host : String.Empty;
      }

      /// <summary>
      /// Populates host list with list of known hosts from Settings
      /// </summary>
      private void updateHostsDropdownList()
      {
         updateEnablementsOfWorkflowSelectors();

         if (listViewKnownHosts.Items.Count == 0)
         {
            disableComboBox(comboBoxHost, String.Empty);
            return;
         }

         enableComboBox(comboBoxHost);

         for (int idx = comboBoxHost.Items.Count - 1; idx >= 0; --idx)
         {
            HostComboBoxItem item = (HostComboBoxItem)comboBoxHost.Items[idx];
            if (!listViewKnownHosts.Items.Cast<ListViewItem>().Any(x => x.Text == item.Host))
            {
               comboBoxHost.Items.RemoveAt(idx);
            }
         }

         foreach (ListViewItem item in listViewKnownHosts.Items)
         {
            if (!comboBoxHost.Items.Cast<HostComboBoxItem>().Any(x => x.Host == item.Text))
            {
               HostComboBoxItem hostItem = new HostComboBoxItem(item.Text, item.SubItems[1].Text);
               comboBoxHost.Items.Add(hostItem);
            }
         }
      }

      private void updateEnablementsOfWorkflowSelectors()
      {
         if (listViewKnownHosts.Items.Count > 0)
         {
            radioButtonSelectByUsernames.Enabled = true;
            radioButtonSelectByProjects.Enabled = true;
            listViewUsers.Enabled = radioButtonSelectByUsernames.Checked;
            listViewProjects.Enabled = radioButtonSelectByProjects.Checked;
            buttonEditProjects.Enabled = true;
            buttonEditUsers.Enabled = true;
         }
         else
         {
            radioButtonSelectByUsernames.Enabled = false;
            radioButtonSelectByProjects.Enabled = false;
            listViewUsers.Enabled = false;
            listViewProjects.Enabled = false;
            buttonEditProjects.Enabled = false;
            buttonEditUsers.Enabled = false;
         }
      }

      private bool addKnownHost(string host, string accessToken)
      {
         Trace.TraceInformation(String.Format("[MainForm] Adding host {0} with token {1}",
            host, accessToken));

         foreach (ListViewItem listItem in listViewKnownHosts.Items)
         {
            if (listItem.Text == host)
            {
               Trace.TraceInformation(String.Format("[MainForm] Host name already exists"));
               return false;
            }
         }

         ListViewItem item = new ListViewItem(host);
         item.SubItems.Add(accessToken);
         listViewKnownHosts.Items.Add(item);
         return true;
      }

      private bool removeKnownHost()
      {
         if (listViewKnownHosts.SelectedItems.Count > 0)
         {
            Trace.TraceInformation(String.Format("[MainForm] Removing host name {0}",
               listViewKnownHosts.SelectedItems[0].ToString()));

            listViewKnownHosts.Items.Remove(listViewKnownHosts.SelectedItems[0]);
            return true;
         }
         return false;
      }

      private string getDefaultColorSchemeFileName()
      {
         return String.Format("{0}.{1}", DefaultColorSchemeName, ColorSchemeFileNamePrefix);
      }

      private void fillColorSchemesList()
      {
         if (Program.Settings.ColorSchemeFileName == String.Empty)
         {
            // Upgrade from old versions which did not have a separate file for Default color scheme
            Program.Settings.ColorSchemeFileName = getDefaultColorSchemeFileName();
         }

         string defaultFileName = getDefaultColorSchemeFileName();
         string defaultFilePath = Path.Combine(Directory.GetCurrentDirectory(), defaultFileName);

         comboBoxColorSchemes.Items.Clear();

         string selectedScheme = null;
         string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
         if (files.Contains(defaultFilePath))
         {
            // put Default scheme first in the list
            comboBoxColorSchemes.Items.Add(defaultFileName);
         }

         foreach (string file in files)
         {
            if (file.EndsWith(ColorSchemeFileNamePrefix))
            {
               string scheme = Path.GetFileName(file);
               if (file != defaultFilePath)
               {
                  comboBoxColorSchemes.Items.Add(scheme);
               }
               if (scheme == Program.Settings.ColorSchemeFileName)
               {
                  selectedScheme = scheme;
               }
            }
         }

         if (selectedScheme != null)
         {
            comboBoxColorSchemes.SelectedItem = selectedScheme;
         }
         else if (comboBoxColorSchemes.Items.Count > 0)
         {
            comboBoxColorSchemes.SelectedIndex = 0;
         }
      }

      private void initializeColorScheme()
      {
         bool createColorScheme(string filename)
         {
            try
            {
               _colorScheme = new ColorScheme(filename, _expressionResolver);
               return true;
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle("Cannot create a color scheme", ex);
            }
            return false;
         }

         if (comboBoxColorSchemes.SelectedIndex < 0 || comboBoxColorSchemes.Items.Count < 1)
         {
            // nothing is selected or list is empty, create an empty scheme
            _colorScheme = new ColorScheme();
         }

         // try to create a scheme for the selected item
         else if (!createColorScheme(comboBoxColorSchemes.Text))
         {
            _colorScheme = new ColorScheme();
            MessageBox.Show("Cannot initialize color scheme", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }

         forEachListView(listView => listView.SetColorScheme(_colorScheme));
      }

      private void initializeIconScheme()
      {
         if (!System.IO.File.Exists(IconSchemeFileName))
         {
            return;
         }

         try
         {
            _iconScheme = JsonUtils.LoadFromFile<Dictionary<string, object>>(
               IconSchemeFileName).ToDictionary(
                  item => item.Key,
                  item => item.Value.ToString());
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle("Cannot load icon scheme", ex);
         }
      }

      private void initializeBadgeScheme()
      {
         if (!System.IO.File.Exists(BadgeSchemeFileName))
         {
            return;
         }

         try
         {
            _badgeScheme = JsonUtils.LoadFromFile<Dictionary<string, object>>(
               BadgeSchemeFileName).ToDictionary(
                  item => item.Key,
                  item => item.Value.ToString());
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle("Cannot load badge scheme", ex);
         }
      }

      private string getInitialHostNameIfKnown()
      {
         return Program.Settings.KnownHosts.Any(host => host == _initialHostName) ? _initialHostName : null;
      }

      private void setInitialHostName(string hostname)
      {
         _initialHostName = hostname;
      }

      private string getDiffTempFolder(Snapshot snapshot)
      {
         if (ConfigurationHelper.GetPreferredStorageType(Program.Settings) == LocalCommitStorageType.FileStorage)
         {
            return snapshot.TempFolder;
         }
         return Environment.GetEnvironmentVariable("TEMP");
      }

      private void launchAddKnownHostDialog()
      {
         AddKnownHostForm form = new AddKnownHostForm();
         if (form.ShowDialog() != DialogResult.OK)
         {
            return;
         }

         BeginInvoke(new Action(async () =>
         {
            string hostname = StringUtils.GetHostWithPrefix(form.Host);
            string accessToken = form.AccessToken;
            ConnectionChecker connectionChecker = new ConnectionChecker();
            ConnectionCheckStatus status = await connectionChecker.CheckConnection(hostname, accessToken);
            if (status != ConnectionCheckStatus.OK)
            {
               string message =
                  status == ConnectionCheckStatus.BadAccessToken
                     ? "Bad access token"
                     : "Invalid hostname";
               MessageBox.Show(message, "Cannot connect to the host",
                  MessageBoxButtons.OK, MessageBoxIcon.Error);
               return;
            }

            if (!addKnownHost(hostname, accessToken))
            {
               MessageBox.Show("Such host is already in the list", "Host will not be added",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
               return;
            }

            updateKnownHostAndTokensInSettings();
            updateHostsDropdownList();
            selectHost(PreferredSelection.Latest);
            reconnect();
         }));
      }

      private void onRemoveSelectedHost()
      {
         bool removeCurrent =
               listViewKnownHosts.SelectedItems.Count > 0 && getHostName() != String.Empty
            && getHostName() == listViewKnownHosts.SelectedItems[0].Text;

         string removedHostName = listViewKnownHosts.SelectedItems.Count > 0
            ? listViewKnownHosts.SelectedItems[0].Text
            : String.Empty;

         if (!removeKnownHost())
         {
            return;
         }

         Debug.Assert(!String.IsNullOrEmpty(removedHostName));

         _currentUser.Remove(removedHostName);
         updateKnownHostAndTokensInSettings();
         updateHostsDropdownList();
         if (removeCurrent)
         {
            if (comboBoxHost.Items.Count == 0)
            {
               updateProjectsListView();
               updateUsersListView();
            }
            else
            {
               selectHost(PreferredSelection.Latest);
            }

            // calling this unconditionally to drop current sessions and disable UI
            reconnect();
         }
      }

      private void updateKnownHostAndTokensInSettings()
      {
         Program.Settings.KnownHosts = listViewKnownHosts
            .Items
            .Cast<ListViewItem>()
            .Select(i => i.Text)
            .ToArray();
         Program.Settings.KnownAccessTokens = listViewKnownHosts
            .Items
            .Cast<ListViewItem>()
            .Select(i => i.SubItems[1].Text)
            .ToArray();
      }

      private void onTextBoxDisplayFilterUpdate()
      {
         Program.Settings.DisplayFilter = textBoxDisplayFilter.Text;
         if (_mergeRequestFilter != null)
         {
            _mergeRequestFilter.Filter = createMergeRequestFilterState();
         }
      }

      private void setupDefaultProjectList()
      {
         // Check if file exists. If it does not, it is not an error.
         if (!System.IO.File.Exists(ProjectListFileName))
         {
            return;
         }

         try
         {
            ConfigurationHelper.InitializeSelectedProjects(JsonUtils.
               LoadFromFile<IEnumerable<ConfigurationHelper.HostInProjectsFile>>(
                  ProjectListFileName), Program.Settings);
         }
         catch (Exception ex) // whatever de-serialization exception
         {
            ExceptionHandlers.Handle("Cannot load projects from file", ex);
         }
      }

      private void launchEditProjectListDialog()
      {
         string host = getHostName();
         if (host == String.Empty)
         {
            return;
         }

         IEnumerable<Tuple<string, bool>> projects = ConfigurationHelper.GetProjectsForHost(host, Program.Settings);
         Debug.Assert(projects != null);

         GitLabInstance gitLabInstance = new GitLabInstance(host, Program.Settings);
         RawDataAccessor rawDataAccessor = new RawDataAccessor(gitLabInstance);
         using (EditOrderedListViewForm form = new EditOrderedListViewForm("Edit Projects",
            "Add project", "Type project name in group/project format",
            projects, new EditProjectsListViewCallback(rawDataAccessor), true))
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               return;
            }

            if (!Enumerable.SequenceEqual(projects, form.Items))
            {
               ConfigurationHelper.SetProjectsForHost(host, form.Items, Program.Settings);
               updateProjectsListView();

               if (ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
               {
                  Trace.TraceInformation("[MainForm] Reconnecting after project list change");
                  reconnect();
               }
            }
         }
      }

      private void launchEditUserListDialog()
      {
         string host = getHostName();
         if (host == String.Empty)
         {
            return;
         }

         IEnumerable<Tuple<string, bool>> users = ConfigurationHelper.GetUsersForHost(host, Program.Settings);
         Debug.Assert(users != null);

         GitLabInstance gitLabInstance = new GitLabInstance(host, Program.Settings);
         RawDataAccessor rawDataAccessor = new RawDataAccessor(gitLabInstance);
         using (EditOrderedListViewForm form = new EditOrderedListViewForm("Edit Users",
            "Add username", "Type a name of GitLab user, teams allowed",
            users, new EditUsersListViewCallback(rawDataAccessor), false))
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               return;
            }

            if (!Enumerable.SequenceEqual(users, form.Items))
            {
               ConfigurationHelper.SetUsersForHost(host, form.Items, Program.Settings);
               updateUsersListView();

               if (!ConfigurationHelper.IsProjectBasedWorkflowSelected(Program.Settings))
               {
                  Trace.TraceInformation("[MainForm] Reconnecting after user list change");
                  reconnect();
               }
            }
         }
      }

      private void applyNeedShiftRepliesChange(bool value)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.NeedShiftReplies = value;
         }
      }

      private void applyIsFixedWidthChange(bool value)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.IsDiscussionColumnWidthFixed = value;
         }
      }

      private void applyDiffContextPositionChange()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         ConfigurationHelper.DiffContextPosition mode = ConfigurationHelper.DiffContextPosition.Top;
         if (radioButtonDiffContextPositionTop.Checked)
         {
            mode = ConfigurationHelper.DiffContextPosition.Top;
         }
         else if (radioButtonDiffContextPositionLeft.Checked)
         {
            mode = ConfigurationHelper.DiffContextPosition.Left;
         }
         else if (radioButtonDiffContextPositionRight.Checked)
         {
            mode = ConfigurationHelper.DiffContextPosition.Right;
         }
         ConfigurationHelper.SetDiffContextPosition(Program.Settings, mode);
      }

      private void applyDiscussionColumnWidthChange()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         ConfigurationHelper.DiscussionColumnWidth mode = ConfigurationHelper.DiscussionColumnWidth.Narrow;
         if (radioButtonDiscussionColumnWidthNarrow.Checked)
         {
            mode = ConfigurationHelper.DiscussionColumnWidth.Narrow;
         }
         else if (radioButtonDiscussionColumnWidthNarrowPlus.Checked)
         {
            mode = ConfigurationHelper.DiscussionColumnWidth.NarrowPlus;
         }
         else if (radioButtonDiscussionColumnWidthMedium.Checked)
         {
            mode = ConfigurationHelper.DiscussionColumnWidth.Medium;
         }
         else if (radioButtonDiscussionColumnWidthMediumPlus.Checked)
         {
            mode = ConfigurationHelper.DiscussionColumnWidth.MediumPlus;
         }
         else if (radioButtonDiscussionColumnWidthWide.Checked)
         {
            mode = ConfigurationHelper.DiscussionColumnWidth.Wide;
         }
         ConfigurationHelper.SetDiscussionColumnWidth(Program.Settings, mode);
      }

      private void applyWorkflowTypeChange()
      {
         listViewUsers.Enabled = radioButtonSelectByUsernames.Checked;
         listViewProjects.Enabled = radioButtonSelectByProjects.Checked;

         if (_loadingConfiguration)
         {
            return;
         }

         if (radioButtonSelectByProjects.Checked)
         {
            ConfigurationHelper.SelectProjectBasedWorkflow(Program.Settings);
         }
         else
         {
            ConfigurationHelper.SelectUserBasedWorkflow(Program.Settings);
         }

         Trace.TraceInformation("[MainForm] Reconnecting after workflow type change");
         reconnect();
      }

      private void applyGitUsageChange()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         LocalCommitStorageType type = radioButtonDontUseGit.Checked
            ? LocalCommitStorageType.FileStorage
            : (radioButtonUseGitFullClone.Checked
               ? LocalCommitStorageType.FullGitRepository
               : LocalCommitStorageType.ShallowGitRepository);
         ConfigurationHelper.SelectPreferredStorageType(Program.Settings, type);

         Trace.TraceInformation("[MainForm] Reconnecting after storage type change");
         reconnect();
      }

      private void applyNotificationTypeChange(CheckBox checkBox)
      {
         if (_loadingConfiguration)
         {
            return;
         }

         bool state = checkBox.Checked;
         if (checkBox == checkBoxShowNewMergeRequests)
         {
            Program.Settings.Notifications_NewMergeRequests = state;
         }
         else if (checkBox == checkBoxShowMergedMergeRequests)
         {
            Program.Settings.Notifications_MergedMergeRequests = state;
         }
         else if (checkBox == checkBoxShowUpdatedMergeRequests)
         {
            Program.Settings.Notifications_UpdatedMergeRequests = state;
         }
         else if (checkBox == checkBoxShowResolvedAll)
         {
            Program.Settings.Notifications_AllThreadsResolved = state;
         }
         else if (checkBox == checkBoxShowOnMention)
         {
            Program.Settings.Notifications_OnMention = state;
         }
         else if (checkBox == checkBoxShowKeywords)
         {
            Program.Settings.Notifications_Keywords = state;
         }
         else if (checkBox == checkBoxShowMyActivity)
         {
            Program.Settings.Notifications_MyActivity = state;
         }
         else if (checkBox == checkBoxShowServiceNotifications)
         {
            Program.Settings.Notifications_Service = state;
         }
      }

      private void applyAutoSelectionModeChange()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         var mode = ConfigurationHelper.RevisionAutoSelectionMode.LastVsLatest;
         if (radioButtonLastVsLatest.Checked)
         {
            mode = ConfigurationHelper.RevisionAutoSelectionMode.LastVsLatest;
         }
         else if (radioButtonLastVsNext.Checked)
         {
            mode = ConfigurationHelper.RevisionAutoSelectionMode.LastVsNext;
         }
         else if (radioButtonBaseVsLatest.Checked)
         {
            mode = ConfigurationHelper.RevisionAutoSelectionMode.BaseVsLatest;
         }
         ConfigurationHelper.SelectAutoSelectionMode(Program.Settings, mode);
      }

      private void applyShowWarningsOnFileMismatchChange()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         var mode = ConfigurationHelper.ShowWarningsOnFileMismatchMode.Never;
         if (radioButtonShowWarningsNever.Checked)
         {
            mode = ConfigurationHelper.ShowWarningsOnFileMismatchMode.Never;
         }
         else if (radioButtonShowWarningsAlways.Checked)
         {
            mode = ConfigurationHelper.ShowWarningsOnFileMismatchMode.Always;
         }
         else if (radioButtonShowWarningsOnce.Checked)
         {
            mode = ConfigurationHelper.ShowWarningsOnFileMismatchMode.UntilUserIgnoresFile;
         }
         ConfigurationHelper.SetShowWarningsOnFileMismatchMode(Program.Settings, mode);
      }

      private void applyRevisionTypeChange()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         if (radioButtonCommits.Checked)
         {
            ConfigurationHelper.SelectRevisionType(Program.Settings, RevisionType.Commit);
         }
         else
         {
            Debug.Assert(radioButtonVersions.Checked);
            ConfigurationHelper.SelectRevisionType(Program.Settings, RevisionType.Version);
         }
      }

      private void applyFilterChange(bool enabled)
      {
         Program.Settings.DisplayFilterEnabled = enabled;
         if (_mergeRequestFilter != null)
         {
            _mergeRequestFilter.Filter = createMergeRequestFilterState();
         }
      }

      private void applyDiffContextDepthChange()
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.DiffContextDepth = comboBoxDCDepth.Text;
         }
      }

      private void applyDisableSplitterRestrictionsChange(bool disable)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.DisableSplitterRestrictions = disable;
         }
      }

      private void applyColorSchemeChange(string colorSchemeName)
      {
         if (!_loadingConfiguration)
         {
            initializeColorScheme();
            Program.Settings.ColorSchemeFileName = colorSchemeName;
         }
      }

      private void applyThemeChange()
      {
         string theme = comboBoxThemes.SelectedItem.ToString();
         Program.Settings.VisualThemeName = theme;
         applyTheme(theme);
         resetMergeRequestTabMinimumSizes();
      }

      private void applyAutostartSettingChange(bool enabled)
      {
         if (_loadingConfiguration)
         {
            return;
         }

         Program.Settings.RunWhenWindowsStarts = enabled;
         applyAutostartSetting(enabled);
      }

      private void applyMinimizeOnCloseChange(bool value)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.MinimizeOnClose = value;
         }
      }

      private void applyRemindAboutAvailableNewVersionChange(bool value)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.RemindAboutAvailableNewVersion = value;
         }
      }

      private void applyNewDiscussionIsTopMostFormChange(bool value)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.NewDiscussionIsTopMostForm = value;
         }
      }

      private void applyDisableSpellCheckerChange(bool value)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.DisableSpellChecker = value;
         }
      }

      private void applyAutostartSetting(bool enabled)
      {
         if (_allowAutoStartApplication)
         {
            return;
         }

         string command = String.Format("{0} -m", Application.ExecutablePath);
         AutoStartHelper.ApplyAutostartSetting(enabled, "mrHelper", command);
      }

      private void applyFontChange()
      {
         string font = comboBoxFonts.SelectedItem.ToString();
         Program.Settings.MainWindowFontSizeName = font;
         applyFont(font);
      }

      private MergeRequestFilterState createMergeRequestFilterState()
      {
         return new MergeRequestFilterState
         (
            ConfigurationHelper.GetDisplayFilterKeywords(Program.Settings),
            Program.Settings.DisplayFilterEnabled
         );
      }

      private void onSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         Program.Settings.Update();
      }
   }
}

