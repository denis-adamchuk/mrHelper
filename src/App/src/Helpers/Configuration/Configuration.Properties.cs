using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Helpers
{
   public partial class UserDefinedSettings : INotifyPropertyChanged, IHostProperties
   {
      public string[] KnownHosts
      {
         get => getValues(KnownHostsKeyName, KnownHostsDefaultValue, setValues).ToArray();
         set => setValues(KnownHostsKeyName, value);
      }

      public string[] KnownAccessTokens
      {
         get => getAccessTokens().ToArray();
         set => setAccessTokens(value);
      }

      public string LocalGitFolder
      {
         get => getValue(LocalGitFolderKeyName, LocalGitFolderDefaultValue);
         set => setValue(LocalGitFolderKeyName, value);
      }

      public string AutoSelectionMode
      {
         get => getValue(AutoSelectionModeKeyName, AutoSelectionModeDefaultValue);
         set => setValue(AutoSelectionModeKeyName, value);
      }

      public string RevisionType
      {
         get => getValue(RevisionTypeKeyName, RevisionTypeDefaultValue);
         set => setValue(RevisionTypeKeyName, value);
      }

      public string GitUsageForStorage
      {
         get => getValue(GitUsageForStorageKeyName, GitUsageForStorageDefaultValue);
         set => setValue(GitUsageForStorageKeyName, value);
      }

      public bool AllowAuthorToTrackTime
      {
         get => getBoolValue(AllowAuthorToTrackTimeKeyName, AllowAuthorToTrackTimeDefaultValue);
         set => setBoolValue(AllowAuthorToTrackTimeKeyName, value);
      }

      public bool RemindAboutAvailableNewVersion
      {
         get => getBoolValue(RemindAboutAvailableNewVersionKeyName, RemindAboutAvailableNewVersionDefaultValue);
         set => setBoolValue(RemindAboutAvailableNewVersionKeyName, value);
      }

      public bool DisplayFilterEnabled
      {
         get => getBoolValue(CheckedLabelsFilterKeyName, CheckedLabelsFilterDefaultValue);
         set => setBoolValue(CheckedLabelsFilterKeyName, value);
      }

      public string DisplayFilter
      {
         get => getValue(LastUsedLabelsKeyName, LastUsedLabelsDefaultValue);
         set => setValue(LastUsedLabelsKeyName, value);
      }

      public bool ShowPublicOnly
      {
         get => getBoolValue(ShowPublicOnlyKeyName, ShowPublicOnlyDefaultValue);
         set => setBoolValue(ShowPublicOnlyKeyName, value);
      }

      public bool UpdateManagerExtendedLogging
      {
         get => getBoolValue(UpdateManagerExtendedLoggingKeyName, UpdateManagerExtendedLoggingDefaultValue);
         set => setBoolValue(UpdateManagerExtendedLoggingKeyName, value);
      }

      public bool MinimizeOnClose
      {
         get => getBoolValue(MinimizeOnCloseKeyName, MinimizeOnCloseDefaultValue);
         set => setBoolValue(MinimizeOnCloseKeyName, value);
      }

      public bool RunWhenWindowsStarts
      {
         get => getBoolValue(RunWhenWindowsStartsKeyName, RunWhenWindowsStartsDefaultValue);
         set => setBoolValue(RunWhenWindowsStartsKeyName, value);
      }

      public bool DisableSpellChecker
      {
         get => getBoolValue(DisableSpellCheckerKeyName, DisableSpellCheckerDefaultValue);
         set => setBoolValue(DisableSpellCheckerKeyName, value);
      }

      public bool WasMaximizedBeforeClose
      {
         get => getBoolValue(WasMaximizedBeforeCloseKeyName, WasMaximizedBeforeCloseDefaultValue);
         set => setBoolValue(WasMaximizedBeforeCloseKeyName, value);
      }

      public bool DisableSplitterRestrictions
      {
         get => getBoolValue(DisableSplitterRestrictionsKeyName, DisableSplitterRestrictionsDefaultValue);
         set => setBoolValue(DisableSplitterRestrictionsKeyName, value);
      }

      public bool NewDiscussionIsTopMostForm
      {
         get => getBoolValue(NewDiscussionIsTopMostFormKeyName, NewDiscussionIsTopMostFormDefaultValue);
         set => setBoolValue(NewDiscussionIsTopMostFormKeyName, value);
      }

      public string ShowWarningsOnFileMismatchMode
      {
         get => getValue(ShowWarningsOnFileMismatchKeyName, ShowWarningsOnFileMismatchDefaultValue);
         set => setValue(ShowWarningsOnFileMismatchKeyName, value);
      }

      public bool ShowWarningOnReloadList
      {
         get => getBoolValue(ShowWarningOnReloadListKeyName, ShowWarningOnReloadListDefaultValue);
         set => setBoolValue(ShowWarningOnReloadListKeyName, value);
      }

      public bool ShowWarningOnCreateMergeRequest
      {
         get => getBoolValue(ShowWarningOnCreateMergeRequestKeyName, ShowWarningOnCreateMergeRequestDefaultValue);
         set => setBoolValue(ShowWarningOnCreateMergeRequestKeyName, value);
      }

      public bool ShowWarningOnFilterMigration
      {
         get => getBoolValue(ShowWarningOnFilterMigrationKeyName, ShowWarningOnFilterMigrationDefaultValue);
         set => setBoolValue(ShowWarningOnFilterMigrationKeyName, value);
      }

      public bool ShowWarningOnHideToTray
      {
         get => getBoolValue(ShowWarningOnHideToTrayKeyName, ShowWarningOnHideToTrayDefaultValue);
         set => setBoolValue(ShowWarningOnHideToTrayKeyName, value);
      }

      public bool ShowRelatedThreads
      {
         get => getBoolValue(ShowRelatedThreadsKeyName, ShowRelatedThreadsDefaultValue);
         set => setBoolValue(ShowRelatedThreadsKeyName, value);
      }

      public string DiffContextDepth
      {
         get => getValue(DiffContextDepthKeyName, DiffContextDepthDefaultValue);
         set => setValue(DiffContextDepthKeyName, value);
      }

      public string DiffContextPosition
      {
         get => getValue(DiffContextPositionKeyName, DiffContextPositionDefaultValue);
         set => setValue(DiffContextPositionKeyName, value);
      }

      public string DiscussionColumnWidth
      {
         get => getValue(DiscussionColumnWidthKeyName, DiscussionColumnWidthDefaultValue);
         set => setValue(DiscussionColumnWidthKeyName, value);
      }

      public bool NeedShiftReplies
      {
         get => getBoolValue(NeedShiftRepliesKeyName, NeedShiftRepliesDefaultValue);
         set => setBoolValue(NeedShiftRepliesKeyName, value);
      }

      public bool IsDiscussionColumnWidthFixed
      {
         get => getBoolValue(IsDiscussionColumnWidthFixedKeyName, IsDiscussionColumnWidthFixedDefaultValue);
         set => setBoolValue(IsDiscussionColumnWidthFixedKeyName, value);
      }

      public string ColorSchemeFileName
      {
         get => getValue(ColorSchemeFileNameKeyName, ColorSchemeFileNameDefaultValue);
         set => setValue(ColorSchemeFileNameKeyName, value);
      }

      public string VisualThemeName
      {
         get => getValue(VisualThemeNameKeyName, VisualThemeNameDefaultValue);
         set => setValue(VisualThemeNameKeyName, value);
      }

      public string MainWindowFontSizeName
      {
         get => getValue(MainWindowFontSizeNameKeyName, MainWindowFontSizeNameDefaultValue);
         set => setValue(MainWindowFontSizeNameKeyName, value);
      }

      public int ServicePointConnectionLimit
      {
         get => getIntValue(ServicePointConnectionLimitKeyName, ServicePointConnectionLimitDefaultValue);
         set => setIntValue(ServicePointConnectionLimitKeyName, value);
      }

      public int LogFilesToKeep
      {
         get => getIntValue(LogFilesToKeepKeyName, LogFilesToKeepDefaultValue);
         set => setIntValue(LogFilesToKeepKeyName, value);
      }

      public int RevisionsToKeep
      {
         get => getIntValue(RevisionsToKeepKeyName, RevisionsToKeepDefaultValue);
         set => setIntValue(RevisionsToKeepKeyName, value);
      }

      public int ComparisonsToKeep
      {
         get => getIntValue(ComparisonsToKeepKeyName, ComparisonsToKeepDefaultValue);
         set => setIntValue(ComparisonsToKeepKeyName, value);
      }

      public string RecentMergeRequestsPerProjectCount
      {
         get => getValue(RecentMergeRequestsPerProjectCountKeyName, RecentMergeRequestsPerProjectCountDefaultValue);
         set => setValue(RecentMergeRequestsPerProjectCountKeyName, value);
      }

      public bool Notifications_NewMergeRequests
      {
         get => getBoolValue(Notifications_NewMergeRequests_KeyName, Notifications_NewMergeRequests_DefaultValue);
         set => setBoolValue(Notifications_NewMergeRequests_KeyName, value);
      }

      public bool Notifications_MergedMergeRequests
      {
         get => getBoolValue(Notifications_MergedMergeRequests_KeyName, Notifications_MergedMergeRequests_DefaultValue);
         set => setBoolValue(Notifications_MergedMergeRequests_KeyName, value);
      }

      public bool Notifications_UpdatedMergeRequests
      {
         get => getBoolValue(Notifications_UpdatedMergeRequests_KeyName, Notifications_UpdatedMergeRequests_DefaultValue);
         set => setBoolValue(Notifications_UpdatedMergeRequests_KeyName, value);
      }

      public bool Notifications_AllThreadsResolved
      {
         get => getBoolValue(Notifications_AllThreadsResolved_KeyName, Notifications_AllThreadsResolved_DefaultValue);
         set => setBoolValue(Notifications_AllThreadsResolved_KeyName, value);
      }

      public bool Notifications_OnMention
      {
         get => getBoolValue(Notifications_OnMention_KeyName, Notifications_OnMention_DefaultValue);
         set => setBoolValue(Notifications_OnMention_KeyName, value);
      }

      public bool Notifications_Keywords
      {
         get => getBoolValue(Notifications_Keywords_KeyName, Notifications_Keywords_DefaultValue);
         set => setBoolValue(Notifications_Keywords_KeyName, value);
      }

      public bool Notifications_MyActivity
      {
         get => getBoolValue(Notifications_MyActivity_KeyName, Notifications_MyActivity_DefaultValue);
         set => setBoolValue(Notifications_MyActivity_KeyName, value);
      }

      public bool Notifications_Service
      {
         get => getBoolValue(Notifications_Service_KeyName, Notifications_Service_DefaultValue);
         set => setBoolValue(Notifications_Service_KeyName, value);
      }

      public Dictionary<string, int> ListViewMergeRequestsColumnWidths
      {
         get => getStringToIntDictionary(ListViewMergeRequestsColumnWidthsKeyName,
                                         ListViewMergeRequestsColumnWidthsDefaultValue,
                                         ListViewMergeRequestsSingleColumnWidthDefaultValue,
                                         -1);
         set => setStringToIntDictionary(ListViewMergeRequestsColumnWidthsKeyName, value);
      }

      public Dictionary<string, int> ListViewMergeRequestsDisplayIndices
      {
         get => getStringToIntDictionary(ListViewMergeRequestsDisplayIndicesKeyName,
                                         ListViewMergeRequestsDisplayIndicesDefaultValue,
                                         -1,
                                         -1);
         set => setStringToIntDictionary(ListViewMergeRequestsDisplayIndicesKeyName, value);
      }

      public Dictionary<string, int> ListViewFoundMergeRequestsColumnWidths
      {
         get => getStringToIntDictionary(ListViewFoundMergeRequestsColumnWidthsKeyName,
                                         ListViewFoundMergeRequestsColumnWidthsDefaultValue,
                                         ListViewFoundMergeRequestsSingleColumnWidthDefaultValue,
                                         -1);
         set => setStringToIntDictionary(ListViewFoundMergeRequestsColumnWidthsKeyName, value);
      }

      public Dictionary<string, int> ListViewFoundMergeRequestsDisplayIndices
      {
         get => getStringToIntDictionary(ListViewFoundMergeRequestsDisplayIndicesKeyName,
                                         ListViewFoundMergeRequestsDisplayIndicesDefaultValue,
                                         -1,
                                         -1);
         set => setStringToIntDictionary(ListViewFoundMergeRequestsDisplayIndicesKeyName, value);
      }

      public Dictionary<string, int> ListViewRecentMergeRequestsColumnWidths
      {
         get => getStringToIntDictionary(ListViewRecentMergeRequestsColumnWidthsKeyName,
                                         ListViewRecentMergeRequestsColumnWidthsDefaultValue,
                                         ListViewRecentMergeRequestsSingleColumnWidthDefaultValue,
                                         -1);
         set => setStringToIntDictionary(ListViewRecentMergeRequestsColumnWidthsKeyName, value);
      }

      public Dictionary<string, int> ListViewRecentMergeRequestsDisplayIndices
      {
         get => getStringToIntDictionary(ListViewRecentMergeRequestsDisplayIndicesKeyName,
                                         ListViewRecentMergeRequestsDisplayIndicesDefaultValue,
                                         -1,
                                         -1);
         set => setStringToIntDictionary(ListViewRecentMergeRequestsDisplayIndicesKeyName, value);
      }

      public Dictionary<string, int> RevisionBrowserColumnWidths
      {
         get => getStringToIntDictionary(RevisionBrowserColumnWidthsKeyName,
                                         RevisionBrowserColumnWidthsDefaultValue,
                                         RevisionBrowserSingleColumnWidthDefaultValue,
                                         -1);
         set => setStringToIntDictionary(RevisionBrowserColumnWidthsKeyName, value);
      }

      public int MainWindowSplitterDistance
      {
         get => getIntValue(MainWindowSplitterDistanceKeyName, MainWindowSplitterDistanceDefaultValue);
         set => setIntValue(MainWindowSplitterDistanceKeyName, value);
      }

      public int RightPaneSplitterDistance
      {
         get => getIntValue(RightPaneSplitterDistanceKeyName, RightPaneSplitterDistanceDefaultValue);
         set => setIntValue(RightPaneSplitterDistanceKeyName, value);
      }

      public int AutoUpdatePeriodMs
      {
         get => getIntValue(AutoUpdatePeriodMsKeyName, AutoUpdatePeriodMsDefaultValue);
         set => setIntValue(AutoUpdatePeriodMsKeyName, value);
      }

      public int OneShotUpdateFirstChanceDelayMs
      {
         get => getIntValue(OneShotUpdateFirstChanceDelayMsKeyName, OneShotUpdateFirstChanceDelayMsDefaultValue);
         set => setIntValue(OneShotUpdateFirstChanceDelayMsKeyName, value);
      }

      public int OneShotUpdateSecondChanceDelayMs
      {
         get => getIntValue(OneShotUpdateSecondChanceDelayMsKeyName, OneShotUpdateSecondChanceDelayMsDefaultValue);
         set => setIntValue(OneShotUpdateSecondChanceDelayMsKeyName, value);
      }

      public int OneShotUpdateOnNewMergeRequestFirstChanceDelayMs
      {
         get => getIntValue(OneShotUpdateOnNewMergeRequestFirstChanceDelayMsKeyName, OneShotUpdateOnNewMergeRequestFirstChanceDelayMsDefaultValue);
         set => setIntValue(OneShotUpdateOnNewMergeRequestFirstChanceDelayMsKeyName, value);
      }

      public int OneShotUpdateOnNewMergeRequestSecondChanceDelayMs
      {
         get => getIntValue(OneShotUpdateOnNewMergeRequestSecondChanceDelayMsKeyName, OneShotUpdateOnNewMergeRequestSecondChanceDelayMsDefaultValue);
         set => setIntValue(OneShotUpdateOnNewMergeRequestSecondChanceDelayMsKeyName, value);
      }

      public int CacheRevisionsPeriodMs
      {
         get => getIntValue(CacheRevisionsPeriodMsKeyName, CacheRevisionsPeriodMsDefaultValue);
         set => setIntValue(CacheRevisionsPeriodMsKeyName, value);
      }

      public bool UseGitBasedSizeCollection
      {
         get => getBoolValue(UseGitBasedSizeCollectionKeyName, UseGitBasedSizeCollectionDefaultValue);
         set => setBoolValue(UseGitBasedSizeCollectionKeyName, value);
      }

      public bool DisableSSLVerification
      {
         get => getBoolValue(DisableSSLVerificationKeyName, DisableSSLVerificationDefaultValue);
         set => setBoolValue(DisableSSLVerificationKeyName, value);
      }

      public string WorkflowType
      {
         get => getValue(WorkflowTypeKeyName, WorkflowTypeDefaultValue);
         set => setValue(WorkflowTypeKeyName, value);
      }

      public Dictionary<string, string> SelectedUsers
      {
         get => getStringToStringDictionary(SelectedUsersKeyName, SelectedUsersDefaultValue);
         set => setStringToStringDictionary(SelectedUsersKeyName, value);
      }

      public Dictionary<string, string> SelectedProjects
      {
         get => getStringToStringDictionary(SelectedProjectsKeyName, SelectedProjectsDefaultValue);
         set => setStringToStringDictionary(SelectedProjectsKeyName, value);
      }

      public bool SelectedProjectsUpgraded
      {
         get => getBoolValue(SelectedProjectsUpgradedKeyName, SelectedProjectsUpgradedDefaultValue);
         set => setBoolValue(SelectedProjectsUpgradedKeyName, value);
      }

      private bool AccessTokensProtected
      {
         get => getBoolValue(AccessTokensProtectedKeyName, AccessTokensProtectedDefaultValue);
         set => setBoolValue(AccessTokensProtectedKeyName, value);
      }
   }
}

