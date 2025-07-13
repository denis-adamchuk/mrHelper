using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using static mrHelper.App.Helpers.ConfigurationHelper;
using mrHelper.GitLabClient;
using mrHelper.App.Forms.Helpers;
using mrHelper.Common.Interfaces;
using static mrHelper.Common.Constants.Constants;

namespace mrHelper.App.Forms
{
   internal partial class MainForm
   {
      private void setControlStateFromConfiguration()
      {
         _loadingConfiguration = true;
         Trace.TraceInformation("[MainForm] Loading configuration");

         minimizeOnCloseToolStripMenuItem.Checked = Program.Settings.MinimizeOnClose;
         remindAboutNewVersionsToolStripMenuItem.Checked = Program.Settings.RemindAboutAvailableNewVersion;
         runMrHelperWhenWindowsStartsToolStripMenuItem.Checked = Program.Settings.RunWhenWindowsStarts;
         runMrHelperWhenWindowsStartsToolStripMenuItem.Enabled = _allowAutoStartApplication;
         disableSplitterRestrictionsToolStripMenuItem.Checked = Program.Settings.DisableSplitterRestrictions;
         showNewDiscussionOnTopOfAllApplicationsToolStripMenuItem.Checked = Program.Settings.NewDiscussionIsTopMostForm;
         wrapLongRowsToolStripMenuItem.Checked = Program.Settings.WordWrapLongRows;
         showHiddenMergeRequestIdsToolStripMenuItem.Checked = Program.Settings.ShowHiddenMergeRequestIds;
         flatRevisionPreviewToolStripMenuItem.Checked = Program.Settings.FlatRevisionPreview;

         setAutoSelectionModeChangeRadioValue();
         setShowWarningOnFileMismatchRadioValue();
         setDefaultRevisionTypeRadioValue();
         setMainWindowLayoutRadioValue();
         setThemeRadioValue();
         setToolBarPositionRadioValue();
         addFontSizes();

         upgradeOldWorkflowToCommonWorkflow();
         initializeProjectListIfEmpty();

         Trace.TraceInformation("[MainForm] Configuration loaded");
         _loadingConfiguration = false;
      }

      private static void upgradeOldWorkflowToCommonWorkflow()
      {
         if (!ConfigurationHelper.IsCommonWorkflowSelected(Program.Settings))
         {
            if (!ConfigurationHelper.IsOldProjectBasedWorkflowSelected(Program.Settings))
            {
               foreach (string hostname in Program.Settings.KnownHosts)
               {
                  StringToBooleanCollection projects = ConfigurationHelper.GetProjectsForHost(hostname, Program.Settings);
                  StringToBooleanCollection projectsUpdated = new StringToBooleanCollection(projects
                     .Select(project => new Tuple<string, bool>(project.Item1, false)));
                  ConfigurationHelper.SetProjectsForHost(hostname, projectsUpdated, Program.Settings);
               }
            }
            // Intentionally don't switch off "users" if even "Project-based" workflow was used.
            // Most likely user wants to complement projects with users. If not, it is configurable.
            ConfigurationHelper.SelectCommonWorkflow(Program.Settings);
         }
      }

      private void initializeProjectListIfEmpty()
      {
         Program.Settings.KnownHosts
            .Where(hostname => !ConfigurationHelper.GetProjectsForHost(hostname, Program.Settings).Any())
            .ToList()
            .ForEach(hostname =>
            {
               StringToBooleanCollection projects = DefaultWorkflowLoader.GetDefaultProjectsForHost(hostname, false);
               ConfigurationHelper.SetProjectsForHost(hostname, projects, Program.Settings);
            });

         Program.Settings.KnownHosts
            .Where(hostname => !ConfigurationHelper.GetProjectsWithEnvironmentsForHost(hostname, Program.Settings).Any())
            .ToList()
            .ForEach(hostname =>
            {
               StringToBooleanCollection projects = DefaultWorkflowLoader.GetDefaultProjectsWithEnvironments(hostname, true);
               ConfigurationHelper.SetProjectsWithEnvironmentsForHost(hostname, projects, Program.Settings);
            });
      }

