using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrCore.Context
{
   internal class Helpers
   {
      public static bool IsValidPosition(Position position)
      {
         return ((position.OldLine != null && int.TryParse(position.OldLine, out int oldLineNumber) && oldLineNumber > 0)
              || (position.NewLine != null && int.TryParse(position.NewLine, out int newLineNumber) && newLineNumber > 0));
      }

      public static bool IsValidContextDepth(ContextDepth depth)
      {
         return depth.Up <= depth.Down && Math.Min(depth.Up, depth.Down) >= 0;
      }
   }
}
