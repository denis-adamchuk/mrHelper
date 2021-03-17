using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
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

         textBoxStorageFolder.Text = Program.Settings.LocalStorageFolder;
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
         selectDiffContextDepthDropdownValue();
         selectRecentMergeRequestsPerProjectCountDropdownValue();
         setFontFromSettings();
         setThemeFromSettings();
         createMessageFilterFromSettings();

         Trace.TraceInformation("[MainForm] Configuration loaded");
         _loadingConfiguration = false;

         updateRestrictionsInNotifications();
      }

      private void createMessageFilterFromSettings()
      {
         _mergeRequestFilter = new MergeRequestFilter(createMergeRequestFilterState());
         _mergeRequestFilter.FilterChanged += () => updateMergeRequestList(EDataCacheType.Live);
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

      private void selectDiffContextDepthDropdownValue()
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

      private void selectRecentMergeRequestsPerProjectCountDropdownValue()
      {
         Debug.Assert(Constants.RecentMergeRequestPerProjectMinCount <= Constants.RecentMergeRequestPerProjectMaxCount);
         int min = Math.Min(Constants.RecentMergeRequestPerProjectMinCount, Constants.RecentMergeRequestPerProjectMaxCount);
         int max = Math.Max(Constants.RecentMergeRequestPerProjectMinCount, Constants.RecentMergeRequestPerProjectMaxCount);

         comboBoxRecentMergeRequestsPerProjectCount.Items.Clear();
         foreach (int count in Enumerable.Range(min, max - min + 1))
         {
            comboBoxRecentMergeRequestsPerProjectCount.Items.Add(count.ToString());
         }

         if (comboBoxRecentMergeRequestsPerProjectCount.Items.Count == 0)
         {
            comboBoxRecentMergeRequestsPerProjectCount.Items.Add(Constants.RecentMergeRequestPerProjectDefaultCount);
         }

         if (comboBoxRecentMergeRequestsPerProjectCount.Items.Contains(Program.Settings.RecentMergeRequestsPerProjectCount))
         {
            comboBoxRecentMergeRequestsPerProjectCount.Text = Program.Settings.RecentMergeRequestsPerProjectCount;
         }
         else
         {
            comboBoxRecentMergeRequestsPerProjectCount.SelectedIndex = 0;
         }
      }

      private void setKnownHostsDropdownValue()
      {
         // Remove all items except header
         for (int iListViewItem = 1; iListViewItem < listViewKnownHosts.Items.Count; ++iListViewItem)
         {
            listViewKnownHosts.Items.RemoveAt(iListViewItem);
         }

         List<string> newKnownHosts = new List<string>();
         List<string> newAccessTokens = new List<string>();
         string[] hosts = Program.Settings.KnownHosts.ToArray();
         for (int iKnownHost = 0; iKnownHost < hosts.Length; ++iKnownHost)
         {
            // Upgrade from old versions which did not have prefix
            string host = StringUtils.GetHostWithPrefix(hosts[iKnownHost]);
            string accessToken = Program.Settings.GetAccessToken(hosts[iKnownHost]);
            if (addKnownHost(host, accessToken))
            {
               newKnownHosts.Add(host);
               newAccessTokens.Add(accessToken);
            }
         }
         ConfigurationHelper.SetAuthInfo(Enumerable.Zip(newKnownHosts, newAccessTokens,
            (a, b) => new Tuple<string, string>(a, b)), Program.Settings);
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
         Trace.TraceInformation("[MainForm] Adding host {0}", host);

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

      private void fillColorSchemeList()
      {
         if (Program.Settings.ColorSchemeFileName == String.Empty)
         {
            // Upgrade from old versions which did not have a separate file for Default color scheme
            Program.Settings.ColorSchemeFileName = getDefaultColorSchemeFileName();
         }

         string defaultFileName = getDefaultColorSchemeFileName();
         string defaultFilePath = Path.Combine(Directory.GetCurrentDirectory(), defaultFileName);

         comboBoxColorSchemes.Items.Clear();

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
            }
         }
      }

      private void fillColorList()
      {
         comboBoxColorSelector.Items.Clear();
         Constants.ColorSchemeKnownColorNames
            .ToList()
            .ForEach(humanFriendlyName =>
            {
               string colorName = humanFriendlyName.Replace(" ", "");
               Color color = Color.FromName(colorName);
               if (color.A != 0 || color.R != 0 || color.G != 0 || color.B != 0)
               {
                  addColorToList(color, humanFriendlyName);
                  addIconToCache(color);
               }
               else
               {
                  Trace.TraceWarning("[MainForm] Cannot create a Color from name {0}", colorName);
               }
            });
      }

      private void fillColorSchemeItemList()
      {
         listBoxColorSchemeItemSelector.Items.Clear();
         _colorScheme.GetColors("MergeRequests")
            .Concat(_colorScheme.GetColors("Status"))
            .ToList()
            .ForEach(colorSchemeItem =>
            {
               addColorToList(colorSchemeItem.Color);
               addIconToCache(colorSchemeItem.Color);
               listBoxColorSchemeItemSelector.Items.Add(colorSchemeItem.Name);
            });

         if (listBoxColorSchemeItemSelector.Items.Count > 0)
         {
            listBoxColorSchemeItemSelector.SelectedIndex = 0;
         }
      }

      private void addColorToList(Color color, string humanFriendlyName = null)
      {
         if (comboBoxColorSelector.Items
            .Cast<ColorSelectorComboBoxItem>()
            .Select(item => item.Color)
            .Any(itemColor => itemColor.Equals(color)))
         {
            return;
         }

         string colorName = String.IsNullOrEmpty(humanFriendlyName)
            ? color.IsNamedColor ? color.Name : "Custom"
            : humanFriendlyName;
         comboBoxColorSelector.Items.Add(new ColorSelectorComboBoxItem(colorName, color));
      }

      private void addIconToCache(Color color)
      {
         if (_iconCache.ContainsKey(color))
         {
            return;
         }

         Bitmap imageWithoutBorder = WinFormsHelpers.ReplaceColorInBitmap(
            Properties.Resources.gitlab_icon_stub_16x16, Color.Green, color);
         Icon iconWithoutBorder = WinFormsHelpers.ConvertToIco(imageWithoutBorder, 16);

         Bitmap imageWithBorder = WinFormsHelpers.ReplaceColorInBitmap(
            Properties.Resources.gitlab_icon_stub_16x16_border, Color.Green, color);
         Icon iconWithBorder = WinFormsHelpers.ConvertToIco(imageWithBorder, 16);

         _iconCache.Add(color, new IconGroup(iconWithoutBorder, iconWithBorder));
      }

      private void selectColorScheme()
      {
         string selectedScheme = comboBoxColorSchemes.Items
            .Cast<string>()
            .FirstOrDefault(scheme => scheme == Program.Settings.ColorSchemeFileName);
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
            string filepath = Path.Combine(Directory.GetCurrentDirectory(), filename);
            try
            {
               _colorScheme = new ColorScheme(filepath, _expressionResolver);
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
            string message = String.Format("Cannot initialize color scheme {0}",
               comboBoxColorSchemes.Text);
            addOperationRecord(message);

            if (comboBoxColorSchemes.SelectedIndex > 0)
            {
               comboBoxColorSchemes.SelectedIndex = 0;
            }
            else
            {
               _colorScheme = new ColorScheme();
            }
         }

         forEachListView(listView => listView.SetColorScheme(_colorScheme));
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
            ConnectionCheckStatus status = await ConnectionChecker.CheckConnectionAsync(hostname, accessToken);
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
         IEnumerable<string> hosts = listViewKnownHosts
            .Items
            .Cast<ListViewItem>()
            .Select(i => i.Text);
         IEnumerable<string> tokens = listViewKnownHosts
            .Items
            .Cast<ListViewItem>()
            .Select(i => i.SubItems[1].Text);
         ConfigurationHelper.SetAuthInfo(Enumerable.Zip(hosts, tokens,
            (a, b) => new Tuple<string, string>(a, b)), Program.Settings);
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
         string filepath = Path.Combine(Directory.GetCurrentDirectory(), ProjectListFileName);
         if (!System.IO.File.Exists(filepath))
         {
            return;
         }

         try
         {
            ConfigurationHelper.InitializeSelectedProjects(JsonUtils.
               LoadFromFile<IEnumerable<ConfigurationHelper.HostInProjectsFile>>(filepath), Program.Settings);
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

         RawDataAccessor rawDataAccessor = new RawDataAccessor(_gitLabInstance);
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

         RawDataAccessor rawDataAccessor = new RawDataAccessor(_gitLabInstance);
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

         updateRestrictionsInNotifications();

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

      private void updateRestrictionsInNotifications()
      {
         checkBoxShowMergedMergeRequests.Enabled = radioButtonSelectByProjects.Checked;
         checkBoxShowMergedMergeRequests.Checked &= radioButtonSelectByProjects.Checked;
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

      private void applyRecentMergeRequestsPerProjectCount()
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.RecentMergeRequestsPerProjectCount = comboBoxRecentMergeRequestsPerProjectCount.Text;
         }
      }

      private void applyDisableSplitterRestrictionsChange(bool disable)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.DisableSplitterRestrictions = disable;
            resetMergeRequestTabMinimumSizes();
         }
      }

      private void applyColorSchemeChange(string colorSchemeName)
      {
         if (!_loadingConfiguration)
         {
            initializeColorScheme();
            if (Program.Settings.ColorSchemeFileName != colorSchemeName)
            {
               Program.Settings.ColorSchemeFileName = colorSchemeName;
               onResetColorSchemeToFactoryValues();
            }
            fillColorSchemeItemList();
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

      private void onListBoxColorSelected(string colorSchemeItemName)
      {
         ColorSchemeItem colorSchemeItem = _colorScheme.GetColor(colorSchemeItemName);
         if (colorSchemeItem == null)
         {
            Trace.TraceError(
               "[MainForm.onListBoxColorSelected] Cannot find color scheme item {0} in the color scheme",
               colorSchemeItemName);
            return;
         }
         selectComboBoxColor(colorSchemeItem.Color);
      }

      private void onResetColorSchemeToFactoryValues()
      {
         Program.Settings.CustomColors = new Dictionary<string, string>();

         listBoxColorSchemeItemSelector.Refresh();
         updateResetSchemeToFactoryValuesLinkLabelVisibility();
         updateResetToFactoryValueLinkLabelVisibility();
         updateTrayAndTaskBar();
      }

      private void onResetColorSchemeItemToFactoryValue()
      {
         string colorSchemeItemName = listBoxColorSchemeItemSelector.SelectedItem.ToString();
         ColorSchemeItem colorSchemeItem = _colorScheme.GetColor(colorSchemeItemName);
         if (colorSchemeItem == null)
         {
            Trace.TraceError(
               "[MainForm.onResetColorSchemeItemToFactoryValue] Cannot find color scheme item {0} in the color scheme",
               colorSchemeItemName);
            return;
         }
         selectComboBoxColor(colorSchemeItem.FactoryColor);
      }

      private void selectComboBoxColor(Color color)
      {
         comboBoxColorSelector.SelectedItem = null;
         ColorSelectorComboBoxItem comboBoxItem = comboBoxColorSelector.Items
            .Cast<ColorSelectorComboBoxItem>()
            .FirstOrDefault(item => item.Color.Equals(color));
         if (comboBoxItem == null)
         {
            Debug.Assert(false);
            Trace.TraceError("[MainForm] Cannot find color {0} in comboBoxColorSelector", color.ToString());
            return;
         }
         comboBoxColorSelector.SelectedItem = comboBoxItem;
      }

      private void onComboBoxColorSelected(Color color)
      {
         object selectedItem = listBoxColorSchemeItemSelector.SelectedItem;
         if (selectedItem == null)
         {
            return;
         }

         string colorSchemeItemName = selectedItem.ToString();
         setColorForColorSchemeItem(colorSchemeItemName, color);

         listBoxColorSchemeItemSelector.Refresh();
         updateResetSchemeToFactoryValuesLinkLabelVisibility();
         updateResetToFactoryValueLinkLabelVisibility();
         updateTrayAndTaskBar();
      }

      private void setColorForColorSchemeItem(string colorSchemeItemName, Color color)
      {
         ColorSchemeItem colorSchemeItem = colorSchemeItemName != null
            ? _colorScheme.GetColor(colorSchemeItemName) : null;
         if (colorSchemeItem != null && !color.Equals(colorSchemeItem.Color))
         {
            Dictionary<string, string> dict = Program.Settings.CustomColors;
            if (colorSchemeItem.FactoryColor.Equals(color))
            {
               dict.Remove(colorSchemeItem.Name);
            }
            else
            {
               string colorAsText = color.IsNamedColor
                  ? color.Name : String.Format("{0},{1},{2},{3}", color.A, color.R, color.G, color.B);
               dict[colorSchemeItem.Name] = colorAsText;
            }
            Program.Settings.CustomColors = dict;
         }
      }

      private void updateResetSchemeToFactoryValuesLinkLabelVisibility()
      {
         linkLabelResetAllColors.Visible = listBoxColorSchemeItemSelector.Items
            .Cast<string>()
            .Any(itemName => !isColorSchemeItemHasFactoryValue(itemName));
      }

      private void updateResetToFactoryValueLinkLabelVisibility()
      {
         object selectedItem = listBoxColorSchemeItemSelector.SelectedItem;
         if (selectedItem == null)
         {
            return;
         }
         linkLabelResetToFactoryValue.Visible = !isColorSchemeItemHasFactoryValue(selectedItem.ToString());
      }

      private bool isColorSchemeItemHasFactoryValue(string colorSchemeItemName)
      {
         ColorSchemeItem updatedColorSchemeItem = _colorScheme.GetColor(colorSchemeItemName);
         return updatedColorSchemeItem == null
             || updatedColorSchemeItem.Color.Name == updatedColorSchemeItem.FactoryColor.Name;
      }

      private void onDrawListBoxColorSchemeItemSelectorItem(DrawItemEventArgs e)
      {
         if (e.Index < 0)
         {
            return;
         }

         string colorSchemeItemName = listBoxColorSchemeItemSelector.Items[e.Index].ToString();
         ColorSchemeItem colorSchemeItem = _colorScheme.GetColor(colorSchemeItemName);
         if (colorSchemeItem == null)
         {
            return;
         }

         Color color = colorSchemeItem.Color;
         bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
         WinFormsHelpers.FillRectangle(e, e.Bounds, color, isSelected);

         StringFormat format =
            new StringFormat
            {
               Trimming = StringTrimming.EllipsisCharacter,
               FormatFlags = StringFormatFlags.NoWrap
            };

         string text = colorSchemeItem.DisplayName;
         Font font = listBoxColorSchemeItemSelector.Font;
         if (isSelected)
         {
            using (Brush brush = new SolidBrush(color))
            {
               e.Graphics.DrawString(text, font, brush, e.Bounds, format);
            }
         }
         else
         {
            e.Graphics.DrawString(text, font, SystemBrushes.ControlText, e.Bounds, format);
         }
      }

      private void onMeasureListBoxColorSchemeItemSelectorItem(MeasureItemEventArgs e)
      {
         if (e.Index >= 0)
         {
            e.ItemHeight = listBoxColorSchemeItemSelector.Font.Height + 2;
         }
      }

      private void onDrawComboBoxColorSelectorItem(DrawItemEventArgs e)
      {
         if (e.Index < 0)
         {
            return;
         }

         e.DrawBackground();
         e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

         int iconMargin = 5;
         int iconSize = e.Bounds.Height; // draw square icon
         ColorSelectorComboBoxItem item = (ColorSelectorComboBoxItem)(comboBoxColorSelector.Items[e.Index]);
         Icon icon = getCachedIcon(item.Color);
         if (icon != null)
         {
            Rectangle iconRect = new Rectangle(e.Bounds.X + iconMargin, e.Bounds.Y, iconSize, iconSize);
            e.Graphics.DrawIcon(icon, iconRect);
         }

         int iconTextMargin = 5;
         Rectangle textRect = new Rectangle(
            e.Bounds.X + iconMargin + iconSize + iconTextMargin, e.Bounds.Y,
            e.Bounds.Width - iconMargin - iconSize - iconTextMargin, e.Bounds.Height);
         bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
         Brush textBrush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;
         e.Graphics.DrawString(item.HumanFriendlyName, comboBoxColorSelector.Font, textBrush, textRect);
      }
   }
}