      private void initializeColorScheme()
      {
         ColorScheme.Initialize();
      }

      private void upgradeHostList()
      {
         List<string> newKnownHosts = new List<string>();
         List<string> newAccessTokens = new List<string>();
         string[] hosts = Program.Settings.KnownHosts.ToArray();
         for (int iKnownHost = 0; iKnownHost < hosts.Length; ++iKnownHost)
         {
            // Upgrade from old versions which did not have prefix
            string host = StringUtils.GetHostWithPrefix(hosts[iKnownHost]);
            string accessToken = Program.Settings.GetAccessToken(hosts[iKnownHost]);
            if (!newKnownHosts.Contains(host))
            {
               newKnownHosts.Add(host);
               newAccessTokens.Add(accessToken);
            }
         }
         ConfigurationHelper.SetAuthInfo(Enumerable.Zip(newKnownHosts, newAccessTokens,
            (a, b) => new Tuple<string, string>(a, b)), Program.Settings);
      }

      private void setAutoSelectionModeChangeRadioValue()
      {
         RevisionAutoSelectionMode defaultAutoSelectionMode =
            ConfigurationHelper.GetRevisionAutoSelectionMode(Program.Settings);
         switch (defaultAutoSelectionMode)
         {
            case RevisionAutoSelectionMode.LastVsLatest:
               lastReviewedVsLatestToolStripMenuItem.Checked = true;
               break;

            case RevisionAutoSelectionMode.BaseVsLatest:
               baseVsLatestToolStripMenuItem.Checked = true;
               break;

            case RevisionAutoSelectionMode.LastVsNext:
               lastReviewedVsNextToolStripMenuItem.Checked = true;
               break;
         }
      }

      private void setDefaultRevisionTypeRadioValue()
      {
         RevisionType defaultRevisionType = ConfigurationHelper.GetDefaultRevisionType(Program.Settings);
         switch (defaultRevisionType)
         {
            case RevisionType.Commit:
               commitsToolStripMenuItem.Checked = true;
               break;

            case RevisionType.Version:
               versionsToolStripMenuItem.Checked = true;
               break;
         }
      }

      private void setMainWindowLayoutRadioValue()
      {
         MainWindowLayout mainWindowLayout = ConfigurationHelper.GetMainWindowLayout(Program.Settings);
         switch (mainWindowLayout)
         {
            case MainWindowLayout.Horizontal:
               horizontalToolStripMenuItem.Checked = true;
               break;

            case MainWindowLayout.Vertical:
               verticalToolStripMenuItem.Checked = true;
               break;
         }
      }

      private void setThemeRadioValue()
      {
         ColorMode colorMode = ConfigurationHelper.GetColorMode(Program.Settings);
         switch (colorMode)
         {
            case ColorMode.Dark:
               darkToolStripMenuItem.Checked = true;
               break;

            case ColorMode.Light:
               lightToolStripMenuItem.Checked = true;
               break;
         }
      }

      private void setToolBarPositionRadioValue()
      {
         ToolBarPosition ToolBarPosition = ConfigurationHelper.GetToolBarPosition(Program.Settings);
         switch (ToolBarPosition)
         {
            case ToolBarPosition.Top:
               topTBPositionToolStripMenuItem.Checked = true;
               break;

            case ToolBarPosition.Left:
               leftTBPositionToolStripMenuItem.Checked = true;
               break;

            case ToolBarPosition.Right:
               rightTBPositionToolStripMenuItem.Checked = true;
               break;
         }
      }

