using System;
using System.Linq;

namespace mrHelper.Common.Tools
{
   public static class FileUtils
   {
      public static void OverwriteFile(string filename, string content)
      {
         if (System.IO.File.Exists(filename))
         {
            System.IO.File.Delete(filename);
         }

         System.IO.File.WriteAllText(filename, content);
      }
   }
}

