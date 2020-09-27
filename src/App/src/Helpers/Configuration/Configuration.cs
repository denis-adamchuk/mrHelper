using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;
using System.Diagnostics;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Helpers
{
   class ChangesNotAllowedException : Exception {}

   public class UserDefinedSettings : INotifyPropertyChanged, IHostProperties
   {
      private static readonly string KnownHostsKeyName = "KnownHosts";
      private static readonly string[] KnownHostsDefaultValue = Array.Empty<string>();

      private static readonly string KnownAccessTokensKeyName = "KnownAccessTokens";
      private static readonly string[] KnownAccessTokensDefaultValue = Array.Empty<string>();

      private static readonly string LocalGitFolderKeyName = "LocalGitFolder";
      private static readonly string LocalGitFolderDefaultValue = Environment.GetEnvironmentVariable("TEMP");

      private static readonly string AutoSelectionModeKeyName      = "AutoSelectionMode";
      private static readonly string AutoSelectionModeDefaultValue = "LastVsLatest";

      private static readonly string RevisionTypeKeyName      = "RevisionType";
      private static readonly string RevisionTypeDefaultValue = "Version";

      private static readonly string GitUsageForStorageKeyName       = "GitUsageForStorage";
      private static readonly string GitUsageForStorageDefaultValue  = "DontUseGit";

      private static readonly string AllowAuthorToTrackTimeKeyName      = "AllowAuthorToTrackTime";
      private static readonly bool   AllowAuthorToTrackTimeDefaultValue = false;

      private static readonly string CheckedLabelsFilterKeyName = "CheckedLabelsFilter";
      private static readonly bool   CheckedLabelsFilterDefaultValue = false;

      private static readonly string LastUsedLabelsKeyName = "LastUsedLabels";
      private static readonly string LastUsedLabelsDefaultValue = "";

      private static readonly string ShowPublicOnlyKeyName = "ShowPublicOnly";
      private static readonly bool   ShowPublicOnlyDefaultValue = true;

      private static readonly string DiffContextDepthKeyName = "DiffContextDepth";
      private static readonly string DiffContextDepthDefaultValue = "2";

      private static readonly string MinimizeOnCloseKeyName = "MinimizeOnClose";
      private static readonly bool   MinimizeOnCloseDefaultValue = false;

      private static readonly string RunWhenWindowsStartsKeyName        = "RunWhenWindowsStarts";
      private static readonly bool   RunWhenWindowsStartsDefaultValue   = false;

      private static readonly string DisableSpellCheckerKeyName      = "DisableSpellChecker";
      private static readonly bool   DisableSpellCheckerDefaultValue = false;

      private static readonly string WasMaximizedBeforeCloseKeyName       = "WasMaximizedBeforeClose";
      private static readonly bool   WasMaximizedBeforeCloseDefaultValue  = true;

      private static readonly string DisableSplitterRestrictionsKeyName = "DisableSplitterRestrictions";
      private static readonly bool   DisableSplitterRestrictionsDefaultValue = false;

      private static readonly string NewDiscussionIsTopMostFormKeyName        = "NewDiscussionIsTopMostForm";
      private static readonly bool   NewDiscussionIsTopMostFormDefaultValue   = false;

      private static readonly string ShowWarningsOnFileMismatchKeyName       = "ShowWarningsOnFileMismatch";
      private static readonly string ShowWarningsOnFileMismatchDefaultValue  = "until_user_ignores_file";

      private static readonly string ShowWarningOnReloadListKeyName      = "ShowWarningOnReloadList";
      private static readonly bool   ShowWarningOnReloadListDefaultValue = true;

      private static readonly string ShowWarningOnFilterMigrationKeyName      = "ShowWarningOnFilterMigration";
      private static readonly bool   ShowWarningOnFilterMigrationDefaultValue = true;

      private static readonly string ShowWarningOnHideToTrayKeyName      = "ShowWarningOnHideToTray";
      private static readonly bool   ShowWarningOnHideToTrayDefaultValue = true;

      private static readonly string ColorSchemeFileNameKeyName = "ColorSchemeFileName";
      private static readonly string ColorSchemeFileNameDefaultValue = "";

      private static readonly string AutoUpdatePeriodMsKeyName      = "AutoUpdatePeriodMs";
      private static readonly int    AutoUpdatePeriodMsDefaultValue = 5 * 60 * 1000; // 5 minutes

      private static readonly string OneShotUpdateFirstChanceDelayMsKeyName        = "OneShotUpdateFirstChanceDelayMs";
      private static readonly int    OneShotUpdateFirstChanceDelayMsDefaultValue   = 5 * 1000; // 5 seconds

      private static readonly string OneShotUpdateSecondChanceDelayMsKeyName        = "OneShotUpdateSecondChanceDelayMs";
      private static readonly int    OneShotUpdateSecondChanceDelayMsDefaultValue   = 15 * 1000; // 15 seconds

      private static readonly string OneShotUpdateOnNewMergeRequestFirstChanceDelayMsKeyName        =
         "OneShotUpdateOnNewMergeRequestFirstChanceDelayMs";
      private static readonly int    OneShotUpdateOnNewMergeRequestFirstChanceDelayMsDefaultValue   =
         30 * 1000; // 30 seconds

      private static readonly string OneShotUpdateOnNewMergeRequestSecondChanceDelayMsKeyName        =
         "OneShotUpdateOnNewMergeRequestSecondChanceDelayMs";
      private static readonly int    OneShotUpdateOnNewMergeRequestSecondChanceDelayMsDefaultValue   =
         60 * 1000; // 60 seconds

      private static readonly string CacheRevisionsPeriodMsKeyName        = "CacheRevisionsPeriodMs";
      private static readonly int    CacheRevisionsPeriodMsDefaultValue   = 8 * 60 * 1000; // 8 minutes

      private static readonly string UseGitBasedSizeCollectionKeyName      = "UseGitBasedSizeCollection";
      private static readonly bool   UseGitBasedSizeCollectionDefaultValue = false;

      private static readonly string DisableSSLVerificationKeyName      = "DisableSSLVerification";
      private static readonly bool   DisableSSLVerificationDefaultValue = true;

      private static readonly string LogFilesToKeepKeyName = "LogFilesToKeep";
      private static readonly int    LogFilesToKeepDefaultValue = 10;

      private static readonly string RevisionsToKeepKeyName = "FileStorageRevisionsToKeep";
      private static readonly int    RevisionsToKeepDefaultValue = 100;

      private static readonly string ComparisonsToKeepKeyName = "FileStorageComparisonsToKeep";
      private static readonly int    ComparisonsToKeepDefaultValue = 200;

      private static readonly string Notifications_NewMergeRequests_KeyName      = "Notifications_NewMergeRequests";
      private static readonly bool   Notifications_NewMergeRequests_DefaultValue = true;

      private static readonly string Notifications_MergedMergeRequests_KeyName      = "Notifications_MergedMergeRequests";
      private static readonly bool   Notifications_MergedMergeRequests_DefaultValue = true;

      private static readonly string Notifications_UpdatedMergeRequests_KeyName      = "Notifications_UpdatedMergeRequests";
      private static readonly bool   Notifications_UpdatedMergeRequests_DefaultValue = true;

      private static readonly string Notifications_AllThreadsResolved_KeyName      = "Notifications_AllThreadsResolved";
      private static readonly bool   Notifications_AllThreadsResolved_DefaultValue = true;

      private static readonly string Notifications_OnMention_KeyName      = "Notifications_OnMention";
      private static readonly bool   Notifications_OnMention_DefaultValue = true;

      private static readonly string Notifications_Keywords_KeyName      = "Notifications_Keywords";
      private static readonly bool   Notifications_Keywords_DefaultValue = true;

      private static readonly string Notifications_MyActivity_KeyName      = "Notifications_MyActivity";
      private static readonly bool   Notifications_MyActivity_DefaultValue = false;

      private static readonly string Notifications_Service_KeyName      = "Notifications_Service";
      private static readonly bool   Notifications_Service_DefaultValue = false;

      private static readonly string ListViewMergeRequestsColumnWidthsKeyName           = "LVMR_ColWidths";
      private static readonly string ListViewMergeRequestsColumnWidthsDefaultValue      = String.Empty;
      private static readonly int    ListViewMergeRequestsSingleColumnWidthDefaultValue = 100;

      private static readonly string ListViewMergeRequestsDisplayIndicesKeyName      = "LVMR_DisplayIndices";
      private static readonly string ListViewMergeRequestsDisplayIndicesDefaultValue = String.Empty;

      private static readonly string ListViewFoundMergeRequestsColumnWidthsKeyName           = "LVFMR_ColWidths";
      private static readonly string ListViewFoundMergeRequestsColumnWidthsDefaultValue      = String.Empty;
      private static readonly int    ListViewFoundMergeRequestsSingleColumnWidthDefaultValue = 100;

      private static readonly string ListViewFoundMergeRequestsDisplayIndicesKeyName      = "LVFMR_DisplayIndices";
      private static readonly string ListViewFoundMergeRequestsDisplayIndicesDefaultValue = String.Empty;

      private static readonly string RevisionBrowserColumnWidthsKeyName           = "RB_ColWidths";
      private static readonly string RevisionBrowserColumnWidthsDefaultValue      = String.Empty;
      private static readonly int    RevisionBrowserSingleColumnWidthDefaultValue = 100;

      private static readonly string MainWindowSplitterDistanceKeyName      = "MWSplitterDistance";
      private static readonly int    MainWindowSplitterDistanceDefaultValue = 0;

      private static readonly string RightPaneSplitterDistanceKeyName      = "RPSplitterDistance";
      private static readonly int    RightPaneSplitterDistanceDefaultValue = 0;

      private static readonly string VisualThemeNameKeyName       = "VisualThemeName";
      private static readonly string VisualThemeNameDefaultValue  =
         Constants.DefaultThemeName;

      private static readonly string WorkflowTypeKeyName      = "WorkflowType";
      private static readonly string WorkflowTypeDefaultValue = "User";

      private static readonly string SelectedUsersKeyName      = "SelectedUsers";
      private static readonly string SelectedUsersDefaultValue = String.Empty;

      private static readonly string SelectedProjectsKeyName      = "SelectedProjects";
      private static readonly string SelectedProjectsDefaultValue = String.Empty;

      private static readonly string SelectedProjectsUpgradedKeyName      = "SelectedProjectsUpgraded";
      private static readonly bool   SelectedProjectsUpgradedDefaultValue = false;

      private static readonly string MainWindowFontSizeNameKeyName       = "MWFontSize";
      private static readonly string MainWindowFontSizeNameDefaultValue  =
         Constants.DefaultMainWindowFontSizeChoice;

      public event PropertyChangedEventHandler PropertyChanged;

      internal UserDefinedSettings()
      {
         string configFileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName) + ".config";
         string configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               Constants.ApplicationDataFolderName, configFileName);

         ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap
         {
            ExeConfigFilename = configFilePath
         };

         _config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
      }

      internal void Update()
      {
         try
         {
            _config.Save(ConfigurationSaveMode.Full);
         }
         catch (System.Configuration.ConfigurationErrorsException ex)
         {
            ExceptionHandlers.Handle("Cannot save configuration to disk", ex);
         }
         ConfigurationManager.RefreshSection("appSettings");
      }

      // TODO Sync KnownHosts and KnownAccessTokens
      public string[] KnownHosts
      {
         get { return getValues(KnownHostsKeyName, KnownHostsDefaultValue).ToArray(); }
         set { setValues(KnownHostsKeyName, value); }
      }

      public string[] KnownAccessTokens
      {
         get { return getValues(KnownAccessTokensKeyName, KnownAccessTokensDefaultValue).ToArray(); }
         set { setValues(KnownAccessTokensKeyName, value); }
      }

      public string LocalGitFolder
      {
         get { return getValue(LocalGitFolderKeyName, LocalGitFolderDefaultValue); }
         set { setValue(LocalGitFolderKeyName, value); }
      }

      public string AutoSelectionMode
      {
         get { return getValue(AutoSelectionModeKeyName, AutoSelectionModeDefaultValue); }
         set { setValue(AutoSelectionModeKeyName, value); }
      }

      public string RevisionType
      {
         get { return getValue(RevisionTypeKeyName, RevisionTypeDefaultValue); }
         set { setValue(RevisionTypeKeyName, value); }
      }

      public string GitUsageForStorage
      {
         get { return getValue(GitUsageForStorageKeyName, GitUsageForStorageDefaultValue); }
         set { setValue(GitUsageForStorageKeyName, value); }
      }

      public bool AllowAuthorToTrackTime
      {
         get
         {
            return bool.TryParse(getValue(
               AllowAuthorToTrackTimeKeyName, boolToString(AllowAuthorToTrackTimeDefaultValue)),
                  out bool result) ? result : AllowAuthorToTrackTimeDefaultValue;
         }
         set { setValue(AllowAuthorToTrackTimeKeyName, boolToString(value)); }
      }

      public bool DisplayFilterEnabled
      {
         get
         {
            return bool.TryParse(getValue(
               CheckedLabelsFilterKeyName, boolToString(CheckedLabelsFilterDefaultValue)),
                  out bool result) ? result : CheckedLabelsFilterDefaultValue;
         }
         set { setValue(CheckedLabelsFilterKeyName, boolToString(value)); }
      }

      public string DisplayFilter
      {
         get { return getValue(LastUsedLabelsKeyName, LastUsedLabelsDefaultValue); }
         set { setValue(LastUsedLabelsKeyName, value); }
      }

      public bool ShowPublicOnly
      {
         get
         {
            return bool.TryParse(getValue(
               ShowPublicOnlyKeyName, boolToString(ShowPublicOnlyDefaultValue)),
                  out bool result) ? result : ShowPublicOnlyDefaultValue;
         }
         set { setValue(ShowPublicOnlyKeyName, boolToString(value)); }
      }

      public bool MinimizeOnClose
      {
         get
         {
            return bool.TryParse(getValue(
               MinimizeOnCloseKeyName, boolToString(MinimizeOnCloseDefaultValue)),
                  out bool result) ? result : MinimizeOnCloseDefaultValue;
         }
         set { setValue(MinimizeOnCloseKeyName, boolToString(value)); }
      }

      public bool RunWhenWindowsStarts
      {
         get
         {
            return bool.TryParse(getValue(
               RunWhenWindowsStartsKeyName, boolToString(RunWhenWindowsStartsDefaultValue)),
                  out bool result) ? result : RunWhenWindowsStartsDefaultValue;
         }
         set { setValue(RunWhenWindowsStartsKeyName, boolToString(value)); }
      }

      public bool DisableSpellChecker
      {
         get
         {
            return bool.TryParse(getValue(
               DisableSpellCheckerKeyName, boolToString(DisableSpellCheckerDefaultValue)),
                  out bool result) ? result : DisableSpellCheckerDefaultValue;
         }
         set { setValue(DisableSpellCheckerKeyName, boolToString(value)); }
      }

      public bool WasMaximizedBeforeClose
      {
         get
         {
            return bool.TryParse(getValue(
               WasMaximizedBeforeCloseKeyName, boolToString(WasMaximizedBeforeCloseDefaultValue)),
                  out bool result) ? result : WasMaximizedBeforeCloseDefaultValue;
         }
         set { setValue(WasMaximizedBeforeCloseKeyName, boolToString(value)); }
      }

      public bool DisableSplitterRestrictions
      {
         get
         {
            return bool.TryParse(getValue(
               DisableSplitterRestrictionsKeyName, boolToString(DisableSplitterRestrictionsDefaultValue)),
                  out bool result) ? result : DisableSplitterRestrictionsDefaultValue;
         }
         set { setValue(DisableSplitterRestrictionsKeyName, boolToString(value)); }
      }

      public bool NewDiscussionIsTopMostForm
      {
         get
         {
            return bool.TryParse(getValue(
               NewDiscussionIsTopMostFormKeyName, boolToString(NewDiscussionIsTopMostFormDefaultValue)),
                  out bool result) ? result : NewDiscussionIsTopMostFormDefaultValue;
         }
         set { setValue(NewDiscussionIsTopMostFormKeyName, boolToString(value)); }
      }

      public string ShowWarningsOnFileMismatchMode
      {
         get { return getValue(ShowWarningsOnFileMismatchKeyName, ShowWarningsOnFileMismatchDefaultValue); }
         set { setValue(ShowWarningsOnFileMismatchKeyName, value); }
      }

      public bool ShowWarningOnReloadList
      {
         get
         {
            return bool.TryParse(getValue(
               ShowWarningOnReloadListKeyName, boolToString(ShowWarningOnReloadListDefaultValue)),
                  out bool result) ? result : ShowWarningOnReloadListDefaultValue;
         }
         set { setValue(ShowWarningOnReloadListKeyName, boolToString(value)); }
      }

      public bool ShowWarningOnFilterMigration
      {
         get
         {
            return bool.TryParse(getValue(
               ShowWarningOnFilterMigrationKeyName, boolToString(ShowWarningOnFilterMigrationDefaultValue)),
                  out bool result) ? result : ShowWarningOnFilterMigrationDefaultValue;
         }
         set { setValue(ShowWarningOnFilterMigrationKeyName, boolToString(value)); }
      }

      public bool ShowWarningOnHideToTray
      {
         get
         {
            return bool.TryParse(getValue(
               ShowWarningOnHideToTrayKeyName, boolToString(ShowWarningOnHideToTrayDefaultValue)),
                  out bool result) ? result : ShowWarningOnHideToTrayDefaultValue;
         }
         set { setValue(ShowWarningOnHideToTrayKeyName, boolToString(value)); }
      }

      public string DiffContextDepth
      {
         get { return getValue(DiffContextDepthKeyName, DiffContextDepthDefaultValue); }
         set { setValue(DiffContextDepthKeyName, value); }
      }

      public string ColorSchemeFileName
      {
         get { return getValue(ColorSchemeFileNameKeyName, ColorSchemeFileNameDefaultValue); }
         set { setValue(ColorSchemeFileNameKeyName, value); }
      }

      public string VisualThemeName
      {
         get { return getValue(VisualThemeNameKeyName, VisualThemeNameDefaultValue); }
         set { setValue(VisualThemeNameKeyName, value); }
      }

      public string MainWindowFontSizeName
      {
         get { return getValue(MainWindowFontSizeNameKeyName, MainWindowFontSizeNameDefaultValue); }
         set { setValue(MainWindowFontSizeNameKeyName, value); }
      }

      public int LogFilesToKeep
      {
         get
         {
            return int.TryParse(getValue(
               LogFilesToKeepKeyName, LogFilesToKeepDefaultValue.ToString()),
                  out int result) ? result : LogFilesToKeepDefaultValue;
         }
         set { setValue(LogFilesToKeepKeyName, value.ToString()); }
      }

      public int RevisionsToKeep
      {
         get
         {
            return int.TryParse(getValue(
               RevisionsToKeepKeyName, RevisionsToKeepDefaultValue.ToString()),
                  out int result) ? result : RevisionsToKeepDefaultValue;
         }
         set { setValue(RevisionsToKeepKeyName, value.ToString()); }
      }

      public int ComparisonsToKeep
      {
         get
         {
            return int.TryParse(getValue(
               ComparisonsToKeepKeyName, ComparisonsToKeepDefaultValue.ToString()),
                  out int result) ? result : ComparisonsToKeepDefaultValue;
         }
         set { setValue(ComparisonsToKeepKeyName, value.ToString()); }
      }

      public bool Notifications_NewMergeRequests
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_NewMergeRequests_KeyName, boolToString(Notifications_NewMergeRequests_DefaultValue)),
                  out bool result) ? result : Notifications_NewMergeRequests_DefaultValue;
         }
         set { setValue(Notifications_NewMergeRequests_KeyName, boolToString(value)); }
      }

      public bool Notifications_MergedMergeRequests
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_MergedMergeRequests_KeyName, boolToString(Notifications_MergedMergeRequests_DefaultValue)),
                  out bool result) ? result : Notifications_MergedMergeRequests_DefaultValue;
         }
         set { setValue(Notifications_MergedMergeRequests_KeyName, boolToString(value)); }
      }

      public bool Notifications_UpdatedMergeRequests
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_UpdatedMergeRequests_KeyName, boolToString(Notifications_UpdatedMergeRequests_DefaultValue)),
                  out bool result) ? result : Notifications_UpdatedMergeRequests_DefaultValue;
         }
         set { setValue(Notifications_UpdatedMergeRequests_KeyName, boolToString(value)); }
      }

      public bool Notifications_AllThreadsResolved
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_AllThreadsResolved_KeyName, boolToString(Notifications_AllThreadsResolved_DefaultValue)),
                  out bool result) ? result : Notifications_AllThreadsResolved_DefaultValue;
         }
         set { setValue(Notifications_AllThreadsResolved_KeyName, boolToString(value)); }
      }

      public bool Notifications_OnMention
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_OnMention_KeyName, boolToString(Notifications_OnMention_DefaultValue)),
                  out bool result) ? result : Notifications_OnMention_DefaultValue;
         }
         set { setValue(Notifications_OnMention_KeyName, boolToString(value)); }
      }

      public bool Notifications_Keywords
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_Keywords_KeyName, boolToString(Notifications_Keywords_DefaultValue)),
                  out bool result) ? result : Notifications_Keywords_DefaultValue;
         }
         set { setValue(Notifications_Keywords_KeyName, boolToString(value)); }
      }

      public bool Notifications_MyActivity
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_MyActivity_KeyName, boolToString(Notifications_MyActivity_DefaultValue)),
                  out bool result) ? result : Notifications_MyActivity_DefaultValue;
         }
         set { setValue(Notifications_MyActivity_KeyName, boolToString(value)); }
      }

      public bool Notifications_Service
      {
         get
         {
            return bool.TryParse(getValue(
               Notifications_Service_KeyName, boolToString(Notifications_Service_DefaultValue)),
                  out bool result) ? result : Notifications_Service_DefaultValue;
         }
         set { setValue(Notifications_Service_KeyName, boolToString(value)); }
      }

      public Dictionary<string, int> ListViewMergeRequestsColumnWidths
      {
         get
         {
            return getStringToIntDictionary(ListViewMergeRequestsColumnWidthsKeyName,
                                            ListViewMergeRequestsColumnWidthsDefaultValue,
                                            ListViewMergeRequestsSingleColumnWidthDefaultValue, -1);
         }
         set
         {
            setStringToIntDictionary(ListViewMergeRequestsColumnWidthsKeyName, value);
         }
      }

      public Dictionary<string, int> ListViewMergeRequestsDisplayIndices
      {
         get
         {
            return getStringToIntDictionary(ListViewMergeRequestsDisplayIndicesKeyName,
                                            ListViewMergeRequestsDisplayIndicesDefaultValue, -1, -1);
         }
         set
         {
            setStringToIntDictionary(ListViewMergeRequestsDisplayIndicesKeyName, value);
         }
      }

      public Dictionary<string, int> ListViewFoundMergeRequestsColumnWidths
      {
         get
         {
            return getStringToIntDictionary(ListViewFoundMergeRequestsColumnWidthsKeyName,
                                            ListViewFoundMergeRequestsColumnWidthsDefaultValue,
                                            ListViewFoundMergeRequestsSingleColumnWidthDefaultValue, -1);
         }
         set
         {
            setStringToIntDictionary(ListViewFoundMergeRequestsColumnWidthsKeyName, value);
         }
      }

      public Dictionary<string, int> ListViewFoundMergeRequestsDisplayIndices
      {
         get
         {
            return getStringToIntDictionary(ListViewFoundMergeRequestsDisplayIndicesKeyName,
                                            ListViewFoundMergeRequestsDisplayIndicesDefaultValue, -1, -1);
         }
         set
         {
            setStringToIntDictionary(ListViewFoundMergeRequestsDisplayIndicesKeyName, value);
         }
      }

      public Dictionary<string, int> RevisionBrowserColumnWidths
      {
         get
         {
            return getStringToIntDictionary(RevisionBrowserColumnWidthsKeyName,
                                            RevisionBrowserColumnWidthsDefaultValue,
                                            RevisionBrowserSingleColumnWidthDefaultValue, -1);
         }
         set
         {
            setStringToIntDictionary(RevisionBrowserColumnWidthsKeyName, value);
         }
      }

      private Dictionary<string, int> getStringToIntDictionary(string keyName, string defaultValue,
         int fallbackValue, int errorValue)
      {
         return DictionaryStringHelper.DeserializeRawDictionaryString(getValue(keyName, defaultValue), false)
            .ToDictionary(
               item => item.Key,
               item => int.TryParse(item.Value, out int result) ? result : fallbackValue)
            .Where(x => x.Value != errorValue)
            .ToDictionary(
               item => item.Key,
               item => item.Value);
      }

      private void setStringToIntDictionary(string keyName, Dictionary<string, int> value)
      {
         setValue(keyName, DictionaryStringHelper.SerializeRawDictionaryString(
            value.ToDictionary(item => item.Key, item => item.Value.ToString())));
      }

      public int MainWindowSplitterDistance
      {
         get
         {
            return int.TryParse(getValue(
               MainWindowSplitterDistanceKeyName, MainWindowSplitterDistanceDefaultValue.ToString()),
                  out int result) ? result : MainWindowSplitterDistanceDefaultValue;
         }
         set { setValue(MainWindowSplitterDistanceKeyName, value.ToString()); }
      }

      public int RightPaneSplitterDistance
      {
         get
         {
            return int.TryParse(getValue(
               RightPaneSplitterDistanceKeyName, RightPaneSplitterDistanceDefaultValue.ToString()),
                  out int result) ? result : RightPaneSplitterDistanceDefaultValue;
         }
         set { setValue(RightPaneSplitterDistanceKeyName, value.ToString()); }
      }

      public int AutoUpdatePeriodMs
      {
         get
         {
            return int.TryParse(getValue(
               AutoUpdatePeriodMsKeyName, AutoUpdatePeriodMsDefaultValue.ToString()),
                  out int result) ? result : AutoUpdatePeriodMsDefaultValue;
         }
         set { setValue(AutoUpdatePeriodMsKeyName, value.ToString()); }
      }

      public int OneShotUpdateFirstChanceDelayMs
      {
         get
         {
            return int.TryParse(getValue(
               OneShotUpdateFirstChanceDelayMsKeyName, OneShotUpdateFirstChanceDelayMsDefaultValue.ToString()),
                  out int result) ? result : OneShotUpdateFirstChanceDelayMsDefaultValue;
         }
         set { setValue(OneShotUpdateFirstChanceDelayMsKeyName, value.ToString()); }
      }

      public int OneShotUpdateSecondChanceDelayMs
      {
         get
         {
            return int.TryParse(getValue(
               OneShotUpdateSecondChanceDelayMsKeyName, OneShotUpdateSecondChanceDelayMsDefaultValue.ToString()),
                  out int result) ? result : OneShotUpdateSecondChanceDelayMsDefaultValue;
         }
         set { setValue(OneShotUpdateSecondChanceDelayMsKeyName, value.ToString()); }
      }

      public int OneShotUpdateOnNewMergeRequestFirstChanceDelayMs
      {
         get
         {
            return int.TryParse(getValue(
               OneShotUpdateOnNewMergeRequestFirstChanceDelayMsKeyName,
               OneShotUpdateOnNewMergeRequestFirstChanceDelayMsDefaultValue.ToString()),
                  out int result) ? result : OneShotUpdateOnNewMergeRequestFirstChanceDelayMsDefaultValue;
         }
         set { setValue(OneShotUpdateOnNewMergeRequestFirstChanceDelayMsKeyName, value.ToString()); }
      }

      public int OneShotUpdateOnNewMergeRequestSecondChanceDelayMs
      {
         get
         {
            return int.TryParse(getValue(
               OneShotUpdateOnNewMergeRequestSecondChanceDelayMsKeyName,
               OneShotUpdateOnNewMergeRequestSecondChanceDelayMsDefaultValue.ToString()),
                  out int result) ? result : OneShotUpdateOnNewMergeRequestSecondChanceDelayMsDefaultValue;
         }
         set { setValue(OneShotUpdateOnNewMergeRequestSecondChanceDelayMsKeyName, value.ToString()); }
      }

      public int CacheRevisionsPeriodMs
      {
         get
         {
            return int.TryParse(getValue(
               CacheRevisionsPeriodMsKeyName,
               CacheRevisionsPeriodMsDefaultValue.ToString()),
                  out int result) ? result : CacheRevisionsPeriodMsDefaultValue;
         }
         set { setValue(CacheRevisionsPeriodMsKeyName, value.ToString()); }
      }

      public bool UseGitBasedSizeCollection
      {
         get
         {
            return bool.TryParse(getValue(
               UseGitBasedSizeCollectionKeyName, boolToString(UseGitBasedSizeCollectionDefaultValue)),
                  out bool result) ? result : UseGitBasedSizeCollectionDefaultValue;
         }
         set { setValue(UseGitBasedSizeCollectionKeyName, boolToString(value)); }
      }

      public bool DisableSSLVerification
      {
         get
         {
            return bool.TryParse(getValue(
               DisableSSLVerificationKeyName, boolToString(DisableSSLVerificationDefaultValue)),
                  out bool result) ? result : DisableSSLVerificationDefaultValue;
         }
         set { setValue(DisableSSLVerificationKeyName, boolToString(value)); }
      }

      public string WorkflowType
      {
         get { return getValue(WorkflowTypeKeyName, WorkflowTypeDefaultValue); }
         set { setValue(WorkflowTypeKeyName, value); }
      }

      public Dictionary<string, string> SelectedUsers
      {
         get
         {
            return DictionaryStringHelper.DeserializeRawDictionaryString(
               getValue(SelectedUsersKeyName, SelectedUsersDefaultValue), true);
         }
         set
         {
            setValue(SelectedUsersKeyName, DictionaryStringHelper.SerializeRawDictionaryString(value));
         }
      }

      public bool HasSelectedProjects()
      {
         return _config.AppSettings.Settings[SelectedProjectsKeyName] != null;
      }

      public Dictionary<string, string> SelectedProjects
      {
         get
         {
            return DictionaryStringHelper.DeserializeRawDictionaryString(
               getValue(SelectedProjectsKeyName, SelectedProjectsDefaultValue), true);
         }
         set
         {
            setValue(SelectedProjectsKeyName, DictionaryStringHelper.SerializeRawDictionaryString(value));
         }
      }

      public bool SelectedProjectsUpgraded
      {
         get
         {
            return bool.TryParse(getValue(
               SelectedProjectsUpgradedKeyName, boolToString(SelectedProjectsUpgradedDefaultValue)),
                  out bool result) ? result : SelectedProjectsUpgradedDefaultValue;
         }
         set { setValue(SelectedProjectsUpgradedKeyName, boolToString(value)); }
      }

      public string GetAccessToken(string host)
      {
         for (int iKnownHost = 0; iKnownHost < KnownHosts.Count(); ++iKnownHost)
         {
            if (host == KnownHosts[iKnownHost])
            {
               return KnownAccessTokens.Length > iKnownHost ? KnownAccessTokens[iKnownHost] : String.Empty;
            }
         }
         return String.Empty;
      }

      private string getValue(string key, string defaultValue)
      {
         if (_config.AppSettings.Settings[key] != null)
         {
            return _config.AppSettings.Settings[key].Value;
         }

         setValue(key, defaultValue);
         return defaultValue;
      }

      private void setValue(string key, string value)
      {
         if (_config.AppSettings.Settings[key] != null)
         {
            if (_config.AppSettings.Settings[key].Value != value)
            {
               _config.AppSettings.Settings[key].Value = value;
               OnPropertyChanged(key);

               Trace.TraceInformation(String.Format("[Configuration] Changed property {0} value to {1}", key, value));
            }
            return;
         }

         _config.AppSettings.Settings.Add(key, value);
         OnPropertyChanged(key);

         Trace.TraceInformation(String.Format("[Configuration] Added a new property {0} with value {1}", key, value));
      }

      private IEnumerable<string> getValues(string key, string[] defaultValues)
      {
         if (_config.AppSettings.Settings[key] != null)
         {
            var valuesString = _config.AppSettings.Settings[key].Value;
            List<string> values = new List<string>();
            if (valuesString.Length > 0)
            {
               foreach (var value in valuesString.Split(';'))
               {
                  values.Add(value);
               }
            }
            return values;
         }

         setValues(key, defaultValues);
         return defaultValues;
      }

      private void setValues(string key, string[] values)
      {
         string valuesString = string.Join(";", values);
         setValue(key, valuesString);
      }

      private string boolToString(bool value)
      {
         return value.ToString().ToLower();
      }

      private void OnPropertyChanged(string name)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }

      private readonly Configuration _config;
   }
}

