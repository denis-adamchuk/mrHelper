using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace mrHelper.Core.Interprocess
{
   /// <summary>
   /// It is expected that diff tool provides these details about lines of code a new discussion shall be created for
   /// </summary>
   public struct DiffToolInfo
   {
      public bool IsLeftSide;
      public string FileName;
      public int LineNumber;

      new public string ToString()
      {
         return String.Format("\nFileName: {0}\nLineNumber: {1}\nIsLeftSide: {2}", FileName, LineNumber, IsLeftSide);
      }
   }
}

