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
      public static string NewDiscussionCaption = "Create New Discussion";

      public static string IconSchemeFileName = "icons.json";
      public static string ProjectListFileName = "projects.json";

      public static string TimeStampFilenameFormat = "yyyy_MM_dd_HHmmss";

      public static string BugReportLogArchiveName => String.Format(
         "mrhelper.logs.{0}.zip", DateTime.Now.ToString(TimeStampFilenameFormat));

      public static int FullContextSize = 20000;

      public static string AuthorLabelPrefix = "#";
      public static string GitLabLabelPrefix = "@";

      public static int CheckForUpdatesTimerInterval = 1000 * 60 * 60 * 4; // 4 hours

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
   }
}

