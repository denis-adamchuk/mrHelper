using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Common.Constants
{
   public static class Constants
   {
      public static string CustomProtocolName = "mrhelper";
      public static string MainWindowCaption  = "Merge Request Helper";
      public static string StartNewThreadCaption = "Start New Thread";

      public static int MaxLabelRows = 3;
      public static string MoreLabelsHint = "See more labels in tooltip";

      public static string IconSchemeFileName = "icons.json";
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

      public static int GitInstancesInBatch = 10;
      public static int GitInstancesInterBatchDelay = 1000; // ms

      public static int MergeRequestsInBatch = 20;
      public static int MergeRequestsInterBatchDelay = 0;

      public static int CrossProjectMergeRequestsInBatch = 20;
      public static int CrossProjectMergeRequestsInterBatchDelay = 200;

      public static int ProjectsInBatch = 20;
      public static int ProjectsInterBatchDelay = 0;

      public static int VersionsInBatch = 20;
      public static int VersionsInterBatchDelay = 0;

      public static int BranchInBatch = 10;
      public static int BranchInterBatchDelay = 0;
   }
}

