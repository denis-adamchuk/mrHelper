using System;
using System.Collections.Generic;
using mrHelper.Common.Tools;

namespace mrHelper.Common.Constants
{
   public static class Constants
   {
      public static string GitDiffToolName = "mrhelperdiff";
      public static string GitDiffToolConfigKey = "difftool.mrhelperdiff.cmd";
      public static string CustomProtocolName = "mrhelper";
      public static string UWP_Launcher_Name = "mrHelper.Launcher.exe";
      public static string MainWindowCaption = "Merge Request Helper";
      public static string StartNewThreadCaption = "Start New Thread";

      public static int MaxLabelRows = 3;
      public static string MoreLabelsHint = "See more labels in tooltip";

      public static string NotStartedTimeTrackingText = "Not Started";
      public static string NotAllowedTimeTrackingText = "<mine>";

      public static string IconSchemeFileName = "icons.json";
      public static string BadgeSchemeFileName = "badges.json";
      public static string ProjectListFileName = "projects.json";

      public static string TimeStampLogFilenameFormat = "yyyy_MM_dd_HHmmss";

      public static string BugReportLogArchiveName => String.Format(
         "mrhelper.logs.{0}.zip", DateTime.Now.ToString(TimeStampLogFilenameFormat));

      public static string TimeStampFormat = "d-MMM-yyyy HH:mm";

      public static int FullContextSize = 20000;

      public static string AuthorLabelPrefix = "#";
      public static string GitLabLabelPrefix = "@";

      public static int CheckForUpdatesTimerInterval = 1000 * 60 * 60 * 4; // 4 hours

      public static int DiscussionCheckOnNewThreadInterval = 1000 * 3; // 3 seconds
      public static int DiscussionCheckOnNewThreadFromDiffToolInterval = 1000 * 15; // 15 seconds

      public static int ReloadListPseudoTimerInterval = 100 * 1; // 0.1 second

      public static string[] ThemeNames = { "Default", "New Year 2020" };
      public static string DefaultThemeName = "Default";

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

      public static IEnumerable<string> MainWindowFontSizeChoices = new string[]
         { "Tiny", "Small", "Medium", "Large" };

      public static string DefaultMainWindowFontSizeChoice = "Small";

      public static IEnumerable<string> DiscussionsWindowFontSizeChoices = new string[]
         { "Tiny", "Small", "Medium", "Large", "Meeting Mode" };

      public static int MaxSearchByTitleAndDescriptionResults = 20;

      public static TaskUtils.BatchLimits MergeRequestLoaderProjectBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 20,
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

      // @{ FileStorageUpdater
      public static TaskUtils.BatchLimits ComparisonLoadingForAwaitedUpdateBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 20,
         Delay = 1000
      };

      public static TaskUtils.BatchLimits FileRevisionLoadingForAwaitedUpdateBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 10,
         Delay = 1000
      };

      public static TaskUtils.BatchLimits ComparisonLoadingForNonAwaitedUpdateBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 20,
         Delay = 1000 // this is multiplied by number of storages
      };

      public static TaskUtils.BatchLimits FileRevisionLoadingForNonAwaitedUpdateBatchLimits = new TaskUtils.BatchLimits
      {
         Size = 10,
         Delay = 1000 // this is multiplied by number of storages
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

