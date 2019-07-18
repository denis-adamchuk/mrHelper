using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrCore.Context
{
   internal class Helpers
   {
      public static bool IsValidPosition(DiffPosition position)
      {
         return ((position.LeftLine != null && int.TryParse(position.LeftLine, out int LeftLineNumber) && LeftLineNumber > 0)
              || (position.RightLine != null && int.TryParse(position.RightLine, out int RightLineNumber) && RightLineNumber > 0));
      }

      public static bool IsValidContextDepth(ContextDepth depth)
      {
         return depth.Up <= depth.Down && Math.Min(depth.Up, depth.Down) >= 0;
      }
   }
}
