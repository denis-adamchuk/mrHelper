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

         setAutoSelectionModeChangeRadioValue();
         setShowWarningOnFileMismatchRadioValue();
         setDefaultRevisionTypeRadioValue();
         setMainWindowLayoutRadioValue();
         addFontSizes();
         initializeColorScheme();

         Trace.TraceInformation("[MainForm] Configuration loaded");
         _loadingConfiguration = false;
      }

      private void initializeColorScheme()
      {
         if (Program.Settings.ColorSchemeFileName == String.Empty)
         {
            // Upgrade from old versions which did not have a separate file for Default color scheme
            Program.Settings.ColorSchemeFileName = Constants.DefaultColorSchemeFileName;
         }
         try
         {
            _colorScheme = new ColorScheme(Program.Settings.ColorSchemeFileName);
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot create a color scheme", ex);
         }
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
            _reviewedRevisions =
               new DictionaryWrapper<MergeRequestKey, HashSet<string>>(revisions, saveState);
         }

         new PersistentStateLoadHelper("RecentMergeRequests", reader).Load(out HashSet<MergeRequestKey> mergeRequests);
         new PersistentStateLoadHelper("RecentMergeRequestsWithDateTime", reader).Load(
            out Dictionary<MergeRequestKey, DateTime> mergeRequestsWithDateTime);
         if (mergeRequestsWithDateTime != null)
         {
            _recentMergeRequests =
               new DictionaryWrapper<MergeRequestKey, DateTime>(mergeRequestsWithDateTime, saveState);
         }
         else if (mergeRequests != null)
         {
            // deprecated format
            var recentMergeRequests = mergeRequests.ToDictionary(item => item, item => DateTime.Now);
            _recentMergeRequests =
               new DictionaryWrapper<MergeRequestKey, DateTime>(recentMergeRequests, saveState);
         }

         new PersistentStateLoadHelper("MergeRequestsByHosts", reader).
            Load(out Dictionary<string, MergeRequestKey> mergeRequestsByHosts);
         if (mergeRequestsByHosts != null)
         {
            _lastMergeRequestsByHosts =
               new DictionaryWrapper<string, MergeRequestKey>(mergeRequestsByHosts, saveState);
         }

         new PersistentStateLoadHelper("NewMergeRequestDialogStatesByHosts", reader).Load(
            out Dictionary<string, NewMergeRequestProperties> properties);
         if (properties != null)
         {
            _newMergeRequestDialogStatesByHosts =
               new DictionaryWrapper<string, NewMergeRequestProperties>(properties, saveState);
         }
      }
   }
}

