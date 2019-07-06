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
            Added,
            Removed,
            Unchanged
         }
         
         public struct Side
         {
            public bool Right; // false - Left, true - Right
            public int Number;
            public State State;
         }
         
         public string Text;
         public List<Side> Sides;
      }

      public string FileName;
      public List<Line> Lines;
   }

   public interface ContextMaker
   {
      DiffContext GetContext(Position position, int size);
   }
}
