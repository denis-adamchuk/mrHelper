using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace mrCore
{
   /// <summary>
   /// It is expected that diff tool provides these details about lines of code a new discussion shall be created for
   /// </summary>
   public struct DiffToolInfo
   {
      public struct Side
      {
         public string FileName;
         public int LineNumber;

         public Side(string filename, int linenumber)
         {
            FileName = filename;
            LineNumber = linenumber;
         }

         new public string ToString()
         {
            return String.Format("\nFileName: {0}\nLineNumber: {1}", FileName, LineNumber.ToString());
         }
      }

      public bool IsValid()
      {
         if (!Left.HasValue && !Right.HasValue)
         {
            return false;
         }
         if (IsLeftSideCurrent && (!Left.HasValue || Left.Value.FileName == null))
         {
            return false;
         }
         if (!IsLeftSideCurrent && (!Right.HasValue || Right.Value.FileName == null))
         {
            return false;
         }
         return true;
      }

      new public string ToString()
      {
         return String.Format("\nLeft: {0}\nRight: {1}\nIsLeftSideCurrent: {2}",
            (Left?.ToString() ?? "null"), (Right?.ToString() ?? "null"), IsLeftSideCurrent);
      }

      public Side? Left;
      public Side? Right;
      public bool IsLeftSideCurrent;
   }
}
