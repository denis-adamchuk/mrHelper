using System;

namespace mrCore
{
   /// <summary>
   /// Defines position of two lines in two files according to the following rules:
   /// (a) If left-side file is missing at all:
   ///   LeftPath = RightPath
   ///   LeftLine = null
   ///   RightLine = line number
   /// (b) If right-side file is missing at all:
   ///   LeftPath = RightPath
   ///   LeftLine = line number
   ///   RightLine = null
   /// (c) If line is a new line in the right-side file
   ///   LeftPath = left-side filename
   ///   RightPath = right-side filename // can be different from LeftPath if renamed
   ///   LeftLine = null
   ///   RightLine = line number
   /// (d) If line is a removed line in the left-side file
   ///   LeftPath = left-side filename
   ///   RightPath = right-side filename // can be different from LeftPath if renamed
   ///   LeftLine = line number
   ///   RightLine = null
   /// (e) If line is unchanged in two files:
   ///   LeftPath = left-side filename
   ///   RightPath = right-side filename // can be different from LeftPath if renamed
   ///   LeftLine = line number in the left-side file
   ///   RightLine = line number in the right-side file
   /// Refs is just a pair of SHAs that correspond to this diff
   /// </summary>
   public struct DiffPosition
   {
      public string LeftPath;
      public string RightPath;
      public string LeftLine;
      public string RightLine;
      public DiffRefs Refs;

      new public string ToString()
      {
         return String.Format("\nLeftPath: {0}\nRightPath: {1}\nLeftLine: {2}\nRightLine: {3}\nRefs: {4}",
            LeftPath, RightPath, LeftLine, RightLine, Refs.ToString());
      }
   }

   /// <summary>
   /// Git SHAs corresponding to left and right commits in diff
   /// </summary>
   public struct DiffRefs
   {
      public string LeftSHA;
      public string RightSHA;

      new public string ToString()
      {
         return String.Format("\nLeftSHA: {0}\nRightSHA: {1}", LeftSHA, RightSHA);
      }
   }
}

