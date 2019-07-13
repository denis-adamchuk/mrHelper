using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrCore.Context
{
   class Helpers
   {
      public static bool IsValidPosition(Position position)
      {
         int oldLineNumber = 0;
         int newLineNumber = 0;
         return ((position.OldLine != null && int.TryParse(position.OldLine, out oldLineNumber) && oldLineNumber > 0)
              || (position.NewLine != null && int.TryParse(position.NewLine, out newLineNumber) && newLineNumber > 0));
      }

      public static bool IsValidContextDepth(ContextDepth depth)
      {
         return depth.Up <= depth.Down && Math.Min(depth.Up, depth.Down) >= 0;
      }
   }
}