      private void setShowWarningOnFileMismatchRadioValue()
      {
         var showWarningsOnFileMismatchMode = ConfigurationHelper.GetShowWarningsOnFileMismatchMode(Program.Settings);
         switch (showWarningsOnFileMismatchMode)
         {
            case ConfigurationHelper.ShowWarningsOnFileMismatchMode.Always:
               showWarningsAlwaysToolStripMenuItem.Checked = true;
               break;

            case ConfigurationHelper.ShowWarningsOnFileMismatchMode.Never:
               showWarningsNeverToolStripMenuItem.Checked = true;
               break;

            case ConfigurationHelper.ShowWarningsOnFileMismatchMode.UntilUserIgnoresFile:
               showWarningsUntilIgnoredByUserToolStripMenuItem.Checked = true;
               break;
         }
      }

      private void applyAutoSelectionModeChange()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         var mode = ConfigurationHelper.RevisionAutoSelectionMode.LastVsLatest;
         if (lastReviewedVsLatestToolStripMenuItem.Checked)
         {
            mode = ConfigurationHelper.RevisionAutoSelectionMode.LastVsLatest;
         }
         else if (lastReviewedVsNextToolStripMenuItem.Checked)
         {
            mode = ConfigurationHelper.RevisionAutoSelectionMode.LastVsNext;
         }
         else if (baseVsLatestToolStripMenuItem.Checked)
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
         if (showWarningsNeverToolStripMenuItem.Checked)
         {
            mode = ConfigurationHelper.ShowWarningsOnFileMismatchMode.Never;
         }
         else if (showWarningsAlwaysToolStripMenuItem.Checked)
         {
            mode = ConfigurationHelper.ShowWarningsOnFileMismatchMode.Always;
         }
         else if (showWarningsUntilIgnoredByUserToolStripMenuItem.Checked)
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

         if (commitsToolStripMenuItem.Checked)
         {
            ConfigurationHelper.SelectRevisionType(Program.Settings, RevisionType.Commit);
         }
         else
         {
            Debug.Assert(versionsToolStripMenuItem.Checked);
            ConfigurationHelper.SelectRevisionType(Program.Settings, RevisionType.Version);
         }
      }

