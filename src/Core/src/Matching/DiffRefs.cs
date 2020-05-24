using System;
using System.Collections.Generic;

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

      public override bool Equals(object obj)
      {
         return obj is DiffRefs refs &&
                LeftSHA == refs.LeftSHA &&
                RightSHA == refs.RightSHA;
      }

      public override int GetHashCode()
      {
         int hashCode = -2045036093;
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LeftSHA);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RightSHA);
         return hashCode;
      }

      new public string ToString()
      {
         return String.Format("\nLeftSHA: {0}\nRightSHA: {1}",
            (LeftSHA?.ToString() ?? "null"), (RightSHA?.ToString() ?? "null"));
      }
   }
}

