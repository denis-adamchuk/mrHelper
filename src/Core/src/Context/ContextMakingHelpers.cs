using System;
using mrHelper.Core.Matching;

namespace mrHelper.Core.Context
{
   public static class Helpers
   {
      public static bool IsValidPosition(DiffPosition position)
      {
         string leftLine = position.LeftLine;
         string rightLine = position.RightLine;
         if (leftLine != null && GetLeftLineNumber(position) < 1)
         {
            return false;
         }
         if (rightLine != null && GetRightLineNumber(position) < 1)
         {
            return false;
         }
         return leftLine != null || rightLine != null;
      }

      public static bool IsRightSidePosition(DiffPosition position,
         UnchangedLinePolicy unchangedLinePolicy = UnchangedLinePolicy.TakeFromRight)
      {
         if (position.RightLine != null && position.LeftLine != null)
         {
            return unchangedLinePolicy == UnchangedLinePolicy.TakeFromRight;
         }
         return position.RightLine != null;
      }

      public static bool IsLeftSidePosition(DiffPosition position,
         UnchangedLinePolicy unchangedLinePolicy = UnchangedLinePolicy.TakeFromLeft)
      {
         if (position.RightLine != null && position.LeftLine != null)
         {
            return unchangedLinePolicy == UnchangedLinePolicy.TakeFromLeft;
         }
         return position.LeftLine != null;
      }

      public static int GetRightLineNumber(DiffPosition position)
      {
         return int.TryParse(position.RightLine, out int lineNumber) ? lineNumber : default(int);
      }

      public static int GetLeftLineNumber(DiffPosition position)
      {
         return int.TryParse(position.LeftLine, out int lineNumber) ? lineNumber : default(int);
      }

      public static DiffPosition Scroll(DiffPosition position, bool up)
      {
         if (IsValidPosition(position))
         {
            if (IsLeftSidePosition(position))
            {
               int lineNumber = GetLeftLineNumber(position);
               return new DiffPosition(
                  position.LeftPath,
                  position.RightPath,
                  (lineNumber + 1* (up ? -1 : 1 )).ToString(),
                  position.RightLine,
                  position.Refs);
            }
            else if (IsRightSidePosition(position))
            {
               int lineNumber = GetRightLineNumber(position);
               return new DiffPosition(
                  position.LeftPath,
                  position.RightPath,
                  position.LeftLine,
                  (lineNumber + 1 * (up ? -1 : 1 )).ToString(),
                  position.Refs);
            }
         }
         return position;
      }

      public static bool IsValidContextDepth(ContextDepth depth)
      {
         return depth.Up <= depth.Down && Math.Min(depth.Up, depth.Down) >= 0;
      }
   }
}
