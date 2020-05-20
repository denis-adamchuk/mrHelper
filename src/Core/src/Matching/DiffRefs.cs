using System;

namespace mrHelper.Core.Matching
{
   /// <summary>
   /// Git SHAs corresponding to left and right commits in diff
   /// </summary>
   public class DiffRefs
   {
      public DiffRefs(string leftSHA, string rightSHA)
      {
         LeftSHA = leftSHA;
         RightSHA = rightSHA;
      }

      public string LeftSHA { get; }
      public string RightSHA { get; }

      new public string ToString()
      {
         return String.Format("\nLeftSHA: {0}\nRightSHA: {1}",
            (LeftSHA?.ToString() ?? "null"), (RightSHA?.ToString() ?? "null"));
      }
   }
}

