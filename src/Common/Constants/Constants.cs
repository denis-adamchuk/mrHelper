using System;
using System.Collections.Generic;
using mrHelper.Common.Tools;

namespace mrHelper.Common.Constants
{
   public static class Constants
   {
      public static string ApplicationDataFolderName = "mrHelper";
      public static string GitDiffToolName = "mrhelperdiff";
      public static string GitDiffToolConfigKey = "difftool.mrhelperdiff.cmd";
      public static string CustomProtocolName = "mrhelper";
      public static string UWP_Launcher_Name = "mrHelper.Launcher.exe";
      public static string MainWindowCaption = "Merge Request Helper";
      public static string StartNewThreadCaption = "Start a thread";
      public static string MsiExecName = "msiexec.exe";
      public static string MsiExecSilentLaunchArguments = "/passive /norestart LAUNCH_AFTER_INSTALL=1";

      public static string NotStartedTimeTrackingText = "Not Started";
      public static string NotAllowedTimeTrackingText = "<mine>";

      public static string CreateMergeRequestCustomActionName = "Create Merge Request";
      public static string CreateMergeRequestBashScriptName = "create-new-merge-request.sh";
      public static string BashFileName = "bash.exe";

      public static string CustomActionsFileName = "CustomActions.xml";
      public static string CustomActionsWithApprovalStatusSupportFileName = "CustomActionsWithApprovalStatusSupport.xml";
      public static string KeywordsFileName = "keywords.json";

      public static string TimeStampLogFilenameFormat = "yyyy_MM_dd_HHmmss";

      public static string ConfigurationBadValueSaved  = "bad-value-saved";
      public static string ConfigurationBadValueLoaded = "bad-value-loaded";

      public static string BugReportLogArchiveName => String.Format(
         "mrhelper.logs.{0}.zip", DateTime.Now.ToString(TimeStampLogFilenameFormat));

      public static string BugReportDumpArchiveName => String.Format(
         "mrhelper.dumps.{0}.zip", DateTime.Now.ToString(TimeStampLogFilenameFormat));

      public static int FullContextSize = 20000;

      public static string ExcludeLabelPrefix = "!";
      public static string PinLabelPrefix = "^";
      public static string AuthorLabelPrefix = "#";
      public static string GitLabLabelPrefix = "@";
      public static string HighPriorityLabel = "high-priority";

      public static int CheckForUpdatesTimerInterval = 1000 * 60 * 60 * 4; // 4 hours

      public static int DiscussionCheckOnNewThreadInterval = 1000 * 3; // 3 seconds
      public static int DiscussionCheckOnNewThreadFromDiffToolInterval = 500; // 0.5 seconds

      public static string DefaultColorSchemeName = "Default";
      public static string ColorSchemeFileNamePrefix = "colors.json";

      public static string DefaultColorSchemeFileName =>
         String.Format("{0}.{1}", DefaultColorSchemeName, ColorSchemeFileNamePrefix);

      public static string[] ThemeNames = { "Default" };
      public static string DefaultThemeName = "Default";

      public static string[] ColorSchemeKnownColorNames =
         { "Brown", "Chocolate", "Salmon",
           "Red", "Tomato", "Coral", "Orange",
           "Dark Khaki", "Khaki", "Gold", "Yellow",
           "Dark Gray", "Gray", "Silver", "Whitesmoke",
           "Green", "Pale Green", "Spring Green", "Lime",
           "Dodger Blue", "Aqua", "Light Cyan", "Light Sky Blue",
           "Misty Rose", "Lavender", "Magenta", "Violet", "Orchid" };

      public static string LiveListViewName = "Live";
      public static string SearchListViewName = "Search";
      public static string RecentListViewName = "Recent";

      public static string WarningOnUnescapedMarkdown =
         "Warning: Some markdown characters may require surrounding them with apostrophes, e.g. `<vector>` or `f<int>()`";

