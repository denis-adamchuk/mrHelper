﻿using System;
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
            Changed,
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

      public List<Line> Lines;

      public int SelectedIndex;
   }

   public interface ContextMaker
   {
      DiffContext GetContext(Position position, int size);
   }
}
