using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Core.Context
{
   internal class Helpers
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
