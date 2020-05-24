using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace mrHelper.Core.Matching
{
   /// <summary>
   /// Describes position of a line to be matched between two files
   /// </summary>
   public class MatchInfo
   {
      public MatchInfo(string leftFileName, string rightFileName, int lineNumber, bool isLeftSideLineNumber)
      {
         LeftFileName = leftFileName;
         RightFileName = rightFileName;
         LineNumber = lineNumber;
         IsLeftSideLineNumber = isLeftSideLineNumber;
      }

      public bool IsValid()
      {
         return LineNumber > 0 && (IsLeftSideLineNumber ? LeftFileName != String.Empty : RightFileName != String.Empty);
      }

      new public string ToString()
      {
         return String.Format("\nLeftFileName: {0}\nRightFileName: {1}\nLineNumber: {2}\nIsLeftSideLineNumber: {3}",
            LeftFileName, RightFileName, LineNumber, IsLeftSideLineNumber);
      }

      public override bool Equals(object obj)
      {
         return obj is MatchInfo info &&
                LeftFileName == info.LeftFileName &&
                RightFileName == info.RightFileName &&
                LineNumber == info.LineNumber &&
                IsLeftSideLineNumber == info.IsLeftSideLineNumber;
      }

      public override int GetHashCode()
      {
         int hashCode = -1644089817;
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LeftFileName);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RightFileName);
         hashCode = hashCode * -1521134295 + LineNumber.GetHashCode();
         hashCode = hashCode * -1521134295 + IsLeftSideLineNumber.GetHashCode();
         return hashCode;
      }

      public string LeftFileName { get; }
      public string RightFileName { get; }
      public int LineNumber { get; }
      public bool IsLeftSideLineNumber { get; }
   }
}

