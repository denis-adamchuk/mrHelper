using System;

namespace mrHelper.Core.Matching
{
   /// <summary>
   /// Git SHAs corresponding to left and right commits in diff
   /// </summary>
   public struct DiffRefs
   {
      public string LeftSHA;
      public string RightSHA;

      new public string ToString()
      {
         return String.Format("\nLeftSHA: {0}\nRightSHA: {1}",
            (LeftSHA?.ToString() ?? "null"), (RightSHA?.ToString() ?? "null"));
      }
   }
}

