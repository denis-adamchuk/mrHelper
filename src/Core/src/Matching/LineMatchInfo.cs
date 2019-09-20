using System;
using System.Diagnostics;
using System.IO;

namespace mrHelper.Core.Matching
{
   /// <summary>
   /// Describes position of a line to be matched between two files
   /// </summary>
   public struct LineMatchInfo
   {
      public bool IsValid()
      {
         if (IsLeftSideLineNumber)
         {
            return LeftFileName != String.Empty && LineNumber > 0;
         }
         else
         {
            return RightFileName != String.Empty && LineNumber > 0;
         }
      }

      new public string ToString()
      {
         return String.Format("\nLeftFileName: {0}\nRightFileName: {1}\nLineNumber: {2}\nIsLeftSideLineNumber: {3}",
            LeftFileName, RightFileName, LineNumber, IsLeftSideLineNumber);
      }

      public string LeftFileName;
      public string RightFileName;
      public int LineNumber;
      public bool IsLeftSideLineNumber;
   }
}