      public static string NoDataAtGitLab = "GitLab can't compare";

      public static Dictionary<string, double> FontSizeChoices = new Dictionary<string, double>
      {
         { "Design",  8.25 }, // Design-time font size
         { "Tiny",    8.25 },
         { "Small",   9.00 },
         { "Medium",  9.75 },
         { "Large",  11.25 },
         // skip 12.00
         // skip 14.25
         { "Meeting Mode",  15.75 }
      };

      public static string[] MainWindowFontSizeChoices = new string[]
         { "Tiny", "Small", "Medium", "Large" };

      public static string DefaultMainWindowFontSizeChoice = "Small";

      public static string[] DiscussionsWindowFontSizeChoices = new string[]
         { "Tiny", "Small", "Medium", "Large", "Meeting Mode" };

      public static int MaxSearchResults = 20;
      public static int MaxCommitsToLoad = 50;
      public static int MaxAllowedDiffsInComparison = 1000;
      public static int MaxAllowedDiffsInBackgroundComparison = 100;
      public static int MinDiffsInComparisonToNotifyUser = 200;
      public static int MaxCommitDepth = 10;
      public static int RecentMergeRequestPerProjectDefaultCount = 7;
      public static int FavoriteProjectsPerHostDefaultCount = 5;
      public static int DiscussionPageSizeDefaultCount = 200;

      public static TaskUtils.BatchLimits MergeRequestLoaderSearchQueryBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 20,
         Delay = 0
      };

      public static TaskUtils.BatchLimits ProjectResolverBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 5,
         Delay = 0
      };

      public static TaskUtils.BatchLimits VersionLoaderMergeRequestBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 20,
         Delay = 0
      };

      public static TaskUtils.BatchLimits VersionLoaderCommitBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 20,
         Delay = 0
      };

      public static TaskUtils.BatchLimits DiscussionLoaderMergeRequestBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 10,
         Delay = 1000
      };

      public static TaskUtils.BatchLimits ApprovalLoaderMergeRequestBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 20,
         Delay = 0
      };

      public static TaskUtils.BatchLimits AvatarLoaderUserBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 20,
         Delay = 0
      };

      public static TaskUtils.BatchLimits AvatarLoaderForDiscussionsUserBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 10,
         Delay = 1000
      };

      public static TaskUtils.BatchLimits AvatarLoaderForUsersUserBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 5,
         Delay = 10000
      };

      // @{ Default properties for FileStorageUpdater (can be overridden by user in a configuration file)
      // AwaitedUpdateComparisonBatchSizeKeyName
      public static TaskUtils.BatchLimits AwaitedUpdateComparisonBatchDefaultLimits = new TaskUtils.BatchLimits
      {
         Size = 25,
         Delay = 100
      };

      public static TaskUtils.BatchLimits AwaitedUpdateFileBatchDefaultLimits = new TaskUtils.BatchLimits
      {
         Size = 25,
         Delay = 100
      };

      public static TaskUtils.BatchLimits NonAwaitedUpdateComparisonBatchDefaultLimits = new TaskUtils.BatchLimits
      {
         Size = 10,
         Delay = 2000 // this is multiplied by number of storages
      };

      public static TaskUtils.BatchLimits NonAwaitedUpdateFileBatchDefaultLimits = new TaskUtils.BatchLimits
      {
         Size = 5,
         Delay = 2000 // this is multiplied by number of storages
      };
      // @} FileStorageUpdater

      public static int MaxMergeRequestStorageUpdatesInParallel = 3;

      /// <summary>
      /// Missing SHA checks don't make requests to GitLab and run locally
      /// </summary>
      public static TaskUtils.BatchLimits MissingShaCheckBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 20,
         Delay = 20
      };

      /// <summary>
      /// Git data update requests don't make requests to GitLab and run locally
      /// </summary>
      public static TaskUtils.BatchLimits GitDataUpdaterBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 5,
         Delay = 500
      };
   }
}

