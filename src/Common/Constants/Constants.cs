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

      public static string ProjectListFileName = "projects.json";

      public static string TimeStampFilenameFormat = "yyyy_MM_dd_HHmmss";

      public static string BugReportLogArchiveName => String.Format(
         "mrhelper.logs.{0}.zip", DateTime.Now.ToString(TimeStampFilenameFormat));

      public static int FullContextSize = 20000;
   }
}

