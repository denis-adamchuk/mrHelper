using System;
using System.ComponentModel;
using mrHelper.Common.Constants;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   public partial class UserDefinedSettings : IHostProperties
   {
      private static readonly string KnownHostsKeyName = "KnownHosts";
      private static readonly string[] KnownHostsDefaultValue = Array.Empty<string>();

      private static readonly string KnownAccessTokensKeyName = "KnownAccessTokens";

      private static readonly string LocalStorageFolderKeyName      = "LocalGitFolder";
      private static readonly string LocalStorageFolderDefaultValue = PathFinder.DefaultStorage;

      private static readonly string AutoSelectionModeKeyName      = "AutoSelectionMode";
      private static readonly string AutoSelectionModeDefaultValue = "LastVsLatest";

      private static readonly string RevisionTypeKeyName      = "RevisionType";
      private static readonly string RevisionTypeDefaultValue = "Version";

      private static readonly string GitUsageForStorageKeyName       = "GitUsageForStorage";
      private static readonly string GitUsageForStorageDefaultValue  = "DontUseGit";

      private static readonly string AllowAuthorToTrackTimeKeyName      = "AllowAuthorToTrackTime";
      private static readonly bool   AllowAuthorToTrackTimeDefaultValue = false;

      private static readonly string RemindAboutAvailableNewVersionKeyName      = "RemindAboutAvailableNewVersion";
      private static readonly bool   RemindAboutAvailableNewVersionDefaultValue = true;

      private static readonly string CheckedLabelsFilterKeyName = "CheckedLabelsFilter";
      private static readonly bool   CheckedLabelsFilterDefaultValue = false;

      private static readonly string LastUsedLabelsKeyName = "LastUsedLabels";
      private static readonly string LastUsedLabelsDefaultValue = "";

      private static readonly string CheckedLabelsFilterRecentKeyName = "CheckedLabelsFilterRecent";
      private static readonly bool   CheckedLabelsFilterRecentDefaultValue = false;

      private static readonly string LastUsedLabelsRecentKeyName = "LastUsedLabelsRecent";
      private static readonly string LastUsedLabelsRecentDefaultValue = "";

      private static readonly string ShowPublicOnlyKeyName = "ShowPublicOnly";
      private static readonly bool   ShowPublicOnlyDefaultValue = true;

      private static readonly string UpdateManagerExtendedLoggingKeyName = "UpdateManagerExtendedLogging";
      private static readonly bool   UpdateManagerExtendedLoggingDefaultValue = false;

      private static readonly string WordWrapLongRowsKeyName      = "WordWrapLongRows";
      private static readonly bool   WordWrapLongRowsDefaultValue = true;

      private static readonly string FlatRevisionPreviewKeyName      = "FlatRevisionPreview";
      private static readonly bool   FlatRevisionPreviewDefaultValue = false;

      private static readonly string DiffContextDepthKeyName = "DiffContextDepth";
      private static readonly int    DiffContextDepthDefaultValue = 2;

      private static readonly string DiffContextPositionKeyName = "DiffContextPosition";
      private static readonly string DiffContextPositionDefaultValue = "right";

      private static readonly string DiscussionColumnWidthKeyName = "DiscussionColumnWidth";
      private static readonly string DiscussionColumnWidthDefaultValue = "medium";

      private static readonly string IsDiscussionColumnWidthFixedKeyName = "IsDiscussionColumnWidthFixed";
      private static readonly bool   IsDiscussionColumnWidthFixedDefaultValue = false;

      private static readonly string NeedShiftRepliesKeyName = "NeedShiftReplies";
      private static readonly bool   NeedShiftRepliesDefaultValue = true;

      private static readonly string EmulateNativeLineBreaksInDiscussionsKeyName      = "EmulateNativeLineBreaksInDiscussions";
      private static readonly bool   EmulateNativeLineBreaksInDiscussionsDefaultValue = false;

      private static readonly string MinimizeOnCloseKeyName = "MinimizeOnClose";
      private static readonly bool   MinimizeOnCloseDefaultValue = false;

      private static readonly string RunWhenWindowsStartsKeyName        = "RunWhenWindowsStarts";
      private static readonly bool   RunWhenWindowsStartsDefaultValue   = false;

      private static readonly string DisableSpellCheckerKeyName      = "DisableSpellChecker";
      private static readonly bool   DisableSpellCheckerDefaultValue = false;

      private static readonly string WPFSoftwareOnlyRenderModeKeyName      = "WPFSoftwareOnlyRenderMode";
      private static readonly bool   WPFSoftwareOnlyRenderModeDefaultValue = false;

      private static readonly string LeftBeforeCloseKeyName      = "LeftBeforeClose";
      private static readonly int    LeftBeforeCloseDefaultValue = 0;

      private static readonly string TopBeforeCloseKeyName      = "TopBeforeClose";
      private static readonly int    TopBeforeCloseDefaultValue = 0;

      private static readonly string WidthBeforeCloseKeyName      = "WidthBeforeClose";
      private static readonly int    WidthBeforeCloseDefaultValue = 0;

      private static readonly string HeightBeforeCloseKeyName      = "HeightBeforeClose";
      private static readonly int    HeightBeforeCloseDefaultValue = 0;

      private static readonly string WasMaximizedBeforeCloseKeyName       = "WasMaximizedBeforeClose";
      private static readonly bool   WasMaximizedBeforeCloseDefaultValue  = true;

      private static readonly string WasMinimizedBeforeCloseKeyName       = "WasMinimizedBeforeClose";
      private static readonly bool   WasMinimizedBeforeCloseDefaultValue  = false;

      private static readonly string DisableSplitterRestrictionsKeyName = "DisableSplitterRestrictions";
      private static readonly bool   DisableSplitterRestrictionsDefaultValue = false;

      private static readonly string NewDiscussionIsTopMostFormKeyName        = "NewDiscussionIsTopMostForm";
      private static readonly bool   NewDiscussionIsTopMostFormDefaultValue   = false;

      private static readonly string ShowWarningsOnFileMismatchKeyName       = "ShowWarningsOnFileMismatch";
      private static readonly string ShowWarningsOnFileMismatchDefaultValue  = "until_user_ignores_file";

      private static readonly string ShowWarningOnReloadListKeyName      = "ShowWarningOnReloadList";
      private static readonly bool   ShowWarningOnReloadListDefaultValue = true;

      private static readonly string ShowWarningOnCreateMergeRequestKeyName      = "ShowWarningOnCreateMergeRequest";
      private static readonly bool   ShowWarningOnCreateMergeRequestDefaultValue = true;

      private static readonly string ShowWarningOnHideToTrayKeyName      = "ShowWarningOnHideToTray";
      private static readonly bool   ShowWarningOnHideToTrayDefaultValue = true;

      private static readonly string ShowRelatedThreadsKeyName      = "ShowRelatedThreads";
      private static readonly bool   ShowRelatedThreadsDefaultValue = true;

      private static readonly string ColorSchemeFileNameKeyName = "ColorSchemeFileName";
      private static readonly string ColorSchemeFileNameDefaultValue = String.Empty;

      private static readonly string CustomColorsKeyName = "CustomColors";
      private static readonly string CustomColorsDefaultValue = String.Empty;

      private static readonly string AutoUpdatePeriodMsKeyName      = "AutoUpdatePeriodMs";
      private static readonly int    AutoUpdatePeriodMsDefaultValue = 5 * 60 * 1000; // 5 minutes

      // replaced with "New_..." after introducing default configuration property values
      private static readonly string OneShotUpdateFirstChanceDelayMsKeyName        = "New_OneShotUpdateFirstChanceDelayMs";
      private static readonly int    OneShotUpdateFirstChanceDelayMsDefaultValue   = 5 * 1000; // 5 seconds

      // replaced with "New_..." after introducing default configuration property values
      private static readonly string OneShotUpdateSecondChanceDelayMsKeyName        = "New_OneShotUpdateSecondChanceDelayMs";
      private static readonly int    OneShotUpdateSecondChanceDelayMsDefaultValue   = 20 * 1000; // 20 seconds

      private static readonly string OneShotUpdateOnNewMergeRequestFirstChanceDelayMsKeyName        =
         "OneShotUpdateOnNewMergeRequestFirstChanceDelayMs";
      private static readonly int    OneShotUpdateOnNewMergeRequestFirstChanceDelayMsDefaultValue   =
         30 * 1000; // 30 seconds

      private static readonly string OneShotUpdateOnNewMergeRequestSecondChanceDelayMsKeyName        =
         "OneShotUpdateOnNewMergeRequestSecondChanceDelayMs";
      private static readonly int    OneShotUpdateOnNewMergeRequestSecondChanceDelayMsDefaultValue   =
         60 * 1000; // 60 seconds

      private static readonly string NewOrClosedMergeRequestRefreshListDelayMsKeyName        = "NewOrClosedMergeRequestRefreshListDelayMs";
      private static readonly int    NewOrClosedMergeRequestRefreshListDelayMsDefaultValue   = 3 * 1000; // 3 seconds

      private static readonly string CacheRevisionsPeriodMsKeyName        = "CacheRevisionsPeriodMs";
      private static readonly int    CacheRevisionsPeriodMsDefaultValue   = 8 * 60 * 1000; // 8 minutes

      private static readonly string UseGitBasedSizeCollectionKeyName      = "UseGitBasedSizeCollection";
      private static readonly bool   UseGitBasedSizeCollectionDefaultValue = false;

      private static readonly string DisableSSLVerificationKeyName      = "DisableSSLVerification";
      private static readonly bool   DisableSSLVerificationDefaultValue = true;

      private static readonly string ServicePointConnectionLimitKeyName = "ServicePointConnectionLimit";
      private static readonly int    ServicePointConnectionLimitDefaultValue = 25;

      private static readonly string AsyncOperationTimeOutSecondsKeyName = "AsyncOperationTimeOutSeconds";
      private static readonly int    AsyncOperationTimeOutSecondsDefaultValue = 90;

      private static readonly string LogFilesToKeepKeyName = "LogFilesToKeep";
      private static readonly int    LogFilesToKeepDefaultValue = 10;

      private static readonly string RevisionsToKeepKeyName = "FileStorageRevisionsToKeep";
      private static readonly int    RevisionsToKeepDefaultValue = 100;

      private static readonly string ComparisonsToKeepKeyName = "FileStorageComparisonsToKeep";
      private static readonly int    ComparisonsToKeepDefaultValue = 200;

      private static readonly string RecentMergeRequestsPerProjectCountKeyName      =
         "RecentMergeRequestsPerProjectCount";
      private static readonly string RecentMergeRequestsPerProjectCountDefaultValue =
         Constants.RecentMergeRequestPerProjectDefaultCount.ToString();

      private static readonly string FavoriteProjectsPerHostCountKeyName      =
         "FavoriteProjectsPerHostCount";
      private static readonly string FavoriteProjectsPerHostCountDefaultValue =
         Constants.FavoriteProjectsPerHostDefaultCount.ToString();

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

      private static readonly string ListViewRecentMergeRequestsColumnWidthsKeyName           = "LVRMR_ColWidths";
      private static readonly string ListViewRecentMergeRequestsColumnWidthsDefaultValue      = String.Empty;
      private static readonly int    ListViewRecentMergeRequestsSingleColumnWidthDefaultValue = 100;

      private static readonly string ListViewRecentMergeRequestsDisplayIndicesKeyName      = "LVRMR_DisplayIndices";
      private static readonly string ListViewRecentMergeRequestsDisplayIndicesDefaultValue = String.Empty;

      private static readonly string RevisionBrowserColumnWidthsKeyName           = "RB_ColWidths";
      private static readonly string RevisionBrowserColumnWidthsDefaultValue      = String.Empty;
      private static readonly int    RevisionBrowserSingleColumnWidthDefaultValue = 100;

      private static readonly string RevisionPreviewBrowserColumnWidthsKeyName           = "RPB_ColWidths";
      private static readonly string RevisionPreviewBrowserColumnWidthsDefaultValue      = String.Empty;
      private static readonly int    RevisionPreviewBrowserSingleColumnWidthDefaultValue = 100;

      private static readonly string RevisionComparisonColumnWidthsKeyName           = "RC_ColWidths";
      private static readonly string RevisionComparisonColumnWidthsDefaultValue      = String.Empty;
      private static readonly int    RevisionComparisonSingleColumnWidthDefaultValue = 100;

      private static readonly string PrimarySplitContainerDistanceKeyName      = "MWSplitterDistance";
      private static readonly int    PrimarySplitContainerDistanceDefaultValue = 0;

      private static readonly string SecondarySplitContainerDistanceKeyName      = "RPSplitterDistance";
      private static readonly int    SecondarySplitContainerDistanceDefaultValue = 0;

      private static readonly string DescriptionSplitContainerDistanceKeyName      = "DescriptionSplitterDistance";
      private static readonly int    DescriptionSplitContainerDistanceDefaultValue = 0;

      private static readonly string RevisionSplitContainerDistanceKeyName      = "RevisionSplitterDistance";
      private static readonly int    RevisionSplitContainerDistanceDefaultValue = 0;

      private static readonly string VisualThemeNameKeyName       = "VisualThemeName";
      private static readonly string VisualThemeNameDefaultValue  = Constants.DefaultThemeName;

      private static readonly string WorkflowTypeKeyName      = "WorkflowType";
      private static readonly string WorkflowTypeDefaultValue = "Users";

      private static readonly string SelectedUsersKeyName      = "SelectedUsers";
      private static readonly string SelectedUsersDefaultValue = String.Empty;

      private static readonly string SelectedProjectsKeyName      = "SelectedProjects";
      private static readonly string SelectedProjectsDefaultValue = String.Empty;

      private static readonly string SelectedProjectsUpgradedKeyName      = "SelectedProjectsUpgraded";
      private static readonly bool   SelectedProjectsUpgradedDefaultValue = false;

      private static readonly string MainWindowFontSizeNameKeyName       = "MWFontSize";
      private static readonly string MainWindowFontSizeNameDefaultValue  = Constants.DefaultMainWindowFontSizeChoice;

      private static readonly string MainWindowLayoutKeyName      = "MWLayout";
      private static readonly string MainWindowLayoutDefaultValue = "Horizontal";

      private static readonly string ToolBarPositionKeyName      = "TBLayout";
      private static readonly string ToolBarPositionDefaultValue = "Top";

      private static readonly string AccessTokensProtectedKeyName      = "AccessTokensProtected";
      private static readonly bool   AccessTokensProtectedDefaultValue = false;

      private static readonly string AwaitedUpdateComparisonBatchSizeKeyName      =
         "AwaitedUpdateComparisonBatchSize";
      private static readonly int    AwaitedUpdateComparisonBatchSizeDefaultValue = 
         Constants.AwaitedUpdateComparisonBatchDefaultLimits.Size;

      private static readonly string AwaitedUpdateComparisonBatchDelayKeyName      =
         "AwaitedUpdateComparisonBatchDelay";
      private static readonly int    AwaitedUpdateComparisonBatchDelayDefaultValue = 
         Constants.AwaitedUpdateComparisonBatchDefaultLimits.Delay;

      private static readonly string AwaitedUpdateFileBatchSizeKeyName      =
         "AwaitedUpdateFileBatchSize";
      private static readonly int    AwaitedUpdateFileBatchSizeDefaultValue = 
         Constants.AwaitedUpdateFileBatchDefaultLimits.Size;

      private static readonly string AwaitedUpdateFileBatchDelayKeyName      =
         "AwaitedUpdateFileBatchDelay";
      private static readonly int    AwaitedUpdateFileBatchDelayDefaultValue = 
         Constants.AwaitedUpdateFileBatchDefaultLimits.Delay;

      private static readonly string NonAwaitedUpdateComparisonBatchSizeKeyName      =
         "NonAwaitedUpdateComparisonBatchSize";
      private static readonly int    NonAwaitedUpdateComparisonBatchSizeDefaultValue = 
         Constants.NonAwaitedUpdateComparisonBatchDefaultLimits.Size;

      private static readonly string NonAwaitedUpdateComparisonBatchDelayKeyName      =
         "NonAwaitedUpdateComparisonBatchDelay";
      private static readonly int    NonAwaitedUpdateComparisonBatchDelayDefaultValue = 
         Constants.NonAwaitedUpdateComparisonBatchDefaultLimits.Delay;

      private static readonly string NonAwaitedUpdateFileBatchSizeKeyName      =
         "NonAwaitedUpdateFileBatchSize";
      private static readonly int    NonAwaitedUpdateFileBatchSizeDefaultValue = 
         Constants.NonAwaitedUpdateFileBatchDefaultLimits.Size;

      private static readonly string NonAwaitedUpdateFileBatchDelayKeyName      =
         "NonAwaitedUpdateFileBatchDelay";
      private static readonly int    NonAwaitedUpdateFileBatchDelayDefaultValue = 
         Constants.NonAwaitedUpdateFileBatchDefaultLimits.Delay;
   }
}

