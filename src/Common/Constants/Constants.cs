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
      public static string DefaultThemeName = "New Year 2020";

      public static Dictionary<string, double> FontSizeChoices = new Dictionary<string, double>
      {
         { "Tiny",   1.00 },
         { "Small",  1.10 },
         { "Medium", 1.20 },
         { "Large",  1.30 },
         { "Giant",  1.40 }
      };
      public static string DefaultFontSizeChoice = "Tiny";

      public static double OriginalFontEmSize = 0;
   }
}

