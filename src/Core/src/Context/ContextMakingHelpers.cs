﻿using System;
using mrHelper.Core.Matching;

namespace mrHelper.Core.Context
{
   public static class Helpers
   {
      public static bool IsValidPosition(DiffPosition position)
      {
         if (position == null)
         {
            return false;
         }
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

      public static bool IsValidContextDepth(ContextDepth depth)
      {
         return depth.Up <= depth.Down && Math.Min(depth.Up, depth.Down) >= 0;
      }
   }
}
