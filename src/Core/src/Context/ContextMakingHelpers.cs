using System;
using mrHelper.Core.Matching;

namespace mrHelper.Core.Context
{
   internal static class Helpers
   {
      public static bool IsValidPosition(DiffPosition position)
      {
         var leftLine = position.LeftLine;
         var rightLine = position.RightLine;
         return ((leftLine != null && int.TryParse(leftLine, out int LeftLineNumber) && LeftLineNumber > 0)
              || (rightLine != null && int.TryParse(rightLine, out int RightLineNumber) && RightLineNumber > 0));
      }

      public static bool IsValidContextDepth(ContextDepth depth)
      {
         return depth.Up <= depth.Down && Math.Min(depth.Up, depth.Down) >= 0;
      }
   }
}
