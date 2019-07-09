using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrCore
{
   public struct DiffContext
   {
      public struct Line
      {
         public enum State
         {
            // TODO No need to have both Added and Removed because we cannot have Added for Left and Removed for Right
            Added,
            Removed,
            Unchanged
         }
         
         public struct Side
         {
            public int Number;
            public State State;
         }
         
         public string Text;
         public Side? Left;
         public Side? Right;
      }

      public string FileName;
      public List<Line> Lines;
   }

   public interface ContextMaker
   {
      DiffContext GetContext(Position position, int size);
   }
}
