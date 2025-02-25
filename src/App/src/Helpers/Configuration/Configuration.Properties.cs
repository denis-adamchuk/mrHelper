﻿using System;
using System.Linq;
using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.App.Helpers
{
   public partial class UserDefinedSettings : IHostProperties
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

      public string LocalStorageFolder
      {
         get => getValue(LocalStorageFolderKeyName, LocalStorageFolderDefaultValue);
         set => setValue(LocalStorageFolderKeyName, value);
      }

      public string DiffToolName
      {
         get => getValue(DiffToolNameKeyName, DiffToolNameDefaultValue);
         set => setValue(DiffToolNameKeyName, value);
      }

      public string DiffToolPath
      {
         get => getValue(DiffToolPathKeyName, DiffToolPathDefaultValue);
         set => setValue(DiffToolPathKeyName, value);
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

      public bool UpdateManagerExtendedLogging
      {
         get => getBoolValue(UpdateManagerExtendedLoggingKeyName, UpdateManagerExtendedLoggingDefaultValue);
         set => setBoolValue(UpdateManagerExtendedLoggingKeyName, value);
      }

      public bool WordWrapLongRows
      {
         get => getBoolValue(WordWrapLongRowsKeyName, WordWrapLongRowsDefaultValue);
         set => setBoolValue(WordWrapLongRowsKeyName, value);
      }
      public event Action WordWrapLongRowsChanged;

      public int MaxListViewRows
      {
         get => getIntValue(MaxListViewRowsKeyName, MaxListViewRowsDefaultValue);
         set => setIntValue(MaxListViewRowsKeyName, value);
      }

      public int MinListViewRows
      {
         get => getIntValue(MinListViewRowsKeyName, MinListViewRowsDefaultValue);
         set => setIntValue(MinListViewRowsKeyName, value);
      }

      public bool FlatRevisionPreview
      {
         get => getBoolValue(FlatRevisionPreviewKeyName, FlatRevisionPreviewDefaultValue);
         set => setBoolValue(FlatRevisionPreviewKeyName, value);
      }
      public event Action FlatRevisionPreviewChanged;

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

      public bool WPFSoftwareOnlyRenderMode
      {
         get => getBoolValue(WPFSoftwareOnlyRenderModeKeyName, WPFSoftwareOnlyRenderModeDefaultValue);
         set => setBoolValue(WPFSoftwareOnlyRenderModeKeyName, value);
      }

      public int WidthBeforeClose
      {
         get => getIntValue(WidthBeforeCloseKeyName, WidthBeforeCloseDefaultValue);
         set => setIntValue(WidthBeforeCloseKeyName, value);
      }

      public int HeightBeforeClose
      {
         get => getIntValue(HeightBeforeCloseKeyName, HeightBeforeCloseDefaultValue);
         set => setIntValue(HeightBeforeCloseKeyName, value);
      }

      public int LeftBeforeClose
      {
         get => getIntValue(LeftBeforeCloseKeyName, LeftBeforeCloseDefaultValue);
         set => setIntValue(LeftBeforeCloseKeyName, value);
      }

      public int TopBeforeClose
      {
         get => getIntValue(TopBeforeCloseKeyName, TopBeforeCloseDefaultValue);
         set => setIntValue(TopBeforeCloseKeyName, value);
      }

      public bool WasMaximizedBeforeClose
      {
         get => getBoolValue(WasMaximizedBeforeCloseKeyName, WasMaximizedBeforeCloseDefaultValue);
         set => setBoolValue(WasMaximizedBeforeCloseKeyName, value);
      }

      public bool WasMinimizedBeforeClose
      {
         get => getBoolValue(WasMinimizedBeforeCloseKeyName, WasMinimizedBeforeCloseDefaultValue);
         set => setBoolValue(WasMinimizedBeforeCloseKeyName, value);
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

      public bool ShowHiddenMergeRequestIds
      {
         get => getBoolValue(ShowHiddenMergeRequestIdsKeyName, ShowHiddenMergeRequestIdsDefaultValue);
         set => setBoolValue(ShowHiddenMergeRequestIdsKeyName, value);
      }
      public event Action ShowHiddenMergeRequestIdsChanged;

      public bool ShowWarningOnCreateMergeRequest
      {
         get => getBoolValue(ShowWarningOnCreateMergeRequestKeyName, ShowWarningOnCreateMergeRequestDefaultValue);
         set => setBoolValue(ShowWarningOnCreateMergeRequestKeyName, value);
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

      public int DiffContextDepth
      {
         get => getIntValue(DiffContextDepthKeyName, DiffContextDepthDefaultValue);
         set => setIntValue(DiffContextDepthKeyName, value);
      }

      public string DiffContextPosition
      {
         get => getValue(DiffContextPositionKeyName, DiffContextPositionDefaultValue);
         set => setValue(DiffContextPositionKeyName, value);
      }
      public event Action DiffContextPositionChanged;

      public string DiscussionColumnWidth
      {
         get => getValue(DiscussionColumnWidthKeyName, DiscussionColumnWidthDefaultValue);
         set => setValue(DiscussionColumnWidthKeyName, value);
      }
      public event Action DiscussionColumnWidthChanged;

      public int DiscussionPageSize
      {
         get => getIntValue(DiscussionPageSizeKeyName, DiscussionPageSizeDefaultValue);
         set => setIntValue(DiscussionPageSizeKeyName, value);
      }

      public bool NeedShiftReplies
      {
         get => getBoolValue(NeedShiftRepliesKeyName, NeedShiftRepliesDefaultValue);
         set => setBoolValue(NeedShiftRepliesKeyName, value);
      }
      public event Action NeedShiftRepliesChanged;

      public bool EmulateNativeLineBreaksInDiscussions
      {
         get => getBoolValue(EmulateNativeLineBreaksInDiscussionsKeyName, EmulateNativeLineBreaksInDiscussionsDefaultValue);
         set => setBoolValue(EmulateNativeLineBreaksInDiscussionsKeyName, value);
      }

      public bool IsDiscussionColumnWidthFixed
      {
         get => getBoolValue(IsDiscussionColumnWidthFixedKeyName, IsDiscussionColumnWidthFixedDefaultValue);
         set => setBoolValue(IsDiscussionColumnWidthFixedKeyName, value);
      }

      public string ColorMode
      {
         get => getValue(ColorModeKeyName, ColorModeDefaultValue);
         set => setValue(ColorModeKeyName, value);
      }
      public event Action ColorModeChanged;

      public string MainWindowFontSizeName
      {
         get => getValue(MainWindowFontSizeNameKeyName, MainWindowFontSizeNameDefaultValue);
         set => setValue(MainWindowFontSizeNameKeyName, value);
      }

      public string MainWindowLayout
      {
         get => getValue(MainWindowLayoutKeyName, MainWindowLayoutDefaultValue);
         set => setValue(MainWindowLayoutKeyName, value);
      }
      public event Action MainWindowLayoutChanged;

      public string ToolBarPosition
      {
         get => getValue(ToolBarPositionKeyName, ToolBarPositionDefaultValue);
         set => setValue(ToolBarPositionKeyName, value);
      }
      public event Action ToolBarPositionChanged;

      public int ServicePointConnectionLimit
      {
         get => getIntValue(ServicePointConnectionLimitKeyName, ServicePointConnectionLimitDefaultValue);
         set => setIntValue(ServicePointConnectionLimitKeyName, value);
      }

      public int AsyncOperationTimeOutSeconds
      {
         get => getIntValue(AsyncOperationTimeOutSecondsKeyName, AsyncOperationTimeOutSecondsDefaultValue);
         set => setIntValue(AsyncOperationTimeOutSecondsKeyName, value);
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

      public string FavoriteProjectsPerHostCount
      {
         get => getValue(FavoriteProjectsPerHostCountKeyName, FavoriteProjectsPerHostCountDefaultValue);
         set => setValue(FavoriteProjectsPerHostCountKeyName, value);
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

      public string ListViewMergeRequestsSortingDirection
      {
         get => getValue(ListViewMergeRequestsSortingDirectionKeyName, ListViewMergeRequestsSortingDirectionDefaultValue);
         set => setValue(ListViewMergeRequestsSortingDirectionKeyName, value);
      }
      public event Action ListViewMergeRequestsSortingDirectionChanged;

      public string ListViewMergeRequestsSortedByColumnName
      {
         get => getValue(ListViewMergeRequestsSortedByColumnNameKeyName, ListViewMergeRequestsSortedByColumnNameDefaultValue);
         set => setValue(ListViewMergeRequestsSortedByColumnNameKeyName, value);
      }
      public event Action ListViewMergeRequestsSortedByColumnChanged;

      public Dictionary<string, int> ListViewMergeRequestsColumnWidths
      {
         get => getStringToIntDictionary(ListViewMergeRequestsColumnWidthsKeyName,
                                         ListViewMergeRequestsColumnWidthsDefaultValue,
                                         ListViewMergeRequestsSingleColumnWidthDefaultValue,
                                         -1);
         set => setStringToIntDictionary(ListViewMergeRequestsColumnWidthsKeyName, value);
      }
      public event Action ListViewMergeRequestsColumnWidthsChanged;

      public Dictionary<string, int> ListViewMergeRequestsDisplayIndices
      {
         get => getStringToIntDictionary(ListViewMergeRequestsDisplayIndicesKeyName,
                                         ListViewMergeRequestsDisplayIndicesDefaultValue,
                                         -1,
                                         -1);
         set => setStringToIntDictionary(ListViewMergeRequestsDisplayIndicesKeyName, value);
      }
      public event Action ListViewMergeRequestsDisplayIndicesChanged;

      public string ListViewFoundMergeRequestsSortingDirection
      {
         get => getValue(ListViewFoundMergeRequestsSortingDirectionKeyName, ListViewFoundMergeRequestsSortingDirectionDefaultValue);
         set => setValue(ListViewFoundMergeRequestsSortingDirectionKeyName, value);
      }
      public event Action ListViewFoundMergeRequestsSortingDirectionChanged;

      public string ListViewFoundMergeRequestsSortedByColumnName
      {
         get => getValue(ListViewFoundMergeRequestsSortedByColumnNameKeyName, ListViewFoundMergeRequestsSortedByColumnNameDefaultValue);
         set => setValue(ListViewFoundMergeRequestsSortedByColumnNameKeyName, value);
      }
      public event Action ListViewFoundMergeRequestsSortedByColumnChanged;

      public Dictionary<string, int> ListViewFoundMergeRequestsColumnWidths
      {
         get => getStringToIntDictionary(ListViewFoundMergeRequestsColumnWidthsKeyName,
                                         ListViewFoundMergeRequestsColumnWidthsDefaultValue,
                                         ListViewFoundMergeRequestsSingleColumnWidthDefaultValue,
                                         -1);
         set => setStringToIntDictionary(ListViewFoundMergeRequestsColumnWidthsKeyName, value);
      }
      public event Action ListViewFoundMergeRequestsColumnWidthsChanged;

      public Dictionary<string, int> ListViewFoundMergeRequestsDisplayIndices
      {
         get => getStringToIntDictionary(ListViewFoundMergeRequestsDisplayIndicesKeyName,
                                         ListViewFoundMergeRequestsDisplayIndicesDefaultValue,
                                         -1,
                                         -1);
         set => setStringToIntDictionary(ListViewFoundMergeRequestsDisplayIndicesKeyName, value);
      }
      public event Action ListViewFoundMergeRequestsDisplayIndicesChanged;

      public string ListViewRecentMergeRequestsSortingDirection
      {
         get => getValue(ListViewRecentMergeRequestsSortingDirectionKeyName, ListViewRecentMergeRequestsSortingDirectionDefaultValue);
         set => setValue(ListViewRecentMergeRequestsSortingDirectionKeyName, value);
      }
      public event Action ListViewRecentMergeRequestsSortingDirectionChanged;

      public string ListViewRecentMergeRequestsSortedByColumnName
      {
         get => getValue(ListViewRecentMergeRequestsSortedByColumnNameKeyName, ListViewRecentMergeRequestsSortedByColumnNameDefaultValue);
         set => setValue(ListViewRecentMergeRequestsSortedByColumnNameKeyName, value);
      }
      public event Action ListViewRecentMergeRequestsSortedByColumnChanged;

      public Dictionary<string, int> ListViewRecentMergeRequestsColumnWidths
      {
         get => getStringToIntDictionary(ListViewRecentMergeRequestsColumnWidthsKeyName,
                                         ListViewRecentMergeRequestsColumnWidthsDefaultValue,
                                         ListViewRecentMergeRequestsSingleColumnWidthDefaultValue,
                                         -1);
         set => setStringToIntDictionary(ListViewRecentMergeRequestsColumnWidthsKeyName, value);
      }
      public event Action ListViewRecentMergeRequestsColumnWidthsChanged;

      public Dictionary<string, int> ListViewRecentMergeRequestsDisplayIndices
      {
         get => getStringToIntDictionary(ListViewRecentMergeRequestsDisplayIndicesKeyName,
                                         ListViewRecentMergeRequestsDisplayIndicesDefaultValue,
                                         -1,
                                         -1);
         set => setStringToIntDictionary(ListViewRecentMergeRequestsDisplayIndicesKeyName, value);
      }
      public event Action ListViewRecentMergeRequestsDisplayIndicesChanged;

      public Dictionary<string, int> RevisionBrowserColumnWidths
      {
         get => getStringToIntDictionary(RevisionBrowserColumnWidthsKeyName,
                                         RevisionBrowserColumnWidthsDefaultValue,
                                         RevisionBrowserSingleColumnWidthDefaultValue,
                                         -1);
         set => setStringToIntDictionary(RevisionBrowserColumnWidthsKeyName, value);
      }

      public Dictionary<string, int> RevisionPreviewBrowserColumnWidths
      {
         get => getStringToIntDictionary(RevisionPreviewBrowserColumnWidthsKeyName,
                                         RevisionPreviewBrowserColumnWidthsDefaultValue,
                                         RevisionPreviewBrowserSingleColumnWidthDefaultValue,
                                         -1);
         set => setStringToIntDictionary(RevisionPreviewBrowserColumnWidthsKeyName, value);
      }

      public Dictionary<string, int> RevisionComparisonColumnWidths
      {
         get => getStringToIntDictionary(RevisionComparisonColumnWidthsKeyName,
                                         RevisionComparisonColumnWidthsDefaultValue,
                                         RevisionComparisonSingleColumnWidthDefaultValue,
                                         -1);
         set => setStringToIntDictionary(RevisionComparisonColumnWidthsKeyName, value);
      }

      public int PrimarySplitContainerDistance
      {
         get => getIntValue(PrimarySplitContainerDistanceKeyName, PrimarySplitContainerDistanceDefaultValue);
         set => setIntValue(PrimarySplitContainerDistanceKeyName, value);
      }

      public int SecondarySplitContainerDistance
      {
         get => getIntValue(SecondarySplitContainerDistanceKeyName, SecondarySplitContainerDistanceDefaultValue);
         set => setIntValue(SecondarySplitContainerDistanceKeyName, value);
      }

      public int DescriptionSplitContainerDistance
      {
         get => getIntValue(DescriptionSplitContainerDistanceKeyName, DescriptionSplitContainerDistanceDefaultValue);
         set => setIntValue(DescriptionSplitContainerDistanceKeyName, value);
      }

      public int RevisionSplitContainerDistance
      {
         get => getIntValue(RevisionSplitContainerDistanceKeyName, RevisionSplitContainerDistanceDefaultValue);
         set => setIntValue(RevisionSplitContainerDistanceKeyName, value);
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

      public int NewOrClosedMergeRequestRefreshListDelayMs
      {
         get => getIntValue(NewOrClosedMergeRequestRefreshListDelayMsKeyName, NewOrClosedMergeRequestRefreshListDelayMsDefaultValue);
         set => setIntValue(NewOrClosedMergeRequestRefreshListDelayMsKeyName, value);
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
         get => getStringToStringDictionary(SelectedUsersKeyName, SelectedUsersDefaultValue, true);
         set => setStringToStringDictionary(SelectedUsersKeyName, value);
      }

      public Dictionary<string, string> SelectedProjects
      {
         get => getStringToStringDictionary(SelectedProjectsKeyName, SelectedProjectsDefaultValue, true);
         set => setStringToStringDictionary(SelectedProjectsKeyName, value);
      }

      public bool SelectedProjectsUpgraded
      {
         get => getBoolValue(SelectedProjectsUpgradedKeyName, SelectedProjectsUpgradedDefaultValue);
         set => setBoolValue(SelectedProjectsUpgradedKeyName, value);
      }

      public Dictionary<string, string> ProjectsWithEnvironments
      {
         get => getStringToStringDictionary(ProjectsWithEnvironmentsKeyName, ProjectsWithEnvironmentsDefaultValue, true);
         set => setStringToStringDictionary(ProjectsWithEnvironmentsKeyName, value);
      }

      public Dictionary<string, string> CustomColorsLight
      {
         get => getStringToStringDictionary(CustomColorsLightKeyName, CustomColorsLightDefaultValue, false);
         set => setStringToStringDictionary(CustomColorsLightKeyName, value);
      }

      public Dictionary<string, string> CustomColorsDark
      {
         get => getStringToStringDictionary(CustomColorsDarkKeyName, CustomColorsDarkDefaultValue, false);
         set => setStringToStringDictionary(CustomColorsDarkKeyName, value);
      }

      public bool AutoRotateAccessTokens
      {
         get => getBoolValue(AutoRotateAccessTokensKeyName, AutoRotateAccessTokensDefaultValue);
         set => setBoolValue(AutoRotateAccessTokensKeyName, value);
      }

      private bool AccessTokensProtected
      {
         get => getBoolValue(AccessTokensProtectedKeyName, AccessTokensProtectedDefaultValue);
         set => setBoolValue(AccessTokensProtectedKeyName, value);
      }

      private int AwaitedUpdateComparisonBatchSize
      {
         get => getIntValue(AwaitedUpdateComparisonBatchSizeKeyName, AwaitedUpdateComparisonBatchSizeDefaultValue);
         set => setIntValue(AwaitedUpdateComparisonBatchSizeKeyName, value);
      }

      private int AwaitedUpdateFileBatchSize
      {
         get => getIntValue(AwaitedUpdateFileBatchSizeKeyName, AwaitedUpdateFileBatchSizeDefaultValue);
         set => setIntValue(AwaitedUpdateFileBatchSizeKeyName, value);
      }

      private int AwaitedUpdateComparisonBatchDelay
      {
         get => getIntValue(AwaitedUpdateComparisonBatchDelayKeyName, AwaitedUpdateComparisonBatchDelayDefaultValue);
         set => setIntValue(AwaitedUpdateComparisonBatchDelayKeyName, value);
      }

      private int AwaitedUpdateFileBatchDelay
      {
         get => getIntValue(AwaitedUpdateFileBatchDelayKeyName, AwaitedUpdateFileBatchDelayDefaultValue);
         set => setIntValue(AwaitedUpdateFileBatchDelayKeyName, value);
      }

      private int NonAwaitedUpdateComparisonBatchSize
      {
         get => getIntValue(NonAwaitedUpdateComparisonBatchSizeKeyName, NonAwaitedUpdateComparisonBatchSizeDefaultValue);
         set => setIntValue(NonAwaitedUpdateComparisonBatchSizeKeyName, value);
      }

      private int NonAwaitedUpdateFileBatchSize
      {
         get => getIntValue(NonAwaitedUpdateFileBatchSizeKeyName, NonAwaitedUpdateFileBatchSizeDefaultValue);
         set => setIntValue(NonAwaitedUpdateFileBatchSizeKeyName, value);
      }

      private int NonAwaitedUpdateComparisonBatchDelay
      {
         get => getIntValue(NonAwaitedUpdateComparisonBatchDelayKeyName, NonAwaitedUpdateComparisonBatchDelayDefaultValue);
         set => setIntValue(NonAwaitedUpdateComparisonBatchDelayKeyName, value);
      }

      private int NonAwaitedUpdateFileBatchDelay
      {
         get => getIntValue(NonAwaitedUpdateFileBatchDelayKeyName, NonAwaitedUpdateFileBatchDelayDefaultValue);
         set => setIntValue(NonAwaitedUpdateFileBatchDelayKeyName, value);
      }
   }
}