      private void applyMainWindowLayoutChange()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         if (horizontalToolStripMenuItem.Checked)
         {
            ConfigurationHelper.SetMainWindowLayout(Program.Settings, MainWindowLayout.Horizontal);
         }
         else
         {
            Debug.Assert(verticalToolStripMenuItem.Checked);
            ConfigurationHelper.SetMainWindowLayout(Program.Settings, MainWindowLayout.Vertical);
         }
      }

      private void applyThemeChange()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         if (darkToolStripMenuItem.Checked)
         {
            ConfigurationHelper.SetColorMode(Program.Settings, Constants.ColorMode.Dark);
         }
         else
         {
            Debug.Assert(lightToolStripMenuItem.Checked);
            ConfigurationHelper.SetColorMode(Program.Settings, Constants.ColorMode.Light);
         }

         initializeColorScheme();
         assignImagesToToolbar();
      }

      private void applyToolbarLayoutChange()
      {
         if (_loadingConfiguration)
         {
            return;
         }

         if (topTBPositionToolStripMenuItem.Checked)
         {
            ConfigurationHelper.SetToolBarPosition(Program.Settings, ToolBarPosition.Top);
         }
         else if (leftTBPositionToolStripMenuItem.Checked)
         {
            ConfigurationHelper.SetToolBarPosition(Program.Settings, ToolBarPosition.Left);
         }
         else
         {
            Debug.Assert(rightTBPositionToolStripMenuItem.Checked);
            ConfigurationHelper.SetToolBarPosition(Program.Settings, ToolBarPosition.Right);
         }
      }

      private void applyDisableSplitterRestrictionsChange(bool disable)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.DisableSplitterRestrictions = disable;
         }
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

      private void applyWordWrapLongRows(bool value)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.WordWrapLongRows = value;
         }
      }

      private void applyShowHiddenMergeRequestIds(bool value)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.ShowHiddenMergeRequestIds = value;
         }
      }

      private void applyFlatRevisionPreview(bool value)
      {
         if (!_loadingConfiguration)
         {
            Program.Settings.FlatRevisionPreview = value;
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

      private void applyAutostartSetting(bool enabled)
      {
         if (!_allowAutoStartApplication)
         {
            return;
         }

         string command = String.Format("{0} -m", Application.ExecutablePath);
         AutoStartHelper.ApplyAutostartSetting(enabled, "mrHelper", command);
      }

      private void applyFontChange()
      {
         foreach (ToolStripMenuItem item in fontSizeToolStripMenuItem.DropDownItems)
         {
            if (item.Checked)
            {
               string font = item.Text;
               Program.Settings.MainWindowFontSizeName = font;
               applyFont(font);
               break;
            }
         }
      }

      protected override void applyFont(string font)
      {
         base.applyFont(font);
         menuStrip1.Font = new Font(menuStrip1.Font.FontFamily, (float)Constants.FontSizeChoices[font],
            menuStrip1.Font.Style, GraphicsUnit.Point, menuStrip1.Font.GdiCharSet, menuStrip1.Font.GdiVerticalFont);
         foreach (ToolStrip toolStrip in new ToolStrip[] { toolStripHosts, toolStripActions, toolStripCustomActions})
         {
            toolStrip.Font = new Font(toolStrip.Font.FontFamily, (float)Constants.FontSizeChoices[font],
               toolStrip.Font.Style, GraphicsUnit.Point, toolStrip.Font.GdiCharSet, toolStrip.Font.GdiVerticalFont);
         }
      }

      private void onPersistentStorageSerialize(IPersistentStateSetter writer)
      {
         new PersistentStateSaveHelper("SelectedHost", writer).Save(_defaultHostName);
         new PersistentStateSaveHelper("ReviewedCommits", writer).Save(_reviewedRevisions.Data);
         new PersistentStateSaveHelper("RecentMergeRequestsWithDateTime", writer).Save(_recentMergeRequests.Data);
         new PersistentStateSaveHelper("MergeRequestsByHosts", writer).Save(_lastMergeRequestsByHosts.Data);
         new PersistentStateSaveHelper("NewMergeRequestDialogStatesByHosts", writer).Save(_newMergeRequestDialogStatesByHosts.Data);
         new PersistentStateSaveHelper("CollapsedProjects_" + Constants.LiveListViewName, writer)
            .Save(_collapsedProjectsLive.Data);
         new PersistentStateSaveHelper("CollapsedProjects_" + Constants.RecentListViewName, writer)
            .Save(_collapsedProjectsRecent.Data);
         new PersistentStateSaveHelper("CollapsedProjects_" + Constants.SearchListViewName, writer)
            .Save(_collapsedProjectsSearch.Data);
         new PersistentStateSaveHelper("MutedMergeRequests_" + Constants.LiveListViewName, writer)
            .Save(_mutedMergeRequests.Data);
         new PersistentStateSaveHelper("FiltersByHostsLive", writer).Save(_filtersByHostsLive.Data
            .ToDictionary(item => item.Key,
                          item => new Tuple<string, string>(item.Value.State.ToString(),
                                                            item.Value.Keywords.ToString())));
         new PersistentStateSaveHelper("FiltersByHostsRecent", writer).Save(_filtersByHostsRecent.Data
            .ToDictionary(item => item.Key,
                          item => new Tuple<string, string>(item.Value.State.ToString(),
                                                            item.Value.Keywords.ToString())));
      }

      private void onPersistentStorageDeserialize(IPersistentStateGetter reader)
      {
         new PersistentStateLoadHelper("SelectedHost", reader).Load(out string hostname);
         if (hostname != null)
         {
            _defaultHostName = StringUtils.GetHostWithPrefix(hostname);
         }

         new PersistentStateLoadHelper("ReviewedCommits", reader).Load(
            out Dictionary<MergeRequestKey, HashSet<string>> revisions);
         if (revisions != null)
         {
            _reviewedRevisions.Assign(revisions);
         }

         new PersistentStateLoadHelper("RecentMergeRequests", reader).Load(out HashSet<MergeRequestKey> mergeRequests);
         new PersistentStateLoadHelper("RecentMergeRequestsWithDateTime", reader).Load(
            out Dictionary<MergeRequestKey, DateTime> mergeRequestsWithDateTime);
         if (mergeRequestsWithDateTime != null)
         {
            _recentMergeRequests.Assign(mergeRequestsWithDateTime);
         }
         else if (mergeRequests != null)
         {
            // deprecated format
            var recentMergeRequests = mergeRequests.ToDictionary(item => item, item => DateTime.Now);
            _recentMergeRequests.Assign(recentMergeRequests);
         }

         new PersistentStateLoadHelper("MergeRequestsByHosts", reader).
            Load(out Dictionary<string, MergeRequestKey> mergeRequestsByHosts);
         if (mergeRequestsByHosts != null)
         {
            _lastMergeRequestsByHosts.Assign(mergeRequestsByHosts);
         }

         new PersistentStateLoadHelper("NewMergeRequestDialogStatesByHosts", reader).Load(
            out Dictionary<string, NewMergeRequestProperties> properties);
         if (properties != null)
         {
            _newMergeRequestDialogStatesByHosts.Assign(properties);
         }

         new PersistentStateLoadHelper("CollapsedProjects_" + Constants.LiveListViewName, reader).Load(
            out HashSet<ProjectKey> collapsedProjectsLiveHashSet);
         if (collapsedProjectsLiveHashSet != null)
         {
            _collapsedProjectsLive.Assign(collapsedProjectsLiveHashSet);
         }

         new PersistentStateLoadHelper("CollapsedProjects_" + Constants.RecentListViewName, reader).Load(
            out HashSet<ProjectKey> collapsedProjectsRecentHashSet);
         if (collapsedProjectsRecentHashSet != null)
         {
            _collapsedProjectsRecent.Assign(collapsedProjectsRecentHashSet);
         }

         new PersistentStateLoadHelper("CollapsedProjects_" + Constants.SearchListViewName, reader).Load(
            out HashSet<ProjectKey> collapsedProjectsSearchHashSet);
         if (collapsedProjectsSearchHashSet != null)
         {
            _collapsedProjectsSearch.Assign(collapsedProjectsSearchHashSet);
         }

         new PersistentStateLoadHelper("MutedMergeRequests_" + Constants.LiveListViewName, reader).Load(
            out Dictionary<MergeRequestKey, DateTime> mutedMergeRequests);
         if (mutedMergeRequests != null)
         {
            _mutedMergeRequests.Assign(mutedMergeRequests);
         }

         new PersistentStateLoadHelper("FiltersByHostsLive", reader).
            Load(out Dictionary<string, Tuple<string, string>> filtersByHostsLive);
         if (filtersByHostsLive != null)
         {
            _filtersByHostsLive.Assign(filtersByHostsLive
               .ToDictionary(item => item.Key,
                             item => new MergeRequestFilterState(item.Value.Item2, readFilterState(item))));
         }

         new PersistentStateLoadHelper("FiltersByHostsRecent", reader).
            Load(out Dictionary<string, Tuple<string, string>> filtersByHostsRecent);
         if (filtersByHostsRecent != null)
         {
            _filtersByHostsRecent.Assign(filtersByHostsRecent
               .ToDictionary(item => item.Key,
                             item => new MergeRequestFilterState(item.Value.Item2, readFilterState(item))));
         }
      }

      private static FilterState readFilterState(KeyValuePair<string, Tuple<string, string>> item)
      {
         if (!Enum.TryParse(item.Value.Item1, out FilterState filterState)
           && bool.TryParse(item.Value.Item1, out bool oldStyleValue)) // before 2.7.7
         {
            filterState = oldStyleValue ? FilterState.Enabled : FilterState.Disabled;
         }
         return filterState;
      }
   }
}

